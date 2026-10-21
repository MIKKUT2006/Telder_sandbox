using System.Collections.Generic;
using Game.Blocks;
using Game.Content;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public static class VoxelBlockCatalog
    {
        private static readonly Dictionary<ushort,BlockDefinition> definitions = new Dictionary<ushort,BlockDefinition>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){definitions.Clear();}
        public static BlockDefinition Get(ushort id)
        {
            if (id == 0) return null;
            if (definitions.TryGetValue(id,out var cached)) return cached;
            try { var key=BlockIDRegistry.GetContentID(id);cached=BlockRegistry.Contains(key)?BlockRegistry.Get(key):null; }
            catch { cached=null; }
            definitions[id]=cached; return cached;
        }
        public static ushort Id(string id)
        { try { var key=ContentID.Parse(id);return BlockIDRegistry.Contains(key)?BlockIDRegistry.GetID(key):(ushort)0; } catch {return 0;} }
        public static bool Tag(BlockDefinition d,string tag) => Game.Visuals.PixelEffects.PixelWorldOcclusion.HasTag(d,tag);
        public static bool Opaque(ushort id)
        {var d=Get(id);return d!=null && d.Solid && d.LightOpacity>=15 && d.CollisionShape!="None";}
    }

    // Scene-independent light colors also work with URP's 2D renderer.
    // Sources are logical cells; hundreds of torches create no Unity Lights.
    public sealed class VoxelLighting3D
    {
        private readonly VoxelWorld3D world;
        private readonly Voxel3DGenerator generator;
        private readonly Dictionary<Vector2Int,int> columnTops=new Dictionary<Vector2Int,int>();
        private readonly Dictionary<Vector3Int,Color32> samples=new Dictionary<Vector3Int,Color32>();
        private readonly Dictionary<Vector3Int,Color> sources=new Dictionary<Vector3Int,Color>();
        private readonly Dictionary<Vector2Int,int> raisedTops=new Dictionary<Vector2Int,int>();
        private static readonly Vector2Int[] skyOffsets={new Vector2Int(2,0),new Vector2Int(-2,0),new Vector2Int(0,2),new Vector2Int(0,-2),new Vector2Int(4,0),new Vector2Int(-4,0),new Vector2Int(0,4),new Vector2Int(0,-4)};
        public VoxelLighting3D(VoxelWorld3D world,Voxel3DGenerator generator){this.world=world;this.generator=generator;}
        public void Changed(Vector3Int cell,ushort id)
        {
            columnTops.Remove(new Vector2Int(cell.x,cell.z)); samples.Clear();sources.Remove(cell);
            var def=VoxelBlockCatalog.Get(id);
            if(def==null)return;
            var key=new Vector2Int(cell.x,cell.z);
            if(!raisedTops.TryGetValue(key,out int top)||cell.y>top)raisedTops[key]=cell.y;
            Color c=new Color(Mathf.Clamp01(def.LightEmissionR/15f),Mathf.Clamp01(def.LightEmissionG/15f),Mathf.Clamp01(def.LightEmissionB/15f));
            var rt=VoxelWorld3DRuntime.Instance;
            if(rt!=null&&rt.Services!=null&&rt.Services.IsBurning(cell))c=new Color(1,.53f,.15f);
            if(c.maxColorComponent>0)sources[cell]=c;
        }
        public void ChunkLoaded(VoxelChunk3D chunk)
        {
            Vector3Int o=chunk.WorldOrigin;
            for(int x=0;x<16;x++)for(int y=0;y<16;y++)for(int z=0;z<16;z++)
            {
                ushort id=chunk.Get(x,y,z);if(id==0)continue;
                var d=VoxelBlockCatalog.Get(id);if(d==null)continue;
                var key=new Vector2Int(o.x+x,o.z+z);if(VoxelBlockCatalog.Opaque(id)&&(!raisedTops.TryGetValue(key,out int top)||o.y+y>top))raisedTops[key]=o.y+y;
                if(d.LightEmissionR+d.LightEmissionG+d.LightEmissionB>0)Changed(o+new Vector3Int(x,y,z),id);
            }
            columnTops.Clear();samples.Clear();
        }
        public void ChunkRemoved(Vector3Int chunk)
        {
            var remove=new List<Vector3Int>();
            foreach(var p in sources)if(VoxelWorld3D.WorldToChunk(p.Key.x,p.Key.y,p.Key.z)==chunk)remove.Add(p.Key);
            foreach(var p in remove)sources.Remove(p);samples.Clear();columnTops.Clear();
        }
        public Color32 Encoded(Vector3Int cell)
        {
            if(samples.TryGetValue(cell,out var color))return color;
            float sun=Sky(cell.x,cell.z)<cell.y?1:0;
            if(sun==0)
                foreach(var delta in skyOffsets)
                {
                    var other=cell+new Vector3Int(delta.x,0,delta.y);
                    if(Sky(other.x,other.z)>=cell.y)continue;
                    if(Clear((Vector3)cell+Vector3.one*.5f,(Vector3)other+Vector3.one*.5f))
                        sun=Mathf.Max(sun,1-Mathf.Sqrt(delta.x*delta.x+delta.y*delta.y)/9f);
                }
            Color emission=Color.black;
            foreach(var source in sources)
            {
                float distance=((Vector3)(source.Key-cell)).magnitude;
                if(distance>14||source.Value.maxColorComponent<=0)continue;
                if(distance>1.5f&&!Clear((Vector3)cell+Vector3.one*.5f,(Vector3)source.Key+Vector3.one*.5f))continue;
                float power=Mathf.Pow(Mathf.Clamp01(1-distance/14),1.5f);
                emission.r=Mathf.Max(emission.r,source.Value.r*power);
                emission.g=Mathf.Max(emission.g,source.Value.g*power);
                emission.b=Mathf.Max(emission.b,source.Value.b*power);
            }
            color=new Color(emission.r,emission.g,emission.b,sun);
            if(samples.Count>32768)samples.Clear();samples[cell]=color;return color;
        }
        public Color Sample(Vector3 position)
        {
            Color c=Encoded(Vector3Int.FloorToInt(position));
            var held=Game.World.Lighting.HandheldLightRuntime.SampleVoxel(VoxelWorld3DRuntime.Instance,position);
            c.r=Mathf.Max(c.r,held.R/15f);c.g=Mathf.Max(c.g,held.G/15f);c.b=Mathf.Max(c.b,held.B/15f);
            float day=Mathf.Clamp01(Shader.GetGlobalFloat("_TelderDayFactor"));
            return new Color(Mathf.Pow(Mathf.Max(.035f,Mathf.Max(c.r,c.a*day)),.8f),Mathf.Pow(Mathf.Max(.035f,Mathf.Max(c.g,c.a*day)),.8f),Mathf.Pow(Mathf.Max(.04f,Mathf.Max(c.b,c.a*day)),.8f),1);
        }
        private int Sky(int x,int z)
        {
            var key=new Vector2Int(x,z);if(columnTops.TryGetValue(key,out int top))return top;
            int surface=generator!=null?generator.GetSurfaceHeight(x,z):0;
            int start=raisedTops.TryGetValue(key,out int raised)?Mathf.Max(surface,raised):surface;
            top=surface-96;
            for(int y=start;y>=surface-96;y--)
            {
                if(!world.TryGetChunk(VoxelWorld3D.WorldToChunk(x,y,z),out _))
                {if(y<=surface){top=y;break;}continue;}
                ushort id=world.GetBlock(x,y,z);
                if(VoxelBlockCatalog.Opaque(id)&&!VoxelGameplayState3D.IsOpen(new Vector3Int(x,y,z))){top=y;break;}
            }
            columnTops[key]=top;return top;
        }
        private bool Clear(Vector3 start,Vector3 end)
        {
            float distance=(end-start).magnitude;int steps=Mathf.Max(1,Mathf.CeilToInt(distance*2));
            for(int i=1;i<steps;i++)
            {
                var p=Vector3Int.FloorToInt(Vector3.Lerp(start,end,i/(float)steps));
                if(VoxelBlockCatalog.Opaque(world.GetBlock(p.x,p.y,p.z))&&!VoxelGameplayState3D.IsOpen(p))return false;
            }
            return true;
        }
    }
}
