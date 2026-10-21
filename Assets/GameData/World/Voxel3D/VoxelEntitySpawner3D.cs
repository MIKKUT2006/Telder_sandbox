using System.Collections.Generic;
using Game.Entities;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelEntitySpawner3D : MonoBehaviour
    {
        private float next;
        private void Update()
        {
            var rt=VoxelWorld3DRuntime.Instance;if(rt==null||!rt.Ready||Time.time<next||VoxelEntityActor3D.Active.Count>=24)return;next=Time.time+1.8f;
            var choices=new List<EntityDefinition>();float total=0;
            foreach(var def in EntityRegistry.All)
            {
                if(def==null||!def.Enabled||!def.Spawn.Enabled)continue;
                int count=0;foreach(var actor in VoxelEntityActor3D.Active)if(actor!=null&&actor.Definition.ID==def.ID)count++;
                if(count>=def.Spawn.MaxAlive||def.IsBoss&&count>=1)continue;choices.Add(def);total+=def.Spawn.Weight;
            }
            if(total<=0)return;float roll=Random.value*total;EntityDefinition selected=choices[choices.Count-1];
            foreach(var def in choices){roll-=def.Spawn.Weight;if(roll<=0){selected=def;break;}}
            Vector3 player=rt.Motor.transform.position;var spawn=selected.Spawn;
            for(int attempt=0;attempt<10;attempt++)
            {
                float angle=Random.value*Mathf.PI*2,distance=Random.Range(spawn.MinDistanceFromPlayer,Mathf.Min(40,spawn.MaxDistanceFromPlayer));
                int x=Mathf.FloorToInt(player.x+Mathf.Cos(angle)*distance),z=Mathf.FloorToInt(player.z+Mathf.Sin(angle)*distance);
                var biome=rt.Generator.GetBiome(x,z);bool allowed=spawn.Biomes==null||spawn.Biomes.Length==0;
                if(spawn.Biomes!=null)foreach(var name in spawn.Biomes)if(biome!=null&&biome.Matches(name))allowed=true;
                if(!allowed)continue;
                for(int y=Mathf.FloorToInt(player.y)+5;y>=Mathf.FloorToInt(player.y)-10;y--)
                {
                    var cell=new Vector3Int(x,y,z);if(!rt.IsLoaded(cell)||!rt.IsLoaded(cell+Vector3Int.up*spawn.RequiredHeight))continue;
                    if(rt.World.GetBlock(x,y,z)!=0||!VoxelBlockCatalog.Opaque(rt.World.GetBlock(x,y-1,z)))continue;
                    bool clear=true;for(int dx=0;dx<spawn.RequiredWidth;dx++)for(int dy=0;dy<spawn.RequiredHeight;dy++)if(rt.World.GetBlock(x+dx,y+dy,z)!=0)clear=false;
                    if(!clear)continue;Vector3 feet=(Vector3)cell+new Vector3(.5f,.05f,.5f);Color lighting=rt.Lighting.Sample(feet+Vector3.up);
                    float light=Mathf.Max(lighting.r,Mathf.Max(lighting.g,lighting.b));if(light<spawn.MinLight||light>spawn.MaxLight)continue;
                    VoxelEntityActor3D.Spawn(rt,selected,feet);return;
                }
            }
        }
    }
}
