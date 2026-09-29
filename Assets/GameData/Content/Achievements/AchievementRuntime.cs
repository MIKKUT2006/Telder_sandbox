using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Game.Save;

namespace Game.Achievements
{
    public sealed class AchievementRuntime : MonoBehaviour
    {
        [Serializable] private class ProgressEntry { public string ID; public bool Unlocked; public int[] Values; }
        [Serializable] private class ProgressFile { public List<ProgressEntry> Entries = new List<ProgressEntry>(); }

        private static AchievementRuntime instance;
        private readonly Dictionary<string, ProgressEntry> progress = new Dictionary<string, ProgressEntry>(StringComparer.OrdinalIgnoreCase);
        private string loadedSaveId;
        private float exploreTimer;
        private string lastBiome = "";
        private string lastDimension = "";

        public static event Action<AchievementDefinition> Unlocked;
        public static AchievementRuntime Instance { get { EnsureExists(); return instance; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { EnsureExists(); }

        private static void EnsureExists()
        {
            if (instance != null) return;
            GameObject go = new GameObject("AchievementRuntime");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AchievementRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject); ReloadIfNeeded();
        }

        private void Update()
        {
            ReloadIfNeeded();
            exploreTimer += Time.unscaledDeltaTime;
            if (exploreTimer < 1f) return;
            exploreTimer = 0f;

            Game.World.Dimensions.DimensionDefinition d = Game.World.Dimensions.DimensionTravelRuntime.Current;
            if (d != null && !string.Equals(lastDimension, d.Name, StringComparison.OrdinalIgnoreCase))
            {
                lastDimension = d.Name;
                NotifyEnterDimension(d.Name);
            }

            Game.World.WorldManager wm = Game.World.WorldManager.Instance;
            if (wm == null || !wm.IsReady || wm.GetGenerator() == null) return;
            Game.Inventory.PlayerInventory inv = FindFirstObjectByType<Game.Inventory.PlayerInventory>();
            if (inv == null) return;
            int x = Mathf.FloorToInt(inv.transform.position.x);
            Game.World.Biomes.BiomeDefinition biome = wm.GetGenerator().GetDominantBiome(x);
            if (biome != null && !string.Equals(lastBiome, biome.ID, StringComparison.OrdinalIgnoreCase))
            {
                lastBiome = biome.ID;
                NotifyExploreBiome(biome.ID);
            }
        }

        private void ReloadIfNeeded()
        {
            string id = SaveGameRuntime.HasActiveSave ? SaveGameRuntime.CurrentSaveId : "__session";
            if (string.Equals(id, loadedSaveId, StringComparison.Ordinal)) return;
            loadedSaveId = id; progress.Clear();
            if (!SaveGameRuntime.HasActiveSave) return;
            string path = GetPath();
            if (!File.Exists(path)) return;
            try
            {
                ProgressFile file = JsonUtility.FromJson<ProgressFile>(File.ReadAllText(path));
                if (file != null && file.Entries != null)
                    for (int i = 0; i < file.Entries.Count; i++)
                        if (file.Entries[i] != null && !string.IsNullOrWhiteSpace(file.Entries[i].ID))
                            progress[file.Entries[i].ID] = file.Entries[i];
            }
            catch (Exception e) { Debug.LogWarning("ACHIEVEMENTS: progress load failed: " + e.Message); }
        }

        private string GetPath() => Path.Combine(SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId), "achievements.json");

        public bool IsUnlocked(string id)
        {
            ReloadIfNeeded();
            return progress.TryGetValue(id, out ProgressEntry e) && e.Unlocked;
        }

        public int GetProgress(string id, int conditionIndex)
        {
            ReloadIfNeeded();
            if (!progress.TryGetValue(id, out ProgressEntry e) || e.Values == null || conditionIndex < 0 || conditionIndex >= e.Values.Length) return 0;
            return e.Values[conditionIndex];
        }

