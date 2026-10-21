using System.Collections.Generic;
using Game.Combat;
using Game.InputSupport;
using Game.Inventory;
using Game.Items.Durability;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [DefaultExecutionOrder(31100)]
    public sealed class VoxelCombat3D : MonoBehaviour
    {
        private VoxelWorld3DRuntime rt;
        private PlayerInventory inventory;
        private Game.PlayerStats.PlayerStats stats;
        private WeaponMetadata weapon,activeWeapon;
        private ItemStack attackingStack;
        private WeaponKind kind,activeKind;
        private float nextAttack,start,duration,chargeStart=-1,lastAttack=-100,nextTrail;
        private bool attacking,thrust,power,dashed;
        private int combo;
        private string heldId;
        private GameObject itemObject;
        private SpriteRenderer itemRenderer;
        private Material itemMaterial;
        private readonly HashSet<VoxelEntityActor3D> hits=new HashSet<VoxelEntityActor3D>();
        public void Initialize(VoxelWorld3DRuntime runtime){rt=runtime;inventory=GetComponent<PlayerInventory>();stats=GetComponent<Game.PlayerStats.PlayerStats>();}
        private void Update()
        {
            if(rt==null||!rt.Ready||inventory==null)return;
            string id=inventory.GetSelectedItemId();WeaponMetadataRegistry.TryGet(id,out weapon);kind=weapon!=null?weapon.GetKind():WeaponKind.None;
            if(id!=heldId){heldId=id;chargeStart=-1;attacking=false;combo=0;UpdateSprite(id);}
            bool blocked=GetComponent<Game.PlayerStats.FoodUseController>()?.IsEating==true||VoxelWorld3DRuntime.UIBlocked||stats!=null&&stats.IsDead||!GameInput.MobileEnabled&&Cursor.lockState!=CursorLockMode.Locked;
            if(itemObject!=null)itemObject.SetActive(!blocked&&itemRenderer.sprite!=null);
            if(blocked){attacking=false;chargeStart=-1;return;}
            if(weapon==null||DurabilitySystem.IsBroken(inventory.GetSelectedStack()))return;
            if(attacking){TickMelee();return;}
            if(Time.time<nextAttack)return;
            if(kind==WeaponKind.Bow)
            {
                if(GameInput.Held(GameAction.Attack)&&chargeStart<0)chargeStart=Time.time;
                if(!GameInput.Held(GameAction.Attack)&&chargeStart>=0){Shoot(Mathf.Lerp(weapon.BowMinPower,1,Mathf.Clamp01((Time.time-chargeStart)/Mathf.Max(.05f,weapon.BowChargeTime))));chargeStart=-1;}
            }
            else if(GameInput.Held(GameAction.Attack))
            {
                if(kind==WeaponKind.Sword||kind==WeaponKind.Spear)BeginMelee();
                else if(kind==WeaponKind.Harpoon)
                {
                    rt.Motor.ToggleGrapple(rt.PlayerCamera.transform.forward,weapon.HarpoonMaxRange,weapon.HarpoonWinchSpeed);
                    SpendDurability(inventory.GetSelectedStack(),weapon.HarpoonDurabilityCost);nextAttack=Time.time+.4f;
                }
                else if(kind==WeaponKind.Bomb||kind==WeaponKind.Gun)Shoot(1);
            }
        }
        private void BeginMelee()
        {
            if(Time.time-lastAttack>weapon.MeleeComboResetTime)combo=0;
            activeWeapon=weapon;activeKind=kind;attackingStack=inventory.GetSelectedStack();
            thrust=kind==WeaponKind.Spear||weapon.GetSwordPatternEntry(combo)==SwordAttackKind.Thrust;
            power=kind==WeaponKind.Spear&&weapon.IsPowerSpearAttack(combo);combo++;
            start=Time.time;lastAttack=start;duration=1/Mathf.Max(.1f,weapon.AttackSpeed);nextAttack=start+duration;
            attacking=true;dashed=false;hits.Clear();
        }
        private void TickMelee()
        {
            float phase=Mathf.Clamp01((Time.time-start)/duration);
            if(phase>=1){attacking=false;SpendDurability(attackingStack,1);return;}
            if(!MeleeAttackMotion.IsStrike(phase))return;
            if(!dashed){dashed=true;if(activeKind==WeaponKind.Sword&&thrust)rt.Motor.TryDash(activeWeapon.SwordDashDistance,activeWeapon.SwordDashDuration);}
            Transform camera=rt.PlayerCamera.transform;Vector3 origin=rt.Motor.transform.position+Vector3.up*1.1f;
            float reach=activeKind==WeaponKind.Spear?activeWeapon.SpearBaseReach+activeWeapon.SpearThrustDistance:Mathf.Max(activeWeapon.Range,activeWeapon.SwingReach+activeWeapon.ThrustDistance*.5f);
            if(power)reach*=activeWeapon.SpearPowerReachMultiplier;
            Vector3 direction=camera.forward;
            if(!thrust)direction=Quaternion.AngleAxis(Mathf.Lerp(-65,65,Mathf.InverseLerp(.22f,.78f,phase)),camera.up)*direction;
            float radius=activeKind==WeaponKind.Spear?activeWeapon.SpearHitRadius:activeWeapon.SwingHitRadius;
            foreach(var actor in VoxelEntityActor3D.Active)
            {
                if(actor==null||actor.IsDead||hits.Contains(actor))continue;
                Vector3 to=actor.Center-origin;float along=Vector3.Dot(to,direction);
                if(along<0||along>reach+actor.Radius||(to-direction*Mathf.Clamp(along,0,reach)).magnitude>radius+actor.Radius)continue;
                if(!VoxelRaycast3D.Clear(rt,origin,actor.Center))continue;
                hits.Add(actor);actor.TakeDamage(activeWeapon.Damage*Game.Gameplay.Effects.PlayerFoodEffects.Strength(this)*(power?activeWeapon.SpearPowerDamageMultiplier:1),direction*activeWeapon.Knockback);
            }
            if(Time.time>=nextTrail){nextTrail=Time.time+.025f;Vector3 tip=origin+direction*reach;rt.Particles.Burst(tip,Color.white,2,.065f,.05f,false,false,.15f);}
        }
        private void Shoot(float powerValue)
        {
            if(!string.IsNullOrEmpty(weapon.AmmoItem)&&!inventory.TryConsumeItem(weapon.AmmoItem,Mathf.Max(1,weapon.AmmoPerShot)))return;
            string projectile=string.IsNullOrEmpty(weapon.ProjectileItem)?heldId:weapon.ProjectileItem;
            var stack=inventory.GetSelectedStack();
            if(kind==WeaponKind.Bomb&&!inventory.TryConsumeSelected(1))return;
            Vector3 origin=rt.Motor.transform.position+Vector3.up*1.4f;
            Vector3 aim=rt.PlayerCamera.transform.position+rt.PlayerCamera.transform.forward*40;
            Vector3 direction=(aim-origin).normalized;
            int count=Mathf.Clamp(weapon.ProjectileCount,1,32);
            for(int i=0;i<count;i++)
            {
                float spread=count==1?Random.Range(-weapon.SpreadDegrees,weapon.SpreadDegrees):Mathf.Lerp(-weapon.SpreadDegrees,weapon.SpreadDegrees,i/(float)(count-1));
                Vector3 shot=Quaternion.AngleAxis(spread,rt.PlayerCamera.transform.up)*direction;
                VoxelProjectile3D.Spawn(rt,origin+shot*.4f,shot,weapon,projectile,powerValue,true);
            }
            if(kind!=WeaponKind.Bomb)SpendDurability(stack,1);nextAttack=Time.time+1/Mathf.Max(.1f,weapon.AttackSpeed);
        }
        private void SpendDurability(ItemStack stack,int count){DurabilitySystem.Damage(stack,count);inventory.NotifyExternalChange();}
        private void UpdateSprite(string id)
        {
            if(itemObject==null)
            {
                itemObject=new GameObject("Voxel held item");itemObject.layer=2;itemObject.transform.SetParent(rt.Motor.transform,false);
                itemRenderer=itemObject.AddComponent<SpriteRenderer>();
                Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");if(shader!=null){itemMaterial=new Material(shader);itemRenderer.sharedMaterial=itemMaterial;}
            }
            itemRenderer.sprite=ItemSpriteResolver.Resolve(id);
        }
        private void LateUpdate()
        {
            if(rt==null||itemObject==null||itemRenderer.sprite==null)return;
            bool third=rt.Motor.ThirdPerson;Transform anchor=third?rt.Avatar.RightHand:rt.PlayerCamera.transform;
            itemObject.transform.SetParent(anchor,false);
            float phase=attacking?Mathf.Clamp01((Time.time-start)/duration):0;
            float swing=attacking&&!thrust?Mathf.Lerp(-55,65,Mathf.InverseLerp(activeWeapon.SwordSwingStartAngle,activeWeapon.SwordSwingEndAngle,MeleeAttackMotion.SwingAngle(activeWeapon,phase))):0;
            float extension=attacking&&thrust?MeleeAttackMotion.Thrust(phase):0;
            itemObject.transform.localPosition=third?new Vector3(0,.15f,.1f+extension*.6f):new Vector3(.36f,-.35f,.65f+extension*.3f);
            itemObject.transform.localRotation=Quaternion.Euler(0,0,-35+swing);
            float size=kind==WeaponKind.Spear?1.5f:.85f;itemObject.transform.localScale=Vector3.one*(size/Mathf.Max(.01f,itemRenderer.sprite.bounds.size.magnitude));
            itemRenderer.color=rt.Lighting.Sample(rt.Motor.transform.position+Vector3.up);
        }
        private void OnDestroy(){if(itemObject!=null)Destroy(itemObject);if(itemMaterial!=null)Destroy(itemMaterial);}
    }
}
