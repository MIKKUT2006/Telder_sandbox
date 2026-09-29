using UnityEngine;
using Game.World;

namespace Game.Entities.Spawn
{
    public static class EntityRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            WorldManager manager = UnityEngine.Object.FindObjectOfType<WorldManager>();
            if (manager == null)
                return;

            if (manager.GetComponent<EntitySpawnManager>() == null)
                manager.gameObject.AddComponent<EntitySpawnManager>();
        }
    }
}
