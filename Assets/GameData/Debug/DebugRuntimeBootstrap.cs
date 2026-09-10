using UnityEngine;

using Game.Visuals;

namespace Game.Debugging
{
    /// <summary>
    /// Creates debug and visual runtime systems automatically.
    /// Nothing needs to be placed in the scene.
    /// </summary>
    public static class DebugRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void CreateRuntime()
        {
            DebugMenuController existingMenu =
                Object.FindObjectOfType<
                    DebugMenuController
                >();


            if (existingMenu == null)
            {
                GameObject debugObject =
                    new GameObject(
                        "[Runtime] Debug Menu"
                    );


                debugObject.AddComponent<
                    DebugMenuController
                >();
            }


            DimensionTitleOverlay existingTitle =
                Object.FindObjectOfType<
                    DimensionTitleOverlay
                >();


            if (existingTitle == null)
            {
                GameObject titleObject =
                    new GameObject(
                        "[Runtime] Dimension Title"
                    );


                titleObject.AddComponent<
                    DimensionTitleOverlay
                >();
            }


            BiomeSkyGradient existingSky =
                Object.FindObjectOfType<
                    BiomeSkyGradient
                >();


            if (existingSky == null)
            {
                GameObject skyObject =
                    new GameObject(
                        "[Runtime] Biome Sky"
                    );


                skyObject.AddComponent<
                    BiomeSkyGradient
                >();
            }
        }
    }
}
