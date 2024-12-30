using UnityEngine;

public class ChunkHelper : MonoBehaviour
{
    public static int GetChunkXCoordinate(int x)
    {
        int chunkSize = HelperClass.chunkSize;
        return Mathf.FloorToInt(x / (float)chunkSize);
    }
    public static int GetChunkYCoordinate(int y)
    {
        int chunkSize = HelperClass.chunkSize;
        return Mathf.FloorToInt(y / (float)chunkSize);
    }
}
