using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Visual;
using Game.Combat;
using UnityEngine;

namespace Game.World.Voxel3D
{
    // The same definitions, star scaling, equipment and loot drive both dimensions.
    public sealed class VoxelEntityActor3D : MonoBehaviour
    {
        public static readonly List<VoxelEntityActor3D> Active=new List<VoxelEntityActor3D>();
        public EntityDefinition Definition {get;private set;}
        public bool IsDead=>health<=0;
        public float Radius=>Mathf.Max(Definition.ColliderWidth*.5f,Definition.ColliderHeight*.35f);
        public Vector3 Center=>transform.position+Vector3.up*Definition.ColliderHeight*.5f;
        private VoxelWorld3DRuntime rt;
        private CharacterController body;
        private EntityVisualController visual;
        private Transform billboard;
        private SpriteRenderer[] sprites;
        private Color[] tints;private Material spriteMaterial;
        private int stars;
        private float health,maximum,nextAttack,attackAt,attackEnd,aggroUntil,nextWander,yVelocity,attackDamage,attackRange,nextTrail;
        private bool pendingAttack;
        private Vector3 knockback,wander,origin,attackTarget;
        private WeaponMetadata weapon;
        private string heldItem;private float nextHazard;
        public static VoxelEntityActor3D Spawn(VoxelWorld3DRuntime runtime,EntityDefinition definition,Vector3 feet)
        {
            var go=new GameObject(definition.Name+" (3D)");go.transform.SetParent(runtime.transform,false);go.transform.position=feet;
            var actor=go.AddComponent<VoxelEntityActor3D>();actor.Initialize(runtime,definition);return actor;
        }
        private void Initialize(VoxelWorld3DRuntime runtime,EntityDefinition definition)
        {
            rt=runtime;Definition=definition;stars=definition.Stars.RollStars();maximum=definition.MaxHealth*definition.Stars.HealthMultiplier(stars);health=maximum;origin=transform.position;
            body=gameObject.AddComponent<CharacterController>();body.height=Mathf.Max(.5f,definition.ColliderHeight);body.radius=Mathf.Clamp(definition.ColliderWidth*.5f,.15f,body.height*.49f);body.center=Vector3.up*body.height*.5f;body.stepOffset=Mathf.Min(.5f,body.height*.4f);body.skinWidth=.03f;
            // Actors are triggers for projectile traces; their own motor still collides with terrain.
            gameObject.layer=2;
            var holder=new GameObject("Billboard rig");holder.transform.SetParent(transform,false);billboard=holder.transform;billboard.localPosition=Vector3.up*body.height*.5f;
            visual=new EntityVisualController(billboard,definition,stars);
            if(definition.Equipment!=null)foreach(var entry in definition.Equipment)if(entry!=null&&Random.value<=entry.Chance){heldItem=entry.ItemId;break;}
            visual.SetHeldItem(heldItem);WeaponMetadataRegistry.TryGet(heldItem,out weapon);
            foreach(var collider in holder.GetComponentsInChildren<Collider2D>(true))collider.enabled=false;
            foreach(var rigid in holder.GetComponentsInChildren<Rigidbody2D>(true))rigid.simulated=false;
            var shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");if(shader!=null)spriteMaterial=new Material(shader);
            sprites=holder.GetComponentsInChildren<SpriteRenderer>(true);foreach(var sprite in sprites)if(spriteMaterial!=null)sprite.sharedMaterial=spriteMaterial;
            tints=new Color[sprites.Length];for(int i=0;i<sprites.Length;i++)tints[i]=sprites[i].color;
            Active.Add(this);
        }
        public void TakeDamage(float amount,Vector3 impulse)
        {
            if(IsDead||Definition.Invulnerable||amount<=0)return;health=Mathf.Max(0,health-amount);knockback+=impulse;aggroUntil=Time.time+Definition.Combat.AggroAfterHitSeconds;
            rt.Particles.Burst(Center,new Color(.8f,.95f,1),8,.065f,2,true,false,.3f);
            if(IsDead)
            {
                visual.SetState(EntityVisualState.Death,true);body.enabled=false;
                if(Definition.Loot!=null)foreach(var drop in Definition.Loot)
                    if(drop!=null&&Random.value<=drop.Chance){int count=Mathf.RoundToInt(Random.Range(drop.Min,drop.Max+1)*Definition.Stars.LootMultiplier(stars));if(count>0)VoxelItemDrop3D.Spawn(drop.ItemId,count,Center);}
                Game.Achievements.AchievementRuntime.NotifyMobKill(Definition.ID);Destroy(gameObject,Mathf.Max(.15f,Definition.Visual.DeathDespawnDelay));
            }
            else visual.SetState(EntityVisualState.Hurt,true);
        }
        private void Update()
        {
            if(rt==null||!rt.Ready||Definition==null)return;
            if(IsDead){visual.Tick();return;}
            if(Time.time>=nextHazard)
            {
                var liquid=rt.Fluids.At(Center);
                if(liquid!=null&&Game.Fluids.FluidRegistry.TryGet(liquid.Id,out var hazard)&&hazard.Damage>0)
                {nextHazard=Time.time+Mathf.Max(.1f,hazard.DamageInterval);TakeDamage(hazard.Damage,Vector3.zero);if(IsDead)return;}
            }
            Vector3 player=rt.Motor.transform.position,to=player-transform.position;
            if(to.sqrMagnitude>10000){Destroy(gameObject);return;}
            var stats=rt.Player.GetComponent<Game.PlayerStats.PlayerStats>();bool playerAlive=stats!=null&&!stats.IsDead;
            bool angry=playerAlive&&(Definition.GetDisposition()==EntityDisposition.Aggressive||Time.time<aggroUntil)&&to.magnitude<Definition.Combat.LoseTargetRange;
            Vector3 horizontal=new Vector3(to.x,0,to.z);float range=weapon!=null?Mathf.Max(Definition.Combat.AttackRange,weapon.Range):Definition.Combat.AttackRange;
            bool ranged=weapon!=null&&(weapon.GetKind()==WeaponKind.Bow||weapon.GetKind()==WeaponKind.Gun||weapon.GetKind()==WeaponKind.Bomb);
            if(ranged)range=Mathf.Min(20,Definition.Combat.VisionRange);
            if(pendingAttack&&Time.time>=attackAt){pendingAttack=false;ResolveAttack(stats);}
            bool attacking=Time.time<attackEnd;
            if(angry&&!attacking&&Time.time>=nextAttack&&horizontal.magnitude<range&&Mathf.Abs(to.y)<(Definition.IsBoss?8:4)&&
                (!Definition.Combat.RequireLineOfSight||VoxelRaycast3D.Clear(rt,Center,player+Vector3.up)))
            {
                float windup=Definition.IsBoss?Mathf.Max(.65f,Definition.Boss.SlamWindup):.28f;
                attackAt=Time.time+windup;attackEnd=attackAt+.3f;nextAttack=attackEnd+Definition.Combat.AttackCooldown;
                attackTarget=player;attackDamage=(weapon!=null?weapon.Damage:Definition.Combat.BaseDamage)*Definition.Stars.DamageMultiplier(stars);
                attackRange=Definition.IsBoss?Mathf.Max(4,range):range;pendingAttack=true;visual.SetState(EntityVisualState.Attack,true);
            }
            Vector3 move=Vector3.zero;
            if(!attacking)
            {
                if(angry&&horizontal.magnitude>range*.75f)move=horizontal.normalized;
                else if(!angry)
                {
                    if(Time.time>=nextWander){nextWander=Time.time+Random.Range(2,5);Vector2 r=Random.insideUnitCircle;wander=new Vector3(r.x,0,r.y);if(Vector3.Distance(transform.position,origin)>Definition.WanderRadius)wander=(origin-transform.position).normalized;}
                    move=wander*.35f;
                }
            }
            var motion=Definition.Movement;bool flying=motion.GetKind()==EntityMovementKind.Flying;
            if(flying){yVelocity=angry?Mathf.Clamp(to.y,-1,1)*motion.Speed:Mathf.Sin(Time.time)*.4f;}
            else if(body.isGrounded)
            {
                yVelocity=-2;
                if(move.sqrMagnitude>.01f&&(motion.GetKind()==EntityMovementKind.Jumping||UnityEngine.Physics.Raycast(Center,move,.7f,~(1<<2),QueryTriggerInteraction.Ignore)))yVelocity=motion.JumpForce;
            }
            else yVelocity=Mathf.Max(-motion.MaxFallSpeed,yVelocity-motion.Gravity*Time.deltaTime);
            Vector3 velocity=move*motion.Speed+Vector3.up*yVelocity+knockback;knockback=Vector3.MoveTowards(knockback,Vector3.zero,18*Time.deltaTime);
            Vector3 next=transform.position+velocity*Mathf.Min(.05f,Time.deltaTime);
            if(rt.IsLoaded(Vector3Int.FloorToInt(next))&&rt.IsLoaded(Vector3Int.FloorToInt(next+Vector3.up*body.height)))body.Move(velocity*Mathf.Min(.05f,Time.deltaTime));
            if(!attacking)visual.SetState(move.sqrMagnitude>.03f?EntityVisualState.Walk:EntityVisualState.Idle);
            Vector3 screen=rt.PlayerCamera.transform.InverseTransformDirection(horizontal);visual.SetFacing(screen.x>=0?1:-1);
            visual.AimHeldItem(new Vector2(screen.x,to.y),attacking?Mathf.Clamp01(1-(attackEnd-Time.time)/.8f):-1,weapon);visual.Tick();
            if(attacking&&weapon!=null&&(weapon.GetKind()==WeaponKind.Sword||weapon.GetKind()==WeaponKind.Spear)&&Time.time>=nextTrail)
            {nextTrail=Time.time+.045f;rt.Particles.Burst(Center+horizontal.normalized*Mathf.Min(1.5f,range),Color.white,2,.07f,.1f,false,false,.15f);}
        }
        private void ResolveAttack(Game.PlayerStats.PlayerStats stats)
        {
            if(stats==null||stats.IsDead)return;
            if(Definition.IsBoss)
            {
                rt.gameObject.AddComponent<VoxelBossShockwave3D>().Initialize(rt,transform.position,Definition.Boss.ShockwaveDamage*Definition.Stars.DamageMultiplier(stars),Definition.Boss.ShockwaveRange,Definition.Boss.ShockwaveSpeed,health/maximum<Definition.Boss.PhaseTwoHealth);
                rt.Particles.Burst(transform.position+Vector3.up*.1f,new Color(.7f,.55f,.9f),32,.12f,3,true,false,1);
                return;
            }
            if(weapon!=null&&(weapon.GetKind()==WeaponKind.Bow||weapon.GetKind()==WeaponKind.Gun||weapon.GetKind()==WeaponKind.Bomb))
            {Vector3 aim=rt.Motor.transform.position+Vector3.up;VoxelProjectile3D.Spawn(rt,Center+(aim-Center).normalized*.5f,(aim-Center).normalized,weapon,string.IsNullOrEmpty(weapon.ProjectileItem)?heldItem:weapon.ProjectileItem,1,false);return;}
            if(Vector3.Distance(Center,rt.Motor.transform.position+Vector3.up)<attackRange+.4f&&VoxelRaycast3D.Clear(rt,Center,rt.Motor.transform.position+Vector3.up))stats.TakeDamage(attackDamage);
        }
        private void LateUpdate()
        {
            if(rt==null||billboard==null||rt.PlayerCamera==null)return;billboard.rotation=rt.PlayerCamera.transform.rotation;
            Color light=rt.Lighting.Sample(Center);for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null)sprites[i].color=tints[i]*light;
        }
        private void OnDestroy(){Active.Remove(this);if(spriteMaterial!=null)Destroy(spriteMaterial);}
    }
    public sealed class VoxelBossShockwave3D : MonoBehaviour
    {
        private VoxelWorld3DRuntime rt;private Vector3 center;private float damage,range,speed,radius,previous,nextParticles;private bool hit,second;
        public void Initialize(VoxelWorld3DRuntime world,Vector3 origin,float damage,float range,float speed,bool phaseTwo)
        {rt=world;center=origin;this.damage=damage;this.range=range;this.speed=Mathf.Max(1,speed);second=phaseTwo;}
        private void Update()
        {
            if(rt==null){Destroy(this);return;}previous=radius;radius+=speed*Time.deltaTime;
            Vector3 p=rt.Motor.transform.position;float distance=new Vector2(p.x-center.x,p.z-center.z).magnitude;
            if(!hit&&distance>=previous-.6f&&distance<=radius+.6f&&rt.Motor.IsGrounded){rt.Player.GetComponent<Game.PlayerStats.PlayerStats>()?.TakeDamage(damage);hit=true;}
            if(Time.time>=nextParticles)
            {
                nextParticles=Time.time+.05f;int count=Mathf.Clamp(Mathf.CeilToInt(radius*4),12,64);
                for(int i=0;i<count;i++)
                {float angle=i*Mathf.PI*2/count;Vector3 edge=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                    if(UnityEngine.Physics.Raycast(edge+Vector3.up*3,Vector3.down,out var ground,7,~(1<<2),QueryTriggerInteraction.Ignore))rt.Particles.Burst(ground.point+Vector3.up*.1f,new Color(.75f,.65f,.95f),1,.14f,1.5f,true,false,.4f);}
            }
            if(radius>range){if(second){second=false;radius=-speed*.55f;hit=false;}else Destroy(this);}
        }
    }
}
