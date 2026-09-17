
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory.UI;
using Game.Save;
using Game.World.Dimensions;
using Game.World.Items;


namespace Game.World.Furniture
{

    [Serializable]
    public class FurnitureSaveEntry
    {

        public int X;

        public int Y;

        public string BlockId;

        public byte Transform;

    }


    [Serializable]
    public class GeneratedFurnitureRemovalEntry
    {

        public int X;

        public int Y;

        public string BlockId;

    }


    [Serializable]
    public class FurnitureSaveFile
    {

        public List<FurnitureSaveEntry> Entries =
            new List<FurnitureSaveEntry>();


        // Only tombstones for procedurally generated furniture are saved.
        // The generated grass/flowers themselves are regenerated from the seed
        // every time their chunk is loaded.
        public List<GeneratedFurnitureRemovalEntry> RemovedGenerated =
            new List<GeneratedFurnitureRemovalEntry>();

    }


    public class FurnitureLayerManager :
        MonoBehaviour
    {

        public static FurnitureLayerManager Instance
        {
            get;
            private set;
        }


        public static event Action<int, int, string>
            FurnitureRemoved;


        public static event Action<int, int, string>
            FurniturePlaced;


        private readonly Dictionary<
            long,
            FurnitureSaveEntry
        > data =
            new Dictionary<
                long,
                FurnitureSaveEntry
            >();


        private readonly Dictionary<
            long,
            GameObject
        > visuals =
            new Dictionary<
                long,
                GameObject
            >();


        // Active procedural furniture exists only while its chunk is loaded.
        // It is deliberately NOT written to the furniture save file.
        private readonly HashSet<long> generatedKeys =
            new HashSet<long>();


        // A removed generated plant must not respawn when the chunk reloads.
        // Only these small tombstones are persisted.
        private readonly Dictionary<long, string> removedGenerated =
            new Dictionary<long, string>();


        // RGB block light has a maximum level of 15.
        // +2 matches the lighting propagation safety radius.
        private const int LightVisualRefreshRadius =
            17;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {

            EnsureInstance();

        }


        public static FurnitureLayerManager EnsureInstance()
        {

            if (
                Instance !=
                null
            )
            {

                return Instance;

            }


            if (
                WorldManager.Instance ==
                null
            )
            {

                return null;

            }


            GameObject gameObject =
                new GameObject(
                    "FurnitureLayer"
                );


            gameObject.transform.SetParent(
                WorldManager.Instance.transform,
                false
            );


            return
                gameObject.AddComponent<
                    FurnitureLayerManager
                >();

        }


        private void Awake()
        {

            if (
                Instance != null
                &&
                Instance != this
            )
            {

                Destroy(
                    gameObject
                );


                return;

            }


            Instance =
                this;


            Load();

        }


        private void OnDestroy()
        {

            if (
                Instance ==
                this
            )
            {

                Instance =
                    null;

            }

        }


        // =====================================================
        // ACCESS
        // =====================================================

        public string GetFurniture(
            int x,
            int y
        )
        {

            return
                data.TryGetValue(
                    Pack(
                        x,
                        y
                    ),
                    out FurnitureSaveEntry entry
                )
                    ? entry.BlockId
                    : null;

        }

        public byte GetFurnitureTransform(
            int x,
            int y
        )
        {
            return
                data.TryGetValue(
                    Pack(
                        x,
                        y
                    ),
                    out FurnitureSaveEntry entry
                )
                    ? entry.Transform
                    : (byte)0;
        }



        public bool HasFurniture(
            int x,
            int y
        )
        {

            return
                !string.IsNullOrWhiteSpace(
                    GetFurniture(
                        x,
                        y
                    )
                );

        }


        public bool TryGetDefinition(
            int x,
            int y,
            out BlockDefinition definition
        )
        {

            definition =
                null;


            string id =
                GetFurniture(
                    x,
                    y
                );


            if (
                string.IsNullOrWhiteSpace(
                    id
                )
            )
            {

                return false;

            }


            try
            {

                ContentID contentID =
                    ContentID.Parse(
                        id
                    );


                if (
                    !BlockRegistry.Contains(
                        contentID
                    )
                )
                {

                    return false;

                }


                definition =
                    BlockRegistry.Get(
                        contentID
                    );


                return
                    definition !=
                    null;

            }
            catch
            {

                return false;

            }

        }


        // =====================================================
        // PLACE
        // =====================================================

        public bool SetFurniture(
            int x,
            int y,
            string blockId
        )
        {
            return
                SetFurniture(
                    x,
                    y,
                    blockId,
                    0
                );
        }


        public bool SetFurniture(
            int x,
            int y,
            string blockId,
            byte transform
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
                ||
                HasFurniture(
                    x,
                    y
                )
            )
            {

                return false;

            }


            FurnitureSaveEntry entry =
                new FurnitureSaveEntry
                {
                    X =
                        x,

                    Y =
                        y,

                    BlockId =
                        blockId,

                    Transform =
                        transform
                };


            data[
                Pack(
                    x,
                    y
                )
            ] =
                entry;


            CreateVisual(
                entry
            );


            Save();


            FurniturePlaced?.Invoke(
                x,
                y,
                blockId
            );


            RebuildLightingAndVisuals(
                x,
                y
            );


            return true;

        }


