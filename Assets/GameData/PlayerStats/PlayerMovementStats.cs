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
        private float accumulatedDistance;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            playerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            playerCollision = controller.GetPlayerCollision();
            previousX = transform.position.x;
        }

        private void Update()
        {
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
            accumulatedDistance = 0f;
        }
    }
}
