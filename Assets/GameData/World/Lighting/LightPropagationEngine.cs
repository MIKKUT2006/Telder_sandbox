using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Blocks;
using Game.Fluids;
using Game.World.Furniture;
using UnityEngine;
using ChunkKey = UnityEngine.Vector2Int;

namespace Game.World.Lighting
{
    // Relight independent chunk tiles with a light-radius halo. World light is
    // published only after a complete tile has converged; rendering never sees
    // the temporary cleared buffer while work is spread across frames.
    public class LightPropagationEngine
    {
        public const byte MaxLight = 15;
        private const int Halo = MaxLight + 2;
        private const int Width = Chunk.SizeX + Halo * 2;
        private const int Height = Chunk.SizeY + Halo * 2;
        private const int StepCells = 32;
        private readonly ILightWorld world;
        private readonly HashSet<ChunkKey> pending = new HashSet<ChunkKey>();
        private readonly Dictionary<int,int> shadowColumns = new Dictionary<int,int>();
        private readonly Dictionary<ushort,BlockLightInfo> blockInfo = new Dictionary<ushort,BlockLightInfo>();
        private readonly Queue<int> flood = new Queue<int>(8192);
        private readonly LightNode[] values = new LightNode[Width * Height];
        private readonly byte[] opacity = new byte[Width * Height];
        private readonly bool[] loaded = new bool[Width * Height];
        private readonly bool[] queued = new bool[Width * Height];
        private readonly Stopwatch clock = new Stopwatch();
        private Func<int,int,byte> unloadedSkyOpacity;
        private ChunkKey priority, active;
        private bool working;
        private int stage,cursor,minX,minY,skyX,skyY,opaqueDepth,skyTop,requestedHeight=256;
        private byte directSun;
        public int PendingChunkCount => pending.Count + (working ? 1 : 0) + shadowColumns.Count;
        public long CompletedTiles {get;private set;}
        public int LastProcessSteps {get;private set;}
        public LightPropagationEngine(ILightWorld world){this.world=world;}
        public void SetPriority(int chunkX,int chunkY){priority=new ChunkKey(chunkX,chunkY);}
        public void SetUnloadedSkyOpacityResolver(Func<int,int,byte> resolver){unloadedSkyOpacity=resolver;}
        public bool IsChunkPending(int x,int y)=>pending.Contains(new ChunkKey(x,y)) || working&&active==new ChunkKey(x,y) ||
            shadowColumns.TryGetValue(x,out int highest)&&y*Chunk.SizeY<=highest+Halo;
        private bool HasChunk(int x,int y)
        {return world!=null&&world.IsLoaded(x*Chunk.SizeX,y*Chunk.SizeY);}
        public void ForgetChunk(int x,int y)
        {var key=new ChunkKey(x,y);pending.Remove(key);if(working&&active==key)working=false;}
        public void RequestChunk(int chunkX,int chunkY,bool force=false)
        {var key=new ChunkKey(chunkX,chunkY);if(!force&&working&&active==key)return;if(HasChunk(chunkX,chunkY))pending.Add(key);}
        public void RebuildLoadedWorld(int worldHeight)
        {
            if(world is Game.World.World movingWorld)movingWorld.HandheldLight.Invalidate();
            requestedHeight=Mathf.Max(1,worldHeight);
            blockInfo.Clear();
            if(world is Game.World.World concrete)
                foreach(var pair in concrete.GetLoadedChunks())RequestChunk(pair.Key.x,pair.Key.y,true);
        }
        public void RebuildAfterChunkGenerated(int chunkX,int chunkY,int worldHeight)
        {
            if(world is Game.World.World movingWorld)movingWorld.HandheldLight.Invalidate();
            requestedHeight=Mathf.Max(1,worldHeight);
            for(int x=chunkX-1;x<=chunkX+1;x++)for(int y=chunkY-1;y<=chunkY+1;y++)RequestChunk(x,y,true);
            // A newly loaded roof can correct sunlight in an already visible
            // cave. An old "lit once" flag must not suppress that correction.
            for(int x=chunkX-1;x<=chunkX+1;x++)QueueShadowColumn(x,(chunkY+1)*Chunk.SizeY-1);
        }
        public void RebuildAfterBlockChanged(int x,int y,int worldHeight)
        {RebuildAfterBlockChanged(x,y,worldHeight,true);}
        public void RebuildAfterBlockChanged(int x,int y,int worldHeight,bool sunlightChanged)
        {
            if(world is Game.World.World movingWorld)movingWorld.HandheldLight.Invalidate();
            requestedHeight=Mathf.Max(1,worldHeight);
            RequestRectangle(x-Halo,y-Halo,x+Halo,y+Halo);
            if(sunlightChanged)
                for(int cx=FloorDiv(x-MaxLight,Chunk.SizeX);cx<=FloorDiv(x+MaxLight,Chunk.SizeX);cx++)QueueShadowColumn(cx,y);
        }
        public void RebuildAfterLiquidRegionChanged(int minX,int minY,int maxX,int maxY,int worldHeight)
        {requestedHeight=Mathf.Max(1,worldHeight);RequestRectangle(minX-Halo,minY-Halo,maxX+Halo,maxY+Halo);}
        public void GenerateSunlightForChunk(int chunkX,int chunkY)
        {RebuildAfterChunkGenerated(chunkX,chunkY,requestedHeight);}
        private void QueueShadowColumn(int chunkX,int y)
        {if(!shadowColumns.TryGetValue(chunkX,out int old)||y>old)shadowColumns[chunkX]=y;}
        private void RequestRectangle(int minX,int minY,int maxX,int maxY)
        {
            for(int x=FloorDiv(minX,Chunk.SizeX);x<=FloorDiv(maxX,Chunk.SizeX);x++)
                for(int y=FloorDiv(minY,Chunk.SizeY);y<=FloorDiv(maxY,Chunk.SizeY);y++)RequestChunk(x,y,true);
        }
        private void FlushShadowColumns()
        {
            if(shadowColumns.Count==0)return;
            if(world is Game.World.World concrete)
                foreach(var pair in concrete.GetLoadedChunks())
                    if(shadowColumns.TryGetValue(pair.Key.x,out int highest)&&pair.Key.y*Chunk.SizeY<=highest+Halo)RequestChunk(pair.Key.x,pair.Key.y,true);
            shadowColumns.Clear();
        }
        public void ProcessPending(double budgetMilliseconds=1.25,int maxSteps=256)
        {
            LastProcessSteps=0;if(world==null)return;clock.Restart();FlushShadowColumns();
            while(LastProcessSteps<maxSteps && (LastProcessSteps==0||clock.Elapsed.TotalMilliseconds<budgetMilliseconds))
            {
                if(!working&&!BeginNext())break;
                Advance();LastProcessSteps++;
            }
            clock.Stop();
        }
        private bool BeginNext()
        {
            bool found=false;ChunkKey next=default;long best=long.MaxValue;
            foreach(var p in pending)
            {
                long dx=(long)p.x-priority.x,dy=(long)p.y-priority.y;long score=dx*dx+dy*dy;
                if(!found||score<best){next=p;best=score;found=true;}
            }
            if(!found)return false;pending.Remove(next);
            if(!HasChunk(next.x,next.y))return BeginNext();
            active=next;working=true;stage=0;cursor=0;minX=active.x*Chunk.SizeX-Halo;minY=active.y*Chunk.SizeY-Halo;
            flood.Clear();skyX=0;skyTop=Mathf.Max(requestedHeight-1,minY+Height-1);
            if(world is Game.World.World concrete)
                foreach(var pair in concrete.GetLoadedChunks())skyTop=Mathf.Max(skyTop,(pair.Key.y+1)*Chunk.SizeY-1);
            skyY=skyTop;directSun=MaxLight;opaqueDepth=0;return true;
        }
        private void Advance()
        {
            if(!HasChunk(active.x,active.y)){working=false;return;}
            if(stage==0)
            {
                int end=Mathf.Min(values.Length,cursor+StepCells);
                for(;cursor<end;cursor++)Snapshot(cursor);
                if(cursor==values.Length){stage=1;cursor=0;}return;
            }
            if(stage==1)
            {
                int steps=StepCells;
                while(steps-->0&&skyX<Width)SeedSkyCell();
                if(skyX==Width)stage=2;return;
            }
            if(stage==2)
            {
                int budget=StepCells;
                while(budget-->0&&flood.Count>0)
                {
                    int source=flood.Dequeue();queued[source]=false;int x=source%Width,y=source/Width;
                    if(x>0)Spread(source,source-1);if(x+1<Width)Spread(source,source+1);
                    if(y>0)Spread(source,source-Width);if(y+1<Height)Spread(source,source+Width);
                }
                if(flood.Count==0)stage=3;return;
            }
            Publish();working=false;CompletedTiles++;
        }
        private BlockLightInfo Info(ushort id)
        {
            if(id==0)return default;
            if(blockInfo.TryGetValue(id,out var info))return info;
            var def=world.GetBlockDefinition(id);info=def==null?new BlockLightInfo{Opacity=MaxLight}:new BlockLightInfo
            {Opacity=(byte)Mathf.Clamp(def.LightOpacity,0,MaxLight),R=(byte)Mathf.Min(MaxLight,def.LightEmissionR),G=(byte)Mathf.Min(MaxLight,def.LightEmissionG),B=(byte)Mathf.Min(MaxLight,def.LightEmissionB)};
            blockInfo[id]=info;return info;
        }
        private void Snapshot(int index)
        {
            int x=minX+index%Width,y=minY+index/Width;queued[index]=false;values[index]=default;
            loaded[index]=world.IsLoaded(x,y);opacity[index]=0;if(!loaded[index])return;
            BlockLightInfo info=Info(world.GetBlock(x,y));opacity[index]=info.Opacity;
            byte r=info.R,g=info.G,b=info.B;
            var furniture=FurnitureLayerManager.Instance;
            if(furniture!=null&&furniture.TryGetDefinition(x,y,out BlockDefinition f)&&f!=null)
            {r=(byte)Mathf.Max(r,Mathf.Min(MaxLight,f.LightEmissionR));g=(byte)Mathf.Max(g,Mathf.Min(MaxLight,f.LightEmissionG));b=(byte)Mathf.Max(b,Mathf.Min(MaxLight,f.LightEmissionB));}
            if(world is Game.World.World concrete)
            {
                ushort liquid=concrete.GetLiquidID(x,y);
                if(liquid!=0&&concrete.GetLiquidAmount(x,y)>0&&FluidRegistry.TryGet(FluidIDRegistry.GetString(liquid),out FluidDefinition fluid))
                {r=(byte)Mathf.Max(r,Mathf.Min(MaxLight,fluid.LightEmissionR));g=(byte)Mathf.Max(g,Mathf.Min(MaxLight,fluid.LightEmissionG));b=(byte)Mathf.Max(b,Mathf.Min(MaxLight,fluid.LightEmissionB));}
            }
            values[index]=new LightNode(0,r,g,b);if(r+g+b>0)Enqueue(index);
        }
        private void NextSkyColumn(){skyX++;skyY=skyTop;directSun=MaxLight;opaqueDepth=0;}
        private void SeedSkyCell()
        {
            if(skyY<minY||directSun==0){NextSkyColumn();return;}
            int x=minX+skyX,y=skyY--;
            bool inside=y<minY+Height;int index=inside?(y-minY)*Width+skyX:-1;
            int op;
            if(inside&&loaded[index])op=opacity[index];
            else if(world.IsLoaded(x,y))op=Info(world.GetBlock(x,y)).Opacity;
            else op=unloadedSkyOpacity!=null?Mathf.Clamp(unloadedSkyOpacity(x,y),0,MaxLight):MaxLight;
            // Unknown cells are conservative when no generation resolver exists.
            // Missing chunk data is never proof of an open skylight.
            byte amount=directSun;bool propagate=true;
            if(op>=MaxLight){opaqueDepth++;if(opaqueDepth>2){NextSkyColumn();return;}amount=opaqueDepth==1?(byte)10:(byte)5;propagate=false;}
            else
            {
                if(opaqueDepth>0){NextSkyColumn();return;}
                directSun=(byte)Mathf.Max(0,directSun-op);amount=directSun;
            }
            if(index>=0&&loaded[index]&&amount>0)
            {var n=values[index];n.Sun=(byte)Mathf.Max(n.Sun,amount);values[index]=n;if(propagate)Enqueue(index);}
        }
        private void Enqueue(int index){if(!queued[index]){queued[index]=true;flood.Enqueue(index);}}
        private void Spread(int source,int target)
        {
            if(!loaded[target])return;int op=opacity[target];bool solid=op>=MaxLight;int attenuation=solid?1:1+op;
            LightNode a=values[source],b=values[target];
            var next=new LightNode((byte)Mathf.Max(b.Sun,a.Sun-attenuation),(byte)Mathf.Max(b.R,a.R-attenuation),(byte)Mathf.Max(b.G,a.G-attenuation),(byte)Mathf.Max(b.B,a.B-attenuation));
            if(next.Sun==b.Sun&&next.R==b.R&&next.G==b.G&&next.B==b.B)return;
            values[target]=next;if(!solid)Enqueue(target);
        }
        private void Publish()
        {
            var data=world.GetLightData(active.x*Chunk.SizeX,active.y*Chunk.SizeY);if(data==null)return;
            data.ApplyRegion(values,Halo*Width+Halo,Width);
            // The GPU light textures include a one-cell neighbor border.
            for(int x=active.x-1;x<=active.x+1;x++)for(int y=active.y-1;y<=active.y+1;y++)
                world.GetLightData(x*Chunk.SizeX,y*Chunk.SizeY)?.MarkDirty();
        }
        private static int FloorDiv(int value,int size){int q=value/size;return value%size<0?q-1:q;}
        private struct BlockLightInfo{public byte Opacity,R,G,B;}
    }
}
