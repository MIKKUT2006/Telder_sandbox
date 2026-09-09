using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif


namespace Game.World.Dimensions
{
    /*
     * DimensionRiftRendererFeature V5.1
     *
     * Поддерживает ОБА режима Unity 6 URP:
     *
     * 1. Render Graph включён
     *      -> RecordRenderGraph()
     *
     * 2. Compatibility Mode
     *    (Render Graph Disabled)
     *      -> Execute()
     *
     * Поэтому ошибка:
     *
     * "Execute is not implemented..."
     *
     * больше не возникает.
     */
    public class DimensionRiftRendererFeature :
        ScriptableRendererFeature
    {
        [Header("Shader")]

        [SerializeField]
        private Shader fullscreenShader;


        private Material material;

        private RiftPass pass;


        public override void Create()
        {
            if (fullscreenShader == null)
            {
                fullscreenShader =
                    Shader.Find(
                        "Hidden/Game/DimensionRiftFullscreen"
                    );
            }

            if (fullscreenShader == null)
            {
                Debug.LogError(
                    "DimensionRiftRendererFeature: " +
                    "shader Hidden/Game/DimensionRiftFullscreen not found."
                );

                return;
            }

            if (material != null)
            {
                CoreUtils.Destroy(
                    material
                );
            }

            material =
                CoreUtils.CreateEngineMaterial(
                    fullscreenShader
                );

            pass =
                new RiftPass(
                    material
                );
        }


        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData
        )
        {
            if (
                pass == null ||
                material == null
            )
            {
                return;
            }

            /*
             * Только Game Camera.
             *
             * Не запускаем эффект для Scene View,
             * Preview и прочих editor camera.
             */
            if (
                renderingData.cameraData.cameraType !=
                CameraType.Game
            )
            {
                return;
            }

            DimensionPortalManager manager =
                DimensionPortalManager.Instance;

            if (
                manager == null ||
                manager.CurrentPortal == null
            )
            {
                return;
            }

            /*
             * Сообщаем URP, что нам нужен camera color.
             *
             * В Compatibility Mode это также помогает URP
             * использовать intermediate color texture.
             */
            pass.ConfigureInput(
                ScriptableRenderPassInput.Color
            );

            renderer.EnqueuePass(
                pass
            );
        }


        protected override void Dispose(
            bool disposing
        )
        {
            if (pass != null)
            {
                pass.Dispose();
                pass = null;
            }

            if (material != null)
            {
                CoreUtils.Destroy(
                    material
                );

                material = null;
            }
        }


