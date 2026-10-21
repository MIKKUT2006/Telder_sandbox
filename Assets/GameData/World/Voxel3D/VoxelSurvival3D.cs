using UnityEngine;
using Game.Fluids;
using Game.Content;

namespace Game.World.Voxel3D
{
    public sealed class VoxelSurvival3D : MonoBehaviour
    {
        private VoxelWorld3DRuntime runtime;
        private Game.PlayerStats.PlayerStats stats;
        private float nextDust,nextFire,nextScan,nextHazard;
        private string lastBiome;
        public bool InLiquid {get;private set;}
        public bool OnLadder {get;private set;}
        public void Initialize(VoxelWorld3DRuntime runtime){this.runtime=runtime;stats=GetComponent<Game.PlayerStats.PlayerStats>();}
        private void Update()
        {
            if(runtime==null||runtime.Motor==null||stats==null||stats.IsDead)return;
            Vector3 feet=runtime.Motor.transform.position;
            var body=runtime.Fluids.At(feet+Vector3.up*.6f);var head=runtime.Fluids.At(feet+Vector3.up*1.6f);
            InLiquid=body!=null;stats.SetHeadSubmerged(head!=null);
            if(body!=null&&FluidRegistry.TryGet(body.Id,out var fluid)&&fluid.Damage>0&&Time.time>=nextHazard)
            {nextHazard=Time.time+Mathf.Max(.05f,fluid.DamageInterval);stats.TakeDamage(fluid.Damage);}
            var cell=Vector3Int.FloorToInt(feet+Vector3.up*.7f);var def=VoxelBlockCatalog.Get(runtime.World.GetBlock(cell.x,cell.y,cell.z));
            OnLadder=(VoxelBlockCatalog.Tag(def,"ladder")||VoxelBlockCatalog.Tag(def,"vines"))||def!=null&&string.Equals(def.Behavior,"Ladder",System.StringComparison.OrdinalIgnoreCase);
            if(runtime.Motor.IsGrounded&&runtime.Motor.HorizontalSpeed>1.1f&&Time.time>=nextDust)
            {nextDust=Time.time+.09f;runtime.Particles.Burst(feet+Vector3.up*.05f,new Color(.52f,.43f,.32f),3,.06f,.25f,false,false,1.6f);}
            if(Time.time>=nextScan)
            {
                nextScan=Time.time+.75f;var biome=runtime.Generator.GetBiome(cell.x,cell.z);
                if(biome!=null&&biome.Id!=lastBiome){lastBiome=biome.Id;Game.Achievements.AchievementRuntime.NotifyExploreBiome(lastBiome);}
                bool bench=false;
                for(int x=-3;x<=3&&!bench;x++)for(int y=-2;y<=2&&!bench;y++)for(int z=-3;z<=3&&!bench;z++)
                    bench=VoxelBlockCatalog.Tag(VoxelBlockCatalog.Get(runtime.World.GetBlock(cell.x+x,cell.y+y,cell.z+z)),"workbench");
                Game.Crafting.CraftingSession.SetWorkbench(bench);
            }
            if(Time.time>=nextFire)
            {
                nextFire=Time.time+.2f;int count=0;
                for(int x=-5;x<=5&&count<12;x++)for(int y=-3;y<=3&&count<12;y++)for(int z=-5;z<=5&&count<12;z++)
                {
                    var p=cell+new Vector3Int(x,y,z);var d=VoxelBlockCatalog.Get(runtime.World.GetBlock(p.x,p.y,p.z));if(d==null)continue;
                    bool fire=VoxelBlockCatalog.Tag(d,"torch")||VoxelBlockCatalog.Tag(d,"campfire")||runtime.Services.IsBurning(p);
                    if(!fire)continue;count++;
                    runtime.Particles.Burst((Vector3)p+new Vector3(.5f,.85f,.5f),new Color(1,.65f,.12f),1,.05f,.35f,false,false,.6f);
                }
            }
        }
        private void OnDisable(){if(stats!=null)stats.SetHeadSubmerged(false);Game.Crafting.CraftingSession.Clear();}
    }
}