        public static void NotifyBlockBreak(string id, int count = 1) => Instance.Notify(AchievementConditionType.BlockBreak, id, count);
        public static void NotifyBlockPlace(string id, int count = 1) => Instance.Notify(AchievementConditionType.BlockPlace, id, count);
        public static void NotifyItemCollect(string id, int count = 1) => Instance.Notify(AchievementConditionType.ItemCollect, id, count);
        public static void NotifyMobKill(string id, int count = 1) => Instance.Notify(AchievementConditionType.MobKill, id, count);
        public static void NotifyCraft(string id, int count = 1) => Instance.Notify(AchievementConditionType.Craft, id, count);
        public static void NotifyExploreBiome(string id) => Instance.Notify(AchievementConditionType.ExploreBiome, id, 1);
        public static void NotifyDiscoverStructure(string id) => Instance.Notify(AchievementConditionType.DiscoverStructure, id, 1);
        public static void NotifyEnterDimension(string id) => Instance.Notify(AchievementConditionType.EnterDimension, id, 1);

        public void Notify(AchievementConditionType type, string target, int amount)
        {
            if (amount <= 0) return;
            ReloadIfNeeded();
            foreach (AchievementDefinition def in AchievementRegistry.GetAll())
            {
                if (def == null) continue;
                string id = def.ID.ToString();
                if (IsUnlocked(id)) continue;
                List<AchievementCondition> conditions = GetConditions(def);
                if (conditions.Count == 0) continue;
                ProgressEntry entry = GetOrCreate(id, conditions.Count);
                bool touched = false;
                for (int i = 0; i < conditions.Count; i++)
                {
                    AchievementCondition c = conditions[i];
                    if (c == null || c.Type != type) continue;
                    if (!TargetMatches(c.Target, target)) continue;
                    entry.Values[i] += amount; touched = true;
                }
                if (!touched) continue;
                if (ConditionsComplete(conditions, entry) && ParentUnlocked(def))
                {
                    entry.Unlocked = true;
                    Save();
                    Unlocked?.Invoke(def);
                }
                else Save();
            }
        }

        private bool ParentUnlocked(AchievementDefinition def)
        {
            return string.IsNullOrWhiteSpace(def.Parent) || IsUnlocked(def.Parent);
        }

        private static bool TargetMatches(string required, string actual)
        {
            if (string.IsNullOrWhiteSpace(required) || required == "*") return true;
            return string.Equals(required.Trim(), actual ?? "", StringComparison.OrdinalIgnoreCase);
        }

        public static List<AchievementCondition> GetConditions(AchievementDefinition def)
        {
            if (def.Conditions != null && def.Conditions.Count > 0) return def.Conditions;
            List<AchievementCondition> result = new List<AchievementCondition>();
            if (def.ConditionType != AchievementConditionType.Custom)
                result.Add(new AchievementCondition { Type = def.ConditionType, Target = def.Target.ToString(), Count = Math.Max(1, def.Count) });
            return result;
        }

        private ProgressEntry GetOrCreate(string id, int count)
        {
            if (!progress.TryGetValue(id, out ProgressEntry e))
            {
                e = new ProgressEntry { ID = id, Values = new int[count] };
                progress[id] = e;
            }
            if (e.Values == null || e.Values.Length != count) Array.Resize(ref e.Values, count);
            return e;
        }

        private static bool ConditionsComplete(List<AchievementCondition> conditions, ProgressEntry e)
        {
            for (int i = 0; i < conditions.Count; i++)
                if (e.Values == null || i >= e.Values.Length || e.Values[i] < Mathf.Max(1, conditions[i].Count)) return false;
            return true;
        }

        public void Save()
        {
            if (!SaveGameRuntime.HasActiveSave) return;
            try
            {
                string folder = SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId); Directory.CreateDirectory(folder);
                ProgressFile f = new ProgressFile { Entries = progress.Values.OrderBy(x => x.ID, StringComparer.OrdinalIgnoreCase).ToList() };
                File.WriteAllText(GetPath(), JsonUtility.ToJson(f, true));
            }
            catch (Exception e) { Debug.LogWarning("ACHIEVEMENTS: progress save failed: " + e.Message); }
        }
    }
}
