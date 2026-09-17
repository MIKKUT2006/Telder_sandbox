
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Game.UI.MainMenu;


namespace Game.EditorTools
{

    public static class MainMenuV21UpgradeWizard
    {

        [MenuItem(
            "Tools/Game/Upgrade Main Menu V21"
        )]
        public static void Upgrade()
        {

            GameObject system =
                GameObject.Find(
                    "MainMenuSystem"
                );


            if (
                system ==
                null
            )
            {

                EditorUtility.DisplayDialog(
                    "Main Menu V21",
                    "MainMenuSystem не найден. Открой сцену MainMenu.",
                    "OK"
                );


                return;

            }


            Camera camera =
                null;


            GameObject cameraObject =
                GameObject.Find(
                    "MainMenuCamera"
                );


            if (
                cameraObject !=
                null
            )
            {

                camera =
                    cameraObject.GetComponent<
                        Camera
                    >();

            }


            if (
                camera ==
                null
            )
            {

                camera =
                    Camera.main;

            }


            MainMenuSpaceAmbient ambient =
                system.GetComponent<
                    MainMenuSpaceAmbient
                >();


            if (
                ambient ==
                null
            )
            {

                ambient =
                    system.AddComponent<
                        MainMenuSpaceAmbient
                    >();

            }


            if (
                camera !=
                null
            )
            {

                SerializedObject ambientSerialized =
                    new SerializedObject(
                        ambient
                    );


                ambientSerialized.FindProperty(
                    "menuCamera"
                ).objectReferenceValue =
                    camera;


                ambientSerialized
                    .ApplyModifiedPropertiesWithoutUndo();

            }


            SaveSelectionController saves =
                system.GetComponent<
                    SaveSelectionController
                >();


            if (
                saves !=
                null
                &&
                camera !=
                null
            )
            {

                SerializedObject saveSerialized =
                    new SerializedObject(
                        saves
                    );


                SerializedProperty property =
                    saveSerialized.FindProperty(
                        "menuCamera"
                    );


                if (
                    property !=
                    null
                )
                {

                    property.objectReferenceValue =
                        camera;

                }


                saveSerialized
                    .ApplyModifiedPropertiesWithoutUndo();

            }


            EditorUtility.SetDirty(
                system
            );


            Selection.activeGameObject =
                system;


            EditorUtility.DisplayDialog(
                "Main Menu V21",
                "Готово.\n\nДобавлены мерцающие звёзды и редкие порталы с вылетающими блоками.\nSaveSelectionController использует хаотичное размещение планет, а последнее сохранение на 15% больше.",
                "OK"
            );

        }

    }

}

#endif
