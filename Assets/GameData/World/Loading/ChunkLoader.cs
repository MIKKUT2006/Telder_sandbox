using Game.World.Collision;
using Game.World.Generation;
using Game.World.Rendering;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Collision;

namespace Game.World.Loading
{

    public class ChunkLoader
    {

        private readonly World world;

        private readonly ChunkCollision collision;

        private readonly WorldGenerator generator;

        private readonly WorldSettings settings;

        private readonly ChunkRenderer renderer;


        private readonly HashSet<Vector2Int> loadedChunks =
            new HashSet<Vector2Int>();


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
        }


        public void Update(
            int playerChunkX,
            int playerChunkY
        )
        {

            LoadAroundPlayer(
                playerChunkX,
                playerChunkY
            );


            UnloadFarChunks(
                playerChunkX,
                playerChunkY
            );

        }


        private void LoadAroundPlayer(
            int playerChunkX,
            int playerChunkY
        )
        {

            for (
                int cx = -settings.ViewDistance;
                cx <= settings.ViewDistance;
                cx++
            )
            {

                for (
                    int cy = -settings.ViewDistance;
                    cy <= settings.ViewDistance;
                    cy++
                )
                {

                    int chunkX =
                        playerChunkX +
                        cx;


                    int chunkY =
                        playerChunkY +
                        cy;


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
                        continue;
                    }

                    Chunk chunk =
    world.CreateChunk(
        chunkX,
        chunkY
    );


                    generator.GenerateChunk(
                        chunk
                    );


                    renderer.Render(
                        chunk
                    );


                    collision.BuildChunkCollision(chunk);


                    loadedChunks.Add(
                        position
                    );

                }

            }

        }


        private void UnloadFarChunks(
            int playerChunkX,
            int playerChunkY
        )
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
                        playerChunkX
                    );


                int distanceY =
                    Mathf.Abs(
                        position.y -
                        playerChunkY
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


        public IEnumerable<Vector2Int> GetLoadedChunks()
        {

            return loadedChunks;

        }

    }

}