using UnityEngine;
using Game.World;

namespace Game.Entities.Boss
{
    /// <summary>A ground-following, single-hit wave propagating out from a boss slam.</summary>
    public sealed class BossShockwave : MonoBehaviour
    {
        private static Sprite sprite;
        private SpriteRenderer visual;
        private float direction;
        private float remaining;
        private float speed;
        private float damage;
        private bool hitPlayer;
        private Game.PlayerStats.PlayerStats player;

        public static void Spawn(Vector2 origin, float sign, float range, float waveSpeed,
            float waveDamage, string textureName = null)
        {
            GameObject go = new GameObject("Boss ground shockwave");
            go.transform.position = origin + Vector2.up * .28f;
            BossShockwave wave = go.AddComponent<BossShockwave>();
            wave.player = FindFirstObjectByType<Game.PlayerStats.PlayerStats>();
            wave.direction = sign;
            wave.remaining = Mathf.Max(1f, range);
            wave.speed = Mathf.Max(1f, waveSpeed);
            wave.damage = Mathf.Max(0f, waveDamage);
            wave.visual.flipX = sign < 0f;
            wave.ApplyTexture(textureName);
        }
        private void Awake()
        {
            // Keep the renderer on a child: pivot adjustments must not move
            // the root transform used by terrain tracking and hit detection.
            GameObject visualObject = new GameObject("Wave visual");
            visualObject.transform.SetParent(transform, false);
            visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = MakeSprite();
            visual.sortingLayerName = "Default";
            visual.sortingOrder = 190;
            visual.color = new Color(.67f,.43f,1f,1f);
            transform.localScale = new Vector3(1.05f, .75f, 1f);
        }
        private void ApplyTexture(string configuredTexture)
        {
            if (string.IsNullOrWhiteSpace(configuredTexture)) return;

            Sprite custom = BossSlamGroundParticles.TryGetConfiguredSprite(configuredTexture);
            if (custom == null) return; // Keep the original visual if PNG is missing.

            visual.sprite = custom;
            visual.color = Color.white; // Preserve the pixels/alpha of the supplied texture.
            // Custom PNG sprites use a centered pivot, whereas the built-in
            // wave has its pivot at the bottom. Keep every custom wave grounded.
            visual.transform.localPosition = new Vector3(0f, -custom.bounds.min.y, 0f);
            if (custom.texture != null)
                custom.texture.filterMode = FilterMode.Point;
        }

        private static Sprite MakeSprite()
        {
            if (sprite != null) return sprite;
            Texture2D texture = new Texture2D(16,16,TextureFormat.RGBA32,false);
            texture.filterMode = FilterMode.Point;
            Color32[] pixels = new Color32[256];
            for (int x=0;x<16;x++)
                for (int y=0;y<16;y++)
                    if (y <= 12 - Mathf.Abs(8-x) && y > 0 && (x + y) % 5 != 0)
                        pixels[x+y*16] = new Color32(220,182,255,255);
            texture.SetPixels32(pixels); texture.Apply();
            sprite = Sprite.Create(texture,new Rect(0,0,16,16),new Vector2(.5f,0f),16);
            return sprite;
        }
        private void FixedUpdate()
        {
            var manager = WorldManager.Instance;
            if (remaining <= 0 || manager == null || manager.GetWorld() == null || manager.GetWorldCollision() == null)
            { Destroy(gameObject); return; }
            float distance = Mathf.Min(remaining,speed*Time.fixedDeltaTime);
            int steps = Mathf.Max(1,Mathf.CeilToInt(distance/.2f));
            for (int i=0;i<steps;i++)
            {
                Vector2 pos = transform.position; pos.x += direction*distance/steps;
                int x = Mathf.FloorToInt(pos.x), estimate = Mathf.FloorToInt(pos.y);
                if (!manager.GetWorld().IsLoaded(x,estimate)) { Destroy(gameObject); return; }
                bool found = false;
                // Eighth-block probes follow slabs and stairs without jumping
                // to a different floor several blocks above the wave.
                for (float y=estimate+1.5f;y>=estimate-3f;y-=.125f)
                {
                    var collision = manager.GetWorldCollision();
                    if (collision.OverlapsAny(new Rect(pos.x-.06f,y-.125f,.12f,.12f)) &&
                        !collision.OverlapsAny(new Rect(pos.x-.06f,y+.01f,.12f,.15f)))
                    { pos.y=y+.04f;found=true;break; }
                }
                if (!found) { Destroy(gameObject); return; }
                transform.position=pos;
                if (!hitPlayer && player != null && !player.IsDead &&
                    Mathf.Abs(player.transform.position.x-pos.x)<.7f &&
                    player.transform.position.y-.85f<pos.y+1.05f && player.transform.position.y+.85f>pos.y)
                {player.TakeDamage(damage);hitPlayer=true;}
            }
            remaining-=distance;
        }

    }
}
