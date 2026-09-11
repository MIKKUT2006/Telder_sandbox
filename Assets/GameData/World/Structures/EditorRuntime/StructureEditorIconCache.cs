using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.World.Structures.EditorRuntime
{
    public static class StructureEditorIconCache
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string blockId)
        {
            StructureEditorContentScanner.BlockInfo info = null;
            List<StructureEditorContentScanner.BlockInfo> blocks = StructureEditorContentScanner.LoadBlocks();
            for (int i=0;i<blocks.Count;i++) if (blocks[i].ID==blockId) { info=blocks[i]; break; }
            return Get(info);
        }

        public static Texture2D Get(StructureEditorContentScanner.BlockInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.ID)) return null;
            if (cache.TryGetValue(info.ID,out Texture2D cached)) return cached;
#if UNITY_EDITOR
            string key = string.IsNullOrWhiteSpace(info.Texture) ? ShortId(info.ID) : info.Texture;
            string[] guids = AssetDatabase.FindAssets(key + " t:Texture2D");
            Texture2D best = null;
            for (int i=0;i<guids.Length;i++)
            {
                string path=AssetDatabase.GUIDToAssetPath(guids[i]);
                Texture2D candidate=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(candidate==null) continue;
                string file=System.IO.Path.GetFileNameWithoutExtension(path);
                if(string.Equals(file,key,System.StringComparison.OrdinalIgnoreCase)) { best=candidate; break; }
                if(best==null) best=candidate;
            }
            cache[info.ID]=best;
            return best;
#else
            return null;
#endif
        }

        private static string ShortId(string id)
        {
            int colon=id.IndexOf(':'); return colon>=0 && colon<id.Length-1?id.Substring(colon+1):id;
        }

        public static void Clear() { cache.Clear(); }
    }
}
