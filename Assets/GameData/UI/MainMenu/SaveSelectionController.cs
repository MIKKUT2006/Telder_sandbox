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


        [Header("Planet Layout")]

        [SerializeField]
        private Transform planetRoot;


        [SerializeField]
        private Material planetMaterial;


        [SerializeField]
        private Vector3 firstPlanetLocalPosition =
            new Vector3(
                -4f,
                1f,
                0f
            );


        [SerializeField]
        private float horizontalSpacing =
            2.7f;


        [SerializeField]
        private float verticalSpacing =
            2.6f;


        [SerializeField]
        private int planetsPerRow =
            4;


        [SerializeField]
        private float planetScale =
            1.15f;


        [Header("New Telder World")]

        [SerializeField]
        private TMP_InputField worldNameInput;


        [SerializeField]
        private string defaultWorldName =
            "Telder мир";


        private readonly List<GameObject>
            createdPlanets =
            new List<GameObject>();


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
                worldNameInput != null
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


            Time.timeScale =
                1f;


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


            Time.timeScale =
                1f;


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
                planetRoot != null
                    ? planetRoot
                    : transform;


            GameObject root =
                new GameObject(
                    "TelderWorld_" +
                    save.SaveId
                );


            root.transform.SetParent(
                parent,
                false
            );


            int row =
                index /
                Mathf.Max(
                    1,
                    planetsPerRow
                );


            int column =
                index %
                Mathf.Max(
                    1,
                    planetsPerRow
                );


            root.transform.localPosition =
                firstPlanetLocalPosition +
                new Vector3(
                    column *
                    horizontalSpacing,

                    -row *
                    verticalSpacing,

                    0f
                );


            GameObject sphere =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );


            sphere.name =
                "Planet";


            sphere.transform.SetParent(
                root.transform,
                false
            );


            sphere.transform.localScale =
                Vector3.one *
                planetScale;


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
                    -1.05f *
                    planetScale,
                    0f
                );


            TextMeshPro label =
                labelObject.AddComponent<
                    TextMeshPro
                >();


            label.alignment =
                TextAlignmentOptions.Center;


            label.fontSize =
                3.5f;


            label.text =
                save.DisplayName;


            RectTransform rect =
                label.rectTransform;


            rect.sizeDelta =
                new Vector2(
                    4f,
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


        private void ClearPlanets()
        {
            for (
                int i = 0;
                i < createdPlanets.Count;
                i++
            )
            {
                if (
                    createdPlanets[i] != null
                )
                {
                    Destroy(
                        createdPlanets[i]
                    );
                }
            }


            createdPlanets.Clear();
        }
    }
}
