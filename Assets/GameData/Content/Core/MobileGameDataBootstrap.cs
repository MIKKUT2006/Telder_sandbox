using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Content
{
    /// <summary>
    /// Android: wait for the GameData payload bundled in StreamingAssets to
    /// be copied and extracted to persistentDataPath. Regular File/Directory
    /// APIs cannot read directly from jar:file://.../base.apk!/assets/...
    /// </summary>
    public sealed class MobileGameDataBootstrap : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const string RelativeZip = "Telder/GameData.zip";
        private const string RelativeHash = "Telder/GameData.sha256";
        private const string VersionFile = ".telder_archive_sha256";

        private static MobileGameDataBootstrap instance;
        private static bool ready;
        private static string error;
        private static float progress;
        private static string phase = "Подготовка ресурсов...";
        private Coroutine installCoroutine;
        private Texture2D backdrop;

        public static bool IsReady { get { return ready; } }
        public static bool HasError { get { return !string.IsNullOrEmpty(error); } }
        public static string ErrorMessage { get { return error; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartBeforeFirstScene()
        {
            if (instance != null) return;
            var go = new GameObject("Telder Android GameData Installer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MobileGameDataBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            backdrop = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            backdrop.SetPixel(0, 0, Color.black);
            backdrop.Apply();
            installCoroutine = StartCoroutine(Install());
        }

        private IEnumerator Install()
        {
            ready = false;
            error = null;
            progress = 0f;
            string userDataRoot = Path.Combine(Application.persistentDataPath, "GameData");
            string workingZip = Path.Combine(Application.persistentDataPath, "telder_gamedata_pending.zip");
            string stagingRoot = Path.Combine(Application.persistentDataPath, "GameData_installing");
            string backupRoot = Path.Combine(Application.persistentDataPath, "GameData_old");
            string expectedHash = null;
            phase = "Проверка ресурсов игры...";

            // StreamingAssets is a file:// path on iOS/desktop, but a jar:
            // URL on Android. UnityWebRequest is mandatory for APK assets.
            string baseUrl = Application.streamingAssetsPath.TrimEnd('/', '\\');
            using (var request = UnityWebRequest.Get(baseUrl + "/" + RelativeHash))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Fail("Архив GameData отсутствует в APK (manifest). " + request.error +
                        ". Пересобери Android-билд в Unity.");
                    yield break;
                }
                expectedHash = request.downloadHandler.text.Trim().ToLowerInvariant();
            }
            if (expectedHash.Length != 64)
            {
                Fail("Неверная версия архива GameData в APK.");
                yield break;
            }
            try
            {
                // Recover the previous install if Android was killed during
                // an update between the two directory renames.
                if (!Directory.Exists(userDataRoot) && Directory.Exists(backupRoot))
                    Directory.Move(backupRoot, userDataRoot);

                string installedHash = Path.Combine(userDataRoot, VersionFile);
                if (File.Exists(installedHash) &&
                    string.Equals(File.ReadAllText(installedHash).Trim(), expectedHash,
                        StringComparison.OrdinalIgnoreCase) && ValidContent(userDataRoot))
                {
                    progress = 1f;
                    ready = true;
                    Debug.Log("TELDER ANDROID: GameData already installed: " + userDataRoot);
                    yield break;
                }
                if (File.Exists(workingZip)) File.Delete(workingZip);
                if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true);
            }
            catch (Exception exception)
            {
                Fail("Не удалось подготовить папку GameData: " + exception.Message);
                yield break;
            }

            phase = "Распаковка ресурсов: получение архива...";
            // DownloadHandlerFile does not allocate a whole APK payload in RAM.
            using (var request = new UnityWebRequest(baseUrl + "/" + RelativeZip, "GET"))
            {
                request.downloadHandler = new DownloadHandlerFile(workingZip);
                var requestOperation = request.SendWebRequest();
                while (!requestOperation.isDone)
                {
                    progress = Mathf.Clamp01(request.downloadProgress) * 0.35f;
                    yield return null;
                }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Fail("Не удалось прочитать GameData.zip из APK: " + request.error);
                    yield break;
                }
            }

            phase = "Проверка целостности архива...";
            try
            {
                string actualHash;
                using (var input = File.OpenRead(workingZip))
                using (var sha = SHA256.Create())
                    actualHash = BitConverter.ToString(sha.ComputeHash(input))
                        .Replace("-", "").ToLowerInvariant();
                if (actualHash != expectedHash)
                    throw new InvalidDataException("Контрольная сумма GameData не совпадает");
            }
            catch (Exception exception)
            {
                Fail("Ошибка архива GameData: " + exception.Message);
                yield break;
            }

            phase = "Распаковка игровых данных...";
            // Incremental extraction: give Android one frame every few files.
            // Never remove the installed version before staging passes checks.
            ZipArchive zip = null;
            FileStream stream = null;
            try
            {
                Directory.CreateDirectory(stagingRoot);
                stream = File.OpenRead(workingZip);
                zip = new ZipArchive(stream, ZipArchiveMode.Read, false);
            }
            catch (Exception exception)
            {
                if (zip != null) zip.Dispose();
                if (stream != null) stream.Dispose();
                Fail("Не удалось открыть GameData: " + exception.Message);
                yield break;
            }

            bool extractionFailed = false;
            string extractionError = null;
            int total = zip.Entries.Count;
            for (int i = 0; i < total; i++)
            {
                try
                {
                    ZipArchiveEntry entry = zip.Entries[i];
                    string relative = entry.FullName.Replace('\\', '/');
                    // Prevent ZIP entries from escaping the install directory.
                    if (relative.StartsWith("/", StringComparison.Ordinal) ||
                        relative.StartsWith("../", StringComparison.Ordinal) ||
                        relative.Contains("/../") || relative.Contains(":") ||
                        relative.StartsWith("./", StringComparison.Ordinal))
                        throw new InvalidDataException("Unsafe ZIP entry: " + relative);
                    string destination = Path.Combine(stagingRoot,
                        relative.Replace('/', Path.DirectorySeparatorChar));
                    if (relative.EndsWith("/", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(destination);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        using (var input = entry.Open())
                        using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write))
                            input.CopyTo(output);
                    }
                }
                catch (Exception exception)
                {
                    extractionFailed = true;
                    extractionError = exception.Message;
                    break;
                }
                progress = 0.4f + (i + 1f) / Mathf.Max(1, total) * 0.55f;
                if ((i & 7) == 0) yield return null;
            }
            zip.Dispose();
            // ZipArchive owns stream when leaveOpen=false.
            if (extractionFailed)
            {
                Fail("Не удалось распаковать GameData: " + extractionError);
                yield break;
            }

            try
            {
                if (!ValidContent(stagingRoot))
                    throw new InvalidDataException("В архиве нет Blocks/Items/ResourcePacks текстур");

                File.WriteAllText(Path.Combine(stagingRoot, VersionFile), expectedHash);
                if (Directory.Exists(backupRoot)) Directory.Delete(backupRoot, true);
                if (Directory.Exists(userDataRoot)) Directory.Move(userDataRoot, backupRoot);
                try
                {
                    Directory.Move(stagingRoot, userDataRoot);
                }
                catch
                {
                    if (!Directory.Exists(userDataRoot) && Directory.Exists(backupRoot))
                        Directory.Move(backupRoot, userDataRoot);
                    throw;
                }
                progress = 1f;
                ready = true;
                Debug.Log("TELDER ANDROID: GameData installed successfully at " + userDataRoot);
                try
                {
                    if (Directory.Exists(backupRoot)) Directory.Delete(backupRoot, true);
                    if (File.Exists(workingZip)) File.Delete(workingZip);
                }
                catch (Exception cleanupError)
                {
                    Debug.LogWarning("TELDER ANDROID: old files cleanup delayed: " + cleanupError.Message);
                }
            }
            catch (Exception exception)
            {
                Fail("Установка GameData не завершена: " + exception.Message);
            }
        }

        private static bool ValidContent(string folder)
        {
            return Directory.Exists(Path.Combine(folder, "Blocks")) &&
                Directory.GetFiles(Path.Combine(folder, "Blocks"), "*.json").Length > 0 &&
                Directory.Exists(Path.Combine(folder, "Items")) &&
                Directory.GetFiles(Path.Combine(folder, "Items"), "*.json").Length > 0 &&
                Directory.Exists(Path.Combine(folder, "ResourcePacks", "Default", "textures", "blocks")) &&
                Directory.GetFiles(Path.Combine(folder, "ResourcePacks", "Default", "textures", "blocks"), "*.png").Length > 0;
        }

        private static void Fail(string message)
        {
            error = message;
            Debug.LogError("TELDER ANDROID GAMEDATA: " + message);
        }

        private void OnGUI()
        {
            if (ready) return;
            GUI.depth = -100001;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backdrop);
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;
            style.fontSize = Mathf.Max(18, Screen.height / 28);
            style.normal.textColor = Color.white;
            string message = HasError
                ? "Ошибка загрузки ресурсов\n" + error
                : phase + "\n" + Mathf.RoundToInt(progress * 100f) + "%";
            GUI.Label(new Rect(Screen.width * .1f, Screen.height * .3f,
                Screen.width * .8f, Screen.height * .27f), message, style);
            if (HasError && GUI.Button(new Rect(Screen.width * .36f, Screen.height * .65f,
                Screen.width * .28f, Screen.height * .11f), "Повторить"))
            {
                if (installCoroutine != null) StopCoroutine(installCoroutine);
                installCoroutine = StartCoroutine(Install());
            }
        }

        private void OnDestroy()
        {
            if (backdrop != null) Destroy(backdrop);
            if (instance == this) instance = null;
        }
#else
        // In Editor and standalone builds regular GameData files are used.
        public static bool IsReady { get { return true; } }
        public static bool HasError { get { return false; } }
        public static string ErrorMessage { get { return null; } }
#endif
    }
}
