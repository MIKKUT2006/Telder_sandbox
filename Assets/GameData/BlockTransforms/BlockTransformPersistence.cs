using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.BlockTransforms
{
    public sealed class BlockTransformPersistence : MonoBehaviour
    {
        private static BlockTransformPersistence instance;
        private static bool dirty;

        private string contextKey;
        private float nextContextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void EnsureInstance()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("BlockTransformPersistence");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BlockTransformPersistence>();
        }

        public static void MarkDirty()
        {
            dirty = true;
            EnsureInstance();
        }

        public static void SaveNow()
        {
            EnsureInstance();
            instance.SaveCurrent();
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
            SceneManager.activeSceneChanged += OnSceneChanged;
            SwitchContext(true);
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.activeSceneChanged -= OnSceneChanged;
            SaveCurrent();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            SaveCurrent();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextContextCheck)
            {
                nextContextCheck = Time.unscaledTime + 0.5f;
                SwitchContext(false);
            }

            if (dirty && !string.IsNullOrEmpty(contextKey))
                SaveCurrent();
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            SwitchContext(true);
        }

        private void SwitchContext(bool force)
        {
            string newKey = ResolveContextKey();

            if (!force && string.Equals(contextKey, newKey, StringComparison.Ordinal))
                return;

            if (!string.IsNullOrEmpty(contextKey) && dirty)
                SaveCurrent();

            contextKey = newKey;
            LoadCurrent();
        }

        private void LoadCurrent()
        {
            dirty = false;
            BlockTransformRegistry.ClearAll(false);

            string path = CurrentPath();
            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                BlockTransformSaveFile file =
                    JsonUtility.FromJson<BlockTransformSaveFile>(json);

                BlockTransformRegistry.Import(
                    file != null ? file.Entries : null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "BLOCK TRANSFORMS: load failed: " + path + "\n" + exception);
            }
        }

        private void SaveCurrent()
        {
            if (string.IsNullOrEmpty(contextKey))
                return;

            try
            {
                string path = CurrentPath();
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                BlockTransformSaveFile file = new BlockTransformSaveFile
                {
                    Version = 1,
                    Entries = BlockTransformRegistry.Export()
                };

                File.WriteAllText(path, JsonUtility.ToJson(file, true));
                dirty = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "BLOCK TRANSFORMS: save failed.\n" + exception);
            }
        }

        private string CurrentPath()
        {
            return Path.Combine(
                Application.persistentDataPath,
                "BlockTransforms",
                MakeSafeFileName(contextKey) + ".json");
        }

        private static string ResolveContextKey()
        {
            string save = ResolveStaticValue(
                "SaveGameRuntime",
                new[] { "CurrentSaveId", "CurrentSaveID", "SaveId", "SaveID", "CurrentSave", "Current" });

            string dimension = ResolveStaticValue(
                "DimensionTravelRuntime",
                new[] { "CurrentDimension", "Current", "Dimension", "DimensionName" });

            if (string.IsNullOrEmpty(save))
                save = "default-save";

            if (string.IsNullOrEmpty(dimension))
                dimension = SceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(dimension))
                dimension = "default-dimension";

            return save + "__" + dimension;
        }

        private static string ResolveStaticValue(string typeName, string[] members)
        {
            Type type = FindType(typeName);
            if (type == null)
                return null;

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            for (int i = 0; i < members.Length; i++)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(members[i], flags);
                    if (property != null && property.GetIndexParameters().Length == 0)
                    {
                        string text = ExtractName(property.GetValue(null, null));
                        if (!string.IsNullOrEmpty(text))
                            return text;
                    }

                    FieldInfo field = type.GetField(members[i], flags);
                    if (field != null)
                    {
                        string text = ExtractName(field.GetValue(null));
                        if (!string.IsNullOrEmpty(text))
                            return text;
                    }
                }
                catch { }
            }

            return null;
        }

        private static string ExtractName(object value)
        {
            if (value == null)
                return null;

            string direct = value as string;
            if (direct != null)
                return direct;

            Type type = value.GetType();
            string[] names = { "SaveId", "SaveID", "Id", "ID", "Name", "DimensionName" };
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(names[i], flags);
                    if (property != null && property.GetIndexParameters().Length == 0)
                    {
                        object nested = property.GetValue(value, null);
                        if (nested != null)
                            return nested.ToString();
                    }

                    FieldInfo field = type.GetField(names[i], flags);
                    if (field != null)
                    {
                        object nested = field.GetValue(value);
                        if (nested != null)
                            return nested.ToString();
                    }
                }
                catch { }
            }

            return value.ToString();
        }

        private static Type FindType(string name)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try { types = assemblies[i].GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types; }
                catch { continue; }

                if (types == null)
                    continue;

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j] != null && types[j].Name == name)
                        return types[j];
                }
            }

            return null;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "default";

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                value = value.Replace(invalid[i], '_');

            return value;
        }
    }
}
