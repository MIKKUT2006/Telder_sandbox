using System;
using System.Collections.Generic;

using UnityEngine;

using Game.World.Lighting;
using Game.World.Rendering;

namespace Game.World.Effects
{
    /// <summary>
    /// Pixel/voxel-like destruction debris for blocks.
    ///
    /// Key ideas:
    /// - uses BlockRenderer.DrawBlock(), therefore the debris
    ///   uses exactly the same block texture as the world;
    /// - no Rigidbody2D per shard;
    /// - no Collider2D per shard;
    /// - one central Update loop;
    /// - GameObjects/SpriteRenderers are pooled;
    /// - source block textures are cached by block ID;
    /// - shards use lightweight custom gravity/bounce.
    /// </summary>
    public sealed class BlockBreakDebrisSystem :
        MonoBehaviour
    {
        private static BlockBreakDebrisSystem
            instance;


        // =====================================================
        // SETTINGS
        // =====================================================

        private const int MinFragments =
            10;

        private const int MaxFragments =
            16;

        private const int MaxActiveFragments =
            240;


        private const float MinLifetime =
            0.65f;

        private const float MaxLifetime =
            1.15f;


        private const float Gravity =
            11.5f;


        private const float LinearDrag =
            0.65f;


        private const float AngularDrag =
            0.45f;


        private const float MinOutwardSpeed =
            1.25f;

        private const float MaxOutwardSpeed =
            3.40f;


        private const float MinUpwardKick =
            1.20f;

        private const float MaxUpwardKick =
            3.20f;


        private const float MinAngularSpeed =
            160f;

        private const float MaxAngularSpeed =
            620f;


        private const float CollisionStartAge =
            0.06f;


        private const int ForegroundSortingOrder =
            8;

        private const int BackgroundSortingOrder =
            3;


        // =====================================================
        // CACHE
        // =====================================================

        private readonly Dictionary<
            ushort,
            Texture2D
        >
        blockTextureCache =
            new Dictionary<
                ushort,
                Texture2D
            >();


        // =====================================================
        // POOL
        // =====================================================

        private readonly List<ParticleSlot>
            active =
            new List<ParticleSlot>(
                MaxActiveFragments
            );


        private readonly Stack<ParticleSlot>
            pool =
            new Stack<ParticleSlot>(
                MaxActiveFragments
            );


        // =====================================================
        // BOOTSTRAP
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void RuntimeBootstrap()
        {
            EnsureInstance();
        }


        private static void EnsureInstance()
        {
            if (instance != null)
                return;


            instance =
                UnityEngine.Object.FindObjectOfType<
                    BlockBreakDebrisSystem
                >();


            if (instance != null)
                return;


            GameObject root =
                new GameObject(
                    "[Runtime] Block Break Debris"
                );


            instance =
                root.AddComponent<
                    BlockBreakDebrisSystem
                >();
        }


        private void Awake()
        {
            if (
                instance != null &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );
        }


        // =====================================================
        // PUBLIC
        // =====================================================

        /// <summary>
        /// Call this AFTER the world data successfully changed
        /// from oldBlockID to air, but BEFORE the block renderer
        /// redraws that block.
        ///
        /// The visual source itself does not depend on the chunk
        /// texture, so calling slightly later is also safe.
        /// </summary>
        public static void Emit(
            Game.World.World world,
            int worldX,
            int worldY,
            ushort oldBlockID,
            bool background
        )
        {
            if (oldBlockID == 0)
                return;


            EnsureInstance();


            if (instance == null)
                return;


            instance.EmitInternal(
                world,
                worldX,
                worldY,
                oldBlockID,
                background
            );
        }


        public static void ClearAll()
        {
            if (instance == null)
                return;


            instance.ClearActive();
        }


        // =====================================================
        // EMIT
        // =====================================================

        private void EmitInternal(
            Game.World.World world,
            int worldX,
            int worldY,
            ushort blockID,
            bool background
        )
        {
            Texture2D texture =
                GetOrCreateBlockTexture(
                    blockID
                );


            if (texture == null)
                return;


            int pixelSize =
                BlockRenderer.BlockPixelSize;


            if (pixelSize <= 0)
                return;


            int requestedFragments =
                UnityEngine.Random.Range(
                    MinFragments,
                    MaxFragments + 1
                );


            List<PixelRect> pieces =
                BuildFragments(
                    pixelSize,
                    requestedFragments
                );


            if (
                pieces == null ||
                pieces.Count == 0
            )
            {
                return;
            }


            Vector2 blockCenter =
                new Vector2(
                    worldX + 0.5f,
                    worldY + 0.5f
                );


            Color lightColor =
                CalculateLightColor(
                    world,
                    worldX,
                    worldY,
                    background
                );


            for (
                int i = 0;
                i < pieces.Count;
                i++
            )
            {
                PixelRect piece =
                    pieces[i];


                if (
                    !HasVisiblePixels(
                        texture,
                        piece
                    )
                )
                {
                    continue;
                }


                SpawnPiece(
                    world,
                    texture,
                    piece,
                    pixelSize,
                    worldX,
                    worldY,
                    blockCenter,
                    lightColor,
                    background
                );
            }
        }


        // =====================================================
        // BLOCK TEXTURE
        // =====================================================

        private Texture2D GetOrCreateBlockTexture(
            ushort blockID
        )
        {
            if (
                blockTextureCache.TryGetValue(
                    blockID,
                    out Texture2D cached
                ) &&
                cached != null
            )
            {
                return cached;
            }


            int size =
                BlockRenderer.BlockPixelSize;


            if (size <= 0)
                return null;


            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                "DebrisBlock_" +
                blockID;


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            Color[] clear =
                new Color[
                    size *
                    size
                ];


            for (
                int i = 0;
                i < clear.Length;
                i++
            )
            {
                clear[i] =
                    Color.clear;
            }


            texture.SetPixels(
                clear
            );


            // -------------------------------------------------
            // This is the SAME renderer used by ChunkRenderer.
            // -------------------------------------------------

            BlockRenderer.DrawBlock(
                texture,
                0,
                0,
                blockID
            );


            texture.Apply(
                false,
                false
            );


            blockTextureCache[
                blockID
            ] =
                texture;


            return texture;
        }


        // =====================================================
        // FRACTURE
        // =====================================================

        private List<PixelRect> BuildFragments(
            int size,
            int targetCount
        )
        {
            List<PixelRect> pieces =
                new List<PixelRect>(
                    targetCount
                );


            pieces.Add(
                new PixelRect(
                    0,
                    0,
                    size,
                    size
                )
            );


            int safety =
                128;


            while (
                pieces.Count <
                targetCount &&
                safety-- >
                0
            )
            {
                int splitIndex =
                    FindBestSplitCandidate(
                        pieces
                    );


                if (splitIndex < 0)
                    break;


                PixelRect source =
                    pieces[
                        splitIndex
                    ];


                if (
                    !TrySplit(
                        source,
                        out PixelRect first,
                        out PixelRect second
                    )
                )
                {
                    break;
                }


                pieces[
                    splitIndex
                ] =
                    first;


                pieces.Add(
                    second
                );
            }


            return pieces;
        }


        private int FindBestSplitCandidate(
            List<PixelRect> pieces
        )
        {
            int bestIndex =
                -1;


            int bestScore =
                -1;


            for (
                int i = 0;
                i < pieces.Count;
                i++
            )
            {
                PixelRect rect =
                    pieces[i];


                bool canSplitX =
                    rect.Width >=
                    4;


                bool canSplitY =
                    rect.Height >=
                    4;


                if (
                    !canSplitX &&
                    !canSplitY
                )
                {
                    continue;
                }


                int area =
                    rect.Width *
                    rect.Height;


                // Slight randomness prevents the same regular
                // fracture pattern every time.
                int score =
                    area *
                    100 +
                    UnityEngine.Random.Range(
                        0,
                        50
                    );


                if (
                    score >
                    bestScore
                )
                {
                    bestScore =
                        score;

                    bestIndex =
                        i;
                }
            }


            return bestIndex;
        }


        private bool TrySplit(
            PixelRect source,
            out PixelRect first,
            out PixelRect second
        )
        {
            first =
                source;


            second =
                source;


            bool canX =
                source.Width >=
                4;


            bool canY =
                source.Height >=
                4;


            if (
                !canX &&
                !canY
            )
            {
                return false;
            }


            bool verticalSplit;


            if (
                canX &&
                canY
            )
            {
                if (
                    source.Width >
                    source.Height
                )
                {
                    verticalSplit =
                        UnityEngine.Random.value >
                        0.18f;
                }
                else if (
                    source.Height >
                    source.Width
                )
                {
                    verticalSplit =
                        UnityEngine.Random.value <
                        0.18f;
                }
                else
                {
                    verticalSplit =
                        UnityEngine.Random.value >
                        0.5f;
                }
            }
            else
            {
                verticalSplit =
                    canX;
            }


            if (verticalSplit)
            {
                int min =
                    2;


                int max =
                    source.Width -
                    2;


                if (max < min)
                    return false;


                int split =
                    UnityEngine.Random.Range(
                        min,
                        max + 1
                    );


                first =
                    new PixelRect(
                        source.X,
                        source.Y,
                        split,
                        source.Height
                    );


                second =
                    new PixelRect(
                        source.X + split,
                        source.Y,
                        source.Width - split,
                        source.Height
                    );


                return true;
            }


            {
                int min =
                    2;


                int max =
                    source.Height -
                    2;


                if (max < min)
                    return false;


                int split =
                    UnityEngine.Random.Range(
                        min,
                        max + 1
                    );


                first =
                    new PixelRect(
                        source.X,
                        source.Y,
                        source.Width,
                        split
                    );


                second =
                    new PixelRect(
                        source.X,
                        source.Y + split,
                        source.Width,
                        source.Height - split
                    );


                return true;
            }
        }


        private bool HasVisiblePixels(
            Texture2D texture,
            PixelRect rect
        )
        {
            Color[] pixels =
                texture.GetPixels(
                    rect.X,
                    rect.Y,
                    rect.Width,
                    rect.Height
                );


            for (
                int i = 0;
                i < pixels.Length;
                i++
            )
            {
                if (
                    pixels[i].a >
                    0.08f
                )
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // SPAWN PIECE
        // =====================================================

        private void SpawnPiece(
            Game.World.World world,
            Texture2D texture,
            PixelRect piece,
            int pixelsPerUnit,
            int worldX,
            int worldY,
            Vector2 blockCenter,
            Color lightColor,
            bool background
        )
        {
            while (
                active.Count >=
                MaxActiveFragments
            )
            {
                ReleaseAt(
                    0
                );
            }


            ParticleSlot slot =
                GetSlot();


            Rect spriteRect =
                new Rect(
                    piece.X,
                    piece.Y,
                    piece.Width,
                    piece.Height
                );


            Sprite sprite =
                Sprite.Create(
                    texture,
                    spriteRect,
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect
                );


            sprite.name =
                "Debris_" +
                piece.Width +
                "x" +
                piece.Height;


            slot.Sprite =
                sprite;


            slot.Renderer.sprite =
                sprite;


            slot.Renderer.sortingOrder =
                background
                    ? BackgroundSortingOrder
                    : ForegroundSortingOrder;


            slot.BaseColor =
                lightColor;


            slot.Renderer.color =
                lightColor;


            float centerPixelX =
                piece.X +
                piece.Width *
                0.5f;


            float centerPixelY =
                piece.Y +
                piece.Height *
                0.5f;


            Vector2 position =
                new Vector2(
                    worldX +
                    centerPixelX /
                    pixelsPerUnit,

                    worldY +
                    centerPixelY /
                    pixelsPerUnit
                );


            slot.Position =
                position;


            slot.Transform.position =
                new Vector3(
                    position.x,
                    position.y,
                    0f
                );


            slot.Transform.rotation =
                Quaternion.identity;


            slot.Transform.localScale =
                Vector3.one;


            Vector2 outward =
                position -
                blockCenter;


            if (
                outward.sqrMagnitude <
                0.0001f
            )
            {
                outward =
                    UnityEngine.Random.insideUnitCircle;
            }


            outward.Normalize();


            Vector2 random =
                UnityEngine.Random
                    .insideUnitCircle *
                0.85f;


            float outwardSpeed =
                UnityEngine.Random.Range(
                    MinOutwardSpeed,
                    MaxOutwardSpeed
                );


            float upwardKick =
                UnityEngine.Random.Range(
                    MinUpwardKick,
                    MaxUpwardKick
                );


            Vector2 velocity =
                outward *
                outwardSpeed +
                random;


            velocity.y +=
                upwardKick;


            // Background debris should feel lighter/subtler.
            if (background)
            {
                velocity *=
                    0.72f;
            }


            slot.Velocity =
                velocity;


            float angular =
                UnityEngine.Random.Range(
                    MinAngularSpeed,
                    MaxAngularSpeed
                );


            if (
                UnityEngine.Random.value <
                0.5f
            )
            {
                angular =
                    -angular;
            }


            slot.AngularVelocity =
                angular;


            slot.Age =
                0f;


            slot.Lifetime =
                UnityEngine.Random.Range(
                    MinLifetime,
                    MaxLifetime
                );


            if (background)
            {
                slot.Lifetime *=
                    0.82f;
            }


            slot.World =
                world;


            slot.Active =
                true;


            slot.GameObject.SetActive(
                true
            );


            active.Add(
                slot
            );
        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void Update()
        {
            float dt =
                Time.deltaTime;


            if (dt <= 0f)
                return;


            for (
                int i =
                    active.Count - 1;
                i >= 0;
                i--
            )
            {
                ParticleSlot slot =
                    active[i];


                slot.Age +=
                    dt;


                if (
                    slot.Age >=
                    slot.Lifetime
                )
                {
                    ReleaseAt(
                        i
                    );

                    continue;
                }


                UpdateParticle(
                    slot,
                    dt
                );
            }
        }


        private void UpdateParticle(
            ParticleSlot slot,
            float dt
        )
        {
            slot.Velocity.y -=
                Gravity *
                dt;


            float linearDrag =
                Mathf.Exp(
                    -LinearDrag *
                    dt
                );


            slot.Velocity.x *=
                linearDrag;


            slot.AngularVelocity *=
                Mathf.Exp(
                    -AngularDrag *
                    dt
                );


            Vector2 position =
                slot.Position;


            // =================================================
            // LIGHTWEIGHT COLLISION
            // =================================================

            if (
                slot.Age >
                CollisionStartAge &&
                slot.World !=
                null
            )
            {
                // X axis

                float nextX =
                    position.x +
                    slot.Velocity.x *
                    dt;


                if (
                    IsSolid(
                        slot.World,
                        nextX,
                        position.y
                    )
                )
                {
                    slot.Velocity.x *=
                        -0.28f;
                }
                else
                {
                    position.x =
                        nextX;
                }


                // Y axis

                float nextY =
                    position.y +
                    slot.Velocity.y *
                    dt;


                if (
                    IsSolid(
                        slot.World,
                        position.x,
                        nextY
                    )
                )
                {
                    if (
                        slot.Velocity.y <
                        0f
                    )
                    {
                        slot.Velocity.y =
                            -slot.Velocity.y *
                            0.24f;


                        slot.Velocity.x *=
                            0.72f;
                    }
                    else
                    {
                        slot.Velocity.y *=
                            -0.18f;
                    }
                }
                else
                {
                    position.y =
                        nextY;
                }
            }
            else
            {
                position +=
                    slot.Velocity *
                    dt;
            }


            slot.Position =
                position;


            slot.Transform.position =
                new Vector3(
                    position.x,
                    position.y,
                    0f
                );


            slot.Transform.Rotate(
                0f,
                0f,
                slot.AngularVelocity *
                dt
            );


            // =================================================
            // FADE + SHRINK
            // =================================================

            float life01 =
                Mathf.Clamp01(
                    slot.Age /
                    slot.Lifetime
                );


            float fade =
                1f;


            if (
                life01 >
                0.58f
            )
            {
                fade =
                    1f -
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        (
                            life01 -
                            0.58f
                        )
                        /
                        0.42f
                    );
            }


            Color color =
                slot.BaseColor;


            color.a =
                fade;


            slot.Renderer.color =
                color;


            float scale =
                1f;


            if (
                life01 >
                0.78f
            )
            {
                scale =
                    Mathf.Lerp(
                        1f,
                        0.45f,
                        Mathf.InverseLerp(
                            0.78f,
                            1f,
                            life01
                        )
                    );
            }


            slot.Transform.localScale =
                new Vector3(
                    scale,
                    scale,
                    1f
                );
        }


        private bool IsSolid(
            Game.World.World world,
            float x,
            float y
        )
        {
            int blockX =
                Mathf.FloorToInt(
                    x
                );


            int blockY =
                Mathf.FloorToInt(
                    y
                );


            return
                world.GetBlock(
                    blockX,
                    blockY
                ) !=
                0;
        }


        // =====================================================
        // LIGHTING
        // =====================================================

        private Color CalculateLightColor(
            Game.World.World world,
            int worldX,
            int worldY,
            bool background
        )
        {
            float layerBrightness =
                background
                    ? 0.70f
                    : 1f;


            if (
                Shader.GetGlobalFloat(
                    "_DebugFullBright"
                ) >
                0.5f
            )
            {
                return
                    new Color(
                        layerBrightness,
                        layerBrightness,
                        layerBrightness,
                        1f
                    );
            }


            if (world == null)
            {
                return
                    new Color(
                        layerBrightness,
                        layerBrightness,
                        layerBrightness,
                        1f
                    );
            }


            LightNode light =
                world.GetLight(
                    worldX,
                    worldY
                );


            float sunlight =
                light.Sun /
                15f;


            float red =
                Mathf.Max(
                    sunlight,
                    light.R /
                    15f
                );


            float green =
                Mathf.Max(
                    sunlight,
                    light.G /
                    15f
                );


            float blue =
                Mathf.Max(
                    sunlight,
                    light.B /
                    15f
                );


            const float ambient =
                0.07f;


            red =
                Mathf.Max(
                    red,
                    ambient
                );


            green =
                Mathf.Max(
                    green,
                    ambient
                );


            blue =
                Mathf.Max(
                    blue,
                    ambient
                );


            return
                new Color(
                    Mathf.Clamp01(
                        red *
                        layerBrightness
                    ),

                    Mathf.Clamp01(
                        green *
                        layerBrightness
                    ),

                    Mathf.Clamp01(
                        blue *
                        layerBrightness
                    ),

                    1f
                );
        }


        // =====================================================
        // POOL
        // =====================================================

        private ParticleSlot GetSlot()
        {
            if (
                pool.Count >
                0
            )
            {
                return pool.Pop();
            }


            GameObject obj =
                new GameObject(
                    "Block Debris"
                );


            obj.transform.SetParent(
                transform,
                false
            );


            SpriteRenderer renderer =
                obj.AddComponent<
                    SpriteRenderer
                >();


            renderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode
                    .Off;


            renderer.receiveShadows =
                false;


            obj.SetActive(
                false
            );


            return
                new ParticleSlot
                {
                    GameObject =
                        obj,

                    Transform =
                        obj.transform,

                    Renderer =
                        renderer
                };
        }


        private void ReleaseAt(
            int index
        )
        {
            ParticleSlot slot =
                active[index];


            active.RemoveAt(
                index
            );


            ReleaseSlot(
                slot
            );
        }


        private void ReleaseSlot(
            ParticleSlot slot
        )
        {
            if (slot == null)
                return;


            slot.Active =
                false;


            slot.World =
                null;


            slot.Renderer.sprite =
                null;


            if (
                slot.Sprite !=
                null
            )
            {
                Destroy(
                    slot.Sprite
                );


                slot.Sprite =
                    null;
            }


            slot.GameObject.SetActive(
                false
            );


            pool.Push(
                slot
            );
        }


        private void ClearActive()
        {
            for (
                int i =
                    active.Count - 1;
                i >= 0;
                i--
            )
            {
                ReleaseAt(
                    i
                );
            }
        }


        private void OnDestroy()
        {
            ClearActive();


            foreach (
                KeyValuePair<
                    ushort,
                    Texture2D
                >
                pair
                in blockTextureCache
            )
            {
                if (
                    pair.Value !=
                    null
                )
                {
                    Destroy(
                        pair.Value
                    );
                }
            }


            blockTextureCache.Clear();


            if (instance == this)
            {
                instance =
                    null;
            }
        }


        // =====================================================
        // DATA
        // =====================================================

        private sealed class ParticleSlot
        {
            public GameObject
                GameObject;

            public Transform
                Transform;

            public SpriteRenderer
                Renderer;

            public Sprite
                Sprite;


            public Game.World.World
                World;


            public Vector2
                Position;

            public Vector2
                Velocity;


            public float
                AngularVelocity;

            public float
                Age;

            public float
                Lifetime;


            public Color
                BaseColor;


            public bool
                Active;
        }


        private readonly struct PixelRect
        {
            public readonly int X;
            public readonly int Y;

            public readonly int Width;
            public readonly int Height;


            public PixelRect(
                int x,
                int y,
                int width,
                int height
            )
            {
                X =
                    x;

                Y =
                    y;

                Width =
                    width;

                Height =
                    height;
            }
        }
    }
}
