
using UnityEngine;


namespace Game.World.Furniture
{

    [DefaultExecutionOrder(32000)]
    public class PlacementModeIndicator :
        MonoBehaviour
    {

        private static PlacementModeIndicator instance;


        private string message;


        private float visibleUntil;


        private const float VisibleDuration =
            1.8f;


        private const float FadeDuration =
            0.25f;


        private const float ApproximateHotbarSlotSize =
            54f;


        private const int HotbarSlotCount =
            9;


        private GUIStyle textStyle;

        private GUIStyle boxStyle;

        private Texture2D boxTexture;


        public static void Show(
            string text
        )
        {

            EnsureInstance();


            if (
                instance ==
                null
            )
            {

                return;

            }


            instance.message =
                text;


            instance.visibleUntil =
                Time.unscaledTime +
                VisibleDuration;

        }


        private static void EnsureInstance()
        {

            if (
                instance !=
                null
            )
            {

                return;

            }


            GameObject gameObject =
                new GameObject(
                    "PlacementModeIndicator"
                );


            instance =
                gameObject.AddComponent<
                    PlacementModeIndicator
                >();


            DontDestroyOnLoad(
                gameObject
            );

        }


        private void Awake()
        {

            if (
                instance !=
                null
                &&
                instance !=
                this
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


            // Texture creation is legal outside OnGUI.
            // GUI.skin / GUIStyle(GUI.skin.*) is NOT.
            EnsureTexture();

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
                boxTexture !=
                null
            )
            {

                Destroy(
                    boxTexture
                );

            }

        }


        private void EnsureTexture()
        {

            if (
                boxTexture !=
                null
            )
            {

                return;

            }


            boxTexture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );


            boxTexture.name =
                "PlacementModeIndicator_Background";


            boxTexture.SetPixel(
                0,
                0,
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.90f
                )
            );


            boxTexture.Apply(
                false,
                false
            );

        }


        private void EnsureStylesInsideOnGUI()
        {

            if (
                boxStyle !=
                null
                &&
                textStyle !=
                null
            )
            {

                return;

            }


            EnsureTexture();


            // IMPORTANT:
            // GUI.skin can only be accessed during OnGUI.
            boxStyle =
                new GUIStyle(
                    GUI.skin.box
                );


            boxStyle.normal.background =
                boxTexture;


            boxStyle.padding =
                new RectOffset(
                    10,
                    10,
                    6,
                    6
                );


            textStyle =
                new GUIStyle(
                    GUI.skin.label
                );


            textStyle.alignment =
                TextAnchor.MiddleCenter;


            textStyle.fontSize =
                14;


            textStyle.fontStyle =
                FontStyle.Bold;


            textStyle.normal.textColor =
                Color.white;

        }


        private void OnGUI()
        {

            if (
                string.IsNullOrWhiteSpace(
                    message
                )
                ||
                Time.unscaledTime >
                visibleUntil
            )
            {

                return;

            }


            EnsureStylesInsideOnGUI();


            float remaining =
                visibleUntil -
                Time.unscaledTime;


            float alpha =
                remaining <
                FadeDuration
                    ? Mathf.Clamp01(
                        remaining /
                        FadeDuration
                    )
                    : 1f;


            Color oldColor =
                GUI.color;


            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    alpha
                );


            const float width =
                225f;


            const float height =
                42f;


            float hotbarWidth =
                ApproximateHotbarSlotSize *
                HotbarSlotCount;


            float x =
                Screen.width *
                0.5f
                -
                hotbarWidth *
                0.5f
                -
                width
                -
                14f;


            float y =
                Screen.height -
                74f;


            x =
                Mathf.Max(
                    8f,
                    x
                );


            Rect rect =
                new Rect(
                    x,
                    y,
                    width,
                    height
                );


            GUI.Box(
                rect,
                GUIContent.none,
                boxStyle
            );


            GUI.Label(
                rect,
                message,
                textStyle
            );


            GUI.color =
                oldColor;

        }

    }

}
