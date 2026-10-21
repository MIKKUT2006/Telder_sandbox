using UnityEngine;
using Game.World.Biomes;

namespace Game.World.Voxel3D
{
    public sealed class VoxelWeather3D : MonoBehaviour
    {
        private float changeAt,nextParticles;
        private string type="Clear";
        private VoxelWorld3DRuntime rt;
        private BiomeWeatherSettings settings;
        public string CurrentWeather=>type;
        public void ForceNext(){changeAt=0;}
        private void Update()
        {
            if(rt==null)rt=VoxelWorld3DRuntime.Instance;if(rt==null||!rt.Ready)return;
            Vector3 feet=rt.Motor.transform.position;var biome=rt.Generator.GetBiome(Mathf.FloorToInt(feet.x),Mathf.FloorToInt(feet.z));
            settings=biome!=null?BiomeRegistry.Get(biome.SourceBiomeId)?.Weather:null;
            if(Time.time>=changeAt)
            {
                if(type!="Clear")type="Clear";
                else if(settings!=null&&Random.value<settings.Chance)
                {type=Random.value<settings.FogChance?"Fog":settings.Precipitation;if(string.IsNullOrEmpty(type)||type=="None")type="Clear";}
                changeAt=Time.time+(settings==null?120:Random.Range(type=="Clear"?settings.MinClearDuration:settings.MinDuration,type=="Clear"?settings.MaxClearDuration:settings.MaxDuration));
            }
            float ground=rt.Generator.GetSurfaceHeight(Mathf.FloorToInt(feet.x),Mathf.FloorToInt(feet.z));
            bool surface=feet.y>=ground-3;Shader.SetGlobalFloat("_VoxelWeatherFog",type=="Fog"&&surface?(settings!=null?settings.FogIntensity:.65f):0);Shader.SetGlobalFloat("_VoxelFogHeight",ground+4);
            if(!surface||Time.time<nextParticles||(type!="Rain"&&type!="Snow"))return;
            nextParticles=Time.time+.04f;int count=Mathf.RoundToInt(10*(settings!=null?settings.Intensity:1));
            for(int i=0;i<count;i++)
            {
                Vector3 position=feet+new Vector3(Random.Range(-16,16),Random.Range(8,14),Random.Range(-16,16));
                if(rt.Lighting.Encoded(Vector3Int.FloorToInt(position)).a<250)continue;
                bool snow=type=="Snow";Vector3 velocity=snow?new Vector3(.7f,-2.4f,.2f):new Vector3(-1,-18,.2f);
                rt.Particles.Emit(position,velocity,snow?Color.white:new Color(.48f,.66f,.9f),snow?.075f:.045f,snow?5:1.5f,false);
            }
        }
        private void OnDisable(){Shader.SetGlobalFloat("_VoxelWeatherFog",0);}
    }
}
