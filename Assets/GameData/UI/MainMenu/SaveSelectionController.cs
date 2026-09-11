
using System;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Save;


namespace Game.UI.MainMenu
{

    public class SaveSelectionController :
        MonoBehaviour
    {

        [Header("Scene")]

        [SerializeField]
        private string gameSceneName =
            "Game";


        [Header("Planet References")]

        [SerializeField]
        private Transform planetRoot;


        [SerializeField]
        private Material planetMaterial;


        [SerializeField]
        private Camera menuCamera;


        [Header("Chaotic Planet Layout")]

        [Tooltip("Reserved left side keeps planets away from menu/back buttons.")]
        [SerializeField]
        private Vector2 viewportMin =
            new Vector2(
                0.34f,
                0.25f
            );


        [SerializeField]
        private Vector2 viewportMax =
            new Vector2(
                0.92f,
                0.82f
            );


        [SerializeField]
        private float planetDepth =
            10f;


        [SerializeField]
        private float planetScale =
            1.15f;


        [SerializeField]
        private float latestSaveScaleMultiplier =
            1.15f;


        [SerializeField]
        private float minimumPlanetDistance =
            2.35f;


        [Header("New Telder World")]

        [SerializeField]
        private TMP_InputField worldNameInput;


        [SerializeField]
        private string defaultWorldName =
            "Telder мир";


        private readonly List<GameObject>
            createdPlanets =
            new List<GameObject>();


        private readonly List<Vector3>
            occupiedPositions =
            new List<Vector3>();


        private void Awake()
        {
            if (menuCamera == null)
                menuCamera = Camera.main;

            SaveContextMenuController.EnsureExists(this);
        }


        public void Refresh()
        {

            ClearPlanets();


            List<TelderSaveSummary> saves =
                SaveGameRuntime
                    .GetSaveSummaries();


            for (
                int i = 0;
                i < saves.Count;
                i++
            )
            {

                CreatePlanet(
                    saves[i],
                    i
                );

            }

        }


        public void CreateNewTelderWorld()
        {

            string name =
                worldNameInput !=
                null
                    ? worldNameInput.text
                    : null;


            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {

                name =
                    defaultWorldName;

            }


            if (
                !SaveGameRuntime
                    .CreateNewSave(
                        name
                    )
            )
            {

                return;

            }


            Time.timeScale = 1f;
            WorldLoadingOverlay.Show("Загрузка мира...");

            SceneManager.LoadScene(
                gameSceneName
            );

        }


        public void LoadSave(
            string saveId
        )
        {

            if (
                !SaveGameRuntime.LoadSave(
                    saveId
                )
            )
            {

                return;

            }


            Time.timeScale = 1f;
            WorldLoadingOverlay.Show("Загрузка мира...");

            SceneManager.LoadScene(
                gameSceneName
            );

        }


        private void CreatePlanet(
            TelderSaveSummary save,
            int index
        )
        {

            Transform parent =
                planetRoot !=
                null
                    ? planetRoot
                    : transform;


            GameObject root =
                new GameObject(
                    "TelderWorld_" +
                    save.SaveId
                );


            root.transform.SetParent(
                parent,
                true
            );


            bool latest =
                index ==
                0;


            float scale =
                planetScale *
                (
                    latest
                        ? latestSaveScaleMultiplier
                        : 1f
                );


            Vector3 worldPosition =
                FindPlanetPosition(
                    save.SaveId,
                    scale,
                    index
                );


            root.transform.position =
                worldPosition;


            occupiedPositions.Add(
                worldPosition
            );


            GameObject sphere =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );


            sphere.name =
                latest
                    ? "Planet_Latest"
                    : "Planet";


            sphere.transform.SetParent(
                root.transform,
                false
            );


            sphere.transform.localScale =
                Vector3.one *
                scale;


            GameObject labelObject =
                new GameObject(
                    "Name"
                );


            labelObject.transform.SetParent(
                root.transform,
                false
            );


            labelObject.transform.localPosition =
                new Vector3(
                    0f,
                    -1.15f *
                    scale,
                    0f
                );


            TextMeshPro label =
                labelObject.AddComponent<
                    TextMeshPro
                >();


            label.alignment =
                TextAlignmentOptions.Center;


            label.fontSize =
                latest
                    ? 4f
                    : 3.5f;


            label.text =
                save.DisplayName;


            RectTransform rect =
                label.rectTransform;


            rect.sizeDelta =
                new Vector2(
                    4.5f,
                    1f
                );


            SavePlanet planet =
                sphere.AddComponent<
                    SavePlanet
                >();


            planet.Configure(
                this,
                save.SaveId,
                save.DisplayName,
                label,
                planetMaterial
            );


            createdPlanets.Add(
                root
            );

        }


