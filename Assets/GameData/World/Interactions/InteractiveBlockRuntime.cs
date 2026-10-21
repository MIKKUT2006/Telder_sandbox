using Game.InputSupport;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Blocks;
using Game.Content;
using Game.GameplaySystems;
using Game.GameplaySystems.Multiblock;
using Game.World.Furniture;
using Game.World.Rendering;
using GameWorld = Game.World.World;

namespace Game.GameplaySystems.Interactions
{
    /// <summary>
    /// Shared gameplay behaviour for blocks that are more than static terrain.
    /// Supported BlockDefinition.Behavior values:
    /// - "Ladder": pass-through traversal surface. Hold Space to climb.
    /// - "Door": RMB toggles open/closed. Open disables collision.
    /// - "Hatch": same state/collision rules as Door.
    ///
    /// Tags "ladder", "door" and "hatch" are accepted as a backwards-compatible
    /// alternative to Behavior.
    ///
    /// Door/hatch open state is persisted per save+dimension and mirrored into
    /// BlockVisualStateRuntime as the visual state "open".
    /// </summary>
    public static class InteractiveBlockRuntime
    {
        [Serializable]
        private sealed class StateEntry
        {
            public int X;
            public int Y;
            public int Layer;
            public string BlockId;
            public string State;
        }

        [Serializable]
        private sealed class StateFile
        {
            public int Version = 1;
            public List<StateEntry> Entries = new List<StateEntry>();

            public StateFile()
            {
            }
        }

        private struct Key : IEquatable<Key>
        {
            public int X;
            public int Y;
            public BlockVisualLayer Layer;

            public bool Equals(Key other)
            {
                return X == other.X && Y == other.Y && Layer == other.Layer;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + X;
                    hash = hash * 31 + Y;
                    hash = hash * 31 + (int)Layer;
                    return hash;
                }
            }
        }

        private static readonly Dictionary<Key, StateEntry> states =
            new Dictionary<Key, StateEntry>();

        private static string contextKey;
        private static bool initialized;

        public static void SetContext(string key)
        {
            key = string.IsNullOrWhiteSpace(key) ? "default" : key.Trim();

            if (initialized && string.Equals(contextKey, key, StringComparison.Ordinal))
            {
                foreach (KeyValuePair<Key, StateEntry> pair in states)
                {
                    StateEntry existing = pair.Value;
                    if (existing == null || string.IsNullOrWhiteSpace(existing.State))
                        continue;

                    BlockVisualStateRuntime.SetState(
                        existing.X,
                        existing.Y,
                        pair.Key.Layer,
                        existing.State
                    );
                }

                return;
            }

            contextKey = key;
            initialized = true;
            states.Clear();

            StateFile file = GameplaySaveIO.Load<StateFile>(GetPath());
            if (file.Entries == null)
                file.Entries = new List<StateEntry>();

            for (int i = 0; i < file.Entries.Count; i++)
            {
                StateEntry entry = file.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.State))
                    continue;

                BlockVisualLayer layer = NormalizeLayer(entry.Layer);
                Key stateKey = new Key
                {
                    X = entry.X,
                    Y = entry.Y,
                    Layer = layer
                };

                states[stateKey] = entry;

