using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;


namespace Game.UI.MainMenu
{

    [DefaultExecutionOrder(-32000)]
    public class MainMenuSceneIsolation :
        MonoBehaviour
    {

        [SerializeField]
        private Camera menuCamera;


        [SerializeField]
        private bool logDisabledObjects =
            true;


        private Scene menuScene;


        private bool logged;


        private void Awake()
        {

            menuScene =
                gameObject.scene;


            if (
                menuCamera == null
            )
            {

                menuCamera =
                    GetComponent<Camera>();

            }


            Isolate();

        }


        private void OnEnable()
        {

            menuScene =
                gameObject.scene;


            Isolate();

        }


        private void Update()
        {

            // Некоторые DontDestroyOnLoad manager'ы
            // могут включить/создать свой фон уже после SceneLoaded.
            //
            // Поэтому изоляция поддерживается постоянно,
            // пока мы находимся в MainMenu.
            Isolate();

        }


        private void LateUpdate()
        {

            Isolate();

        }


        private void Isolate()
        {

            if (
                !menuScene.IsValid()
            )
            {

                menuScene =
                    gameObject.scene;

            }


            int camerasDisabled =
                0;


            int renderersDisabled =
                0;


            int canvasesDisabled =
                0;


            int volumesDisabled =
                0;


            int lightsDisabled =
                0;


            // =================================================
            // MAIN MENU CAMERA
            // =================================================

            if (
                menuCamera != null
            )
            {

                menuCamera.enabled =
                    true;


                menuCamera.targetTexture =
                    null;


                menuCamera.rect =
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f
                    );


                menuCamera.clearFlags =
                    CameraClearFlags.SolidColor;


                menuCamera.depth =
                    10000f;

            }


            // =================================================
            // CAMERAS FROM OTHER SCENES
            // =================================================

            Camera[] cameras =
                Object.FindObjectsByType<
                    Camera
                >(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < cameras.Length;
                i++
            )
            {

                Camera camera =
                    cameras[i];


                if (
                    camera == null
                    ||
                    camera ==
                    menuCamera
                )
                {

                    continue;

                }


                if (
                    !BelongsToMenuScene(
                        camera.gameObject
                    )
                )
                {

                    if (
                        camera.enabled
                    )
                    {

                        camera.enabled =
                            false;


                        camerasDisabled++;

                    }

                }

            }


            // =================================================
            // RENDERERS FROM OTHER SCENES
            // =================================================

            Renderer[] renderers =
                Object.FindObjectsByType<
                    Renderer
                >(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < renderers.Length;
                i++
            )
            {

                Renderer renderer =
                    renderers[i];


                if (
                    renderer == null
                )
                {

                    continue;

                }


                if (
                    !BelongsToMenuScene(
                        renderer.gameObject
                    )
                )
                {

                    if (
                        renderer.enabled
                    )
                    {

                        renderer.enabled =
                            false;


                        renderersDisabled++;

                    }

                }

            }


            // =================================================
            // CANVASES FROM OTHER SCENES
            // =================================================

            Canvas[] canvases =
                Object.FindObjectsByType<
                    Canvas
                >(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < canvases.Length;
                i++
            )
            {

                Canvas canvas =
                    canvases[i];


                if (
                    canvas == null
                )
                {

                    continue;

                }


                if (
                    !BelongsToMenuScene(
                        canvas.gameObject
                    )
                )
                {

                    if (
                        canvas.enabled
                    )
                    {

                        canvas.enabled =
                            false;


                        canvasesDisabled++;

                    }

                }

            }


            // =================================================
            // GLOBAL VOLUMES FROM OTHER SCENES
            // =================================================
            //
            // Бело-синий оттенок может идти не от фона,
            // а от persistent URP Volume / post-processing.
            //

            Volume[] volumes =
                Object.FindObjectsByType<
                    Volume
                >(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < volumes.Length;
                i++
            )
            {

                Volume volume =
                    volumes[i];


                if (
                    volume == null
                )
                {

                    continue;

                }


                if (
                    !BelongsToMenuScene(
                        volume.gameObject
                    )
                )
                {

                    if (
                        volume.enabled
                    )
                    {

                        volume.enabled =
                            false;


                        volumesDisabled++;

                    }

                }

            }


            // =================================================
            // LIGHTS FROM OTHER SCENES
            // =================================================

            Light[] lights =
                Object.FindObjectsByType<
                    Light
                >(
                    FindObjectsSortMode.None
                );


            for (
                int i = 0;
                i < lights.Length;
                i++
            )
            {

                Light light =
                    lights[i];


                if (
                    light == null
                )
                {

                    continue;

                }


                if (
                    !BelongsToMenuScene(
                        light.gameObject
                    )
                )
                {

                    if (
                        light.enabled
                    )
                    {

                        light.enabled =
                            false;


                        lightsDisabled++;

                    }

                }

            }


            // =================================================
            // RENDER SETTINGS
            // =================================================

            RenderSettings.skybox =
                null;


            RenderSettings.fog =
                false;


            RenderSettings.ambientMode =
                AmbientMode.Flat;


            RenderSettings.ambientLight =
                new Color(
                    0.004f,
                    0.008f,
                    0.02f,
                    1f
                );


            // =================================================
            // DEBUG ONCE
            // =================================================

            if (
                logDisabledObjects
                &&
                !logged
            )
            {

                logged =
                    true;


                Debug.Log(
                    "MAIN MENU ISOLATION: " +
                    "Cameras disabled = " +
                    camerasDisabled +
                    ", Renderers disabled = " +
                    renderersDisabled +
                    ", Canvases disabled = " +
                    canvasesDisabled +
                    ", Volumes disabled = " +
                    volumesDisabled +
                    ", Lights disabled = " +
                    lightsDisabled
                );

            }

        }


        private bool BelongsToMenuScene(
            GameObject target
        )
        {

            if (
                target == null
            )
            {

                return false;

            }


            Scene scene =
                target.scene;


            return
                scene.IsValid()
                &&
                scene ==
                menuScene;

        }

    }

}
