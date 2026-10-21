using System;
using System.Collections.Generic;
using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Items;
using Game.World.Voxel3D;
using UnityEngine;

namespace Game.World.Lighting
{
    [DefaultExecutionOrder(-50)]
    public sealed class HandheldLightRuntime : MonoBehaviour
    {
        public const string Tag="held_light";
        private static HandheldLightRuntime instance;
        private PlayerInventory player;
        private World boundWorld;
        private VoxelWorld3DRuntime boundVoxel;
        private HandheldLightField volumeField;
        private readonly List<HandheldLightSource> sources=new List<HandheldLightSource>(1);
        private Texture3D volume;
        private readonly Color32[] pixels=new Color32[31*31*31];
        private float nextUpdate,nextPlayerSearch;
        private static readonly int VolumeId=Shader.PropertyToID("_TelderHeldLightVolume");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {if(instance==null)new GameObject("Handheld light").AddComponent<HandheldLightRuntime>();}
        private void Awake()
        {if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);}
        public static bool TryGetEmission(string id,out LightNode emission)
        {
            emission=default;if(string.IsNullOrWhiteSpace(id))return false;
            ItemRegistry.TryGet(id,out ItemDefinition item);
            BlockDefinition block=null;
            var key=ContentID.Parse(id);
            if(BlockRegistry.Contains(key))block=BlockRegistry.Get(key);
            if(!HasTag(item?.Tags)&&!HasTag(block?.Tags))return false;
            emission=new LightNode(0,item?.LightEmissionR??0,item?.LightEmissionG??0,item?.LightEmissionB??0);
            if(emission.R+emission.G+emission.B==0&&block!=null)
                emission=new LightNode(0,block.LightEmissionR,block.LightEmissionG,block.LightEmissionB);
            if(emission.R+emission.G+emission.B==0)emission=new LightNode(0,15,11,6);
            return true;
        }
        private static bool HasTag(IEnumerable<string> tags)
        {if(tags!=null)foreach(var tag in tags)if(string.Equals(tag?.Trim(),Tag,StringComparison.OrdinalIgnoreCase))return true;return false;}
        private void Update()
        {
            if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.08f;
            var manager=WorldManager.Instance;
            if(manager==null||!manager.IsReady){ClearContext();return;}
            var voxel=VoxelWorld3DRuntime.Instance;
            if(voxel!=null&&voxel.Ready)
            {
                if(boundWorld!=null){boundWorld.HandheldLight.Clear();boundWorld=null;}
                if(boundVoxel!=voxel)
                {
                    ClearContext();boundVoxel=voxel;
                    volumeField=new HandheldLightField(true,cell=>
                    {
                        if(!voxel.IsLoaded(cell))return 15;
                        var def=VoxelBlockCatalog.Get(voxel.World.GetBlock(cell.x,cell.y,cell.z));
                        if(VoxelGameplayState3D.IsOpen(cell))return 0;
                        return def?.LightOpacity??0;
                    });
                }
                Collect(voxel.Inventory,voxel.Player);
                if(volumeField.ReplaceSources(sources))UploadVolume();
                return;
            }
            if(boundVoxel!=null)ClearContext();
            World world=manager.GetWorld();if(world==null){ClearContext();return;}
            if(boundWorld!=world){ClearContext();boundWorld=world;}
            if(player==null&&Time.unscaledTime>=nextPlayerSearch)
            {nextPlayerSearch=Time.unscaledTime+.5f;player=FindFirstObjectByType<PlayerInventory>();}
            Collect(player,player!=null?player.transform:null);
            world.HandheldLight.ReplaceSources(sources);
        }
        private void Collect(PlayerInventory inventory,Transform owner)
        {
            sources.Clear();if(inventory==null||owner==null||!inventory.gameObject.activeInHierarchy)return;
            if(!TryGetEmission(inventory.GetSelectedItemId(),out LightNode light))return;
            Vector3 p=owner.position+Vector3.up*.35f;
            var cell=Vector3Int.FloorToInt(p);if(boundVoxel==null)cell.z=0;
            sources.Add(new HandheldLightSource(cell,light));
        }
        public static void InvalidateVoxel(VoxelWorld3DRuntime runtime)
        {if(instance!=null&&instance.boundVoxel==runtime)instance.volumeField?.Invalidate();}
        public static LightNode SampleVoxel(VoxelWorld3DRuntime runtime,Vector3 position)
        {return instance!=null&&instance.boundVoxel==runtime&&instance.volumeField!=null?instance.volumeField.Get(Vector3Int.FloorToInt(position)):default;}
        private void UploadVolume()
        {
            if(sources.Count==0){Shader.SetGlobalFloat("_TelderHeldLightActive",0);return;}
            if(volume==null){volume=new Texture3D(31,31,31,TextureFormat.RGBA32,false);volume.name="Handheld light cells";volume.filterMode=FilterMode.Point;volume.wrapMode=TextureWrapMode.Clamp;}
            Vector3Int min=sources[0].Cell-new Vector3Int(15,15,15);int at=0;
            for(int z=0;z<31;z++)for(int y=0;y<31;y++)for(int x=0;x<31;x++)
            {var n=volumeField.Get(min+new Vector3Int(x,y,z));pixels[at++]=new Color32((byte)(n.R*17),(byte)(n.G*17),(byte)(n.B*17),0);}
            volume.SetPixels32(pixels);volume.Apply(false,false);Shader.SetGlobalTexture(VolumeId,volume);
            Shader.SetGlobalVector("_TelderHeldLightMin",new Vector4(min.x,min.y,min.z,0));Shader.SetGlobalFloat("_TelderHeldLightActive",1);
        }
        private void ClearContext()
        {
            boundWorld?.HandheldLight.Clear();boundWorld=null;boundVoxel=null;volumeField=null;player=null;
            Shader.SetGlobalFloat("_TelderHeldLightActive",0);
        }
        private void OnDestroy()
        {if(instance!=this)return;ClearContext();if(volume!=null)Destroy(volume);instance=null;}
    }
}
