using System.Collections.Generic;
using UnityEngine;
using Game.World;
using Game.Audio;

namespace Game.Entities.Boss
{
    /// <summary>
    /// Large purple projectile. Its screen-space lens is rendered by the
    /// existing DimensionRiftRendererFeature, including RenderGraph mode.
    /// </summary>
    public sealed class BossVoidOrb : MonoBehaviour
    {
        private const int MaxShaderOrbs = 8;
        private static readonly List<BossVoidOrb> Active = new List<BossVoidOrb>(8);
        private static readonly Vector4[] ScreenOrbs = new Vector4[MaxShaderOrbs];
        private static Sprite sprite;
        private Vector2 velocity;
        private float radius;
        private float healthFraction;
        private float age;
        private Transform owner;
        private SpriteRenderer visual;
        public static bool HasActiveOrbs { get { return Active.Count > 0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { Active.Clear(); }

        public static BossVoidOrb Spawn(Vector2 location, Vector2 speed, float size,
            float fraction, Transform origin)
        {
            GameObject go = new GameObject("Boss violet dimensional sphere");
            go.transform.position = location;
            var result = go.AddComponent<BossVoidOrb>();
            result.velocity = speed;
            result.radius = Mathf.Max(.2f, size);
            result.healthFraction = Mathf.Clamp01(fraction);
            result.owner = origin;
            go.transform.localScale = Vector3.one * result.radius * 2f;
            // Release sound at the exact moment the sphere is created.
            TelderAudioSystem.PlayAt("boss.ancient_golem.orb.fire",
                location, origin != null ? origin.GetInstanceID() : 0);
            // Loop is attached to the flying sphere: it follows its position,
            // respects SFX volume/spatial fade and stops when it is destroyed.
            TelderAudioSystem.AttachLoop(go, "boss.ancient_golem.orb.fly");
            return result;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }
        private void OnDisable() { Active.Remove(this); }
        private void Awake()
        {
            visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = GetOrbSprite();
            visual.sortingLayerName = "Default";
            visual.sortingOrder = 200;
            visual.color = Color.white;
        }
        private static Sprite GetOrbSprite()
        {
            if (sprite != null) return sprite;
            const int size = 32;
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.filterMode = FilterMode.Point;
            var colors = new Color32[size * size];
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float r = new Vector2(x-15.5f,y-15.5f).magnitude / 15.5f;
                if (r > 1f) continue;
                if (r > .82f) colors[y*size+x] = new Color32(194,75,255,255);
                else if (r > .68f) colors[y*size+x] = new Color32(96,24,200,215);
                else colors[y*size+x] = new Color32(39,3,82,188);
            }
            texture.SetPixels32(colors); texture.Apply(false,false);
            sprite = Sprite.Create(texture,new Rect(0,0,size,size), new Vector2(.5f,.5f),size);
            return sprite;
        }
        private void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (age > 7f) { Destroy(gameObject); return; }
            Vector2 origin = transform.position;
            Vector2 delta = velocity * Time.fixedDeltaTime;
            float distance = delta.magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / .15f));
            var manager = WorldManager.Instance;
            var collision = manager != null ? manager.GetWorldCollision() : null;
            var player = FindFirstObjectByType<Game.PlayerStats.PlayerStats>();
            for (int i=1;i<=steps;i++)
            {
                Vector2 current = origin + delta * (i / (float)steps);
                if (collision != null && collision.IsSolid(Mathf.FloorToInt(current.x), Mathf.FloorToInt(current.y)))
                { Destroy(gameObject); return; }
                if (player != null && !player.IsDead &&
                    Vector2.Distance(player.transform.position, current) <= radius + .45f)
                {
                    player.TakeDamage(player.MaxHealth * healthFraction);
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position = origin + delta;
            transform.Rotate(0,0, Time.fixedDeltaTime*46f);
            visual.color = Color.Lerp(Color.white, new Color(.85f,.65f,1f,1f),
                .3f + .3f * Mathf.Sin(Time.time * 5f));
        }

        /// <summary>Called once per camera, not from Update, to avoid mismatched viewports.</summary>
        public static void ApplyToMaterial(Material material, Camera camera)
        {
            if (material == null) return;
            int used = 0;
            if (camera != null)
            {
                for (int i=0; i<Active.Count && used<MaxShaderOrbs; i++)
                {
                    var orb = Active[i];
                    if (orb == null || !orb.isActiveAndEnabled) continue;
                    Vector3 center = camera.WorldToViewportPoint(orb.transform.position);
                    if (center.z <= 0f) continue;
                    Vector3 edge = camera.WorldToViewportPoint(orb.transform.position + Vector3.right*orb.radius);
                    float screenRadius = Mathf.Abs(edge.x - center.x);
                    if (screenRadius < .0001f) continue;
                    ScreenOrbs[used++] = new Vector4(center.x,center.y,screenRadius,1f);
                }
            }
            material.SetInt("_BossOrbCount", used);
            if (used > 0) material.SetVectorArray("_BossOrbs",ScreenOrbs);
        }
    }
}
