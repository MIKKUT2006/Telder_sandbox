using Game.World;
using Game.World.Dimensions;
using UnityEngine;
using GameWorld = Game.World.World;

namespace Game.Visuals.PixelEffects
{
    // A single persistent coordinator rebinds when Game is reloaded during travel.
    [DefaultExecutionOrder(62000)]
    public sealed class PixelWorldEffects : MonoBehaviour
    {
        public bool FireParticles = true;
        public bool CelestialRays = true;
        [Range(32,1500)] public int MaxFireParticles = 600;
        [Range(8,32)] public int FirePixelsPerBlock = 16;
        [Range(2,8)] public int RayPixelsPerBlock = 4;
        [Range(0f,.5f)] public float SunRayOpacity = .22f;
        [Range(0f,.5f)] public float MoonRayOpacity = .15f;
        public int RaySortingOrder = -1;

        private static PixelWorldEffects instance;
        private GameWorld world;
        private Camera targetCamera;
        private PixelWorldOcclusion occlusion;
        private PixelFireParticles fire;
        private PixelCelestialRays rays;
        private readonly PixelVegetationMotion vegetation = new PixelVegetationMotion();
        private float nextCameraCheck;
        private bool firesWereEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance=null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance==null) new GameObject("Pixel World Effects").AddComponent<PixelWorldEffects>();
        }

        private void Awake()
        {
            if (instance!=null && instance!=this) { Destroy(gameObject); return; }
            instance=this;
            DontDestroyOnLoad(gameObject);
            vegetation.Clear();
            fire=new PixelFireParticles(transform);
            rays=new PixelCelestialRays(transform);
        }

        private void LateUpdate()
        {
            if (instance != this || fire == null || rays == null) return;
            WorldManager manager=WorldManager.Instance;
            GameWorld current=manager!=null && manager.isActiveAndEnabled ? manager.GetWorld() : null;
            if (current!=world)
            {
                world=current;
                vegetation.Clear();
                occlusion=world!=null ? new PixelWorldOcclusion(world) : null;
                fire.Bind(world,occlusion); rays.Bind(world,occlusion);
                targetCamera=null; nextCameraCheck=0f; firesWereEnabled=false;
            }
            if (Time.unscaledTime>=nextCameraCheck)
            { nextCameraCheck=Time.unscaledTime+.4f; targetCamera=Camera.main; }
            bool active=world!=null && targetCamera!=null && targetCamera.orthographic &&
                targetCamera.isActiveAndEnabled && !DimensionTravelRuntime.IsTraveling &&
                !DimensionTravelRuntime.Current.Is3D;
            if (!active)
            {
                if (firesWereEnabled) fire.Clear();
                firesWereEnabled=false; rays.Hide();
                return;
            }
            vegetation.Tick(targetCamera);
            if (FireParticles)
            {
                fire.Tick(targetCamera,MaxFireParticles,FirePixelsPerBlock);
                firesWereEnabled=true;
            }
            else if (firesWereEnabled) { fire.Clear(); firesWereEnabled=false; }
            if (CelestialRays) rays.Tick(targetCamera,RayPixelsPerBlock,SunRayOpacity,MoonRayOpacity,RaySortingOrder);
            else rays.Hide();
        }

        private void OnDestroy()
        {
            if (instance!=this) return;
            instance=null;
            vegetation.Clear();
            if (fire!=null) fire.Dispose();
            if (rays!=null) rays.Dispose();
        }
    }
}

