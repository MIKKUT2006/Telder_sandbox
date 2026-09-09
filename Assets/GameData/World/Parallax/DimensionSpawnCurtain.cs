using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game.World.Generation;

namespace Game.World.Parallax
{
    /*
     * V10.6
     *
     * Fixes the permanent black-screen deadlock from V10.5.
     *
     * V10.5 depended too strongly on this exact external call:
     *
     *     DimensionSpawnCurtain.NotifyPlayerSpawned();
     *
     * If it was missed, if SpawnPlayer returned early, or if the call
     * happened before this persistent curtain had classified the scene,
     * playerReady never became true and the curtain stayed black forever.
     *
     * V10.6 uses BOTH:
     *
     * 1. explicit NotifyPlayerSpawned() if you keep it;
     * 2. automatic detection that the Player is actually above the
     *    generated surface.
     *
     * Therefore the signal is now optional, not a hard dependency.
     */
    public sealed class DimensionSpawnCurtain :
        MonoBehaviour
    {
        private static DimensionSpawnCurtain instance;


        private bool gameplayScene;

        private bool playerReady;

        private bool parallaxReady;


        private int playerReadyFrame =
            -1;

        private int parallaxReadyFrame =
            -1;


        private float sceneLoadedTime;

        private float playerReadyTime;


        private float alpha;


        private Transform detectedPlayer;

        private WorldManager worldManager;

        private WorldGenerator worldGenerator;

        private MethodInfo getSurfaceHeightMethod;

        private FieldInfo worldManagerPlayerField;


        /*
         * Player must remain in a valid surface-spawn position for a few
         * consecutive frames. This avoids releasing on a transient state.
         */
        private int validSurfaceFrames;


        private const int RequiredSurfaceFrames =
            2;


        private const float FadeDuration =
            0.36f;


        /*
         * After the PLAYER is ready, parallax gets a short window to prime.
         * If parallax has an unrelated error, the world still becomes visible.
         */
        private const float ParallaxReadyTimeout =
            1.75f;


        /*
         * Absolute protection against a permanent black screen.
         *
         * This is intentionally generous. Normally automatic spawn detection
         * releases the curtain far earlier.
         */
        private const float AbsoluteBlackTimeout =
            8.0f;


        public static bool IsPlayerReady
        {
            get
            {
                return
                    instance != null &&
                    instance.gameplayScene &&
                    instance.playerReady;
            }
        }


        /*
         * Visual systems initialize one frame AFTER spawn readiness so the
         * Camera/Cinemachine has time to follow the teleported player.
         */
        public static bool CanInitializeVisuals
        {
            get
            {
                return
                    instance != null &&
                    instance.gameplayScene &&
                    instance.playerReady &&
                    Time.frameCount >
                    instance.playerReadyFrame;
            }
        }


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Install()
        {
            if (instance == null)
            {
                GameObject root =
                    new GameObject(
                        "DimensionSpawnCurtain"
                    );

                DontDestroyOnLoad(
                    root
                );

                instance =
                    root.AddComponent<
                        DimensionSpawnCurtain
                    >();
            }

            SceneManager.sceneLoaded -=
                OnSceneLoadedStatic;

            SceneManager.sceneLoaded +=
                OnSceneLoadedStatic;
        }


        private static void OnSceneLoadedStatic(
            Scene scene,
            LoadSceneMode mode
        )
        {
            if (instance == null)
                return;

            instance.OnSceneLoaded();
        }


        private void OnSceneLoaded()
        {
            ResetState();

            worldManager =
                FindFirstObjectByType<
                    WorldManager
                >();

            gameplayScene =
                worldManager != null;

            alpha =
                gameplayScene
                ? 1f
                : 0f;

            sceneLoadedTime =
                Time.unscaledTime;

            if (!gameplayScene)
            {
                return;
            }

            ResolveWorldReferences();

            Debug.Log(
                "WORLD CURTAIN V10.6: black until player is confirmed above surface."
            );
        }


        private void ResetState()
        {
            gameplayScene =
                false;

            playerReady =
                false;

            parallaxReady =
                false;

            playerReadyFrame =
                -1;

            parallaxReadyFrame =
                -1;

            playerReadyTime =
                0f;

            validSurfaceFrames =
                0;

            detectedPlayer =
                null;

            worldManager =
                null;

            worldGenerator =
                null;

            getSurfaceHeightMethod =
                null;

            worldManagerPlayerField =
                null;
        }


        private void ResolveWorldReferences()
        {
            if (worldManager == null)
                return;

            try
            {
                worldGenerator =
                    worldManager.GetGenerator();
            }
            catch
            {
                worldGenerator =
                    null;
            }

            if (worldGenerator != null)
            {
                getSurfaceHeightMethod =
                    worldGenerator
                        .GetType()
                        .GetMethod(
                            "GetSurfaceHeight",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic,
                            null,
                            new Type[]
                            {
                                typeof(int)
                            },
                            null
                        );
            }

            /*
             * Fallback if the Player tag is missing:
             * read the existing private Transform player field from WorldManager.
             */
            worldManagerPlayerField =
                worldManager
                    .GetType()
                    .GetField(
                        "player",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );
        }


        private void Update()
        {
            if (!gameplayScene)
                return;


            /*
             * WorldManager/WorldGenerator can become fully initialized a little
             * later than sceneLoaded, so retry references if necessary.
             */
            if (worldManager == null)
            {
                worldManager =
                    FindFirstObjectByType<
                        WorldManager
                    >();

                if (worldManager != null)
                {
                    ResolveWorldReferences();
                }
            }
            else if (
                worldGenerator == null
            )
            {
                ResolveWorldReferences();
            }


            if (!playerReady)
            {
                TryAutoDetectSurfaceSpawn();
            }


            /*
             * Absolute deadlock breaker.
             *
             * If some project-specific setup prevents automatic detection,
             * never leave the user staring at a permanent black screen.
             */
            if (
                !playerReady &&
                Time.unscaledTime -
                sceneLoadedTime >=
                AbsoluteBlackTimeout
            )
            {
                Debug.LogWarning(
                    "WORLD CURTAIN V10.6: spawn detection timed out. " +
                    "Releasing curtain to prevent permanent black screen."
                );

                MarkPlayerReady();
            }


            if (!playerReady)
            {
                alpha =
                    1f;

                return;
            }


            bool visualsReady =
                parallaxReady &&
                Time.frameCount >
                parallaxReadyFrame;


            bool parallaxTimedOut =
                Time.unscaledTime -
                playerReadyTime >=
                ParallaxReadyTimeout;


            if (
                !visualsReady &&
                !parallaxTimedOut
            )
            {
                alpha =
                    1f;

                return;
            }


            alpha =
                Mathf.MoveTowards(
                    alpha,
                    0f,
                    Time.unscaledDeltaTime /
                    FadeDuration
                );
        }


        private void TryAutoDetectSurfaceSpawn()
        {
            Transform player =
                ResolvePlayer();

            if (player == null)
            {
                validSurfaceFrames =
                    0;

                return;
            }


            int worldX =
                Mathf.FloorToInt(
                    player.position.x
                );


            if (
                !TryGetSurfaceHeight(
                    worldX,
                    out float surfaceY
                )
            )
            {
                validSurfaceFrames =
                    0;

                return;
            }


            /*
             * SpawnPlayer puts the player's CENTER above:
             *
             *     surfaceY + 1 + playerHalfHeight + 0.05
             *
             * We intentionally use a looser threshold so this detector does
             * not depend on the exact collider dimensions.
             */
            bool aboveSurface =
                player.position.y >
                surfaceY +
                0.75f;


            /*
             * The temporary bad state described by the project is around
             * world origin / caves. Requiring a sane relation to the generated
             * surface is much more reliable than just checking y != 0.
             */
            if (aboveSurface)
            {
                validSurfaceFrames++;
            }
            else
            {
                validSurfaceFrames =
                    0;
            }


            if (
                validSurfaceFrames >=
                RequiredSurfaceFrames
            )
            {
                Debug.Log(
                    "WORLD CURTAIN V10.6: player automatically detected above surface. " +
                    "PLAYER Y = " +
                    player.position.y +
                    " SURFACE Y = " +
                    surfaceY
                );

                MarkPlayerReady();
            }
        }


        private Transform ResolvePlayer()
        {
            if (detectedPlayer != null)
            {
                return detectedPlayer;
            }


            /*
             * Preferred path.
             */
            try
            {
                GameObject tagged =
                    GameObject.FindGameObjectWithTag(
                        "Player"
                    );

                if (tagged != null)
                {
                    detectedPlayer =
                        tagged.transform;

                    return detectedPlayer;
                }
            }
            catch
            {
                /*
                 * Tag may not exist in a project variant.
                 */
            }


            /*
             * Fallback: use WorldManager.player directly via reflection.
             */
            if (
                worldManager != null &&
                worldManagerPlayerField != null
            )
            {
                try
                {
                    object value =
                        worldManagerPlayerField.GetValue(
                            worldManager
                        );

                    Transform transform =
                        value as Transform;

                    if (transform != null)
                    {
                        detectedPlayer =
                            transform;

                        return detectedPlayer;
                    }
                }
                catch
                {
                }
            }


            return null;
        }


        private bool TryGetSurfaceHeight(
            int worldX,
            out float surfaceY
        )
        {
            surfaceY =
                0f;


            if (
                worldGenerator == null ||
                getSurfaceHeightMethod == null
            )
            {
                return false;
            }


            try
            {
                object value =
                    getSurfaceHeightMethod.Invoke(
                        worldGenerator,
                        new object[]
                        {
                            worldX
                        }
                    );

                if (value == null)
                    return false;

                surfaceY =
                    Convert.ToSingle(
                        value
                    );

                return true;
            }
            catch
            {
                return false;
            }
        }


        private void MarkPlayerReady()
        {
            if (playerReady)
                return;

            playerReady =
                true;

            playerReadyFrame =
                Time.frameCount;

            playerReadyTime =
                Time.unscaledTime;

            alpha =
                1f;

            Debug.Log(
                "WORLD CURTAIN V10.6: player ready. Waiting one camera frame + parallax."
            );
        }


        /*
         * Still supported, but no longer mandatory.
         */
        public static void NotifyPlayerSpawned()
        {
            if (
                instance == null ||
                !instance.gameplayScene
            )
            {
                return;
            }

            instance.MarkPlayerReady();
        }


        public static void NotifyParallaxReady()
        {
            if (
                instance == null ||
                !instance.gameplayScene
            )
            {
                return;
            }

            instance.parallaxReady =
                true;

            instance.parallaxReadyFrame =
                Time.frameCount;

            Debug.Log(
                "WORLD CURTAIN V10.6: parallax ready."
            );
        }


        private void OnGUI()
        {
            if (alpha <= 0.001f)
                return;

            int previousDepth =
                GUI.depth;

            Color previousColor =
                GUI.color;

            GUI.depth =
                -10000;

            GUI.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    Mathf.Clamp01(
                        alpha
                    )
                );

            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height
                ),
                Texture2D.whiteTexture
            );

            GUI.color =
                previousColor;

            GUI.depth =
                previousDepth;
        }
    }
}