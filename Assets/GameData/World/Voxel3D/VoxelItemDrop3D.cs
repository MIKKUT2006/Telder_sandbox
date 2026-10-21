using System;
using System.IO;
using System.Collections.Generic;
using Random=UnityEngine.Random;
using Game.Combat;
using Game.Inventory;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [Serializable] public sealed class VoxelDropRecord{public ItemStack Stack;public Vector3 Position,Velocity;}
    [Serializable] public sealed class VoxelDropFile{public List<VoxelDropRecord> Drops=new List<VoxelDropRecord>();}
    public sealed class VoxelItemDrop3D : MonoBehaviour
    {
        private static readonly List<VoxelItemDrop3D> active=new List<VoxelItemDrop3D>();
        private ItemStack stack;
        private Vector3 velocity;
        private SpriteRenderer visual;
        private float born;
        private Material material;
        public static void Save(VoxelWorld3DRuntime rt)
        {
            string path=Voxel3DSaveRuntime.AuxiliaryPath("drops");if(path==null)return;
            try{var file=new VoxelDropFile();foreach(var item in active)if(item!=null&&item.transform.IsChildOf(rt.transform)&&item.stack!=null&&!item.stack.IsEmpty)file.Drops.Add(new VoxelDropRecord{Stack=item.stack.Clone(),Position=item.transform.position,Velocity=item.velocity});Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(file,true));}
            catch(Exception e){Debug.LogWarning("VOXEL DROPS SAVE: "+e.Message);}
        }
        public static void Restore(VoxelWorld3DRuntime rt)
        {
            string path=Voxel3DSaveRuntime.AuxiliaryPath("drops");if(path==null||!File.Exists(path))return;
            try{var file=JsonUtility.FromJson<VoxelDropFile>(File.ReadAllText(path));if(file?.Drops!=null)foreach(var item in file.Drops)if(item.Stack!=null&&!item.Stack.IsEmpty)Spawn(item.Stack,item.Position);}
            catch(Exception e){Debug.LogWarning("VOXEL DROPS LOAD: "+e.Message);}
        }
        public static bool Spawn(string id,int count,Vector3 at)
        {var stack=new ItemStack();stack.Set(id,count);return Spawn(stack,at);}
        public static bool Spawn(ItemStack stack,Vector3 at)
        {
            if(stack==null||stack.IsEmpty)return false;
            if(active.Count>=256)
            {
                foreach(var drop in active)if(drop!=null&&drop.stack.ItemId==stack.ItemId&&!drop.stack.HasInstanceData&&!stack.HasInstanceData)
                {drop.stack.Count+=stack.Count;drop.transform.position=at;return true;}
                return false; // The caller keeps the stack if no safe capacity is available.
            }
            var sprite=ItemSpriteResolver.Resolve(stack.ItemId);if(sprite==null)return false;
            var item=new GameObject("Voxel Drop "+stack.ItemId).AddComponent<VoxelItemDrop3D>();
            item.stack=stack.Clone();item.transform.SetParent(VoxelWorld3DRuntime.Instance.transform,false);item.transform.position=at;item.born=Time.time;
            item.visual=item.gameObject.AddComponent<SpriteRenderer>();item.visual.sprite=sprite;
            var shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");if(shader!=null){item.material=new Material(shader);item.visual.sharedMaterial=item.material;}
            item.velocity=Random.insideUnitSphere*1.2f+Vector3.up*2;
            float size=Mathf.Max(.001f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));
            item.transform.localScale=Vector3.one*(.45f/size);active.Add(item);return true;
        }
        private void Update()
        {
            var rt=VoxelWorld3DRuntime.Instance;if(rt==null){Destroy(gameObject);return;}
            if(!rt.Ready||!rt.IsLoaded(Vector3Int.FloorToInt(transform.position)))return;
            float dt=Mathf.Min(.05f,Time.deltaTime);velocity+=Vector3.down*12*dt;
            Vector3 next=transform.position+velocity*dt;
            if(UnityEngine.Physics.SphereCast(transform.position,.1f,velocity.normalized,out RaycastHit hit,velocity.magnitude*dt,~(1<<2),QueryTriggerInteraction.Ignore))
            {next=hit.point+hit.normal*.11f;velocity=Vector3.ProjectOnPlane(velocity,hit.normal)*.4f;}
            transform.position=next;
            var camera=Camera.main;if(camera!=null)transform.rotation=camera.transform.rotation;
            visual.color=rt.Lighting.Sample(transform.position);
            var player=rt.Player;bool alive=player!=null&&player.GetComponent<Game.PlayerStats.PlayerStats>()?.IsDead!=true;float distance=player!=null?Vector3.Distance(player.position,transform.position):100;
            if(alive&&Time.time-born>.6f&&distance<1.2f&&rt.Inventory!=null)
            {
                if(stack.HasInstanceData){if(rt.Inventory.TryAddStack(stack))Destroy(gameObject);}
                else{int remainder=rt.Inventory.AddItem(stack.ItemId,stack.Count);if(remainder==0)Destroy(gameObject);else stack.Count=remainder;}
            }
            else if(alive&&Time.time-born>.6f&&distance<2.2f)velocity=(player.position+Vector3.up*.5f-transform.position).normalized*4;
        }
        private void OnDestroy(){active.Remove(this);if(material!=null)Destroy(material);}
    }
}
