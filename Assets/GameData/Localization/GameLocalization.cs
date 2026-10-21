using System;
using System.Collections.Generic;
using System.IO;
using Game.Content;
using UnityEngine;

namespace Game.Localization
{
    /// <summary>
    /// File-based translations. A file is GameData/Localization/<language>.json.
    /// Language is a global player preference; content IDs and save data never change.
    /// JsonUtility requires Entries to be an array rather than a JSON dictionary.
    /// </summary>
    public static class GameLocalization
    {
        [Serializable] public sealed class Entry
        {
            public string Key;
            public string Text;
            public string Source; // Russian/original UI label (for legacy scene text)
        }

        [Serializable] public sealed class LanguageFile
        {
            public string Language;
            public string DisplayName;
            public Entry[] Entries;
        }

        private const string LanguagePreference = "Telder.Language";
        private static readonly Dictionary<string, LanguageFile> files =
            new Dictionary<string, LanguageFile>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, string>> translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> uiSourceKeys =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly List<string> available = new List<string>();
        private static string loadedFolder;
        private static string language;
        private static int version;

        public static event Action Changed;
        public static int Version { get { return version; } }
        public static string CurrentLanguage
        {
            get
            {
                if (string.IsNullOrEmpty(language))
                    language = PlayerPrefs.GetString(LanguagePreference, "ru");
                return language;
            }
        }

        public static string CurrentDisplayName
        {
            get
            {
                EnsureLoaded();
                LanguageFile file;
                if (files.TryGetValue(CurrentLanguage, out file) &&
                    !string.IsNullOrWhiteSpace(file.DisplayName)) return file.DisplayName;
                return CurrentLanguage == "ru" ? "Русский" : CurrentLanguage == "en" ? "English" : CurrentLanguage;
            }
        }

        public static string[] AvailableLanguages
        {
            get
            {
                EnsureLoaded();
                return available.Count > 0 ? available.ToArray() : new[] { "ru", "en" };
            }
        }

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            code = code.Trim().ToLowerInvariant();
            EnsureLoaded();
            if (CurrentLanguage == code) return;
            // Avoid writing a bogus language ID that doesn't have a dictionary.
            if (files.Count > 0 && !files.ContainsKey(code)) return;
            language = code;
            PlayerPrefs.SetString(LanguagePreference, language);
            PlayerPrefs.Save();
            ++version;
            if (Changed != null) Changed();
        }

        public static void NextLanguage()
        {
            string[] choices = AvailableLanguages;
            if (choices.Length == 0) return;
            int index = Array.FindIndex(choices, x => string.Equals(x, CurrentLanguage, StringComparison.OrdinalIgnoreCase));
            SetLanguage(choices[(index + 1) % choices.Length]);
        }

        /// <summary>Explicit keys: item.game:stone.name, item.game:stone.description,
        /// block.game:grass.name and any ui.* key. Missing translations fall back safely.</summary>
        public static string Get(string key, string fallback = "")
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return fallback ?? "";
            Dictionary<string, string> dict;
            string value;
            if (translations.TryGetValue(CurrentLanguage, out dict) &&
                dict.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return value;
            if (translations.TryGetValue("ru", out dict) &&
                dict.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return value;
            return fallback ?? "";
        }

        public static string ContentName(string id, string fallback)
        {
            if (string.IsNullOrEmpty(id)) return fallback ?? "";
            return Get("item." + id + ".name", Get("block." + id + ".name", fallback));
        }

        public static string ContentDescription(string id, string fallback)
        {
            return string.IsNullOrEmpty(id) ? (fallback ?? "") :
                Get("item." + id + ".description", Get("block." + id + ".description", fallback));
        }

        /// <summary>Localize legacy TMP/UI.Text labels without editing every scene/prefab.</summary>
        public static string UI(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            EnsureLoaded();
            string key;
            return uiSourceKeys.TryGetValue(raw, out key) ? Get(key, raw) : raw;
        }

        public static void EnsureLoaded()
        {
            string folder = Path.Combine(DataPaths.GameDataFolder, "Localization");
            if (string.Equals(folder, loadedFolder, StringComparison.Ordinal) && files.Count > 0)
                return;
            if (!Directory.Exists(folder)) return; // Android: bundle may still be extracting.
            Reload();
        }

        public static void Reload()
        {
            string folder = Path.Combine(DataPaths.GameDataFolder, "Localization");
            if (!Directory.Exists(folder)) return;
            files.Clear(); translations.Clear(); uiSourceKeys.Clear(); available.Clear();
            foreach (string path in Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    LanguageFile parsed = JsonUtility.FromJson<LanguageFile>(File.ReadAllText(path));
                    if (parsed == null || string.IsNullOrWhiteSpace(parsed.Language)) continue;
                    string code = parsed.Language.Trim().ToLowerInvariant();
                    var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (parsed.Entries != null)
                    {
                        foreach (Entry entry in parsed.Entries)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.Key)) continue;
                            dict[entry.Key] = entry.Text ?? "";
                            if (!string.IsNullOrEmpty(entry.Source)) uiSourceKeys[entry.Source] = entry.Key;
                        }
                    }
                    files[code] = parsed;
                    translations[code] = dict;
                    available.Add(code);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("LOCALIZATION: Error loading " + path + ": " + ex.Message);
                }
            }
            available.Sort((a, b) => a == "ru" ? -1 : b == "ru" ? 1 : string.CompareOrdinal(a, b));
            loadedFolder = folder;
            ++version;
            if (Changed != null) Changed();
        }
    }
}
