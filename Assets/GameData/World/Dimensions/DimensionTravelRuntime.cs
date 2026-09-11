using System;
using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Save;


namespace Game.World.Dimensions
{
    public static class DimensionTravelRuntime
    {
        private static DimensionDefinition current;

        private static bool traveling;


        public static DimensionDefinition Current
        {
            get
            {
                EnsureInitialized();

                return current;
            }
        }


        public static bool IsTraveling =>
            traveling;


        public static bool IsCurrent(
            string dimensionName
        )
        {
            EnsureInitialized();


            if (
                string.IsNullOrWhiteSpace(
                    dimensionName
                )
            )
            {
                return false;
            }


            return
                string.Equals(
                    current.Name,
                    dimensionName.Trim(),
                    StringComparison.OrdinalIgnoreCase
                );
        }


        public static void TravelTo(
            string dimensionName
        )
        {
            if (
                traveling ||
                string.IsNullOrWhiteSpace(
                    dimensionName
                )
            )
            {
                return;
            }


            EnsureInitialized();


            // Сохраняем инвентарь, позицию
            // и изменения текущего измерения.
            SaveGameRuntime
                .SaveCurrentScene();


            string targetName =
                dimensionName.Trim();


            // IMPORTANT:
            // Every Telder save owns its own visited-dimension list
            // and its own seeds.
            if (
                SaveGameRuntime.HasActiveSave
            )
            {
                int saveSeed =
                    SaveGameRuntime
                        .GetOrCreateVisitedDimensionSeed(
                            targetName
                        );


                current =
                    new DimensionDefinition(
                        targetName,
                        saveSeed
                    );
            }
            else
            {
                // Direct Game-scene testing without a save
                // keeps the old DimensionDatabase behaviour.
                current =
                    DimensionDatabase
                        .GetOrCreate(
                            targetName
                        );
            }


            if (
                current == null
            )
            {
                return;
            }


            SaveGameRuntime
                .SetCurrentDimension(
                    current.Name,
                    current.Seed
                );


            traveling =
                true;


            DimensionSceneTransition
                .EnsureExists()
                .BeginTransition();
        }


        public static void SetCurrentForLoad(
            string dimensionName,
            int seed
        )
        {
            DimensionDatabase.Initialize();


            current =
                new DimensionDefinition(
                    dimensionName,
                    seed
                );


            traveling =
                false;
        }


        internal static void FinishTravel()
        {
            traveling =
                false;
        }


        private static void EnsureInitialized()
        {
            if (
                current != null
            )
            {
                return;
            }


            DimensionDatabase.Initialize();


            current =
                DimensionDatabase.GetOrCreate(
                    DimensionDatabase.StartDimension
                );
        }
    }


    // =========================================================
    // TRANSITION
    // =========================================================

    public class DimensionSceneTransition :
        MonoBehaviour
    {
        private static DimensionSceneTransition
            instance;


        private Texture2D blackTexture;


        private float alpha;


        private bool running;


        private GUIStyle teleportStyle;


        public static DimensionSceneTransition
            EnsureExists()
        {
            if (
                instance != null
            )
            {
                return instance;
            }


            GameObject gameObject =
                new GameObject(
                    "DimensionSceneTransition"
                );


            instance =
                gameObject.AddComponent<
                    DimensionSceneTransition
                >();


            DontDestroyOnLoad(
                gameObject
            );


            return instance;
        }


        private void Awake()
        {
            if (
                instance != null &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );


            blackTexture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );


            blackTexture.SetPixel(
                0,
                0,
                Color.black
            );


            blackTexture.Apply();
        }


        public void BeginTransition()
        {
            if (
                !running
            )
            {
                StartCoroutine(
                    TransitionRoutine()
                );
            }
        }


        private IEnumerator TransitionRoutine()
        {
            running =
                true;


            float duration =
                0.22f;


            float elapsed =
                0f;


            while (
                elapsed <
                duration
            )
            {
                elapsed +=
                    Time.unscaledDeltaTime;


                alpha =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );


                yield return null;
            }


            alpha =
                1f;


            Scene active =
                SceneManager.GetActiveScene();


            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    active.buildIndex,
                    LoadSceneMode.Single
                );


            if (
                operation == null
            )
            {
                DimensionTravelRuntime
                    .FinishTravel();


                running =
                    false;


                yield break;
            }


            while (
                !operation.isDone
            )
            {
                yield return null;
            }


            yield return null;


            duration =
                0.35f;


            elapsed =
                0f;


            while (
                elapsed <
                duration
            )
            {
                elapsed +=
                    Time.unscaledDeltaTime;


                alpha =
                    1f -
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );


                yield return null;
            }


            alpha =
                0f;


            running =
                false;


            DimensionTravelRuntime
                .FinishTravel();
        }


        private void OnGUI()
        {
            if (
                alpha <= 0.001f ||
                blackTexture == null
            )
            {
                return;
            }


            Color old =
                GUI.color;


            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    alpha
                );


            GUI.DrawTexture(
                new Rect(
                    0,
                    0,
                    Screen.width,
                    Screen.height
                ),
                blackTexture
            );


            if (
                teleportStyle ==
                null
            )
            {
                teleportStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );


                teleportStyle.alignment =
                    TextAnchor.MiddleCenter;


                teleportStyle.fontSize =
                    Mathf.RoundToInt(
                        Mathf.Clamp(
                            Screen.height *
                            0.035f,
                            22f,
                            42f
                        )
                    );


                teleportStyle.fontStyle =
                    FontStyle.Bold;


                teleportStyle.normal.textColor =
                    Color.white;
            }


            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    alpha
                );


            GUI.Label(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height
                ),
                "Телепортация...",
                teleportStyle
            );


            GUI.color =
                old;
        }
    }
}
