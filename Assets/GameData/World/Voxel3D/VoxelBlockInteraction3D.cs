using System.Collections.Generic;
using Game.InputSupport;
using UnityEngine;
using Game.Content;
using Game.Inventory;
using Game.Blocks;
using Game.Mining;
using Game.World.Items;

namespace Game.World.Voxel3D
{
    /// <summary>
    /// Minecraft-style center-screen voxel interaction.
    /// LMB hold = mine, RMB = place. Targeting always follows the camera crosshair.
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public sealed class VoxelBlockInteraction3D : MonoBehaviour
    {
        public float interactionDistance = 6f;
        public float baseMiningPower = 1f;

        private Camera cam;
        private VoxelWorld3DRuntime runtime;
        private PlayerInventory inventory;
        private Vector3Int miningCell;
        private bool hasTarget;
        private float progress;

        private GameObject selectionObject;
        private MeshRenderer selectionRenderer;

        private static Texture2D crosshairTexture;

        public void Initialize(Camera camera, VoxelWorld3DRuntime worldRuntime)
        {
            cam = camera;
            runtime = worldRuntime;
            inventory = GetComponent<PlayerInventory>();
            EnsureSelectionOutline();
        }

        private void Update()
        {
            if(cam==null||runtime==null||!runtime.Ready||VoxelWorld3DRuntime.UIBlocked||
                GetComponent<Game.PlayerStats.PlayerStats>()?.IsDead==true||
                !GameInput.MobileEnabled&&Cursor.lockState!=CursorLockMode.Locked)
            {SetSelectionVisible(false);ResetMining();return;}
            if(VoxelRaycast3D.Cast(runtime,cam.transform.position,cam.transform.forward,interactionDistance,out var target,out var normal,out _))
            {
                UpdateSelection(target);
                bool weapon=inventory!=null&&Game.Combat.WeaponMetadataRegistry.IsWeapon(inventory.GetSelectedItemId());
                if(GameInput.Held(GameAction.Attack)&&!weapon)Mine(target);else ResetMining();
                if(GameInput.Pressed(GameAction.Use)||GameInput.Pressed(GameAction.Interact))
                {if(!runtime.Services.Use(target,inventory))Place(target+normal);}
            }
            else{SetSelectionVisible(false);ResetMining();}
        }
        private void Mine(Vector3Int cell)
        {
            cell=runtime.Services.Anchor(cell);ushort id=runtime.World.GetBlock(cell.x,cell.y,cell.z);
            if(id==0){ResetMining();return;}
            if(!hasTarget||cell!=miningCell){miningCell=cell;progress=0;hasTarget=true;}
            var def=GetDef(id);var stack=inventory!=null?inventory.GetSelectedStack():null;
            if(def==null||def.Hardness<0||!MiningToolRules.CanBreak(id,inventory)||Game.Items.Durability.DurabilitySystem.IsBroken(stack)){progress=0;return;}
            float hardness=Mathf.Max(.05f,def.Hardness),speed=MiningToolRules.GetSelectedMiningSpeed(inventory);
            progress+=baseMiningPower*Mathf.Max(.05f,speed)*Time.deltaTime/hardness;
            if(progress<1)return;
            Break(runtime,cell,true);
            GetComponent<Game.PlayerStats.PlayerStats>()?.NotifyBlockBroken();
            Game.Items.Durability.DurabilitySystem.Damage(stack,1);inventory?.NotifyExternalChange();ResetMining();
        }
        public static bool Break(VoxelWorld3DRuntime rt,Vector3Int cell,bool loot)
        {
            cell=rt.Services.Anchor(cell);ushort id=rt.World.GetBlock(cell.x,cell.y,cell.z);var def=VoxelBlockCatalog.Get(id);
            if(def==null||def.Hardness<0)return false;
            var plantDrops=Game.Gameplay.Plants.PlantRuntime.Harvest(def,cell.x,cell.y,cell.z,true);
            foreach(var part in rt.Services.Parts(cell))rt.SetBlock(part,0);
            Vector3 center=(Vector3)cell+Vector3.one*.5f;
            rt.Particles.Burst(center,new Color(.65f,.56f,.43f),14,.09f,2.2f,true,false,.9f);
            if(loot)
            {
                foreach(var drop in plantDrops??BlockLootDropper.RollDrops(def,def.ID))
                    if(drop!=null&&drop.Count>0&&!string.IsNullOrWhiteSpace(drop.ItemId))VoxelItemDrop3D.Spawn(drop.ItemId,drop.Count,center);
                Game.Achievements.AchievementRuntime.NotifyBlockBreak(def.ID);
            }
            return true;
        }
        private void Place(Vector3Int cell)
        {
            if(inventory==null||!runtime.IsLoaded(cell)||runtime.World.GetBlock(cell.x,cell.y,cell.z)!=0)return;
            string selected=inventory.GetSelectedItemId();if(string.IsNullOrWhiteSpace(selected))return;
            if(Game.Fluids.FluidRegistry.TryGet(selected,out _))
            {if(runtime.Fluids.PlaceSource(cell,selected))inventory.TryConsumeSelected(1);return;}
            ushort id=VoxelBlockCatalog.Id(selected);var def=VoxelBlockCatalog.Get(id);if(def==null)return;
            var multi=def.MultiBlock;int width=Mathf.Clamp(multi!=null?multi.Width:1,1,16),height=Mathf.Clamp(multi!=null?multi.Height:1,1,16);
            Vector3Int origin=cell-new Vector3Int(multi!=null?multi.AnchorX:0,multi!=null?multi.AnchorY:0,0);
            var controller=GetComponentInParent<CharacterController>();Bounds playerBounds=controller!=null?controller.bounds:new Bounds(transform.position,new Vector3(.6f,1.8f,.6f));
            for(int x=0;x<width;x++)for(int y=0;y<height;y++)
            {
                var part=origin+new Vector3Int(x,y,0);
                if(!runtime.IsLoaded(part)||runtime.World.GetBlock(part.x,part.y,part.z)!=0||
                    playerBounds.Intersects(new Bounds((Vector3)part+Vector3.one*.5f,Vector3.one*.98f)))return;
            }
            if(def.Plant!=null&&!Game.Gameplay.Plants.PlantRuntime.ValidSoil(def.Plant,cell,runtime))return;
            bool hanging=VoxelBlockCatalog.Tag(def,"hanging")||VoxelBlockCatalog.Tag(def,"vines");
            if(hanging&&runtime.World.GetBlock(cell.x,cell.y+height,cell.z)==0)return;
            if(!hanging&&(def.alwaysFurniture||multi!=null&&multi.RequireFloor))
                for(int x=0;x<width;x++)if(runtime.World.GetBlock(origin.x+x,origin.y-1,origin.z)==0)return;
            if(!inventory.TryConsumeSelected(1))return;
            for(int x=0;x<width;x++)for(int y=0;y<height;y++)
            {var part=origin+new Vector3Int(x,y,0);runtime.SetBlock(part,id);if(width*height>1)runtime.Services.RegisterPart(part,cell,id);}
            runtime.Services.Save();Game.Achievements.AchievementRuntime.NotifyBlockPlace(selected);
        }

        private void ResetMining()
        {
            hasTarget = false;
            progress = 0f;
        }

        private void EnsureSelectionOutline()
        {
            if (selectionObject != null)
                return;

            selectionObject = new GameObject("Voxel3D Block Selection");
            selectionObject.layer = 2;

            MeshFilter filter = selectionObject.AddComponent<MeshFilter>();
            selectionRenderer = selectionObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildWireCube();

            Shader shader = UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite")??Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0f, 0f, 0f, .92f);
                selectionRenderer.sharedMaterial = material;
            }

            selectionObject.transform.localScale = Vector3.one * 1.006f;
            SetSelectionVisible(false);
        }

