using System.Collections.Generic;

using UnityEngine;

using Game.World.Collision;
using Game.World.Generation;
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
        // LOADED
        // =====================================================

        private readonly HashSet<Vector2Int> loadedChunks =
            new HashSet<Vector2Int>();


        // =====================================================
        // QUEUE
        // =====================================================

        private readonly Queue<Vector2Int> loadQueue =
            new Queue<Vector2Int>();


        private readonly HashSet<Vector2Int> queuedChunks =
            new HashSet<Vector2Int>();


        // =====================================================
        // STATE
        // =====================================================

        private Vector2Int currentPlayerChunk;


        private bool initialized;


        // =====================================================
        // SETTINGS
        // =====================================================

        private const int MaxChunksPerProcess =
            1;


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
        // SET PLAYER CHUNK
        // =====================================================

        public void SetPlayerChunk(
            int playerChunkX,
            int playerChunkY
        )
        {

            Vector2Int newPlayerChunk =
                new Vector2Int(
                    playerChunkX,
                    playerChunkY
                );


            bool changed =
                !initialized ||
                newPlayerChunk !=
                currentPlayerChunk;


            currentPlayerChunk =
                newPlayerChunk;


            if (
                !changed
            )
            {
                return;
            }


            if (
                !initialized
            )
            {

                initialized =
                    true;


                BuildInitialLoadQueue();

            }
            else
            {

                UpdateLoadQueue();

            }

        }


        // =====================================================
        // PROCESS
        // =====================================================

        public void Process()
        {

            ProcessLoadQueue();


            UnloadFarChunks();

        }


        // =====================================================
        // INITIAL QUEUE
        // =====================================================

        private void BuildInitialLoadQueue()
        {

            loadQueue.Clear();

            queuedChunks.Clear();


            int viewDistance =
                settings.ViewDistance;


            // =================================================
            // CENTER
            // =================================================

            EnqueueChunk(
                currentPlayerChunk.x,
                currentPlayerChunk.y
            );


            // =================================================
            // RINGS
            // =================================================

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


        // =====================================================
        // UPDATE QUEUE
        // =====================================================

        private void UpdateLoadQueue()
        {

            int viewDistance =
                settings.ViewDistance;


            for (
                int cx = -viewDistance;
                cx <= viewDistance;
                cx++
            )
            {

                for (
                    int cy = -viewDistance;
                    cy <= viewDistance;
                    cy++
                )
                {

                    EnqueueChunk(
                        currentPlayerChunk.x + cx,
                        currentPlayerChunk.y + cy
                    );

                }

            }

        }


        // =====================================================
        // ENQUEUE
        // =====================================================

        private void EnqueueChunk(
            int chunkX,
            int chunkY
        )
        {

            Vector2Int position =
                new Vector2Int(
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
        // PROCESS QUEUE
        // =====================================================

        private void ProcessLoadQueue()
        {

            int processed =
                0;


            while (
                loadQueue.Count > 0 &&
                processed <
                MaxChunksPerProcess
            )
            {

                Vector2Int position =
                    loadQueue.Dequeue();


                queuedChunks.Remove(
                    position
                );


                // =================================================
                // DISTANCE CHECK
                // =================================================

                int distanceX =
                    Mathf.Abs(
                        position.x -
                        currentPlayerChunk.x
                    );


                int distanceY =
                    Mathf.Abs(
                        position.y -
                        currentPlayerChunk.y
                    );


                if (
                    distanceX >
                    settings.ViewDistance
                    ||
                    distanceY >
                    settings.ViewDistance
                )
                {
                    continue;
                }


                // =================================================
                // CREATE CHUNK
                // =================================================

                Chunk chunk =
                    world.CreateChunk(
                        position.x,
                        position.y
                    );


                if (
                    chunk == null
                )
                {
                    continue;
                }


                // =================================================
                // GENERATE
                // =================================================

                ChunkData data =
                    generator.GenerateChunkData(
                        position.x,
                        position.y
                    );


                if (
                    data == null
                )
                {
                    continue;
                }


                // =================================================
                // APPLY
                // =================================================

                chunk.ApplyData(
                    data
                );


                // =================================================
                // RENDER
                // =================================================

                renderer.Render(
                    chunk
                );


                // =================================================
                // COLLISION
                // =================================================

                collision.BuildChunkCollision(
                    chunk
                );


                // =================================================
                // LOADED
                // =================================================

                loadedChunks.Add(
                    position
                );


                processed++;

            }

        }


        // =====================================================
        // UNLOAD
        // =====================================================

        private void UnloadFarChunks()
        {

            List<Vector2Int> chunksToUnload =
                new List<Vector2Int>();


            foreach (
                Vector2Int position
                in loadedChunks
            )
            {

                int distanceX =
                    Mathf.Abs(
                        position.x -
                        currentPlayerChunk.x
                    );


                int distanceY =
                    Mathf.Abs(
                        position.y -
                        currentPlayerChunk.y
                    );


                if (
                    distanceX >
                    settings.ViewDistance
                    ||
                    distanceY >
                    settings.ViewDistance
                )
                {

                    chunksToUnload.Add(
                        position
                    );

                }

            }


            foreach (
                Vector2Int position
                in chunksToUnload
            )
            {

                renderer.RemoveChunk(
                    position.x,
                    position.y
                );


                collision.RemoveChunkCollision(
                    position.x,
                    position.y
                );


                world.RemoveChunk(
                    position.x,
                    position.y
                );


                loadedChunks.Remove(
                    position
                );

            }

        }


        // =====================================================
        // GET LOADED
        // =====================================================

        public IEnumerable<Vector2Int> GetLoadedChunks()
        {

            return loadedChunks;

        }


        // =====================================================
        // QUEUE SIZE
        // =====================================================

        public int GetLoadQueueSize()
        {

            return loadQueue.Count;

        }


        // =====================================================
        // IS LOADED
        // =====================================================

        public bool IsChunkLoaded(
            int chunkX,
            int chunkY
        )
        {

            return loadedChunks.Contains(
                new Vector2Int(
                    chunkX,
                    chunkY
                )
            );

        }

    }

}