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

        [Header("Air")]
        [Tooltip("Optional. If missing, the blue air bar is created automatically above HungerBar at runtime.")]
        [SerializeField] private RectTransform airBarRoot;
        [SerializeField] private Image airFill;
        [SerializeField] private Image airFrame;
        [SerializeField] private Sprite airFillTexture;
        [SerializeField] private Sprite airFrameTexture;
        [SerializeField] private Color airColor = new Color32(52, 142, 230, 255);
        [SerializeField] private Color airFrameColor = Color.white;
        [SerializeField, Min(2f)] private float airBarHeight = 8f;
        [SerializeField, Min(0f)] private float airBarGap = 4f;

        private Vector2 healthShakeBasePosition;
        private float healthShakeEndTime;
        private float healthShakeStartedAt;
        private float healthShakeStrength;
        private bool healthShakeBaseCaptured;

        private void Awake()
        {
            EnsureAirBar();
            ApplyVisualSettings();
            ResolveHealthShakeRoot(true);
        }

        private void OnEnable()
        {
            if (playerStats == null)
                playerStats = FindFirstObjectByType<PlayerStats>();

            Subscribe();
            ResolveHealthShakeRoot(true);
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
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

            Unsubscribe();
            playerStats = source;
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || playerStats == null)
                return;

            playerStats.HealthChanged -= OnHealthChanged;
            playerStats.HungerChanged -= OnHungerChanged;
            playerStats.AirChanged -= OnAirChanged;
            playerStats.Damaged -= OnDamaged;

            playerStats.HealthChanged += OnHealthChanged;
            playerStats.HungerChanged += OnHungerChanged;
            playerStats.AirChanged += OnAirChanged;
            playerStats.Damaged += OnDamaged;
        }

        private void Unsubscribe()
        {
            if (playerStats == null)
                return;

            playerStats.HealthChanged -= OnHealthChanged;
            playerStats.HungerChanged -= OnHungerChanged;
            playerStats.AirChanged -= OnAirChanged;
            playerStats.Damaged -= OnDamaged;
        }

        public void Refresh()
        {
            EnsureAirBar();

            if (playerStats == null)
                return;

            SetFill(healthFill, playerStats.HealthNormalized);
            SetFill(hungerFill, playerStats.HungerNormalized);
            SetAir(playerStats.AirNormalized, playerStats.ShouldShowAir);
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
            SetFill(healthFill, max <= 0f ? 0f : current / max);
        }

        private void OnHungerChanged(float current, float max)
        {
            SetFill(hungerFill, max <= 0f ? 0f : current / max);
        }

        private void OnAirChanged(float current, float max, bool visible)
        {
            SetAir(max <= 0f ? 0f : current / max, visible);
        }

        private void SetAir(float normalized, bool visible)
        {
            EnsureAirBar();
            SetFill(airFill, normalized);

            if (airBarRoot != null)
                airBarRoot.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Makes V5 compatible with scenes made before the air bar existed. If the
        /// editor wizard has not been re-run, the bar is built from the existing
        /// HungerBar geometry and sprite at runtime.
        /// </summary>
        private void EnsureAirBar()
        {
            if (airBarRoot != null && airFill != null && airFrame != null)
                return;

            RectTransform hungerBar = null;
            if (hungerFrame != null)
                hungerBar = hungerFrame.rectTransform.parent as RectTransform;
            if (hungerBar == null && hungerFill != null)
                hungerBar = hungerFill.rectTransform.parent as RectTransform;
            if (hungerBar == null || hungerBar.parent == null)
                return;

            Transform existing = hungerBar.parent.Find("AirBar");
            GameObject rootObject;

            if (existing != null && existing is RectTransform)
            {
                airBarRoot = existing as RectTransform;
                rootObject = existing.gameObject;
            }
            else
            {
                rootObject = new GameObject("AirBar", typeof(RectTransform));
                airBarRoot = rootObject.GetComponent<RectTransform>();
                airBarRoot.SetParent(hungerBar.parent, false);
            }

            airBarRoot.anchorMin = hungerBar.anchorMin;
            airBarRoot.anchorMax = hungerBar.anchorMax;
            airBarRoot.pivot = hungerBar.pivot;
            airBarRoot.sizeDelta = new Vector2(hungerBar.sizeDelta.x, airBarHeight);
            airBarRoot.anchoredPosition = hungerBar.anchoredPosition +
                new Vector2(0f, Mathf.Max(airBarHeight, hungerBar.sizeDelta.y) + airBarGap);

            airFrame = GetOrCreateImage(airBarRoot, "Frame");
            airFill = GetOrCreateImage(airBarRoot, "Fill");

            Stretch(airFrame.rectTransform, 0f);
            Stretch(airFill.rectTransform, 1.5f);

            if (airFrameTexture == null)
                airFrameTexture = hungerFrameTexture != null
                    ? hungerFrameTexture
                    : hungerFrame != null ? hungerFrame.sprite : null;

            if (airFillTexture == null)
                airFillTexture = hungerFillTexture != null
                    ? hungerFillTexture
                    : hungerFill != null ? hungerFill.sprite : airFrameTexture;

            airFrame.rectTransform.SetSiblingIndex(0);
            airFill.rectTransform.SetSiblingIndex(1);
            rootObject.SetActive(false);
        }

        private static Image GetOrCreateImage(RectTransform parent, string name)
        {
            Transform child = parent.Find(name);
            Image image = child != null ? child.GetComponent<Image>() : null;

            if (image != null)
                return image;

            GameObject go = child != null
                ? child.gameObject
                : new GameObject(name, typeof(RectTransform));

            if (child == null)
                go.transform.SetParent(parent, false);

            image = go.GetComponent<Image>();
            if (image == null)
                image = go.AddComponent<Image>();

            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void ApplyVisualSettings()
        {
            ConfigureFill(healthFill, healthFillTexture, healthColor);
            ConfigureFrame(healthFrame, healthFrameTexture, healthFrameColor);
            ConfigureFill(hungerFill, hungerFillTexture, hungerColor);
            ConfigureFrame(hungerFrame, hungerFrameTexture, hungerFrameColor);
            ConfigureFill(airFill, airFillTexture, airColor);
            ConfigureFrame(airFrame, airFrameTexture, airFrameColor);
        }

        private static void ConfigureFill(Image image, Sprite sprite, Color color)
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
            image.raycastTarget = false;
        }

        private static void ConfigureFrame(Image image, Sprite sprite, Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
        }

        private static void SetFill(Image image, float value)
        {
            if (image != null)
                image.fillAmount = Mathf.Clamp01(value);
        }
    }
}
