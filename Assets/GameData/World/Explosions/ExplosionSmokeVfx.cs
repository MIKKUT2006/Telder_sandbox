using System.Collections.Generic;
using Game.Visuals.PixelEffects;
using Game.World.Effects;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameWorld = Game.World.World;

namespace Game.World.Explosions
{
    // Smoke shares a single mesh; an explosion never creates one object per puff.
    [DefaultExecutionOrder(5200)]
    public sealed class ExplosionSmokeVfx : MonoBehaviour
    {
        [Range(32,512)] public int MaxParticles = 192;
        [Range(8,32)] public int PixelsPerBlock = 16;

        private struct Puff
        {
            public Vector2 Position, Velocity;
            public float Age, Lifetime, Size, Seed, Opacity;
        }

        private static ExplosionSmokeVfx instance;
        private readonly List<Puff> active = new List<Puff>(192);
        private PixelEffectMesh draw;
        private GameWorld boundWorld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        public static void Spawn(Vector2 center, float radius)
        {
            if (instance == null)
                new GameObject("[Runtime] Pixel Explosion Smoke").AddComponent<ExplosionSmokeVfx>();
            if (instance != null) instance.Emit(center, Mathf.Clamp(radius,.5f,12f));
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            draw = new PixelEffectMesh("Explosion Smoke Pixels",transform,WorldVfxSorting.ParticleSortingOrder-2);
            boundWorld = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Clear(); }

        private void Emit(Vector2 center, float radius)
        {
            GameWorld current = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            if (current != boundWorld) { Clear(); boundWorld = current; }
            int limit = Mathf.Clamp(MaxParticles,32,512);
            int count = Mathf.Min(limit,Mathf.Clamp(Mathf.RoundToInt(16f+radius*10f),20,56));
            // Keep a new blast visible even when several older clouds fill the buffer.
            int replace = Mathf.Max(0,active.Count+count-limit);
            if (replace>0) active.RemoveRange(0,replace);
            for (int i=0;i<count && active.Count<limit;i++)
            {
                float angle = Random.Range(0f,Mathf.PI*2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 offset = Random.insideUnitCircle*Mathf.Min(radius*.28f,.7f);
                active.Add(new Puff
                {
                    Position = center+offset,
                    Velocity = direction*Random.Range(.9f,2.3f)*Mathf.Sqrt(radius)
                        + Vector2.up*Random.Range(.35f,.85f),
                    Lifetime = Random.Range(1.1f,2.1f),
                    Size = Random.Range(.20f,.42f)*Mathf.Sqrt(radius),
                    Seed = Random.Range(0f,100f),
                    Opacity = Random.Range(.42f,.70f)
                });
            }
        }

        private void LateUpdate()
        {
            if (instance != this || draw == null) return;
            GameWorld current = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            if (current != boundWorld) { Clear(); boundWorld = current; }
            if (active.Count == 0) return;

            float dt = Mathf.Min(Time.deltaTime,.05f);
            float pixel = 1f/Mathf.Clamp(PixelsPerBlock,8,32);
            draw.Begin();
            for (int i=active.Count-1;i>=0;i--)
            {
                Puff p = active[i];
                p.Age += dt;
                if (p.Age >= p.Lifetime)
                { active[i]=active[active.Count-1]; active.RemoveAt(active.Count-1); continue; }
                Vector2 previous = p.Position;
                p.Velocity *= Mathf.Exp(-dt*1.9f);
                p.Velocity.y += dt*.65f;
                p.Position += p.Velocity*dt;
                p.Position.x += Mathf.Sin(p.Seed+p.Age*2f)*dt*.11f;

                if (current != null)
                {
                    var block = current.GetBlockDefinition(current.GetBlock(
                        Mathf.FloorToInt(p.Position.x),Mathf.FloorToInt(p.Position.y)));
                    if (block != null && block.Solid && block.BlocksLight)
                    {
                        p.Position = previous;
                        p.Velocity.y = 0f;
                        p.Age += dt*.7f;
                    }
                }

                float age = Mathf.Clamp01(p.Age/p.Lifetime);
                float growth = Mathf.Lerp(1f,2.7f,Mathf.Sqrt(age));
                int units = Mathf.Clamp(Mathf.RoundToInt(p.Size*growth/pixel),3,32);
                float side = units*pixel;
                float x = Mathf.Floor((p.Position.x-side*.5f)/pixel)*pixel;
                float y = Mathf.Floor((p.Position.y-side*.5f)/pixel)*pixel;
                float alpha = p.Opacity*Mathf.Min(1f,p.Age/.045f)
                    *Mathf.Clamp01((1f-age)/.6f);
                alpha = Mathf.Floor(alpha*12f)/12f;
                float shade = Mathf.Lerp(.16f,.50f,age)+Mathf.Sin(p.Seed)*.035f;
                Color color = new Color(shade,shade*1.01f,shade*1.05f,alpha);
                // A stepped cloud silhouette instead of a soft billboard or rotated square.
                float trim = Mathf.Max(pixel,Mathf.RoundToInt(units*.18f)*pixel);
                draw.Quad(x+trim,y,side-trim*2f,trim,color);
                draw.Quad(x,y+trim,side,Mathf.Max(pixel,side-trim*2f),color);
                draw.Quad(x+trim,y+side-trim,side-trim*2f,trim,color);
                active[i] = p;
            }
            draw.Upload();
        }

        private void Clear()
        {
            active.Clear();
            if (draw != null) { draw.Begin(); draw.Upload(); }
        }

        private void OnDisable() { Clear(); }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance != this) return;
            instance = null;
            if (draw != null) draw.Dispose();
        }
    }
}

