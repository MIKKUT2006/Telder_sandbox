using Game.Combat;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelProjectile3D : MonoBehaviour
    {
        private VoxelWorld3DRuntime rt;
        private WeaponMetadata weapon;
        private Vector3 velocity;
        private float deadline,damage,nextTrail;
        private bool fromPlayer,bomb;
        private string item;
        private SpriteRenderer sprite;
        private Material material;
        public static VoxelProjectile3D Spawn(VoxelWorld3DRuntime runtime,Vector3 origin,Vector3 direction,WeaponMetadata data,string projectile,float power,bool player)
        {
            var go=new GameObject("Voxel projectile");go.transform.SetParent(runtime.transform,false);go.transform.position=origin;
            var shot=go.AddComponent<VoxelProjectile3D>();shot.rt=runtime;shot.weapon=data;shot.item=projectile;shot.fromPlayer=player;
            shot.bomb=data.GetKind()==WeaponKind.Bomb;
            shot.velocity=direction.normalized*(shot.bomb?data.BombThrowSpeed:data.ProjectileSpeed)*Mathf.Max(.25f,power);
            shot.damage=data.Damage*(player?Game.Gameplay.Effects.PlayerFoodEffects.Strength(runtime.Inventory):1)*data.ProjectileDamageMultiplier*Mathf.Max(.25f,power);
            shot.deadline=Time.time+Mathf.Max(.1f,shot.bomb?data.BombFuseTime:data.ProjectileLifetime);
            shot.sprite=go.AddComponent<SpriteRenderer>();shot.sprite.sprite=ItemSpriteResolver.Resolve(projectile);
            var shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");if(shader!=null){shot.material=new Material(shader);shot.sprite.sharedMaterial=shot.material;}
            if(shot.sprite.sprite!=null)go.transform.localScale=Vector3.one*(Mathf.Clamp(data.ProjectileWorldSize*data.ProjectileScale,.15f,1)/Mathf.Max(.01f,shot.sprite.sprite.bounds.size.magnitude));
            return shot;
        }
        private void Update()
        {
            if(rt==null||!rt.Ready){Destroy(gameObject);return;}
            if(Time.time>=deadline){if(bomb)Explode();else Destroy(gameObject);return;}
            float dt=Mathf.Min(.06f,Time.deltaTime);velocity+=Vector3.down*(bomb?weapon.BombGravity:weapon.ProjectileGravity)*dt;
            Vector3 previous=transform.position,delta=velocity*dt,next=previous+delta;
            if(!rt.IsLoaded(Vector3Int.FloorToInt(next))){if(bomb)velocity=Vector3.zero;else Destroy(gameObject);return;}
            if(delta.sqrMagnitude>.000001f&&UnityEngine.Physics.SphereCast(previous,.045f,delta.normalized,out var hit,delta.magnitude,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                transform.position=hit.point+hit.normal*.06f;
                if(bomb)velocity=Vector3.Reflect(velocity,hit.normal)*Mathf.Clamp01(weapon.BombBounce);
                else{if(fromPlayer&&!string.IsNullOrEmpty(item)&&item.IndexOf("arrow",System.StringComparison.OrdinalIgnoreCase)>=0)VoxelItemDrop3D.Spawn(item,1,transform.position);Destroy(gameObject);}return;
            }
            transform.position=next;
            if(!bomb)
            {
                if(fromPlayer)
                {
                    foreach(var actor in VoxelEntityActor3D.Active)
                    {
                        if(actor==null||actor.IsDead||SegmentDistance(previous,next,actor.Center)>actor.Radius+.15f)continue;
                        actor.TakeDamage(damage,velocity.normalized*weapon.Knockback);Destroy(gameObject);return;
                    }
                }
                else
                {
                    Vector3 target=rt.Motor.transform.position+Vector3.up*.9f;
                    if(SegmentDistance(previous,next,target)<.65f){rt.Player.GetComponent<Game.PlayerStats.PlayerStats>()?.TakeDamage(damage);Destroy(gameObject);return;}
                }
            }
            if(Time.time>=nextTrail){nextTrail=Time.time+.035f;rt.Particles.Burst(next,bomb?new Color(.55f,.5f,.45f):Color.white,bomb?2:1,bomb?.08f:.045f,.04f,false,bomb,bomb?.7f:.16f);}
            if(sprite!=null)sprite.color=rt.Lighting.Sample(next);
            if(rt.PlayerCamera!=null){transform.rotation=rt.PlayerCamera.transform.rotation;Vector3 local=rt.PlayerCamera.transform.InverseTransformDirection(velocity);transform.Rotate(0,0,Mathf.Atan2(local.y,local.x)*Mathf.Rad2Deg);}
        }
        private static float SegmentDistance(Vector3 a,Vector3 b,Vector3 point)
        {Vector3 ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,ab)/Mathf.Max(.000001f,ab.sqrMagnitude));return Vector3.Distance(point,a+ab*t);}
        private void Explode()
        {
            Vector3 center=transform.position;float radius=Mathf.Clamp(Mathf.Max(weapon.BombExplosionWidth,weapon.BombExplosionHeight)*.5f,1.2f,6);
            rt.Particles.Burst(center,new Color(.43f,.42f,.4f),52,.16f,3.3f,false,true,2.5f);
            rt.Particles.Burst(center,new Color(1,.7f,.2f),24,.10f,4,true,false,.5f);
            foreach(var actor in VoxelEntityActor3D.Active)
                if(actor!=null&&!actor.IsDead&&Vector3.Distance(center,actor.Center)<radius+actor.Radius)actor.TakeDamage(damage,(actor.Center-center).normalized*weapon.BombKnockback);
            var stats=rt.Player.GetComponent<Game.PlayerStats.PlayerStats>();if(stats!=null&&Vector3.Distance(center,rt.Motor.transform.position+Vector3.up)<radius)stats.TakeDamage(damage);
            if(weapon.BombDestroyBlocks)
            {
                var origin=Vector3Int.FloorToInt(center);int r=Mathf.CeilToInt(radius);int budget=512;
                for(int x=-r;x<=r&&budget>0;x++)for(int y=-r;y<=r&&budget>0;y++)for(int z=-r;z<=r&&budget>0;z++)
                {
                    var p=origin+new Vector3Int(x,y,z);if(!rt.IsLoaded(p)||Vector3.Distance(center,(Vector3)p+Vector3.one*.5f)>radius)continue;
                    var def=VoxelBlockCatalog.Get(rt.World.GetBlock(p.x,p.y,p.z));if(def==null||def.Hardness<0||def.ExplosionResistance>damage)continue;
                    if(VoxelBlockInteraction3D.Break(rt,p,true))budget--;
                }
            }
            Destroy(gameObject);
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
