using UnityEngine;
using Game.World.Collision;

// Runs after the normal player movement scripts.
// This is important because PlayerController uses custom PlayerCollision.Move().

namespace Game.PlayerStats
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerFallDamage : MonoBehaviour
    {
        // =====================================================
        // SETTINGS
        // =====================================================

        [Header("Fall Damage")]

        [Tooltip("Fall damage starts at this height in blocks.")]
        [SerializeField]
        private float minimumFallDistance = 6f;


        [Tooltip("Damage when falling exactly from the minimum height.")]
        [SerializeField]
        private float damageAtMinimumDistance = 5f;


        [Tooltip("Additional damage for every full block above the minimum.")]
        [SerializeField]
        private float damagePerAdditionalBlock = 5f;


        [Header("Teleport Protection")]

        [Tooltip(
            "If the player's Y position changes by more than this amount " +
            "in one frame, the movement is treated as teleport/spawn repositioning."
        )]
        [SerializeField]
        private float teleportIgnoreDistance = 8f;


        [Header("Debug")]

        [SerializeField]
        private bool debugLogs = false;


        // =====================================================
        // REFERENCES
        // =====================================================

        private PlayerController playerController;

        private PlayerCollision playerCollision;

        private PlayerStats playerStats;


        // =====================================================
        // FALL STATE
        // =====================================================

        private bool isTrackingFall;

        private float highestAirY;

        private float previousY;

        private bool initialized;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            playerController =
                GetComponent<PlayerController>();

            playerStats =
                GetComponent<PlayerStats>();
        }


        private void Start()
        {
            ResolveCollision();

            previousY =
                transform.position.y;

            highestAirY =
                previousY;

            initialized =
                true;
        }


        private void LateUpdate()
        {
            if (!initialized)
                return;


            if (playerStats == null ||
                playerStats.IsDead)
            {
                ResetTracking();

                return;
            }


            if (playerCollision == null)
            {
                ResolveCollision();

                if (playerCollision == null)
                    return;
            }


            float currentY =
                transform.position.y;


            // -------------------------------------------------
            // TELEPORT / SPAWN REPOSITION PROTECTION
            // -------------------------------------------------

            float frameVerticalMove =
                Mathf.Abs(
                    currentY -
                    previousY
                );


            if (frameVerticalMove >
                teleportIgnoreDistance)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        "FALL DAMAGE: ignored teleport/reposition. " +
                        "Y delta = " +
                        frameVerticalMove.ToString("0.00")
                    );
                }

                ResetTracking();

                previousY =
                    currentY;

                return;
            }


            bool grounded =
                playerCollision.IsGrounded;


            // -------------------------------------------------
            // PLAYER IS IN AIR
            // -------------------------------------------------

            if (!grounded)
            {
                if (!isTrackingFall)
                {
                    isTrackingFall =
                        true;

                    highestAirY =
                        currentY;


                    if (debugLogs)
                    {
                        Debug.Log(
                            "FALL DAMAGE: player became airborne at Y = " +
                            currentY.ToString("0.00")
                        );
                    }
                }
                else
                {
                    // Important for jumps:
                    // damage is measured from the apex, not from jump start.
                    if (currentY >
                        highestAirY)
                    {
                        highestAirY =
                            currentY;
                    }
                }
            }


            // -------------------------------------------------
            // PLAYER LANDED
            // -------------------------------------------------

            else if (isTrackingFall)
            {
                float fallDistance =
                    Mathf.Max(
                        0f,
                        highestAirY -
                        currentY
                    );


                if (debugLogs)
                {
                    Debug.Log(
                        "FALL DAMAGE: landed. Highest Y = " +
                        highestAirY.ToString("0.00") +
                        ", landing Y = " +
                        currentY.ToString("0.00") +
                        ", fall distance = " +
                        fallDistance.ToString("0.00")
                    );
                }


                ApplyFallDamage(
                    fallDistance
                );


                isTrackingFall =
                    false;

                highestAirY =
                    currentY;
            }


            previousY =
                currentY;
        }


        // =====================================================
        // DAMAGE
        // =====================================================

        private void ApplyFallDamage(
            float fallDistance)
        {
            // Small epsilon prevents e.g. 5.999999 from failing
            // a visually exact 6-block fall.
            const float epsilon =
                0.01f;


            if (fallDistance + epsilon <
                minimumFallDistance)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        "FALL DAMAGE: no damage. " +
                        "Distance = " +
                        fallDistance.ToString("0.00") +
                        ", minimum = " +
                        minimumFallDistance.ToString("0.00")
                    );
                }

                return;
            }


            float extraDistance =
                Mathf.Max(
                    0f,
                    fallDistance -
                    minimumFallDistance
                );


            int fullExtraBlocks =
                Mathf.FloorToInt(
                    extraDistance +
                    epsilon
                );


            float damage =
                damageAtMinimumDistance +
                fullExtraBlocks *
                damagePerAdditionalBlock;


            if (damage <= 0f)
                return;


            if (debugLogs)
            {
                Debug.Log(
                    "FALL DAMAGE: applying " +
                    damage.ToString("0.##") +
                    " damage for " +
                    fallDistance.ToString("0.00") +
                    " blocks."
                );
            }


            playerStats.TakeDamage(
                damage
            );
        }


        // =====================================================
        // REFERENCES
        // =====================================================

        private void ResolveCollision()
        {
            if (playerController == null)
            {
                playerController =
                    GetComponent<PlayerController>();
            }


            if (playerController != null)
            {
                playerCollision =
                    playerController
                        .GetPlayerCollision();
            }
        }


        // =====================================================
        // RESET API
        // =====================================================

        public void ResetFallTracking()
        {
            ResetTracking();

            previousY =
                transform.position.y;
        }


        private void ResetTracking()
        {
            isTrackingFall =
                false;

            highestAirY =
                transform.position.y;
        }
    }
}
