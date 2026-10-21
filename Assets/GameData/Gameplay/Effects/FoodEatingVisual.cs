using System.Collections.Generic;
using UnityEngine;
using Game.PlayerStats;
using Game.Combat;

namespace Game.Gameplay.Effects
{
    [DefaultExecutionOrder(63000)]
    public sealed class FoodEatingVisual : MonoBehaviour
    {
        private struct Bit
        {
            public SpriteRenderer Renderer;
            public Vector3 Position, Velocity;
            public float Life, Total;
            public bool Effect;
            public Color Tint;
        }

        private readonly Bit[] bits = new Bit[96];
        private readonly Dictionary<Sprite, Sprite[]> shards = new Dictionary<Sprite, Sprite[]>();
        private int cursor;
        private float nextBite;
        private FoodUseController food;
        private PlayerWeaponRigController rig;
        private SpriteRenderer food3D;
        private GameObject eatingHand;
        private Material handMaterial, spriteMaterial;
        private Texture2D pixel;
        private Sprite pixelSprite;
        public void EmitEffect(Vector3 position, Color color)
        {
            Emit(position, Pixel(), color, true);
        }

        private Sprite Pixel()
        {
            if (pixelSprite != null)
                return pixelSprite;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            return pixelSprite = Sprite.Create(pixel, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 16);
        }

        private Sprite[] Pieces(Sprite source)
        {
            if (shards.TryGetValue(source, out var cached))
                return cached;
            if (shards.Count >= 32)
                return new[]
                {
                    source
                };
            Rect r = source.rect;
            int columns = Mathf.Min(4, Mathf.Max(1, Mathf.FloorToInt(r.width))), rows = Mathf.Min(4, Mathf.Max(1, Mathf.FloorToInt(r.height)));
            var result = new Sprite[columns * rows];
            int w = Mathf.Max(1, Mathf.FloorToInt(r.width / columns)), h = Mathf.Max(1, Mathf.FloorToInt(r.height / rows));
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    result[x + y * columns] = Sprite.Create(source.texture, new Rect(r.x + x * w, r.y + y * h, w, h), new Vector2(.5f, .5f), source.pixelsPerUnit);
            shards[source] = result;
            return result;
        }

        private void Emit(Vector3 p, Sprite sprite, Color tint, bool effect)
        {
            int i = cursor++ % bits.Length;
            var b = bits[i];
            if (b.Renderer == null)
            {
                var go = new GameObject("Food pixel");
                go.transform.SetParent(transform, false);
                b.Renderer = go.AddComponent<SpriteRenderer>();
                b.Renderer.sortingOrder = 8100;
                b.Renderer.sharedMaterial = SpriteMaterial();
            }

            b.Renderer.sprite = sprite;
            b.Renderer.enabled = true;
            b.Renderer.transform.localScale = Vector3.one * (effect ? UnityEngine.Random.Range(.8f, 1.5f) : .45f);
            b.Effect = effect;
            b.Tint = tint;
            b.Total = b.Life = effect ? .85f : .55f;
            b.Position = p;
            b.Velocity = effect ? new Vector3(UnityEngine.Random.Range(-.4f, .4f), .7f, UnityEngine.Random.Range(-.3f, .3f)) : new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(.4f, 1.4f), 0);
            if (effect)
                b.Position += new Vector3(UnityEngine.Random.Range(-.4f, .4f), UnityEngine.Random.Range(-.6f, .7f), 0);
            b.Renderer.transform.position = b.Position;
            bits[i] = b;
        }

