using UnityEngine;
using UnityEngine.SceneManagement;


namespace Game.UI.MainMenu
{

    [ExecuteAlways]
    [RequireComponent(
        typeof(Camera)
    )]
    [DefaultExecutionOrder(-31000)]
    public class MainMenuGradientBackground :
        MonoBehaviour
    {

        // =====================================================
        // COLORS
        // =====================================================

        [Header("Gradient")]

        [SerializeField]
        private Color topColor =
            new Color(
                0.012f,
                0.035f,
                0.085f,
                1f
            );


        [SerializeField]
        private Color bottomColor =
            new Color(
                0.0005f,
                0.0015f,
                0.006f,
                1f
            );


        [SerializeField]
        [Range(16, 512)]
        private int resolution =
            256;


        // =====================================================
        // BACKGROUND
        // =====================================================

        [Header("Background")]

        [SerializeField]
        private float distance =
            50f;


        // Если true — компонент уничтожает свою камеру,
        // когда активная сцена перестаёт быть похожей на меню.
        // Отключи, если камеру меню переиспользует что-то ещё.
        [Header("Lifetime")]

        [SerializeField]
        private bool destroyOnLeavingMenu =
            true;


        // Дополнительные имена сцен, которые считаются "меню".
        // Проверка регистронезависимая и идёт по вхождению подстроки.
        [SerializeField]
        private string[] menuSceneMarkers =
            new string[]
            {
                "menu",
                "mainmenu",
                "main_menu"
            };


        private Camera targetCamera;


        private GameObject quadObject;


        private MeshRenderer quadRenderer;


        private Material material;


        private Texture2D texture;


        // Scene lifetime tracking.
        private bool sceneHookInstalled;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            InstallSceneHook();


            Initialize();

        }


        private void OnEnable()
        {

            InstallSceneHook();


            Initialize();

        }


        private void Start()
        {

            Initialize();

        }


        private void LateUpdate()
        {

            if (!enabled) return;   // ← защита от гонок

            ForceCamera();
            UpdateQuad();

        }


#if UNITY_EDITOR

        private void OnValidate()
        {

            if (
                !isActiveAndEnabled
            )
            {

                return;

            }


            Initialize();

            BuildTexture();

            UpdateQuad();

        }

#endif


        private void OnDisable()
        {

            RemoveSceneHook();

        }


        private void OnDestroy()
        {

            RemoveSceneHook();

            Cleanup();

        }


        // =====================================================
        // SCENE HOOK
        // =====================================================

        private void InstallSceneHook()
        {

            if (sceneHookInstalled)
            {

                return;

            }


            SceneManager.sceneLoaded +=
                OnSceneLoaded;


            sceneHookInstalled =
                true;

        }


        private void RemoveSceneHook()
        {

            if (!sceneHookInstalled)
            {

                return;

            }


            SceneManager.sceneLoaded -=
                OnSceneLoaded;


            sceneHookInstalled =
                false;

        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!destroyOnLeavingMenu)
                return;

            if (IsMenuLikeScene(scene.name))
            {
                // Вернулись в меню — включаемся и пересобираем градиент.
                enabled = true;
                Initialize();
                BuildTexture();
                UpdateQuad();
                return;
            }

