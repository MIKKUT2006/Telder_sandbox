using System.Collections.Generic;

using UnityEngine;

using Game.World.Lighting;
using Game.World.Rendering;


namespace Game.World.Effects
{
    /// <summary>
    /// Lightweight pixel debris for full block breaks and repeated mining hits.
    ///
    /// Full break:
    /// - fewer fragments;
    /// - smaller scale;
    /// - much shorter lifetime.
    ///
    /// Mining hit:
    /// - 2-4 tiny fragments;
    /// - extremely short lifetime;
    /// - no collision checks.
    /// </summary>
    public sealed class BlockBreakDebrisSystem :
        MonoBehaviour
    {
        private static BlockBreakDebrisSystem instance;


        // =====================================================
        // GLOBAL
        // =====================================================

        private const int MaxActiveFragments =
            96;


        // =====================================================
        // BREAK PRESET
        // =====================================================

        private const int BreakMinFragments =
            6;

        private const int BreakMaxFragments =
            9;

        private const int BreakMinFracturePieces =
            12;

        private const int BreakMaxFracturePieces =
            18;

        private const float BreakMinLifetime =
            0.18f;

        private const float BreakMaxLifetime =
            0.34f;

        private const float BreakMinScale =
            0.58f;

        private const float BreakMaxScale =
            0.78f;

        private const float BreakMinOutwardSpeed =
            0.85f;

        private const float BreakMaxOutwardSpeed =
            2.35f;

        private const float BreakMinUpwardKick =
            0.75f;

        private const float BreakMaxUpwardKick =
            2.15f;


        // =====================================================
        // MINING HIT PRESET
        // =====================================================

        private const int HitMinFragments =
            2;

        private const int HitMaxFragments =
            4;

        private const int HitMinFracturePieces =
            18;

        private const int HitMaxFracturePieces =
            26;

        private const float HitMinLifetime =
            0.08f;

        private const float HitMaxLifetime =
            0.17f;

        private const float HitMinScale =
            0.34f;

        private const float HitMaxScale =
            0.52f;

        private const float HitMinOutwardSpeed =
            0.55f;

        private const float HitMaxOutwardSpeed =
            1.45f;

        private const float HitMinUpwardKick =
            0.35f;

        private const float HitMaxUpwardKick =
            1.15f;


        // =====================================================
        // PHYSICS
        // =====================================================

        private const float Gravity =
            11.5f;

        private const float LinearDrag =
            1.15f;

        private const float AngularDrag =
            1.10f;

        private const float MinAngularSpeed =
            100f;

        private const float MaxAngularSpeed =
            420f;

        private const float CollisionStartAge =
            0.035f;

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
                UnityEngine.Object
                    .FindObjectOfType<
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
        /// Existing full-break API.
        /// Existing callers do not need to change.
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
                background,
                false
            );
        }


        /// <summary>
        /// Tiny repeated debris burst while the block is still being mined.
        /// </summary>
        public static void EmitMiningHit(
            Game.World.World world,
            int worldX,
            int worldY,
            ushort blockID,
            bool background
        )
        {
            if (blockID == 0)
                return;

            EnsureInstance();

            if (instance == null)
                return;

            instance.EmitInternal(
                world,
                worldX,
                worldY,
                blockID,
                background,
                true
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
            bool background,
            bool miningHit
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

            int fractureCount =
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinFracturePieces,
                        HitMaxFracturePieces + 1
                    )
                    : UnityEngine.Random.Range(
                        BreakMinFracturePieces,
                        BreakMaxFracturePieces + 1
                    );

            int emitCount =
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinFragments,
                        HitMaxFragments + 1
                    )
                    : UnityEngine.Random.Range(
                        BreakMinFragments,
                        BreakMaxFragments + 1
                    );

            List<PixelRect> pieces =
                BuildFragments(
                    pixelSize,
                    fractureCount
                );

            if (
                pieces == null ||
                pieces.Count == 0
            )
            {
                return;
            }

            Shuffle(
                pieces
            );

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

            int spawned =
                0;

            for (
                int i = 0;
                i < pieces.Count &&
                spawned < emitCount;
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
                    background,
                    miningHit
                );

                spawned++;
            }
        }


        private void Shuffle(
            List<PixelRect> pieces
        )
        {
            for (
                int i =
                    pieces.Count - 1;
                i > 0;
                i--
            )
            {
                int j =
                    UnityEngine.Random.Range(
                        0,
                        i + 1
                    );

                PixelRect temp =
                    pieces[i];

                pieces[i] =
                    pieces[j];

                pieces[j] =
                    temp;
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
                192;

            while (
                pieces.Count <
                targetCount &&
                safety-- > 0
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
                    rect.Width >= 4;

                bool canSplitY =
                    rect.Height >= 4;

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
                source.Width >= 4;

            bool canY =
                source.Height >= 4;

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
            bool background,
            bool miningHit
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
                miningHit
                    ? "MiningHitDebris"
                    : "BlockDebris";

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

            float startScale =
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinScale,
                        HitMaxScale
                    )
                    : UnityEngine.Random.Range(
                        BreakMinScale,
                        BreakMaxScale
                    );

            if (background)
            {
                startScale *=
                    0.82f;
            }

            slot.BaseScale =
                startScale;

            slot.Transform.localScale =
                new Vector3(
                    startScale,
                    startScale,
                    1f
                );

            Vector2 outward =
                position -
                blockCenter;

            if (
                outward.sqrMagnitude <
                0.0001f
            )
            {
                outward =
                    UnityEngine.Random
                        .insideUnitCircle;
            }

            outward.Normalize();

            Vector2 random =
                UnityEngine.Random
                    .insideUnitCircle *
                (
                    miningHit
                        ? 0.38f
                        : 0.62f
                );

            float outwardSpeed =
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinOutwardSpeed,
                        HitMaxOutwardSpeed
                    )
                    : UnityEngine.Random.Range(
                        BreakMinOutwardSpeed,
                        BreakMaxOutwardSpeed
                    );

            float upwardKick =
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinUpwardKick,
                        HitMaxUpwardKick
                    )
                    : UnityEngine.Random.Range(
                        BreakMinUpwardKick,
                        BreakMaxUpwardKick
                    );

            Vector2 velocity =
                outward *
                outwardSpeed +
                random;

            velocity.y +=
                upwardKick;

            if (background)
            {
                velocity *=
                    0.68f;
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
                miningHit
                    ? UnityEngine.Random.Range(
                        HitMinLifetime,
                        HitMaxLifetime
                    )
                    : UnityEngine.Random.Range(
                        BreakMinLifetime,
                        BreakMaxLifetime
                    );

            if (background)
            {
                slot.Lifetime *=
                    0.78f;
            }

            slot.World =
                world;

            // Mining-hit particles are too short-lived to justify
            // per-fragment collision checks.
            slot.UseCollision =
                !miningHit;

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

            if (
                slot.UseCollision &&
                slot.Age >
                CollisionStartAge &&
                slot.World != null
            )
            {
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
                        -0.24f;
                }
                else
                {
                    position.x =
                        nextX;
                }

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
                            0.20f;

                        slot.Velocity.x *=
                            0.66f;
                    }
                    else
                    {
                        slot.Velocity.y *=
                            -0.15f;
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

            float life01 =
                Mathf.Clamp01(
                    slot.Age /
                    slot.Lifetime
                );

            float fade =
                1f;

            if (
                life01 >
                0.32f
            )
            {
                fade =
                    1f -
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        (
                            life01 -
                            0.32f
                        ) /
                        0.68f
                    );
            }

            Color color =
                slot.BaseColor;

            color.a =
                fade;

            slot.Renderer.color =
                color;

            float shrink =
                1f;

            if (
                life01 >
                0.50f
            )
            {
                shrink =
                    Mathf.Lerp(
                        1f,
                        0.25f,
                        Mathf.InverseLerp(
                            0.50f,
                            1f,
                            life01
                        )
                    );
            }

            float finalScale =
                slot.BaseScale *
                shrink;

            slot.Transform.localScale =
                new Vector3(
                    finalScale,
                    finalScale,
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
                return
                    pool.Pop();
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

            slot.World =
                null;

            slot.UseCollision =
                false;

            slot.Renderer.sprite =
                null;

            if (
                slot.Sprite != null
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
                    pair.Value != null
                )
                {
                    Destroy(
                        pair.Value
                    );
                }
            }

            blockTextureCache.Clear();

            if (
                instance ==
                this
            )
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
            public GameObject GameObject;
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Sprite Sprite;

            public Game.World.World World;

            public Vector2 Position;
            public Vector2 Velocity;

            public float AngularVelocity;
            public float Age;
            public float Lifetime;
            public float BaseScale;

            public Color BaseColor;

            public bool UseCollision;
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
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }
    }
}
