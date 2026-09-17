#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Game.UI.MainMenu;


namespace Game.EditorTools
{

    public static class MainMenuIsolationFixWizard
    {

        [MenuItem(
            "Tools/Game/Force Fix Main Menu Visuals"
        )]
        public static void Fix()
        {

            GameObject cameraObject =
                GameObject.Find(
                    "MainMenuCamera"
                );


            Camera camera =
                cameraObject != null
                    ? cameraObject
                        .GetComponent<Camera>()
                    : Camera.main;


            if (
                camera == null
            )
            {

                EditorUtility.DisplayDialog(
                    "Main Menu",
                    "MainMenuCamera не найдена.",
                    "OK"
                );


                return;

            }


            MainMenuSceneIsolation isolation =
                camera.GetComponent<
                    MainMenuSceneIsolation
                >();


            if (
                isolation == null
            )
            {

                isolation =
                    camera.gameObject
                        .AddComponent<
                            MainMenuSceneIsolation
                        >();

            }


            MainMenuGradientBackground gradient =
                camera.GetComponent<
                    MainMenuGradientBackground
                >();


            if (
                gradient == null
            )
            {

                gradient =
                    camera.gameObject
                        .AddComponent<
                            MainMenuGradientBackground
                        >();

            }


            SerializedObject isolationSerialized =
                new SerializedObject(
                    isolation
                );


            isolationSerialized
                .FindProperty(
                    "menuCamera"
                )
                .objectReferenceValue =
                camera;


            isolationSerialized
                .ApplyModifiedPropertiesWithoutUndo();


            camera.clearFlags =
                CameraClearFlags.SolidColor;


            camera.backgroundColor =
                new Color(
                    0.0005f,
                    0.0015f,
                    0.006f,
                    1f
                );


            camera.depth =
                10000f;


            camera.targetTexture =
                null;


            camera.rect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );


            RenderSettings.skybox =
                null;


            RenderSettings.fog =
                false;


            EditorUtility.SetDirty(
                camera.gameObject
            );


            Selection.activeGameObject =
                camera.gameObject;


            EditorUtility.DisplayDialog(
                "Main Menu",
                "Готово.\n\nДобавлена полная изоляция сцены MainMenu.\n\nТеперь отключаются камеры, Renderer, Canvas, Volume и Light, которые принадлежат не сцене MainMenu.\n\nЗапусти игру, выйди через Pause и посмотри Console: там будет строка MAIN MENU ISOLATION.",
                "OK"
            );

        }

    }

}

#endif
