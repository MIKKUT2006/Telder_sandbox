#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Game.Inventory;


namespace Game.EditorTools
{

    public class ChestDebugWindow :
        EditorWindow
    {

        private string chestItemId =
            "game:chest";


        private int amount =
            1;


        [MenuItem(
            "Tools/Game/Chest Debug"
        )]
        public static void Open()
        {

            GetWindow<
                ChestDebugWindow
            >(
                "Chest Debug"
            );

        }


        private void OnGUI()
        {

            EditorGUILayout.Space(
                8
            );


            EditorGUILayout.LabelField(
                "Chest Debug",
                EditorStyles.boldLabel
            );


            EditorGUILayout.Space(
                8
            );


            chestItemId =
                EditorGUILayout.TextField(
                    "Chest Item ID",
                    chestItemId
                );


            amount =
                EditorGUILayout.IntSlider(
                    "Amount",
                    amount,
                    1,
                    100
                );


            EditorGUILayout.Space(
                12
            );


            using (
                new EditorGUI.DisabledScope(
                    !EditorApplication.isPlaying
                )
            )
            {

                if (
                    GUILayout.Button(
                        "Выдать сундук",
                        GUILayout.Height(
                            34
                        )
                    )
                )
                {

                    GiveChest();

                }

            }


            EditorGUILayout.Space(
                8
            );


            EditorGUILayout.HelpBox(
                EditorApplication.isPlaying
                    ? "Предмет будет добавлен в PlayerInventory."
                    : "Сначала запусти Play Mode.",
                MessageType.Info
            );

        }


        private void GiveChest()
        {

            PlayerInventory inventory =
                Object.FindFirstObjectByType<
                    PlayerInventory
                >();


            if (
                inventory == null
            )
            {

                Debug.LogError(
                    "CHEST DEBUG: PlayerInventory not found."
                );


                return;

            }


            int remaining =
                inventory.AddItem(
                    chestItemId,
                    amount
                );


            int added =
                amount -
                remaining;


            Debug.Log(
                "CHEST DEBUG: Added " +
                chestItemId +
                " x" +
                added
            );


            if (
                remaining >
                0
            )
            {

                Debug.LogWarning(
                    "CHEST DEBUG: Could not add " +
                    remaining +
                    " items. Inventory full or item not registered."
                );

            }

        }

    }

}

#endif