        private sealed class RiftPass :
            ScriptableRenderPass
        {
            private const string PassName =
                "Dimension Rift Fullscreen Distortion";


            private readonly Material material;


            /*
             * Используется только Compatibility Mode.
             *
             * Render Graph создаёт временную текстуру сам.
             */
            private RTHandle temporaryColor;


            public RiftPass(
                Material material
            )
            {
                this.material =
                    material;

                /*
                 * Нам нужен уже полностью отрисованный 2D-мир:
                 *
                 * Background SpriteRenderer
                 * Foreground SpriteRenderer
                 * Player
                 * particles
                 *
                 * Поэтому запускаемся ПОСЛЕ transparents.
                 */
                renderPassEvent =
                    RenderPassEvent.AfterRenderingTransparents;
            }


            // =====================================================
            // COMPATIBILITY MODE
            // =====================================================

            /*
             * Этот метод вызывается, когда:
             *
             * Project Settings
             * -> Graphics
             * -> Compatibility Mode
             *    (Render Graph Disabled)
             *
             * включён.
             */
            public override void Configure(
                CommandBuffer cmd,
                RenderTextureDescriptor cameraTextureDescriptor
            )
            {
                /*
                 * Временной texture depth не нужен.
                 */
                cameraTextureDescriptor.depthBufferBits =
                    0;

                /*
                 * Fullscreen post effect не должен иметь MSAA
                 * у своей промежуточной копии.
                 */
                cameraTextureDescriptor.msaaSamples =
                    1;

                RenderingUtils.ReAllocateIfNeeded(
                    ref temporaryColor,
                    cameraTextureDescriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name:
                    "_DimensionRiftCompatibilityColor"
                );
            }


            /*
             * ВАЖНО:
             *
             * В старой версии этот метод был пустым.
             * Именно поэтому Unity выводила:
             *
             * Execute is not implemented...
             */
            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData
            )
            {
                DimensionPortalManager manager =
                    DimensionPortalManager.Instance;

                if (
                    manager == null ||
                    manager.CurrentPortal == null ||
                    material == null ||
                    temporaryColor == null
                )
                {
                    return;
                }

                Camera camera =
                    renderingData.cameraData.camera;

                if (camera == null)
                {
                    return;
                }

                /*
                 * Передаём шейдеру:
                 *
                 * - позицию портала на экране
                 * - размер
                 * - состояние открытия
                 * - состояние схлопывания
                 * - частицы
                 */
                manager.CurrentPortal
                    .ApplyVisualToMaterial(
                        material,
                        camera
                    );


                RTHandle cameraColor =
                    renderingData
                        .cameraData
                        .renderer
                        .cameraColorTargetHandle;


                if (cameraColor == null)
                {
                    return;
                }


                CommandBuffer cmd =
                    CommandBufferPool.Get(
                        PassName
                    );


                /*
                 * 1.
                 * Берём уже полностью нарисованный camera color.
                 *
                 * 2.
                 * Прогоняем его через наш distortion shader
                 * во временную texture.
                 *
                 * ScriptableRenderPass.Blit использует URP Blitter,
                 * поэтому shader получает _BlitTexture.
                 */
                Blit(
                    cmd,
                    cameraColor,
                    temporaryColor,
                    material,
                    0
                );


                /*
                 * Возвращаем distorted результат обратно
                 * в camera color.
                 */
                Blit(
                    cmd,
                    temporaryColor,
                    cameraColor
                );


                context.ExecuteCommandBuffer(
                    cmd
                );

                CommandBufferPool.Release(
                    cmd
                );
            }


            // =====================================================
            // RENDER GRAPH
            // =====================================================

#if UNITY_6000_0_OR_NEWER

            /*
             * Этот метод используется в обычном
             * Unity 6 Render Graph режиме.
             */
            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData
            )
            {
                DimensionPortalManager manager =
                    DimensionPortalManager.Instance;

                if (
                    manager == null ||
                    manager.CurrentPortal == null ||
                    material == null
                )
                {
                    return;
                }


                UniversalResourceData resourceData =
                    frameData.Get<
                        UniversalResourceData
                    >();


                UniversalCameraData cameraData =
                    frameData.Get<
                        UniversalCameraData
                    >();


                /*
                 * Backbuffer нельзя читать как обычную texture.
                 */
                if (
                    resourceData.isActiveTargetBackBuffer
                )
                {
                    Debug.LogError(
                        "Dimension Rift: active target is BackBuffer. " +
                        "Open your Universal Renderer asset and set " +
                        "Intermediate Texture = Always."
                    );

                    return;
                }


                Camera camera =
                    cameraData.camera;

                if (camera == null)
                {
                    return;
                }


                manager.CurrentPortal
                    .ApplyVisualToMaterial(
                        material,
                        camera
                    );


                TextureHandle source =
                    resourceData.activeColorTexture;


                TextureDesc destinationDesc =
                    renderGraph.GetTextureDesc(
                        source
                    );


                destinationDesc.name =
                    "_DimensionRiftRenderGraphColor";

                destinationDesc.clearBuffer =
                    false;

                destinationDesc.depthBufferBits =
                    0;


                TextureHandle destination =
                    renderGraph.CreateTexture(
                        destinationDesc
                    );


                RenderGraphUtils.BlitMaterialParameters
                    parameters =
                    new RenderGraphUtils
                        .BlitMaterialParameters(
                            source,
                            destination,
                            material,
                            0
                        );


                renderGraph.AddBlitPass(
                    parameters,
                    PassName
                );


                /*
                 * Не делаем лишний copy назад.
                 *
                 * Говорим URP, что distorted texture
                 * теперь является camera color.
                 */
                resourceData.cameraColor =
                    destination;
            }

#endif


            public void Dispose()
            {
                if (temporaryColor != null)
                {
                    temporaryColor.Release();
                    temporaryColor = null;
                }
            }
        }
    }
}