using UnityEngine;

namespace Game.World.Lighting
{
    /// <summary>
    /// Loads the custom chunk-lighting shader from Resources first.
    /// Shader.Find alone is not sufficient for player builds: Unity can strip
    /// shaders that are only referenced by name from C# code.
    /// </summary>
    public static class ChunkLightingShaderProvider
    {
        private const string ResourcePath = "Shaders/ChunkLitSprite";
        private const string ShaderName = "Game/ChunkLitSprite";

        private static Shader cachedShader;
        private static bool loadAttempted;

        public static Shader Get()
        {
            if (cachedShader != null)
                return cachedShader;

            // Retry if Resources was not ready during an earlier call.
            Shader resourceShader = UnityEngine.Resources.Load<Shader>(ResourcePath);
            if (resourceShader != null)
            {
                cachedShader = resourceShader;
                loadAttempted = true;
                return cachedShader;
            }

            // Editor / compatibility fallback. A build should normally use
            // the Resources asset above so the shader cannot be stripped.
            Shader foundShader = Shader.Find(ShaderName);
            if (foundShader != null)
            {
                cachedShader = foundShader;
                loadAttempted = true;
                return cachedShader;
            }

            if (!loadAttempted)
            {
                Debug.LogError(
                    "WORLD LIGHTING: shader '" + ShaderName + "' could not be loaded. " +
                    "Expected Resources asset at '" + ResourcePath + "'. " +
                    "Terrain will render without custom light/shadow shading until this is fixed.");
                loadAttempted = true;
            }

            return null;
        }

        public static void ResetCache()
        {
            cachedShader = null;
            loadAttempted = false;
        }
    }
}
