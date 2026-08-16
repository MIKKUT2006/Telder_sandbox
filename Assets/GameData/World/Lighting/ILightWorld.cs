using Game.Blocks;

namespace Game.World.Lighting
{
    public interface ILightWorld
    {
        bool IsLoaded(
            int worldX,
            int worldY
        );

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

        LightNode GetLight(
            int worldX,
            int worldY
        );

        void SetLight(
            int worldX,
            int worldY,
            byte sun,
            byte red,
            byte green,
            byte blue
        );
    }
}