using System;
using System.Collections.Generic;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.World.Collision;
using Game.World.Rendering;


namespace Game.World.Physics
{
    [DefaultExecutionOrder(5000)]
    public sealed class FallingBlockSystem :
        MonoBehaviour
    {
        private static FallingBlockSystem instance;


        private readonly Queue<Cell>
            active =
                new Queue<Cell>();


        private readonly HashSet<long>
            queued =
                new HashSet<long>();


        private readonly Queue<Chunk>
            warmupChunks =
                new Queue<Chunk>();


        private readonly HashSet<long>
            registeredChunks =
                new HashSet<long>();


        private readonly Dictionary<ushort, bool>
            fallingBlockCache =
                new Dictionary<ushort, bool>();


        private readonly Dictionary<long, Chunk>
            renderDirty =
                new Dictionary<long, Chunk>();


        private readonly Dictionary<long, Chunk>
            collisionDirty =
                new Dictionary<long, Chunk>();


        private readonly Dictionary<int, int>
            lightDirtyColumns =
                new Dictionary<int, int>();


        [Header("Simulation")]

        [SerializeField]
        private float simulationInterval =
            0.033f;


        [SerializeField]
        private int maxMovesPerStep =
            128;


        [Header("Batch refresh")]

        [SerializeField]
        private float renderRefreshInterval =
            0.05f;


        [SerializeField]
        private float collisionRefreshInterval =
            0.12f;


        [SerializeField]
        private float lightRefreshInterval =
            0.12f;


        [SerializeField]
        private int collisionChunkRadius =
            2;


        [Header("New chunk scan")]

        [SerializeField]
        private int maxWarmupChunksPerFrame =
            1;


        private Game.World.WorldManager manager;

        private Game.World.World world;

        private ChunkRenderer renderer;

        private ChunkCollision collision;

        private Transform player;


        private float nextSimulation;

        private float nextRenderRefresh;

        private float nextCollisionRefresh;

        private float nextLightRefresh;


        private struct Cell
        {
            public int X;
            public int Y;


            public Cell(
                int x,
                int y
            )
            {
                X = x;
                Y = y;
            }
        }


        // =====================================================
        // BOOTSTRAP
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Bootstrap()
        {
            EnsureInstance();
        }


        public static void EnsureInstance()
        {
            if (instance != null)
                return;


            Game.World.WorldManager wm =
                Game.World.WorldManager.Instance;


            if (wm == null)
            {
                wm =
                    UnityEngine.Object
                        .FindObjectOfType<
                            Game.World.WorldManager
                        >();
            }


            if (wm == null)
                return;


            FallingBlockSystem existing =
                wm.GetComponent<
                    FallingBlockSystem
                >();


            if (existing != null)
            {
                instance =
                    existing;

                return;
            }


            instance =
                wm.gameObject
                    .AddComponent<
                        FallingBlockSystem
                    >();
        }


        public static void RegisterChunk(
            Chunk chunk
        )
        {
            EnsureInstance();


            if (
                instance == null ||
                chunk == null
            )
            {
                return;
            }


            long key =
                Pack(
                    chunk.X,
                    chunk.Y
                );


            if (
                !instance
                    .registeredChunks
                    .Add(
                        key
                    )
            )
            {
                return;
            }


            instance
                .warmupChunks
                .Enqueue(
                    chunk
                );
        }


        public static void UnregisterChunk(
            int chunkX,
            int chunkY
        )
        {
            if (instance == null)
                return;


            instance
                .registeredChunks
                .Remove(
                    Pack(
                        chunkX,
                        chunkY
                    )
                );
        }


        public static void NotifyCellChanged(
            int worldX,
            int worldY
        )
        {
            EnsureInstance();


            if (instance == null)
                return;


            instance.Enqueue(
                worldX,
                worldY + 1
            );


            instance.Enqueue(
                worldX,
                worldY + 2
            );
        }


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            if (
                instance != null &&
                instance != this
            )
            {
                Destroy(this);

                return;
            }


            instance =
                this;


            ResolveReferences();
        }


        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }


        private void Update()
        {
            ResolveReferences();


            if (
                manager == null ||
                world == null
            )
            {
                return;
            }


            WarmupChunks();


            float now =
                Time.time;


            if (
                now >=
                nextSimulation
            )
            {
                nextSimulation =
                    now +
                    Mathf.Max(
                        0.015f,
                        simulationInterval
                    );


                SimulateBatch();
            }


            if (
                now >=
                nextRenderRefresh
            )
            {
                nextRenderRefresh =
                    now +
                    Mathf.Max(
                        0.025f,
                        renderRefreshInterval
                    );


                FlushRender();
            }


            if (
                now >=
                nextCollisionRefresh
            )
            {
                nextCollisionRefresh =
                    now +
                    Mathf.Max(
                        0.05f,
                        collisionRefreshInterval
                    );


                FlushCollision();
            }


            if (
                now >=
                nextLightRefresh
            )
            {
                nextLightRefresh =
                    now +
                    Mathf.Max(
                        0.05f,
                        lightRefreshInterval
                    );


                FlushLighting();
            }
        }


        // =====================================================
        // REFERENCES
        // =====================================================

        private void ResolveReferences()
        {
            if (manager == null)
            {
                manager =
                    Game.World
                        .WorldManager
                        .Instance;


                if (manager == null)
                {
                    manager =
                        UnityEngine.Object
                            .FindObjectOfType<
                                Game.World.WorldManager
                            >();
                }
            }


            if (manager == null)
                return;


            if (world == null)
            {
                world =
                    manager.GetWorld();
            }


            if (renderer == null)
            {
                renderer =
                    manager.GetChunkRenderer();
            }


            if (collision == null)
            {
                collision =
                    manager.GetChunkCollision();
            }


            if (player == null)
            {
                try
                {
                    GameObject tagged =
                        GameObject.FindGameObjectWithTag(
                            "Player"
                        );


                    if (tagged != null)
                    {
                        player =
                            tagged.transform;
                    }
                }
                catch
                {
                }


                if (player == null)
                {
                    GameObject named =
                        GameObject.Find(
                            "Player"
                        );


                    if (named != null)
                    {
                        player =
                            named.transform;
                    }
                }
            }
        }


        // =====================================================
        // WARMUP
        // =====================================================

        private void WarmupChunks()
        {
            int budget =
                Mathf.Max(
                    1,
                    maxWarmupChunksPerFrame
                );


            while (
                budget-- > 0 &&
                warmupChunks.Count > 0
            )
            {
                Chunk chunk =
                    warmupChunks.Dequeue();


                if (chunk == null)
                    continue;


                Chunk loaded =
                    world.GetChunk(
                        chunk.X,
                        chunk.Y
                    );


                if (loaded != chunk)
                    continue;


                int baseX =
                    chunk.X *
                    Chunk.SizeX;


                int baseY =
                    chunk.Y *
                    Chunk.SizeY;


                for (
                    int localX = 0;
                    localX < Chunk.SizeX;
                    localX++
                )
                {
                    for (
                        int localY = 0;
                        localY < Chunk.SizeY;
                        localY++
                    )
                    {
                        ushort blockID =
                            chunk.GetBlock(
                                localX,
                                localY
                            );


                        if (
                            blockID == 0 ||
                            !IsFallingBlock(
                                blockID
                            )
                        )
                        {
                            continue;
                        }


                        Enqueue(
                            baseX +
                            localX,

                            baseY +
                            localY
                        );
                    }
                }
            }
        }


        // =====================================================
        // SIMULATION
        // =====================================================

        private void SimulateBatch()
        {
            if (active.Count == 0)
                return;


            int count =
                Mathf.Min(
                    active.Count,
                    Mathf.Max(
                        1,
                        maxMovesPerStep
                    )
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                Cell cell =
                    active.Dequeue();


                queued.Remove(
                    Pack(
                        cell.X,
                        cell.Y
                    )
                );


                TryMoveDown(
                    cell.X,
                    cell.Y
                );
            }
        }


        private void TryMoveDown(
            int worldX,
            int worldY
        )
        {
            ushort blockID =
                world.GetBlock(
                    worldX,
                    worldY
                );


            if (
                blockID == 0 ||
                !IsFallingBlock(
                    blockID
                )
            )
            {
                return;
            }


            int targetY =
                worldY -
                1;


            if (
                world.GetBlock(
                    worldX,
                    targetY
                )
                != 0
            )
            {
                return;
            }


            Chunk sourceChunk =
                ResolveChunk(
                    worldX,
                    worldY,
                    out int sourceX,
                    out int sourceY
                );


            Chunk targetChunk =
                ResolveChunk(
                    worldX,
                    targetY,
                    out int targetX,
                    out int targetLocalY
                );


            if (
                sourceChunk == null ||
                targetChunk == null
            )
            {
                return;
            }


            // Direct data change:
            // no renderer/collision/light rebuild inside this hot loop.
            sourceChunk.SetBlock(
                sourceX,
                sourceY,
                0
            );


            targetChunk.SetBlock(
                targetX,
                targetLocalY,
                blockID
            );


            MarkDirty(
                sourceChunk
            );


            MarkDirty(
                targetChunk
            );


            lightDirtyColumns[
                worldX
            ] =
                targetY;


            // Continue falling on the next simulation tick.
            Enqueue(
                worldX,
                targetY
            );


            // Wake the block that just lost support.
            Enqueue(
                worldX,
                worldY + 1
            );
        }


        // =====================================================
        // BATCH OUTPUT
        // =====================================================

        private void MarkDirty(
            Chunk chunk
        )
        {
            if (chunk == null)
                return;


            long key =
                Pack(
                    chunk.X,
                    chunk.Y
                );


            renderDirty[
                key
            ] =
                chunk;


            collisionDirty[
                key
            ] =
                chunk;
        }


        private void FlushRender()
        {
            if (
                renderer == null ||
                renderDirty.Count == 0
            )
            {
                return;
            }


            foreach (
                KeyValuePair<long, Chunk> pair
                in renderDirty
            )
            {
                Chunk chunk =
                    pair.Value;


                if (
                    chunk != null &&
                    world.GetChunk(
                        chunk.X,
                        chunk.Y
                    )
                    ==
                    chunk
                )
                {
                    renderer.Render(
                        chunk
                    );
                }
            }


            renderDirty.Clear();
        }


        private void FlushCollision()
        {
            if (
                collision == null ||
                collisionDirty.Count == 0
            )
            {
                return;
            }


            int playerChunkX =
                0;


            int playerChunkY =
                0;


            bool havePlayer =
                player != null;


            if (havePlayer)
            {
                playerChunkX =
                    Mathf.FloorToInt(
                        player.position.x /
                        Chunk.SizeX
                    );


                playerChunkY =
                    Mathf.FloorToInt(
                        player.position.y /
                        Chunk.SizeY
                    );
            }


            foreach (
                KeyValuePair<long, Chunk> pair
                in collisionDirty
            )
            {
                Chunk chunk =
                    pair.Value;


                if (
                    chunk == null ||
                    world.GetChunk(
                        chunk.X,
                        chunk.Y
                    )
                    !=
                    chunk
                )
                {
                    continue;
                }


                if (havePlayer)
                {
                    int dx =
                        Mathf.Abs(
                            chunk.X -
                            playerChunkX
                        );


                    int dy =
                        Mathf.Abs(
                            chunk.Y -
                            playerChunkY
                        );


                    if (
                        dx >
                        collisionChunkRadius
                        ||
                        dy >
                        collisionChunkRadius
                    )
                    {
                        continue;
                    }
                }


                collision.BuildChunkCollision(
                    chunk
                );
            }


            collisionDirty.Clear();
        }


        private void FlushLighting()
        {
            if (
                lightDirtyColumns.Count == 0 ||
                world == null ||
                manager == null ||
                manager.GetSettings() == null
            )
            {
                return;
            }


            var light =
                world.GetLightEngine();


            if (light == null)
            {
                lightDirtyColumns.Clear();

                return;
            }


            int worldHeight =
                manager
                    .GetSettings()
                    .WorldHeight;


            foreach (
                KeyValuePair<int, int> pair
                in lightDirtyColumns
            )
            {
                light.RebuildAfterBlockChanged(
                    pair.Key,
                    pair.Value,
                    worldHeight
                );
            }


            lightDirtyColumns.Clear();
        }


        // =====================================================
        // BLOCK LOOKUP
        // =====================================================

        private bool IsFallingBlock(
            ushort blockID
        )
        {
            if (
                fallingBlockCache.TryGetValue(
                    blockID,
                    out bool cached
                )
            )
            {
                return cached;
            }


            bool result =
                false;


            try
            {
                ContentID id =
                    BlockIDRegistry.GetContentID(
                        blockID
                    );


                if (
                    BlockRegistry.Contains(
                        id
                    )
                )
                {
                    BlockDefinition definition =
                        BlockRegistry.Get(
                            id
                        );


                    if (
                        definition != null &&
                        definition.Tags != null
                    )
                    {
                        for (
                            int i = 0;
                            i < definition.Tags.Count;
                            i++
                        )
                        {
                            if (
                                string.Equals(
                                    definition.Tags[i],
                                    "falling",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                result =
                                    true;

                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                result =
                    false;
            }


            fallingBlockCache[
                blockID
            ] =
                result;


            return result;
        }


        // =====================================================
        // COORDINATES
        // =====================================================

        private Chunk ResolveChunk(
            int worldX,
            int worldY,
            out int localX,
            out int localY
        )
        {
            int chunkX =
                Mathf.FloorToInt(
                    worldX /
                    (float)Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    worldY /
                    (float)Chunk.SizeY
                );


            localX =
                worldX -
                chunkX *
                Chunk.SizeX;


            localY =
                worldY -
                chunkY *
                Chunk.SizeY;


            return
                world.GetChunk(
                    chunkX,
                    chunkY
                );
        }


        private void Enqueue(
            int x,
            int y
        )
        {
            long key =
                Pack(
                    x,
                    y
                );


            if (
                !queued.Add(
                    key
                )
            )
            {
                return;
            }


            active.Enqueue(
                new Cell(
                    x,
                    y
                )
            );
        }


        private static long Pack(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    ((long)x << 32)
                    ^
                    (uint)y;
            }
        }
    }
}
