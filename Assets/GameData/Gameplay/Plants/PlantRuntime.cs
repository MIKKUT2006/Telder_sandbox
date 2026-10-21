using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Blocks;
using Game.Content;
using Game.GameplaySystems;
using Game.World;
using Game.World.Furniture;
using Game.World.Items;
using Game.World.Structures;
using Game.World.Voxel3D;
using Game.World.Rendering;
using Game.BlockTransforms;

namespace Game.Gameplay.Plants
{
    [Serializable]
    public sealed class GrowingPlant
    {
        public int X, Y, Z;
        public bool Voxel;
        public string Id;
        public double PlantedAt;
        public string Structure;
        [NonSerialized]
        public int LastStage = -1;
    }

    [Serializable]
    public sealed class GrownPlantStructure
    {
        public int X, Y, Z, Seed;
        public bool Voxel;
        public string Id;
    }

    [Serializable]
    public sealed class PlantSave
    {
        public double Clock;
        public List<GrowingPlant> Plants = new List<GrowingPlant>();
        public List<GrownPlantStructure> Structures = new List<GrownPlantStructure>();
    }

    [DefaultExecutionOrder(-500)]
    public sealed class PlantRuntime : MonoBehaviour
    {
        private static PlantRuntime instance;
        private PlantSave data = new PlantSave();
        private readonly Dictionary<(int, int, int, bool), GrowingPlant> lookup = new Dictionary<(int, int, int, bool), GrowingPlant>();
        private string context, path;
        private bool metadataRestored;
        private bool plantRecordsValidated;
        private float nextCheck, saveTimer;
        private int cursor, lastBindFrame = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Ensure();
        }

        private static PlantRuntime Ensure()
        {
            if (instance == null)
                new GameObject("Plant growth").AddComponent<PlantRuntime>();
            return instance;
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
            FurnitureLayerManager.FurnitureRemoved += Removed;
        }

        private void Bind()
        {
            if (lastBindFrame == Time.frameCount)
                return;
            lastBindFrame = Time.frameCount;
            string key = GameplayContextKey.Get();
            if (key == context)
                return;
            Save();
            context = key;
            path = Game.Save.SaveGameRuntime.HasActiveSave ? Path.Combine(Application.persistentDataPath, "TelderGameplaySystems", "plants_" + key + ".json") : null;
            data = GameplaySaveIO.Load<PlantSave>(path);
            if (data.Plants == null)
                data.Plants = new List<GrowingPlant>();
            if (data.Structures == null)
                data.Structures = new List<GrownPlantStructure>();
            metadataRestored = false;
            plantRecordsValidated = false;
            lookup.Clear();
            cursor = 0;
            for (int i = data.Plants.Count - 1; i >= 0; i--)
            {
                var p = data.Plants[i];
                if (p == null || string.IsNullOrEmpty(p.Id))
                {
                    data.Plants.RemoveAt(i);
                    continue;
                }

                var k = Key(p.X, p.Y, p.Z, p.Voxel);
                if (lookup.ContainsKey(k))
                    data.Plants.RemoveAt(i);
                else
                    lookup[k] = p;
            }
        }

        private static (int, int, int, bool) Key(int x, int y, int z, bool voxel) => (x, y, z, voxel);
        public static GrowingPlant Track(string id, int x, int y, int z = 0, bool voxel = false)
        {
            var runtime = Ensure();
            runtime.Bind();
            var k = Key(x, y, z, voxel);
            if (runtime.lookup.TryGetValue(k, out var p) && p.Id == id)
                return p;
            if (p != null)
                runtime.data.Plants.Remove(p);
            p = new GrowingPlant
            {
                X = x,
                Y = y,
                Z = z,
                Voxel = voxel,
                Id = id,
                PlantedAt = runtime.data.Clock
            };
            runtime.lookup[k] = p;
            runtime.data.Plants.Add(p);
            return p;
        }

        private double Age(GrowingPlant p) => Math.Max(0, data.Clock - p.PlantedAt);
        public static string Texture(BlockDefinition block, int x, int y)
        {
            if (block?.Plant == null)
                return block?.Texture;
            var p = Track(block.ID.ToString(), x, y);
            return StageTexture(block.Plant, Ensure().Age(p), block.Texture);
        }

