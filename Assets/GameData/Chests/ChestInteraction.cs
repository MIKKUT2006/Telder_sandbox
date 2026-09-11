using UnityEngine;

using Game.Inventory.UI;
using Game.World;
using Game.Chests.UI;


namespace Game.Chests
{

    [DefaultExecutionOrder(-5000)]
    public class ChestInteraction :
        MonoBehaviour
    {

        [Header("Interaction")]

        [SerializeField]
        private float interactionDistance =
            6f;


        [SerializeField]
        private Camera worldCamera;


        [Header("Cursor")]

        [Tooltip(
            "Необязательно. Если не назначено, " +
            "скрипт создаст простой пиксельный палец сам."
        )]
        [SerializeField]
        private Texture2D fingerCursor;


        [SerializeField]
        private Vector2 cursorHotspot =
            new Vector2(
                1f,
                1f
            );


        private ChestManager chestManager;


        private Texture2D generatedCursor;


        private bool fingerVisible;


        private void Awake()
        {

            chestManager =
                GetComponent<
                    ChestManager
                >();


            if (
                worldCamera == null
            )
            {

                worldCamera =
                    Camera.main;

            }


            if (
                fingerCursor == null
            )
            {

                generatedCursor =
                    CreateFallbackFingerCursor();

            }

        }


        private void OnDisable()
        {

            SetDefaultCursor();

        }


        private void OnDestroy()
        {

            SetDefaultCursor();


            if (
                generatedCursor != null
            )
            {

                Destroy(
                    generatedCursor
                );

            }

        }


        private void Update()
        {

            if (
                chestManager == null
                ||
                WorldManager.Instance ==
                null
            )
            {

                SetDefaultCursor();

                return;

            }


            if (
                worldCamera == null
            )
            {

                worldCamera =
                    Camera.main;

            }


            if (
                worldCamera == null
            )
            {

                SetDefaultCursor();

                return;

            }


            // While an inventory/chest UI is already open,
            // world hover should not fight with the UI cursor.
            if (
                InventoryUI.Instance != null
                &&
                InventoryUI.Instance.IsOpen
            )
            {

                SetDefaultCursor();

                return;

            }


            GetMouseCell(
                out int worldX,
                out int worldY
            );


            bool inRange =
                IsInRange(
                    worldX,
                    worldY
                );


            bool hoveringChest =
                inRange
                &&
                chestManager.IsChestAt(
                    worldX,
                    worldY
                );


            if (
                hoveringChest
            )
            {

                SetFingerCursor();


                if (
                    Input.GetMouseButtonDown(
                        1
                    )
                )
                {

                    if (
                        ChestUIController.Instance !=
                        null
                    )
                    {

                        ChestUIController.Instance
                            .OpenChest(
                                worldX,
                                worldY
                            );

                    }

                }

            }
            else
            {

                SetDefaultCursor();

            }

        }


        private void GetMouseCell(
            out int worldX,
            out int worldY
        )
        {

            Vector3 mouse =
                worldCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );


            worldX =
                Mathf.FloorToInt(
                    mouse.x
                );


            worldY =
                Mathf.FloorToInt(
                    mouse.y
                );

        }


        private bool IsInRange(
            int worldX,
            int worldY
        )
        {

            Vector2 cellCenter =
                new Vector2(
                    worldX +
                    0.5f,
                    worldY +
                    0.5f
                );


            return
                Vector2.Distance(
                    transform.position,
                    cellCenter
                )
                <=
                interactionDistance;

        }


        private void SetFingerCursor()
        {

            if (
                fingerVisible
            )
            {

                return;

            }


            Texture2D texture =
                fingerCursor != null
                    ? fingerCursor
                    : generatedCursor;


            if (
                texture == null
            )
            {

                return;

            }


            UnityEngine.Cursor.SetCursor(
                texture,
                cursorHotspot,
                CursorMode.Auto
            );


            fingerVisible =
                true;

        }


        private void SetDefaultCursor()
        {

            if (
                !fingerVisible
            )
            {

                return;

            }


            UnityEngine.Cursor.SetCursor(
                null,
                Vector2.zero,
                CursorMode.Auto
            );


            fingerVisible =
                false;

        }


        // =====================================================
        // FALLBACK PIXEL FINGER
        // =====================================================

        private Texture2D CreateFallbackFingerCursor()
        {

            const int size =
                16;


            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                "GeneratedChestFingerCursor";


            texture.filterMode =
                FilterMode.Point;


            Color clear =
                new Color(
                    0f,
                    0f,
                    0f,
                    0f
                );


            Color outline =
                new Color(
                    0.05f,
                    0.05f,
                    0.07f,
                    1f
                );


            Color fill =
                new Color(
                    1f,
                    1f,
                    1f,
                    1f
                );


            Color[] pixels =
                new Color[
                    size *
                    size
                ];


            for (
                int i = 0;
                i < pixels.Length;
                i++
            )
            {

                pixels[i] =
                    clear;

            }


            // Very small pixel-art pointing hand.
            // Texture coordinates begin at bottom-left.
            int[,] fillPixels =
            {
                {7,14},{8,14},
                {7,13},{8,13},
                {7,12},{8,12},
                {7,11},{8,11},
                {7,10},{8,10},
                {7,9},{8,9},{9,9},{10,9},
                {6,8},{7,8},{8,8},{9,8},{10,8},{11,8},
                {5,7},{6,7},{7,7},{8,7},{9,7},{10,7},{11,7},
                {5,6},{6,6},{7,6},{8,6},{9,6},{10,6},{11,6},
                {6,5},{7,5},{8,5},{9,5},{10,5},{11,5},
                {6,4},{7,4},{8,4},{9,4},{10,4},
                {7,3},{8,3},{9,3},{10,3}
            };


            for (
                int i = 0;
                i < fillPixels.GetLength(0);
                i++
            )
            {

                int x =
                    fillPixels[i, 0];


                int y =
                    fillPixels[i, 1];


                SetPixel(
                    pixels,
                    size,
                    x,
                    y,
                    fill
                );


                // outline around fill
                for (
                    int ox = -1;
                    ox <= 1;
                    ox++
                )
                {

                    for (
                        int oy = -1;
                        oy <= 1;
                        oy++
                    )
                    {

                        int px =
                            x +
                            ox;


                        int py =
                            y +
                            oy;


                        if (
                            px < 0
                            ||
                            px >= size
                            ||
                            py < 0
                            ||
                            py >= size
                        )
                        {

                            continue;

                        }


                        int index =
                            py *
                            size +
                            px;


                        if (
                            pixels[index].a <
                            0.01f
                        )
                        {

                            pixels[index] =
                                outline;

                        }

                    }

                }

            }


            // Restore fill after outline pass.
            for (
                int i = 0;
                i < fillPixels.GetLength(0);
                i++
            )
            {

                SetPixel(
                    pixels,
                    size,
                    fillPixels[i, 0],
                    fillPixels[i, 1],
                    fill
                );

            }


            texture.SetPixels(
                pixels
            );


            texture.Apply(
                false,
                false
            );


            return texture;

        }


        private void SetPixel(
            Color[] pixels,
            int width,
            int x,
            int y,
            Color color
        )
        {

            pixels[
                y *
                width +
                x
            ] =
                color;

        }

    }

}
