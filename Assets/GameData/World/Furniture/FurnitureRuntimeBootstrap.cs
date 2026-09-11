using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory;

namespace Game.World.Furniture
{
    public class FurnitureRuntimeBootstrap : MonoBehaviour
    {
        private static FurnitureRuntimeBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if(instance!=null) return;
            GameObject go=new GameObject("FurnitureRuntimeBootstrap");
            instance=go.AddComponent<FurnitureRuntimeBootstrap>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable(){SceneManager.sceneLoaded+=OnSceneLoaded;}
        private void OnDisable(){SceneManager.sceneLoaded-=OnSceneLoaded;}
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode){StartCoroutine(Setup());}

        private IEnumerator Setup()
        {
            for(int i=0;i<300 && WorldManager.Instance==null;i++) yield return null;
            if(WorldManager.Instance==null) yield break;

            if(FurnitureLayerManager.Instance==null)
            {
                GameObject layer=new GameObject("FurnitureLayer");
                layer.transform.SetParent(WorldManager.Instance.transform,false);
                layer.AddComponent<FurnitureLayerManager>();
            }

            PlayerInventory inventory=null;
            for(int i=0;i<300 && inventory==null;i++) { inventory=FindFirstObjectByType<PlayerInventory>(); if(inventory==null) yield return null; }
            if(inventory!=null && inventory.GetComponent<FurniturePlacementModeController>()==null)
                inventory.gameObject.AddComponent<FurniturePlacementModeController>();
        }
    }
}
