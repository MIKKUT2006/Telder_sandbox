using Game.Blocks;

namespace Game.World.Lighting
{
    public interface ILightWorld
    {
        ushort GetBlock(
            int worldX,
            int worldY
        );


        BlockDefinition GetBlockDefinition(
            ushort blockID
        );


        ChunkLightData GetLightData(
            int worldX,
            int worldY
        );


        void SetLight(
            int worldX,
            int worldY,
            byte sunlight,
            byte red,
            byte green,
            byte blue
        );


        bool IsLoaded(
            int worldX,
            int worldY
        );
    }
}