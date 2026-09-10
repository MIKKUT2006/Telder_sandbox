using UnityEngine;

using Game.Inventory;


namespace Game.World.Items
{

    public class PlayerItemCollector :
        MonoBehaviour
    {

        // =====================================================
        // REFERENCES
        // =====================================================

        [SerializeField]
        private PlayerInventory inventory;


        [SerializeField]
        private Transform pickupPoint;


        // =====================================================
        // SETTINGS
        // =====================================================

        [Header("Pickup")]

        [SerializeField]
        private float magnetRadius =
            1f;


        [SerializeField]
        private float pickupRadius =
            0.28f;


        [SerializeField]
        private float magnetSpeed =
            5f;


        [SerializeField]
        private float magnetAcceleration =
            35f;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            if (
                inventory == null
            )
            {

                inventory =
                    GetComponent<
                        PlayerInventory
                    >();

            }


            if (
                pickupPoint == null
            )
            {

                pickupPoint =
                    transform;

            }

        }


        private void FixedUpdate()
        {

            CollectAndAttractItems();

        }


        // =====================================================
        // COLLECT / MAGNET
        // =====================================================

        private void CollectAndAttractItems()
        {

            if (
                inventory == null
                ||
                pickupPoint == null
            )
            {

                return;

            }


            Vector2 target =
                pickupPoint.position;


            // Никакого Physics2D.OverlapCircle.
            // Никакого LayerMask.
            //
            // Это специально сделано так,
            // чтобы подбор не зависел от Collider2D
            // на DroppedItem.

            var items =
                DroppedItem.ActiveItems;


            for (
                int i =
                    items.Count - 1;

                i >= 0;

                i--
            )
            {

                DroppedItem item =
                    items[i];


                if (
                    item == null
                    ||
                    !item.isActiveAndEnabled
                    ||
                    !item.CanBePickedUp
                )
                {

                    continue;

                }


                Vector2 itemPosition =
                    item.transform.position;


                float sqrDistance =
                    (
                        itemPosition -
                        target
                    ).sqrMagnitude;


                float pickupRadiusSquared =
                    pickupRadius *
                    pickupRadius;


                // =================================================
                // PICKUP
                // =================================================

                if (
                    sqrDistance <=
                    pickupRadiusSquared
                )
                {

                    item.TryPickup(
                        inventory
                    );


                    continue;

                }


                // =================================================
                // MAGNET
                // =================================================

                float magnetRadiusSquared =
                    magnetRadius *
                    magnetRadius;


                if (
                    sqrDistance <=
                    magnetRadiusSquared
                )
                {

                    item.AttractTo(
                        target,
                        magnetSpeed,
                        magnetAcceleration
                    );

                }

            }

        }


#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {

            Transform point =
                pickupPoint != null
                    ? pickupPoint
                    : transform;


            Gizmos.DrawWireSphere(
                point.position,
                magnetRadius
            );


            Gizmos.DrawWireSphere(
                point.position,
                pickupRadius
            );

        }

#endif

    }

}
