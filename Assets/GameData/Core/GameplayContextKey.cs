using System;
using UnityEngine.SceneManagement;
using Game.Save;
using Game.World.Dimensions;

namespace Game.GameplaySystems
{
    /// <summary>
    /// Stable save+dimension key used by small gameplay-side persistence systems.
    /// This deliberately avoids reflection: the previous implementation scanned
    /// every loaded assembly/type every time Get() was called, and Get() is used
    /// from runtime Update paths.
    /// </summary>
    internal static class GameplayContextKey
    {
        public static string Get()
        {
            string save = SaveGameRuntime.HasActiveSave
                ? SaveGameRuntime.CurrentSaveId
                : "default";

            string dimension = null;

            if (SaveGameRuntime.HasActiveSave)
            {
                try
                {
                    DimensionDefinition current = DimensionTravelRuntime.Current;
                    if (current != null)
                        dimension = current.Name;
                }
                catch
                {
                    // Scene name is a safe fallback during transitions/startup.
                }
            }

            if (string.IsNullOrWhiteSpace(dimension))
                dimension = SceneManager.GetActiveScene().name;

            return Sanitize(save) + "__" + Sanitize(dimension);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "default";

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value
                .Replace(':', '_')
                .Replace('/', '_')
                .Replace('\\', '_');
        }
    }
}
