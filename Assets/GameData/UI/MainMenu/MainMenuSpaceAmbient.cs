
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Game.Inventory.UI;


namespace Game.UI.MainMenu
{

    public class MainMenuSpaceAmbient :
        MonoBehaviour
    {

        private sealed class Star
        {

            public SpriteRenderer Renderer;

            public float BaseAlpha;

            public float Speed;

            public float Phase;

        }


        [Header("Camera")]

        [SerializeField]
        private Camera menuCamera;


        [Header("Stars")]

        [SerializeField]
        [Range(20, 160)]
        private int starCount =
            72;


        [SerializeField]
        private float starDepth =
            28f;


        [Header("Portals")]

        [Tooltip("Optional: assign a VISUAL-ONLY copy of the player portal prefab (without gameplay scripts). If empty, a runtime ring portal is generated.")]
        [SerializeField]
        private GameObject portalPrefab;


        [SerializeField]
        private float portalMinInterval =
            7f;


        [SerializeField]
        private float portalMaxInterval =
            15f;


        [SerializeField]
        private Vector2 portalViewportMin =
            new Vector2(
                0.30f,
                0.20f
            );


        [SerializeField]
        private Vector2 portalViewportMax =
            new Vector2(
                0.92f,
                0.82f
            );


        [SerializeField]
        private float portalDepth =
            14f;


        [SerializeField]
        private string[] floatingBlockIds =
        {
            "game:stone",
            "game:dirt",
            "game:grass"
        };


        private readonly List<Star>
            stars =
            new List<Star>();


        private Sprite starSprite;

        private Sprite fallbackBlockSprite;

        private Material portalMaterial;


        private Coroutine portalLoop;


        private void Awake()
        {

            if (
                menuCamera ==
                null
            )
            {

                menuCamera =
                    Camera.main;

            }


            TryInitializeContent();


            CreateSharedVisuals();


            CreateStars();

        }


        private void OnEnable()
        {

            if (
                portalLoop ==
                null
            )
            {

                portalLoop =
                    StartCoroutine(
                        PortalLoop()
                    );

            }

        }


        private void OnDisable()
        {

            if (
                portalLoop !=
                null
            )
            {

                StopCoroutine(
                    portalLoop
                );


                portalLoop =
                    null;

            }

        }


        private void Update()
        {

            float time =
                Time.unscaledTime;


            for (
                int i = 0;
                i < stars.Count;
                i++
            )
            {

                Star star =
                    stars[i];


                if (
                    star ==
                    null
                    ||
                    star.Renderer ==
                    null
                )
                {

                    continue;

                }


                float pulse =
                    0.5f +
                    0.5f *
                    Mathf.Sin(
                        time *
                        star.Speed +
                        star.Phase
                    );


                Color color =
                    star.Renderer.color;


                color.a =
                    Mathf.Lerp(
                        star.BaseAlpha *
                        0.35f,
                        star.BaseAlpha,
                        pulse
                    );


                star.Renderer.color =
                    color;

            }

        }


        private IEnumerator PortalLoop()
        {

            while (
                true
            )
            {

                float delay =
                    UnityEngine.Random.Range(
                        portalMinInterval,
                        portalMaxInterval
                    );


                float elapsed =
                    0f;


                while (
                    elapsed <
                    delay
                )
                {

                    elapsed +=
                        Time.unscaledDeltaTime;


                    yield return null;

                }


                yield return
                    SpawnPortalEvent();

            }

        }


        private IEnumerator SpawnPortalEvent()
        {

            if (
                menuCamera ==
                null
            )
            {

                yield break;

            }


            float vx =
                UnityEngine.Random.Range(
                    portalViewportMin.x,
                    portalViewportMax.x
                );


            float vy =
                UnityEngine.Random.Range(
                    portalViewportMin.y,
                    portalViewportMax.y
                );


            Vector3 position =
                menuCamera.ViewportToWorldPoint(
                    new Vector3(
                        vx,
                        vy,
                        portalDepth
                    )
                );


            GameObject portal;


            LineRenderer line =
                null;


            if (
                portalPrefab !=
                null
            )
            {

                portal =
                    Instantiate(
                        portalPrefab,
                        position,
                        Quaternion.identity,
                        transform
                    );


                portal.name =
                    "MenuPortal_PlayerPrefab";


                // Menu ambience does not need gameplay collision.
                Collider2D[] colliders2D =
                    portal.GetComponentsInChildren<
                        Collider2D
                    >(
                        true
                    );


                for (
                    int i = 0;
                    i < colliders2D.Length;
                    i++
                )
                {

                    colliders2D[i].enabled =
                        false;

                }


                Rigidbody2D[] bodies2D =
                    portal.GetComponentsInChildren<
                        Rigidbody2D
                    >(
                        true
                    );


                for (
                    int i = 0;
                    i < bodies2D.Length;
                    i++
                )
                {

                    bodies2D[i].simulated =
                        false;

                }


                // Disable gameplay scripts on the copied portal so a
                // decorative main-menu portal cannot teleport anything.
                MonoBehaviour[] behaviours =
                    portal.GetComponentsInChildren<
                        MonoBehaviour
                    >(
                        true
                    );


                for (
                    int i = 0;
                    i < behaviours.Length;
                    i++
                )
                {

                    behaviours[i].enabled =
                        false;

                }

            }
            else
            {

                portal =
                    new GameObject(
                        "MenuPortal"
                    );


                portal.transform.SetParent(
                    transform,
                    true
                );


                portal.transform.position =
                    position;


                line =
                    portal.AddComponent<
                        LineRenderer
                    >();


                line.useWorldSpace =
                    false;


                line.loop =
                    true;


                line.positionCount =
                    40;


                line.startWidth =
                    0.07f;


                line.endWidth =
                    0.07f;


                line.sortingOrder =
                    -10;


                if (
                    portalMaterial !=
                    null
                )
                {

                    line.sharedMaterial =
                        portalMaterial;

                }


                Color portalColor =
                    new Color(
                        0.35f,
                        0.72f,
                        1f,
                        0f
                    );


                line.startColor =
                    portalColor;


                line.endColor =
                    portalColor;


                for (
                    int i = 0;
                    i < line.positionCount;
                    i++
                )
                {

                    float angle =
                        (
                            (float)i /
                            line.positionCount
                        )
                        *
                        Mathf.PI *
                        2f;


                    float wobble =
                        1f +
                        0.08f *
                        Mathf.Sin(
                            angle *
                            5f
                        );


                    line.SetPosition(
                        i,
                        new Vector3(
                            Mathf.Cos(
                                angle
                            )
                            *
                            0.75f *
                            wobble,

                            Mathf.Sin(
                                angle
                            )
                            *
                            1.05f *
                            wobble,

                            0f
                        )
                    );

                }

            }


            Vector3 originalScale =
                portal.transform.localScale;


            if (
                originalScale.sqrMagnitude <
                0.0001f
            )
            {

                originalScale =
                    Vector3.one;

            }


            portal.transform.localScale =
                Vector3.zero;


            float openTime =
                0.35f;


            float elapsed =
                0f;


            while (
                elapsed <
                openTime
            )
            {

                elapsed +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        elapsed /
                        openTime
                    );


                portal.transform.localScale =
                    originalScale *
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );


                if (
                    line !=
                    null
                )
                {

                    SetLineAlpha(
                        line,
                        t
                    );

                }


                portal.transform.Rotate(
                    0f,
                    0f,
                    35f *
                    Time.unscaledDeltaTime
                );


                yield return null;

            }