        private static Mesh BuildWireCube()
        {
            Vector3[] v =
            {
                new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0),
                new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1)
            };

            int[] lines =
            {
                0,1, 1,2, 2,3, 3,0,
                4,5, 5,6, 6,7, 7,4,
                0,4, 1,5, 2,6, 3,7
            };

            Mesh mesh = new Mesh();
            mesh.name = "Voxel3D Selection Wire Cube";
            mesh.vertices = v;
            mesh.SetIndices(lines, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void UpdateSelection(Vector3Int cell)
        {
            EnsureSelectionOutline();
            selectionObject.transform.position = (Vector3)cell - Vector3.one * .003f;
            SetSelectionVisible(true);
        }

        private void SetSelectionVisible(bool visible)
        {
            if (selectionRenderer != null)
                selectionRenderer.enabled = visible;
        }

        private void OnGUI()
        {
            if (cam == null || VoxelWorld3DRuntime.UIBlocked || UnityEngine.Cursor.lockState != CursorLockMode.Locked)
                return;

            if (crosshairTexture == null)
            {
                crosshairTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                crosshairTexture.SetPixel(0, 0, Color.white);
                crosshairTexture.Apply();
            }

            float cx = Screen.width * .5f;
            float cy = Screen.height * .5f;

            // Alpha-era simple white + with a dark one-pixel shadow/edge.
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .85f);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 3f, 17f), crosshairTexture);
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 17f, 3f), crosshairTexture);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx, cy - 7f, 1f, 15f), crosshairTexture);
            GUI.DrawTexture(new Rect(cx - 7f, cy, 15f, 1f), crosshairTexture);
            GUI.color = old;
        }

        private void OnDestroy()
        {
            if(selectionObject!=null)
            {
                var filter=selectionObject.GetComponent<MeshFilter>();if(filter!=null&&filter.sharedMesh!=null)Destroy(filter.sharedMesh);
                if(selectionRenderer!=null&&selectionRenderer.sharedMaterial!=null)Destroy(selectionRenderer.sharedMaterial);Destroy(selectionObject);
            }
        }

        private static BlockDefinition GetDef(ushort id)
        {
            try
            {
                ContentID cid = BlockIDRegistry.GetContentID(id);
                return BlockRegistry.Contains(cid) ? BlockRegistry.Get(cid) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