        public static string Texture(BlockDefinition block, Vector3Int cell)
        {
            if (block?.Plant == null)
                return block?.Texture;
            var p = Track(block.ID.ToString(), cell.x, cell.y, cell.z, true);
            return StageTexture(block.Plant, Ensure().Age(p), block.Texture);
        }

        private static string StageTexture(PlantDefinition plant, double age, string fallback)
        {
            int stage = plant.Stage(age);
            return plant.Stages != null && stage < plant.Stages.Length && !string.IsNullOrWhiteSpace(plant.Stages[stage]) ? plant.Stages[stage] : fallback;
        }

        public static bool ValidSoil(PlantDefinition plant, int x, int y)
        {
            var world = WorldManager.Instance?.GetWorld();
            if (world == null || world.GetChunk(Mathf.FloorToInt(x / (float)Chunk.SizeX), Mathf.FloorToInt((y - 1) / (float)Chunk.SizeY)) == null)
                return false;
            ushort id = world.GetBlock(x, y - 1);
            return id != 0 && plant.Accepts(world.GetBlockDefinition(id)?.ID.ToString());
        }

        public static bool ValidSoil(PlantDefinition plant, Vector3Int cell, VoxelWorld3DRuntime rt)
        {
            var below = cell + Vector3Int.down;
            return rt != null && rt.IsLoaded(below) && plant.Accepts(VoxelBlockCatalog.Get(rt.World.GetBlock(below.x, below.y, below.z))?.ID.ToString());
        }

        public static List<BlockLootResult> Harvest(BlockDefinition block, int x, int y, int z = 0, bool voxel = false)
        {
            if (block?.Plant == null)
                return null;
            var p = Track(block.ID.ToString(), x, y, z, voxel);
            var plant = block.Plant;
            var table = Ensure().Age(p) >= Math.Max(1, plant.GrowthSeconds) ? plant.MatureLoot : plant.ImmatureLoot;
            if (table == null)
                return new List<BlockLootResult>
                {
                    new BlockLootResult(block.ID.ToString(), 1)
                };
            return BlockLootDropper.RollDrops(new BlockDefinition { HasLootTable = true, Loot = table }, block.ID.ToString());
        }

        public static void Forget(int x, int y, int z = 0, bool voxel = false)
        {
            if (instance == null)
                return;
            instance.Bind();
            var k = Key(x, y, z, voxel);
            if (instance.lookup.TryGetValue(k, out var p))
            {
                instance.lookup.Remove(k);
                instance.data.Plants.Remove(p);
            }
        }

        private void Removed(int x, int y, string id)
        {
            Forget(x, y);
        }

