using System.Collections.Generic;
using UnityEngine;
using Game.Blocks;
using Game.Resources;

namespace Game.World.Voxel3D
{
    public sealed class VoxelChunkRenderer3D : System.IDisposable
    {
        private readonly VoxelWorld3D world;
        private readonly Transform root;
        private readonly VoxelLighting3D lighting;
        private readonly Dictionary<Vector3Int,GameObject> objects=new Dictionary<Vector3Int,GameObject>();
        private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        private readonly Queue<Vector3Int> rebuildQueue=new Queue<Vector3Int>();
        private readonly HashSet<Vector3Int> pendingRebuilds=new HashSet<Vector3Int>();
        private readonly System.Diagnostics.Stopwatch rebuildClock=new System.Diagnostics.Stopwatch();
        public int PendingRebuildCount=>pendingRebuilds.Count;
        private static readonly Vector3Int[] dirs={Vector3Int.right,Vector3Int.left,Vector3Int.up,Vector3Int.down,new Vector3Int(0,0,1),new Vector3Int(0,0,-1)};
        private static readonly int[,] corners={{1,0,0,1,1,0,1,1,1,1,0,1},{0,0,1,0,1,1,0,1,0,0,0,0},{0,1,0,0,1,1,1,1,1,1,1,0},{0,0,1,0,0,0,1,0,0,1,0,1},{1,0,1,1,1,1,0,1,1,0,0,1},{0,0,0,0,1,0,1,1,0,1,0,0}};
        public VoxelChunkRenderer3D(VoxelWorld3D world,Transform root,VoxelLighting3D lighting=null){this.world=world;this.root=root;this.lighting=lighting;}
        public bool HasVisual(Vector3Int c)=>objects.ContainsKey(c);
        public void Remove(Vector3Int c)
        {
            pendingRebuilds.Remove(c);
            if(!objects.TryGetValue(c,out var go))return;
            var filter=go.GetComponent<MeshFilter>();if(filter!=null&&filter.sharedMesh!=null)Object.Destroy(filter.sharedMesh);
            var collider=go.GetComponent<MeshCollider>();if(collider!=null&&collider.sharedMesh!=null)Object.Destroy(collider.sharedMesh);
            Object.Destroy(go);objects.Remove(c);
        }
        public void QueueRebuild(Vector3Int c)
        {
            if(world.TryGetChunk(c,out _)&&pendingRebuilds.Add(c))rebuildQueue.Enqueue(c);
        }
        public void ProcessQueuedRebuilds(int maxChunks=1,double budgetMilliseconds=2.0)
        {
            rebuildClock.Restart();int done=0;
            while(rebuildQueue.Count>0&&done<maxChunks&&(done==0||rebuildClock.Elapsed.TotalMilliseconds<budgetMilliseconds))
            {
                var c=rebuildQueue.Dequeue();if(!pendingRebuilds.Remove(c))continue;
                if(!world.TryGetChunk(c,out _))continue;
                Rebuild(c);done++;
            }
            rebuildClock.Stop();
        }
        public void Rebuild(Vector3Int c)
        {
            if(!world.TryGetChunk(c,out var chunk)){Remove(c);return;}
            Remove(c);var go=new GameObject("VoxelChunk_"+c.x+"_"+c.y+"_"+c.z);
            go.transform.SetParent(root,false);go.transform.position=chunk.WorldOrigin;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color32>();var ao=new List<Vector2>();
            var colliderVertices=new List<Vector3>();var colliderTriangles=new List<int>();var groups=new Dictionary<Material,List<int>>();
            Vector3Int origin=chunk.WorldOrigin;
            for(int x=0;x<16;x++)for(int y=0;y<16;y++)for(int z=0;z<16;z++)
            {
                ushort id=chunk.Get(x,y,z);if(id==0)continue;
                BlockDefinition def=VoxelBlockCatalog.Get(id);if(def==null)continue;
                Vector3Int cell=origin+new Vector3Int(x,y,z);Vector3 local=new Vector3(x,y,z);
                bool open=VoxelGameplayState3D.IsOpen(cell);
                bool plant=!def.Solid||def.CollisionShape=="None";
                var mat=GetMaterial(def,cell);if(!groups.TryGetValue(mat,out var triangles)){triangles=new List<int>();groups[mat]=triangles;}
                List<Bounds> boxes=Boxes(def,open);
                foreach(var box in boxes)
                {
                    bool full=box.size==Vector3.one;
                    for(int face=0;face<6;face++)
                    {
                        Vector3Int neighbor=cell+dirs[face];
                        if(full&&!plant&&FullOccluder(world.GetBlock(neighbor.x,neighbor.y,neighbor.z))&&!VoxelGameplayState3D.IsOpen(neighbor))continue;
                        Color32 light=lighting!=null?lighting.Encoded(neighbor):new Color32(0,0,0,255);
                        int first=vertices.Count,firstCollider=colliderVertices.Count;
                        for(int k=0;k<4;k++)
                        {
                            Vector3 point=new Vector3(corners[face,k*3],corners[face,k*3+1],corners[face,k*3+2]);
                            point=box.min+Vector3.Scale(point,box.size);
                            Vector3 vertex=local+point;vertices.Add(vertex);colors.Add(light);
                            uv.Add(VoxelGameplayState3D.UV(def,cell,face<2?new Vector2(point.z,point.y):face<4?new Vector2(point.x,point.z):new Vector2(point.x,point.y)));
                            ao.Add(new Vector2(face==3?.65f:1f,0));
                            if(def.Solid&&!plant&&!open)colliderVertices.Add(vertex);
                        }
                        AddQuad(triangles,first);
                        if(def.Solid&&!plant&&!open)AddQuad(colliderTriangles,firstCollider);
                    }
                }
            }
            Mesh mesh=new Mesh{name="Voxel visible geometry",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,ao);mesh.SetColors(colors);mesh.subMeshCount=groups.Count;
            var mats=new Material[groups.Count];int sub=0;
            foreach(var group in groups){mesh.SetTriangles(group.Value,sub);mats[sub++]=group.Key;}
            mesh.RecalculateNormals();mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=mats;
            if(colliderVertices.Count>0)
            {
                Mesh collision=new Mesh{name="Voxel solid geometry",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                collision.SetVertices(colliderVertices);collision.SetTriangles(colliderTriangles,0);collision.RecalculateBounds();
                go.AddComponent<MeshCollider>().sharedMesh=collision;
            }
            objects[c]=go;
        }
        private static bool FullOccluder(ushort id)
        {var d=VoxelBlockCatalog.Get(id);return VoxelBlockCatalog.Opaque(id)&&(string.IsNullOrEmpty(d.CollisionShape)||d.CollisionShape=="Full")&&!VoxelBlockCatalog.Tag(d,"door")&&!VoxelBlockCatalog.Tag(d,"hatch");}
        private static void AddQuad(List<int> triangles,int first)
        {triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);}
        private static List<Bounds> Boxes(BlockDefinition def,bool open)
        {
            var result=new List<Bounds>(2);
            if(VoxelBlockCatalog.Tag(def,"door")||string.Equals(def.Behavior,"Door",System.StringComparison.OrdinalIgnoreCase))
            {result.Add(open?new Bounds(new Vector3(.94f,.5f,.5f),new Vector3(.12f,1,1)):new Bounds(new Vector3(.5f,.5f,.5f),new Vector3(1,1,.12f)));return result;}
            if(VoxelBlockCatalog.Tag(def,"hatch")||string.Equals(def.Behavior,"Hatch",System.StringComparison.OrdinalIgnoreCase))
            {result.Add(open?new Bounds(new Vector3(.5f,.5f,.94f),new Vector3(1,1,.12f)):new Bounds(new Vector3(.5f,.06f,.5f),new Vector3(1,.12f,1)));return result;}
            switch(def.CollisionShape)
            {
                case "HalfBottom":result.Add(new Bounds(new Vector3(.5f,.25f,.5f),new Vector3(1,.5f,1)));break;
                case "HalfTop":result.Add(new Bounds(new Vector3(.5f,.75f,.5f),new Vector3(1,.5f,1)));break;
                case "StairUpRight":case "StairUpLeft":
                    result.Add(new Bounds(new Vector3(.5f,.25f,.5f),new Vector3(1,.5f,1)));
                    result.Add(new Bounds(new Vector3(def.CollisionShape=="StairUpRight"?.75f:.25f,.75f,.5f),new Vector3(.5f,.5f,1)));break;
                case "Custom":
                    if(def.CollisionRects!=null)foreach(var r in def.CollisionRects)result.Add(new Bounds(new Vector3(r.X+r.Width*.5f,r.Y+r.Height*.5f,.5f),new Vector3(r.Width,r.Height,1)));break;
                default:
                    if(!def.Solid||def.CollisionShape=="None")
                    {result.Add(new Bounds(new Vector3(.5f,.5f,.5f),new Vector3(1,1,.015f)));result.Add(new Bounds(new Vector3(.5f,.5f,.5f),new Vector3(.015f,1,1)));}
                    else result.Add(new Bounds(Vector3.one*.5f,Vector3.one));break;
            }
            return result;
        }
        private Material GetMaterial(BlockDefinition def,Vector3Int cell)
        {
            string texture=VoxelGameplayState3D.TextureAt(def,cell);
            string key=texture??"__fallback";if(materials.TryGetValue(key,out var material))return material;
            Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelLit")??Shader.Find("Unlit/Texture");
            material=new Material(shader){name="Voxel "+key};
            if(TextureManager.TryGet(texture,out var image)){image.filterMode=FilterMode.Point;material.mainTexture=image;}
            materials[key]=material;return material;
        }
        public void Dispose()
        {rebuildQueue.Clear();pendingRebuilds.Clear();var keys=new List<Vector3Int>(objects.Keys);foreach(var c in keys)Remove(c);foreach(var material in materials.Values)Object.Destroy(material);materials.Clear();}
    }
}
