using System;
using System.IO;
using UnityEngine;

namespace Game.GameplaySystems
{
    internal static class GameplaySaveIO
    {
        public static T Load<T>(string path) where T : new()
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new T();

            try
            {
                string json = File.ReadAllText(path);
                T value = JsonUtility.FromJson<T>(json);
                return value == null ? new T() : value;
            }
            catch (Exception e)
            {
                Debug.LogWarning("GAMEPLAY SAVE: Failed to load '" + path + "': " + e.Message);
                return new T();
            }
        }

        public static bool Save<T>(string path, T value, bool pretty = false)
        {
            if (string.IsNullOrWhiteSpace(path) || value == null)
                return false;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(value, pretty));

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temp, path, null);
                    }
                    catch
                    {
                        File.Delete(path);
                        File.Move(temp, path);
                    }
                }
                else
                {
                    File.Move(temp, path);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("GAMEPLAY SAVE: Failed to save '" + path + "': " + e.Message);
                try
                {
                    string temp = path + ".tmp";
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch { }
                return false;
            }
        }
    }
}
