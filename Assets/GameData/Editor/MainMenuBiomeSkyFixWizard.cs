#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Game.UI.MainMenu;


namespace Game.EditorTools
{

    public static class MainMenuBiomeSkyFixWizard
    {

        [MenuItem(
            "Tools/Game/Fix Biome Sky In Main Menu"
        )]
        public static void Fix()
        {

            GameObject host =
                GameObject.Find(
                    "MainMenuSystem"
                );


            if (
                host == null
            )
            {

                host =
                    GameObject.Find(
                        "MainMenuCamera"
                    );

            }


            if (
                host == null
            )
            {

                EditorUtility.DisplayDialog(
                    "Biome Sky Fix",
                    "Не найден MainMenuSystem или MainMenuCamera.",
                    "OK"
                );


                return;

            }


            MainMenuBiomeSkyCleanup cleanup =
                host.GetComponent<
                    MainMenuBiomeSkyCleanup
                >();


            if (
                cleanup == null
            )
            {

                cleanup =
                    host.AddComponent<
                        MainMenuBiomeSkyCleanup
                    >();

            }


            EditorUtility.SetDirty(
                host
            );


            Selection.activeGameObject =
                host;


            EditorUtility.DisplayDialog(
                "Biome Sky Fix",
                "Готово.\n\nMainMenu теперь точечно удаляет объект \"Biome Sky Gradient\" и отключает компоненты, в имени типа которых есть BiomeSkyGradient.\n\nЭто уже фикс конкретно найденной причины, а не общий workaround.",
                "OK"
            );

        }

    }

}

#endif
