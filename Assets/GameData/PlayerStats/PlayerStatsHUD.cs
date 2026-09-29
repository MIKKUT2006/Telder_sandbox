using UnityEngine;
using UnityEngine.UI;

namespace Game.PlayerStats
{
    public class PlayerStatsHUD : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthFrame;
        [SerializeField] private Sprite healthFillTexture;
        [SerializeField] private Sprite healthFrameTexture;
        [SerializeField] private Color healthColor = new Color32(140, 14, 26, 255);
        [SerializeField] private Color healthFrameColor = Color.white;

        [Header("Damage shake")]
        [Tooltip("Optional. If empty, the parent of Health Fill/Frame is used automatically.")]
        [SerializeField] private RectTransform healthShakeRoot;
        [SerializeField, Min(0f)] private float healthShakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float healthShakePixels = 6f;
        [SerializeField, Min(1f)] private float healthShakeFrequency = 48f;

        [Header("Hunger")]
        [SerializeField] private Image hungerFill;
        [SerializeField] private Image hungerFrame;
        [SerializeField] private Sprite hungerFillTexture;
        [SerializeField] private Sprite hungerFrameTexture;
        [SerializeField] private Color hungerColor = new Color32(226, 130, 91, 255);
        [SerializeField] private Color hungerFrameColor = Color.white;

        private Vector2 healthShakeBasePosition;
        private float healthShakeEndTime;
        private float healthShakeStartedAt;
        private float healthShakeStrength;
        private bool healthShakeBaseCaptured;

        private void Awake()
        {
            ApplyVisualSettings();
            ResolveHealthShakeRoot(true);
        }

        private void OnEnable()
        {
            if (playerStats == null)
                return;

            playerStats.HealthChanged += OnHealthChanged;
            playerStats.HungerChanged += OnHungerChanged;
            playerStats.Damaged += OnDamaged;

            ResolveHealthShakeRoot(true);
            Refresh();
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.HealthChanged -= OnHealthChanged;
                playerStats.HungerChanged -= OnHungerChanged;
                playerStats.Damaged -= OnDamaged;
            }

            RestoreHealthBarPosition();
        }

        private void OnValidate()
        {
            ApplyVisualSettings();
        }

        public void SetPlayerStats(PlayerStats source)
        {
            if (playerStats == source)
                return;

            if (isActiveAndEnabled && playerStats != null)
            {
                playerStats.HealthChanged -= OnHealthChanged;
                playerStats.HungerChanged -= OnHungerChanged;
                playerStats.Damaged -= OnDamaged;
            }

            playerStats = source;

            if (isActiveAndEnabled && playerStats != null)
            {
                playerStats.HealthChanged += OnHealthChanged;
                playerStats.HungerChanged += OnHungerChanged;
                playerStats.Damaged += OnDamaged;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (playerStats == null)
                return;

            SetFill(healthFill, playerStats.HealthNormalized);
            SetFill(hungerFill, playerStats.HungerNormalized);
        }

        private void Update()
        {
            if (healthShakeRoot == null || !healthShakeBaseCaptured)
                return;

            if (Time.unscaledTime >= healthShakeEndTime)
            {
                RestoreHealthBarPosition();
                return;
            }

            float duration = Mathf.Max(0.001f, healthShakeEndTime - healthShakeStartedAt);
            float remaining = Mathf.Clamp01((healthShakeEndTime - Time.unscaledTime) / duration);
            float phase = Time.unscaledTime * Mathf.Max(1f, healthShakeFrequency);

            // Two non-matching oscillations give a sharp, game-like hit shake
            // without random-position drift from frame to frame.
            float x = Mathf.Sin(phase * 1.37f) * healthShakeStrength * remaining;
            float y = Mathf.Sin(phase * 2.11f + 1.7f) * healthShakeStrength * 0.45f * remaining;

            healthShakeRoot.anchoredPosition =
                healthShakeBasePosition + new Vector2(x, y);
        }

        private void OnDamaged(float damage)
        {
            ResolveHealthShakeRoot(false);
            if (healthShakeRoot == null || healthShakeDuration <= 0f || healthShakePixels <= 0f)
                return;

            if (Time.unscaledTime >= healthShakeEndTime)
            {
                healthShakeBasePosition = healthShakeRoot.anchoredPosition;
                healthShakeBaseCaptured = true;
            }

            float damage01 = playerStats != null && playerStats.MaxHealth > 0f
                ? Mathf.Clamp01(damage / playerStats.MaxHealth * 8f)
                : 0.5f;

            healthShakeStrength = healthShakePixels * Mathf.Lerp(0.75f, 1.35f, damage01);
            healthShakeStartedAt = Time.unscaledTime;
            healthShakeEndTime = Time.unscaledTime + Mathf.Max(0.01f, healthShakeDuration);
        }

        private void ResolveHealthShakeRoot(bool captureBase)
        {
            if (healthShakeRoot == null)
            {
                if (healthFrame != null)
                    healthShakeRoot = healthFrame.rectTransform.parent as RectTransform;

                if (healthShakeRoot == null && healthFill != null)
                    healthShakeRoot = healthFill.rectTransform.parent as RectTransform;
            }

            if (captureBase && healthShakeRoot != null)
            {
                healthShakeBasePosition = healthShakeRoot.anchoredPosition;
                healthShakeBaseCaptured = true;
            }
        }

        private void RestoreHealthBarPosition()
        {
            if (healthShakeRoot != null && healthShakeBaseCaptured)
                healthShakeRoot.anchoredPosition = healthShakeBasePosition;
        }

        private void OnHealthChanged(float current, float max)
        {
            SetFill(
                healthFill,
                max <= 0f ? 0f : current / max
            );
        }

        private void OnHungerChanged(float current, float max)
        {
            SetFill(
                hungerFill,
                max <= 0f ? 0f : current / max
            );
        }

        private void ApplyVisualSettings()
        {
            ConfigureFill(
                healthFill,
                healthFillTexture,
                healthColor
            );

            ConfigureFrame(
                healthFrame,
                healthFrameTexture,
                healthFrameColor
            );

            ConfigureFill(
                hungerFill,
                hungerFillTexture,
                hungerColor
            );

            ConfigureFrame(
                hungerFrame,
                hungerFrameTexture,
                hungerFrameColor
            );
        }

        private static void ConfigureFill(
            Image image,
            Sprite sprite,
            Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillClockwise = true;
            image.preserveAspect = false;
        }

        private static void ConfigureFrame(
            Image image,
            Sprite sprite,
            Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
        }

        private static void SetFill(Image image, float value)
        {
            if (image == null)
                return;

            image.fillAmount = Mathf.Clamp01(value);
        }
    }
}
