using System;
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

        private readonly Queue<LightNode>
            removalPropagationQueue =
                new Queue<LightNode>();

        private readonly DayNightSystem dayNight;

        /*
         * Проверяет, может ли блок пропускать свет.
         *
         * true  = свет проходит
         * false = блокирует свет
         */
        private readonly Func<int, int, bool>
            isTransparent;

        /*
         * Насколько быстро затухает обычный свет.
         */
        private readonly float lightFalloff;

        /*
         * Максимальное количество операций
         * за один Process().
         */
        private readonly int maxOperationsPerUpdate;

        public LightSystem(
            int chunkWidth,
            int chunkHeight,
            DayNightSettings dayNightSettings,
            Func<int, int, bool> isTransparent,
            float lightFalloff = 0.08f,
            int maxOperationsPerUpdate = 5000
        )
        {
            this.chunkWidth =
                chunkWidth;

            this.chunkHeight =
                chunkHeight;

            this.isTransparent =
                isTransparent;

            this.lightFalloff =
                Mathf.Clamp(
                    lightFalloff,
                    0.001f,
                    1f
                );

            this.maxOperationsPerUpdate =
                Mathf.Max(
                    100,
                    maxOperationsPerUpdate
                );

            dayNight =
                new DayNightSystem(
                    dayNightSettings
                );
        }

        // =====================================================
        // CHUNK
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
                    out ChunkLightData chunk
                )
            )
            {
                return chunk;
            }

            chunk =
                new ChunkLightData(
                    chunkWidth,
                    chunkHeight
                );

            chunks.Add(
                key,
                chunk
            );

            return chunk;
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
        // COORDINATES
        // =====================================================

        private int WorldToChunk(
            int value,
            int size
        )
        {
            if (value >= 0)
            {
                return value / size;
            }

            return
                (value - (size - 1))
                / size;
        }

        private int WorldToLocal(
            int value,
            int size
        )
        {
            int chunk =
                WorldToChunk(
                    value,
                    size
                );

            return
                value -
                chunk * size;
        }

        // =====================================================
        // GET LIGHT
        // =====================================================

        public LightValue GetLight(
            int worldX,
            int worldY
        )
        {
            int chunkX =
                WorldToChunk(
                    worldX,
                    chunkWidth
                );

            int chunkY =
                WorldToChunk(
                    worldY,
                    chunkHeight
                );

            int localX =
                WorldToLocal(
                    worldX,
                    chunkWidth
                );

            int localY =
                WorldToLocal(
                    worldY,
                    chunkHeight
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
        // SET LIGHT
        // =====================================================

        private void SetLight(
            int worldX,
            int worldY,
            LightValue light
        )
        {
            int chunkX =
                WorldToChunk(
                    worldX,
                    chunkWidth
                );

            int chunkY =
                WorldToChunk(
                    worldY,
                    chunkHeight
                );

            int localX =
                WorldToLocal(
                    worldX,
                    chunkWidth
                );

            int localY =
                WorldToLocal(
                    worldY,
                    chunkHeight
                );

            ChunkLightData chunk =
                GetOrCreateChunk(
                    chunkX,
                    chunkY
                );

            chunk.Set(
                localX,
                localY,
                light.Clamp()
            );
        }

        // =====================================================
        // ADD SOURCE
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
        // PROCESS
        // =====================================================

        public void Update(
            float deltaTime
        )
        {
            dayNight.Update(
                deltaTime
            );

            ProcessLight();
        }

        private void ProcessLight()
        {
            int operations = 0;

            // -------------------------------------------------
            // REMOVE
            // -------------------------------------------------

            while (
                removeQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNode node =
                    removeQueue.Dequeue();

                RemoveLight(
                    node.X,
                    node.Y
                );

                operations++;
            }

            // -------------------------------------------------
            // PROPAGATE REMOVAL
            // -------------------------------------------------

            while (
                removalPropagationQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNode node =
                    removalPropagationQueue.Dequeue();

                PropagateRemoval(
                    node
                );

                operations++;
            }

            // -------------------------------------------------
            // ADD
            // -------------------------------------------------

            while (
                addQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNode node =
                    addQueue.Dequeue();

                PropagateAddition(
                    node
                );

                operations++;
            }
        }

        // =====================================================
        // REMOVE LIGHT
        // =====================================================

        private void RemoveLight(
            int worldX,
            int worldY
        )
        {
            LightValue old =
                GetLight(
                    worldX,
                    worldY
                );

            if (
                old.IsBlack
            )
            {
                return;
            }

            SetLight(
                worldX,
                worldY,
                LightValue.Black
            );

            removalPropagationQueue.Enqueue(
                new LightNode(
                    worldX,
                    worldY,
                    old
                )
            );
        }

        // =====================================================
        // REMOVAL PROPAGATION
        // =====================================================

        private void PropagateRemoval(
            LightNode node
        )
        {
            for (
                int i = 0;
                i < 4;
                i++
            )
            {
                int nx;
                int ny;

                GetNeighbour(
                    node.X,
                    node.Y,
                    i,
                    out nx,
                    out ny
                );

                LightValue neighbour =
                    GetLight(
                        nx,
                        ny
                    );

                if (
                    neighbour.IsBlack
                )
                {
                    continue;
                }

                /*
                 * Если сосед имеет меньше света,
                 * скорее всего он был частью
                 * распространения этого источника.
                 */
                if (
                    IsAffectedBy(
                        neighbour,
                        node.Light
                    )
                )
                {
                    SetLight(
                        nx,
                        ny,
                        LightValue.Black
                    );

                    removalPropagationQueue.Enqueue(
                        new LightNode(
                            nx,
                            ny,
                            neighbour
                        )
                    );
                }
            }
        }

        // =====================================================
        // ADDITION
        // =====================================================

        private void PropagateAddition(
            LightNode node
        )
        {
            LightValue current =
                GetLight(
                    node.X,
                    node.Y
                );

            LightValue result =
                MaxLight(
                    current,
                    node.Light
                );

            if (
                SameLight(
                    current,
                    result
                )
            )
            {
                return;
            }

            SetLight(
                node.X,
                node.Y,
                result
            );

            for (
                int i = 0;
                i < 4;
                i++
            )
            {
                int nx;
                int ny;

                GetNeighbour(
                    node.X,
                    node.Y,
                    i,
                    out nx,
                    out ny
                );

                if (
                    !isTransparent(
                        nx,
                        ny
                    )
                )
                {
                    continue;
                }

                LightValue next =
                    Attenuate(
                        result
                    );

                if (
                    next.IsBlack
                )
                {
                    continue;
                }

                LightValue neighbour =
                    GetLight(
                        nx,
                        ny
                    );

                LightValue merged =
                    MaxLight(
                        neighbour,
                        next
                    );

                if (
                    SameLight(
                        neighbour,
                        merged
                    )
                )
                {
                    continue;
                }

                addQueue.Enqueue(
                    new LightNode(
                        nx,
                        ny,
                        next
                    )
                );
            }
        }

        // =====================================================
        // ATTENUATION
        // =====================================================

        private LightValue Attenuate(
            LightValue value
        )
        {
            float amount =
                1f -
                lightFalloff;

            return new LightValue(
                value.R * amount,
                value.G * amount,
                value.B * amount,
                0f
            );
        }

        // =====================================================
        // MAX LIGHT
        // =====================================================

        private LightValue MaxLight(
            LightValue a,
            LightValue b
        )
        {
            return new LightValue(
                Mathf.Max(
                    a.R,
                    b.R
                ),

                Mathf.Max(
                    a.G,
                    b.G
                ),

                Mathf.Max(
                    a.B,
                    b.B
                ),

                Mathf.Max(
                    a.Sun,
                    b.Sun
                )
            );
        }

        // =====================================================
        // AFFECTED
        // =====================================================

        private bool IsAffectedBy(
            LightValue neighbour,
            LightValue source
        )
        {
            return
                neighbour.R <=
                source.R + 0.001f &&

                neighbour.G <=
                source.G + 0.001f &&

                neighbour.B <=
                source.B + 0.001f;
        }

        // =====================================================
        // EQUALITY
        // =====================================================

        private bool SameLight(
            LightValue a,
            LightValue b
        )
        {
            const float epsilon =
                0.001f;

            return
                Mathf.Abs(
                    a.R - b.R
                ) < epsilon &&

                Mathf.Abs(
                    a.G - b.G
                ) < epsilon &&

                Mathf.Abs(
                    a.B - b.B
                ) < epsilon &&

                Mathf.Abs(
                    a.Sun - b.Sun
                ) < epsilon;
        }

        // =====================================================
        // NEIGHBOURS
        // =====================================================

        private void GetNeighbour(
            int x,
            int y,
            int direction,
            out int nx,
            out int ny
        )
        {
            nx = x;
            ny = y;

            switch (direction)
            {
                case 0:
                    nx++;
                    break;

                case 1:
                    nx--;
                    break;

                case 2:
                    ny++;
                    break;

                case 3:
                    ny--;
                    break;
            }
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
    }
}