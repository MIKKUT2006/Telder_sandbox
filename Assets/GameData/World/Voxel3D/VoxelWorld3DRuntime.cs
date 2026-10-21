using System.Collections.Generic;
using UnityEngine;
using Game.Save;

namespace Game.World.Voxel3D
{
    public sealed class VoxelWorld3DRuntime : MonoBehaviour
    {
        public static VoxelWorld3DRuntime Instance {get;private set;}
        [SerializeField] private int horizontalChunkRadius=4,verticalChunkRadius=3,chunksPerFrame=2;
        public VoxelWorld3D World {get;private set;}
        public Voxel3DGenerator Generator {get;private set;}
        public VoxelLighting3D Lighting {get;private set;}
        public VoxelGameplayState3D Services {get;private set;}
        public VoxelCellSimulation3D Simulation {get;private set;}
        public VoxelFluids3D Fluids {get;private set;}
        public VoxelPlayerController3D Motor {get;private set;}
        public VoxelWeather3D Weather {get;private set;}
        public VoxelParticles3D Particles {get;private set;}
        public Game.Inventory.PlayerInventory Inventory {get;private set;}
        public Transform Player {get;private set;}
        public Camera PlayerCamera {get;private set;}
        public VoxelAvatar3D Avatar {get;private set;}
        public bool Ready {get;private set;}
        private VoxelChunkRenderer3D renderer;
        private VoxelChunkLoader3D loader;
        private readonly Queue<Vector3Int> dirty=new Queue<Vector3Int>();
        private readonly HashSet<Vector3Int> queued=new HashSet<Vector3Int>();
        private Material sky,previousSky;
        private Transform skyDome;
        private Color previousAmbient,previousFogColor;
        private bool previousFog;
        private float day,nextSky,nextClockSave;
        private string timeKey;
        private Game.World.Dimensions.DimensionTravelMenu dimensionMenu;
        public static bool UIBlocked => Time.timeScale<.001f || Game.UI.Settings.SettingsTabsController.IsOpen ||
            Game.GameplaySystems.Respawn.DeathScreenUI.IsVisible ||
            (Game.Inventory.UI.InventoryUI.Instance!=null&&Game.Inventory.UI.InventoryUI.Instance.IsOpen) ||
            (Instance!=null&&Instance.dimensionMenu!=null&&Instance.dimensionMenu.IsOpen);

