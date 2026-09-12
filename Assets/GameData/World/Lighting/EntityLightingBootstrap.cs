
using UnityEngine;

using Game.Inventory;
using Game.World.Items;


namespace Game.World.Lighting
{
    /// <summary>
    /// Automatically connects world lighting to:
    /// - Player sprites
    /// - Dropped item sprites
    ///
    /// No prefab/scene setup is required.
    /// </summary>
    [DefaultExecutionOrder(32400)]
    public class EntityLightingBootstrap :
        MonoBehaviour
    {
        private static EntityLightingBootstrap instance;


        private PlayerInventory playerInventory;


        private float nextPlayerSearchTime;


        // =====================================================
        // CREATE
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {
            if (
                instance !=
                null
            )
            {
                return;
            }


            GameObject gameObject =
                new GameObject(
                    "EntityLightingBootstrap"
                );


            instance =
                gameObject.AddComponent<
                    EntityLightingBootstrap
                >();


            DontDestroyOnLoad(
                gameObject
            );
        }


        private void Awake()
        {
            if (
                instance !=
                null
                &&
                instance !=
                this
            )
            {
                Destroy(
                    gameObject
                );


                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );
        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void LateUpdate()
        {
            AttachPlayerIfNeeded();

            AttachDroppedItemsIfNeeded();
        }


        // =====================================================
        // PLAYER
        // =====================================================

        private void AttachPlayerIfNeeded()
        {
            if (
                playerInventory ==
                null
            )
            {
                if (
                    Time.unscaledTime <
                    nextPlayerSearchTime
                )
                {
                    return;
                }


                nextPlayerSearchTime =
                    Time.unscaledTime +
                    0.5f;


                playerInventory =
                    Object.FindFirstObjectByType<
                        PlayerInventory
                    >();


                if (
                    playerInventory ==
                    null
                )
                {
                    return;
                }
            }


            WorldLightSpriteReceiver receiver =
                playerInventory
                    .GetComponent<
                        WorldLightSpriteReceiver
                    >();


            if (
                receiver ==
                null
            )
            {
                receiver =
                    playerInventory
                        .gameObject
                        .AddComponent<
                            WorldLightSpriteReceiver
                        >();


                // Player root in this project is positioned at the
                // collision/body center, so no vertical correction
                // is needed.
                receiver.Configure(
                    Vector2.zero
                );
            }
        }


        // =====================================================
        // DROPPED ITEMS
        // =====================================================

        private void AttachDroppedItemsIfNeeded()
        {
            var activeItems =
                DroppedItem.ActiveItems;


            if (
                activeItems ==
                null
            )
            {
                return;
            }


            for (
                int i = 0;
                i < activeItems.Count;
                i++
            )
            {
                DroppedItem item =
                    activeItems[i];


                if (
                    item ==
                    null
                    ||
                    !item.isActiveAndEnabled
                )
                {
                    continue;
                }


                WorldLightSpriteReceiver receiver =
                    item.GetComponent<
                        WorldLightSpriteReceiver
                    >();


                if (
                    receiver !=
                    null
                )
                {
                    continue;
                }


                receiver =
                    item.gameObject.AddComponent<
                        WorldLightSpriteReceiver
                    >();


                receiver.Configure(
                    Vector2.zero
                );
            }
        }
    }
}
