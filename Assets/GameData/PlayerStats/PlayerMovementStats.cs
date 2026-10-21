using UnityEngine;
using Game.World.Collision;

namespace Game.PlayerStats
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerMovementStats : MonoBehaviour
    {
        [Header("Walking hunger")]
        [SerializeField] private float blockDistance = 1f;

        [Tooltip("Horizontal movement larger than this in one frame is treated as teleport/repositioning.")]
        [SerializeField] private float teleportIgnoreDistance = 2.5f;

        private PlayerController controller;
        private PlayerCollision playerCollision;
        private PlayerStats playerStats;

        private float previousX;
        private Vector3 previousPosition3D;
        private bool use3D;
        private Game.World.Voxel3D.VoxelPlayerController3D controller3D;
        private float accumulatedDistance;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            playerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            use3D = Game.World.Dimensions.DimensionTravelRuntime.Current.Is3D;
            if (use3D)
            {
                controller3D = GetComponentInParent<Game.World.Voxel3D.VoxelPlayerController3D>();
                previousPosition3D = transform.position;
                return;
            }

            playerCollision = controller.GetPlayerCollision();
            previousX = transform.position.x;
        }

        private void Update()
        {
            if (use3D)
            {
                if (playerStats == null || playerStats.IsDead) return;
                if(controller3D==null)controller3D=GetComponentInParent<Game.World.Voxel3D.VoxelPlayerController3D>();
                Vector3 current = transform.position;
                Vector2 delta = new Vector2(current.x - previousPosition3D.x, current.z - previousPosition3D.z);
                previousPosition3D = current;
                float distance = delta.magnitude;
                if (distance > teleportIgnoreDistance) { accumulatedDistance = 0f; return; }
                if (controller3D != null && !controller3D.IsGrounded) return;
                accumulatedDistance += distance;
                float step3D = Mathf.Max(0.01f, blockDistance);
                while (accumulatedDistance >= step3D) { accumulatedDistance -= step3D; playerStats.NotifyWalkedBlock(); }
                return;
            }

            float currentX = transform.position.x;
            float deltaX = Mathf.Abs(currentX - previousX);
            previousX = currentX;

            if (playerCollision == null || playerStats == null || playerStats.IsDead)
                return;

            if (deltaX > teleportIgnoreDistance)
            {
                accumulatedDistance = 0f;
                return;
            }

            if (!playerCollision.IsGrounded)
                return;

            accumulatedDistance += deltaX;

            float step = Mathf.Max(0.01f, blockDistance);

            while (accumulatedDistance >= step)
            {
                accumulatedDistance -= step;
                playerStats.NotifyWalkedBlock();
            }
        }

        public void ResetDistanceTracking()
        {
            previousX = transform.position.x;
            previousPosition3D = transform.position;
            accumulatedDistance = 0f;
        }
    }
}
