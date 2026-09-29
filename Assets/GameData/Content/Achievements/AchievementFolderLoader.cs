using System;
using System.IO;
using UnityEngine;
using Game.Content;

namespace Game.Achievements
{
    public static class AchievementFolderLoader
    {
        [Serializable]
        private class JsonAchievement
        {
            public string ID;
            public string Title;
            public string Description;
            public string Icon;
            public string Parent;
            public bool HiddenUntilUnlocked;
            public int SortOrder;
            public AchievementCondition[] Conditions;
        }

        public static void LoadDefaultFolder()
        {
            string folder = Path.Combine(Application.dataPath, "GameData", "Achievements");
            Directory.CreateDirectory(folder);
            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++) LoadFile(files[i]);
        }

        private static void LoadFile(string path)
        {
            try
            {
                JsonAchievement dto = JsonUtility.FromJson<JsonAchievement>(File.ReadAllText(path));
                if (dto == null || string.IsNullOrWhiteSpace(dto.ID)) return;
                AchievementDefinition def = new AchievementDefinition();
                def.ID = ContentID.Parse(dto.ID.Trim());
                def.Title = string.IsNullOrWhiteSpace(dto.Title) ? dto.ID : dto.Title;
                def.Description = dto.Description ?? "";
                def.Icon = dto.Icon ?? "";
                def.Parent = dto.Parent ?? "";
                def.HiddenUntilUnlocked = dto.HiddenUntilUnlocked;
                def.SortOrder = dto.SortOrder;
                if (dto.Conditions != null) def.Conditions.AddRange(dto.Conditions);
                AchievementRegistry.Register(def);
            }
            catch (Exception e)
            {
                Debug.LogError("ACHIEVEMENTS: failed to load " + path + "" + e);
            }
        }
    }
}
