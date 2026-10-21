using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.MainMenu
{
    // The existing fullscreen scene-color pass refracts the pixels behind rings.
    // No captured screenshots or new camera render textures are needed per planet.
    public sealed class PixelPlanetRings : MonoBehaviour
    {
        private static readonly List<PixelPlanetRings> active = new List<PixelPlanetRings>();
        private static readonly Vector4[] circles = new Vector4[12];
        private static readonly Vector4[] colors = new Vector4[12];
        private Renderer planet;
        private Color tint = new Color(.35f, .6f, .9f);
        private float seed;
        public static bool HasActiveRings => active.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active.Clear(); }
        private void OnEnable() { if (!active.Contains(this)) active.Add(this); planet = GetComponent<Renderer>(); seed = Mathf.Abs(GetInstanceID() % 997) * .01f; }
        private void OnDisable() { active.Remove(this); }
        private void OnDestroy() { active.Remove(this); }

        public void SetPreview(Texture2D screenshot)
        {
            if (screenshot == null) return;
            try { tint = Dominant(screenshot.GetPixels32()); }
            catch (UnityException) { }
        }

        public static Color Dominant(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0) return new Color(.35f, .6f, .9f);
            var counts = new int[512]; var sums = new Vector3[512]; int best = 0;
            int stride = Mathf.Max(1, pixels.Length / 8192);
            for (int i = 0; i < pixels.Length; i += stride)
            {
                Color32 p = pixels[i]; if (p.a < 100) continue;
                int max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                if (max < 24) continue;
                int bin = (p.r >> 5) * 64 + (p.g >> 5) * 8 + (p.b >> 5);
                counts[bin]++; sums[bin] += new Vector3(p.r, p.g, p.b);
                if (counts[bin] > counts[best]) best = bin;
            }
            if (counts[best] == 0) return new Color(.35f, .6f, .9f);
            Vector3 rgb = sums[best] / (counts[best] * 255f);
            return new Color(rgb.x, rgb.y, rgb.z, 1);
        }

        public static void ApplyToMaterial(Material material, Camera camera)
        {
            int count = 0;
            for (int i = active.Count - 1; i >= 0 && count < circles.Length; i--)
            {
                PixelPlanetRings ring = active[i];
                if (ring == null) { active.RemoveAt(i); continue; }
                if (!ring.isActiveAndEnabled || ring.planet == null || !ring.planet.enabled) continue;
                Bounds bounds = ring.planet.bounds;
                Vector3 center = camera.WorldToViewportPoint(bounds.center);
                if (center.z <= camera.nearClipPlane) continue;
                float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
                Vector3 edge = camera.WorldToViewportPoint(bounds.center + camera.transform.right * radius);
                float rx = Mathf.Abs(edge.x - center.x);
                if (rx < .0001f || center.x < -rx * 2 || center.x > 1 + rx * 2) continue;
                circles[count] = new Vector4(center.x, center.y, rx, rx * camera.aspect);
                Color c = ring.tint; colors[count] = new Vector4(c.r, c.g, c.b, ring.seed); count++;
            }
            material.SetInt("_PlanetRingCount", count);
            material.SetVectorArray("_PlanetRings", circles); material.SetVectorArray("_PlanetRingColors", colors);
        }
    }
}
