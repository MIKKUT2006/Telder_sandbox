#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Moves the runtime chunk-lighting shader into a Resources folder, preserving
/// its .meta GUID. This ensures it is packed into standalone builds when the
/// game creates its lighting material dynamically through C#.
/// </summary>
[InitializeOnLoad]
public static class TelderEnsureLightingShaderInResources
{
    private const string SourcePath = "Assets/GameData/Shaders/ChunkLitSprite.shader";
    private const string DestinationPath = "Assets/GameData/Resources/Shaders/ChunkLitSprite.shader";
    private static bool queued;

    static TelderEnsureLightingShaderInResources()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += EnsureShaderIsInResources;
    }

    private static void EnsureShaderIsInResources()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureShaderIsInResources;
            return;
        }

        bool sourceExists = File.Exists(SourcePath);
        bool destinationExists = File.Exists(DestinationPath);

        if (!sourceExists)
        {
            if (destinationExists)
                Debug.Log("TELDER LIGHTING BUILD FIX: shader is already in Resources.");
            return;
        }

        string directory = Path.GetDirectoryName(DestinationPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (destinationExists)
        {
            string sourceText = File.ReadAllText(SourcePath);
            string destinationText = File.ReadAllText(DestinationPath);
            if (sourceText == destinationText)
            {
                AssetDatabase.DeleteAsset(SourcePath);
                AssetDatabase.Refresh();
                Debug.Log("TELDER LIGHTING BUILD FIX: removed duplicate shader; Resources copy will be used.");
            }
            else
            {
                Debug.LogError("TELDER LIGHTING BUILD FIX: both source and Resources shaders exist but differ. " +
                    "Resolve duplicate Game/ChunkLitSprite shaders before building.");
            }
            return;
        }

        string error = AssetDatabase.MoveAsset(SourcePath, DestinationPath);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("TELDER LIGHTING BUILD FIX: failed to move shader into Resources: " + error);
            return;
        }

        AssetDatabase.Refresh();
        Game.World.Lighting.ChunkLightingShaderProvider.ResetCache();
        Debug.Log("TELDER LIGHTING BUILD FIX: moved Game/ChunkLitSprite to Resources/Shaders. " +
            "Its original .meta GUID is preserved; rebuild the player to restore terrain lighting and shadows.");
    }
}
#endif
