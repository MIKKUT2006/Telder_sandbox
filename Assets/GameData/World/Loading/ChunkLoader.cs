using System.Collections.Generic;
using UnityEngine;

using Game.Save;

using Game.World.Collision;
using Game.World.Generation;
using Game.World.Rendering;
using Game.World.Structures;


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
        // LOADED CHUNKS
        // =====================================================

        private readonly HashSet<Vector2Int> loadedChunks =
            new HashSet<Vector2Int>();


        // =====================================================
        // LOAD QUEUE
        // =====================================================

        private readonly Queue<Vector2Int> loadQueue =
            new Queue<Vector2Int>();


        private readonly HashSet<Vector2Int> queuedChunks =
            new HashSet<Vector2Int>();


        // =====================================================
        // PLAYER CHUNK
        // =====================================================

        private Vector2Int currentPlayerChunk;

        private bool initialized;


        // =====================================================
        // SETTINGS
        // =====================================================

        // Сколько чанков максимум
        // загружаем за один кадр.
        //
        // Начинаем с 1 для максимальной стабильности.
        //
        // Позже можно увеличить до 2-3.

        private const int MaxChunksPerProcess = 1;


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

            Vector2Int newChunk =
                new Vector2Int(
                    playerChunkX,
                    playerChunkY
                );


            // =================================================
            // ПЕРВЫЙ ЗАПУСК
            // =================================================

            if (
                !initialized
            )
            {

                initialized =
                    true;


                currentPlayerChunk =
                    newChunk;


                BuildInitialQueue();


                return;

            }


            // =================================================
            // ИГРОК НЕ СМЕНИЛ ЧАНК
            // =================================================

            if (
                newChunk ==
                currentPlayerChunk
            )
            {
                return;
            }


            // =================================================
            // ИГРОК СМЕНИЛ ЧАНК
            // =================================================

            currentPlayerChunk =
                newChunk;


            UpdateLoadQueue();

        }


        // =====================================================
        // PROCESS
        // =====================================================

        public void Process()
        {
            //Debug.Log(
            //    "CHUNK LOADER PROCESS: Queue = " +
            //    loadQueue.Count
            //);

            ProcessLoadQueue();

            UnloadFarChunks();
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


            // =================================================
            // ЦЕНТР
            // =================================================

            EnqueueChunk(
                currentPlayerChunk.x,
                currentPlayerChunk.y
            );


            // =================================================
            // ОСТАЛЬНЫЕ ЧАНКИ
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

                        // Берём только внешний край кольца.

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
        // UPDATE LOAD QUEUE
        // =====================================================

        private void UpdateLoadQueue()
        {

            int viewDistance =
                settings.ViewDistance;


            // =================================================
            // ВСЕ ЧАНКИ ВОКРУГ ИГРОКА
            // =================================================

            for (
                int x = -viewDistance;
                x <= viewDistance;
                x++
            )
            {

                for (
                    int y = -viewDistance;
                    y <= viewDistance;
                    y++
                )
                {

                    EnqueueChunk(
                        currentPlayerChunk.x + x,
                        currentPlayerChunk.y + y
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


            // Уже загружен.

            if (
                loadedChunks.Contains(
                    position
                )
            )
            {
                return;
            }


            // Уже в очереди.

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
                // ПРОВЕРЯЕМ ДАЛЬНОСТЬ
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
                // ПРОВЕРЯЕМ, НЕ СОЗДАН ЛИ УЖЕ
                // =================================================

                if (
                    loadedChunks.Contains(
                        position
                    )
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
                // GENERATE DATA
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

                    world.RemoveChunk(
                        position.x,
                        position.y
                    );


                    continue;

                }


                // =================================================
                // PROCEDURAL STRUCTURES
                // =================================================

                StructureGenerationRuntime.ApplyToChunk(
                    generator,
                    settings,
                    data,
                    position.x,
                    position.y
                );


                // =================================================
                // APPLY DATA
                // =================================================

                chunk.ApplyData(
                    data
                );


                // =================================================
                // APPLY SAVED DIMENSION CHANGES
                // =================================================

                SaveGameRuntime.ApplyChangesToChunk(
                    chunk
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
                // MARK LOADED
                // =================================================

                loadedChunks.Add(
                    position
                );


                processed++;

            }

        }


        // =====================================================
        // UNLOAD FAR CHUNKS
        // =====================================================

        private void UnloadFarChunks()
        {

            List<Vector2Int> toUnload =
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

                    toUnload.Add(
                        position
                    );

                }

            }


            foreach (
                Vector2Int position
                in toUnload
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