        // =====================================================
        // PROCEDURAL FURNITURE
        // =====================================================

        public bool SetGeneratedFurniture(
            int x,
            int y,
            string blockId
        )
        {
            return
                SetGeneratedFurniture(
                    x,
                    y,
                    blockId,
                    0
                );
        }


        public bool SetGeneratedFurniture(
            int x,
            int y,
            string blockId,
            byte transform
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
                ||
                HasFurniture(
                    x,
                    y
                )
                ||
                WasGeneratedFurnitureRemoved(
                    x,
                    y,
                    blockId
                )
            )
            {

                return false;

            }


            long key =
                Pack(
                    x,
                    y
                );


            FurnitureSaveEntry entry =
                new FurnitureSaveEntry
                {
                    X =
                        x,

                    Y =
                        y,

                    BlockId =
                        blockId,

                    Transform =
                        transform
                };


            data[
                key
            ] =
                entry;


            generatedKeys.Add(
                key
            );


            CreateVisual(
                entry
            );


            return true;

        }


        public bool WasGeneratedFurnitureRemoved(
            int x,
            int y,
            string blockId
        )
        {

            long key =
                Pack(
                    x,
                    y
                );


            if (
                !removedGenerated.TryGetValue(
                    key,
                    out string removedBlockId
                )
            )
            {

                return false;

            }


            return
                string.Equals(
                    removedBlockId,
                    blockId,
                    StringComparison.OrdinalIgnoreCase
                );

        }


        public void UnloadGeneratedFurnitureChunk(
            int chunkX,
            int chunkY
        )
        {

            if (
                generatedKeys.Count ==
                0
            )
            {

                return;

            }


            List<long> remove =
                new List<long>();


            foreach (
                long key
                in generatedKeys
            )
            {

                int x =
                    UnpackX(
                        key
                    );


                int y =
                    UnpackY(
                        key
                    );


                int entryChunkX =
                    Mathf.FloorToInt(
                        x /
                        (float)Chunk.SizeX
                    );


                int entryChunkY =
                    Mathf.FloorToInt(
                        y /
                        (float)Chunk.SizeY
                    );


                if (
                    entryChunkX ==
                    chunkX
                    &&
                    entryChunkY ==
                    chunkY
                )
                {

                    remove.Add(
                        key
                    );

                }

            }


            for (
                int i = 0;
                i < remove.Count;
                i++
            )
            {

                long key =
                    remove[i];


                generatedKeys.Remove(
                    key
                );


                data.Remove(
                    key
                );


                if (
                    visuals.TryGetValue(
                        key,
                        out GameObject visual
                    )
                    &&
                    visual !=
                    null
                )
                {

                    Destroy(
                        visual
                    );

                }


                visuals.Remove(
                    key
                );

            }

        }


        // =====================================================
        // REMOVE
        // =====================================================

        public bool RemoveFurniture(
            int x,
            int y
        )
        {

            long key =
                Pack(
                    x,
                    y
                );


            if (
                !data.TryGetValue(
                    key,
                    out FurnitureSaveEntry entry
                )
            )
            {

                return false;

            }


            bool wasGenerated =
                generatedKeys.Remove(
                    key
                );


            if (
                wasGenerated
            )
            {

                removedGenerated[
                    key
                ] =
                    entry.BlockId;

            }


            data.Remove(
                key
            );


            if (
                visuals.TryGetValue(
                    key,
                    out GameObject visual
                )
                &&
                visual !=
                null
            )
            {

                Destroy(
                    visual
                );

            }


            visuals.Remove(
                key
            );


            Save();


            // ChestManager listens to this event and drops chest
            // CONTENTS before/alongside the furniture block item.
            FurnitureRemoved?.Invoke(
                x,
                y,
                entry.BlockId
            );


            RebuildLightingAndVisuals(
                x,
                y
            );


            return true;

        }


        // =====================================================
        // REMOVE + DROP BLOCK ITEM
        // =====================================================

        public bool BreakFurniture(
            int x,
            int y
        )
        {

            string blockId =
                GetFurniture(
                    x,
                    y
                );


            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {

                return false;

            }


            string dropId =
                blockId;


            int dropCount =
                1;


            if (
                TryGetDefinition(
                    x,
                    y,
                    out BlockDefinition definition
                )
                &&
                definition !=
                null
            )
            {

                dropCount =
                    Mathf.Max(
                        0,
                        definition.DropCount
                    );


                try
                {

                    string configuredDrop =
                        definition.Drop
                            .ToString();


                    if (
                        !string.IsNullOrWhiteSpace(
                            configuredDrop
                        )
                    )
                    {

                        dropId =
                            configuredDrop;

                    }

                }
                catch
                {
                }

            }


            if (
                !RemoveFurniture(
                    x,
                    y
                )
            )
            {

                return false;

            }


            if (
                dropCount <=
                0
            )
            {

                return true;

            }


            if (
                ItemDropSpawner.Instance ==
                null
            )
            {

                Debug.LogError(
                    "FURNITURE: ItemDropSpawner.Instance is null. " +
                    "Furniture was removed but its block item could not be spawned."
                );


                return true;

            }


            Vector2 dropPosition =
                new Vector2(
                    x +
                    0.5f,

                    y +
                    0.65f
                );


            bool spawned =
                ItemDropSpawner.Instance
                    .SpawnFromBlock(
                        dropId,
                        dropCount,
                        dropPosition
                    );


            if (
                !spawned
            )
            {

                Debug.LogWarning(
                    "FURNITURE: Failed to spawn drop " +
                    dropId +
                    " x" +
                    dropCount
                );

            }


            return true;

        }


        // =====================================================
        // VISUAL
        // =====================================================

        private void CreateVisual(
            FurnitureSaveEntry entry
        )
        {

            long key =
                Pack(
                    entry.X,
                    entry.Y
                );


            if (
                visuals.TryGetValue(
                    key,
                    out GameObject old
                )
                &&
                old !=
                null
            )
            {

                Destroy(
                    old
                );

            }


            GameObject gameObject =
                new GameObject(
                    "Furniture_" +
                    entry.BlockId +
                    "_" +
                    entry.X +
                    "_" +
                    entry.Y
                );


            gameObject.transform.SetParent(
                transform,
                false
            );


            gameObject.transform.position =
                new Vector3(
                    entry.X +
                    0.5f,

                    entry.Y +
                    0.5f,

                    0f
                );


            SpriteRenderer spriteRenderer =
                gameObject.AddComponent<
                    SpriteRenderer
                >();


            // Background = 0
            // Furniture = 1
            // Foreground = 2
            spriteRenderer.sortingOrder =
                1;


            spriteRenderer.sprite =
                ItemIconProvider.GetIcon(
                    entry.BlockId
                );


            spriteRenderer.color =
                Color.white;


            if (
                TryGetDefinition(
                    entry.X,
                    entry.Y,
                    out BlockDefinition definition
                )
            )
            {

                gameObject.transform.position +=
                    new Vector3(
                        definition.VisualOffsetX,
                        definition.VisualOffsetY,
                        0f
                    );


                gameObject.transform.localScale =
                    Vector3.one *
                    Mathf.Max(
                        0.01f,
                        definition.VisualScale
                    );


                if (
                    HasTag(
                        definition,
                        "torch"
                    )
                    ||
                    HasTag(
                        definition,
                        "fire"
                    )
                )
                {

                    CreateFireParticles(
                        gameObject.transform
                    );

                }

            }


            ApplyFurnitureTransform(
                gameObject.transform,
                entry.Transform
            );


            visuals[
                key
            ] =
                gameObject;

        }


        private static void ApplyFurnitureTransform(
            Transform target,
            byte transform
        )
        {
            if (
                target ==
                null
            )
            {
                return;
            }


            int rotation =
                transform &
                0x03;


            bool mirror =
                (
                    transform &
                    0x04
                )
                !=
                0;


            target.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -rotation *
                    90f
                );


            Vector3 scale =
                target.localScale;


            scale.x =
                Mathf.Abs(
                    scale.x
                )
                *
                (
                    mirror
                        ? -1f
                        : 1f
                );


            target.localScale =
                scale;
        }


        private void CreateFireParticles(
            Transform parent
        )
        {

            GameObject gameObject =
                new GameObject(
                    "FireParticles"
                );


            gameObject.transform.SetParent(
                parent,
                false
            );


            gameObject.transform.localPosition =
                new Vector3(
                    0f,
                    0.28f,
                    -0.01f
                );


            ParticleSystem particleSystem =
                gameObject.AddComponent<
                    ParticleSystem
                >();


            ParticleSystem.MainModule main =
                particleSystem.main;


            main.loop =
                true;


            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.28f,
                    0.55f
                );


            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    0.15f,
                    0.45f
                );


            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.035f,
                    0.075f
                );


            main.maxParticles =
                24;


            main.simulationSpace =
                ParticleSystemSimulationSpace.World;


            ParticleSystem.EmissionModule emission =
                particleSystem.emission;


            emission.rateOverTime =
                10f;


            ParticleSystem.ShapeModule shape =
                particleSystem.shape;


            shape.shapeType =
                ParticleSystemShapeType.Circle;


            shape.radius =
                0.06f;


            ParticleSystem.ColorOverLifetimeModule color =
                particleSystem.colorOverLifetime;


            color.enabled =
                true;


            Gradient gradient =
                new Gradient();


            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(
                            1f,
                            0.75f,
                            0.18f
                        ),
                        0f
                    ),

                    new GradientColorKey(
                        new Color(
                            1f,
                            0.18f,
                            0.04f
                        ),
                        1f
                    )
                },

                new[]
                {
                    new GradientAlphaKey(
                        0.95f,
                        0f
                    ),

                    new GradientAlphaKey(
                        0f,
                        1f
                    )
                }
            );


            color.color =
                new ParticleSystem.MinMaxGradient(
                    gradient
                );


            ParticleSystemRenderer renderer =
                gameObject.GetComponent<
                    ParticleSystemRenderer
                >();


            renderer.sortingOrder =
                3;

        }


        private bool HasTag(
            BlockDefinition definition,
            string tag
        )
        {

            if (
                definition ==
                null
                ||
                definition.Tags ==
                null
            )
            {

                return false;

            }


            for (
                int i = 0;
                i < definition.Tags.Count;
                i++
            )
            {

                if (
                    string.Equals(
                        definition.Tags[i],
                        tag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {

                    return true;

                }

            }


            return false;

        }


        // =====================================================
        // LIGHTING
        // =====================================================

        private void RebuildLightingAndVisuals(
            int x,
            int y
        )
        {

            WorldManager worldManager =
                WorldManager.Instance;


            if (
                worldManager ==
                null
                ||
                worldManager.GetWorld() ==
                null
                ||
                worldManager.GetSettings() ==
                null
            )
            {

                return;

            }


            // First update the actual light data.
            worldManager
                .GetWorld()
                .GetLightEngine()
                .RebuildAfterBlockChanged(
                    x,
                    y,
                    worldManager
                        .GetSettings()
                        .WorldHeight
                );


            // IMPORTANT:
            //
            // Furniture is rendered separately, therefore placing it
            // does NOT call WorldManager.SetBlock(), which normally
            // redraws a chunk.
            //
            // The light data could already be correct while the
            // chunk's light texture still showed the OLD values.
            // That is why a deep torch could appear to "turn on"
            // only after the player moved and caused chunks to
            // render again.
            //
            // Redraw only chunks touched by the block-light radius.
            RefreshLightTexturesAround(
                worldManager,
                x,
                y
            );

        }


        private void RefreshLightTexturesAround(
            WorldManager worldManager,
            int worldX,
            int worldY
        )
        {

            if (
                worldManager ==
                null
                ||
                worldManager.GetWorld() ==
                null
                ||
                worldManager.GetChunkRenderer() ==
                null
            )
            {

                return;

            }


            World world =
                worldManager.GetWorld();


            var renderer =
                worldManager.GetChunkRenderer();


            int minChunkX =
                Mathf.FloorToInt(
                    (
                        worldX -
                        LightVisualRefreshRadius
                    )
                    /
                    (float)Chunk.SizeX
                );


            int maxChunkX =
                Mathf.FloorToInt(
                    (
                        worldX +
                        LightVisualRefreshRadius
                    )
                    /
                    (float)Chunk.SizeX
                );


            int minChunkY =
                Mathf.FloorToInt(
                    (
                        worldY -
                        LightVisualRefreshRadius
                    )
                    /
                    (float)Chunk.SizeY
                );


            int maxChunkY =
                Mathf.FloorToInt(
                    (
                        worldY +
                        LightVisualRefreshRadius
                    )
                    /
                    (float)Chunk.SizeY
                );


            for (
                int chunkX = minChunkX;
                chunkX <= maxChunkX;
                chunkX++
            )
            {

                for (
                    int chunkY = minChunkY;
                    chunkY <= maxChunkY;
                    chunkY++
                )
                {

                    Chunk chunk =
                        world.GetChunk(
                            chunkX,
                            chunkY
                        );


                    if (
                        chunk ==
                        null
                    )
                    {

                        continue;

                    }


                    renderer.Render(
                        chunk
                    );

                }

            }

        }


        // =====================================================
        // SAVE
        // =====================================================

        private string GetFile()
        {

            if (
                !SaveGameRuntime.HasActiveSave
                ||
                DimensionTravelRuntime.Current ==
                null
            )
            {

                return null;

            }


            string dimensionFile =
                SavePaths.GetDimensionFile(
                    SaveGameRuntime.CurrentSaveId,
                    DimensionTravelRuntime.Current.Name,
                    DimensionTravelRuntime.Current.Seed
                );


            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    dimensionFile
                )
            );


            return
                dimensionFile +
                ".furniture.json";

        }


        private void Load()
        {

            data.Clear();

            generatedKeys.Clear();

            removedGenerated.Clear();


            foreach (
                KeyValuePair<long, GameObject> pair
                in visuals
            )
            {

                if (
                    pair.Value !=
                    null
                )
                {

                    Destroy(
                        pair.Value
                    );

                }

            }


            visuals.Clear();


            string file =
                GetFile();


            if (
                string.IsNullOrWhiteSpace(
                    file
                )
                ||
                !File.Exists(
                    file
                )
            )
            {

                return;

            }


            try
            {

                FurnitureSaveFile save =
                    JsonUtility.FromJson<
                        FurnitureSaveFile
                    >(
                        File.ReadAllText(
                            file
                        )
                    );


                if (
                    save ==
                    null
                    ||
                    save.Entries ==
                    null
                )
                {

                    return;

                }


                if (
                    save.RemovedGenerated !=
                    null
                )
                {

                    for (
                        int i = 0;
                        i < save.RemovedGenerated.Count;
                        i++
                    )
                    {

                        GeneratedFurnitureRemovalEntry removed =
                            save.RemovedGenerated[i];


                        if (
                            removed ==
                            null
                            ||
                            string.IsNullOrWhiteSpace(
                                removed.BlockId
                            )
                        )
                        {

                            continue;

                        }


                        removedGenerated[
                            Pack(
                                removed.X,
                                removed.Y
                            )
                        ] =
                            removed.BlockId;

                    }

                }


                for (
                    int i = 0;
                    i < save.Entries.Count;
                    i++
                )
                {

                    FurnitureSaveEntry entry =
                        save.Entries[i];


                    if (
                        entry ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            entry.BlockId
                        )
                    )
                    {

                        continue;

                    }


                    data[
                        Pack(
                            entry.X,
                            entry.Y
                        )
                    ] =
                        entry;


                    CreateVisual(
                        entry
                    );

                }

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FURNITURE LOAD: " +
                    exception
                );

            }

        }


        private void Save()
        {

            string file =
                GetFile();


            if (
                string.IsNullOrWhiteSpace(
                    file
                )
            )
            {

                return;

            }


            try
            {

                FurnitureSaveFile save =
                    new FurnitureSaveFile();


                foreach (
                    KeyValuePair<long, FurnitureSaveEntry> pair
                    in data
                )
                {

                    if (
                        generatedKeys.Contains(
                            pair.Key
                        )
                    )
                    {

                        continue;

                    }


                    save.Entries.Add(
                        pair.Value
                    );

                }


                foreach (
                    KeyValuePair<long, string> pair
                    in removedGenerated
                )
                {

                    save.RemovedGenerated.Add(
                        new GeneratedFurnitureRemovalEntry
                        {
                            X =
                                UnpackX(
                                    pair.Key
                                ),

                            Y =
                                UnpackY(
                                    pair.Key
                                ),

                            BlockId =
                                pair.Value
                        }
                    );

                }


                File.WriteAllText(
                    file,
                    JsonUtility.ToJson(
                        save,
                        true
                    )
                );

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FURNITURE SAVE: " +
                    exception
                );

            }

        }


        private static long Pack(
            int x,
            int y
        )
        {

            return
                (
                    (long)x <<
                    32
                )
                ^
                (uint)y;

        }


        private static int UnpackX(
            long key
        )
        {

            return
                (int)(
                    key >>
                    32
                );

        }


        private static int UnpackY(
            long key
        )
        {

            return
                unchecked(
                    (int)(uint)key
                );

        }

    }

}
