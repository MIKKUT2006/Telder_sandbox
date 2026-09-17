
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Game.UI.Cursor;


namespace Game.EditorTools
{

    public static class PixelCursorSetupWizard
    {

        [MenuItem(
            "Tools/Game/Create Pixel Cursor"
        )]
        public static void CreatePixelCursor()
        {

            PixelCursorController existing =
                Object.FindFirstObjectByType<
                    PixelCursorController
                >();


            if (
                existing !=
                null
            )
            {

                Selection.activeGameObject =
                    existing.gameObject;


                EditorUtility.DisplayDialog(
                    "Pixel Cursor",
                    "PixelCursorController уже существует. Выделил его в Hierarchy.",
                    "OK"
                );


                return;

            }


            GameObject gameObject =
                new GameObject(
                    "PixelCursorSystem"
                );


            gameObject.AddComponent<
                PixelCursorController
            >();


            Selection.activeGameObject =
                gameObject;


            EditorUtility.DisplayDialog(
                "Pixel Cursor",
                "Готово.\n\nПо умолчанию используются сгенерированные пиксельные курсоры.\nЕсли хочешь свои — назначь Default Cursor и Pointer Cursor в Inspector.",
                "OK"
            );

        }

    }

}

#endif
