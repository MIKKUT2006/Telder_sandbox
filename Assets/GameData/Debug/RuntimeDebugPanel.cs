
using System;
using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Items;
using Game.World;
using Game.World.Structures;

namespace Game.Debugging
{
    public class RuntimeDebugPanel : MonoBehaviour
    {
        private static RuntimeDebugPanel instance;

        private bool open;
        private Vector2 chestScroll;
        private Vector2 structureScroll;

        private Rect windowRect =
            new Rect(30f, 30f, 430f, 620f);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {
            if (instance != null)
                return;

            GameObject gameObject =
                new GameObject(
                    "RuntimeDebugPanel"
                );

            instance =
                gameObject.AddComponent<
                    RuntimeDebugPanel
                >();

            DontDestroyOnLoad(gameObject);
        }

        private void Awake()
        {
            if (instance != null &&
                instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.L))
                return;

            if (WorldManager.Instance == null)
            {
                open = false;
                return;
            }

            open = !open;

            if (open)
                StructureRegistry.Reload();
        }

        private void OnGUI()
        {
            if (!open ||
                WorldManager.Instance == null)
                return;

            windowRect =
                GUI.Window(
                    941723,
                    windowRect,
                    DrawWindow,
                    "TELDER DEBUG [L]"
                );
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(
                "СУНДУКИ",
                GUI.skin.box
            );

            chestScroll =
                GUILayout.BeginScrollView(
                    chestScroll,
                    GUILayout.Height(180f)
                );

            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {
                if (block == null ||
                    !HasTag(block, "chest"))
                    continue;

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    block.Name +
                    "\n" +
                    block.ID,
                    GUILayout.Width(220f)
                );

                if (GUILayout.Button(
                    block.Closed
                        ? "Выдать (Closed)"
                        : "Выдать",
                    GUILayout.Height(38f)))
                {
                    GiveItem(block.ID);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10f);

            GUILayout.Label(
                "СТРУКТУРЫ",
                GUI.skin.box
            );

            structureScroll =
                GUILayout.BeginScrollView(
                    structureScroll,
                    GUILayout.Height(280f)
                );

            var structures =
                StructureRegistry.GetAll();

            for (int i = 0;
                 i < structures.Count;
                 i++)
            {
                StructureDefinition structure =
                    structures[i];

                if (structure == null)
                    continue;

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    structure.DisplayName +
                    "\n" +
                    structure.ID,
                    GUILayout.Width(220f)
                );

                if (GUILayout.Button(
                    "Спавн перед собой",
                    GUILayout.Height(38f)))
                {
                    SpawnStructure(structure);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            GUILayout.Label("L — закрыть окно");

            GUI.DragWindow();
        }

        private void GiveItem(string itemId)
        {
            PlayerInventory inventory =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        PlayerInventory
                    >();

            if (inventory == null)
            {
                Debug.LogError(
                    "DEBUG: PlayerInventory not found."
                );
                return;
            }

            if (!ItemRegistry.Contains(itemId))
            {
                Debug.LogWarning(
                    "DEBUG: No item registered for chest block " +
                    itemId
                );
                return;
            }

            inventory.AddItem(itemId, 1);
        }

        private void SpawnStructure(
            StructureDefinition structure
        )
        {
            PlayerInventory inventory =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        PlayerInventory
                    >();

            if (inventory == null)
                return;

            bool spawned =
                StructureRuntimeSpawner
                    .SpawnAheadOfPlayer(
                        structure,
                        inventory.transform
                    );

            Debug.Log(
                "DEBUG STRUCTURE SPAWN: " +
                structure.ID +
                " = " +
                spawned
            );
        }

        private bool HasTag(
            BlockDefinition block,
            string tag
        )
        {
            if (block.Tags == null)
                return false;

            for (int i = 0;
                 i < block.Tags.Count;
                 i++)
            {
                if (string.Equals(
                    block.Tags[i],
                    tag,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
