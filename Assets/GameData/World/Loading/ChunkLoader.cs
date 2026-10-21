using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

using Game.World.Collision;
using Game.World.Furniture;
using Game.World.Generation;
using Game.World.Physics;
using Game.World.Rendering;
using Game.Save;


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


        public enum ChunkVisibilityState { Unloaded, Hidden, Visible }
        public ChunkVisibilityState GetChunkVisibilityState(int x, int y)
        {
            var key = new UnityEngine.Vector2Int(x, y);
            if (!loadedChunks.Contains(key)) return ChunkVisibilityState.Unloaded;
            return renderer != null && renderer.IsChunkVisualEnabled(x, y)
                ? ChunkVisibilityState.Visible : ChunkVisibilityState.Hidden;
        }

        // A chunk exists in one of three visual states:
        //   Visible: loaded and GPU visuals active,
        //   Hidden:  loaded data, visuals disabled/not allocated,
        //   Unloaded: no chunk in World.
        // All Unity objects and content registries remain on the main thread.
        private sealed class GenerationJob
        {
            public UnityEngine.Vector2Int Position;
            public ChunkData Data;
            public int Column;
            public int PostprocessStage;
            public int CommitStage;
            public Chunk Chunk;
        }
        private GenerationJob activeJob;
        private readonly Queue<UnityEngine.Vector2Int> visualQueue = new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingVisuals = new HashSet<UnityEngine.Vector2Int>();
        private readonly Queue<UnityEngine.Vector2Int> lightQueue = new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingLights = new HashSet<UnityEngine.Vector2Int>();

        private readonly Queue<UnityEngine.Vector2Int> unloadQueue = new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingUnloads = new HashSet<UnityEngine.Vector2Int>();

        private readonly Queue<UnityEngine.Vector2Int> colliderQueue = new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingColliders = new HashSet<UnityEngine.Vector2Int>();
        private Camera visibleCamera;
        private int boundsFrame=-1;
        private float viewMinX,viewMaxX,viewMinY,viewMaxY;
        private bool initialWarmup=true;
        private float nextCameraCulling;
        private const float CameraCullingInterval = .10f;
        private const float CameraChunkPadding = 2f;
        private const int TerrainColumnsPerStep = 4;
        private readonly Stopwatch processClock = new Stopwatch();
        private int lastProcessFrame=-1;

        private bool CanContinueFrame => processClock.Elapsed.TotalMilliseconds < ProcessBudgetMilliseconds;

        public bool IsChunkOnScreen(int chunkX, int chunkY)
        {
            return IsInCamera(new UnityEngine.Vector2Int(chunkX, chunkY));
        }

        private bool IsInCamera(UnityEngine.Vector2Int chunk)
        {
            if(initialWarmup)return Mathf.Abs(chunk.x-currentPlayerChunk.x)<=2&&Mathf.Abs(chunk.y-currentPlayerChunk.y)<=2;
            if(visibleCamera==null)visibleCamera=Camera.main;
            if(visibleCamera==null)return Mathf.Abs(chunk.x-currentPlayerChunk.x)<=2&&Mathf.Abs(chunk.y-currentPlayerChunk.y)<=2;
            if(boundsFrame!=Time.frameCount)
            {
                boundsFrame=Time.frameCount;float z=Mathf.Abs(visibleCamera.transform.position.z);
                Vector3 low=visibleCamera.ViewportToWorldPoint(new Vector3(0,0,z)),high=visibleCamera.ViewportToWorldPoint(new Vector3(1,1,z));
                viewMinX=Mathf.Min(low.x,high.x)-CameraChunkPadding;viewMaxX=Mathf.Max(low.x,high.x)+CameraChunkPadding;
                viewMinY=Mathf.Min(low.y,high.y)-CameraChunkPadding;viewMaxY=Mathf.Max(low.y,high.y)+CameraChunkPadding;
            }
            float x=chunk.x*Chunk.SizeX,y=chunk.y*Chunk.SizeY;
            return x<viewMaxX&&x+Chunk.SizeX>viewMinX&&y<viewMaxY&&y+Chunk.SizeY>viewMinY;
        }

        private void EnqueueVisual(UnityEngine.Vector2Int position)
        {
            if (!loadedChunks.Contains(position) || !IsInCamera(position)) return;
            if (pendingVisuals.Add(position)) visualQueue.Enqueue(position);
        }

        private void EnqueueLight(UnityEngine.Vector2Int position)
        {
            if(!loadedChunks.Contains(position)||!IsInCamera(position))return;
            var light=world.GetChunk(position.x,position.y)?.GetLightData();
            if(light!=null&&light.IsInitialized)return;
            if(pendingLights.Add(position))lightQueue.Enqueue(position);
        }

        private void UpdateCameraVisibility()
        {
            if (Time.unscaledTime < nextCameraCulling) return;
            nextCameraCulling = Time.unscaledTime + CameraCullingInterval;
            foreach (UnityEngine.Vector2Int position in loadedChunks)
            {
                bool visible = IsInCamera(position);
                if (renderer == null) continue;
                if (!visible)
                {
                    renderer.SetChunkVisible(position.x, position.y, false);
                }
                else if (renderer.HasChunkVisual(position.x, position.y))
                {
                    renderer.SetChunkVisible(position.x, position.y, true);
                    EnqueueLight(position);
                }
                else
                {
                    EnqueueLight(position);
                    EnqueueVisual(position);
                }
            }
        }

        private void ProcessVisualQueue()
        {
            int attempts=visualQueue.Count;
            while(attempts-->0&&visualQueue.Count>0)
            {
                var position=visualQueue.Dequeue();pendingVisuals.Remove(position);
                if(!loadedChunks.Contains(position)||!IsInCamera(position))continue;
                Chunk chunk=world.GetChunk(position.x,position.y);if(chunk==null||renderer==null)continue;
                if(chunk.GetLightData()==null||!chunk.GetLightData().IsInitialized)
                {EnqueueLight(position);EnqueueVisual(position);continue;}
                renderer.QueueRender(chunk);break;
            }
        }

        private void ProcessLightQueue()
        {
            int attempts=lightQueue.Count;
            while(attempts-->0&&lightQueue.Count>0)
            {
                var position=lightQueue.Dequeue();pendingLights.Remove(position);
                if(!loadedChunks.Contains(position)||!IsInCamera(position))continue;
                world.GetLightEngine()?.RequestChunk(position.x,position.y);
            }
        }

        private void QueueCollision(UnityEngine.Vector2Int position)
        {
            if (collisionChunks.Contains(position) || !ShouldHaveCollision(position)) return;
            if (pendingColliders.Add(position)) colliderQueue.Enqueue(position);
        }

        private void ProcessCollisionQueue()
        {
            while (colliderQueue.Count > 0)
            {
                var position = colliderQueue.Dequeue();
                pendingColliders.Remove(position);
                if (!loadedChunks.Contains(position) || !ShouldHaveCollision(position)) continue;
                BuildCollision(position, world.GetChunk(position.x, position.y));
                break;
            }
        }

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
            1;


        // Prevent chunk generation from monopolizing the main thread.
        private const double ProcessBudgetMilliseconds =
            2.0;


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
            if (renderer != null) renderer.RenderEligibility = (x, y) =>
                IsInCamera(new UnityEngine.Vector2Int(x, y));
        }


        // =====================================================
        // PLAYER CHUNK
        // =====================================================

        public void SetPlayerChunk(
            int playerChunkX,
            int playerChunkY
        )
        {
            world.GetLightEngine()?.SetPriority(playerChunkX,playerChunkY);
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
            if(lastProcessFrame==Time.frameCount)return;lastProcessFrame=Time.frameCount;
            processClock.Restart();UpdateCameraVisibility();
            // These queues get a turn before generation consumes its slice.
            ProcessLightQueue();
            if(CanContinueFrame)ProcessUnloadQueue();
            if(CanContinueFrame)ProcessCollisionQueue();
            if(CanContinueFrame)ProcessVisualQueue();
            int steps=0;
            while(steps<8&&(steps==0||CanContinueFrame)&&(activeJob!=null||loadQueue.Count>0))
            {ProcessLoadQueue();steps++;}
            processClock.Stop();
        }
        public void PrepareInitialLighting(int x,int y,int radius)
        {
            world.GetLightEngine()?.SetPriority(x,y);
            for(int cx=x-radius;cx<=x+radius;cx++)for(int cy=y;cy<=(radius==0?y:y+1);cy++)
            {world.GetLightEngine()?.RequestChunk(cx,cy);EnqueueVisual(new UnityEngine.Vector2Int(cx,cy));}
        }
        public bool IsInitialLightingReady(int x,int y,int radius)
        {
            for(int cx=x-radius;cx<=x+radius;cx++)for(int cy=y;cy<=(radius==0?y:y+1);cy++)
            {
                Chunk chunk=world.GetChunk(cx,cy);
                if(chunk?.GetLightData()==null||!chunk.GetLightData().IsInitialized||world.GetLightEngine().IsChunkPending(cx,cy))return false;
            }
            return true;
        }
        public void FinishInitialLoad(){initialWarmup=false;nextCameraCulling=0;boundsFrame=-1;}



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

            // Android startup should load surface chunks before far-below
            // cells. This is safe on other platforms and improves first spawn.
            if (viewDistance >= 1)
            {
                for (int x = -1; x <= 1; x++)
                {
                    EnqueueChunk(currentPlayerChunk.x + x, currentPlayerChunk.y);
                    EnqueueChunk(currentPlayerChunk.x + x, currentPlayerChunk.y + 1);
                }
            }

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


            // Reprioritize around the player to avoid stale distant chunks.
            loadQueue.Clear();
            queuedChunks.Clear();
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


            if (activeJob!=null&&activeJob.Position==position)return;
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
            if (activeJob == null)
            {
                while (loadQueue.Count > 0)
                {
                    UnityEngine.Vector2Int key = loadQueue.Dequeue();
                    queuedChunks.Remove(key);
                    if (!IsInsideViewDistance(key) || loadedChunks.Contains(key)) continue;
                    activeJob = new GenerationJob
                    {
                        Position = key,
                        Data = generator.BeginChunkData()
                    };
                    break;
                }
            }
            if (activeJob == null) return;
            GenerationJob job = activeJob;
            UnityEngine.Vector2Int position = job.Position;
            if (!IsInsideViewDistance(position))
            {
                activeJob = null;
                return;
            }
            // Base terrain work is split into four columns per frame at most.
            if (job.Column < Chunk.SizeX)
            {
                int end = Mathf.Min(Chunk.SizeX, job.Column + TerrainColumnsPerStep);
                for (; job.Column < end; job.Column++)
                {
                    generator.GenerateTerrainColumn(job.Data, position.x, position.y, job.Column);
                    if (!CanContinueFrame) { job.Column++; break; }
                }
                return;
            }
            // Cave materials, structures, lakes and flora are separate stages.
            if (job.PostprocessStage < WorldGenerator.ChunkPostprocessStageCount)
            {
                generator.CompleteChunkGenerationStage(job.Data, position.x, position.y,
                    job.PostprocessStage);
                job.PostprocessStage++;
                return;
            }
            if (job.CommitStage == 0)
            {
                job.Chunk = world.CreateChunk(position.x, position.y);
                if (job.Chunk == null) { activeJob = null; return; }
                job.Chunk.ApplyData(job.Data);
                SaveGameRuntime.ApplyChangesToChunk(job.Chunk);
                job.CommitStage++;
                return;
            }
            if (job.CommitStage == 1)
            {
                Game.World.Vegetation.Runtime.GeneratedVegetationFurnitureBridge.Apply(
                    job.Data, position.x, position.y);
                job.CommitStage++;
                return;
            }
            if (job.CommitStage == 2)
            {
                Game.World.PolishFixes.GeneratedFurnitureChunkBridge.Apply(
                    job.Data, position.x, position.y);
                job.CommitStage++;
                return;
            }
            loadedChunks.Add(position);
            world.NotifyChunkGenerated(position.x,position.y);
            FallingBlockSystem.RegisterChunk(job.Chunk);
            if (ShouldHaveCollision(position)) QueueCollision(position);
            if (IsInCamera(position))
            {
                EnqueueLight(position);
                EnqueueVisual(position);
            }
            activeJob = null;
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


                QueueCollision(position);
            }
        }


        // =====================================================
        // UNLOAD
        // =====================================================

        private void UnloadFarChunks()
        {
            // Crossing a chunk boundary must not destroy 20+ Unity objects
            // and their textures synchronously on a single frame.
            foreach (UnityEngine.Vector2Int position in loadedChunks)
            {
                if (!IsInsideViewDistance(position) && pendingUnloads.Add(position))
                    unloadQueue.Enqueue(position);
            }
        }

        private void ProcessUnloadQueue()
        {
            while (unloadQueue.Count > 0)
            {
                UnityEngine.Vector2Int position = unloadQueue.Dequeue();
                pendingUnloads.Remove(position);
                if (!loadedChunks.Contains(position) || IsInsideViewDistance(position)) continue;
                FallingBlockSystem.UnregisterChunk(position.x, position.y);
                FurnitureLayerManager furniture = FurnitureLayerManager.Instance;
                if (furniture != null)
                    furniture.UnloadGeneratedFurnitureChunk(position.x, position.y);
                renderer?.RemoveChunk(position.x, position.y);
                if (collisionChunks.Remove(position))
                    collision?.RemoveChunkCollision(position.x, position.y);
                world.RemoveChunk(position.x, position.y);
                loadedChunks.Remove(position);
                pendingVisuals.Remove(position);
                pendingLights.Remove(position);

                pendingColliders.Remove(position);
                break; // One unload per frame; stale items are ignored.
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
                loadQueue.Count + (activeJob != null ? 1 : 0);
        }


        // Startup only: verify that nearby terrain is solid and ready
        // before the player is allowed to move into it.
        public bool IsChunkCollisionReady(int chunkX, int chunkY)
        {
            return collision == null || collisionChunks.Contains(
                new UnityEngine.Vector2Int(chunkX, chunkY));
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
