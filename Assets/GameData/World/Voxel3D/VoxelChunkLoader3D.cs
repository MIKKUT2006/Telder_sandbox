using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelChunkLoader3D
    {
        private readonly VoxelWorld3D world;
        private readonly Voxel3DGenerator generator;
        private readonly VoxelChunkRenderer3D renderer;
        private readonly int horizontalRadius;
        private readonly int verticalRadius;
        private readonly Action<VoxelChunk3D> chunkLoaded;
        private readonly Action<Vector3Int> chunkRemoved;
        private readonly Queue<Vector3Int> queue = new Queue<Vector3Int>();
        private readonly HashSet<Vector3Int> queued = new HashSet<Vector3Int>();
        private Vector3Int current;
        private bool initialized;
        private readonly System.Diagnostics.Stopwatch processClock=new System.Diagnostics.Stopwatch();

        public VoxelChunkLoader3D(
            VoxelWorld3D world,
            Voxel3DGenerator generator,
            VoxelChunkRenderer3D renderer,
            int horizontalRadius = 4,
            int verticalRadius = 3,
            Action<VoxelChunk3D> chunkLoaded = null, Action<Vector3Int> chunkRemoved = null)
        {
            this.world = world;
            this.generator = generator;
            this.renderer = renderer;
            this.horizontalRadius = Mathf.Max(1, horizontalRadius);
            this.verticalRadius = Mathf.Max(1, verticalRadius);
            this.chunkLoaded = chunkLoaded;
            this.chunkRemoved = chunkRemoved;
        }

        public void UpdatePlayer(Vector3 worldPosition)
        {
            Vector3Int next = VoxelWorld3D.WorldToChunk(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                Mathf.FloorToInt(worldPosition.z));

            if (initialized && next == current)
                return;

            current = next;
            initialized = true;
            RebuildQueue();
            UnloadFar();
        }

        public void Process(int maxChunks = 1, double budgetMilliseconds = 2.0)
        {
            processClock.Restart();
            for (int i = 0; i < maxChunks && queue.Count > 0; i++)
            {
                if(i>0&&processClock.Elapsed.TotalMilliseconds>=budgetMilliseconds)break;
                Vector3Int c = queue.Dequeue();
                queued.Remove(c);

                if (world.TryGetChunk(c, out _))
                    continue;

                VoxelChunk3D generated = generator.Generate(c);
                world.SetChunk(generated);
                chunkLoaded?.Invoke(generated);
                renderer.Rebuild(c);

                foreach (Vector3Int d in NeighborCoords(c))
                {
                    if (world.TryGetChunk(d, out _))
                        renderer.QueueRebuild(d);
                }
            }
            processClock.Stop();
        }

        private void RebuildQueue()
        {
            // Keep already queued work but add new coordinates in distance order. The
            // previous implementation iterated bottom-to-top, so deep underground chunks
            // could load before the surface the player was actually standing on.
            queue.Clear(); queued.Clear();
            var candidates = new List<Vector3Int>();

            for (int x = -horizontalRadius; x <= horizontalRadius; x++)
            for (int z = -horizontalRadius; z <= horizontalRadius; z++)
            for (int y = -verticalRadius; y <= verticalRadius; y++)
            {
                // Square render distance is intentional here; it resembles early voxel
                // games and prevents holes at diagonal chunk borders.
                Vector3Int c = current + new Vector3Int(x, y, z);
                if (world.TryGetChunk(c, out _) || queued.Contains(c))
                    continue;

                candidates.Add(c);
            }

            candidates.Sort((a, b) => Priority(a).CompareTo(Priority(b)));
            foreach (Vector3Int c in candidates)
                Enqueue(c);
        }

        private float Priority(Vector3Int c)
        {
            Vector3Int d = c - current;
            // Horizontal distance matters most visually. Vertical chunks close to the
            // player's level are preferred, but below/above still stream normally.
            return d.x * d.x + d.z * d.z + Mathf.Abs(d.y) * .65f;
        }

        private void Enqueue(Vector3Int c)
        {
            if (world.TryGetChunk(c, out _) || queued.Contains(c))
                return;

            queue.Enqueue(c);
            queued.Add(c);
        }

        private void UnloadFar()
        {
            var remove = new List<Vector3Int>();
            foreach (var kv in world.Chunks)
            {
                Vector3Int d = kv.Key - current;
                if (Mathf.Abs(d.x) > horizontalRadius + 1 ||
                    Mathf.Abs(d.z) > horizontalRadius + 1 ||
                    Mathf.Abs(d.y) > verticalRadius + 1)
                {
                    remove.Add(kv.Key);
                }
            }

            foreach (Vector3Int c in remove)
            {
                chunkRemoved?.Invoke(c);
                world.RemoveChunk(c);
                renderer.Remove(c);
            }
        }

        private static IEnumerable<Vector3Int> NeighborCoords(Vector3Int c)
        {
            yield return c + Vector3Int.right;
            yield return c + Vector3Int.left;
            yield return c + Vector3Int.up;
            yield return c + Vector3Int.down;
            yield return c + new Vector3Int(0, 0, 1);
            yield return c + new Vector3Int(0, 0, -1);
        }
    }
}
