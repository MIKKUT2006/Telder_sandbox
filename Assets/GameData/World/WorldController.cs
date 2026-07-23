using UnityEngine;
using Game.World.Loading;


namespace Game.World
{

    public class WorldController : MonoBehaviour
    {


        public Transform player;



        private ChunkLoader loader;


        private WorldManager worldManager;



        private int lastChunkX;

        private int lastChunkY;



        private void Start()
        {

            worldManager =
                FindObjectOfType<WorldManager>();


            loader =
                worldManager.GetLoader();



            UpdateChunks();

        }





        private void Update()
        {

            UpdateChunks();

        }





        private void UpdateChunks()
        {

            int chunkX =
                Mathf.FloorToInt(
                    player.position.x / Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    player.position.y / Chunk.SizeY
                );



            if (
                chunkX == lastChunkX &&
                chunkY == lastChunkY
            )
            {
                return;
            }



            lastChunkX = chunkX;

            lastChunkY = chunkY;



            loader.Update(
                chunkX,
                chunkY
            );

        }


    }

}