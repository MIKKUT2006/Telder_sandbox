using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Lighting
{
    public class LightSystem
    {
        private readonly int chunkWidth;
        private readonly int chunkHeight;

        private readonly Dictionary<
            Vector2Int,
            ChunkLightData
        > chunks =
            new Dictionary<
                Vector2Int,
                ChunkLightData
            >();

        private readonly Queue<LightNode>
            addQueue =
                new Queue<LightNode>();

        private readonly Queue<LightNode>
            removeQueue =
                new Queue<LightNode>();

        private readonly DayNightSystem dayNight;

        public LightSystem(
            int chunkWidth,
            int chunkHeight,
            DayNightSettings dayNightSettings
        )
        {
            this.chunkWidth =
                chunkWidth;

            this.chunkHeight =
                chunkHeight;

            dayNight =
                new DayNightSystem(
                    dayNightSettings
                );
        }

        // =====================================================
        // CHUNKS
        // =====================================================

        public ChunkLightData GetOrCreateChunk(
            int chunkX,
            int chunkY
        )
        {
            Vector2Int key =
                new Vector2Int(
                    chunkX,
                    chunkY
                );

            if (
                chunks.TryGetValue(
                    key,
                    out ChunkLightData data
                )
            )
            {
                return data;
            }

            data =
                new ChunkLightData(
                    chunkWidth,
                    chunkHeight
                );

            chunks.Add(
                key,
                data
            );

            return data;
        }

        public bool HasChunk(
            int chunkX,
            int chunkY
        )
        {
            return chunks.ContainsKey(
                new Vector2Int(
                    chunkX,
                    chunkY
                )
            );
        }

        public void RemoveChunk(
            int chunkX,
            int chunkY
        )
        {
            chunks.Remove(
                new Vector2Int(
                    chunkX,
                    chunkY
                )
            );
        }

        // =====================================================
        // WORLD -> CHUNK
        // =====================================================

        private int WorldToChunk(
            int value
        )
        {
            if (value >= 0)
            {
                return
                    value /
                    chunkWidth;
            }

            return
                (value -
                 (chunkWidth - 1))
                /
                chunkWidth;
        }

        private int WorldToLocal(
            int value
        )
        {
            int chunk =
                WorldToChunk(
                    value
                );

            return
                value -
                chunk *
                chunkWidth;
        }

        // =====================================================
        // GET
        // =====================================================

        public LightValue GetLight(
            int worldX,
            int worldY
        )
        {
            int chunkX =
                WorldToChunk(
                    worldX
                );

            int chunkY =
                WorldToChunk(
                    worldY
                );

            int localX =
                WorldToLocal(
                    worldX
                );

            int localY =
                WorldToLocal(
                    worldY
                );

            if (
                !chunks.TryGetValue(
                    new Vector2Int(
                        chunkX,
                        chunkY
                    ),
                    out ChunkLightData chunk
                )
            )
            {
                return LightValue.Black;
            }

            return chunk.Get(
                localX,
                localY
            );
        }

        // =====================================================
        // SET
        // =====================================================

        public void SetLight(
            int worldX,
            int worldY,
            LightValue value
        )
        {
            int chunkX =
                WorldToChunk(
                    worldX
                );

            int chunkY =
                WorldToChunk(
                    worldY
                );

            int localX =
                WorldToLocal(
                    worldX
                );

            int localY =
                WorldToLocal(
                    worldY
                );

            ChunkLightData chunk =
                GetOrCreateChunk(
                    chunkX,
                    chunkY
                );

            chunk.Set(
                localX,
                localY,
                value
            );
        }

        // =====================================================
        // LIGHT SOURCE
        // =====================================================

        public void AddLightSource(
            int worldX,
            int worldY,
            LightSource source
        )
        {
            if (
                !source.Enabled
            )
            {
                return;
            }

            LightValue light =
                source.ToLight();

            addQueue.Enqueue(
                new LightNode(
                    worldX,
                    worldY,
                    light
                )
            );
        }

        // =====================================================
        // REMOVE SOURCE
        // =====================================================

        public void RemoveLightSource(
            int worldX,
            int worldY
        )
        {
            removeQueue.Enqueue(
                new LightNode(
                    worldX,
                    worldY,
                    LightValue.Black
                )
            );
        }

        // =====================================================
        // DAY / NIGHT
        // =====================================================

        public DayNightSystem DayNight
        {
            get
            {
                return dayNight;
            }
        }

        public void Update(
            float deltaTime
        )
        {
            dayNight.Update(
                deltaTime
            );

            /*
             * Пока здесь только фундамент.
             *
             * Следующим этапом сюда подключим:
             *
             * 1. RemoveLight()
             * 2. PropagateLight()
             * 3. Sunlight
             * 4. Chunk boundaries
             * 5. Dirty chunks
             * 6. Light textures
             */
        }
    }
}