                BlockVisualStateRuntime.SetState(
                    entry.X,
                    entry.Y,
                    layer,
                    entry.State
                );
            }
        }

        public static bool IsLadder(BlockDefinition definition)
        {
            return HasBehavior(definition, "ladder") || HasTag(definition, "ladder");
        }

        public static bool IsPlatform(BlockDefinition definition)
        {
            return HasBehavior(definition, "platform") ||
                   HasTag(definition, "platform");
        }

        public static bool IsDoorOrHatch(BlockDefinition definition)
        {
            return
                HasBehavior(definition, "door") ||
                HasBehavior(definition, "hatch") ||
                HasBehavior(definition, "trapdoor") ||
                HasTag(definition, "door") ||
                HasTag(definition, "hatch") ||
                HasTag(definition, "trapdoor");
        }

        /// <summary>
        /// Foreground collision is disabled for ladders at all times and for
        /// doors/hatches while their persistent state is "open".
        /// </summary>
        public static bool DisablesForegroundCollision(
            int x,
            int y,
            BlockDefinition definition)
        {
            if (definition == null)
                return false;

            if (IsLadder(definition))
                return true;

            return
                IsDoorOrHatch(definition) &&
                IsOpen(x, y, BlockVisualLayer.Foreground, definition.ID);
        }

        public static bool TryToggleForeground(
            GameWorld world,
            int x,
            int y,
            Rect playerBounds)
        {
            if (world == null)
                return false;

            ushort blockId = world.GetBlock(x, y);
            if (blockId == 0)
                return false;

            BlockDefinition definition = world.GetBlockDefinition(blockId);
            if (!IsDoorOrHatch(definition))
                return false;

            string contentId = GetDefinitionId(definition);
            bool currentlyOpen = IsOpen(
                x,
                y,
                BlockVisualLayer.Foreground,
                contentId
            );

            // Never close a door on top of the player. The interaction is still
            // consumed, so RMB cannot place another block behind it.
            if (currentlyOpen && playerBounds.width > 0f && playerBounds.height > 0f)
            {
                Rect closedBounds = new Rect(x, y, 1f, 1f);
                if (Intersects(playerBounds, closedBounds))
                    return true;
            }

            SetOpen(
                x,
                y,
                BlockVisualLayer.Foreground,
                contentId,
                !currentlyOpen
            );

            return true;
        }

        public static bool TryToggleFurniture(
            int cellX,
            int cellY,
            Rect playerBounds)
        {
            FurnitureLayerManager furniture = FurnitureLayerManager.EnsureInstance();
            if (furniture == null)
                return false;

            MultiBlockRecord record = MultiBlockStore.FindCell(cellX, cellY);

            int anchorX = cellX;
            int anchorY = cellY;
            string blockId = null;
            Rect closedBounds = new Rect(cellX, cellY, 1f, 1f);

            if (record != null)
            {
                anchorX = record.X;
                anchorY = record.Y;
                blockId = record.BlockId;

                int width = Mathf.Max(1, record.Width);
                int height = Mathf.Max(1, record.Height);
                int minX = record.X - Mathf.Clamp(record.AnchorX, 0, width - 1);
                int minY = record.Y - Mathf.Clamp(record.AnchorY, 0, height - 1);

                closedBounds = new Rect(minX, minY, width, height);
            }
            else
            {
                blockId = furniture.GetFurniture(cellX, cellY);
            }

            if (string.IsNullOrWhiteSpace(blockId))
                return false;

            if (!TryGetDefinition(blockId, out BlockDefinition definition))
                return false;

            if (!IsDoorOrHatch(definition))
                return false;

            bool currentlyOpen = IsOpen(
                anchorX,
                anchorY,
                BlockVisualLayer.Furniture,
                blockId
            );

            if (
                currentlyOpen &&
                playerBounds.width > 0f &&
                playerBounds.height > 0f &&
                Intersects(playerBounds, closedBounds)
            )
            {
                return true;
            }

            SetOpen(
                anchorX,
                anchorY,
                BlockVisualLayer.Furniture,
                blockId,
                !currentlyOpen
            );

            return true;
        }

        public static bool IsTouchingLadder(
            GameWorld world,
            Rect playerBounds)
        {
            if (world == null || playerBounds.width <= 0f || playerBounds.height <= 0f)
                return false;

            const float epsilon = 0.0001f;
            int minX = Mathf.FloorToInt(playerBounds.xMin + epsilon);
            int maxX = Mathf.FloorToInt(playerBounds.xMax - epsilon);
            int minY = Mathf.FloorToInt(playerBounds.yMin + epsilon);
            int maxY = Mathf.FloorToInt(playerBounds.yMax - epsilon);

            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    ushort foregroundId = world.GetBlock(x, y);
                    if (
                        foregroundId != 0 &&
                        IsLadder(world.GetBlockDefinition(foregroundId))
                    )
                    {
                        return true;
                    }

                    ushort backgroundId = world.GetBackground(x, y);
                    if (
                        backgroundId != 0 &&
                        IsLadder(world.GetBlockDefinition(backgroundId))
                    )
                    {
                        return true;
                    }

                    MultiBlockRecord record = MultiBlockStore.FindCell(x, y);
                    if (
                        record != null &&
                        TryGetDefinition(record.BlockId, out BlockDefinition multiDefinition) &&
                        IsLadder(multiDefinition)
                    )
                    {
                        return true;
                    }

                    if (furniture != null)
                    {
                        string furnitureId = furniture.GetFurniture(x, y);
                        if (
                            !string.IsNullOrWhiteSpace(furnitureId) &&
                            TryGetDefinition(furnitureId, out BlockDefinition furnitureDefinition) &&
                            IsLadder(furnitureDefinition)
                        )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Furniture normally has no collision in Telder. Door/hatch furniture is
        /// the exception: while closed it blocks its cell or the entire multi-block
        /// footprint. Open furniture never collides.
        /// </summary>
        public static bool IntersectsClosedFurniture(Rect worldBounds)
        {
            if (worldBounds.width <= 0f || worldBounds.height <= 0f)
                return false;

            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;
            if (furniture == null)
                return false;

            const float epsilon = 0.0001f;
            int minX = Mathf.FloorToInt(worldBounds.xMin + epsilon);
            int maxX = Mathf.FloorToInt(worldBounds.xMax - epsilon);
            int minY = Mathf.FloorToInt(worldBounds.yMin + epsilon);
            int maxY = Mathf.FloorToInt(worldBounds.yMax - epsilon);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    MultiBlockRecord record = MultiBlockStore.FindCell(x, y);
                    if (record != null)
                    {
                        if (
                            !TryGetDefinition(record.BlockId, out BlockDefinition definition) ||
                            definition == null ||
                            !definition.Solid ||
                            !IsDoorOrHatch(definition) ||
                            IsOpen(
                                record.X,
                                record.Y,
                                BlockVisualLayer.Furniture,
                                record.BlockId
                            )
                        )
                        {
                            continue;
                        }

                        int width = Mathf.Max(1, record.Width);
                        int height = Mathf.Max(1, record.Height);
                        int footprintX = record.X - Mathf.Clamp(record.AnchorX, 0, width - 1);
                        int footprintY = record.Y - Mathf.Clamp(record.AnchorY, 0, height - 1);

                        if (Intersects(worldBounds, new Rect(footprintX, footprintY, width, height)))
                            return true;

                        continue;
                    }

                    string blockId = furniture.GetFurniture(x, y);
                    if (
                        string.IsNullOrWhiteSpace(blockId) ||
                        !TryGetDefinition(blockId, out BlockDefinition furnitureDefinition) ||
                        furnitureDefinition == null ||
                        !furnitureDefinition.Solid ||
                        !IsDoorOrHatch(furnitureDefinition) ||
                        IsOpen(x, y, BlockVisualLayer.Furniture, blockId)
                    )
                    {
                        continue;
                    }

                    if (Intersects(worldBounds, new Rect(x, y, 1f, 1f)))
                        return true;
                }
            }

            return false;
        }

        public static bool IsOpen(
            int x,
            int y,
            BlockVisualLayer layer,
            string expectedBlockId = null)
        {
            EnsureInitialized();

            Key key = new Key { X = x, Y = y, Layer = layer };
            if (!states.TryGetValue(key, out StateEntry entry) || entry == null)
            {
                return string.Equals(
                    BlockVisualStateRuntime.GetState(x, y, layer),
                    "open",
                    StringComparison.OrdinalIgnoreCase
                );
            }

            if (
                !string.IsNullOrWhiteSpace(expectedBlockId) &&
                !string.IsNullOrWhiteSpace(entry.BlockId) &&
                !string.Equals(entry.BlockId, expectedBlockId, StringComparison.OrdinalIgnoreCase)
            )
            {
                states.Remove(key);
                BlockVisualStateRuntime.ClearState(x, y, layer, true);
                Save();
                return false;
            }

            return string.Equals(entry.State, "open", StringComparison.OrdinalIgnoreCase);
        }

        public static void ClearStateAt(
            int x,
            int y,
            BlockVisualLayer layer,
            bool save = true)
        {
            EnsureInitialized();

            Key key = new Key { X = x, Y = y, Layer = layer };
            bool removed = states.Remove(key);

            BlockVisualStateRuntime.ClearState(x, y, layer, true);

            if (removed && save)
                Save();
        }

        public static Rect GetPlayerBounds(Transform player, Vector2 colliderSize)
        {
            if (player == null)
                return new Rect();

            float width = Mathf.Max(0.01f, colliderSize.x);
            float height = Mathf.Max(0.01f, colliderSize.y);

            return new Rect(
                player.position.x - width * 0.5f,
                player.position.y - height * 0.5f,
                width,
                height
            );
        }

        private static void SetOpen(
            int x,
            int y,
            BlockVisualLayer layer,
            string blockId,
            bool open)
        {
            EnsureInitialized();

            Key key = new Key { X = x, Y = y, Layer = layer };

            if (open)
            {
                StateEntry entry = new StateEntry
                {
                    X = x,
                    Y = y,
                    Layer = (int)layer,
                    BlockId = blockId,
                    State = "open"
                };

                states[key] = entry;
                BlockVisualStateRuntime.SetState(x, y, layer, "open");
            }
            else
            {
                states.Remove(key);
                BlockVisualStateRuntime.ClearState(x, y, layer, true);
            }

            Save();
        }

        private static bool HasBehavior(BlockDefinition definition, string expected)
        {
            return
                definition != null &&
                !string.IsNullOrWhiteSpace(definition.Behavior) &&
                string.Equals(definition.Behavior.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasTag(BlockDefinition definition, string expected)
        {
            if (definition == null || definition.Tags == null)
                return false;

            for (int i = 0; i < definition.Tags.Count; i++)
            {
                if (string.Equals(definition.Tags[i], expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool TryGetDefinition(string blockId, out BlockDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(blockId))
                return false;

            try
            {
                ContentID contentId = ContentID.Parse(blockId);
                if (!BlockRegistry.Contains(contentId))
                    return false;

                definition = BlockRegistry.Get(contentId);
                return definition != null;
            }
            catch
            {
                return false;
            }
        }

        private static string GetDefinitionId(BlockDefinition definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.ID)
                ? definition.ID
                : string.Empty;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            SetContext(GameplayContextKey.Get());
        }

        private static string GetPath()
        {
            string folder = Path.Combine(
                Application.persistentDataPath,
                "Telder",
                "InteractiveBlocks"
            );

            return Path.Combine(
                folder,
                "interactive_" + (contextKey ?? "default") + ".json"
            );
        }

        private static void Save()
        {
            if (!initialized)
                return;

            StateFile file = new StateFile();
            foreach (KeyValuePair<Key, StateEntry> pair in states)
            {
                if (pair.Value != null)
                    file.Entries.Add(pair.Value);
            }

            GameplaySaveIO.Save(GetPath(), file, false);
        }

        private static BlockVisualLayer NormalizeLayer(int value)
        {
            if (value == (int)BlockVisualLayer.Background)
                return BlockVisualLayer.Background;
            if (value == (int)BlockVisualLayer.Furniture)
                return BlockVisualLayer.Furniture;
            return BlockVisualLayer.Foreground;
        }

        private static bool Intersects(Rect a, Rect b)
        {
            return
                a.xMax > b.xMin &&
                a.xMin < b.xMax &&
                a.yMax > b.yMin &&
                a.yMin < b.yMax;
        }
    }
}
