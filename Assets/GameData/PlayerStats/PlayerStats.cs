using System;
using UnityEngine;

namespace Game.PlayerStats
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Maximum values")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxHunger = 100f;

        [Header("Start values")]
        [SerializeField] private float startHealth = 100f;
        [SerializeField] private float startHunger = 100f;

        [Header("Passive hunger")]
        [SerializeField] private float passiveHungerInterval = 15f;
        [SerializeField] private float passiveHungerCost = 0.005f;

        [Header("Actions")]
        [SerializeField] private float hungerPerBrokenBlock = 0.05f;
        [SerializeField] private float hungerPerWalkedBlock = 0.05f;

        [Header("Starvation")]
        [SerializeField] private float starvationDamage = 5f;
        [SerializeField] private float starvationDamageInterval = 5f;

        [Header("Regeneration")]
        [SerializeField]
        private float regenerationHungerThreshold = 70f;

        [SerializeField]
        private float regenerationHealthAmount = 10f;

        [SerializeField]
        private float regenerationInterval = 7f;

        [SerializeField]
        private float hungerCostPer10RegeneratedHealth = 5f;

        private float health;
        private float hunger;

        private float passiveHungerTimer;
        private float starvationTimer;
        private float regenerationTimer;

        public float Health => health;
        public float Hunger => hunger;
        public float MaxHealth => maxHealth;
        public float MaxHunger => maxHunger;

        public float HealthNormalized =>
            maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);

        public float HungerNormalized =>
            maxHunger <= 0f ? 0f : Mathf.Clamp01(hunger / maxHunger);

        public bool IsDead => health <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action<float, float> HungerChanged;
        public event Action<float> Damaged;
        public event Action<float> Healed;
        public event Action Died;

        private void Awake()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            maxHunger = Mathf.Max(1f, maxHunger);

            health = Mathf.Clamp(startHealth, 0f, maxHealth);
            hunger = Mathf.Clamp(startHunger, 0f, maxHunger);
        }

        private void Start()
        {
            RaiseAllChanged();
        }

        private void Update()
        {
            if (IsDead)
                return;

            UpdatePassiveHunger();
            UpdateStarvation();
            UpdateRegeneration();
        }

        private void UpdatePassiveHunger()
        {
            if (passiveHungerInterval <= 0f)
                return;

            passiveHungerTimer += Time.deltaTime;

            while (passiveHungerTimer >= passiveHungerInterval)
            {
                passiveHungerTimer -= passiveHungerInterval;
                SpendHunger(passiveHungerCost);
            }
        }

        private void UpdateStarvation()
        {
            if (hunger > 0f)
            {
                starvationTimer = 0f;
                return;
            }

            if (starvationDamageInterval <= 0f)
                return;

            starvationTimer += Time.deltaTime;

            while (starvationTimer >= starvationDamageInterval)
            {
                starvationTimer -= starvationDamageInterval;
                TakeDamage(starvationDamage);

                if (IsDead)
                    break;
            }
        }

        private void UpdateRegeneration()
        {
            if (hunger <= regenerationHungerThreshold ||
                health >= maxHealth)
            {
                regenerationTimer = 0f;
                return;
            }

            if (regenerationInterval <= 0f)
                return;

            regenerationTimer += Time.deltaTime;

            while (regenerationTimer >= regenerationInterval)
            {
                regenerationTimer -= regenerationInterval;

                float healthBefore =
                    health;

                Heal(
                    regenerationHealthAmount
                );

                float actuallyHealed =
                    health -
                    healthBefore;


                if (actuallyHealed > 0f)
                {
                    // 10 HP = 5 hunger by default.
                    float hungerCost =
                        actuallyHealed /
                        10f *
                        hungerCostPer10RegeneratedHealth;

                    SpendHunger(
                        hungerCost
                    );
                }


                if (health >= maxHealth)
                {
                    regenerationTimer = 0f;
                    break;
                }


                // Regeneration must immediately stop
                // if its own hunger cost pushed hunger
                // down to the regeneration threshold.
                if (hunger <= regenerationHungerThreshold)
                {
                    regenerationTimer = 0f;
                    break;
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            float oldHealth = health;
            health = Mathf.Clamp(health - amount, 0f, maxHealth);

            float realDamage = oldHealth - health;

            if (realDamage <= 0f)
                return;

            HealthChanged?.Invoke(health, maxHealth);
            Damaged?.Invoke(realDamage);

            if (health <= 0f)
                Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            float oldHealth = health;
            health = Mathf.Clamp(health + amount, 0f, maxHealth);

            float realHeal = health - oldHealth;

            if (realHeal <= 0f)
                return;

            HealthChanged?.Invoke(health, maxHealth);
            Healed?.Invoke(realHeal);
        }

        public void SpendHunger(float amount)
        {
            if (amount <= 0f)
                return;

            SetHunger(hunger - amount);
        }

        public void RestoreHunger(float amount)
        {
            if (amount <= 0f)
                return;

            SetHunger(hunger + amount);
        }

        public void NotifyBlockBroken()
        {
            SpendHunger(hungerPerBrokenBlock);
        }

        public void NotifyWalkedBlock()
        {
            SpendHunger(hungerPerWalkedBlock);
        }

        public void SetHealth(float value)
        {
            float newValue = Mathf.Clamp(value, 0f, maxHealth);

            if (Mathf.Approximately(newValue, health))
                return;

            bool wasAlive = health > 0f;
            health = newValue;
            HealthChanged?.Invoke(health, maxHealth);

            if (wasAlive && health <= 0f)
                Died?.Invoke();
        }

        public void SetHunger(float value)
        {
            float newValue = Mathf.Clamp(value, 0f, maxHunger);

            if (Mathf.Approximately(newValue, hunger))
                return;

            hunger = newValue;
            HungerChanged?.Invoke(hunger, maxHunger);
        }

        public void RestoreState(float savedHealth, float savedHunger)
        {
            health = Mathf.Clamp(savedHealth, 0f, maxHealth);
            hunger = Mathf.Clamp(savedHunger, 0f, maxHunger);

            passiveHungerTimer = 0f;
            starvationTimer = 0f;
            regenerationTimer = 0f;

            RaiseAllChanged();
        }

        private void RaiseAllChanged()
        {
            HealthChanged?.Invoke(health, maxHealth);
            HungerChanged?.Invoke(hunger, maxHunger);
        }
    }
}
