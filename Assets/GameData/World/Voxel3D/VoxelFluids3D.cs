using System;
using System.Collections.Generic;
using System.IO;
using Game.Fluids;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [Serializable] public sealed class VoxelFluidCell {public int X,Y,Z,Level;public string Id;public bool Source;}
    [Serializable] public sealed class VoxelFluidFile {public List<VoxelFluidCell> Cells=new List<VoxelFluidCell>();}
    public sealed class VoxelFluids3D : IDisposable
    {
        private readonly VoxelWorld3DRuntime runtime;
        private readonly Dictionary<Vector3Int,VoxelFluidCell> cells=new Dictionary<Vector3Int,VoxelFluidCell>();
        private readonly Dictionary<Vector2Int,int> surfaces=new Dictionary<Vector2Int,int>();
        private readonly Dictionary<Vector3Int,GameObject> visuals=new Dictionary<Vector3Int,GameObject>();
        private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        private readonly Queue<Vector3Int> pending=new Queue<Vector3Int>();private readonly HashSet<Vector3Int> queued=new HashSet<Vector3Int>();
        private readonly Transform root;
        private readonly string path;
        private float nextTick,nextSave;private bool dirty;
        private static readonly Vector3Int[] horizontal={Vector3Int.left,Vector3Int.right,new Vector3Int(0,0,1),new Vector3Int(0,0,-1)};
        public VoxelFluids3D(VoxelWorld3DRuntime runtime,Transform root)
        {
            this.runtime=runtime;this.root=root;path=Voxel3DSaveRuntime.AuxiliaryPath("liquids");
            try{if(path!=null&&File.Exists(path)){var file=JsonUtility.FromJson<VoxelFluidFile>(File.ReadAllText(path));if(file?.Cells!=null)foreach(var p in file.Cells)cells[new Vector3Int(p.X,p.Y,p.Z)]=p;}}
            catch(Exception e){Debug.LogWarning("VOXEL LIQUID: "+e.Message);}
            foreach(var p in cells)Schedule(p.Key);
        }
        public VoxelFluidCell At(Vector3 position)=>At(Vector3Int.FloorToInt(position));
        public VoxelFluidCell At(Vector3Int p)
        {
            if(!runtime.IsLoaded(p)||runtime.World.GetBlock(p.x,p.y,p.z)!=0)return null;
            if(cells.TryGetValue(p,out var entry))return entry.Level>0?entry:null;
            var key=new Vector2Int(p.x,p.z);
            if(!surfaces.TryGetValue(key,out int surface)){surface=runtime.Generator.GetSurfaceHeight(p.x,p.z);if(surfaces.Count>32768)surfaces.Clear();surfaces[key]=surface;}
            var biome=runtime.Generator.GetBiome(p.x,p.z);
            if(biome!=null&&p.y>surface&&p.y<=biome.WaterLevel&&!string.IsNullOrEmpty(biome.FluidId))
                return new VoxelFluidCell{X=p.x,Y=p.y,Z=p.z,Id=biome.FluidId,Level=8,Source=true};
            return null;
        }
        public bool PlaceSource(Vector3Int p,string id)
        {
            if(!runtime.IsLoaded(p)||runtime.World.GetBlock(p.x,p.y,p.z)!=0||!FluidRegistry.TryGet(id,out _))return false;
            Set(p,id,8,true);Around(p);return true;
        }
        public void Around(Vector3Int p){Schedule(p);Schedule(p+Vector3Int.up);Schedule(p+Vector3Int.down);foreach(var d in horizontal)Schedule(p+d);}
        private void Schedule(Vector3Int p){if(queued.Count<8192&&queued.Add(p))pending.Enqueue(p);}
        private void Set(Vector3Int p,string id,int level,bool source)
        {
            if(cells.TryGetValue(p,out var old)&&old.Id==id&&old.Level==level&&old.Source==source)return;
            cells[p]=new VoxelFluidCell{X=p.x,Y=p.y,Z=p.z,Id=id,Level=level,Source=source};dirty=true;runtime.Refresh(p);Around(p);
        }
        public void Tick()
        {
            if(Time.time>=nextTick)
            {
                nextTick=Time.time+.333f;int budget=Mathf.Min(96,pending.Count);
                while(budget-->0){Vector3Int p=pending.Dequeue();queued.Remove(p);if(!runtime.IsLoaded(p)||runtime.World.GetBlock(p.x,p.y,p.z)!=0)continue;Flow(p);}
            }
            if(dirty&&Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+2;Save();}
        }
        private void Flow(Vector3Int p)
        {
            var current=At(p);if(current!=null&&current.Source){Around(p+Vector3Int.down);foreach(var d in horizontal)Schedule(p+d);return;}
            var above=At(p+Vector3Int.up);string id=null;int level=0;
            if(above!=null){id=above.Id;level=8;}
            else foreach(var d in horizontal)
            {
                var neighbor=At(p+d);if(neighbor==null)continue;
                bool floor=runtime.World.GetBlock(p.x+d.x,p.y-1,p.z+d.z)!=0||At(p+d+Vector3Int.down)!=null;
                if(!floor)continue;
                if(neighbor.Level-1>level){level=neighbor.Level-1;id=neighbor.Id;}
            }
            if(level>0)Set(p,id,level,false);else if(current!=null)Set(p,current.Id,0,false);
        }
        public void Rebuild(Vector3Int chunk)
        {
            if(visuals.TryGetValue(chunk,out var previous)){var mesh=previous.GetComponent<MeshFilter>().sharedMesh;if(mesh!=null)UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(previous);visuals.Remove(chunk);}
            if(!runtime.World.TryGetChunk(chunk,out _))return;
            var vertices=new List<Vector3>();var colors=new List<Color32>();var uv=new List<Vector2>();var groups=new Dictionary<Material,List<int>>();Vector3Int origin=chunk*16;
            for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=0;y<16;y++)
            {
                var p=origin+new Vector3Int(x,y,z);var fluid=At(p);if(fluid==null)continue;
                if(!FluidRegistry.TryGet(fluid.Id,out var definition))continue;
                if(!materials.TryGetValue(fluid.Id,out var material))
                {
                    var shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelLiquid");if(shader==null)continue;
                    material=new Material(shader);Color color;if(!ColorUtility.TryParseHtmlString(definition.Color,out color))color=new Color(.2f,.5f,.9f,.7f);
                    material.SetColor("_Color",color);material.SetFloat("_Glow",definition.IsHot?1:0);materials[fluid.Id]=material;
                }
                if(!groups.TryGetValue(material,out var triangles)){triangles=new List<int>();groups[material]=triangles;}
                float height=Mathf.Clamp01(fluid.Level/8f);Vector3 a=new Vector3(x,y,z);
                if(At(p+Vector3Int.up)==null)Quad(a+new Vector3(0,height,0),a+new Vector3(0,height,1),a+new Vector3(1,height,1),a+new Vector3(1,height,0),p,vertices,uv,colors,triangles);
                foreach(var d in horizontal)
                {
                    var other=At(p+d);if(other!=null&&other.Id==fluid.Id&&other.Level>=fluid.Level)continue;
                    if(runtime.World.GetBlock(p.x+d.x,p.y,p.z+d.z)!=0)continue;
                    if(d.x==1)Quad(a+new Vector3(1,0,0),a+new Vector3(1,height,0),a+new Vector3(1,height,1),a+new Vector3(1,0,1),p,vertices,uv,colors,triangles);
                    else if(d.x==-1)Quad(a+new Vector3(0,0,1),a+new Vector3(0,height,1),a+new Vector3(0,height,0),a,p,vertices,uv,colors,triangles);
                    else if(d.z==1)Quad(a+new Vector3(1,0,1),a+new Vector3(1,height,1),a+new Vector3(0,height,1),a+new Vector3(0,0,1),p,vertices,uv,colors,triangles);
                    else Quad(a,a+new Vector3(0,height,0),a+new Vector3(1,height,0),a+new Vector3(1,0,0),p,vertices,uv,colors,triangles);
                }
            }
            if(vertices.Count==0)return;
            var go=new GameObject("Voxel pixel liquids");go.transform.SetParent(root,false);go.transform.position=origin;
            var result=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};result.SetVertices(vertices);result.SetUVs(0,uv);result.SetColors(colors);result.subMeshCount=groups.Count;
            var mats=new Material[groups.Count];int sub=0;foreach(var g in groups){result.SetTriangles(g.Value,sub);mats[sub++]=g.Key;}
            result.RecalculateNormals();result.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=result;go.AddComponent<MeshRenderer>().sharedMaterials=mats;visuals[chunk]=go;
        }
        private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3Int p,List<Vector3> vertices,List<Vector2> uv,List<Color32> colors,List<int> triangles)
        {
            int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            uv.Add(Vector2.zero);uv.Add(Vector2.up);uv.Add(Vector2.one);uv.Add(Vector2.right);
            Color32 light=runtime.Lighting.Encoded(p);for(int i=0;i<4;i++)colors.Add(light);
            triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
        }
        public void Flush(){Save();}
        private void Save()
        {
            if(path==null)return;
            try{Directory.CreateDirectory(Path.GetDirectoryName(path));var file=new VoxelFluidFile();file.Cells.AddRange(cells.Values);File.WriteAllText(path,JsonUtility.ToJson(file,true));dirty=false;}
            catch(Exception e){Debug.LogWarning("VOXEL LIQUID SAVE: "+e.Message);}
        }
        public void Remove(Vector3Int chunk)
        {
            if(!visuals.TryGetValue(chunk,out var go))return;
            if(go!=null){var mesh=go.GetComponent<MeshFilter>().sharedMesh;if(mesh!=null)UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(go);}visuals.Remove(chunk);
        }
        public void Dispose(){Save();foreach(var key in new List<Vector3Int>(visuals.Keys))Remove(key);foreach(var m in materials.Values)UnityEngine.Object.Destroy(m);}
    }
}
