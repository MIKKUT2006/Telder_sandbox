using System.Collections.Generic;

using UnityEngine;

using Game.Inventory;
using Game.Inventory.UI;
using Game.World.Collision;


namespace Game.World.Items
{

    public class DroppedItem :
        MonoBehaviour
    {

        // =====================================================
        // ACTIVE ITEMS
        // =====================================================
        //
        // PlayerItemCollector больше не зависит от Collider2D
        // и LayerMask. Все активные дропы регистрируются здесь.
        //

        private static readonly List<DroppedItem>
            activeItems =
            new List<DroppedItem>();


        public static IReadOnlyList<DroppedItem>
            ActiveItems =>
            activeItems;


        // =====================================================
        // VISUAL
        // =====================================================

        [Header("Visual")]

        [SerializeField]
        private SpriteRenderer spriteRenderer;


        [SerializeField]
        [Range(0.1f, 1f)]
        private float itemScale =
            0.5f;


        [SerializeField]
        private int sortingOrder =
            100;


        // =====================================================
        // CUSTOM COLLISION
        // =====================================================

        [Header("World Collision")]

        [SerializeField]
        private Vector2 collisionSize =
            new Vector2(
                0.48f,
                0.48f
            );


        [SerializeField]
        private float skin =
            0.01f;


        [SerializeField]
        private float gravity =
            18f;


        [SerializeField]
        private float maxFallSpeed =
            12f;


        [SerializeField]
        private float groundFriction =
            8f;


        [SerializeField]
        private float movementStep =
            0.02f;


        // =====================================================
        // MAGNET
        // =====================================================

        private Vector2 magnetTarget;


        private float magnetSpeed;


        private float magnetAcceleration;


        private float magnetActiveUntil;


        // =====================================================
        // LEGACY UNITY PHYSICS
        // =====================================================

        private Rigidbody2D legacyBody;


        private Collider2D legacyCollider;


        // =====================================================
        // WORLD
        // =====================================================

        private WorldCollision worldCollision;


        // =====================================================
        // DATA
        // =====================================================

        private string itemId;


        private int count;


        private float pickupAllowedTime;


        // =====================================================
        // MOVEMENT
        // =====================================================

        private Vector2 velocity;


        private bool isGrounded;


        public string ItemId =>
            itemId;


        public int Count =>
            count;


        public bool IsGrounded =>
            isGrounded;


        public bool CanBePickedUp =>
            Time.time >=
            pickupAllowedTime;


        // =====================================================
        // UNITY
        // =====================================================

        private void OnEnable()
        {

            if (
                !activeItems.Contains(
                    this
                )
            )
            {

                activeItems.Add(
                    this
                );

            }

        }


        private void OnDisable()
        {

            activeItems.Remove(
                this
            );

        }


        private void Awake()
        {

            // =================================================
            // COMPONENTS
            // =================================================

            if (
                spriteRenderer == null
            )
            {

                spriteRenderer =
                    GetComponentInChildren<
                        SpriteRenderer
                    >();

            }


            legacyBody =
                GetComponent<
                    Rigidbody2D
                >();


            legacyCollider =
                GetComponent<
                    Collider2D
                >();


            // =================================================
            // UNITY PHYSICS OFF
            // =================================================
            //
            // Дроп использует кастомный WorldCollision.
            //

            if (
                legacyBody != null
            )
            {

                legacyBody.velocity =
                    Vector2.zero;


                legacyBody.angularVelocity =
                    0f;


                legacyBody.simulated =
                    false;

            }


            if (
                legacyCollider != null
            )
            {

                legacyCollider.enabled =
                    false;

            }


            // =================================================
            // VISUAL
            // =================================================

            transform.localScale =
                Vector3.one *
                itemScale;


            if (
                spriteRenderer != null
            )
            {

                spriteRenderer.sortingOrder =
                    sortingOrder;

            }


            InitializeWorldCollision();

        }


        private void FixedUpdate()
        {

            if (
                worldCollision == null
            )
            {

                InitializeWorldCollision();


                if (
                    worldCollision == null
                )
                {

                    return;

                }

            }


            float deltaTime =
                Time.fixedDeltaTime;


            bool magnetized =
                Time.time <=
                magnetActiveUntil;


            // =================================================
            // MAGNET
            // =================================================

            if (
                magnetized
            )
            {

                Vector2 toTarget =
                    magnetTarget -
                    (Vector2)transform.position;


                float distance =
                    toTarget.magnitude;


                if (
                    distance >
                    0.001f
                )
                {

                    Vector2 desiredVelocity =
                        toTarget /
                        distance *
                        magnetSpeed;


                    velocity =
                        Vector2.MoveTowards(
                            velocity,
                            desiredVelocity,
                            magnetAcceleration *
                            deltaTime
                        );

                }

            }
            else
            {

                // =============================================
                // NORMAL GRAVITY
                // =============================================

                velocity.y -=
                    gravity *
                    deltaTime;


                velocity.y =
                    Mathf.Max(
                        velocity.y,
                        -maxFallSpeed
                    );


                if (
                    isGrounded
                )
                {

                    velocity.x =
                        Mathf.MoveTowards(
                            velocity.x,
                            0f,
                            groundFriction *
                            deltaTime
                        );

                }

            }


            // =================================================
            // MOVE
            // =================================================

            MoveHorizontal(
                velocity.x *
                deltaTime
            );


            isGrounded =
                false;


            MoveVertical(
                velocity.y *
                deltaTime
            );


            CheckGround();

        }


        // =====================================================
        // INITIALIZE
        // =====================================================

        public void Initialize(
            string newItemId,
            int newCount,
            float pickupDelay
        )
        {

            itemId =
                newItemId;


            count =
                Mathf.Max(
                    1,
                    newCount
                );


            pickupAllowedTime =
                Time.time +
                pickupDelay;


            if (
                worldCollision == null
            )
            {

                InitializeWorldCollision();

            }


            if (
                spriteRenderer == null
            )
            {

                spriteRenderer =
                    GetComponentInChildren<
                        SpriteRenderer
                    >();

            }


            if (
                spriteRenderer != null
            )
            {

                spriteRenderer.sprite =
                    ItemIconProvider.GetIcon(
                        itemId
                    );


                spriteRenderer.sortingOrder =
                    sortingOrder;

            }


            transform.localScale =
                Vector3.one *
                itemScale;


            UpdateName();

        }


        // =====================================================
        // WORLD
        // =====================================================

        private void InitializeWorldCollision()
        {

            WorldManager manager =
                WorldManager.Instance;


            if (
                manager == null
            )
            {

                return;

            }


            World world =
                manager.GetWorld();


            if (
                world == null
            )
            {

                return;

            }


            worldCollision =
                new WorldCollision(
                    world
                );

        }


        // =====================================================
        // MAGNET API
        // =====================================================

        public void AttractTo(
            Vector2 target,
            float speed,
            float acceleration
        )
        {

            if (
                !CanBePickedUp
            )
            {

                return;

            }


            magnetTarget =
                target;


            magnetSpeed =
                Mathf.Max(
                    0f,
                    speed
                );


            magnetAcceleration =
                Mathf.Max(
                    0f,
                    acceleration
                );


            // Collector обновляет это каждый FixedUpdate.
            // Небольшой запас не даёт магниту мигать между тиками.
            magnetActiveUntil =
                Time.time +
                0.08f;

        }


        // =====================================================
        // SET VELOCITY
        // =====================================================

        public void SetVelocity(
            Vector2 newVelocity
        )
        {

            velocity =
                newVelocity;

        }


        // =====================================================
        // MOVEMENT
        // =====================================================

        private void MoveHorizontal(
            float movement
        )
        {

            if (
                Mathf.Abs(
                    movement
                ) <=
                0.0001f
            )
            {

                return;

            }


            float direction =
                Mathf.Sign(
                    movement
                );


            float distance =
                Mathf.Abs(
                    movement
                );


            float moved =
                0f;


            float step =
                Mathf.Max(
                    0.005f,
                    movementStep
                );


            while (
                moved <
                distance
            )
            {

                float currentStep =
                    Mathf.Min(
                        step,
                        distance -
                        moved
                    );


                float testX =
                    transform.position.x +
                    direction *
                    currentStep;


                if (
                    CheckCollisionAt(
                        testX,
                        transform.position.y
                    )
                )
                {

                    velocity.x =
                        0f;


                    break;

                }


                transform.position =
                    new Vector3(
                        testX,
                        transform.position.y,
                        transform.position.z
                    );


                moved +=
                    currentStep;

            }

        }


        private void MoveVertical(
            float movement
        )
        {

            if (
                Mathf.Abs(
                    movement
                ) <=
                0.0001f
            )
            {

                return;

            }


            float direction =
                Mathf.Sign(
                    movement
                );


            float distance =
                Mathf.Abs(
                    movement
                );


            float moved =
                0f;


            float step =
                Mathf.Max(
                    0.005f,
                    movementStep
                );


            while (
                moved <
                distance
            )
            {

                float currentStep =
                    Mathf.Min(
                        step,
                        distance -
                        moved
                    );


                float testY =
                    transform.position.y +
                    direction *
                    currentStep;


                if (
                    CheckCollisionAt(
                        transform.position.x,
                        testY
                    )
                )
                {

                    velocity.y =
                        0f;


                    if (
                        direction <
                        0f
                    )
                    {

                        isGrounded =
                            true;

                    }


                    break;

                }


                transform.position =
                    new Vector3(
                        transform.position.x,
                        testY,
                        transform.position.z
                    );


                moved +=
                    currentStep;

            }

        }


        // =====================================================
        // COLLISION
        // =====================================================

        private bool CheckCollisionAt(
            float centerX,
            float centerY
        )
        {

            if (
                worldCollision == null
            )
            {

                return false;

            }


            float halfWidth =
                collisionSize.x *
                0.5f;


            float halfHeight =
                collisionSize.y *
                0.5f;


            float minX =
                centerX -
                halfWidth +
                skin;


            float maxX =
                centerX +
                halfWidth -
                skin;


            float minY =
                centerY -
                halfHeight +
                skin;


            float maxY =
                centerY +
                halfHeight -
                skin;


            int minBlockX =
                Mathf.FloorToInt(
                    minX
                );


            int maxBlockX =
                Mathf.FloorToInt(
                    maxX
                );


            int minBlockY =
                Mathf.FloorToInt(
                    minY
                );


            int maxBlockY =
                Mathf.FloorToInt(
                    maxY
                );


            for (
                int x = minBlockX;
                x <= maxBlockX;
                x++
            )
            {

                for (
                    int y = minBlockY;
                    y <= maxBlockY;
                    y++
                )
                {

                    if (
                        worldCollision.IsSolid(
                            x,
                            y
                        )
                    )
                    {

                        return true;

                    }

                }

            }


            return false;

        }


        private void CheckGround()
        {

            if (
                worldCollision == null
            )
            {

                isGrounded =
                    false;


                return;

            }


            float halfWidth =
                collisionSize.x *
                0.5f;


            float bottom =
                transform.position.y -
                collisionSize.y *
                0.5f;


            float checkY =
                bottom -
                skin *
                2f;


            float minX =
                transform.position.x -
                halfWidth +
                skin;


            float maxX =
                transform.position.x +
                halfWidth -
                skin;


            int minBlockX =
                Mathf.FloorToInt(
                    minX
                );


            int maxBlockX =
                Mathf.FloorToInt(
                    maxX
                );


            int blockY =
                Mathf.FloorToInt(
                    checkY
                );


            isGrounded =
                false;


            for (
                int x = minBlockX;
                x <= maxBlockX;
                x++
            )
            {

                if (
                    worldCollision.IsSolid(
                        x,
                        blockY
                    )
                )
                {

                    isGrounded =
                        true;


                    return;

                }

            }

        }


        // =====================================================
        // PICKUP
        // =====================================================

        public bool TryPickup(
            PlayerInventory inventory
        )
        {

            if (
                inventory == null
                ||
                !CanBePickedUp
            )
            {

                return false;

            }


            int oldCount =
                count;


            int remaining =
                inventory.AddItem(
                    itemId,
                    count
                );


            // Инвентарь заполнен.
            if (
                remaining ==
                oldCount
            )
            {

                return false;

            }


            count =
                remaining;


            if (
                count <= 0
            )
            {

                Destroy(
                    gameObject
                );


                return true;

            }


            UpdateName();


            return true;

        }


        // =====================================================
        // NAME
        // =====================================================

        private void UpdateName()
        {

            name =
                "DroppedItem [" +
                itemId +
                "] x" +
                count;

        }


#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {

            Gizmos.DrawWireCube(
                transform.position,
                collisionSize
            );

        }

#endif

    }

}
