using System;
using System.Collections.Generic;

using UnityEngine;

using Game.World.Lighting;


namespace Game.World.Furniture
{
    /// <summary>
    /// Applies the world RGB/sun lighting to every Furniture SpriteRenderer.
    ///
    /// Furniture is rendered outside ChunkRenderer, so it does not receive
    /// the chunk light texture automatically. This runtime samples World.GetLight()
    /// at each furniture anchor and applies the same light to its sprite.
    ///
    /// Works with:
    /// - placed furniture;
    /// - generated grass / flowers;
    /// - chests / workbenches;
    /// - torches / campfires;
    /// - any future FurnitureLayerManager visual.
    ///
    /// No component is added to every furniture object: one central updater
    /// handles all furniture with a frame budget.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class FurnitureLightingRuntime :
        MonoBehaviour
    {
        private const int MaxUpdatesPerFrame =
            512;

        private const float RescanInterval =
            0.25f;

        // Keep this aligned with the current chunk lighting shader.
        // If the chunk shader changes later, this is the one value to adjust.
        private const float LightGamma =
            0.72f;


        private readonly List<VisualEntry>
            entries =
                new List<VisualEntry>(
                    1024
                );


        private FurnitureLayerManager
            manager;


        private Transform
            furnitureRoot;


        private int
            cachedChildCount =
                -1;


        private int
            updateCursor;


        private float
            nextRescanTime;


        // =====================================================
        // BOOTSTRAP
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Bootstrap()
        {
            FurnitureLightingRuntime existing =
                UnityEngine.Object
                    .FindObjectOfType<
                        FurnitureLightingRuntime
                    >();


            if (existing != null)
            {
                return;
            }


            GameObject root =
                new GameObject(
                    "[Runtime] Furniture Lighting"
                );


            root.AddComponent<
                FurnitureLightingRuntime
            >();
        }


        // =====================================================
        // UNITY
        // =====================================================

        private void OnEnable()
        {
            FurnitureLayerManager.FurniturePlaced +=
                OnFurnitureChanged;


            FurnitureLayerManager.FurnitureRemoved +=
                OnFurnitureChanged;


            nextRescanTime =
                0f;
        }


        private void OnDisable()
        {
            FurnitureLayerManager.FurniturePlaced -=
                OnFurnitureChanged;


            FurnitureLayerManager.FurnitureRemoved -=
                OnFurnitureChanged;
        }


        private void LateUpdate()
        {
            if (
                !ResolveManager()
            )
            {
                return;
            }


            bool hierarchyChanged =
                furnitureRoot.childCount !=
                cachedChildCount;


            if (
                hierarchyChanged ||
                Time.unscaledTime >=
                nextRescanTime
            )
            {
                RebuildCache();


                nextRescanTime =
                    Time.unscaledTime +
                    RescanInterval;
            }


            UpdateLightingBudget();
        }


        // =====================================================
        // MANAGER
        // =====================================================

        private bool ResolveManager()
        {
            FurnitureLayerManager current =
                FurnitureLayerManager.Instance;


            if (
                current == null
            )
            {
                manager =
                    null;


                furnitureRoot =
                    null;


                cachedChildCount =
                    -1;


                entries.Clear();


                updateCursor =
                    0;


                return false;
            }


            if (
                current !=
                manager
            )
            {
                manager =
                    current;


                furnitureRoot =
                    current.transform;


                cachedChildCount =
                    -1;


                entries.Clear();


                updateCursor =
                    0;


                nextRescanTime =
                    0f;
            }


            return
                furnitureRoot !=
                null;
        }


        private void OnFurnitureChanged(
            int x,
            int y,
            string blockId
        )
        {
            // Generated furniture does not emit FurniturePlaced,
            // therefore hierarchy childCount + periodic rescan remain
            // the authoritative fallback.
            nextRescanTime =
                0f;
        }


        // =====================================================
        // CACHE
        // =====================================================

        private void RebuildCache()
        {
            entries.Clear();


            if (
                furnitureRoot == null
            )
            {
                cachedChildCount =
                    -1;


                updateCursor =
                    0;


                return;
            }


            int childCount =
                furnitureRoot.childCount;


            for (
                int i = 0;
                i < childCount;
                i++
            )
            {
                Transform child =
                    furnitureRoot.GetChild(
                        i
                    );


                if (
                    child == null
                )
                {
                    continue;
                }


                SpriteRenderer renderer =
                    child.GetComponent<
                        SpriteRenderer
                    >();


                if (
                    renderer == null
                    ||
                    renderer.sprite == null
                )
                {
                    continue;
                }


                if (
                    !TryReadFurnitureCoordinates(
                        child.name,
                        out int worldX,
                        out int worldY
                    )
                )
                {
                    // Current FurnitureLayerManager names visuals as:
                    // Furniture_<BlockId>_<X>_<Y>
                    //
                    // Fallback exists only for custom furniture visuals.
                    worldX =
                        Mathf.FloorToInt(
                            child.position.x
                        );


                    worldY =
                        Mathf.FloorToInt(
                            child.position.y
                        );
                }


                entries.Add(
                    new VisualEntry
                    {
                        Transform =
                            child,

                        Renderer =
                            renderer,

                        WorldX =
                            worldX,

                        WorldY =
                            worldY,

                        Alpha =
                            renderer.color.a,

                        LastPackedLight =
                            uint.MaxValue,

                        LastFullBright =
                            false
                    }
                );
            }


            cachedChildCount =
                childCount;


            if (
                updateCursor >=
                entries.Count
            )
            {
                updateCursor =
                    0;
            }


            // New visuals should not wait for the normal rolling budget.
            int immediateCount =
                Mathf.Min(
                    entries.Count,
                    MaxUpdatesPerFrame
                );


            for (
                int i = 0;
                i < immediateCount;
                i++
            )
            {
                UpdateEntry(
                    entries[i]
                );
            }
        }


        private static bool TryReadFurnitureCoordinates(
            string objectName,
            out int worldX,
            out int worldY
        )
        {
            worldX =
                0;


            worldY =
                0;


            if (
                string.IsNullOrWhiteSpace(
                    objectName
                )
            )
            {
                return false;
            }


            int last =
                objectName.LastIndexOf(
                    '_'
                );


            if (
                last <= 0 ||
                last >=
                objectName.Length - 1
            )
            {
                return false;
            }


            int secondLast =
                objectName.LastIndexOf(
                    '_',
                    last - 1
                );


            if (
                secondLast <= 0 ||
                secondLast >=
                last - 1
            )
            {
                return false;
            }


            string xText =
                objectName.Substring(
                    secondLast + 1,
                    last - secondLast - 1
                );


            string yText =
                objectName.Substring(
                    last + 1
                );


            return
                int.TryParse(
                    xText,
                    out worldX
                )
                &&
                int.TryParse(
                    yText,
                    out worldY
                );
        }


        // =====================================================
        // LIGHTING
        // =====================================================

        private void UpdateLightingBudget()
        {
            if (
                entries.Count == 0
            )
            {
                updateCursor =
                    0;


                return;
            }


            int count =
                Mathf.Min(
                    MaxUpdatesPerFrame,
                    entries.Count
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                if (
                    updateCursor >=
                    entries.Count
                )
                {
                    updateCursor =
                        0;
                }


                VisualEntry entry =
                    entries[
                        updateCursor
                    ];


                updateCursor++;


                UpdateEntry(
                    entry
                );
            }
        }


        private static void UpdateEntry(
            VisualEntry entry
        )
        {
            if (
                entry == null
                ||
                entry.Renderer == null
            )
            {
                return;
            }


            WorldManager worldManager =
                WorldManager.Instance;


            if (
                worldManager == null
            )
            {
                return;
            }


            Game.World.World world =
                worldManager.GetWorld();


            if (
                world == null
                ||
                !world.IsLoaded(
                    entry.WorldX,
                    entry.WorldY
                )
            )
            {
                return;
            }


            bool fullBright =
                Shader.GetGlobalFloat(
                    "_DebugFullBright"
                ) >
                0.5f;


            LightNode light =
                world.GetLight(
                    entry.WorldX,
                    entry.WorldY
                );


            uint packed =
                PackLight(
                    light
                );


            if (
                packed ==
                entry.LastPackedLight
                &&
                fullBright ==
                entry.LastFullBright
            )
            {
                return;
            }


            entry.LastPackedLight =
                packed;


            entry.LastFullBright =
                fullBright;


            Color lightColor =
                fullBright
                    ? Color.white
                    : CalculateLightColor(
                        light
                    );


            lightColor.a =
                entry.Alpha;


            entry.Renderer.color =
                lightColor;
        }


        private static Color CalculateLightColor(
            LightNode light
        )
        {
            float sunlight =
                light.Sun /
                15f;


            float red =
                Mathf.Max(
                    sunlight,
                    light.R /
                    15f
                );


            float green =
                Mathf.Max(
                    sunlight,
                    light.G /
                    15f
                );


            float blue =
                Mathf.Max(
                    sunlight,
                    light.B /
                    15f
                );


            // No ambient floor.
            // Light = 0 means completely black, matching the
            // current cave-lighting requirement.
            red =
                ApplyGamma(
                    red
                );


            green =
                ApplyGamma(
                    green
                );


            blue =
                ApplyGamma(
                    blue
                );


            return
                new Color(
                    red,
                    green,
                    blue,
                    1f
                );
        }


        private static float ApplyGamma(
            float value
        )
        {
            value =
                Mathf.Clamp01(
                    value
                );


            if (
                value <=
                0f
            )
            {
                return 0f;
            }


            return
                Mathf.Clamp01(
                    Mathf.Pow(
                        value,
                        LightGamma
                    )
                );
        }


        private static uint PackLight(
            LightNode light
        )
        {
            return
                (uint)light.Sun
                |
                (
                    (uint)light.R <<
                    8
                )
                |
                (
                    (uint)light.G <<
                    16
                )
                |
                (
                    (uint)light.B <<
                    24
                );
        }


        // =====================================================
        // DATA
        // =====================================================

        private sealed class VisualEntry
        {
            public Transform
                Transform;


            public SpriteRenderer
                Renderer;


            public int
                WorldX;


            public int
                WorldY;


            public float
                Alpha;


            public uint
                LastPackedLight;


            public bool
                LastFullBright;
        }
    }
}
