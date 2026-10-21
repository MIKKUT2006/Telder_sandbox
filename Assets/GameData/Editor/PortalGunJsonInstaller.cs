#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Game.World.Dimensions;

namespace Game.EditorTools
{
    public static class PortalGunJsonInstaller
    {
        private const string DefaultSettings =
            "  \"PortalGun\": {\n" +
            "    \"CrystalItemId\": \"game:crystal\",\n" +
            "    \"CrystalCost\": 1,\n" +
            "    \"BeamSpeed\": 28.0,\n" +
            "    \"BeamWidth\": 5.0,\n" +
            "    \"BeamDistortion\": 6.0,\n" +
            "    \"BeamStrength\": 1.0,\n" +
            "    \"BeamColor\": \"#62E9FF\"\n" +
            "  }";

        [MenuItem("Tools/Game/Portalgun/Add settings to existing item JSON")]
        public static void AddSettingsToExistingPortalGun()
        {
            string folder = Path.Combine(Application.dataPath, "GameData", "Items");
            if (!Directory.Exists(folder))
            {
                EditorUtility.DisplayDialog("Portalgun", "Не найдена папка предметов:\n" + folder, "OK");
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            var matches = files
                .Select(path => new { Path = path, Json = SafeRead(path) })
                .Where(x => !string.IsNullOrEmpty(x.Json))
                .Select(x => new
                {
                    x.Path,
                    x.Json,
                    Score = Regex.IsMatch(x.Json, "\"Texture\"\\s*:\\s*\"portal[_-]?gun\"", RegexOptions.IgnoreCase) ? 100 :
                            Regex.IsMatch(x.Json, "\"ID\"\\s*:\\s*\"[^\"]*portal[_-]?gun", RegexOptions.IgnoreCase) ? 50 :
                            Regex.IsMatch(x.Json, "\"Name\"\\s*:\\s*\"portalgun\"", RegexOptions.IgnoreCase) ? 10 : 0
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Path.IndexOf("_WeaponExamples", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (matches.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Portalgun",
                    "Не найден JSON предмета с Texture = portal_gun или ID, содержащим portal_gun.\n\n" +
                    "Открой существующий JSON и перенеси в него объект PortalGun из PORTALGUN_JSON_EXAMPLE.json.",
                    "OK");
                return;
            }

            var target = matches[0];
            if (Regex.IsMatch(target.Json, "\"PortalGun\"\\s*:", RegexOptions.IgnoreCase))
            {
                PortalGunConfigRegistry.Reload();
                EditorUtility.DisplayDialog("Portalgun", "Секция PortalGun уже существует:\n" + target.Path, "OK");
                return;
            }

            try
            {
                string updated = InsertRootProperty(target.Json, DefaultSettings);
                if (updated == null)
                    throw new InvalidDataException("Не удалось определить закрывающую скобку корневого JSON-объекта.");

                string backup = target.Path + ".portalgun.bak";
                if (!File.Exists(backup))
                    File.Copy(target.Path, backup);

                File.WriteAllText(target.Path, updated, new UTF8Encoding(false));
                AssetDatabase.Refresh();
                PortalGunConfigRegistry.Reload();

                EditorUtility.DisplayDialog(
                    "Portalgun",
                    "Настройки добавлены в JSON:\n" + target.Path +
                    "\n\nРезервная копия:\n" + backup +
                    "\n\nТеперь можно менять CrystalCost и остальные параметры прямо в JSON.",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Portalgun", "Не удалось обновить JSON:\n" + e.Message, "OK");
            }
        }

        private static string SafeRead(string path)
        {
            try { return File.ReadAllText(path); }
            catch { return null; }
        }

        private static string InsertRootProperty(string json, string property)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            int rootOpen = json.IndexOf('{');
            if (rootOpen < 0) return null;

            bool inString = false;
            bool escaped = false;
            int depth = 0;
            int rootClose = -1;
            for (int i = rootOpen; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') { inString = true; continue; }
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) { rootClose = i; break; }
                }
            }

            if (rootClose < 0) return null;
            int lastContent = rootClose - 1;
            while (lastContent > rootOpen && char.IsWhiteSpace(json[lastContent])) lastContent--;
            bool hasExistingProperties = lastContent > rootOpen && json[lastContent] != '{';
            string prefix = hasExistingProperties ? ",\n" : "\n";
            return json.Insert(rootClose, prefix + property + "\n");
        }
    }
}
#endif
