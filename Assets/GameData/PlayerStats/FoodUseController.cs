using System;
using UnityEngine;
using Game.Inventory;

namespace Game.PlayerStats
{
    [RequireComponent(typeof(PlayerStats))]
    public class FoodUseController : MonoBehaviour
    {
        // =====================================================
        // REFERENCES
        // =====================================================

        [Header("References")]

        [SerializeField]
        private PlayerInventory inventory;


        // =====================================================
        // SETTINGS
        // =====================================================

        [Header("Eating")]

        [Tooltip(
            "Used only if EatTime in the item's JSON is 0 or negative."
        )]
        [SerializeField]
        private float fallbackEatTime =
            1f;


        [Header("Debug")]

        [SerializeField]
        private bool debugLogs =
            false;


        // =====================================================
        // STATE
        // =====================================================

        private PlayerStats playerStats;

        private bool isEating;

        private float eatProgress;

        private string eatingItemId;


        public bool IsEating =>
            isEating;


        public float EatingProgress01
        {
            get;
            private set;
        }


        // =====================================================
        // EVENTS
        // =====================================================

        public event Action<string>
            EatingStarted;


        public event Action<float>
            EatingProgressChanged;


        public event Action
            EatingCanceled;


        public event Action<string>
            EatingCompleted;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            playerStats =
                GetComponent<PlayerStats>();


            if (inventory == null)
            {
                inventory =
                    GetComponent<PlayerInventory>();
            }
        }


        private void Start()
        {
            FoodMetadataRegistry
                .EnsureLoaded();


            if (inventory == null)
            {
                Debug.LogError(
                    "FOOD: PlayerInventory reference is missing."
                );
            }
        }


        private void Update()
        {
            if (inventory == null ||
                playerStats == null ||
                playerStats.IsDead)
            {
                CancelEating();

                return;
            }


            // Useful one-shot diagnostic when RMB is first pressed.
            if (debugLogs &&
                Input.GetMouseButtonDown(1))
            {
                DebugSelectedItem();
            }


            // Player MUST keep RMB held for the entire EatTime.
            if (!Input.GetMouseButton(1))
            {
                CancelEating();

                return;
            }


            string selectedItemId =
                inventory.GetSelectedItemId();


            if (string.IsNullOrWhiteSpace(
                selectedItemId
            ))
            {
                CancelEating();

                return;
            }


            // Do not use ItemDefinition.Tags/EatTime here.
            // In this project extra item metadata can be loaded separately
            // from the base ItemDefinition.
            if (!FoodMetadataRegistry.TryGet(
                selectedItemId,
                out FoodMetadataRegistry.FoodMetadata food
            ))
            {
                CancelEating();

                return;
            }


            if (!isEating ||
                !string.Equals(
                    eatingItemId,
                    selectedItemId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                BeginEating(
                    selectedItemId
                );
            }


            float eatTime =
                food.EatTime > 0f
                    ? food.EatTime
                    : Mathf.Max(
                        0.01f,
                        fallbackEatTime
                    );


            eatProgress +=
                Time.deltaTime;


            EatingProgress01 =
                Mathf.Clamp01(
                    eatProgress /
                    eatTime
                );


            EatingProgressChanged?.Invoke(
                EatingProgress01
            );


            if (eatProgress <
                eatTime)
            {
                return;
            }


            CompleteEating(
                food,
                selectedItemId
            );
        }


        // =====================================================
        // EATING
        // =====================================================

        private void BeginEating(
            string itemId)
        {
            isEating =
                true;

            eatProgress =
                0f;

            EatingProgress01 =
                0f;

            eatingItemId =
                itemId;


            if (debugLogs)
            {
                Debug.Log(
                    "FOOD: started eating " +
                    itemId
                );
            }


            EatingStarted?.Invoke(
                itemId
            );


            EatingProgressChanged?.Invoke(
                0f
            );
        }


        private void CompleteEating(
            FoodMetadataRegistry.FoodMetadata food,
            string itemId)
        {
            // Item is removed only after the FULL eating time completes.
            bool consumed =
                inventory.TryConsumeSelected(
                    1
                );


            if (!consumed)
            {
                if (debugLogs)
                {
                    Debug.LogWarning(
                        "FOOD: eating finished, but " +
                        "TryConsumeSelected(1) returned false."
                    );
                }

                CancelEating();

                return;
            }


            if (food.HungerRestore >
                0f)
            {
                playerStats.RestoreHunger(
                    food.HungerRestore
                );
            }


            if (food.HealthRestore >
                0f)
            {
                playerStats.Heal(
                    food.HealthRestore
                );
            }


            if (debugLogs)
            {
                Debug.Log(
                    "FOOD: ate " +
                    itemId +
                    " | Hunger +" +
                    food.HungerRestore +
                    " | Health +" +
                    food.HealthRestore
                );
            }


            EatingCompleted?.Invoke(
                itemId
            );


            ResetEatingState(
                invokeCanceledEvent: false
            );
        }


        private void CancelEating()
        {
            if (!isEating &&
                eatProgress <= 0f)
            {
                return;
            }


            if (debugLogs &&
                isEating)
            {
                Debug.Log(
                    "FOOD: eating canceled: " +
                    eatingItemId
                );
            }


            ResetEatingState(
                invokeCanceledEvent: true
            );
        }


        private void ResetEatingState(
            bool invokeCanceledEvent)
        {
            bool wasEating =
                isEating;


            isEating =
                false;

            eatProgress =
                0f;

            EatingProgress01 =
                0f;

            eatingItemId =
                null;


            if (invokeCanceledEvent &&
                wasEating)
            {
                EatingCanceled?.Invoke();
            }


            EatingProgressChanged?.Invoke(
                0f
            );
        }


        // =====================================================
        // DEBUG
        // =====================================================

        private void DebugSelectedItem()
        {
            string selectedItemId =
                inventory.GetSelectedItemId();


            Debug.Log(
                "FOOD DEBUG: selected item = " +
                (
                    string.IsNullOrWhiteSpace(
                        selectedItemId
                    )
                        ? "<EMPTY>"
                        : selectedItemId
                )
            );


            if (string.IsNullOrWhiteSpace(
                selectedItemId
            ))
            {
                return;
            }


            if (FoodMetadataRegistry.TryGet(
                selectedItemId,
                out FoodMetadataRegistry.FoodMetadata food
            ))
            {
                Debug.Log(
                    "FOOD DEBUG: metadata found. " +
                    "EatTime = " +
                    food.EatTime +
                    ", HungerRestore = " +
                    food.HungerRestore +
                    ", HealthRestore = " +
                    food.HealthRestore
                );
            }
            else
            {
                Debug.LogWarning(
                    "FOOD DEBUG: no food metadata found for " +
                    selectedItemId +
                    ". Check ID, Tags:[\"eat\"] and item JSON path."
                );
            }
        }
    }
}
