using System;
using System.Collections.Generic;
using System.IO;
using Game.Blocks;
using Game.Inventory;
using Game.World.Rendering;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [Serializable] public sealed class VoxelServiceCell
    {
        public int X,Y,Z,Address;
        public ushort BlockId;public string ContentId;
        public bool Open;
        public int AnchorX,AnchorY,AnchorZ;
    }
    [Serializable] public sealed class VoxelServiceFile
    { public List<VoxelServiceCell> Cells=new List<VoxelServiceCell>();public bool HasBed;public Vector3 Bed; }

    // Stable virtual addresses let the original chest/furnace UIs and save data
    // operate on three-dimensional cells without throwing away the Z coordinate.
    public sealed class VoxelGameplayState3D
    {
        private readonly Dictionary<Vector3Int,VoxelServiceCell> cells=new Dictionary<Vector3Int,VoxelServiceCell>();
        private readonly Dictionary<int,VoxelServiceCell> addresses=new Dictionary<int,VoxelServiceCell>();
        private readonly VoxelWorld3DRuntime runtime;
        private VoxelServiceFile file=new VoxelServiceFile();
        private int nextAddress=int.MinValue+1024;
        private readonly string path;
        public VoxelGameplayState3D(VoxelWorld3DRuntime runtime)
        {
            this.runtime=runtime;path=Voxel3DSaveRuntime.AuxiliaryPath("services");
            try{if(path!=null&&File.Exists(path))file=JsonUtility.FromJson<VoxelServiceFile>(File.ReadAllText(path))??file;}
            catch(Exception e){Debug.LogWarning("VOXEL SERVICES: "+e.Message);}
            if(file.Cells==null)file.Cells=new List<VoxelServiceCell>();
            foreach(var cell in file.Cells)
            {
                if(!string.IsNullOrEmpty(cell.ContentId))cell.BlockId=VoxelBlockCatalog.Id(cell.ContentId);
                cells[new Vector3Int(cell.X,cell.Y,cell.Z)]=cell;addresses[cell.Address]=cell;
                nextAddress=Mathf.Max(nextAddress,cell.Address+1);
            }
            BlockVisualStateRuntime.StateChanged+=StateChanged;
        }
        public void Dispose(){Save();BlockVisualStateRuntime.StateChanged-=StateChanged;}
        public void Save()
        {
            if(path==null)return;
            try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(file,true));}
            catch(Exception e){Debug.LogWarning("VOXEL SERVICES SAVE: "+e.Message);}
        }
        private void StateChanged(int x,int y,BlockVisualLayer layer,string state)
        {if(y==0&&addresses.TryGetValue(x,out var cell))runtime.Refresh(new Vector3Int(cell.X,cell.Y,cell.Z));}
        public int Address(Vector3Int p,ushort id)
        {
            if(!cells.TryGetValue(p,out var entry))
            {
                entry=new VoxelServiceCell{X=p.x,Y=p.y,Z=p.z,Address=nextAddress++,BlockId=id,ContentId=VoxelBlockCatalog.Get(id)?.ID,AnchorX=p.x,AnchorY=p.y,AnchorZ=p.z};
                cells[p]=entry;addresses[entry.Address]=entry;file.Cells.Add(entry);Save();
            }
            entry.BlockId=id;entry.ContentId=VoxelBlockCatalog.Get(id)?.ID;return entry.Address;
        }
        public static bool TryResolve(int x,int y,out Vector3Int cell,out ushort id)
        {
            cell=default;id=0;var rt=VoxelWorld3DRuntime.Instance;
            if(rt==null||rt.Services==null||y!=0||!rt.Services.addresses.TryGetValue(x,out var entry))return false;
            cell=new Vector3Int(entry.X,entry.Y,entry.Z);id=entry.BlockId;return id!=0;
        }
        public static Vector3 Position(int x,int y,Vector3 fallback)
        {return TryResolve(x,y,out var p,out _)?(Vector3)p+Vector3.one*.5f:fallback;}
        public static bool IsOpen(Vector3Int cell)
        {var rt=VoxelWorld3DRuntime.Instance;return rt!=null&&rt.Services!=null&&rt.Services.cells.TryGetValue(cell,out var entry)&&entry.Open;}
        public static Vector2 UV(BlockDefinition def,Vector3Int cell,Vector2 uv)
        {
            var rt=VoxelWorld3DRuntime.Instance;if(rt==null||rt.Services==null||def.MultiBlock==null||!rt.Services.cells.TryGetValue(cell,out var entry))return uv;
            var m=def.MultiBlock;return new Vector2((uv.x+cell.x-entry.AnchorX+m.AnchorX)/Mathf.Max(1,m.Width),(uv.y+cell.y-entry.AnchorY+m.AnchorY)/Mathf.Max(1,m.Height));
        }
        public static string TextureAt(BlockDefinition def,Vector3Int cell)
        {
            if(def.Plant!=null)return Game.Gameplay.Plants.PlantRuntime.Texture(def,cell);
            var rt=VoxelWorld3DRuntime.Instance;
            if(rt!=null&&rt.Services!=null&&rt.Services.cells.TryGetValue(cell,out var entry))
                return BlockVisualResolver.ResolveTextureName(def,entry.Address,0,BlockVisualLayer.Foreground);
            return def.Texture;
        }
        public bool IsBurning(Vector3Int p)
        {return cells.TryGetValue(p,out var entry)&&string.Equals(BlockVisualStateRuntime.GetState(entry.Address,0,BlockVisualLayer.Foreground),"burning",StringComparison.OrdinalIgnoreCase);}
        public Vector3Int Anchor(Vector3Int p)
        {return cells.TryGetValue(p,out var entry)?new Vector3Int(entry.AnchorX,entry.AnchorY,entry.AnchorZ):p;}
        public void RegisterPart(Vector3Int p,Vector3Int anchor,ushort id)
        {Address(p,id);var entry=cells[p];entry.AnchorX=anchor.x;entry.AnchorY=anchor.y;entry.AnchorZ=anchor.z;}
        public List<Vector3Int> Parts(Vector3Int anchor)
        {
            var result=new List<Vector3Int>();foreach(var pair in cells)
                if(pair.Value.BlockId!=0&&new Vector3Int(pair.Value.AnchorX,pair.Value.AnchorY,pair.Value.AnchorZ)==anchor)result.Add(pair.Key);
            if(result.Count==0)result.Add(anchor);return result;
        }
        public bool Use(Vector3Int p,PlayerInventory inventory)
        {
            p=Anchor(p);ushort id=runtime.World.GetBlock(p.x,p.y,p.z);var def=VoxelBlockCatalog.Get(id);if(def==null)return false;
            bool interactive=Game.Gameplay.Cooking.CauldronRuntime.IsCauldron(def.ID)||VoxelBlockCatalog.Tag(def,"furnace")||VoxelBlockCatalog.Tag(def,"chest")||VoxelBlockCatalog.Tag(def,"tool_bench")||VoxelBlockCatalog.Tag(def,"workbench")||VoxelBlockCatalog.Tag(def,"bed")||def.MultiBlock!=null&&string.Equals(def.MultiBlock.Kind,"bed",StringComparison.OrdinalIgnoreCase)||Game.GameplaySystems.Interactions.InteractiveBlockRuntime.IsDoorOrHatch(def);
            if(!interactive)return false;
            int address=Address(p,id);
            if(Game.Gameplay.Cooking.CauldronRuntime.IsCauldron(def.ID))return Game.Gameplay.Cooking.CauldronRuntime.Instance?.TryOpenAtCell(address,0)==true;
            if(VoxelBlockCatalog.Tag(def,"furnace"))
            {var furnace=Game.GameplaySystems.Furnace.FurnaceRuntime.Instance;return furnace!=null&&furnace.TryOpenAtCell(address,0);}
            if(VoxelBlockCatalog.Tag(def,"chest"))
            {var ui=Game.Chests.UI.ChestUIController.Instance;if(ui!=null)ui.OpenChest(address,0);return ui!=null;}
            if(VoxelBlockCatalog.Tag(def,"tool_bench"))
            {Game.ToolBench.ToolBenchUI.EnsureCreated().Open(inventory);return true;}
            if(VoxelBlockCatalog.Tag(def,"workbench"))
            {Game.Crafting.UI.CraftingUIController.Instance?.OpenWorkbench();return true;}
            if(VoxelBlockCatalog.Tag(def,"bed")||def.MultiBlock!=null&&string.Equals(def.MultiBlock.Kind,"bed",StringComparison.OrdinalIgnoreCase))
            {file.HasBed=true;file.Bed=(Vector3)p+new Vector3(.5f,1,.5f);Save();Game.GameplaySystems.Respawn.DeathScreenUI.ShowToast("Точка возрождения установлена");return true;}
            if(Game.GameplaySystems.Interactions.InteractiveBlockRuntime.IsDoorOrHatch(def))
            {
                bool open=!cells[p].Open;
                foreach(var part in Parts(p))
                {
                    Address(part,id);cells[part].Open=open;
                    BlockVisualStateRuntime.SetState(cells[part].Address,0,BlockVisualLayer.Foreground,open?"open":"closed");
                    runtime.Refresh(part);
                }
                Save();return true;
            }
            return false;
        }
        public void Removing(Vector3Int p,ushort id)
        {
            if(!cells.TryGetValue(p,out var entry))return;
            var def=VoxelBlockCatalog.Get(id);
            if(Game.Gameplay.Cooking.CauldronRuntime.IsCauldron(def?.ID))Game.Gameplay.Cooking.CauldronRuntime.Instance?.NotifyRemoved(entry.Address,0);
            if(VoxelBlockCatalog.Tag(def,"furnace"))Game.GameplaySystems.Furnace.FurnaceRuntime.Instance?.NotifyForegroundBlockRemoved(entry.Address,0,id);
            if(VoxelBlockCatalog.Tag(def,"chest"))Game.Chests.ChestManager.Instance?.NotifyVoxelRemoved(entry.Address);
            if(file.HasBed&&(file.Bed-((Vector3)p+new Vector3(.5f,1,.5f))).sqrMagnitude<.1f)file.HasBed=false;
            entry.Open=false;entry.BlockId=0;entry.ContentId=null;BlockVisualStateRuntime.ClearState(entry.Address,0,BlockVisualLayer.Foreground);Save();
        }
        public Vector3 RespawnPosition()
        {
            if(file.HasBed)
            {
                var p=Vector3Int.FloorToInt(file.Bed-new Vector3(0,1,0));
                if(!runtime.IsLoaded(p)||runtime.World.GetBlock(p.x,p.y,p.z)!=0)return file.Bed;
                file.HasBed=false;Save();
            }
            return new Vector3(.5f,runtime.Generator.GetSurfaceHeight(0,0)+2,.5f);
        }
    }
}