        private void Update()
        {
            Bind();
            var manager = WorldManager.Instance;
            var rt = VoxelWorld3DRuntime.Instance;
            bool voxel = rt != null && rt.isActiveAndEnabled && rt.Ready;
            if (!voxel && (manager == null || !manager.isActiveAndEnabled || !manager.IsReady))
                return;
            // Remove records made by the previous loader for ordinary terrain.
            // Wait for a ready world: during bootstrap the registry is incomplete.
            if (!plantRecordsValidated)
            {
                data.Plants.RemoveAll(p => p == null || string.IsNullOrWhiteSpace(p.Id) ||
                    !BlockRegistry.Contains(ContentID.Parse(p.Id)) ||
                    BlockRegistry.Get(ContentID.Parse(p.Id)).Plant == null);
                lookup.Clear();
                foreach (var p in data.Plants)
                    lookup[Key(p.X, p.Y, p.Z, p.Voxel)] = p;
                cursor = 0;
                plantRecordsValidated = true;
            }

            if (!metadataRestored)
            {
                foreach (var grown in data.Structures)
                    RestoreMetadata(grown, rt);
                metadataRestored = true;
            }

            if (!voxel)
                TryPlant(manager);
            data.Clock += Time.deltaTime;
            saveTimer += Time.deltaTime;
            if (saveTimer >= 5)
            {
                Save();
                saveTimer = 0;
            }

            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + .25f;
            int budget = Mathf.Min(32, data.Plants.Count);
            while (budget-- > 0 && data.Plants.Count > 0)
            {
                if (cursor >= data.Plants.Count)
                    cursor = 0;
                var p = data.Plants[cursor++];
                if (p.Voxel != voxel)
                    continue;
                var cell = new Vector3Int(p.X, p.Y, p.Z);
                BlockDefinition block;
                if (voxel)
                {
                    if (!rt.IsLoaded(cell) || !rt.IsLoaded(cell + Vector3Int.down))
                        continue;
                    block = VoxelBlockCatalog.Get(rt.World.GetBlock(p.X, p.Y, p.Z));
                }
                else
                {
                    var world = manager.GetWorld();
                    if (world.GetChunk(Mathf.FloorToInt(p.X / (float)Chunk.SizeX), Mathf.FloorToInt(p.Y / (float)Chunk.SizeY)) == null || world.GetChunk(Mathf.FloorToInt(p.X / (float)Chunk.SizeX), Mathf.FloorToInt((p.Y - 1) / (float)Chunk.SizeY)) == null)
                        continue;
                    var furniture = FurnitureLayerManager.Instance;
                    string id = furniture != null ? furniture.GetFurniture(p.X, p.Y) : null;
                    var cid = ContentID.Parse(id ?? "");
                    block = BlockRegistry.Contains(cid) ? BlockRegistry.Get(cid) : null;
                }

                if (block?.Plant == null || block.ID.ToString() != p.Id)
                {
                    Forget(p.X, p.Y, p.Z, p.Voxel);
                    continue;
                }

                if (!(voxel ? ValidSoil(block.Plant, cell, rt) : ValidSoil(block.Plant, p.X, p.Y)))
                {
                    if (voxel)
                        VoxelBlockInteraction3D.Break(rt, cell, true);
                    else
                        FurnitureLayerManager.Instance.BreakFurniture(p.X, p.Y);
                    continue;
                }

                int stage = block.Plant.Stage(Age(p));
                if (stage != p.LastStage)
                {
                    p.LastStage = stage;
                    if (voxel)
                        rt.Refresh(cell);
                }

                var choices = voxel && block.Plant.Structures3D != null && block.Plant.Structures3D.Length > 0 ? block.Plant.Structures3D : block.Plant.Structures;
                if (!block.Plant.Mature(Age(p)) || choices == null || choices.Length == 0)
                    continue;
                if (string.IsNullOrEmpty(p.Structure))
                    p.Structure = choices[UnityEngine.Random.Range(0, choices.Length)];
                if (GrowStructure(p, rt))
                {
                    Forget(p.X, p.Y, p.Z, p.Voxel);
                    Save();
                    break;
                }
            }
        }

        private void TryPlant(WorldManager manager)
        {
            if (Time.timeScale <= 0 || Game.Inventory.UI.InventoryUI.Instance != null && Game.Inventory.UI.InventoryUI.Instance.IsOpen || Game.UI.Settings.SettingsTabsController.IsOpen || !Game.InputSupport.GameInput.Pressed(Game.InputSupport.GameAction.Use))
                return;
            var inventory = Game.Inventory.UI.InventoryUI.Instance?.PlayerInventory;
            if (inventory == null)
                inventory = FindFirstObjectByType<Game.Inventory.PlayerInventory>();
            if (inventory == null || inventory.GetComponent<Game.PlayerStats.PlayerStats>()?.IsDead == true)
                return;
            string id = inventory.GetSelectedItemId();
            var cid = ContentID.Parse(id ?? "");
            if (!BlockRegistry.Contains(cid))
                return;
            var block = BlockRegistry.Get(cid);
            if (block.Plant == null)
                return;
            var camera = Camera.main;
            if (camera == null || !camera.orthographic)
                return;
            Vector3 point = camera.ScreenToWorldPoint(Game.InputSupport.GameInput.PointerPosition);
            int x = Mathf.FloorToInt(point.x), y = Mathf.FloorToInt(point.y);
            var world = manager.GetWorld();
            ushort soil = world.GetBlock(x, y);
            if (soil != 0 && block.Plant.Accepts(world.GetBlockDefinition(soil)?.ID))
                y++;
            if (((Vector2)inventory.transform.position - new Vector2(x + .5f, y + .5f)).sqrMagnitude > 36 || world.GetBlock(x, y) != 0 || !ValidSoil(block.Plant, x, y))
                return;
            var layer = FurnitureLayerManager.Instance;
            if (layer == null || layer.HasFurniture(x, y))
                return;
            if (!layer.SetFurniture(x, y, id))
                return;
            if (!inventory.TryConsumeSelected(1))
            {
                layer.RemoveFurniture(x, y);
                return;
            }

            Track(id, x, y);
            Game.Achievements.AchievementRuntime.NotifyBlockPlace(id);
        }

