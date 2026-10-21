using UnityEngine;

namespace Game.World.Effects
{
    /// <summary>Scene-local, pixel-art crack stages over the actively mined tile.</summary>
    public sealed class BlockMiningCracks : MonoBehaviour
    {
        private static BlockMiningCracks instance;
        private SpriteRenderer overlay;
        private readonly Sprite[] stages = new Sprite[10];
        private readonly Texture2D[] textures = new Texture2D[10];
        private static BlockMiningCracks Ensure()
        {
            if (instance != null) return instance;
            GameObject obj = new GameObject("[World] Mining cracks");
            instance = obj.AddComponent<BlockMiningCracks>();
            return instance;
        }
        private void Awake()
        {
            overlay = gameObject.AddComponent<SpriteRenderer>();
            overlay.sortingLayerName = "Default";
            overlay.sortingOrder = 8200;
            overlay.enabled = false;
            for (int level = 0; level < stages.Length; level++)
            {
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                Color32[] pixels = new Color32[256];
                int count = 1 + level * 2;
                for (int branch = 0; branch < count; branch++)
                {
                    // Fixed procedural pattern prevents flicker between mining frames.
                    float angle = (branch * 2.399963f) + .2f;
                    Vector2 p = new Vector2(7.5f, 7.5f);
                    int steps = 2 + level / 2 + (branch % 3);
                    for (int k = 0; k < steps; k++)
                    {
                        angle += Mathf.Sin(branch * 17.1f + k * 3.1f) * .45f;
                        p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.35f;
                        int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                        if (x < 0 || x >= 16 || y < 0 || y >= 16) break;
                        pixels[x + y * 16] = new Color32(15, 15, 21, 235);
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                textures[level] = tex;
                stages[level] = Sprite.Create(tex, new Rect(0,0,16,16),
                    new Vector2(.5f,.5f), 16f, 0, SpriteMeshType.FullRect);
            }
        }
        public static void Show(int x, int y, float progress, bool background)
        {
            var fx = Ensure();
            int stage = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * 10f), 0, 9);
            fx.overlay.sprite = fx.stages[stage];
            fx.overlay.color = background ? new Color(1,1,1,.65f) : Color.white;
            fx.transform.position = new Vector3(x + .5f, y + .5f, -.25f);
            fx.overlay.enabled = true;
        }
        public static void Hide()
        {
            if (instance != null && instance.overlay != null) instance.overlay.enabled = false;
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            for (int i = 0; i < 10; i++)
            {
                if (stages[i] != null) Destroy(stages[i]);
                if (textures[i] != null) Destroy(textures[i]);
            }
        }
    }
}
