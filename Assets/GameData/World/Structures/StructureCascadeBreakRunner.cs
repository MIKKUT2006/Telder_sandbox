using System;
using System.Collections.Generic;

using UnityEngine;

using Game.World.Furniture;


namespace Game.World.Structures
{
    /// <summary>
    /// Small global scheduler for tree collapse jobs. It uses a strict per-frame
    /// budget, so even very large authored trees cannot be destroyed in one spike.
    /// </summary>
    public sealed class StructureCascadeBreakRunner : MonoBehaviour
    {
        private sealed class Job
        {
            public StructureCascadeBreakRuntime.CascadePlan Plan;
            public int Index;
            public float NextStepTime;

            public bool FurnitureDirty;
            public int FurnitureMinX;
            public int FurnitureMinY;
            public int FurnitureMaxX;
            public int FurnitureMaxY;
        }


        private const float CellInterval = 0.035f;
        private const int MaxCellsPerFrame = 2;
        private const float CollisionFlushInterval = 0.12f;


        private static StructureCascadeBreakRunner instance;

        private readonly List<Job> jobs =
            new List<Job>(8);

        private readonly HashSet<string> activeKeys =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        // One representative world cell per dirty chunk. Rebuilding chunk
        // collision once every 0.12 s is much cheaper than once per destroyed block.
        private readonly Dictionary<long, UnityEngine.Vector2Int>
            dirtyCollisionChunks =
                new Dictionary<long, UnityEngine.Vector2Int>();

        private float nextCollisionFlushTime;
        private int roundRobinIndex;


        internal static bool Schedule(
            StructureCascadeBreakRuntime.CascadePlan plan
        )
        {
            if (
                plan == null ||
                plan.Cells == null ||
                plan.Cells.Count == 0 ||
                plan.Manager == null
            )
            {
                return false;
            }

            StructureCascadeBreakRunner runner =
                EnsureInstance(plan.Manager);

            if (runner == null)
            {
                return false;
            }

            if (
                !runner.activeKeys.Add(
                    plan.Key ?? string.Empty
                )
            )
            {
                // The same cut is already collapsing. Treat it as handled so the
                // normal mining code does not also break a single block beneath it.
                return true;
            }

            Job job =
                new Job
                {
                    Plan = plan,
                    Index = 0,
                    NextStepTime =
                        Time.time +
                        CellInterval
                };

            // The block the player actually finished mining disappears
            // immediately. Only the crown/branches are animated over time.
            StructureCascadeBreakRuntime.CascadeCell firstCell =
                plan.Cells[0];

            bool firstChanged =
                StructureCascadeBreakRuntime.BreakCell(
                    plan,
                    firstCell,
                    FurnitureLayerManager.Instance,
                    out bool firstForegroundChanged,
                    out bool firstFurnitureChanged
                );

            if (!firstChanged)
            {
                runner.activeKeys.Remove(
                    plan.Key ?? string.Empty
                );
                return false;
            }

            job.Index = 1;

            if (firstForegroundChanged)
            {
                runner.MarkCollisionDirty(
                    firstCell.WorldX,
                    firstCell.WorldY
                );

                // One rebuild for the mined trunk cell is intentional: the player
                // must not collide with an invisible block while the rest falls.
                runner.FlushCollisionChanges();
            }

            if (firstFurnitureChanged)
            {
                MarkFurnitureDirty(
                    job,
                    firstCell.WorldX,
                    firstCell.WorldY
                );
            }

            if (job.Index >= plan.Cells.Count)
            {
                FlushFurnitureChanges(job);
                runner.activeKeys.Remove(
                    plan.Key ?? string.Empty
                );
                return true;
            }

            runner.jobs.Add(job);

            return true;
        }


        private static StructureCascadeBreakRunner EnsureInstance(
            WorldManager manager
        )
        {
            if (instance != null)
            {
                return instance;
            }

            StructureCascadeBreakRunner existing =
                UnityEngine.Object.FindFirstObjectByType<
                    StructureCascadeBreakRunner
                >();

            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            if (manager == null)
            {
                return null;
            }

            instance =
                manager.gameObject.AddComponent<
                    StructureCascadeBreakRunner
                >();

            return instance;
        }


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

            instance = this;
        }


        private void OnDestroy()
        {
            FlushCollisionChanges();

            for (
                int i = jobs.Count - 1;
                i >= 0;
                i--
            )
            {
                FlushFurnitureChanges(
                    jobs[i]
                );
            }

            jobs.Clear();
            activeKeys.Clear();

            if (instance == this)
            {
                instance = null;
            }
        }


