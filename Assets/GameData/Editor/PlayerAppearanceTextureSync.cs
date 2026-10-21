#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// One-time migration for the previous appearance editor implementation.
    /// Player appearance textures now load directly from ResourcePacks and are never copied to Resources.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayerAppearanceTextureSync
    {
        private const string LegacyCopies = "Assets/GameData/Resources/TelderPlayerAppearance";

        static PlayerAppearanceTextureSync()
        {
            EditorApplication.delayCall += RemoveLegacyCopies;
        }

        [MenuItem("Tools/Game/Character/Remove Legacy Appearance Texture Copies")]
        private static void RemoveLegacyCopies()
        {
            string absolute = Path.GetFullPath(LegacyCopies);
            bool existed = Directory.Exists(absolute) || File.Exists(absolute + ".meta");
            if (!existed) return;

            if (AssetDatabase.IsValidFolder(LegacyCopies))
            {
                AssetDatabase.DeleteAsset(LegacyCopies);
            }
            else
            {
                if (Directory.Exists(absolute)) FileUtil.DeleteFileOrDirectory(absolute);
                if (File.Exists(absolute + ".meta")) FileUtil.DeleteFileOrDirectory(absolute + ".meta");
            }

            AssetDatabase.Refresh();
            Debug.Log("APPEARANCE: removed legacy Resources/TelderPlayerAppearance copies. The editor now reads directly from ResourcePacks.");
        }
    }
}
#endif
