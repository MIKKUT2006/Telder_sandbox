using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

using Game.World.Collision;
using Game.World.Furniture;
using Game.World.Generation;
using Game.World.Physics;
using Game.World.Rendering;


namespace Game.World.Loading
{
    public class ChunkLoader
    {
        // =====================================================
        // REFERENCES
        // =====================================================

        private readonly World world;

        private readonly WorldGenerator generator;

        private readonly WorldSettings settings;

        private readonly ChunkRenderer renderer;

        private readonly ChunkCollision collision;


        // =====================================================
        // LOADED / QUEUED
        // =====================================================

        private readonly HashSet<UnityEngine.Vector2Int>
            loadedChunks =
                new HashSet<UnityEngine.Vector2Int>();


        private readonly HashSet<UnityEngine.Vector2Int>
            collisionChunks =
                new HashSet<UnityEngine.Vector2Int>();


        private readonly Queue<UnityEngine.Vector2Int>
            loadQueue =
                new Queue<UnityEngine.Vector2Int>();


        private readonly HashSet<UnityEngine.Vector2Int>
            queuedChunks =
                new HashSet<UnityEngine.Vector2Int>();


        // =====================================================
        // PLAYER CHUNK
        // =====================================================

        private UnityEngine.Vector2Int currentPlayerChunk;

        private bool initialized;


        // =====================================================
        // PERFORMANCE SETTINGS
        // =====================================================

        // More than 1 chunk can finish in a frame IF there is budget.
        // The time budget is the real limiter.
        private const int MaxChunksPerProcess =
            2;


        // Prevent chunk generation from monopolizing the main thread.
        private const double ProcessBudgetMilliseconds =
            3.25;


        // Physics collision is unnecessary for the entire render distance.
        // 2 chunks = 64 blocks around the player for 32x32 chunks.
        private const int CollisionChunkRadius =
            2;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public ChunkLoader(
            World world,
            WorldGenerator generator,
            WorldSettings settings,
            ChunkRenderer renderer,
            ChunkCollision collision
        )
        {
            this.world =
                world;

            this.generator =
                generator;

            this.settings =
                settings;

            this.renderer =
                renderer;

            this.collision =
                collision;

            initialized =
                false;
        }


        // =====================================================
        // PLAYER CHUNK
        // =====================================================

        public void SetPlayerChunk(
            int playerChunkX,
            int playerChunkY
        )
        {
            UnityEngine.Vector2Int next =
                new UnityEngine.Vector2Int(
                    playerChunkX,
                    playerChunkY
                );


            if (!initialized)
            {
                initialized =
                    true;

                currentPlayerChunk =
                    next;

                BuildInitialQueue();

                RefreshCollisionWindow();

                return;
            }


            if (
                next ==
                currentPlayerChunk
            )
            {
                return;
            }


            currentPlayerChunk =
                next;


            // These operations only need to happen when the player
            // actually crosses a chunk boundary.
            UpdateLoadQueue();

            UnloadFarChunks();

            RefreshCollisionWindow();
        }


        // =====================================================
        // PROCESS
        // =====================================================

        public void Process()
        {
            ProcessLoadQueue();

            // IMPORTANT:
            // No UnloadFarChunks() here.
            //
            // The old loader iterated every loaded chunk every Process()
            // call even while the player stayed in one chunk.
        }


        // =====================================================
        // INITIAL QUEUE
        // =====================================================

        private void BuildInitialQueue()
        {
            loadQueue.Clear();

            queuedChunks.Clear();


            int viewDistance =
                settings.ViewDistance;


            EnqueueChunk(
                currentPlayerChunk.x,
                currentPlayerChunk.y
            );


            // Ring order = nearest chunks first.
            for (
                int radius = 1;
                radius <= viewDistance;
                radius++
            )
            {
                for (
                    int x = -radius;
                    x <= radius;
                    x++
                )
                {
                    for (
                        int y = -radius;
                        y <= radius;
                        y++
                    )
                    {
                        if (
                            Mathf.Abs(x) != radius &&
                            Mathf.Abs(y) != radius
                        )
                        {
                            continue;
                        }


                        EnqueueChunk(
                            currentPlayerChunk.x + x,
                            currentPlayerChunk.y + y
                        );
                    }
                }
            }
        }