            // Ушли из меню. Камеру НЕ трогаем — она общая для всех сцен.
            // Убираем только визуал меню и выключаем себя.
            DisableMenuVisuals();
        }

        private void DisableMenuVisuals()
        {
            if (quadObject != null)
            {
                if (Application.isPlaying)
                    Destroy(quadObject);
                else
                    DestroyImmediate(quadObject);

                quadObject = null;
            }

            quadRenderer = null;

            // Камеру возвращаем в нормальное состояние, чтобы игра
            // могла использовать свои clearFlags/background.
            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.Skybox;
                targetCamera.depth = 0f;
                // backgroundColor не трогаем — его выставит игра.
            }

            // Отключаем сам компонент. Камера остаётся живой,
            // просто перестаёт быть "фоном меню".
            enabled = false;
        }


        private bool IsMenuLikeScene(
            string sceneName
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    sceneName
                )
            )
            {

                return false;

            }


            string lower =
                sceneName.ToLowerInvariant();


            if (
                menuSceneMarkers == null ||
                menuSceneMarkers.Length == 0
            )
            {

                return lower.Contains("menu");

            }


            for (
                int i = 0;
                i < menuSceneMarkers.Length;
                i++
            )
            {

                string marker =
                    menuSceneMarkers[i];


                if (
                    string.IsNullOrWhiteSpace(
                        marker
                    )
                )
                {

                    continue;

                }


                if (
                    lower.Contains(
                        marker.ToLowerInvariant()
                    )
                )
                {

                    return true;

                }

            }


            return false;

        }


        // =====================================================
        // INITIALIZE
        // =====================================================

        private void Initialize()
        {

            if (
                targetCamera == null
            )
            {

                targetCamera =
                    GetComponent<
                        Camera
                    >();

            }


            if (
                targetCamera == null
            )
            {

                return;

            }


            ForceCamera();

            EnsureQuad();


            if (
                texture == null
            )
            {

                BuildTexture();

            }


            UpdateQuad();

        }


        // =====================================================
        // CAMERA
        // =====================================================

        private void ForceCamera()
        {

            if (
                targetCamera == null
            )
            {

                return;

            }

            targetCamera.enabled =
                true;


            targetCamera.clearFlags =
                CameraClearFlags.SolidColor;


            targetCamera.backgroundColor =
                bottomColor;


            targetCamera.depth =
                10000f;


            targetCamera.rect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );


            targetCamera.targetTexture =
                null;

        }


        // =====================================================
        // QUAD
        // =====================================================

        private void EnsureQuad()
        {

            if (
                quadObject != null
            )
            {

                return;

            }


            Transform existing =
                transform.Find(
                    "MenuGradientBackground"
                );


            if (
                existing != null
            )
            {

                quadObject =
                    existing.gameObject;


                quadRenderer =
                    quadObject
                        .GetComponent<
                            MeshRenderer
                        >();


                if (
                    quadRenderer != null
                )
                {

                    material =
                        quadRenderer
                            .sharedMaterial;

                }


                return;

            }


            quadObject =
                new GameObject(
                    "MenuGradientBackground"
                );


            quadObject.transform.SetParent(
                transform,
                false
            );


            MeshFilter filter =
                quadObject.AddComponent<
                    MeshFilter
                >();


            quadRenderer =
                quadObject.AddComponent<
                    MeshRenderer
                >();


            filter.sharedMesh =
                CreateQuad();


            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );


            if (
                shader == null
            )
            {

                shader =
                    Shader.Find(
                        "Unlit/Texture"
                    );

            }


            if (
                shader == null
            )
            {

                Debug.LogError(
                    "MAIN MENU: Unlit shader not found."
                );


                return;

            }


            material =
                new Material(
                    shader
                );


            material.name =
                "MainMenuGradient_Runtime";


            quadRenderer.sharedMaterial =
                material;

        }


        // =====================================================
        // TEXTURE
        // =====================================================

        private void BuildTexture()
        {

            if (
                texture != null
            )
            {

#if UNITY_EDITOR

                if (
                    !Application.isPlaying
                )
                {

                    DestroyImmediate(
                        texture
                    );

                }
                else

#endif

                {

                    Destroy(
                        texture
                    );

                }

            }


            int height =
                Mathf.Clamp(
                    resolution,
                    16,
                    512
                );


            texture =
                new Texture2D(
                    1,
                    height,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                "MainMenuGradient";


            texture.wrapMode =
                TextureWrapMode.Clamp;


            texture.filterMode =
                FilterMode.Bilinear;


            for (
                int y = 0;
                y < height;
                y++
            )
            {

                float t =
                    (float)y /
                    (
                        height -
                        1
                    );


                // Небольшая smoothstep-кривая:
                // низ дольше остаётся почти чёрным.
                t =
                    t *
                    t *
                    (
                        3f -
                        2f *
                        t
                    );


                texture.SetPixel(
                    0,
                    y,
                    Color.Lerp(
                        bottomColor,
                        topColor,
                        t
                    )
                );

            }


            texture.Apply(
                false,
                false
            );


            EnsureQuad();


            if (
                material ==
                null
                &&
                quadRenderer !=
                null
            )
            {

                material =
                    quadRenderer
                        .sharedMaterial;

            }


            if (
                material != null
            )
            {

                material.mainTexture =
                    texture;


                if (
                    material.HasProperty(
                        "_BaseMap"
                    )
                )
                {

                    material.SetTexture(
                        "_BaseMap",
                        texture
                    );

                }


                if (
                    material.HasProperty(
                        "_BaseColor"
                    )
                )
                {

                    material.SetColor(
                        "_BaseColor",
                        Color.white
                    );

                }

            }

        }


        // =====================================================
        // TRANSFORM
        // =====================================================

        private void UpdateQuad()
        {

            if (
                targetCamera == null
                ||
                quadObject == null
            )
            {

                return;

            }


            float z =
                Mathf.Max(
                    targetCamera.nearClipPlane +
                    1f,
                    distance
                );


            quadObject.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    z
                );


            quadObject.transform.localRotation =
                Quaternion.identity;


            float height;


            float width;


            if (
                targetCamera.orthographic
            )
            {

                height =
                    targetCamera
                        .orthographicSize *
                    2f;


                width =
                    height *
                    targetCamera.aspect;

            }
            else
            {

                height =
                    2f *
                    Mathf.Tan(
                        targetCamera.fieldOfView *
                        0.5f *
                        Mathf.Deg2Rad
                    ) *
                    z;


                width =
                    height *
                    targetCamera.aspect;

            }


            quadObject.transform.localScale =
                new Vector3(
                    width *
                    1.08f,

                    height *
                    1.08f,

                    1f
                );

        }


        // =====================================================
        // MESH
        // =====================================================

        private Mesh CreateQuad()
        {

            Mesh mesh =
                new Mesh();


            mesh.name =
                "MainMenuGradientQuad";


            mesh.vertices =
                new[]
                {
                    new Vector3(
                        -0.5f,
                        -0.5f,
                        0f
                    ),

                    new Vector3(
                        0.5f,
                        -0.5f,
                        0f
                    ),

                    new Vector3(
                        -0.5f,
                        0.5f,
                        0f
                    ),

                    new Vector3(
                        0.5f,
                        0.5f,
                        0f
                    )
                };


            mesh.uv =
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                };


            // Двусторонние треугольники.
            // Так quad гарантированно виден независимо
            // от culling конкретного Unlit shader.
            mesh.triangles =
                new[]
                {
                    0, 2, 1,
                    2, 3, 1,

                    1, 2, 0,
                    1, 3, 2
                };


            mesh.RecalculateBounds();


            return mesh;

        }


        // =====================================================
        // CLEANUP
        // =====================================================

        private void Cleanup()
        {

            if (
                texture != null
            )
            {

#if UNITY_EDITOR

                if (
                    !Application.isPlaying
                )
                {

                    DestroyImmediate(
                        texture
                    );

                }
                else

#endif

                {

                    Destroy(
                        texture
                    );

                }


                texture =
                    null;

            }


            if (
                material != null
            )
            {

#if UNITY_EDITOR

                if (
                    !Application.isPlaying
                )
                {

                    DestroyImmediate(
                        material
                    );

                }
                else

#endif

                {

                    Destroy(
                        material
                    );

                }


                material =
                    null;

            }

        }

    }

}