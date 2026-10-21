#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.World.Dimensions;

namespace Game.EditorTools
{
    // Renderer feature registration is an asset setup step, including Renderer2D.
    [InitializeOnLoad]
    public static class PlanetRingRendererInstaller
    {
        static PlanetRingRendererInstaller() { EditorApplication.delayCall += Install; }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool changed = false;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableRendererData", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableRendererData data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null) continue;
                bool exists = false;
                foreach (var feature in data.rendererFeatures)
                    if (feature is DimensionRiftRendererFeature)
                    {
                        exists=true;
                        if(!feature.isActive){feature.SetActive(true);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(data);changed=true;}
                        break;
                    }
                if (exists) continue;
                var added = ScriptableObject.CreateInstance<DimensionRiftRendererFeature>();
                added.name = "Telder Pixel Space Distortion";
                AssetDatabase.AddObjectToAsset(added, data); data.rendererFeatures.Add(added);
                added.Create(); EditorUtility.SetDirty(data); changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
        }
    }
}
#endif
