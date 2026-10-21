using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Save;
using Game.World.Dimensions;

namespace Game.World.Voxel3D
{
    [Serializable] public class Voxel3DChangeFile { public List<Voxel3DBlockChange> Changes = new List<Voxel3DBlockChange>(); }
    [Serializable] public class Voxel3DBlockChange { public int X; public int Y; public int Z; public ushort BlockId; public string ContentId; }

    public static class Voxel3DSaveRuntime
    {
        private static readonly Dictionary<VoxelCoord3D,ushort> changes = new Dictionary<VoxelCoord3D,ushort>();
        private static string loadedKey, loadedPath;
        private static bool dirty;private static float nextWrite;

        public static void EnsureLoaded()
        {
            string key = GetKey(); if(key==loadedKey) return;
            Flush();loadedKey=key;loadedPath=GetPath();changes.Clear();dirty=false;
            string path=GetPath(); if(string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try {
                var file=JsonUtility.FromJson<Voxel3DChangeFile>(File.ReadAllText(path));
                if(file?.Changes==null) return;
                foreach(var c in file.Changes)
                {
                    ushort id=c.BlockId;
                    if(!string.IsNullOrEmpty(c.ContentId)){id=VoxelBlockCatalog.Id(c.ContentId);if(id==0)continue;}
                    changes[new VoxelCoord3D(c.X,c.Y,c.Z)]=id;
                }
            } catch(Exception e) { Debug.LogWarning("VOXEL3D SAVE: " + e.Message); }
        }

        public static void Record(int x,int y,int z,ushort id)
        {
            EnsureLoaded(); changes[new VoxelCoord3D(x,y,z)]=id;dirty=true;
        }

        public static void ApplyToChunk(VoxelChunk3D chunk)
        {
            EnsureLoaded(); Vector3Int o=chunk.WorldOrigin;
            foreach(var kv in changes) {
                int lx=kv.Key.X-o.x, ly=kv.Key.Y-o.y, lz=kv.Key.Z-o.z;
                if(VoxelChunk3D.InBounds(lx,ly,lz)) chunk.Set(lx,ly,lz,kv.Value);
            }
        }

        public static void Tick(){if(dirty&&Time.unscaledTime>=nextWrite){nextWrite=Time.unscaledTime+1;Flush();}}
        public static void Flush() { if(dirty) Write(); }

        private static void Write()
        {
            string path=loadedPath; if(string.IsNullOrEmpty(path)) return;
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path)); var file=new Voxel3DChangeFile();
                foreach(var kv in changes) file.Changes.Add(new Voxel3DBlockChange{X=kv.Key.X,Y=kv.Key.Y,Z=kv.Key.Z,BlockId=kv.Value,ContentId=kv.Value==0?null:VoxelBlockCatalog.Get(kv.Value)?.ID});
                File.WriteAllText(path,JsonUtility.ToJson(file,true));dirty=false;
            } catch(Exception e) { Debug.LogWarning("VOXEL3D SAVE WRITE: " + e.Message); }
        }

        private static string GetKey() { var d=DimensionTravelRuntime.Current; return (SaveGameRuntime.CurrentSaveId??"direct")+"|"+d.Name+"|"+d.Seed; }
        public static string AuxiliaryPath(string category)
        { string path=GetPath();return string.IsNullOrEmpty(path)?null:Path.ChangeExtension(path,null)+"_"+category+".json"; }

        private static string GetPath()
        {
            if(!SaveGameRuntime.HasActiveSave) return null;
            var d=DimensionTravelRuntime.Current; uint h=StableHash(d.Name);
            return Path.Combine(SavePaths.GetDimensionsFolder(SaveGameRuntime.CurrentSaveId),"voxel3d_"+d.Seed+"_"+h+".json");
        }
        private static uint StableHash(string value) { unchecked { uint h=2166136261u; if(value!=null) foreach(char c in value){h^=c;h*=16777619u;} return h; } }
    }
}
