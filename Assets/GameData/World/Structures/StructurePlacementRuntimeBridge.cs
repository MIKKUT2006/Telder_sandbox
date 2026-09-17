using System;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

using Game.BlockTransforms;
using Game.World.Furniture;


namespace Game.World.Structures
{
    public sealed class StructurePlacementRuntimeBridge :
        MonoBehaviour
    {
        private enum OperationType
        {
            ForegroundTransform,
            BackgroundTransform,
            Furniture
        }


        private struct Operation
        {
            public OperationType Type;

            public int X;
            public int Y;

            public string Id;

            public byte Transform;
        }


        private static readonly ConcurrentQueue<Operation>
            Queue =
                new ConcurrentQueue<Operation>();


        private static StructurePlacementRuntimeBridge
            instance;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Bootstrap()
        {
            EnsureInstance();
        }


        public static void QueueForegroundTransform(
            int x,
            int y,
            byte transform
        )
        {
            if (transform == 0)
                return;


            Queue.Enqueue(
                new Operation
                {
                    Type =
                        OperationType.ForegroundTransform,

                    X =
                        x,

                    Y =
                        y,

                    Transform =
                        transform
                }
            );
        }


        public static void QueueBackgroundTransform(
            int x,
            int y,
            byte transform
        )
        {
            if (transform == 0)
                return;


            Queue.Enqueue(
                new Operation
                {
                    Type =
                        OperationType.BackgroundTransform,

                    X =
                        x,

                    Y =
                        y,

                    Transform =
                        transform
                }
            );
        }


        public static void QueueFurniture(
            int x,
            int y,
            string id,
            byte transform
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    id
                )
            )
            {
                return;
            }


            Queue.Enqueue(
                new Operation
                {
                    Type =
                        OperationType.Furniture,

                    X =
                        x,

                    Y =
                        y,

                    Id =
                        id,

                    Transform =
                        transform
                }
            );
        }


        private static void EnsureInstance()
        {
            if (instance != null)
                return;


            GameObject gameObject =
                new GameObject(
                    "StructurePlacementRuntimeBridge"
                );


            DontDestroyOnLoad(
                gameObject
            );


            instance =
                gameObject.AddComponent<
                    StructurePlacementRuntimeBridge
                >();
        }


        private void Awake()
        {
            if (
                instance != null
                &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );


            BackgroundBlockTransformRegistry.ClearAll();
        }


        private void Update()
        {
            const int maxPerFrame =
                512;


            int processed =
                0;


            while (
                processed <
                maxPerFrame
                &&
                Queue.TryDequeue(
                    out Operation operation
                )
            )
            {
                if (
                    !TryApply(
                        operation
                    )
                )
                {
                    Queue.Enqueue(
                        operation
                    );


                    break;
                }


                processed++;
            }
        }


        private static bool TryApply(
            Operation operation
        )
        {
            WorldManager worldManager =
                WorldManager.Instance;


            if (
                worldManager ==
                null
                ||
                worldManager.GetWorld() ==
                null
            )
            {
                return false;
            }


            switch (
                operation.Type
            )
            {
                case OperationType.ForegroundTransform:
                    BlockTransformRegistry.SetRaw(
                        operation.X,
                        operation.Y,
                        operation.Transform,
                        false
                    );


                    RefreshChunk(
                        worldManager,
                        operation.X,
                        operation.Y,
                        true
                    );
                    return true;


                case OperationType.BackgroundTransform:
                    BackgroundBlockTransformRegistry.SetRaw(
                        operation.X,
                        operation.Y,
                        operation.Transform
                    );


                    RefreshChunk(
                        worldManager,
                        operation.X,
                        operation.Y,
                        false
                    );
                    return true;


                case OperationType.Furniture:
                    FurnitureLayerManager furniture =
                        FurnitureLayerManager.EnsureInstance();


                    if (furniture == null)
                    {
                        return false;
                    }


                    furniture.SetGeneratedFurniture(
                        operation.X,
                        operation.Y,
                        operation.Id,
                        operation.Transform
                    );
                    return true;
            }


            return true;
        }


        private static void RefreshChunk(
            WorldManager worldManager,
            int worldX,
            int worldY,
            bool rebuildCollision
        )
        {
            Game.World.World world =
                worldManager.GetWorld();


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


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (chunk == null)
                return;


            if (
                worldManager.GetChunkRenderer() !=
                null
            )
            {
                worldManager
                    .GetChunkRenderer()
                    .Render(
                        chunk
                    );
            }


            if (!rebuildCollision)
                return;


            try
            {
                MethodInfo getter =
                    worldManager
                        .GetType()
                        .GetMethod(
                            "GetChunkCollision",
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );


                object collision =
                    getter !=
                    null
                        ? getter.Invoke(
                            worldManager,
                            null
                        )
                        : null;


                if (collision == null)
                    return;


                MethodInfo build =
                    collision
                        .GetType()
                        .GetMethod(
                            "BuildChunkCollision",
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );


                if (build != null)
                {
                    build.Invoke(
                        collision,
                        new object[]
                        {
                            chunk
                        }
                    );
                }
            }
            catch
            {
            }
        }
    }
}