        private void UpdateLoadQueue()
        {
            int viewDistance =
                settings.ViewDistance;


            // Add only missing chunks. Existing queue entries remain deduped.
            for (
                int radius = 0;
                radius <= viewDistance;
                radius++
            )
            {
                for (
                    int x = -radius;
                    x <= radius;
                    x++
                )
                {
                    for (
                        int y = -radius;
                        y <= radius;
                        y++
                    )
                    {
                        if (
                            radius > 0 &&
                            Mathf.Abs(x) != radius &&
                            Mathf.Abs(y) != radius
                        )
                        {
                            continue;
                        }


                        EnqueueChunk(
                            currentPlayerChunk.x + x,
                            currentPlayerChunk.y + y
                        );
                    }
                }
            }
        }


        private void EnqueueChunk(
            int chunkX,
            int chunkY
        )
        {
            UnityEngine.Vector2Int position =
                new UnityEngine.Vector2Int(
                    chunkX,
                    chunkY
                );


            if (
                loadedChunks.Contains(
                    position
                )
            )
            {
                return;
            }


            if (
                queuedChunks.Contains(
                    position
                )
            )
            {
                return;
            }


            loadQueue.Enqueue(
                position
            );


            queuedChunks.Add(
                position
            );
        }


        // =====================================================
        // LOAD
        // =====================================================

        private void ProcessLoadQueue()
        {
            if (loadQueue.Count == 0)
                return;


            Stopwatch stopwatch =
                Stopwatch.StartNew();


            int processed =
                0;


            while (
                loadQueue.Count > 0 &&
                processed <
                MaxChunksPerProcess
            )
            {
                // Always allow at least one chunk.
                if (
                    processed > 0 &&
                    stopwatch.Elapsed.TotalMilliseconds >=
                    ProcessBudgetMilliseconds
                )
                {
                    break;
                }


                UnityEngine.Vector2Int position =
                    loadQueue.Dequeue();


                queuedChunks.Remove(
                    position
                );


                if (
                    !IsInsideViewDistance(
                        position
                    )
                )
                {
                    continue;
                }


                if (
                    loadedChunks.Contains(
                        position
                    )
                )
                {
                    continue;
                }


                Chunk chunk =
                    world.CreateChunk(
                        position.x,
                        position.y
                    );


                if (chunk == null)
                    continue;


                ChunkData data =
                    generator.GenerateChunkData(
                        position.x,
                        position.y
                    );


                if (data == null)
                {
                    Game.World.Vegetation.Runtime
                    .GeneratedFloraFurnitureCommit
                    .Unload(
                        position.x,
                        position.y
                    );


                world.RemoveChunk(
                        position.x,
                        position.y
                    );

                    continue;
                }


                chunk.ApplyData(
                    data
                );

                // TELDER_VEGETATION_FIX_V2
                Game.World.Vegetation.Runtime
                    .GeneratedVegetationFurnitureBridge
                    .Apply(
                        data,
                        position.x,
                        position.y
                    );

                // TELDER_POLISH_FIX_PACK_V1
                Game.World.PolishFixes.GeneratedFurnitureChunkBridge.Apply(
                    data,
                    position.x,
                    position.y
                );

                // Newly loaded/generated chunks must get light immediately,
                // not only during the initial WorldManager lighting pass.
                if (
                    world.GetLightEngine() !=
                    null
                )
                {
                    world.GetLightEngine()
                        .RebuildAfterChunkGenerated(
                            position.x,
                            position.y,
                            settings.WorldHeight
                        );
                }


                // Rendering is required for all visible chunks.
                renderer?.Render(
                    chunk
                );


                loadedChunks.Add(
                    position
                );


                // Falling-block discovery is now event driven by chunk load.
                FallingBlockSystem.RegisterChunk(
                    chunk
                );


                // Build expensive collision ONLY close to the player.
                if (
                    ShouldHaveCollision(
                        position
                    )
                )
                {
                    BuildCollision(
                        position,
                        chunk
                    );
                }


                processed++;
            }
        }


        // =====================================================
        // COLLISION WINDOW
        // =====================================================

