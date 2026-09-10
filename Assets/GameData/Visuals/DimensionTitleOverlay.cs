using UnityEngine;

using Game.World.Dimensions;

namespace Game.Visuals
{
    /// <summary>
    /// Dimension title overlay.
    ///
    /// IMPORTANT:
    /// A dimension change only QUEUES the title.
    /// It does not display immediately.
    ///
    /// Call:
    ///
    /// DimensionTitleOverlay.NotifyLoadingScreenFinished();
    ///
    /// only after the black loading/fade screen has completely
    /// disappeared. This guarantees that the title is shown
    /// after world generation, spawn correction and fade-out.
    /// </summary>
    public sealed class DimensionTitleOverlay :
        MonoBehaviour
    {
        private const float FadeInDuration =
            0.35f;

        private const float HoldDuration =
            1.20f;

        private const float FadeOutDuration =
            0.55f;


        private static DimensionTitleOverlay
            instance;


        private string lastDimensionName;

        private string pendingDimensionName;

        private string titleText;


        private float animationStart;

        private bool showing;


        private GUIStyle titleStyle;

        private GUIStyle shadowStyle;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            if (
                instance != null &&
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
        }


        private void Start()
        {
            DimensionDefinition current =
                DimensionTravelRuntime.Current;


            if (current != null)
            {
                lastDimensionName =
                    current.Name;
            }
        }


        private void Update()
        {
            CaptureDimensionChange();
        }


        // =====================================================
        // DIMENSION CHANGE
        // =====================================================

        private void CaptureDimensionChange()
        {
            DimensionDefinition current =
                DimensionTravelRuntime.Current;


            if (current == null)
                return;


            string currentName =
                current.Name;


            if (
                string.IsNullOrWhiteSpace(
                    currentName
                )
            )
            {
                return;
            }


            if (
                string.IsNullOrWhiteSpace(
                    lastDimensionName
                )
            )
            {
                lastDimensionName =
                    currentName;

                return;
            }


            if (
                string.Equals(
                    lastDimensionName,
                    currentName,
                    System.StringComparison.Ordinal
                )
            )
            {
                return;
            }


            lastDimensionName =
                currentName;


            // -------------------------------------------------
            // Queue only.
            //
            // Do NOT show while the screen is black.
            // -------------------------------------------------

            pendingDimensionName =
                currentName;


            showing =
                false;
        }


        // =====================================================
        // LOADING SCREEN HOOK
        // =====================================================

        public static void NotifyLoadingScreenFinished()
        {
            if (instance == null)
                return;


            // Covers the case where fade-out finishes in the
            // same frame as DimensionTravelRuntime.Current changed.
            instance.CaptureDimensionChange();


            instance.ShowPending();
        }


        private void ShowPending()
        {
            if (
                string.IsNullOrWhiteSpace(
                    pendingDimensionName
                )
            )
            {
                return;
            }


            string name =
                pendingDimensionName;


            pendingDimensionName =
                null;


            ShowInternal(
                name
            );
        }


        // =====================================================
        // MANUAL / DEBUG
        // =====================================================

        public static void Show(
            string dimensionName
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dimensionName
                )
            )
            {
                return;
            }


            if (instance != null)
            {
                instance.ShowInternal(
                    dimensionName
                );
            }
        }


        private void ShowInternal(
            string dimensionName
        )
        {
            titleText =
                dimensionName
                    .ToUpperInvariant();


            animationStart =
                Time.unscaledTime;


            showing =
                true;
        }


        // =====================================================
        // GUI
        // =====================================================

        private void OnGUI()
        {
            if (!showing)
                return;


            EnsureStyles();


            float elapsed =
                Time.unscaledTime -
                animationStart;


            float alpha =
                CalculateAlpha(
                    elapsed
                );


            if (alpha <= 0f)
            {
                if (
                    elapsed >
                    FadeInDuration +
                    HoldDuration +
                    FadeOutDuration
                )
                {
                    showing =
                        false;
                }


                return;
            }


            int fontSize =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        Screen.height *
                        0.048f
                    ),
                    26,
                    64
                );


            titleStyle.fontSize =
                fontSize;


            shadowStyle.fontSize =
                fontSize;


            Color main =
                Color.white;


            main.a =
                alpha;


            titleStyle.normal.textColor =
                main;


            Color shadow =
                Color.black;


            shadow.a =
                alpha *
                0.55f;


            shadowStyle.normal.textColor =
                shadow;


            float height =
                fontSize *
                1.8f;


            Rect rect =
                new Rect(
                    0f,
                    Screen.height *
                    0.075f,
                    Screen.width,
                    height
                );


            Rect shadowRect =
                rect;


            shadowRect.x +=
                2f;

            shadowRect.y +=
                2f;


            GUI.Label(
                shadowRect,
                titleText,
                shadowStyle
            );


            GUI.Label(
                rect,
                titleText,
                titleStyle
            );
        }


        private float CalculateAlpha(
            float elapsed
        )
        {
            if (elapsed < 0f)
                return 0f;


            if (
                elapsed <
                FadeInDuration
            )
            {
                return
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        elapsed /
                        FadeInDuration
                    );
            }


            elapsed -=
                FadeInDuration;


            if (
                elapsed <
                HoldDuration
            )
            {
                return 1f;
            }


            elapsed -=
                HoldDuration;


            if (
                elapsed <
                FadeOutDuration
            )
            {
                return
                    1f -
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        elapsed /
                        FadeOutDuration
                    );
            }


            return 0f;
        }


        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );


                titleStyle.alignment =
                    TextAnchor.UpperCenter;


                titleStyle.fontStyle =
                    FontStyle.Bold;


                titleStyle.wordWrap =
                    false;
            }


            if (shadowStyle == null)
            {
                shadowStyle =
                    new GUIStyle(
                        titleStyle
                    );
            }
        }
    }
}
