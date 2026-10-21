using UnityEngine;
using Game.World.Generation;

namespace Game.World.Lighting
{
    /// <summary>Stable daylight + artificial light sampling for sprites and particles.
    /// RGB emission is intentionally unaffected by the day/night cycle.</summary>
    public static class WorldVisualLightSampler
    {
        public static Color Sample(Game.World.World world, int x, int y)
        {
            if (world == null) return Color.white;

            LightNode node = world.GetLight(x, y);
            LightNode above = world.GetLight(x, y + 1);
            float sun = Mathf.Max(node.Sun, above.Sun) / 15f;
            float day = Shader.GetGlobalFloat("_TelderDayFactor");
            if (day < 0.001f) day = 1f;

            if (sun < .45f && IsSurfaceDaylit(world, x, y))
                sun = 1f;

            float daylight = sun * day;
            float emissiveR = Mathf.Max(node.R, above.R) / 15f;
            float emissiveG = Mathf.Max(node.G, above.G) / 15f;
            float emissiveB = Mathf.Max(node.B, above.B) / 15f;

            float r = Mathf.Pow(Mathf.Clamp01(Mathf.Max(.15f, Mathf.Max(daylight, emissiveR))), .88f);
            float g = Mathf.Pow(Mathf.Clamp01(Mathf.Max(.15f, Mathf.Max(daylight, emissiveG))), .88f);
            float b = Mathf.Pow(Mathf.Clamp01(Mathf.Max(.16f, Mathf.Max(daylight, emissiveB))), .88f);
            return new Color(r, g, b, 1f);
        }

        public static Color SampleParticle(Game.World.World world, Vector2 position)
        {
            if (world == null) return Color.white;
            int x = Mathf.FloorToInt(position.x), y = Mathf.FloorToInt(position.y);
            float fx = position.x - x, fy = position.y - y;
            Color a = ParticleCell(world, x, y), b = ParticleCell(world, x + 1, y);
            Color c = ParticleCell(world, x, y + 1), d = ParticleCell(world, x + 1, y + 1);
            return Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
        }

        private static Color ParticleCell(Game.World.World world, int x, int y)
        {
            LightNode node = world.GetLight(x, y);
            float day = Shader.GetGlobalFloat("_TelderDayFactor");
            float sun = node.Sun / 15f * Mathf.Clamp01(day);
            return new Color(Mathf.Pow(Mathf.Max(.035f, Mathf.Max(sun, node.R / 15f)), .88f),
                Mathf.Pow(Mathf.Max(.035f, Mathf.Max(sun, node.G / 15f)), .88f),
                Mathf.Pow(Mathf.Max(.04f, Mathf.Max(sun, node.B / 15f)), .88f), 1f);
        }

        public static Color SampleAveraged(Game.World.World world, int x, int y)
        {
            Color a = Sample(world, x, y);
            Color b = Sample(world, x, y + 1);
            Color c = Sample(world, x - 1, y);
            Color d = Sample(world, x + 1, y);
            return new Color((a.r+b.r+c.r+d.r)*0.25f, (a.g+b.g+c.g+d.g)*0.25f, (a.b+b.b+c.b+d.b)*0.25f, 1f);
        }

        private static bool IsSurfaceDaylit(Game.World.World world, int x, int y)
        {
            WorldManager manager = WorldManager.Instance;
            WorldGenerator generator = manager != null ? manager.GetGenerator() : null;
            if (generator != null)
            {
                int approximateSurface = generator.GetSurfaceHeight(x);
                // Current biome terrain can be above the procedural estimate.
                // Characters outdoors (including beneath foliage) must not render black.
                if (y >= approximateSurface - 20) return true;
            }

            // Unobstructed shafts are also naturally illuminated.
            for (int i = 1; i <= 24; i++)
                if (world.GetBlock(x, y + i) != 0) return false;
            return true;
        }
    }
}
