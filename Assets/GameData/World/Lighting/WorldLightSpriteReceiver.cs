using System.Collections.Generic;
using UnityEngine;
using Game.UI.Character;

namespace Game.World.Lighting
{
    /// <summary>
    /// The ONLY world-light tint writer for player, enemy and dropped-item sprites.
    /// Shares daylight / RGB light sampling with the rest of the 2D world.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(32500)]
    public sealed class WorldLightSpriteReceiver : MonoBehaviour
    {
        [SerializeField] private Vector2 sampleOffset = Vector2.zero;
        [SerializeField, Range(1f, 40f)] private float transitionSpeed = 16f;
        [SerializeField] private float lightUpdateInterval = 0.08f;
        [SerializeField] private float rendererRefreshInterval = 1f;

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>(12);
        private readonly Dictionary<int, Color> baseColors = new Dictionary<int, Color>(12);
        private Color currentLight = Color.white;
        private bool hasLight;
        private float nextLightUpdate;
        private float nextRendererRefresh;

        private void Awake()
        {
            RefreshRenderers();
        }

        private void OnEnable()
        {
            hasLight = false;
            nextLightUpdate = 0f;
            nextRendererRefresh = 0f;
            RefreshRenderers();
        }

        public void Configure(Vector2 offset)
        {
            sampleOffset = offset;
            hasLight = false;
            nextLightUpdate = 0f;
            RefreshRenderers();
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            if (now >= nextRendererRefresh)
            {
                RefreshRenderers();
                nextRendererRefresh = now + Mathf.Max(.2f, rendererRefreshInterval);
            }

            if (now < nextLightUpdate)
                return;

            float interval = Mathf.Max(.03f, lightUpdateInterval);
            nextLightUpdate = now + interval;

            WorldManager manager = WorldManager.Instance;
            if (manager == null || manager.GetWorld() == null)
                return;

            Vector2 p = (Vector2)transform.position + sampleOffset;
            int x = Mathf.FloorToInt(p.x);
            int y = Mathf.FloorToInt(p.y);

            // Sample slightly above the feet / body centre: sprites standing on
            // top of a solid block should see the light in the air, not underground.
            Color target = WorldVisualLightSampler.SampleAveraged(manager.GetWorld(), x, y + 1);
            if (!hasLight)
            {
                currentLight = target;
                hasLight = true;
            }
            else
            {
                float t = 1f - Mathf.Exp(-Mathf.Max(1f, transitionSpeed) * interval);
                currentLight = Color.Lerp(currentLight, target, t);
            }

            ApplyLighting();
        }

        private void RefreshRenderers()
        {
            SpriteRenderer[] found = GetComponentsInChildren<SpriteRenderer>(true);
            renderers.Clear();
            for (int i = 0; i < found.Length; i++)
            {
                SpriteRenderer renderer = found[i];
                if (renderer == null) continue;
                renderers.Add(renderer);
                int id = renderer.GetInstanceID();
                // Capture a new sprite's authored tint only once. Never cache an
                // already-darkened color as its base tint on subsequent refreshes.
                if (!baseColors.ContainsKey(id))
                    baseColors.Add(id, renderer.color);
            }
        }

        private void ApplyLighting()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                if (!baseColors.TryGetValue(renderer.GetInstanceID(), out Color baseColor))
                    continue;

                // Appearance tint is independent from the sampled world light. Read it
                // every update so palette changes never fight the lighting writer and do
                // not flicker between the authored and tinted colors.
                Color appearanceTint = Color.white;
                AppearanceTintData tintData = renderer.GetComponent<AppearanceTintData>();
                if (tintData != null) appearanceTint = tintData.Tint;

                renderer.color = new Color(
                    baseColor.r * appearanceTint.r * currentLight.r,
                    baseColor.g * appearanceTint.g * currentLight.g,
                    baseColor.b * appearanceTint.b * currentLight.b,
                    baseColor.a * appearanceTint.a);
            }
        }
    }
}