        private void LateUpdate()
        {
            if (GetComponent<PlayerFoodEffects>()?.IsLocalPlayer != true)
            {
                OnDisable();
                return;
            }

            if (food == null)
                food = GetComponent<FoodUseController>();
            if (rig == null)
                rig = GetComponent<PlayerWeaponRigController>();
            var voxel = Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
            Camera cam = Camera.main;
            bool eating = food != null && food.IsEating;
            if (eating)
            {
                Vector3 p = rig != null && rig.GetRuntimeHandPoint() != null ? rig.GetRuntimeHandPoint().position : transform.position + new Vector3(.5f, .2f, 0);
                Sprite sprite = Game.Inventory.UI.ItemIconProvider.GetIcon(food.EatingItemId);
                if (voxel != null && cam != null)
                {
                    p = cam.transform.position + cam.transform.forward * .65f + cam.transform.right * .3f - cam.transform.up * (.2f + .035f * Mathf.Sin(Time.time * 18));
                    if (voxel.Motor != null && voxel.Motor.ThirdPerson && voxel.Avatar != null && voxel.Avatar.RightHand != null)
                        p = voxel.Avatar.RightHand.position;
                    if (food3D == null)
                    {
                        var go = new GameObject("Eating food");
                        go.transform.SetParent(transform, false);
                        food3D = go.AddComponent<SpriteRenderer>();
                        food3D.sortingOrder = 8100;
                        food3D.sharedMaterial = SpriteMaterial();
                    }

                    food3D.sprite = sprite;
                    food3D.enabled = true;
                    food3D.transform.position = p;
                    food3D.transform.rotation = cam.transform.rotation;
                    food3D.transform.localScale = Vector3.one * .35f;
                }

                if (sprite != null && Time.time >= nextBite)
                {
                    nextBite = Time.time + .12f;
                    var pieces = Pieces(sprite);
                    for (int i = 0; i < 3; i++)
                        Emit(p, pieces[UnityEngine.Random.Range(0, pieces.Length)], Color.white, false);
                }
            }

            if (!eating && food3D != null)
                food3D.enabled = false;
            UpdateHand(eating, voxel, cam);
            for (int i = 0; i < bits.Length; i++)
            {
                var b = bits[i];
                if (b.Renderer == null || b.Life <= 0)
                    continue;
                b.Life -= Time.deltaTime;
                if (b.Life <= 0)
                {
                    b.Renderer.enabled = false;
                    bits[i] = b;
                    continue;
                }

                if (!b.Effect)
                    b.Velocity.y -= 4 * Time.deltaTime;
                b.Position += b.Velocity * Time.deltaTime;
                b.Renderer.transform.position = b.Position;
                if (voxel != null && cam != null)
                    b.Renderer.transform.rotation = cam.transform.rotation;
                Color light = voxel != null ? voxel.Lighting.Sample(Vector3Int.FloorToInt(b.Position)) : Game.World.Lighting.WorldVisualLightSampler.SampleParticle(Game.World.WorldManager.Instance?.GetWorld(), b.Position);
                Color c = b.Tint * light;
                c.a = Mathf.Clamp01(b.Life / .2f);
                b.Renderer.color = c;
                bits[i] = b;
            }
        }

        private Material SpriteMaterial()
        {
            if (spriteMaterial != null)
                return spriteMaterial;
            var shader = UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite") ?? Shader.Find("Sprites/Default");
            if (shader != null)
                spriteMaterial = new Material(shader);
            return spriteMaterial;
        }

        private void UpdateHand(bool eating, Game.World.Voxel3D.VoxelWorld3DRuntime voxel, Camera camera)
        {
            bool show = eating && voxel != null && camera != null && voxel.Motor != null && !voxel.Motor.ThirdPerson;
            if (!show)
            {
                if (eatingHand != null)
                    eatingHand.SetActive(false);
                return;
            }

            if (eatingHand == null)
            {
                eatingHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eatingHand.name = "Eating arm";
                eatingHand.layer = 2;
                var collider = eatingHand.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    Destroy(collider);
                }

                eatingHand.transform.SetParent(transform, false);
                var shader = UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelLit") ?? Shader.Find("Unlit/Texture");
                if (shader != null)
                {
                    handMaterial = new Material(shader);
                    handMaterial.SetFloat("_UseVertexLighting", 0);
                    handMaterial.color = new Color(.78f, .59f, .42f);
                    foreach (var skin in GetComponentsInChildren<SpriteRenderer>(true))
                        if (skin.sprite != null && skin.name.IndexOf("Arm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            handMaterial.mainTexture = skin.sprite.texture;
                            var r = skin.sprite.textureRect;
                            handMaterial.mainTextureScale = new Vector2(r.width / skin.sprite.texture.width, r.height / skin.sprite.texture.height);
                            handMaterial.mainTextureOffset = new Vector2(r.x / skin.sprite.texture.width, r.y / skin.sprite.texture.height);
                            handMaterial.color = skin.color;
                            break;
                        }

                    eatingHand.GetComponent<MeshRenderer>().sharedMaterial = handMaterial;
                }
            }

            eatingHand.SetActive(true);
            eatingHand.transform.position = food3D.transform.position - camera.transform.forward * .18f - camera.transform.up * .08f;
            eatingHand.transform.rotation = camera.transform.rotation;
            eatingHand.transform.localScale = new Vector3(.1f, .12f, .35f);
        }

        private void OnDisable()
        {
            if (eatingHand != null)
                eatingHand.SetActive(false);
            foreach (var b in bits)
                if (b.Renderer != null)
                    b.Renderer.enabled = false;
            if (food3D != null)
                food3D.enabled = false;
        }

        private void OnDestroy()
        {
            foreach (var pair in shards)
                foreach (var s in pair.Value)
                    if (s != null)
                        Destroy(s);
            if (pixelSprite != null)
                Destroy(pixelSprite);
            if (pixel != null)
                Destroy(pixel);
            if (spriteMaterial != null)
                Destroy(spriteMaterial);
            if (handMaterial != null)
                Destroy(handMaterial);
        }
    }
}