        public void Initialize(Transform player)
        {
            if(World!=null)return;Instance=this;
            World=new VoxelWorld3D();Generator=new Voxel3DGenerator(Game.World.Dimensions.DimensionTravelRuntime.Current.Seed);
            Services=new VoxelGameplayState3D(this);Lighting=new VoxelLighting3D(World,Generator);
            Transform root=new GameObject("Voxel3D Chunks").transform;root.SetParent(transform,false);
            Fluids=new VoxelFluids3D(this,root);Simulation=new VoxelCellSimulation3D(this);
            renderer=new VoxelChunkRenderer3D(World,root,Lighting);
            loader=new VoxelChunkLoader3D(World,Generator,renderer,horizontalChunkRadius,verticalChunkRadius,c => { Lighting.ChunkLoaded(c); Fluids.Rebuild(c.Coordinate);Simulation.Loaded(c); },c => { Lighting.ChunkRemoved(c); Fluids.Remove(c); });
            var weather=GetComponent<Game.World.Weather.WeatherManager>();if(weather!=null)weather.enabled=false;
            var spawn=GetComponent<Game.Entities.Spawn.EntitySpawnManager>();if(spawn!=null)spawn.enabled=false;
            previousSky=RenderSettings.skybox;previousAmbient=RenderSettings.ambientLight;previousFog=RenderSettings.fog;previousFogColor=RenderSettings.fogColor;
            Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelSky3D");
            if(shader!=null){sky=new Material(shader);RenderSettings.skybox=sky;}
            RenderSettings.fog=false;RenderSettings.ambientLight=Color.black;
            string save=SaveGameRuntime.HasActiveSave?SaveGameRuntime.CurrentSaveId:"editor";
            timeKey="Telder.DayTime."+save+"."+Game.World.Dimensions.DimensionTravelRuntime.Current.Name;
            day=PlayerPrefs.GetFloat(timeKey,.32f);TickSky();
            Particles=new GameObject("Voxel Lit Pixel Particles").AddComponent<VoxelParticles3D>();Particles.transform.SetParent(transform,false);
            SetupPlayer(player);Voxel3DSaveRuntime.EnsureLoaded();
            dimensionMenu=FindFirstObjectByType<Game.World.Dimensions.DimensionTravelMenu>();
        }
        private void SetupPlayer(Transform source)
        {
            if(source==null){var inventory=FindFirstObjectByType<Game.Inventory.PlayerInventory>();if(inventory!=null)source=inventory.transform;}
            if(source==null){Debug.LogError("VOXEL3D: player missing");return;}
            Player=source;Inventory=source.GetComponent<Game.Inventory.PlayerInventory>();
            // Keep common inventory, food, crafting, health, hunger and persistence.
            string[] disabled={"PlayerController","PlayerCollision","PlayerAnimationController","HeldItemVisualController","BlockPlacementAnimator","BlockInteraction","PlayerFallDamage","PlayerWeaponController","PlayerWeaponRigController","ArmMiningOverlayController","PlayerFootstepDust","DurabilityMiningHook","MultiBlockPlayerController","WorkbenchInteraction","ToolBenchInteraction","ChestInteraction","FurniturePlacementModeController","HarpoonController2D","PlayerWorldLighting"};
            foreach(var component in source.GetComponentsInChildren<MonoBehaviour>(true))
                foreach(string type in disabled)if(component.GetType().Name==type){component.enabled=false;break;}
            foreach(var body in source.GetComponentsInChildren<Rigidbody2D>(true))body.simulated=false;
            foreach(var collider in source.GetComponentsInChildren<Collider2D>(true))collider.enabled=false;
            PlayerCamera=Camera.main;if(PlayerCamera==null){var go=new GameObject("Voxel Camera");go.tag="MainCamera";PlayerCamera=go.AddComponent<Camera>();}
            PlayerCamera.clearFlags=CameraClearFlags.Skybox;PlayerCamera.farClipPlane=112;
            if(sky!=null)
            {
                // A background mesh also works when the scene uses Renderer2D,
                // whose camera setup may omit the skybox pass.
                var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.name="Pixel Sky Background";dome.layer=2;
                var skyCollider=dome.GetComponent<Collider>();if(skyCollider!=null)skyCollider.enabled=false;Destroy(skyCollider);dome.transform.SetParent(transform,false);dome.transform.localScale=Vector3.one*100;
                dome.GetComponent<MeshRenderer>().sharedMaterial=sky;skyDome=dome.transform;
            }
            var motor=new GameObject("Voxel3D Player Motor");motor.transform.SetParent(transform,false);motor.layer=2;
            Vector3 spawn=new Vector3(.5f,Generator.GetSurfaceHeight(0,0)+2,.5f);
            if(SaveGameRuntime.TryGetCurrentPlayerPosition(out var saved))spawn=saved-Vector3.up*.9f;
            motor.transform.position=spawn;source.SetParent(motor.transform,false);source.localPosition=Vector3.up*.9f;source.localRotation=Quaternion.identity;
            motor.AddComponent<CharacterController>();Motor=motor.AddComponent<VoxelPlayerController3D>();
            var avatar=new GameObject("Voxel Player Model");avatar.layer=2;avatar.transform.SetParent(motor.transform,false);Avatar=avatar.AddComponent<VoxelAvatar3D>();Avatar.Initialize(source);
            if(!Motor.Initialize(PlayerCamera,source))return;
            var interaction=source.GetComponent<VoxelBlockInteraction3D>()??source.gameObject.AddComponent<VoxelBlockInteraction3D>();interaction.Initialize(PlayerCamera,this);
            var combat=source.gameObject.AddComponent<VoxelCombat3D>();combat.Initialize(this);
            var survival=source.gameObject.AddComponent<VoxelSurvival3D>();survival.Initialize(this);
            gameObject.AddComponent<VoxelEntitySpawner3D>();Weather=gameObject.AddComponent<VoxelWeather3D>();
            loader.UpdatePlayer(motor.transform.position);loader.Process(5,double.PositiveInfinity);Ready=true;VoxelItemDrop3D.Restore(this);
        }
        private void Update()
        {
            if(Player==null||loader==null)return;
            loader.UpdatePlayer(Player.position);loader.Process(Mathf.Clamp(chunksPerFrame,1,3));
            int budget=2;while(budget-->0&&dirty.Count>0){Vector3Int cell=dirty.Dequeue();queued.Remove(cell);renderer.QueueRebuild(cell);Fluids.Rebuild(cell);}
            renderer.ProcessQueuedRebuilds(1,2.0);
            Fluids.Tick();Simulation.Tick();Voxel3DSaveRuntime.Tick();
            day=Mathf.Repeat(day+Time.deltaTime/1200f,1);
            if(Time.unscaledTime>=nextSky){nextSky=Time.unscaledTime+.2f;TickSky();}
            if(Time.unscaledTime>=nextClockSave){nextClockSave=Time.unscaledTime+30;PlayerPrefs.SetFloat(timeKey,day);}
        }
        public bool IsLoaded(Vector3Int p)=>World!=null&&World.TryGetChunk(VoxelWorld3D.WorldToChunk(p.x,p.y,p.z),out _);
        public void SetBlock(Vector3Int cell,ushort id)
        {
            if(!IsLoaded(cell))return;ushort old=World.GetBlock(cell.x,cell.y,cell.z);if(old==id)return;
            if(old!=0)Services.Removing(cell,old);
            Game.Gameplay.Plants.PlantRuntime.Forget(cell.x,cell.y,cell.z,true);
            World.SetBlock(cell.x,cell.y,cell.z,id);
            var plant=VoxelBlockCatalog.Get(id);if(plant?.Plant!=null)Game.Gameplay.Plants.PlantRuntime.Track(plant.ID.ToString(),cell.x,cell.y,cell.z,true);Voxel3DSaveRuntime.Record(cell.x,cell.y,cell.z,id);Refresh(cell);Fluids.Around(cell);Simulation.Around(cell);
        }
        public void Refresh(Vector3Int cell)
        {
            Game.World.Lighting.HandheldLightRuntime.InvalidateVoxel(this);
            Lighting.Changed(cell,World.GetBlock(cell.x,cell.y,cell.z));
            Vector3Int chunk=VoxelWorld3D.WorldToChunk(cell.x,cell.y,cell.z);
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
            {var c=chunk+new Vector3Int(x,y,z);if(World.TryGetChunk(c,out _)&&queued.Add(c))dirty.Enqueue(c);}
        }
        public void PositionSky(Vector3 position){if(skyDome!=null)skyDome.position=position;}
        public void SaveState()
        {Services?.Save();Fluids?.Flush();Voxel3DSaveRuntime.Flush();VoxelItemDrop3D.Save(this);if(!string.IsNullOrEmpty(timeKey))PlayerPrefs.SetFloat(timeKey,day);}
        private void OnApplicationPause(bool paused){if(paused)SaveState();}
        private void OnApplicationQuit(){SaveState();}
        public void Respawn()
        {
            Vector3 p=Services.RespawnPosition();loader.UpdatePlayer(p);loader.Process(8,double.PositiveInfinity);Motor.Teleport(p);
        }
        private void TickSky()
        {
            float angle=-day*Mathf.PI*2-Mathf.PI*.5f;
            Vector3 sun=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),.25f).normalized;
            float brightness=Mathf.Lerp(.1f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.22f,sun.y)));
            Shader.SetGlobalFloat("_TelderDayTime",day);Shader.SetGlobalFloat("_TelderDayFactor",brightness);
            Shader.SetGlobalVector("_VoxelSunDirection",sun);
            var biome=Generator.GetBiome(Player!=null?Mathf.FloorToInt(Player.position.x):0,Player!=null?Mathf.FloorToInt(Player.position.z):0);
            if(biome==null)return;
            if(sky!=null){sky.SetColor("_SkyTint",biome.SkyColor);sky.SetColor("_GroundColor",biome.GroundColor);}
            Shader.SetGlobalColor("_VoxelFogColor",biome.FogColor*Mathf.Lerp(.18f,1,brightness));
            Shader.SetGlobalFloat("_VoxelFogStart",48);Shader.SetGlobalFloat("_VoxelFogEnd",108);
        }
        private void OnDestroy()
        {
            Ready=false;if(Services!=null)Services.Dispose();Fluids?.Dispose();Voxel3DSaveRuntime.Flush();renderer?.Dispose();
            if(!string.IsNullOrEmpty(timeKey))PlayerPrefs.SetFloat(timeKey,day);
            if(RenderSettings.skybox==sky)RenderSettings.skybox=previousSky;
            RenderSettings.ambientLight=previousAmbient;RenderSettings.fog=previousFog;RenderSettings.fogColor=previousFogColor;
            if(sky!=null)Destroy(sky);if(Instance==this)Instance=null;
        }
    }
}