            // Emit 2-3 blocks into zero-gravity space.
            int count =
                UnityEngine.Random.Range(
                    2,
                    4
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {

                SpawnFloatingBlock(
                    position,
                    i
                );

            }


            float hold =
                0.65f;


            elapsed =
                0f;


            while (
                elapsed <
                hold
            )
            {

                elapsed +=
                    Time.unscaledDeltaTime;


                portal.transform.Rotate(
                    0f,
                    0f,
                    45f *
                    Time.unscaledDeltaTime
                );


                float pulse =
                    0.75f +
                    0.25f *
                    Mathf.Sin(
                        Time.unscaledTime *
                        8f
                    );


                portal.transform.localScale =
                    originalScale *
                    pulse;


                yield return null;

            }


            float closeTime =
                0.35f;


            elapsed =
                0f;


            while (
                elapsed <
                closeTime
            )
            {

                elapsed +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        elapsed /
                        closeTime
                    );


                portal.transform.localScale =
                    originalScale *
                    (
                        1f -
                        t
                    );


                if (
                    line !=
                    null
                )
                {

                    SetLineAlpha(
                        line,
                        1f -
                        t
                    );

                }


                yield return null;

            }


            Destroy(
                portal
            );

        }


        private void SpawnFloatingBlock(
            Vector3 position,
            int index
        )
        {

            GameObject gameObject =
                new GameObject(
                    "PortalBlock_" +
                    index
                );


            gameObject.transform.SetParent(
                transform,
                true
            );


            gameObject.transform.position =
                position +
                new Vector3(
                    UnityEngine.Random.Range(
                        -0.15f,
                        0.15f
                    ),
                    UnityEngine.Random.Range(
                        -0.2f,
                        0.2f
                    ),
                    -0.2f
                );


            SpriteRenderer renderer =
                gameObject.AddComponent<
                    SpriteRenderer
                >();


            renderer.sortingOrder =
                -5;


            renderer.sprite =
                GetRandomBlockSprite();


            gameObject.transform.localScale =
                Vector3.one *
                UnityEngine.Random.Range(
                    0.45f,
                    0.70f
                );


            MenuFloatingBlock floating =
                gameObject.AddComponent<
                    MenuFloatingBlock
                >();


            Vector2 direction =
                new Vector2(
                    UnityEngine.Random.Range(
                        -1f,
                        1f
                    ),
                    UnityEngine.Random.Range(
                        -0.35f,
                        0.35f
                    )
                );


            if (
                direction.sqrMagnitude <
                0.1f
            )
            {

                direction =
                    Vector2.right;

            }


            direction.Normalize();


            floating.Initialize(
                direction *
                UnityEngine.Random.Range(
                    0.9f,
                    1.8f
                ),
                UnityEngine.Random.Range(
                    -80f,
                    80f
                ),
                4f
            );

        }


        private Sprite GetRandomBlockSprite()
        {

            if (
                floatingBlockIds !=
                null
                &&
                floatingBlockIds.Length >
                0
            )
            {

                string id =
                    floatingBlockIds[
                        UnityEngine.Random.Range(
                            0,
                            floatingBlockIds.Length
                        )
                    ];


                try
                {

                    Sprite sprite =
                        ItemIconProvider.GetIcon(
                            id
                        );


                    if (
                        sprite !=
                        null
                    )
                    {

                        return sprite;

                    }

                }
                catch
                {
                }

            }


            return
                fallbackBlockSprite;

        }


        private void CreateStars()
        {

            if (
                menuCamera ==
                null
                ||
                starSprite ==
                null
            )
            {

                return;

            }


            for (
                int i = 0;
                i < starCount;
                i++
            )
            {

                GameObject starObject =
                    new GameObject(
                        "Star_" +
                        i
                    );


                starObject.transform.SetParent(
                    transform,
                    true
                );


                float vx =
                    UnityEngine.Random.Range(
                        0.02f,
                        0.98f
                    );


                float vy =
                    UnityEngine.Random.Range(
                        0.03f,
                        0.97f
                    );


                starObject.transform.position =
                    menuCamera.ViewportToWorldPoint(
                        new Vector3(
                            vx,
                            vy,
                            starDepth
                        )
                    );


                SpriteRenderer renderer =
                    starObject.AddComponent<
                        SpriteRenderer
                    >();


                renderer.sprite =
                    starSprite;


                renderer.sortingOrder =
                    -40;


                float scale =
                    UnityEngine.Random.Range(
                        0.03f,
                        0.09f
                    );


                starObject.transform.localScale =
                    Vector3.one *
                    scale;


                float alpha =
                    UnityEngine.Random.Range(
                        0.35f,
                        0.90f
                    );


                renderer.color =
                    new Color(
                        0.82f,
                        0.90f,
                        1f,
                        alpha
                    );


                stars.Add(
                    new Star
                    {
                        Renderer =
                            renderer,

                        BaseAlpha =
                            alpha,

                        Speed =
                            UnityEngine.Random.Range(
                                0.8f,
                                2.8f
                            ),

                        Phase =
                            UnityEngine.Random.Range(
                                0f,
                                Mathf.PI *
                                2f
                            )
                    }
                );

            }

        }


        private void CreateSharedVisuals()
        {

            starSprite =
                CreatePixelSprite(
                    "MenuStar",
                    new Color(
                        1f,
                        1f,
                        1f,
                        1f
                    )
                );


            fallbackBlockSprite =
                CreateFallbackBlockSprite();


            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );


            if (
                shader !=
                null
            )
            {

                portalMaterial =
                    new Material(
                        shader
                    );


                portalMaterial.name =
                    "MenuPortal_Runtime";

            }

        }


        private Sprite CreatePixelSprite(
            string name,
            Color color
        )
        {

            Texture2D texture =
                new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                name +
                "_Texture";


            texture.filterMode =
                FilterMode.Point;


            texture.SetPixels(
                new[]
                {
                    color,
                    color,
                    color,
                    color
                }
            );


            texture.Apply(
                false,
                false
            );


            return
                Sprite.Create(
                    texture,
                    new Rect(
                        0,
                        0,
                        2,
                        2
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    2f
                );

        }


        private Sprite CreateFallbackBlockSprite()
        {

            const int size =
                8;


            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );


            texture.filterMode =
                FilterMode.Point;


            for (
                int y = 0;
                y < size;
                y++
            )
            {

                for (
                    int x = 0;
                    x < size;
                    x++
                )
                {

                    bool checker =
                        (
                            x +
                            y
                        )
                        %
                        2 ==
                        0;


                    texture.SetPixel(
                        x,
                        y,
                        checker
                            ? new Color(
                                0.48f,
                                0.52f,
                                0.60f,
                                1f
                            )
                            : new Color(
                                0.34f,
                                0.38f,
                                0.46f,
                                1f
                            )
                    );

                }

            }


            texture.Apply(
                false,
                false
            );


            return
                Sprite.Create(
                    texture,
                    new Rect(
                        0,
                        0,
                        size,
                        size
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    size
                );

        }


        private void SetLineAlpha(
            LineRenderer line,
            float alpha
        )
        {

            Color color =
                new Color(
                    0.35f,
                    0.72f,
                    1f,
                    Mathf.Clamp01(
                        alpha
                    )
                );


            line.startColor =
                color;


            line.endColor =
                color;

        }


        private void TryInitializeContent()
        {

            string[] typeNames =
            {
                "Game.Content.ContentLoader",
                "Game.Content.ContentManager"
            };


            Assembly[] assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies();


            for (
                int t = 0;
                t < typeNames.Length;
                t++
            )
            {

                for (
                    int a = 0;
                    a < assemblies.Length;
                    a++
                )
                {

                    Type type =
                        assemblies[a].GetType(
                            typeNames[t],
                            false
                        );


                    if (
                        type ==
                        null
                    )
                    {

                        continue;

                    }


                    MethodInfo method =
                        type.GetMethod(
                            "Initialize",
                            BindingFlags.Public |
                            BindingFlags.Static
                        );


                    if (
                        method ==
                        null
                    )
                    {

                        continue;

                    }


                    try
                    {

                        method.Invoke(
                            null,
                            null
                        );


                        return;

                    }
                    catch
                    {
                    }

                }

            }

        }

    }


    public class MenuFloatingBlock :
        MonoBehaviour
    {

        private Vector2 velocity;

        private float rotationSpeed;

        private float lifetime;

        private float age;

        private SpriteRenderer renderer;


        public void Initialize(
            Vector2 velocity,
            float rotationSpeed,
            float lifetime
        )
        {

            this.velocity =
                velocity;


            this.rotationSpeed =
                rotationSpeed;


            this.lifetime =
                Mathf.Max(
                    0.1f,
                    lifetime
                );


            renderer =
                GetComponent<
                    SpriteRenderer
                >();

        }


        private void Update()
        {

            float delta =
                Time.unscaledDeltaTime;


            age +=
                delta;


            transform.position +=
                (
                    Vector3
                )
                velocity *
                delta;


            transform.Rotate(
                0f,
                0f,
                rotationSpeed *
                delta
            );


            float fadeStart =
                lifetime *
                0.60f;


            if (
                renderer !=
                null
                &&
                age >
                fadeStart
            )
            {

                float t =
                    Mathf.InverseLerp(
                        lifetime,
                        fadeStart,
                        age
                    );


                Color color =
                    renderer.color;


                color.a =
                    Mathf.Clamp01(
                        t
                    );


                renderer.color =
                    color;

            }


            if (
                age >=
                lifetime
            )
            {

                Destroy(
                    gameObject
                );

            }

        }

    }

}