        private bool GrowStructure(GrowingPlant plant, VoxelWorld3DRuntime rt)
        {
            var structure = StructureRegistry.Get(plant.Structure);
            if (structure?.Cells == null || structure.Cells.Count == 0 || structure.Cells.Count > 4096)
                return false;
            var manager = WorldManager.Instance;
            var world = manager != null ? manager.GetWorld() : null;
            var furniture = FurnitureLayerManager.Instance;
            Vector3Int anchor = new Vector3Int(plant.X, plant.Y, plant.Z);
            int contentCells = 0;
            // Validate every layer before removing the sapling. No chunk creation.
            foreach (var c in structure.Cells)
            {
                if (c == null)
                    continue;
                var p = anchor + new Vector3Int(c.X - structure.OriginX, c.Y - structure.OriginY, plant.Voxel ? c.Z - structure.OriginZ : 0);
                string id = !string.IsNullOrEmpty(c.ForegroundId) ? c.ForegroundId : (!string.IsNullOrEmpty(c.FurnitureId) ? c.FurnitureId : c.BackgroundId);
                bool hasContent = !string.IsNullOrWhiteSpace(id);
                if (hasContent)
                    contentCells++;
                if (plant.Voxel)
                {
                    if (!rt.IsLoaded(p) || !string.IsNullOrWhiteSpace(id) && VoxelBlockCatalog.Id(id) == 0)
                        return false;
                    if (p != anchor && rt.World.GetBlock(p.x, p.y, p.z) != 0 && !string.IsNullOrWhiteSpace(id))
                        return false;
                }
                else
                {
                    if (world == null || world.GetChunk(Mathf.FloorToInt(p.x / (float)Chunk.SizeX), Mathf.FloorToInt(p.y / (float)Chunk.SizeY)) == null)
                        return false;
                    if (!Known(c.ForegroundId) || !Known(c.BackgroundId) || !Known(c.FurnitureId))
                        return false;
                    if ((!string.IsNullOrWhiteSpace(c.ForegroundId) || !string.IsNullOrWhiteSpace(c.BackgroundId) || !string.IsNullOrWhiteSpace(c.FurnitureId)) && world.GetBlock(p.x, p.y) != 0)
                        return false;
                    if (p != anchor && hasContent && (furniture != null && furniture.HasFurniture(p.x, p.y) || Game.GameplaySystems.Multiblock.MultiBlockStore.FindCell(p.x, p.y) != null))
                        return false;
                    if (!string.IsNullOrWhiteSpace(c.BackgroundId) && world.GetBackground(p.x, p.y) != 0)
                        return false;
                }
            }

            if (contentCells == 0)
                return false;
            // Placement and save hooks already coalesce light, render and collider work.
            if (plant.Voxel)
                rt.SetBlock(anchor, 0);
            else
                furniture.RemoveFurniture(plant.X, plant.Y);
            foreach (var c in structure.Cells)
            {
                if (c == null)
                    continue;
                var p = anchor + new Vector3Int(c.X - structure.OriginX, c.Y - structure.OriginY, plant.Voxel ? c.Z - structure.OriginZ : 0);
                if (plant.Voxel)
                {
                    string id = !string.IsNullOrWhiteSpace(c.ForegroundId) ? c.ForegroundId : (!string.IsNullOrWhiteSpace(c.FurnitureId) ? c.FurnitureId : c.BackgroundId);
                    if (!string.IsNullOrWhiteSpace(id))
                        rt.SetBlock(p, VoxelBlockCatalog.Id(id));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(c.ForegroundId))
                {
                    var def = BlockRegistry.Get(ContentID.Parse(c.ForegroundId));
                    if (def.alwaysFurniture)
                        furniture.SetFurniture(p.x, p.y, c.ForegroundId, RuntimeTransform(c.ForegroundTransform != 0 ? c.ForegroundTransform : c.Transform), true);
                    else
                    {
                        manager.SetBlock(p.x, p.y, BlockIDRegistry.GetID(ContentID.Parse(def.ID)));
                        BlockTransformRegistry.SetRaw(p.x, p.y, RuntimeTransform(c.ForegroundTransform != 0 ? c.ForegroundTransform : c.Transform));
                    }
                }

                if (!string.IsNullOrWhiteSpace(c.BackgroundId))
                {
                    manager.SetBackground(p.x, p.y, BlockIDRegistry.GetID(ContentID.Parse(c.BackgroundId)));
                    BackgroundBlockTransformRegistry.SetRaw(p.x, p.y, RuntimeTransform(c.BackgroundTransform));
                }

                if (!string.IsNullOrWhiteSpace(c.FurnitureId))
                    furniture.SetFurniture(p.x, p.y, c.FurnitureId, RuntimeTransform(c.FurnitureTransform), true);
            }

            var grown = new GrownPlantStructure
            {
                X = plant.X,
                Y = plant.Y,
                Z = plant.Z,
                Voxel = plant.Voxel,
                Id = structure.ID,
                Seed = UnityEngine.Random.Range(1, int.MaxValue)
            };
            data.Structures.Add(grown);
            RestoreMetadata(grown, rt);
            return true;
        }

