using System;
using System.IO;
using UnityEngine;
using Game.GameplaySystems;

namespace Game.Gameplay.Effects
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerFoodEffects : MonoBehaviour
    {
        public FoodEffectState State { get; private set; } = new FoodEffectState();

        private Game.PlayerStats.PlayerStats stats;
        private string path;
        private float saveTimer, particleTimer;
        private bool loaded;
        private string boundSave;
        public bool IsLocalPlayer
        {
            get
            {
                if (stats == null || !stats.isActiveAndEnabled || GetComponentInParent<Game.Entities.EntityActor>() != null)
                    return false;
                var inventory = GetComponent<Game.Inventory.PlayerInventory>();
                if (inventory == null)
                    return false;
                var voxel = Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
                if (voxel != null && voxel.isActiveAndEnabled && voxel.Ready)
                    return voxel.Inventory == inventory;
                var manager = Game.World.WorldManager.Instance;
                if (manager == null || !manager.isActiveAndEnabled || !manager.IsReady)
                    return false;
                var ui = Game.Inventory.UI.InventoryUI.Instance;
                return ui == null || ui.PlayerInventory == null || ui.PlayerInventory == inventory;
            }
        }

        public static float Speed(Component player) => player != null ? (player.GetComponent<PlayerFoodEffects>()?.State.Speed ?? 1f) : 1f;
        public static float Jump(Component player) => player != null ? (player.GetComponent<PlayerFoodEffects>()?.State.Jump ?? 1f) : 1f;
        public static float Strength(Component player) => player != null ? (player.GetComponent<PlayerFoodEffects>()?.State.Strength ?? 1f) : 1f;
        public static float Defense(Component player) => player != null ? (player.GetComponent<PlayerFoodEffects>()?.State.Defense ?? 1f) : 1f;
        private void Awake()
        {
            stats = GetComponent<Game.PlayerStats.PlayerStats>();
            if (GetComponentInParent<Game.Entities.EntityActor>() != null)
            {
                enabled = false;
                return;
            }

            if (stats != null)
                stats.Died += Clear;
            if (GetComponent<FoodEatingVisual>() == null)
                gameObject.AddComponent<FoodEatingVisual>();
            if (GetComponent<FoodEffectsHUD>() == null)
                gameObject.AddComponent<FoodEffectsHUD>();
        }

        private void Start()
        {
            Load();
        }

        private void Load()
        {
            if (!IsLocalPlayer || !Game.Save.SaveGameRuntime.HasActiveSave)
                return;
            string currentSave = Game.Save.SaveGameRuntime.CurrentSaveId;
            if (loaded && currentSave == boundSave)
                return;
            if (loaded)
                Save();
            loaded = true;
            boundSave = currentSave;
            // Effects belong to a player save, so travel between dimensions keeps them.
            string id = Game.Save.SaveGameRuntime.CurrentSaveId;
            foreach (char c in Path.GetInvalidFileNameChars())
                id = id.Replace(c, '_');
            path = Path.Combine(Application.persistentDataPath, "TelderGameplaySystems", "food_effects_" + id + ".json");
            State = GameplaySaveIO.Load<FoodEffectState>(path);
            if (State.Entries == null)
                State.Entries = new System.Collections.Generic.List<ActiveFoodEffect>();
            State.Entries.RemoveAll(e => e == null || !Enum.IsDefined(typeof(FoodEffectType), e.Type) || e.Remaining <= 0 || float.IsNaN(e.Remaining));
            foreach (var e in State.Entries)
            {
                e.Level = Mathf.Clamp(e.Level, 1, 10);
                e.Remaining = Mathf.Min(e.Remaining, 86400);
                e.Total = float.IsNaN(e.Total) ? e.Remaining : Mathf.Clamp(e.Total, e.Remaining, 86400);
            }
        }

        private void Clear()
        {
            State.Entries.Clear();
            Save();
        }

        public void Apply(FoodEffectDefinition[] effects)
        {
            Load();
            if (effects != null)
                foreach (var e in effects)
                    State.Add(e);
            Save();
        }

        private void Update()
        {
            Load();
            if (stats == null || !IsLocalPlayer)
                return;
            if (stats.IsDead)
            {
                if (State.Entries.Count > 0)
                {
                    State.Entries.Clear();
                    Save();
                }

                return;
            }

            float poison = State.Advance(Time.deltaTime);
            if (poison > 0)
                stats.TakeDamage(poison);
            if (State.Entries.Count > 0 && (particleTimer -= Time.deltaTime) <= 0)
            {
                particleTimer = .16f;
                var e = State.Entries[UnityEngine.Random.Range(0, State.Entries.Count)];
                var voxel = Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
                Vector3 p = voxel != null && voxel.Player != null ? voxel.Player.position + Vector3.up * .9f : transform.position;
                GetComponent<FoodEatingVisual>()?.EmitEffect(p, FoodEffectsHUD.EffectColor(e.Type));
            }

            saveTimer += Time.deltaTime;
            if (saveTimer >= 5)
            {
                saveTimer = 0;
                Save();
            }
        }

        public void ForceSave()
        {
            Save();
        }

        private void Save()
        {
            if (loaded && path != null)
                GameplaySaveIO.Save(path, State);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDestroy()
        {
            Save();
            if (stats != null)
                stats.Died -= Clear;
        }
    }
}
