
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

    }


    [Serializable]
    public class FurnitureSaveFile
    {

        public List<FurnitureSaveEntry> Entries =
            new List<FurnitureSaveEntry>();

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


        // RGB block light has a maximum level of 15.
        // +2 matches the lighting propagation safety radius.
        private const int LightVisualRefreshRadius =
            17;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {

            if (
                Instance !=
                null
            )
            {

                return;

            }


            if (
                WorldManager.Instance ==
                null
            )
            {

                return;

            }


            GameObject gameObject =
                new GameObject(
                    "FurnitureLayer"
                );


            gameObject.transform.SetParent(
                WorldManager.Instance.transform,
                false
            );


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
                        blockId
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
                        1,
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


            visuals[
                key
            ] =
                gameObject;

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


                save.Entries.AddRange(
                    data.Values
                );


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

    }

}