        private void Update()
        {
            if (jobs.Count == 0)
            {
                if (
                    dirtyCollisionChunks.Count > 0 &&
                    Time.time >= nextCollisionFlushTime
                )
                {
                    FlushCollisionChanges();
                }

                return;
            }

            int processedThisFrame = 0;
            int inspected = 0;

            while (
                jobs.Count > 0 &&
                processedThisFrame < MaxCellsPerFrame &&
                inspected < jobs.Count
            )
            {
                if (roundRobinIndex >= jobs.Count)
                {
                    roundRobinIndex = 0;
                }

                Job job =
                    jobs[roundRobinIndex];

                inspected++;

                if (
                    job == null ||
                    job.Plan == null ||
                    job.Plan.Manager == null ||
                    job.Plan.World == null ||
                    job.Plan.Manager.GetWorld() != job.Plan.World
                )
                {
                    FinishJob(
                        roundRobinIndex,
                        job
                    );
                    continue;
                }

                if (Time.time < job.NextStepTime)
                {
                    roundRobinIndex++;
                    continue;
                }

                if (
                    job.Index >=
                    job.Plan.Cells.Count
                )
                {
                    FinishJob(
                        roundRobinIndex,
                        job
                    );
                    continue;
                }

                StructureCascadeBreakRuntime.CascadeCell cell =
                    job.Plan.Cells[job.Index];

                job.Index++;
                job.NextStepTime =
                    Time.time +
                    CellInterval;

                FurnitureLayerManager furniture =
                    FurnitureLayerManager.Instance;

                StructureCascadeBreakRuntime.BreakCell(
                    job.Plan,
                    cell,
                    furniture,
                    out bool foregroundChanged,
                    out bool furnitureChanged
                );

                if (foregroundChanged)
                {
                    MarkCollisionDirty(
                        cell.WorldX,
                        cell.WorldY
                    );
                }

                if (furnitureChanged)
                {
                    MarkFurnitureDirty(
                        job,
                        cell.WorldX,
                        cell.WorldY
                    );
                }

                processedThisFrame++;

                if (
                    job.Index >=
                    job.Plan.Cells.Count
                )
                {
                    FinishJob(
                        roundRobinIndex,
                        job
                    );
                }
                else
                {
                    roundRobinIndex++;
                }
            }

            if (
                dirtyCollisionChunks.Count > 0 &&
                Time.time >= nextCollisionFlushTime
            )
            {
                FlushCollisionChanges();
            }
        }


        private void FinishJob(
            int index,
            Job job
        )
        {
            if (job != null)
            {
                FlushFurnitureChanges(job);

                if (
                    job.Plan != null
                )
                {
                    activeKeys.Remove(
                        job.Plan.Key ?? string.Empty
                    );
                }
            }

            if (
                index >= 0 &&
                index < jobs.Count
            )
            {
                jobs.RemoveAt(index);
            }

            if (roundRobinIndex >= jobs.Count)
            {
                roundRobinIndex = 0;
            }

            FlushCollisionChanges();
        }


        private void MarkCollisionDirty(
            int worldX,
            int worldY
        )
        {
            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );

            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );

            long key =
                Pack(
                    chunkX,
                    chunkY
                );

            if (
                !dirtyCollisionChunks.ContainsKey(
                    key
                )
            )
            {
                dirtyCollisionChunks.Add(
                    key,
                    new UnityEngine.Vector2Int(
                        worldX,
                        worldY
                    )
                );
            }
        }


        private void FlushCollisionChanges()
        {
            if (dirtyCollisionChunks.Count == 0)
            {
                nextCollisionFlushTime =
                    Time.time +
                    CollisionFlushInterval;
                return;
            }

            WorldManager manager =
                WorldManager.Instance;

            if (manager != null)
            {
                foreach (
                    KeyValuePair<
                        long,
                        UnityEngine.Vector2Int
                    > pair in dirtyCollisionChunks
                )
                {
                    manager.RebuildCollisionAtWorldCell(
                        pair.Value.x,
                        pair.Value.y
                    );
                }
            }

            dirtyCollisionChunks.Clear();

            nextCollisionFlushTime =
                Time.time +
                CollisionFlushInterval;
        }


        private static void MarkFurnitureDirty(
            Job job,
            int x,
            int y
        )
        {
            if (job == null)
            {
                return;
            }

            if (!job.FurnitureDirty)
            {
                job.FurnitureDirty = true;
                job.FurnitureMinX = x;
                job.FurnitureMaxX = x;
                job.FurnitureMinY = y;
                job.FurnitureMaxY = y;
                return;
            }

            if (x < job.FurnitureMinX)
                job.FurnitureMinX = x;

            if (x > job.FurnitureMaxX)
                job.FurnitureMaxX = x;

            if (y < job.FurnitureMinY)
                job.FurnitureMinY = y;

            if (y > job.FurnitureMaxY)
                job.FurnitureMaxY = y;
        }


        private static void FlushFurnitureChanges(
            Job job
        )
        {
            if (
                job == null ||
                !job.FurnitureDirty
            )
            {
                return;
            }

            FurnitureLayerManager furniture =
                FurnitureLayerManager.Instance;

            if (furniture != null)
            {
                furniture.FlushDeferredTreeChanges(
                    job.FurnitureMinX,
                    job.FurnitureMinY,
                    job.FurnitureMaxX,
                    job.FurnitureMaxY
                );
            }

            job.FurnitureDirty = false;
        }


        private static int FloorDiv(
            int value,
            int divisor
        )
        {
            int result =
                value /
                divisor;

            int remainder =
                value %
                divisor;

            if (
                remainder != 0 &&
                value < 0
            )
            {
                result--;
            }

            return result;
        }


        private static long Pack(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    ((long)x << 32) ^
                    (uint)y;
            }
        }
    }
}
