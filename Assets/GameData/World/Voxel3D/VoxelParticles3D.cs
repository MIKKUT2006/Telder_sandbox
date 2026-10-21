using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelParticles3D : MonoBehaviour
    {
        private struct Tint{public Color Color;public bool Gravity,Smoke;public float Size;}
        private readonly Dictionary<uint,Tint> tints=new Dictionary<uint,Tint>();
        private readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[1024];
        private readonly HashSet<uint> alive=new HashSet<uint>();
        private readonly List<uint> remove=new List<uint>();
        private ParticleSystem system;
        private Material material;
        private uint seed=1;
        private float clock;
        private void Awake()
        {
            system=gameObject.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=true;main.playOnAwake=false;main.maxParticles=1024;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=1;main.startSize=.1f;
            var emission=system.emission;emission.enabled=false;var shape=system.shape;shape.enabled=false;
            var fade=system.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.5f),new GradientAlphaKey(0,1)});
            fade.color=new ParticleSystem.MinMaxGradient(gradient);
            Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelEffect")??Shader.Find("Sprites/Default");
            material=new Material(shader);var renderer=GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            system.Play();
        }
        public void Burst(Vector3 p,Color color,int count=12,float size=.09f,float speed=3,bool gravity=true,bool smoke=false,float lifetime=.8f)
        {
            for(int i=0;i<count;i++)
            {
                Emit(p+Random.insideUnitSphere*.15f,Random.insideUnitSphere*speed+Vector3.up*(smoke?speed*.7f:speed*.5f),color,size,lifetime,gravity,smoke);
            }
        }
        public void Emit(Vector3 position,Vector3 velocity,Color color,float size,float lifetime,bool gravity,bool smoke=false)
        {
            if(system==null)return;uint id=seed++;if(seed==0)seed=1;tints[id]=new Tint{Color=color,Gravity=gravity,Smoke=smoke,Size=size};
            var emit=new ParticleSystem.EmitParams();emit.position=position;emit.velocity=velocity;emit.startSize=size;emit.startLifetime=lifetime;emit.startColor=color;emit.randomSeed=id;system.Emit(emit,1);
        }
        private void LateUpdate()
        {
            clock+=Time.deltaTime;if(clock<.04f)return;float dt=clock;clock=0;
            var runtime=VoxelWorld3DRuntime.Instance;if(runtime==null||system==null)return;
            int count=system.GetParticles(buffer);alive.Clear();
            for(int i=0;i<count;i++)
            {
                var p=buffer[i];alive.Add(p.randomSeed);
                if(tints.TryGetValue(p.randomSeed,out var tint))
                {
                    var cell=Vector3Int.FloorToInt(p.position);if(VoxelBlockCatalog.Opaque(runtime.World.GetBlock(cell.x,cell.y,cell.z)))p.remainingLifetime=0;
                    Color light=runtime.Lighting.Sample(p.position);
                    p.startColor=new Color(tint.Color.r*light.r,tint.Color.g*light.g,tint.Color.b*light.b,tint.Color.a);
                    if(tint.Gravity)p.velocity+=Vector3.down*9*dt;
                    if(tint.Smoke)p.startSize=tint.Size*Mathf.Lerp(1,2.6f,1-p.remainingLifetime/Mathf.Max(.01f,p.startLifetime));
                }
                buffer[i]=p;
            }
            system.SetParticles(buffer,count);remove.Clear();foreach(var p in tints)if(!alive.Contains(p.Key))remove.Add(p.Key);foreach(uint id in remove)tints.Remove(id);
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