        private Vector3 FindPlanetPosition(
            string saveId,
            float scale,
            int index
        )
        {

            if (
                menuCamera ==
                null
            )
            {

                menuCamera =
                    Camera.main;

            }


            System.Random random =
                new System.Random(
                    StableHash(
                        saveId
                    )
                );


            float requiredDistance =
                minimumPlanetDistance *
                Mathf.Max(
                    0.8f,
                    scale /
                    Mathf.Max(
                        0.01f,
                        planetScale
                    )
                );


            Vector3 fallback =
                Vector3.zero;


            for (
                int attempt = 0;
                attempt < 80;
                attempt++
            )
            {

                float vx =
                    Mathf.Lerp(
                        viewportMin.x,
                        viewportMax.x,
                        (float)random.NextDouble()
                    );


                float vy =
                    Mathf.Lerp(
                        viewportMin.y,
                        viewportMax.y,
                        (float)random.NextDouble()
                    );


                Vector3 candidate;


                if (
                    menuCamera !=
                    null
                )
                {

                    candidate =
                        menuCamera
                            .ViewportToWorldPoint(
                                new Vector3(
                                    vx,
                                    vy,
                                    planetDepth
                                )
                            );

                }
                else
                {

                    candidate =
                        new Vector3(
                            Mathf.Lerp(
                                -2f,
                                6f,
                                vx
                            ),
                            Mathf.Lerp(
                                -3f,
                                3f,
                                vy
                            ),
                            0f
                        );

                }


                // Planets should live around the same Z plane.
                candidate.z =
                    0f;


                fallback =
                    candidate;


                bool overlaps =
                    false;


                for (
                    int i = 0;
                    i < occupiedPositions.Count;
                    i++
                )
                {

                    if (
                        Vector2.Distance(
                            occupiedPositions[i],
                            candidate
                        )
                        <
                        requiredDistance
                    )
                    {

                        overlaps =
                            true;


                        break;

                    }

                }


                if (
                    !overlaps
                )
                {

                    return candidate;

                }

            }


            // When there are many saves we still keep the object
            // inside the allowed viewport instead of pushing it
            // behind the menu buttons.
            return
                fallback +
                new Vector3(
                    0f,
                    (
                        index %
                        3
                        -
                        1
                    )
                    *
                    0.15f,
                    0f
                );

        }


        private int StableHash(
            string value
        )
        {

            unchecked
            {

                int hash =
                    17;


                if (
                    value !=
                    null
                )
                {

                    for (
                        int i = 0;
                        i < value.Length;
                        i++
                    )
                    {

                        hash =
                            hash *
                            31 +
                            value[i];

                    }

                }


                return hash;

            }

        }


        private void ClearPlanets()
        {

            for (
                int i = 0;
                i < createdPlanets.Count;
                i++
            )
            {

                if (
                    createdPlanets[i] !=
                    null
                )
                {

                    Destroy(
                        createdPlanets[i]
                    );

                }

            }


            createdPlanets.Clear();


            occupiedPositions.Clear();

        }

    }

}
