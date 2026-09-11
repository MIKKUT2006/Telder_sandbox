using System;

using UnityEngine;
using UnityEngine.SceneManagement;


namespace Game.UI.MainMenu
{

    [DefaultExecutionOrder(-32500)]
    public class MainMenuBiomeSkyCleanup :
        MonoBehaviour
    {

        [Header("Target")]

        [SerializeField]
        private string biomeSkyObjectName =
            "Biome Sky Gradient";


        [SerializeField]
        private bool destroyMatchingObject =
            true;


        [SerializeField]
        private bool disableMatchingComponents =
            true;


        [Header("Debug")]

        [SerializeField]
        private bool logCleanup =
            true;


        private int lastCleanupFrame =
            -1;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            Cleanup();

        }


        private void OnEnable()
        {

            SceneManager.sceneLoaded +=
                OnSceneLoaded;


            Cleanup();

        }


        private void OnDisable()
        {

            SceneManager.sceneLoaded -=
                OnSceneLoaded;

        }


        private void Start()
        {

            Cleanup();

        }


        private void LateUpdate()
        {

            // BiomeSky manager может создать фон
            // уже через кадр после загрузки MainMenu.
            //
            // Поэтому пока мы в MainMenu,
            // проверяем наличие объекта каждый кадр.
            Cleanup();

        }


        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {

            Cleanup();

        }


        // =====================================================
        // CLEANUP
        // =====================================================

        private void Cleanup()
        {

            if (
                lastCleanupFrame ==
                Time.frameCount
            )
            {

                return;

            }


            lastCleanupFrame =
                Time.frameCount;


            int destroyedObjects =
                0;


            int disabledComponents =
                0;


            // =================================================
            // 1. OBJECT NAME
            // =================================================
            //
            // Ищем и активные, и неактивные runtime-объекты.
            //
            // Resources.FindObjectsOfTypeAll используется
            // только потому, что GameObject.Find не видит
            // inactive объекты.
            //
            // Обязательно фильтруем scene.IsValid(),
            // чтобы не трогать prefab/assets из Project.
            //

            GameObject[] objects =
                UnityEngine.Resources.FindObjectsOfTypeAll<
                    GameObject
                >();


            for (
                int i = 0;
                i < objects.Length;
                i++
            )
            {

                GameObject target =
                    objects[i];


                if (
                    target == null
                    ||
                    !target.scene.IsValid()
                )
                {

                    continue;

                }


                if (
                    !IsBiomeSkyName(
                        target.name
                    )
                )
                {

                    continue;

                }


                if (
                    destroyMatchingObject
                )
                {

                    Destroy(
                        target
                    );


                    destroyedObjects++;

                }
                else
                {

                    target.SetActive(
                        false
                    );

                }

            }


            // =================================================
            // 2. COMPONENT TYPE
            // =================================================
            //
            // Если сам GameObject называется иначе,
            // но на persistent manager висит:
            //
            // BiomeSkyGradient
            // BiomeSkyGradientController
            // BiomeSkyGradientManager
            //
            // отключаем такой MonoBehaviour.
            //

            if (
                disableMatchingComponents
            )
            {

                MonoBehaviour[] behaviours =
                    UnityEngine.Resources.FindObjectsOfTypeAll<
                        MonoBehaviour
                    >();


                for (
                    int i = 0;
                    i < behaviours.Length;
                    i++
                )
                {

                    MonoBehaviour behaviour =
                        behaviours[i];


                    if (
                        behaviour == null
                        ||
                        !behaviour.gameObject
                            .scene
                            .IsValid()
                    )
                    {

                        continue;

                    }


                    // Сам этот cleanup не трогаем.
                    if (
                        behaviour ==
                        this
                    )
                    {

                        continue;

                    }


                    Type type =
                        behaviour.GetType();


                    string typeName =
                        type.Name;


                    if (
                        typeName.IndexOf(
                            "BiomeSkyGradient",
                            StringComparison.OrdinalIgnoreCase
                        ) <
                        0
                    )
                    {

                        continue;

                    }


                    if (
                        behaviour.enabled
                    )
                    {

                        behaviour.enabled =
                            false;


                        disabledComponents++;

                    }

                }

            }


            // =================================================
            // RENDER SETTINGS
            // =================================================

            RenderSettings.skybox =
                null;


            RenderSettings.fog =
                false;


            // =================================================
            // DEBUG
            // =================================================

            if (
                logCleanup
                &&
                (
                    destroyedObjects >
                    0
                    ||
                    disabledComponents >
                    0
                )
            )
            {

                Debug.Log(
                    "MAIN MENU BIOME SKY CLEANUP: " +
                    "Destroyed objects = " +
                    destroyedObjects +
                    ", Disabled BiomeSkyGradient components = " +
                    disabledComponents
                );

            }

        }


        // =====================================================
        // NAME CHECK
        // =====================================================

        private bool IsBiomeSkyName(
            string objectName
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    objectName
                )
            )
            {

                return false;

            }


            if (
                string.Equals(
                    objectName,
                    biomeSkyObjectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {

                return true;

            }


            // На случай:
            // Biome Sky Gradient(Clone)
            // Biome Sky Gradient 1
            if (
                objectName.StartsWith(
                    biomeSkyObjectName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {

                return true;

            }


            return false;

        }

    }

}
