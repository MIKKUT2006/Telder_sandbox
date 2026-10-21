using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.World.Effects
{
    /// <summary>
    /// Mining / furniture / footstep particles for both the built-in pipeline and URP 2D.
    ///
    /// V11.8 used a MeshRenderer, which is not reliably drawn by URP's 2D Renderer.
    /// This implementation uses the supported ParticleSystemRenderer with an
    /// unlit sprite material. Unity internally pools its particles; no Sprite.Create,
    /// GameObject.Instantiate or mesh upload happens for each mining hit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PixelImpactParticles : MonoBehaviour
    {
        private const int ImpactCapacity = 700;
        private const int DustCapacity = 900;

        private static PixelImpactParticles instance;
        private static int lastDepthFrame = -1;
        private static float cachedVisibleZ = -.5f;

        private readonly ParticleSystem.Particle[] lightBuffer = new ParticleSystem.Particle[DustCapacity];
        private readonly System.Collections.Generic.Dictionary<uint, Color32> albedo = new System.Collections.Generic.Dictionary<uint, Color32>();
        private readonly System.Collections.Generic.HashSet<uint> aliveSeeds = new System.Collections.Generic.HashSet<uint>();
        private readonly System.Collections.Generic.List<uint> retiredSeeds = new System.Collections.Generic.List<uint>();
        private uint nextSeed = 1;
        private float nextLightUpdate;
        private ParticleSystem impacts;
        private ParticleSystem dust;
        private ParticleSystemRenderer impactRenderer;
        private ParticleSystemRenderer dustRenderer;
        private Material sharedMaterial;
        private Texture2D pixelTexture;
        private float lastFootstepTime = -1f;
        private float debugUntil;
        private float nextRendererCheck;
        private int recoveredRendererCount;
        private int blockBursts;
        private int furnitureBursts;
        private Vector3 lastDebugPosition;
        private string activeShader = "none";

        public static int ActiveCount
        {
            get { return instance != null ? instance.GetActiveCount() : 0; }
        }

        public static int BlockBurstCount
        {
            get { return instance != null ? instance.blockBursts : 0; }
        }

        // The main-menu isolation code disables Renderers located in the
        // DontDestroyOnLoad scene.  V11.9 was putting both particle renderers
        // there, so their enabled flag stayed false after entering the world.
        // Keep particles SCENE-LOCAL and recreate them on each scene load.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            // LoadSceneMode.Single normally destroys the previous instance.
            // An explicit check also handles unusual scene transitions.
            if (instance != null && loadMode == LoadSceneMode.Single &&
                instance.gameObject.scene != scene)
            {
                PixelImpactParticles previous = instance;
                instance = null;
                Destroy(previous.gameObject);
            }
            PixelImpactParticles current = Ensure();
            if (current != null && current.gameObject.scene != scene &&
                scene.IsValid() && scene.isLoaded && loadMode == LoadSceneMode.Single)
            {
                SceneManager.MoveGameObjectToScene(current.gameObject, scene);
            }
            if (current != null)
                current.RestoreRenderers();
        }

        private static PixelImpactParticles Ensure()
        {
            if (instance != null) return instance;
            GameObject obj = new GameObject("[Runtime] Telder Visible Pixel Particles");
            instance = obj.AddComponent<PixelImpactParticles>();
            // Do not call DontDestroyOnLoad here. MainMenuVisualCleanup disables
            // all persistent Renderers, including particle effects.
            return instance;
        }

        private void Awake()
        {
            if(!Game.World.Dimensions.DimensionTravelRuntime.Current.Is3D)BlockTextureFragments.Prewarm();
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.layer = 0;

            // A dedicated point-filtered 2x2 texture, not a per-particle sprite.
            pixelTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            pixelTexture.name = "Telder Pixel Particle White";
            pixelTexture.filterMode = FilterMode.Point;
            pixelTexture.wrapMode = TextureWrapMode.Clamp;
            pixelTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            pixelTexture.Apply(false, true);

            // URP 2D Renderer explicitly supports ParticleSystemRenderer with its
            // 2D sprite material. The previous custom MeshRenderer is removed.
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                Debug.LogError("PIXEL PARTICLES: no unlit particle/sprite shader available.");
                enabled = false;
                return;
            }

            activeShader = shader.name;
            sharedMaterial = new Material(shader);
            sharedMaterial.name = "Telder Pixel Particle Sprite Material";
            if (sharedMaterial.HasProperty("_MainTex")) sharedMaterial.SetTexture("_MainTex", pixelTexture);
            if (sharedMaterial.HasProperty("_BaseMap")) sharedMaterial.SetTexture("_BaseMap", pixelTexture);

            impacts = CreateSystem("Mining + Breaking", ImpactCapacity, .38f, out impactRenderer);
            dust = CreateSystem("Running Dust", DustCapacity, 0f, out dustRenderer);
        }

        private ParticleSystem CreateSystem(string title, int capacity, float gravity, out ParticleSystemRenderer renderer)
        {
            GameObject go = new GameObject(title);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true; // Keep the simulation alive after Unity's default duration.
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.startSize = 0.14f;
            main.startLifetime = 1f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            main.gravityModifier = gravity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false; // Only calls to Emit() spawn particles.
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            // Fade out near the end of the lifetime. Per-particle color remains intact.
            ParticleSystem.ColorOverLifetimeModule colorLife = system.colorOverLifetime;
            colorLife.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, .55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(fade);

            renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) renderer = go.AddComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 9000;
            renderer.sharedMaterial = sharedMaterial;
            renderer.enabled = true;

            system.Play(true);
            return system;
        }

        public static void EmitBlock(Game.World.World world, int x, int y, ushort blockId, bool background, bool hit)
        {
            if (blockId == 0) return;
            if (BlockTextureFragments.Emit(world, x, y, blockId, background, hit)) return;
            PixelImpactParticles fx = Ensure();
            if (fx == null || fx.impacts == null) return;

            Color color = ColorForBlock(world, blockId);

            if (background) color = Color.Lerp(color, Color.gray, .2f);

            int amount = hit ? 9 : 21;
            float speed = hit ? 1.8f : 3.2f;
            for (int i = 0; i < amount; i++)
            {
                Vector2 position = new Vector2(x + Random.Range(.12f, .88f), y + Random.Range(.14f, .86f));
                Vector2 velocity = new Vector2(Random.Range(-speed, speed), Random.Range(.45f, speed));
                fx.Emit(fx.impacts, position, velocity, color,
                    Random.Range(hit ? .11f : .14f, hit ? .2f : .26f),
                    Random.Range(hit ? .4f : .55f, hit ? .8f : 1.1f));
            }
            fx.blockBursts++;
        }

        public static void EmitFurniture(int x, int y, string furnitureId, bool hit)
        {
            if (BlockTextureFragments.EmitFurniture(x, y, furnitureId, hit)) return;
            PixelImpactParticles fx = Ensure();
            if (fx == null || fx.impacts == null) return;
            Game.World.World world = Game.World.WorldManager.Instance != null
                ? Game.World.WorldManager.Instance.GetWorld() : null;
            Color color = ColorForName(furnitureId);
            int amount = hit ? 9 : 20;
            for (int i = 0; i < amount; i++)
            {
                fx.Emit(fx.impacts,
                    new Vector2(x + Random.Range(.1f, .9f), y + Random.Range(.1f, .9f)),
                    new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(.5f, 3f)),
                    color, Random.Range(.11f, .23f), Random.Range(.45f, .9f));
            }
            fx.furnitureBursts++;
        }

        public static void EmitFootstep(Vector2 feet, Color color, float direction)
        {
            PixelImpactParticles fx = Ensure();
            if (fx == null || fx.dust == null) return;
            if (Time.time - fx.lastFootstepTime < .055f) return;
            fx.lastFootstepTime = Time.time;

            Game.World.World world = Game.World.WorldManager.Instance != null
                ? Game.World.WorldManager.Instance.GetWorld() : null;
            Color lit = color;
            for (int i = 0; i < 5; i++)
            {
                fx.Emit(fx.dust,
                    feet + new Vector2(Random.Range(-.15f, .15f), Random.Range(0f, .04f)),
                    new Vector2(-direction * Random.Range(.07f, .45f), Random.Range(.025f, .14f)),
                    lit, Random.Range(.055f, .13f), Random.Range(2f, 3.2f));
            }
        }

        private void Emit(ParticleSystem system, Vector2 position, Vector2 velocity, Color color, float size, float lifetime)
        {
            if (system == null) return;
            // In front of the active camera, independent of terrain/chunk Z planes.
            float z = ResolveVisibleZ();
            ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams();
            parameters.position = new Vector3(position.x, position.y, z);
            parameters.velocity = new Vector3(velocity.x, velocity.y, 0f);
            uint seed = nextSeed++;
            if (nextSeed == 0) nextSeed = 1;
            albedo[seed] = color;
            parameters.randomSeed = seed;
            var manager = Game.World.WorldManager.Instance;
            Color light = Game.World.Lighting.WorldVisualLightSampler.SampleParticle(manager != null ? manager.GetWorld() : null, position);
            parameters.startColor = new Color(color.r * light.r, color.g * light.g, color.b * light.b, color.a);
            parameters.startSize = size;
            parameters.startLifetime = lifetime;
            system.Emit(parameters, 1);
        }

        private static float ResolveVisibleZ()
        {
            if (lastDepthFrame == Time.frameCount) return cachedVisibleZ;
            lastDepthFrame = Time.frameCount;
            Camera cam = Camera.main;
            if (cam == null) return cachedVisibleZ = -.5f;
            if (Mathf.Abs(cam.transform.forward.z) < .1f) return cachedVisibleZ = 0f;
            float ahead = Mathf.Max(cam.nearClipPlane + .8f, 1.1f);
            return cachedVisibleZ = cam.transform.position.z + cam.transform.forward.z * ahead;
        }

        public static void ClearAll()
        {
            if (instance == null) return;
            instance.ClearParticles();
        }

        private void ClearParticles()
        {
            albedo.Clear();
            if (impacts != null)
            {
                impacts.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                impacts.Play(true);
            }
            if (dust != null)
            {
                dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                dust.Play(true);
            }
        }

        public static void EmitDebugBurst(Vector2 worldPosition)
        {
            PixelImpactParticles fx = Ensure();
            if (fx == null || fx.impacts == null) return;
            fx.lastDebugPosition = new Vector3(worldPosition.x, worldPosition.y, ResolveVisibleZ());
            for (int i = 0; i < 58; i++)
            {
                Color c = i % 2 == 0 ? new Color(1f, .9f, .08f) : new Color(.12f, .9f, 1f);
                Vector2 p = worldPosition + Random.insideUnitCircle * .24f;
                fx.Emit(fx.impacts, p,
                    Random.insideUnitCircle * 1.4f + Vector2.up * .5f,
                    c, Random.Range(.17f, .31f), Random.Range(1.8f, 3.2f));
            }
            fx.debugUntil = Time.unscaledTime + 6f;
            Debug.Log("PIXEL PARTICLES F8 / ParticleSystemRenderer" +
                " | shader=" + fx.activeShader +
                " | camera=" + (Camera.main != null ? Camera.main.name : "none") +
                " | emitted=" + fx.GetActiveCount());
        }

        private void LateUpdate()
        {
            if (Time.time < nextLightUpdate) return;
            nextLightUpdate = Time.time + .05f;
            var manager = Game.World.WorldManager.Instance;
            var world = manager != null ? manager.GetWorld() : null;
            aliveSeeds.Clear();
            Relight(impacts, world); Relight(dust, world);
            retiredSeeds.Clear();
            foreach (var pair in albedo) if (!aliveSeeds.Contains(pair.Key)) retiredSeeds.Add(pair.Key);
            for (int i = 0; i < retiredSeeds.Count; i++) albedo.Remove(retiredSeeds[i]);
        }

        private void Relight(ParticleSystem system, Game.World.World world)
        {
            if (system == null) return;
            int count = system.GetParticles(lightBuffer);
            for (int i = 0; i < count; i++)
            {
                var particle = lightBuffer[i]; aliveSeeds.Add(particle.randomSeed);
                if (!albedo.TryGetValue(particle.randomSeed, out Color32 baseColor)) continue;
                Color light = Game.World.Lighting.WorldVisualLightSampler.SampleParticle(world, particle.position);
                particle.startColor = new Color(baseColor.r / 255f * light.r, baseColor.g / 255f * light.g,
                    baseColor.b / 255f * light.b, baseColor.a / 255f);
                lightBuffer[i] = particle;
            }
            system.SetParticles(lightBuffer, count);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRendererCheck)
            {
                nextRendererCheck = Time.unscaledTime + 0.25f;
                RestoreRenderers();
            }
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Camera cam = Camera.main;
                Vector2 center = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
                EmitDebugBurst(center);
            }
        }

        private void RestoreRenderers()
        {
            // Do not fight the MainMenuSceneIsolation cleanup on a menu scene.
            // The scene-local particle root is simply recreated after transition.
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || gameObject.scene != active ||
                active.name.IndexOf("menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (impactRenderer != null && !impactRenderer.enabled)
            {
                impactRenderer.enabled = true;
                recoveredRendererCount++;
            }
            if (dustRenderer != null && !dustRenderer.enabled)
            {
                dustRenderer.enabled = true;
                recoveredRendererCount++;
            }
        }

        private int GetActiveCount()
        {
            return (impacts != null ? impacts.particleCount : 0) +
                   (dust != null ? dust.particleCount : 0);
        }

        private void OnGUI()
        {
            if (Time.unscaledTime > debugUntil) return;
            Camera cam = Camera.main;
            Vector3 viewport = cam != null ? cam.WorldToViewportPoint(lastDebugPosition) : Vector3.zero;
            GUI.Label(new Rect(14, 14, 1000, 95),
                "PIXEL PARTICLES V11.10 | Scene-local renderer fix" +
                "\nactive=" + GetActiveCount() + " mining bursts=" + blockBursts +
                " furniture=" + furnitureBursts + " shader=" + activeShader +
                "\nimpact renderer visible=" + (impactRenderer != null && impactRenderer.isVisible) +
                " enabled=" + (impactRenderer != null && impactRenderer.enabled) +
                " activeGO=" + (impactRenderer != null && impactRenderer.gameObject.activeInHierarchy) +
                " recovered=" + recoveredRendererCount +
                " scene=" + gameObject.scene.name +
                " | camera=" + (cam != null ? cam.name : "NONE") +
                " layerVisible=" + (cam != null && (cam.cullingMask & (1 << gameObject.layer)) != 0) +
                " viewport=" + viewport.ToString("F2"));
        }

        private static Color LightForParticles(Game.World.World world, int x, int y, Color tint)
        {
            if (world == null) return tint;
            Color light = Game.World.Lighting.WorldVisualLightSampler.SampleParticle(world, new Vector2(x + .5f, y + .5f));
            return new Color(tint.r * light.r,
                tint.g * light.g,
                tint.b * light.b, tint.a);
        }

        private static Color ColorForBlock(Game.World.World world, ushort id)
        {
            string texture = string.Empty;
            if (world != null)
            {
                var definition = world.GetBlockDefinition(id);
                if (definition != null && !string.IsNullOrEmpty(definition.Texture))
                    texture = definition.Texture;
            }
            return ColorForName(texture);
        }

        private static Color ColorForName(string key)
        {
            key = (key ?? string.Empty).ToLowerInvariant();
            if (key.Contains("snow") || key.Contains("ice")) return new Color(.88f, .95f, 1f, 1f);
            if (key.Contains("sand")) return new Color(.98f, .81f, .53f, 1f);
            if (key.Contains("stone") || key.Contains("rock") || key.Contains("ore")) return new Color(.75f, .79f, .85f, 1f);
            if (key.Contains("grass") || key.Contains("moss") || key.Contains("leaf")) return new Color(.5f, .8f, .39f, 1f);
            if (key.Contains("wood") || key.Contains("trunk")) return new Color(.83f, .58f, .36f, 1f);
            return new Color(.95f, .7f, .48f, 1f);
        }

        private void OnDestroy()
        {
            if (sharedMaterial != null) Destroy(sharedMaterial);
            if (pixelTexture != null) Destroy(pixelTexture);
            if (instance == this) instance = null;
        }
    }
}
