using System;
using UnityEngine;
using Game.World.Generation.Ores;

namespace Game.World.Voxel3D
{
    // Stamp from integer world coordinates, including candidates across chunk
    // borders, so load order never cuts a vein, tree crown or hanging vine.
    public static class VoxelNature3D
    {
        private static int Hash(int seed,int x,int y,int z)
        {unchecked{int h=seed;h=h*397^x;h=h*397^y;h=h*397^z;h^=h>>16;h*=0x45d9f3b;return h^(h>>16);}}
        public static void Apply(Voxel3DGenerator generator,VoxelChunk3D chunk,int seed)
        {
            Vector3Int o=chunk.WorldOrigin;
            foreach(var ore in OreRegistry.GetAll())
            {
                ushort id=VoxelBlockCatalog.Id(ore.ID);if(id==0)continue;
                int salt=seed;unchecked{foreach(char c in ore.ID)salt=salt*31+c;}
                // Veins are bounded inside their source cell with a one-cell halo.
                for(int rx=Mathf.FloorToInt((o.x-8)/8f);rx<=Mathf.FloorToInt((o.x+23)/8f);rx++)
                for(int ry=Mathf.FloorToInt((o.y-8)/8f);ry<=Mathf.FloorToInt((o.y+23)/8f);ry++)
                for(int rz=Mathf.FloorToInt((o.z-8)/8f);rz<=Mathf.FloorToInt((o.z+23)/8f);rz++)
                {
                    var random=new System.Random(Hash(salt,rx,ry,rz));if(random.NextDouble()>Mathf.Clamp01(ore.Rarity))continue;
                    int ax=rx*8+random.Next(8),ay=ry*8+random.Next(8),az=rz*8+random.Next(8);
                    int depth=generator.GetSurfaceHeight(ax,az)-ay;if(depth<ore.MinDepth||depth>ore.MaxDepth)continue;
                    var biome=generator.GetBiome(ax,az);bool allowed=ore.Biomes==null||ore.Biomes.Length==0;
                    if(ore.Biomes!=null)foreach(var b in ore.Biomes)if(biome!=null&&biome.Matches(b))allowed=true;
                    if(!allowed)continue;int count=random.Next(Mathf.Max(1,ore.MinVeinSize),Mathf.Max(ore.MinVeinSize,ore.VeinSize)+1);
                    for(int i=0;i<Mathf.Min(64,count);i++)
                    {
                        int x=ax+random.Next(-3,4)-o.x,y=ay+random.Next(-3,4)-o.y,z=az+random.Next(-3,4)-o.z;
                        if(!VoxelChunk3D.InBounds(x,y,z))continue;
                        ushort existing=chunk.Get(x,y,z);var def=VoxelBlockCatalog.Get(existing);
                        if(def!=null&&(VoxelBlockCatalog.Tag(def,"stone")||def.ID==biome?.StoneBlock))chunk.Set(x,y,z,id);
                    }
                }
            }
            ushort trunk=VoxelBlockCatalog.Id("game:oak_trunk"),leaves=VoxelBlockCatalog.Id("game:leaves"),vines=VoxelBlockCatalog.Id("game:vines");
            for(int rx=Mathf.FloorToInt((o.x-4)/9f);rx<=Mathf.FloorToInt((o.x+19)/9f);rx++)
            for(int rz=Mathf.FloorToInt((o.z-4)/9f);rz<=Mathf.FloorToInt((o.z+19)/9f);rz++)
            {
                var random=new System.Random(Hash(seed,rx,719,rz));if(random.NextDouble()>.3)continue;
                int x=rx*9+random.Next(9),z=rz*9+random.Next(9),surface=generator.GetSurfaceHeight(x,z);
                var biome=generator.GetBiome(x,z);if(biome==null||biome.FluidId!="game:water"||surface<biome.WaterLevel)continue;
                if((biome.SurfaceBlock??"").IndexOf("grass",StringComparison.OrdinalIgnoreCase)<0&&biome.SurfaceBlock!="game:moss"||trunk==0||leaves==0)continue;
                int height=random.Next(4,7);
                for(int dx=-2;dx<=2;dx++)for(int dz=-2;dz<=2;dz++)for(int dy=0;dy<=height+2;dy++)
                {
                    bool wood=dx==0&&dz==0&&dy<height;
                    bool leaf=dy>=height-2&&Mathf.Abs(dx)+Mathf.Abs(dz)+Mathf.Max(0,dy-height)<4;
                    if(!wood&&!leaf)continue;int lx=x+dx-o.x,ly=surface+1+dy-o.y,lz=z+dz-o.z;
                    if(VoxelChunk3D.InBounds(lx,ly,lz)&&chunk.Get(lx,ly,lz)==0)chunk.Set(lx,ly,lz,wood?trunk:leaves);
                }
            }
            if(vines==0)return;
            for(int x=0;x<16;x++)for(int z=0;z<16;z++)
            {
                int wx=o.x+x,wz=o.z+z,surface=generator.GetSurfaceHeight(wx,wz);var biome=generator.GetBiome(wx,wz);
                for(int anchor=o.y+1;anchor<=Mathf.Min(o.y+24,surface-6);anchor++)
                {
                    int h=Hash(seed,wx,anchor,wz);if((h&255)>3)continue;
                    if(generator.IsCave(wx,anchor,wz,biome,surface)||!generator.IsCave(wx,anchor-1,wz,biome,surface))continue;
                    int length=7+(int)((uint)h%3);
                    for(int dy=1;dy<=length;dy++)
                    {
                        int y=anchor-dy;if(!generator.IsCave(wx,y,wz,biome,surface))break;int ly=y-o.y;
                        if(ly>=0&&ly<16&&chunk.Get(x,ly,z)==0)chunk.Set(x,ly,z,vines);
                    }
                }
            }
        }
    }
}
