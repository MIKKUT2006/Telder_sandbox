using System;
using UnityEngine;
using Game.Content;
using Game.World.Structures;

namespace Game.World.Voxel3D.Structures
{
    public static class VoxelStructureGeneration3D
    {
        public static void ApplyToChunk(Voxel3DGenerator generator,VoxelChunk3D chunk,int seed)
        {
            var defs=StructureRegistry.GetAll();if(defs==null)return;Vector3Int origin=chunk.WorldOrigin;
            foreach(var def in defs){if(def==null||def.GeometryMode!=StructureGeometryMode.Voxel3D||def.Cells==null||def.Cells.Count==0)continue;
                int region=Mathf.Max(16,def.RegionSize);int minRX=FloorDiv(origin.x-Mathf.Max(1,def.Width),region),maxRX=FloorDiv(origin.x+15+Mathf.Max(1,def.Width),region);int minRZ=FloorDiv(origin.z-Mathf.Max(1,def.Depth),region),maxRZ=FloorDiv(origin.z+15+Mathf.Max(1,def.Depth),region);
                for(int rx=minRX;rx<=maxRX;rx++)for(int rz=minRZ;rz<=maxRZ;rz++)StampCandidate(generator,chunk,def,seed,rx,rz);
            }
        }
        private static void StampCandidate(Voxel3DGenerator generator,VoxelChunk3D chunk,StructureDefinition def,int seed,int rx,int rz)
        {
            int h=Hash(seed,def.ID,rx,rz);var random=new System.Random(h);if(random.NextDouble()>Mathf.Clamp01(def.SpawnChance))return;int region=Mathf.Max(16,def.RegionSize);int ax=rx*region+random.Next(region);int az=rz*region+random.Next(region);var biome=generator.GetBiome(ax,az);
            if(def.Biomes!=null&&def.Biomes.Count>0){bool ok=false;foreach(string b in def.Biomes)if(string.Equals(b,biome.Id,StringComparison.OrdinalIgnoreCase)){ok=true;break;}if(!ok)return;}
            int surface=generator.GetSurfaceHeight(ax,az);int ay=surface+1;if(def.SpawnType==StructureSpawnType.Underground){int min=Mathf.Min(def.MinY,surface-6),max=Mathf.Min(def.MaxY,surface-4);if(max<=min)return;ay=random.Next(min,max+1);}else if(def.SpawnType==StructureSpawnType.Any&&random.NextDouble()<.45){ay=Mathf.Max(def.MinY,surface-random.Next(8,30));}
            Vector3Int o=chunk.WorldOrigin;foreach(var cell in def.Cells){if(cell==null||string.IsNullOrWhiteSpace(cell.ForegroundId))continue;int wx=ax+(cell.X-def.OriginX),wy=ay+(cell.Y-def.OriginY),wz=az+(cell.Z-def.OriginZ);int lx=wx-o.x,ly=wy-o.y,lz=wz-o.z;if(!VoxelChunk3D.InBounds(lx,ly,lz))continue;ushort id=Resolve(cell.ForegroundId);if(id!=0)chunk.Set(lx,ly,lz,id);}
        }
        private static ushort Resolve(string id){try{var cid=ContentID.Parse(id);return BlockIDRegistry.Contains(cid)?BlockIDRegistry.GetID(cid):(ushort)0;}catch{return 0;}}
        private static int FloorDiv(int v,int d){int q=v/d,r=v%d;if(r!=0&&r<0)q--;return q;}
        private static int Hash(int seed,string id,int x,int z){unchecked{int h=seed;h=h*397+x;h=h*397+z;if(id!=null)for(int i=0;i<id.Length;i++)h=h*31+id[i];return h;}}
    }
}