        private bool ShouldHaveCollision(
            UnityEngine.Vector2Int position
        )
        {
            return
                Mathf.Abs(
                    position.x -
                    currentPlayerChunk.x
                )
                <=
                CollisionChunkRadius
                &&
                Mathf.Abs(
                    position.y -
                    currentPlayerChunk.y
                )
                <=
                CollisionChunkRadius;
        }


        private void BuildCollision(
            UnityEngine.Vector2Int position,
            Chunk chunk
        )
        {
            if (
                collision == null ||
                chunk == null ||
                collisionChunks.Contains(
                    position
                )
            )
            {
                return;
            }


            collision.BuildChunkCollision(
                chunk
            );


            collisionChunks.Add(
                position
            );
        }


        private void RefreshCollisionWindow()
        {
            if (collision == null)
                return;


            List<UnityEngine.Vector2Int>
                remove =
                    null;


            foreach (
                UnityEngine.Vector2Int position
                in collisionChunks
            )
            {
                if (
                    ShouldHaveCollision(
                        position
                    )
                )
                {
                    continue;
                }


                if (remove == null)
                {
                    remove =
                        new List<UnityEngine.Vector2Int>();
                }


                remove.Add(
                    position
                );
            }


            if (remove != null)
            {
                for (
                    int i = 0;
                    i < remove.Count;
                    i++
                )
                {
                    UnityEngine.Vector2Int position =
                        remove[i];


                    collision.RemoveChunkCollision(
                        position.x,
                        position.y
                    );


                    collisionChunks.Remove(
                        position
                    );
                }
            }


            foreach (
                UnityEngine.Vector2Int position
                in loadedChunks
            )
            {
                if (
                    !ShouldHaveCollision(
                        position
                    )
                )
                {
                    continue;
                }


                if (
                    collisionChunks.Contains(
                        position
                    )
                )
                {
                    continue;
                }


                Chunk chunk =
                    world.GetChunk(
                        position.x,
                        position.y
                    );


                BuildCollision(
                    position,
                    chunk
                );
            }
        }


        // =====================================================
        // UNLOAD
        // =====================================================

        private void UnloadFarChunks()
        {
            List<UnityEngine.Vector2Int>
                toUnload =
                    null;


            foreach (
                UnityEngine.Vector2Int position
                in loadedChunks
            )
            {
                if (
                    IsInsideViewDistance(
                        position
                    )
                )
                {
                    continue;
                }


                if (toUnload == null)
                {
                    toUnload =
                        new List<UnityEngine.Vector2Int>();
                }


                toUnload.Add(
                    position
                );
            }


            if (toUnload == null)
                return;


            for (
                int i = 0;
                i < toUnload.Count;
                i++
            )
            {
                UnityEngine.Vector2Int position =
                    toUnload[i];


                FallingBlockSystem.UnregisterChunk(
                    position.x,
                    position.y
                );


                FurnitureLayerManager furniture =
                    FurnitureLayerManager.Instance;


                if (furniture != null)
                {
                    furniture.UnloadGeneratedFurnitureChunk(
                        position.x,
                        position.y
                    );
                }


                renderer?.RemoveChunk(
                    position.x,
                    position.y
                );


                if (
                    collisionChunks.Remove(
                        position
                    )
                )
                {
                    collision?.RemoveChunkCollision(
                        position.x,
                        position.y
                    );
                }


                world.RemoveChunk(
                    position.x,
                    position.y
                );


                loadedChunks.Remove(
                    position
                );
            }
        }


        private bool IsInsideViewDistance(
            UnityEngine.Vector2Int position
        )
        {
            return
                Mathf.Abs(
                    position.x -
                    currentPlayerChunk.x
                )
                <=
                settings.ViewDistance
                &&
                Mathf.Abs(
                    position.y -
                    currentPlayerChunk.y
                )
                <=
                settings.ViewDistance;
        }


        // =====================================================
        // PUBLIC
        // =====================================================

        public IEnumerable<UnityEngine.Vector2Int>
            GetLoadedChunks()
        {
            return
                loadedChunks;
        }


        public int GetLoadQueueSize()
        {
            return
                loadQueue.Count;
        }


        public bool IsChunkLoaded(
            int chunkX,
            int chunkY
        )
        {
            return
                loadedChunks.Contains(
                    new UnityEngine.Vector2Int(
                        chunkX,
                        chunkY
                    )
                );
        }
    }
}
