using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelCellSimulation3D
    {
        private readonly VoxelWorld3DRuntime rt;
        private readonly Queue<Vector3Int> queue=new Queue<Vector3Int>();private readonly HashSet<Vector3Int> queued=new HashSet<Vector3Int>();
        private float next;private readonly HashSet<Vector3Int> moved=new HashSet<Vector3Int>();
        private static readonly Vector3Int[] neighbors={Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right,new Vector3Int(0,0,1),new Vector3Int(0,0,-1)};
        public VoxelCellSimulation3D(VoxelWorld3DRuntime runtime){rt=runtime;}
        private void Add(Vector3Int p){if(queued.Count<8192&&queued.Add(p))queue.Enqueue(p);}
        public void Around(Vector3Int p){Add(p);foreach(var d in neighbors)Add(p+d);}
        public void Loaded(VoxelChunk3D chunk)
        {
            var o=chunk.WorldOrigin;
            for(int x=0;x<16;x++)for(int y=0;y<16;y++)for(int z=0;z<16;z++)
            {var def=VoxelBlockCatalog.Get(chunk.Get(x,y,z));if(def?.Plant!=null){var p=o+new Vector3Int(x,y,z);Game.Gameplay.Plants.PlantRuntime.Track(def.ID,p.x,p.y,p.z,true);}if(def!=null&&(def.alwaysFurniture||VoxelBlockCatalog.Tag(def,"falling")))Around(o+new Vector3Int(x,y,z));}
        }
        public void Tick()
        {
            if(Time.time<next)return;next=Time.time+.09f;int budget=Mathf.Min(32,queue.Count);moved.Clear();
            while(budget-->0)
            {
                var p=queue.Dequeue();queued.Remove(p);if(!rt.IsLoaded(p)||moved.Contains(p))continue;
                ushort id=rt.World.GetBlock(p.x,p.y,p.z);var def=VoxelBlockCatalog.Get(id);if(def==null)continue;
                var below=p+Vector3Int.down;if(!rt.IsLoaded(below))continue;
                if(VoxelBlockCatalog.Tag(def,"falling"))
                {
                    if(rt.World.GetBlock(below.x,below.y,below.z)!=0)continue;
                    var body=rt.Motor!=null?rt.Motor.GetComponent<CharacterController>():null;
                    if(body!=null&&body.bounds.Intersects(new Bounds((Vector3)below+Vector3.one*.5f,Vector3.one)))continue;
                    rt.SetBlock(p,0);rt.SetBlock(below,id);moved.Add(below);continue;
                }
                if(!def.alwaysFurniture&&def.MultiBlock==null)continue;
                var anchor=rt.Services.Anchor(p);var multi=def.MultiBlock;
                bool hanging=VoxelBlockCatalog.Tag(def,"hanging")||VoxelBlockCatalog.Tag(def,"vines");
                Vector3Int support=anchor+(hanging?Vector3Int.up*(multi!=null?Mathf.Max(1,multi.Height):1):Vector3Int.down);
                if(!rt.IsLoaded(support)||rt.World.GetBlock(support.x,support.y,support.z)!=0)continue;
                bool wall=VoxelBlockCatalog.Tag(def,"torch")||VoxelBlockCatalog.Tag(def,"ladder");
                if(wall)
                {bool attached=false;foreach(var d in neighbors)if(rt.World.GetBlock(anchor.x+d.x,anchor.y+d.y,anchor.z+d.z)!=0){attached=true;break;}if(attached)continue;}
                VoxelBlockInteraction3D.Break(rt,anchor,true);
            }
        }
    }
}
