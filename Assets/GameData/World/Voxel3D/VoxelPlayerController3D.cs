using Game.InputSupport;
using UnityEngine;

namespace Game.World.Voxel3D
{
    // One collision motor for both views; F5 changes the camera, never the body.
    [DefaultExecutionOrder(32000)]
    public sealed class VoxelPlayerController3D : MonoBehaviour
    {
        public float walkSpeed=4.7f,runSpeed=7.4f,jumpSpeed=7.2f,gravity=22f;
        public float mouseSensitivity=2,eyeHeight=1.62f,thirdPersonDistance=4.2f,thirdPersonHeight=.18f;
        private CharacterController cc;
        private Camera cam;
        private Transform source;
        private Game.PlayerStats.PlayerStats stats;
        private VoxelSurvival3D survival;
        private float yaw,pitch,verticalVelocity,fallApex,dashTime,dashSpeed;
        private bool thirdPerson,falling;
        private Vector3 dashDirection,grapplePoint;private bool grappling;private float grappleSpeed;private LineRenderer rope;
        public bool IsGrounded=>cc!=null&&cc.isGrounded;
        public bool ThirdPerson=>thirdPerson;
        public float HorizontalSpeed {get;private set;}
        public bool Initialize(Camera camera,Transform visualRoot)
        {
            Game.InputSupport.Mobile.MobileGameplayControls.EnsureInstalled();
            cam=camera;source=visualRoot;cc=GetComponent<CharacterController>()??gameObject.AddComponent<CharacterController>();
            if(cam==null||cc==null)return false;
            stats=source!=null?source.GetComponent<Game.PlayerStats.PlayerStats>():null;
            cc.height=1.8f;cc.radius=.30f;cc.center=Vector3.up*.9f;cc.stepOffset=.5f;cc.slopeLimit=58;cc.skinWidth=.04f;cc.minMoveDistance=0;
            yaw=transform.eulerAngles.y;cam.orthographic=false;cam.fieldOfView=70;cam.nearClipPlane=.04f;
            // Scene 2D followers must not take ownership of the perspective camera.
            foreach(var component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string name=component.GetType().Name;
                if(name.IndexOf("CameraFollow",System.StringComparison.OrdinalIgnoreCase)>=0||name=="FollowPlayer"||name=="CameraController2D"||name.IndexOf("PixelPerfectCamera",System.StringComparison.OrdinalIgnoreCase)>=0)component.enabled=false;
            }
            LockMouse(!GameInput.MobileEnabled);ApplyViewMode();return true;
        }
        public void Teleport(Vector3 feet)
        {
            if(cc!=null)cc.enabled=false;transform.position=feet;
            verticalVelocity=0;falling=false;dashTime=0;HorizontalSpeed=0;
            if(cc!=null)cc.enabled=true;source?.GetComponent<Game.PlayerStats.PlayerMovementStats>()?.ResetDistanceTracking();UpdateCamera();
        }
        public void ToggleGrapple(Vector3 direction,float range,float winchSpeed)
        {
            if(grappling){grappling=false;if(rope!=null)rope.enabled=false;return;}
            if(cam!=null&&UnityEngine.Physics.Raycast(cam.transform.position,direction,out var hit,Mathf.Max(2,range),~(1<<2),QueryTriggerInteraction.Ignore))
            {
                grapplePoint=hit.point;grappleSpeed=Mathf.Max(5,winchSpeed*2);grappling=true;
                if(rope==null){rope=gameObject.AddComponent<LineRenderer>();rope.positionCount=2;rope.startWidth=rope.endWidth=.035f;var sh=UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelEffect");if(sh!=null)rope.sharedMaterial=new Material(sh);rope.startColor=rope.endColor=new Color(.65f,.58f,.4f);}
                rope.enabled=true;
            }
        }
        public void TryDash(float distance=2,float duration=.12f)
        {if(VoxelWorld3DRuntime.UIBlocked||cc==null||stats!=null&&stats.IsDead)return;dashTime=Mathf.Max(.04f,duration);dashSpeed=Mathf.Max(0,distance)/dashTime;dashDirection=transform.forward;}
        private void Update()
        {
            var rt=VoxelWorld3DRuntime.Instance;
            if(cc==null||cam==null||rt==null||!rt.Ready)return;
            if(VoxelWorld3DRuntime.UIBlocked||stats!=null&&stats.IsDead)
            {HorizontalSpeed=0;dashTime=0;LockMouse(false);return;}
            if(!GameInput.MobileEnabled&&Cursor.lockState!=CursorLockMode.Locked)
            {if(GameInput.Pressed(GameAction.Attack)||GameInput.GamepadConnected)LockMouse(true);else return;}
            if(GameInput.Pressed(GameAction.Toggle3DView)){thirdPerson=!thirdPerson;ApplyViewMode();}
            if(GameInput.MobileEnabled){yaw+=GameInput.Look.x*140*Time.deltaTime;pitch-=GameInput.Look.y*110*Time.deltaTime;}
            else{yaw+=(Input.GetAxisRaw("Mouse X")+GameInput.Look.x)*mouseSensitivity;pitch-=(Input.GetAxisRaw("Mouse Y")+GameInput.Look.y)*mouseSensitivity;}
            pitch=Mathf.Clamp(pitch,-89,89);transform.rotation=Quaternion.Euler(0,yaw,0);
            if(survival==null&&source!=null)survival=source.GetComponent<VoxelSurvival3D>();
            float effectSpeed=Game.Gameplay.Effects.PlayerFoodEffects.Speed(stats),effectJump=Game.Gameplay.Effects.PlayerFoodEffects.Jump(stats);
            bool swimming=survival!=null&&survival.InLiquid,ladder=survival!=null&&survival.OnLadder;
            Vector3 wish=Vector3.ClampMagnitude(transform.right*GameInput.Horizontal+transform.forward*GameInput.Vertical,1);
            float speed=(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))?runSpeed:walkSpeed;
            speed*=effectSpeed;
            if(swimming){speed*=.55f;verticalVelocity=Mathf.MoveTowards(verticalVelocity,GameInput.Held(GameAction.Jump)?3.5f:-1.3f,12*Time.deltaTime);falling=false;}
            else if(ladder){verticalVelocity=GameInput.Vertical*3.8f;falling=false;if(GameInput.Pressed(GameAction.Jump))verticalVelocity=jumpSpeed*effectJump;}
            else if(cc.isGrounded)
            {
                if(falling&&stats!=null&&fallApex-transform.position.y>6)stats.TakeDamage(5+Mathf.Floor(fallApex-transform.position.y-6)*5);
                falling=false;verticalVelocity=-2;if(GameInput.Pressed(GameAction.Jump))verticalVelocity=jumpSpeed*effectJump;
            }
            else{if(!falling){falling=true;fallApex=transform.position.y;}fallApex=Mathf.Max(fallApex,transform.position.y);verticalVelocity=Mathf.Max(-50,verticalVelocity-gravity*Time.deltaTime);}
            Vector3 velocity=wish*speed+Vector3.up*verticalVelocity;
            if(grappling)
            {
                Vector3 pull=grapplePoint-(transform.position+Vector3.up);falling=false;verticalVelocity=0;
                if(pull.magnitude<1.3f||GameInput.Pressed(GameAction.Jump)){grappling=false;if(rope!=null)rope.enabled=false;verticalVelocity=jumpSpeed*effectJump;}
                else velocity+=pull.normalized*grappleSpeed;
            }
            if(dashTime>0){velocity.x=dashDirection.x*dashSpeed;velocity.z=dashDirection.z*dashSpeed;dashTime-=Time.deltaTime;}
            Vector3 start=transform.position;float dt=Mathf.Min(Time.deltaTime,.1f);
            // Large frames are subdivided and unloaded chunks behave as a boundary.
            int steps=Mathf.Max(1,Mathf.CeilToInt(velocity.magnitude*dt/.2f));Vector3 step=velocity*dt/steps;
            for(int i=0;i<steps;i++)
            {
                Vector3 next=transform.position+step;
                if(!rt.IsLoaded(Vector3Int.FloorToInt(next))||!rt.IsLoaded(Vector3Int.FloorToInt(next+Vector3.up*1.75f)))break;
                cc.Move(step);
            }
            Vector3 moved=transform.position-start;moved.y=0;HorizontalSpeed=moved.magnitude/Mathf.Max(.001f,Time.deltaTime);
            if(transform.position.y < -256&&stats!=null)stats.TakeDamage(stats.MaxHealth);
        }
        private void LateUpdate(){UpdateCamera();if(grappling&&rope!=null){rope.SetPosition(0,transform.position+Vector3.up);rope.SetPosition(1,grapplePoint);}}
        private void UpdateCamera()
        {
            if(cam==null)return;Quaternion rotation=Quaternion.Euler(pitch,yaw,0);
            Vector3 eye=transform.position+Vector3.up*eyeHeight,desired=eye;
            if(thirdPerson)
            {
                Vector3 delta=-(rotation*Vector3.forward)*thirdPersonDistance+Vector3.up*thirdPersonHeight;
                if(UnityEngine.Physics.SphereCast(eye,.16f,delta.normalized,out var hit,delta.magnitude,~(1<<2),QueryTriggerInteraction.Ignore))desired=eye+delta.normalized*Mathf.Max(.05f,hit.distance-.08f);
                else desired=eye+delta;
            }
            cam.transform.SetPositionAndRotation(desired,rotation);VoxelWorld3DRuntime.Instance?.PositionSky(desired);
        }
        private void ApplyViewMode()
        {
            if(source!=null)foreach(var renderer in source.GetComponentsInChildren<SpriteRenderer>(true))renderer.enabled=false;
            var rt=VoxelWorld3DRuntime.Instance;if(rt!=null&&rt.Avatar!=null)rt.Avatar.gameObject.SetActive(thirdPerson);UpdateCamera();
        }
        private static void LockMouse(bool locked){Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
        private void OnDisable(){grappling=false;if(rope!=null)rope.enabled=false;LockMouse(false);}
        private void OnDestroy(){if(rope!=null&&rope.sharedMaterial!=null)Destroy(rope.sharedMaterial);}
    }
}
