#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Achievements;

namespace Game.EditorTools
{
    public sealed class AchievementEditorWindow : EditorWindow
    {
        [Serializable]
        private class Dto
        {
            public string ID = "game:new_achievement";
            public string Title = "Новое достижение";
            public string Description = "";
            public string Icon = "";
            public string Parent = "";
            public bool HiddenUntilUnlocked;
            public int SortOrder;
            public List<AchievementCondition> Conditions = new List<AchievementCondition>();
        }

        private Dto data = new Dto();
        private Vector2 scroll;
        private string currentPath = "";
        private string[] iconIDs = new string[0];

        private void OnEnable() { RefreshIconIDs(); }

        [Serializable]
        private class IdOnly { public string ID; }

        private void RefreshIconIDs()
        {
            List<string> ids = new List<string>();
            ScanIds(Path.Combine(Application.dataPath, "GameData", "Items"), ids);
            ScanIds(Path.Combine(Application.dataPath, "GameData", "Blocks"), ids);
            ids.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = ids.Count - 1; i > 0; i--)
                if (string.Equals(ids[i], ids[i - 1], StringComparison.OrdinalIgnoreCase)) ids.RemoveAt(i);
            iconIDs = ids.ToArray();
        }

        private static void ScanIds(string folder, List<string> ids)
        {
            if (!Directory.Exists(folder)) return;
            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    IdOnly dto = JsonUtility.FromJson<IdOnly>(File.ReadAllText(files[i]));
                    if (dto != null && !string.IsNullOrWhiteSpace(dto.ID)) ids.Add(dto.ID.Trim());
                }
                catch { }
            }
        }

        [MenuItem("Tools/Game/Achievement Editor")]
        public static void Open() { GetWindow<AchievementEditorWindow>("Achievement Editor"); }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New", GUILayout.Width(80))) { data = new Dto(); currentPath = ""; }
            if (GUILayout.Button("Open", GUILayout.Width(80))) OpenJson();
            if (GUILayout.Button("Save", GUILayout.Width(80))) SaveJson();
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            data.ID = EditorGUILayout.TextField("ID", data.ID);
            data.Title = EditorGUILayout.TextField("Название", data.Title);
            EditorGUILayout.LabelField("Описание");
            data.Description = EditorGUILayout.TextArea(data.Description, GUILayout.MinHeight(60));
            EditorGUILayout.BeginHorizontal();
            data.Icon = EditorGUILayout.TextField("Icon item/block ID", data.Icon);
            if (GUILayout.Button("Выбрать", GUILayout.Width(90))) RefreshIconIDs();
            EditorGUILayout.EndHorizontal();
            if (iconIDs.Length > 0)
            {
                int currentIcon = Array.FindIndex(iconIDs, x => string.Equals(x, data.Icon, StringComparison.OrdinalIgnoreCase));
                int selectedIcon = EditorGUILayout.Popup("Из предметов/блоков", Mathf.Max(0, currentIcon), iconIDs);
                if (selectedIcon >= 0 && selectedIcon < iconIDs.Length && (currentIcon >= 0 || GUILayout.Button("Использовать выбранную иконку")))
                    data.Icon = iconIDs[selectedIcon];
            }
            data.Parent = EditorGUILayout.TextField("Parent achievement ID", data.Parent);
            data.HiddenUntilUnlocked = EditorGUILayout.Toggle("Скрывать до открытия", data.HiddenUntilUnlocked);
            data.SortOrder = EditorGUILayout.IntField("Sort order", data.SortOrder);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Условия", EditorStyles.boldLabel);
            for (int i = 0; i < data.Conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                AchievementCondition c = data.Conditions[i];
                c.Type = (AchievementConditionType)EditorGUILayout.EnumPopup("Тип", c.Type);
                c.Target = EditorGUILayout.TextField("Target ID", c.Target);
                c.Count = Mathf.Max(1, EditorGUILayout.IntField("Количество", c.Count));
                if (GUILayout.Button("Удалить")) { data.Conditions.RemoveAt(i); i--; }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("+ Добавить условие")) data.Conditions.Add(new AchievementCondition());
            EditorGUILayout.EndScrollView();
        }

        private string Folder()
        {
            string p = Path.Combine(Application.dataPath, "GameData", "Achievements");
            Directory.CreateDirectory(p); return p;
        }

        private void OpenJson()
        {
            string p = EditorUtility.OpenFilePanel("Open achievement", Folder(), "json");
            if (string.IsNullOrWhiteSpace(p)) return;
            try { data = JsonUtility.FromJson<Dto>(File.ReadAllText(p)) ?? new Dto(); currentPath = p; }
            catch (Exception e) { Debug.LogError(e); }
        }

        private void SaveJson()
        {
            string p = currentPath;
            if (string.IsNullOrWhiteSpace(p))
            {
                string name = (data.ID ?? "achievement").Replace(":", "_");
                p = EditorUtility.SaveFilePanel("Save achievement", Folder(), name, "json");
            }
            if (string.IsNullOrWhiteSpace(p)) return;
            File.WriteAllText(p, JsonUtility.ToJson(data, true)); currentPath = p; AssetDatabase.Refresh();
        }
    }
}
#endif
