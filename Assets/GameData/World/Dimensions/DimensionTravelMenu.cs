using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Dimensions
{
    public class DimensionTravelMenu :
        MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private KeyCode menuKey =
            KeyCode.T;


        [Header("References")]
        [SerializeField]
        private DimensionPortalManager portalManager;

        [SerializeField]
        private Font interfaceFont;


        [Header("Colors")]
        [SerializeField]
        private Color backgroundColor =
            new Color(
                0.006f,
                0.008f,
                0.015f,
                0.985f
            );

        [SerializeField]
        private Color panelColor =
            new Color(
                0.024f,
                0.029f,
                0.043f,
                0.98f
            );

        [SerializeField]
        private Color cardColor =
            new Color(
                0.043f,
                0.050f,
                0.070f,
                0.98f
            );

        [SerializeField]
        private Color hoverColor =
            new Color(
                0.078f,
                0.090f,
                0.122f,
                1f
            );

        [SerializeField]
        private Color textColor =
            new Color(
                0.94f,
                0.95f,
                0.98f,
                1f
            );

        [SerializeField]
        private Color mutedTextColor =
            new Color(
                0.52f,
                0.57f,
                0.66f,
                1f
            );


        private bool open;

        private string newDimensionName =
            string.Empty;

        private Vector2 scroll;

        private Vector3 capturedPortalPosition;

        private bool hasCapturedPosition;


        private Texture2D backgroundTex;
        private Texture2D panelTex;
        private Texture2D cardTex;
        private Texture2D hoverTex;
        private Texture2D whiteTex;


        private GUIStyle titleStyle;
        private GUIStyle captionStyle;
        private GUIStyle sectionStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle smallStyle;
        private GUIStyle inputStyle;
        private GUIStyle buttonStyle;
        private GUIStyle typeStyle;


        private readonly List<Star> stars =
            new List<Star>();


        private struct Star
        {
            public Vector2 p;
            public float size;
            public float phase;
            public float speed;
        }


        private void Awake()
        {
            DimensionDatabase.Initialize();

            if (portalManager == null)
            {
                portalManager =
                    FindFirstObjectByType<
                        DimensionPortalManager
                    >();
            }

            CreateTextures();
            CreateStars();
        }


        private void Update()
        {
            if (!Input.GetKeyDown(menuKey))
                return;


            if (open)
            {
                open = false;
                return;
            }


            if (!CapturePortalPosition())
                return;


            open = true;
            //Cursor.visible = true;
        }


        private bool CapturePortalPosition()
        {
            if (portalManager == null)
            {
                portalManager =
                    FindFirstObjectByType<
                        DimensionPortalManager
                    >();
            }


            if (
                portalManager == null ||
                portalManager.WorldCamera == null
            )
            {
                return false;
            }


            Ray ray =
                portalManager.WorldCamera
                    .ScreenPointToRay(
                        Input.mousePosition
                    );


            Plane plane =
                new Plane(
                    Vector3.forward,
                    new Vector3(
                        0f,
                        0f,
                        portalManager.PortalWorldZ
                    )
                );


            if (
                !plane.Raycast(
                    ray,
                    out float distance
                )
            )
            {
                return false;
            }


            capturedPortalPosition =
                ray.GetPoint(
                    distance
                );

            capturedPortalPosition.z =
                portalManager.PortalWorldZ;

            hasCapturedPosition =
                true;

            return true;
        }


        private void OnGUI()
        {
            if (!open)
                return;


            EnsureStyles();


            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height
                ),
                backgroundTex
            );


            DrawStars();
            DrawHeader();
            DrawPanel();
        }


        private void DrawHeader()
        {
            DimensionDefinition current =
                DimensionTravelRuntime.Current;


            GUI.Label(
                new Rect(
                    0f,
                    22f,
                    Screen.width,
                    18f
                ),
                "Текущее измерение",
                captionStyle
            );


            GUI.Label(
                new Rect(
                    0f,
                    40f,
                    Screen.width,
                    48f
                ),
                current.Name,
                titleStyle
            );


            GUI.Label(
                new Rect(
                    0f,
                    87f,
                    Screen.width,
                    22f
                ),
                current.Type.DisplayName +
                "  •  СИД " +
                current.Seed,
                typeStyle
            );


            GUI.Label(
                new Rect(
                    Screen.width * 0.15f,
                    110f,
                    Screen.width * 0.70f,
                    20f
                ),
                BuildModifierLine(
                    current,
                    4
                ),
                captionStyle
            );


            DrawLine(
                new Rect(
                    Screen.width * 0.26f,
                    137f,
                    Screen.width * 0.48f,
                    1f
                )
            );
        }


        private void DrawPanel()
        {
            float width =
                Mathf.Min(
                    1000f,
                    Screen.width - 70f
                );

            float height =
                Mathf.Min(
                    640f,
                    Screen.height - 180f
                );


            Rect panel =
                new Rect(
                    (Screen.width - width) * 0.5f,
                    155f,
                    width,
                    height
                );


            GUI.DrawTexture(
                panel,
                panelTex
            );

            DrawBorder(
                panel,
                0.10f
            );


            float pad =
                28f;


            Rect left =
                new Rect(
                    panel.x + pad,
                    panel.y + pad,
                    panel.width * 0.56f -
                    pad * 1.5f,
                    panel.height -
                    pad * 2f
                );


            Rect right =
                new Rect(
                    panel.x +
                    panel.width * 0.58f,
                    panel.y + pad,
                    panel.width * 0.42f -
                    pad,
                    panel.height -
                    pad * 2f
                );


            DrawVisited(
                left
            );

            DrawCreate(
                right
            );
        }


        private void DrawVisited(
            Rect rect
        )
        {
            GUI.Label(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    24f
                ),
                "ПОСЕЩЕННЫЕ ИЗМЕРЕНИЯ",
                sectionStyle
            );


            Rect view =
                new Rect(
                    rect.x,
                    rect.y + 38f,
                    rect.width,
                    rect.height - 38f
                );


            IReadOnlyList<DimensionDefinition> visited =
                DimensionDatabase.Visited;


            const float itemHeight =
                108f;


            Rect content =
                new Rect(
                    0f,
                    0f,
                    view.width - 18f,
                    Mathf.Max(
                        view.height,
                        visited.Count *
                        (itemHeight + 10f)
                    )
                );


            scroll =
                GUI.BeginScrollView(
                    view,
                    scroll,
                    content,
                    false,
                    true
                );


            for (
                int i = 0;
                i < visited.Count;
                i++
            )
            {
                DimensionDefinition dimension =
                    visited[i];


                Rect card =
                    new Rect(
                        0f,
                        i *
                        (itemHeight + 10f),
                        content.width,
                        itemHeight
                    );


                bool hover =
                    card.Contains(
                        Event.current.mousePosition
                    );


                GUI.DrawTexture(
                    card,
                    hover
                        ? hoverTex
                        : cardTex
                );


                DrawBorder(
                    card,
                    hover
                        ? 0.30f
                        : 0.08f
                );


                GUI.Label(
                    new Rect(
                        card.x + 17f,
                        card.y + 10f,
                        card.width - 110f,
                        25f
                    ),
                    dimension.Name,
                    cardTitleStyle
                );


                GUI.Label(
                    new Rect(
                        card.x + 17f,
                        card.y + 37f,
                        card.width - 110f,
                        18f
                    ),
                    dimension.Type.DisplayName +
                    "  •  SEED " +
                    dimension.Seed,
                    smallStyle
                );


                GUI.Label(
                    new Rect(
                        card.x + 17f,
                        card.y + 62f,
                        card.width - 108f,
                        34f
                    ),
                    BuildModifierLine(
                        dimension,
                        3
                    ),
                    smallStyle
                );


                Rect openRect =
                    new Rect(
                        card.xMax - 82f,
                        card.y + 35f,
                        64f,
                        36f
                    );


                string name =
                    dimension.Name;


                DrawOutlineButton(
                    openRect,
                    "ОТКРЫТЬ",
                    () =>
                    {
                        OpenExistingRift(
                            name
                        );
                    }
                );
            }


            GUI.EndScrollView();
        }


        private void DrawCreate(
            Rect rect
        )
        {
            GUI.Label(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    24f
                ),
                "Новое измерение",
                sectionStyle
            );


            GUI.Label(
                new Rect(
                    rect.x,
                    rect.y + 42f,
                    rect.width,
                    44f
                ),
                "Имя определяет сид. Оставьте поле пустым, чтобы создать абсолютно случайное значение сида.",
                smallStyle
            );


            Rect inputBack =
                new Rect(
                    rect.x,
                    rect.y + 106f,
                    rect.width,
                    48f
                );


            GUI.DrawTexture(
                inputBack,
                cardTex
            );


            DrawBorder(
                inputBack,
                0.12f
            );


            newDimensionName =
                GUI.TextField(
                    new Rect(
                        inputBack.x + 14f,
                        inputBack.y + 6f,
                        inputBack.width - 28f,
                        inputBack.height - 12f
                    ),
                    newDimensionName,
                    48,
                    inputStyle
                );


            DimensionDefinition preview =
                DimensionDatabase.PreviewNamed(
                    newDimensionName
                );


            float y =
                rect.y + 174f;


            GUI.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    18f
                ),
                "СИД",
                captionStyle
            );


            GUI.Label(
                new Rect(
                    rect.x,
                    y + 20f,
                    rect.width,
                    27f
                ),
                preview == null
                    ? "СЛУЧАЙНО"
                    : preview.Seed.ToString(),
                cardTitleStyle
            );


            y +=
                62f;


            GUI.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    18f
                ),
                "ТИП",
                captionStyle
            );


            GUI.Label(
                new Rect(
                    rect.x,
                    y + 20f,
                    rect.width,
                    25f
                ),
                preview == null
                    ? "СЛУЧАЙНО"
                    : preview.Type.DisplayName,
                typeStyle
            );


            y +=
                62f;


            GUI.Label(
                new Rect(
                    rect.x,
                    y,
                    rect.width,
                    18f
                ),
                "Модификаторы измерений",
                captionStyle
            );


            GUI.Label(
                new Rect(
                    rect.x,
                    y + 23f,
                    rect.width,
                    74f
                ),
                preview == null
                    ? "3–5 случайных модификатора\nбудет выбран при создании разлома"
                    : BuildModifierMultiline(
                        preview
                    ),
                smallStyle
            );


            DrawFilledButton(
                new Rect(
                    rect.x,
                    rect.y + 402f,
                    rect.width,
                    46f
                ),
                preview == null
                    ? "СОЗДАТЬ СЛУЧАЙНЫЙ РАЗЛОМ"
                    : "СОЗДАТЬ РАЗЛОМ",
                CreateRiftFromInput
            );


            float bottom =
                rect.yMax - 90f;


            DrawLine(
                new Rect(
                    rect.x,
                    bottom,
                    rect.width,
                    1f
                )
            );


            GUI.Label(
                new Rect(
                    rect.x,
                    bottom + 14f,
                    rect.width,
                    20f
                ),
                hasCapturedPosition
                    ? "РИФТОВЫЙ ЯКОРЬ  " +
                      capturedPortalPosition.x.ToString("F1") +
                      " / " +
                      capturedPortalPosition.y.ToString("F1")
                    : "РИФТОВЫЙ ЯКОРЬ  —",
                smallStyle
            );


            DrawOutlineButton(
                new Rect(
                    rect.x,
                    bottom + 43f,
                    106f,
                    34f
                ),
                "CLOSE",
                () =>
                {
                    open = false;
                }
            );
        }


        private void CreateRiftFromInput()
        {
            if (!hasCapturedPosition)
                return;


            if (portalManager == null)
            {
                portalManager =
                    FindFirstObjectByType<
                        DimensionPortalManager
                    >();
            }


            if (portalManager == null)
                return;


            /*
             * НОВОЕ:
             *
             * Пустое имя не блокирует кнопку.
             * Database сама создаёт случайный seed и техническое имя.
             */
            DimensionDefinition dimension =
                DimensionDatabase.CreateFromInput(
                    newDimensionName
                );


            portalManager.OpenPortal(
                capturedPortalPosition,
                dimension.Name
            );


            newDimensionName =
                string.Empty;

            open =
                false;
        }


        private void OpenExistingRift(
            string dimensionName
        )
        {
            if (!hasCapturedPosition)
                return;


            if (portalManager == null)
            {
                portalManager =
                    FindFirstObjectByType<
                        DimensionPortalManager
                    >();
            }


            if (portalManager == null)
                return;


            DimensionDefinition dimension =
                DimensionDatabase.GetOrCreate(
                    dimensionName
                );


            portalManager.OpenPortal(
                capturedPortalPosition,
                dimension.Name
            );


            open =
                false;
        }


        private string BuildModifierLine(
            DimensionDefinition dimension,
            int maxCount
        )
        {
            if (
                dimension == null ||
                dimension.Modifiers == null ||
                dimension.Modifiers.Count == 0
            )
            {
                return "НЕТ МОДИФИКАТОРОВ";
            }


            int count =
                Mathf.Min(
                    maxCount,
                    dimension.Modifiers.Count
                );


            string result =
                string.Empty;


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                if (i > 0)
                    result += "  •  ";

                result +=
                    dimension.Modifiers[i]
                        .DisplayName;
            }


            if (
                dimension.Modifiers.Count >
                count
            )
            {
                result +=
                    "  +" +
                    (
                        dimension.Modifiers.Count -
                        count
                    );
            }


            return result;
        }


        private string BuildModifierMultiline(
            DimensionDefinition dimension
        )
        {
            string result =
                string.Empty;


            for (
                int i = 0;
                i < dimension.Modifiers.Count;
                i++
            )
            {
                if (i > 0)
                    result += "\n";

                result +=
                    "• " +
                    dimension.Modifiers[i]
                        .DisplayName;
            }


            return result;
        }


        private void DrawStars()
        {
            float time =
                Time.unscaledTime;


            for (
                int i = 0;
                i < stars.Count;
                i++
            )
            {
                Star s =
                    stars[i];


                float pulse =
                    0.35f +
                    0.65f *
                    (
                        Mathf.Sin(
                            s.phase +
                            time *
                            s.speed
                        )
                        *
                        0.5f +
                        0.5f
                    );


                Color old =
                    GUI.color;


                GUI.color =
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.07f +
                        pulse *
                        0.20f
                    );


                GUI.DrawTexture(
                    new Rect(
                        s.p.x *
                        Screen.width,
                        s.p.y *
                        Screen.height,
                        s.size,
                        s.size
                    ),
                    whiteTex
                );


                GUI.color =
                    old;
            }
        }


        private void DrawFilledButton(
            Rect rect,
            string text,
            Action action
        )
        {
            bool hover =
                rect.Contains(
                    Event.current.mousePosition
                );


            Color old =
                GUI.color;


            GUI.color =
                hover
                    ? new Color(
                        0.82f,
                        0.86f,
                        0.98f,
                        1f
                    )
                    : Color.white;


            GUI.DrawTexture(
                rect,
                whiteTex
            );


            GUI.color =
                old;


            Color previous =
                buttonStyle.normal.textColor;


            buttonStyle.normal.textColor =
                Color.black;


            if (
                GUI.Button(
                    rect,
                    text,
                    buttonStyle
                )
            )
            {
                action?.Invoke();
            }


            buttonStyle.normal.textColor =
                previous;
        }


        private void DrawOutlineButton(
            Rect rect,
            string text,
            Action action
        )
        {
            bool hover =
                rect.Contains(
                    Event.current.mousePosition
                );


            if (hover)
            {
                GUI.DrawTexture(
                    rect,
                    hoverTex
                );
            }


            DrawBorder(
                rect,
                hover
                    ? 0.40f
                    : 0.18f
            );


            if (
                GUI.Button(
                    rect,
                    text,
                    buttonStyle
                )
            )
            {
                action?.Invoke();
            }
        }


        private void DrawBorder(
            Rect rect,
            float alpha
        )
        {
            Color old =
                GUI.color;


            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    alpha
                );


            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    1f
                ),
                whiteTex
            );


            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.yMax - 1f,
                    rect.width,
                    1f
                ),
                whiteTex
            );


            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    1f,
                    rect.height
                ),
                whiteTex
            );


            GUI.DrawTexture(
                new Rect(
                    rect.xMax - 1f,
                    rect.y,
                    1f,
                    rect.height
                ),
                whiteTex
            );


            GUI.color =
                old;
        }


        private void DrawLine(
            Rect rect
        )
        {
            Color old =
                GUI.color;


            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.12f
                );


            GUI.DrawTexture(
                rect,
                whiteTex
            );


            GUI.color =
                old;
        }


        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;


            titleStyle =
                NewStyle(
                    34,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    textColor
                );


            captionStyle =
                NewStyle(
                    10,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter,
                    mutedTextColor
                );


            sectionStyle =
                NewStyle(
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    textColor
                );


            cardTitleStyle =
                NewStyle(
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    textColor
                );


            smallStyle =
                NewStyle(
                    10,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    mutedTextColor
                );

            smallStyle.wordWrap =
                true;


            inputStyle =
                NewStyle(
                    18,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    textColor
                );


            inputStyle.clipping =
                TextClipping.Clip;


            buttonStyle =
                NewStyle(
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    textColor
                );


            buttonStyle.normal.background =
                null;

            buttonStyle.hover.background =
                null;

            buttonStyle.active.background =
                null;


            typeStyle =
                NewStyle(
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Color(
                        0.74f,
                        0.78f,
                        0.90f,
                        1f
                    )
                );
        }


        private GUIStyle NewStyle(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor anchor,
            Color color
        )
        {
            GUIStyle style =
                new GUIStyle(
                    GUIStyle.none
                );


            style.font =
                interfaceFont;


            style.fontSize =
                fontSize;


            style.fontStyle =
                fontStyle;


            style.alignment =
                anchor;


            style.normal.textColor =
                color;


            style.hover.textColor =
                color;


            style.active.textColor =
                color;


            return style;
        }


        private void CreateTextures()
        {
            backgroundTex =
                CreateTexture(
                    backgroundColor
                );


            panelTex =
                CreateTexture(
                    panelColor
                );


            cardTex =
                CreateTexture(
                    cardColor
                );


            hoverTex =
                CreateTexture(
                    hoverColor
                );


            whiteTex =
                CreateTexture(
                    Color.white
                );
        }


        private Texture2D CreateTexture(
            Color color
        )
        {
            Texture2D texture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );


            texture.SetPixel(
                0,
                0,
                color
            );


            texture.Apply();


            return texture;
        }


        private void CreateStars()
        {
            System.Random random =
                new System.Random(
                    77231
                );


            stars.Clear();


            for (
                int i = 0;
                i < 72;
                i++
            )
            {
                stars.Add(
                    new Star
                    {
                        p =
                            new Vector2(
                                (float)random.NextDouble(),
                                (float)random.NextDouble()
                            ),

                        size =
                            Mathf.Lerp(
                                1f,
                                2.5f,
                                (float)random.NextDouble()
                            ),

                        phase =
                            Mathf.Lerp(
                                0f,
                                Mathf.PI * 2f,
                                (float)random.NextDouble()
                            ),

                        speed =
                            Mathf.Lerp(
                                0.25f,
                                0.75f,
                                (float)random.NextDouble()
                            )
                    }
                );
            }
        }
    }
}