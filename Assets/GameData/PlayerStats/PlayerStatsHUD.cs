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

        [Header("Hunger")]
        [SerializeField] private Image hungerFill;
        [SerializeField] private Image hungerFrame;
        [SerializeField] private Sprite hungerFillTexture;
        [SerializeField] private Sprite hungerFrameTexture;
        [SerializeField] private Color hungerColor = new Color32(226, 130, 91, 255);
        [SerializeField] private Color hungerFrameColor = Color.white;

        private void Awake()
        {
            ApplyVisualSettings();
        }

        private void OnEnable()
        {
            if (playerStats == null)
                return;

            playerStats.HealthChanged += OnHealthChanged;
            playerStats.HungerChanged += OnHungerChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (playerStats == null)
                return;

            playerStats.HealthChanged -= OnHealthChanged;
            playerStats.HungerChanged -= OnHungerChanged;
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
            }

            playerStats = source;

            if (isActiveAndEnabled && playerStats != null)
            {
                playerStats.HealthChanged += OnHealthChanged;
                playerStats.HungerChanged += OnHungerChanged;
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
