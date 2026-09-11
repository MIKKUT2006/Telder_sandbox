
using UnityEngine;


namespace Game.UI.Cursor
{

    [DefaultExecutionOrder(10000)]
    public class PixelCursorController :
        MonoBehaviour
    {

        private static PixelCursorController instance;


        [Header("Optional custom cursor textures")]

        [SerializeField]
        private Texture2D defaultCursor;


        [SerializeField]
        private Texture2D pointerCursor;


        [SerializeField]
        private Vector2 defaultHotspot =
            new Vector2(
                1f,
                1f
            );


        [SerializeField]
        private Vector2 pointerHotspot =
            new Vector2(
                1f,
                1f
            );


        private Texture2D generatedDefault;

        private Texture2D generatedPointer;


        private int pointerRequestedFrame =
            -100;


        private bool pointerApplied;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {

            if (
                Object.FindFirstObjectByType<
                    PixelCursorController
                >() != null
            )
            {

                return;

            }


            GameObject gameObject =
                new GameObject(
                    "PixelCursorSystem"
                );


            gameObject.AddComponent<
                PixelCursorController
            >();


            DontDestroyOnLoad(
                gameObject
            );

        }


        private void Awake()
        {

            if (
                instance != null
                &&
                instance != this
            )
            {

                Destroy(
                    gameObject
                );


                return;

            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );


            if (
                defaultCursor ==
                null
            )
            {

                generatedDefault =
                    CreateArrowCursor();


                defaultCursor =
                    generatedDefault;

            }


            if (
                pointerCursor ==
                null
            )
            {

                generatedPointer =
                    CreatePointerCursor();


                pointerCursor =
                    generatedPointer;

            }


            ApplyDefault();

        }


        private void LateUpdate()
        {

            bool wantsPointer =
                pointerRequestedFrame ==
                Time.frameCount;


            if (
                wantsPointer
                &&
                !pointerApplied
            )
            {

                ApplyPointer();

            }
            else if (
                !wantsPointer
                &&
                pointerApplied
            )
            {

                ApplyDefault();

            }

        }


        private void OnApplicationFocus(
            bool focus
        )
        {

            if (
                focus
            )
            {

                pointerApplied =
                    false;


                ApplyDefault();

            }

        }


        private void OnDestroy()
        {

            if (
                instance ==
                this
            )
            {

                instance =
                    null;

            }


            if (
                generatedDefault !=
                null
            )
            {

                Destroy(
                    generatedDefault
                );

            }


            if (
                generatedPointer !=
                null
            )
            {

                Destroy(
                    generatedPointer
                );

            }

        }


        public static void RequestPointer()
        {

            if (
                instance ==
                null
            )
            {

                return;

            }


            instance.pointerRequestedFrame =
                Time.frameCount;

        }


        private void ApplyDefault()
        {

            UnityEngine.Cursor.SetCursor(
                defaultCursor,
                defaultHotspot,
                CursorMode.Auto
            );


            pointerApplied =
                false;

        }


        private void ApplyPointer()
        {

            UnityEngine.Cursor.SetCursor(
                pointerCursor,
                pointerHotspot,
                CursorMode.Auto
            );


            pointerApplied =
                true;

        }


        // =====================================================
        // GENERATED PIXEL CURSORS
        // =====================================================

        private Texture2D CreateArrowCursor()
        {

            const int size =
                16;


            Texture2D texture =
                NewCursorTexture(
                    "GeneratedPixelArrow",
                    size
                );


            Color[] pixels =
                ClearPixels(
                    size
                );


            Color outline =
                new Color(
                    0.02f,
                    0.02f,
                    0.03f,
                    1f
                );


            Color fill =
                Color.white;


            int[,] shape =
            {
                {1,14},
                {1,13},{2,13},
                {1,12},{2,12},{3,12},
                {1,11},{2,11},{3,11},{4,11},
                {1,10},{2,10},{3,10},{4,10},{5,10},
                {1,9},{2,9},{3,9},{4,9},{5,9},{6,9},
                {1,8},{2,8},{3,8},{4,8},{5,8},{6,8},{7,8},
                {1,7},{2,7},{3,7},{4,7},{5,7},
                {1,6},{2,6},{3,6},
                {1,5},{2,5},
                {1,4},
                {4,6},{5,5},{5,4},{6,3},{7,3}
            };


            DrawOutlinedShape(
                pixels,
                size,
                shape,
                outline,
                fill
            );


            texture.SetPixels(
                pixels
            );


            texture.Apply(
                false,
                false
            );


            return texture;

        }


        private Texture2D CreatePointerCursor()
        {

            const int size =
                16;


            Texture2D texture =
                NewCursorTexture(
                    "GeneratedPixelPointer",
                    size
                );


            Color[] pixels =
                ClearPixels(
                    size
                );


            Color outline =
                new Color(
                    0.02f,
                    0.02f,
                    0.03f,
                    1f
                );


            Color fill =
                Color.white;


            int[,] shape =
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


            DrawOutlinedShape(
                pixels,
                size,
                shape,
                outline,
                fill
            );


            texture.SetPixels(
                pixels
            );


            texture.Apply(
                false,
                false
            );


            return texture;

        }


        private Texture2D NewCursorTexture(
            string name,
            int size
        )
        {

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                name;


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            return texture;

        }


        private Color[] ClearPixels(
            int size
        )
        {

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
                    Color.clear;

            }


            return pixels;

        }


        private void DrawOutlinedShape(
            Color[] pixels,
            int size,
            int[,] shape,
            Color outline,
            Color fill
        )
        {

            for (
                int i = 0;
                i < shape.GetLength(0);
                i++
            )
            {

                int x =
                    shape[i, 0];


                int y =
                    shape[i, 1];


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


            for (
                int i = 0;
                i < shape.GetLength(0);
                i++
            )
            {

                int x =
                    shape[i, 0];


                int y =
                    shape[i, 1];


                pixels[
                    y *
                    size +
                    x
                ] =
                    fill;

            }

        }

    }

}
