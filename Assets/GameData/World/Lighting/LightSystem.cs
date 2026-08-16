using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Lighting
{
    public class LightSystem
    {
        private readonly int chunkWidth;
        private readonly int chunkHeight;

        private readonly Dictionary<Vector2Int, ChunkLightData> chunks =
            new Dictionary<Vector2Int, ChunkLightData>();

        private readonly Queue<LightNodePosition> addQueue =
            new Queue<LightNodePosition>();

        private readonly Queue<LightNodePosition> removeQueue =
            new Queue<LightNodePosition>();

        private readonly Queue<LightNodePosition> removalQueue =
            new Queue<LightNodePosition>();

        private readonly DayNightSystem dayNight;

        private readonly Func<int, int, bool> isTransparent;

        private readonly int maxOperationsPerUpdate;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public LightSystem(
            int chunkWidth,
            int chunkHeight,
            DayNightSettings dayNightSettings,
            Func<int, int, bool> isTransparent,
            int maxOperationsPerUpdate = 5000
        )
        {
            this.chunkWidth = chunkWidth;
            this.chunkHeight = chunkHeight;

            this.isTransparent = isTransparent;

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
                    out ChunkLightData existing
                )
            )
            {
                return existing;
            }

            ChunkLightData data =
                new ChunkLightData();

            chunks.Add(
                key,
                data
            );

            return data;
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
                (value - (size - 1)) /
                size;
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

        public LightNode GetLight(
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
                return LightNode.None;
            }

            return
                chunk.Get(
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
            LightNode light
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
                light
            );
        }

        // =====================================================
        // ADD LIGHT SOURCE
        // =====================================================

        public void AddLightSource(
            int worldX,
            int worldY,
            LightSource source
        )
        {
            if (source == null)
            {
                return;
            }

            addQueue.Enqueue(
                new LightNodePosition(
                    worldX,
                    worldY,
                    source.ToLight()
                )
            );
        }

        // =====================================================
        // REMOVE LIGHT SOURCE
        // =====================================================

        public void RemoveLightSource(
            int worldX,
            int worldY
        )
        {
            removeQueue.Enqueue(
                new LightNodePosition(
                    worldX,
                    worldY,
                    LightNode.None
                )
            );
        }

        // =====================================================
        // UPDATE
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

        // =====================================================
        // PROCESS
        // =====================================================

        private void ProcessLight()
        {
            int operations = 0;

            // -------------------------------------------------
            // REMOVE SOURCES
            // -------------------------------------------------

            while (
                removeQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNodePosition node =
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
                removalQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNodePosition node =
                    removalQueue.Dequeue();

                PropagateRemoval(
                    node
                );

                operations++;
            }

            // -------------------------------------------------
            // ADD LIGHT
            // -------------------------------------------------

            while (
                addQueue.Count > 0 &&
                operations < maxOperationsPerUpdate
            )
            {
                LightNodePosition node =
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
            int x,
            int y
        )
        {
            LightNode old =
                GetLight(
                    x,
                    y
                );

            if (old.IsEmpty)
            {
                return;
            }

            SetLight(
                x,
                y,
                LightNode.None
            );

            removalQueue.Enqueue(
                new LightNodePosition(
                    x,
                    y,
                    old
                )
            );
        }

        // =====================================================
        // REMOVAL PROPAGATION
        // =====================================================

        private void PropagateRemoval(
            LightNodePosition node
        )
        {
            for (
                int direction = 0;
                direction < 4;
                direction++
            )
            {
                GetNeighbour(
                    node.X,
                    node.Y,
                    direction,
                    out int nx,
                    out int ny
                );

                LightNode neighbour =
                    GetLight(
                        nx,
                        ny
                    );

                if (neighbour.IsEmpty)
                {
                    continue;
                }

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
                        LightNode.None
                    );

                    removalQueue.Enqueue(
                        new LightNodePosition(
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
            LightNodePosition node
        )
        {
            if (node.Light.IsEmpty)
            {
                return;
            }

            LightNode current =
                GetLight(
                    node.X,
                    node.Y
                );

            LightNode merged =
                MaxLight(
                    current,
                    node.Light
                );

            if (
                SameLight(
                    current,
                    merged
                )
            )
            {
                return;
            }

            SetLight(
                node.X,
                node.Y,
                merged
            );

            LightNode next =
                Attenuate(
                    merged
                );

            if (next.IsEmpty)
            {
                return;
            }

            for (
                int direction = 0;
                direction < 4;
                direction++
            )
            {
                GetNeighbour(
                    node.X,
                    node.Y,
                    direction,
                    out int nx,
                    out int ny
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

                LightNode neighbour =
                    GetLight(
                        nx,
                        ny
                    );

                LightNode result =
                    MaxLight(
                        neighbour,
                        next
                    );

                if (
                    SameLight(
                        neighbour,
                        result
                    )
                )
                {
                    continue;
                }

                addQueue.Enqueue(
                    new LightNodePosition(
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

        private LightNode Attenuate(
            LightNode value
        )
        {
            return new LightNode(
                AttenuateChannel(
                    value.Sun
                ),

                AttenuateChannel(
                    value.R
                ),

                AttenuateChannel(
                    value.G
                ),

                AttenuateChannel(
                    value.B
                )
            );
        }

        private byte AttenuateChannel(
            byte value
        )
        {
            if (value <= 1)
            {
                return 0;
            }

            return (byte)(value - 1);
        }

        // =====================================================
        // MAX LIGHT
        // =====================================================

        private LightNode MaxLight(
    LightNode a,
    LightNode b
)
        {
            return new LightNode(
                (byte)Mathf.Max(
                    a.Sun,
                    b.Sun
                ),

                (byte)Mathf.Max(
                    a.R,
                    b.R
                ),

                (byte)Mathf.Max(
                    a.G,
                    b.G
                ),

                (byte)Mathf.Max(
                    a.B,
                    b.B
                )
            );
        }

        // =====================================================
        // AFFECTED
        // =====================================================

        private bool IsAffectedBy(
            LightNode neighbour,
            LightNode source
        )
        {
            return
                neighbour.Sun <= source.Sun &&
                neighbour.R <= source.R &&
                neighbour.G <= source.G &&
                neighbour.B <= source.B;
        }

        // =====================================================
        // EQUALITY
        // =====================================================

        private bool SameLight(
            LightNode a,
            LightNode b
        )
        {
            return
                a.Sun == b.Sun &&
                a.R == b.R &&
                a.G == b.G &&
                a.B == b.B;
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