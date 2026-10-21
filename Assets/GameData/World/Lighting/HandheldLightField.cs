using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Lighting
{
    public struct HandheldLightSource : IEquatable<HandheldLightSource>
    {
        public Vector3Int Cell;
        public LightNode Light;
        public HandheldLightSource(Vector3Int cell,LightNode light){Cell=cell;Light=light;}
        public bool Equals(HandheldLightSource other)=>Cell==other.Cell&&Light.R==other.Light.R&&Light.G==other.Light.G&&Light.B==other.Light.B;
    }

    // Moving light is a small independent field. It never clears or queues the
    // static sunlight/torch solver, and can disappear without leaving a trail.
    public sealed class HandheldLightField
    {
        private static readonly Vector3Int[] Directions={Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down,new Vector3Int(0,0,1),new Vector3Int(0,0,-1)};
        private readonly Func<Vector3Int,int> getOpacity;
        private readonly Action<Vector3Int> markDirty;
        private readonly int directionCount;
        private readonly Dictionary<Vector3Int,LightNode> cells=new Dictionary<Vector3Int,LightNode>(1024);
        private readonly Queue<Vector3Int> queue=new Queue<Vector3Int>(1024);
        private readonly HashSet<Vector3Int> queued=new HashSet<Vector3Int>();
        private readonly List<HandheldLightSource> sources=new List<HandheldLightSource>(8);
        private bool invalidated;
        public int CellCount=>cells.Count;
        public HandheldLightField(bool threeD,Func<Vector3Int,int> opacity,Action<Vector3Int> dirty=null)
        {directionCount=threeD?6:4;getOpacity=opacity;markDirty=dirty;}
        public void Invalidate(){invalidated=true;}
        public LightNode Get(Vector3Int cell)=>cells.TryGetValue(cell,out var light)?light:default;
        public static LightNode Combine(LightNode a,LightNode b)=>new LightNode(a.Sun,(byte)Mathf.Max(a.R,b.R),(byte)Mathf.Max(a.G,b.G),(byte)Mathf.Max(a.B,b.B));
        public bool ReplaceSources(IReadOnlyList<HandheldLightSource> next)
        {
            int count=Mathf.Min(next?.Count??0,16);bool changed=invalidated||count!=sources.Count;
            if(!changed)for(int i=0;i<count;i++)if(!sources[i].Equals(next[i])){changed=true;break;}
            if(!changed)return false;
            foreach(var p in cells)markDirty?.Invoke(p.Key);
            sources.Clear();for(int i=0;i<count;i++)sources.Add(next[i]);
            cells.Clear();queue.Clear();queued.Clear();invalidated=false;
            foreach(var source in sources)
            {
                LightNode light=source.Light;light.Sun=0;
                light.R=(byte)Mathf.Min(15,light.R);light.G=(byte)Mathf.Min(15,light.G);light.B=(byte)Mathf.Min(15,light.B);
                if(light.R+light.G+light.B==0)continue;
                cells[source.Cell]=Combine(Get(source.Cell),light);Enqueue(source.Cell);
            }
            while(queue.Count>0)
            {
                var from=queue.Dequeue();queued.Remove(from);LightNode light=cells[from];
                if(Mathf.Max(light.R,Mathf.Max(light.G,light.B))<=1)continue;
                for(int i=0;i<directionCount;i++)
                {
                    var cell=from+Directions[i];int opacity=Mathf.Clamp(getOpacity(cell),0,15);
                    int loss=opacity>=15?1:1+opacity;LightNode old=Get(cell);
                    var updated=new LightNode(0,(byte)Mathf.Max(old.R,light.R-loss),(byte)Mathf.Max(old.G,light.G-loss),(byte)Mathf.Max(old.B,light.B-loss));
                    if(updated.R==old.R&&updated.G==old.G&&updated.B==old.B)continue;
                    cells[cell]=updated;if(opacity<15)Enqueue(cell);
                }
            }
            foreach(var p in cells)markDirty?.Invoke(p.Key);
            return true;
        }
        private void Enqueue(Vector3Int cell){if(queued.Add(cell))queue.Enqueue(cell);}
        public void Clear(){ReplaceSources(null);}
    }
}
