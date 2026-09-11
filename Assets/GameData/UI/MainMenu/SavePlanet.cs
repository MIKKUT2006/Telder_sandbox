using System.IO;

using TMPro;
using UnityEngine;

using Game.Save;


namespace Game.UI.MainMenu
{
    public class SavePlanet :
        MonoBehaviour
    {
        private SaveSelectionController owner;

        private string saveId;
        private string displayName;

        private Vector3 normalScale;

        private bool hovered;


        private Renderer sphereRenderer;

        private TMP_Text label;


        private Material runtimeMaterial;

        private Texture2D previewTexture;


        [SerializeField]
        private float rotationSpeed =
            14f;


        [SerializeField]
        private float hoverScale =
            1.06f;


        [SerializeField]
        private float scaleSpeed =
            8f;


        public void Configure(
            SaveSelectionController owner,
            string saveId,
            string displayName,
            TMP_Text label,
            Material baseMaterial
        )
        {
            this.owner =
                owner;


            this.saveId = saveId;
            this.displayName = displayName;


            this.label =
                label;


            if (
                this.label != null
            )
            {
                this.label.text =
                    displayName;
            }


            sphereRenderer =
                GetComponent<Renderer>();


            normalScale =
                transform.localScale;


            CreateMaterial(
                baseMaterial
            );


            LoadPreview();
        }


        private void Update()
        {
            transform.Rotate(
                0f,
                rotationSpeed *
                Time.unscaledDeltaTime,
                0f,
                Space.Self
            );


            Vector3 targetScale =
                normalScale *
                (
                    hovered
                        ? hoverScale
                        : 1f
                );


            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    targetScale,
                    1f -
                    Mathf.Exp(
                        -scaleSpeed *
                        Time.unscaledDeltaTime
                    )
                );

            if (hovered && Input.GetMouseButtonDown(1))
            {
                SaveContextMenuController
                    .EnsureExists(owner)
                    .Open(saveId, displayName);
            }
        }


        private void OnMouseEnter()
        {
            hovered =
                true;
        }


        private void OnMouseExit()
        {
            hovered =
                false;
        }


        private void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
                owner?.LoadSave(saveId);
        }


        private void OnDestroy()
        {
            if (
                runtimeMaterial != null
            )
            {
                Destroy(
                    runtimeMaterial
                );
            }


            if (
                previewTexture != null
            )
            {
                Destroy(
                    previewTexture
                );
            }
        }


        private void CreateMaterial(
            Material baseMaterial
        )
        {
            if (
                sphereRenderer == null
            )
            {
                return;
            }


            if (
                baseMaterial != null
            )
            {
                runtimeMaterial =
                    new Material(
                        baseMaterial
                    );
            }
            else
            {
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
                    shader != null
                )
                {
                    runtimeMaterial =
                        new Material(
                            shader
                        );
                }
            }


            if (
                runtimeMaterial != null
            )
            {
                sphereRenderer.material =
                    runtimeMaterial;
            }
        }


        private void LoadPreview()
        {
            string path =
                SavePaths.GetPreviewFile(
                    saveId
                );


            if (
                !File.Exists(
                    path
                )
            )
            {
                return;
            }


            try
            {
                byte[] bytes =
                    File.ReadAllBytes(
                        path
                    );


                previewTexture =
                    new Texture2D(
                        2,
                        2,
                        TextureFormat.RGB24,
                        false
                    );


                if (
                    previewTexture.LoadImage(
                        bytes
                    )
                )
                {
                    previewTexture.wrapMode =
                        TextureWrapMode.Repeat;


                    if (
                        runtimeMaterial != null
                    )
                    {
                        runtimeMaterial.mainTexture =
                            previewTexture;
                    }
                }
            }
            catch
            {
                // Preview не должен мешать
                // загрузке самого сохранения.
            }
        }
    }
}
