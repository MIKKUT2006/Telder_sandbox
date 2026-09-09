using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.World.Parallax
{
    public static class ParallaxAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Install()
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;

            SceneManager.sceneLoaded +=
                OnSceneLoaded;
        }


        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            WorldManager manager =
                Object.FindFirstObjectByType<
                    WorldManager
                >();

            if (manager == null)
                return;

            ParallaxWorldController existing =
                Object.FindFirstObjectByType<
                    ParallaxWorldController
                >();

            if (existing != null)
                return;

            GameObject root =
                new GameObject(
                    "ProceduralParallax"
                );

            root.AddComponent<
                ParallaxWorldController
            >();

            Debug.Log(
                "PARALLAX V10.6: controller created for loaded world."
            );
        }
    }
}