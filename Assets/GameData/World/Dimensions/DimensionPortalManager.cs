using System.Collections;
using UnityEngine;

namespace Game.World.Dimensions
{
    public class DimensionPortalManager :
        MonoBehaviour
    {
        public static DimensionPortalManager Instance
        {
            get;
            private set;
        }


        [Header("Camera")]

        [Tooltip(
            "Игровая камера. Лучше назначить её явно. " +
            "Если пусто, будет использована Camera.main."
        )]
        [SerializeField]
        private Camera worldCamera;


        [Header("Portal")]

        [SerializeField]
        private Vector2 portalSize =
            new Vector2(
                3.0f,
                5.7f
            );

        [SerializeField]
        private float portalWorldZ =
            -1f;

        [SerializeField]
        private float openDuration =
            0.78f;

        [SerializeField]
        private float collapseDuration =
            1.20f;


        [Header("Dark Matter Colors")]

        [Tooltip(
            "Цвет самой сердцевины разлома. " +
            "По умолчанию абсолютный чёрный."
        )]
        [SerializeField]
        private Color coreColor =
            new Color(
                0.0f,
                0.0f,
                0.0f,
                1f
            );

        [Tooltip(
            "Цвет обводки / энергии вокруг разлома."
        )]
        [SerializeField]
        private Color rimColor =
            new Color(
                0.24f,
                0.015f,
                0.48f,
                1f
            );


        [Header("Dark Matter Effect")]

        [SerializeField]
        private float distortion =
            0.050f;

        [Tooltip(
            "Сила втягивания уже отрисованной сцены " +
            "к центру портала при его закрытии."
        )]
        [SerializeField]
        private float collapseSuction =
            0.18f;

        [SerializeField]
        private float rimGlow =
            0.95f;


        public Camera WorldCamera
        {
            get
            {
                if (worldCamera == null)
                {
                    worldCamera =
                        ResolveCamera();
                }

                return worldCamera;
            }
        }


        public float PortalWorldZ =>
            portalWorldZ;


        public DimensionPortal CurrentPortal
        {
            get;
            private set;
        }


        private Coroutine replaceRoutine;

        private bool hasPendingRequest;

        private Vector3 pendingPosition;

        private string pendingDimension;


        private void Awake()
        {
            if (
                Instance != null &&
                Instance != this
            )
            {
                Destroy(gameObject);
                return;
            }

            Instance =
                this;

            if (worldCamera == null)
            {
                worldCamera =
                    ResolveCamera();
            }
        }


        private Camera ResolveCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            Camera[] cameras =
                FindObjectsByType<Camera>(
                    FindObjectsSortMode.None
                );

            Camera best =
                null;

            float bestDepth =
                float.MinValue;

            for (
                int i = 0;
                i < cameras.Length;
                i++
            )
            {
                Camera camera =
                    cameras[i];

                if (
                    camera == null ||
                    !camera.enabled ||
                    !camera.gameObject.activeInHierarchy
                )
                {
                    continue;
                }

                if (camera.depth >= bestDepth)
                {
                    best =
                        camera;

                    bestDepth =
                        camera.depth;
                }
            }

            return best;
        }


        public void OpenPortal(
            Vector3 worldPosition,
            string targetDimension
        )
        {
            if (string.IsNullOrWhiteSpace(targetDimension))
            {
                return;
            }

            worldPosition.z =
                portalWorldZ;

            pendingPosition =
                worldPosition;

            pendingDimension =
                targetDimension.Trim();

            hasPendingRequest =
                true;

            if (replaceRoutine == null)
            {
                replaceRoutine =
                    StartCoroutine(
                        ProcessPortalRequests()
                    );
            }
        }


        private IEnumerator ProcessPortalRequests()
        {
            while (hasPendingRequest)
            {
                Vector3 requestedPosition =
                    pendingPosition;

                string requestedDimension =
                    pendingDimension;

                hasPendingRequest =
                    false;


                if (CurrentPortal != null)
                {
                    DimensionPortal oldPortal =
                        CurrentPortal;

                    yield return oldPortal.Collapse(
                        collapseDuration
                    );

                    if (CurrentPortal == oldPortal)
                    {
                        CurrentPortal =
                            null;
                    }
                }


                if (hasPendingRequest)
                {
                    requestedPosition =
                        pendingPosition;

                    requestedDimension =
                        pendingDimension;

                    hasPendingRequest =
                        false;
                }


                CurrentPortal =
                    DimensionPortal.Create(
                        requestedPosition,
                        requestedDimension,
                        portalSize,
                        coreColor,
                        rimColor,
                        distortion,
                        collapseSuction,
                        rimGlow
                    );

                yield return CurrentPortal.Open(
                    openDuration
                );
            }

            replaceRoutine =
                null;
        }


        public void NotifyDestroyed(
            DimensionPortal portal
        )
        {
            if (CurrentPortal == portal)
            {
                CurrentPortal =
                    null;
            }
        }
    }
}