        private static byte RuntimeTransform(byte editor) => (byte)(((4 - (editor & 3)) & 3) | (editor & 4));
        private static void RestoreMetadata(GrownPlantStructure grown, VoxelWorld3DRuntime rt)
        {
            if (grown == null)
                return;
            var structure = StructureRegistry.Get(grown.Id);
            if (structure?.Cells == null)
                return;
            string dimension = Game.World.Dimensions.DimensionTravelRuntime.Current.Name;
            if (!grown.Voxel)
                StructureInstanceRuntimeIndex.Register(Game.World.Dimensions.DimensionTravelRuntime.Current.Seed, dimension, structure, grown.X, grown.X, grown.Y);
            foreach (var c in structure.Cells)
            {
                if (c == null)
                    continue;
                int x = grown.X + c.X - structure.OriginX, y = grown.Y + c.Y - structure.OriginY, z = grown.Z + c.Z - structure.OriginZ;
                if (!grown.Voxel && !string.IsNullOrWhiteSpace(c.BackgroundId))
                    BackgroundBlockTransformRegistry.SetRaw(x, y, RuntimeTransform(c.BackgroundTransform));
                if (c.Loot == null || c.Loot.Count == 0)
                    continue;
                var loot = new List<Game.Chests.StructureLootEntryRuntime>();
                foreach (var e in c.Loot)
                    if (e != null)
                        loot.Add(new Game.Chests.StructureLootEntryRuntime { ItemId = e.ItemId, Chance = e.Chance, MinCount = e.MinCount, MaxCount = e.MaxCount });
                if (grown.Voxel)
                {
                    if (rt == null || rt.Services == null)
                        continue;
                    string id = !string.IsNullOrWhiteSpace(c.ForegroundId) ? c.ForegroundId : (!string.IsNullOrWhiteSpace(c.FurnitureId) ? c.FurnitureId : c.BackgroundId);
                    int address = rt.Services.Address(new Vector3Int(x, y, z), VoxelBlockCatalog.Id(id));
                    Game.Chests.GeneratedChestLootRuntime.Register(dimension, address, 0, grown.Id, grown.Seed, loot);
                }
                else
                    Game.Chests.GeneratedChestLootRuntime.Register(dimension, x, y, grown.Id, grown.Seed, loot);
            }
        }

        private static bool Known(string id) => string.IsNullOrWhiteSpace(id) || BlockIDRegistry.Contains(ContentID.Parse(id));
        public static void Flush()
        {
            if (instance != null)
                instance.Save();
        }

        public void ForceSave()
        {
            Save();
        }

        private void Save()
        {
            if (path != null)
                GameplaySaveIO.Save(path, data);
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
            FurnitureLayerManager.FurnitureRemoved -= Removed;
            if (instance == this)
                instance = null;
        }
    }
}
