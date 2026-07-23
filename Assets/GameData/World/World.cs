using System.Collections.Generic;


namespace Game.World
{

    public class World
    {

        private Dictionary<Vector2Int, Chunk> chunks;



        public World()
        {

            chunks =
                new Dictionary<Vector2Int, Chunk>();

        }





        public Chunk GetChunk(
            int x,
            int y
        )
        {

            Vector2Int position =
                new Vector2Int(
                    x,
                    y
                );



            if (chunks.TryGetValue(
                position,
                out Chunk chunk
            ))
            {
                return chunk;
            }



            return null;

        }





        public Chunk CreateChunk(
            int x,
            int y
        )
        {

            Vector2Int position =
                new Vector2Int(
                    x,
                    y
                );



            if (chunks.ContainsKey(position))
            {
                return chunks[position];
            }



            Chunk chunk =
                new Chunk(
                    x,
                    y
                );



            chunks.Add(
                position,
                chunk
            );



            return chunk;

        }





        public void RemoveChunk(
    int chunkX,
    int chunkY
)
        {

            Vector2Int position =
                new Vector2Int(
                    chunkX,
                    chunkY
                );


            chunks.Remove(
                position
            );

        }





        public bool HasChunk(
            int x,
            int y
        )
        {

            return chunks.ContainsKey(
                new Vector2Int(
                    x,
                    y
                )
            );

        }





        public void Clear()
        {

            chunks.Clear();

        }



        public int ChunkCount
        {

            get
            {
                return chunks.Count;
            }

        }


    }

}