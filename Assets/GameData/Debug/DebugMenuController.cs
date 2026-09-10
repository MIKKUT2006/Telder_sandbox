using UnityEngine;

using Game.World;
using Game.World.Biomes;
using Game.World.Dimensions;
using Game.World.Generation;
using Game.Visuals;

namespace Game.Debugging
{
    /// <summary>
    /// Runtime debug menu.
    ///
    /// L - open / close.
    ///
    /// Uses IMGUI intentionally:
    /// no Canvas, prefab, EventSystem or TMP setup is required.
    /// </summary>
    public sealed class DebugMenuController :
        MonoBehaviour
    {
        private bool isOpen;

        private Rect windowRect =
            new Rect(
                20f,
                20f,
                360f,
                430f
            );

        private Vector2 commandScroll;


        private void Awake()
        {
            DontDestroyOnLoad(
                gameObject
            );


            DebugLightingState.SetFullBright(
                false
            );


            RegisterBuiltInCommands();
        }


        private void Update()
        {
            if (
                Input.GetKeyDown(
                    KeyCode.L
                )
            )
            {
                isOpen =
                    !isOpen;
            }
        }


        private void OnGUI()
        {
            if (!isOpen)
                return;


            windowRect =
                GUI.Window(
                    GetInstanceID(),
                    windowRect,
                    DrawWindow,
                    "DEBUG MENU   [L]"
                );
        }


        private void DrawWindow(
            int windowID
        )
        {
            GUILayout.Space(
                4f
            );


            DrawWorldInfo();


            GUILayout.Space(
                8f
            );


            bool newFullBright =
                GUILayout.Toggle(
                    DebugLightingState.FullBright,
                    "FULL BRIGHT / DISABLE SHADOWS"
                );


            if (
                newFullBright !=
                DebugLightingState.FullBright
            )
            {
                DebugLightingState.SetFullBright(
                    newFullBright
                );
            }


            GUILayout.Space(
                8f
            );


            if (
                GUILayout.Button(
                    "REBUILD LIGHTING"
                )
            )
            {
                RebuildLighting();
            }


            if (
                GUILayout.Button(
                    "SHOW DIMENSION TITLE"
                )
            )
            {
                ShowCurrentDimensionTitle();
            }


            GUILayout.Space(
                10f
            );


            GUILayout.Label(
                "COMMANDS"
            );


            commandScroll =
                GUILayout.BeginScrollView(
                    commandScroll,
                    GUILayout.Height(
                        120f
                    )
                );


            var commands =
                DebugCommandRegistry.Commands;


            for (
                int i = 0;
                i < commands.Count;
                i++
            )
            {
                DebugCommandRegistry.Command command =
                    commands[i];


                if (
                    command ==
                    null ||
                    command.Action ==
                    null
                )
                {
                    continue;
                }


                if (
                    GUILayout.Button(
                        command.Name
                    )
                )
                {
                    command.Action.Invoke();
                }
            }


            GUILayout.EndScrollView();


            GUILayout.FlexibleSpace();


            if (
                GUILayout.Button(
                    "CLOSE"
                )
            )
            {
                isOpen =
                    false;
            }


            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    10000f,
                    24f
                )
            );
        }


        private void DrawWorldInfo()
        {
            Camera camera =
                Camera.main;


            Vector3 position =
                camera != null
                    ? camera.transform.position
                    : Vector3.zero;


            GUILayout.Label(
                "X: " +
                position.x.ToString(
                    "0.0"
                ) +
                "   Y: " +
                position.y.ToString(
                    "0.0"
                )
            );


            string biomeName =
                GetCurrentBiomeName(
                    position.x
                );


            GUILayout.Label(
                "Biome: " +
                biomeName
            );


            string dimensionName =
                GetCurrentDimensionName();


            GUILayout.Label(
                "Dimension: " +
                dimensionName
            );
        }


        private string GetCurrentBiomeName(
            float x
        )
        {
            WorldManager manager =
                WorldManager.Instance;


            if (manager == null)
                return "UNKNOWN";


            WorldGenerator generator =
                manager.GetGenerator();


            if (generator == null)
                return "UNKNOWN";


            BiomeDefinition biome =
                generator.GetDominantBiome(
                    Mathf.FloorToInt(
                        x
                    )
                );


            if (biome == null)
                return "UNKNOWN";


            return
                string.IsNullOrWhiteSpace(
                    biome.DisplayName
                )
                    ? "UNNAMED"
                    : biome.DisplayName;
        }


        private string GetCurrentDimensionName()
        {
            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (dimension == null)
                return "UNKNOWN";


            return
                string.IsNullOrWhiteSpace(
                    dimension.Name
                )
                    ? "UNNAMED"
                    : dimension.Name;
        }


        private void RegisterBuiltInCommands()
        {
            DebugCommandRegistry.Register(
                "Toggle Full Bright",
                DebugLightingState.Toggle
            );


            DebugCommandRegistry.Register(
                "Rebuild Lighting",
                RebuildLighting
            );


            DebugCommandRegistry.Register(
                "Show Dimension Title",
                ShowCurrentDimensionTitle
            );
        }


        private void RebuildLighting()
        {
            WorldManager manager =
                WorldManager.Instance;


            if (manager == null)
                return;


            Game.World.World world =
                manager.GetWorld();


            if (world != null)
            {
                world.RebuildLighting();
            }


            if (
                manager.GetChunkRenderer() !=
                null &&
                world !=
                null
            )
            {
                manager
                    .GetChunkRenderer()
                    .RenderAllLoaded(
                        world
                    );
            }
        }


        private void ShowCurrentDimensionTitle()
        {
            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (dimension == null)
                return;


            DimensionTitleOverlay.Show(
                dimension.Name
            );
        }
    }
}
