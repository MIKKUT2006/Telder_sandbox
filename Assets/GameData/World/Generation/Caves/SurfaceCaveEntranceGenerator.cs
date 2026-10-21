using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Generation
{
    public struct SurfaceCaveEntranceProfile
    {
        public bool Enabled;
        public bool Mountain;
        public float Chance;
        public float Multiplier;
        public static SurfaceCaveEntranceProfile Default =>
            new SurfaceCaveEntranceProfile { Enabled=true, Chance=-1f, Multiplier=1f };
    }

    // Completed region geometry is cached independently of chunks and loading order.
    public sealed class SurfaceCaveEntranceGenerator
    {
        private sealed class Entrance
        {
            public readonly HashSet<long> Cells = new HashSet<long>();
            public int MinX=int.MaxValue, MaxX=int.MinValue;
            public int MinY=int.MaxValue, MaxY=int.MinValue;
        }

        private readonly struct Node
        {
            public readonly Vector2 Position;
            public readonly float Radius;
            public Node(Vector2 position,float radius) { Position=position; Radius=radius; }
        }

        private readonly int seed;
        private readonly CaveSettings settings;
        private readonly Func<int,int> heightAt;
        private readonly Func<int,SurfaceCaveEntranceProfile> profileAt;
        private readonly Func<int,int,int,bool> naturalCave;
        private readonly Dictionary<int,Entrance> regions = new Dictionary<int,Entrance>();
        private readonly Queue<int> regionOrder = new Queue<int>();
        private readonly Dictionary<int,int> buildHeights = new Dictionary<int,int>();
        private readonly List<Entrance> column = new List<Entrance>(4);
        private readonly List<Node> nodes = new List<Node>(192);
        private int columnX=int.MinValue;

        public SurfaceCaveEntranceGenerator(int seed,CaveSettings settings,Func<int,int> heightAt,
            Func<int,SurfaceCaveEntranceProfile> profileAt,Func<int,int,int,bool> naturalCave)
        {
            this.seed=seed;
            this.settings=settings ?? throw new ArgumentNullException(nameof(settings));
            this.heightAt=heightAt ?? throw new ArgumentNullException(nameof(heightAt));
            this.profileAt=profileAt;
            this.naturalCave=naturalCave ?? throw new ArgumentNullException(nameof(naturalCave));
        }

        public bool Contains(int x,int y,int surface)
        {
            if (!settings.EnableSurfaceEntrances || y>surface) return false;
            if (columnX!=x) PrepareColumn(x);
            long key=Pack(x,y);
            for (int i=0;i<column.Count;i++)
            {
                Entrance entrance=column[i];
                if (y>=entrance.MinY && y<=entrance.MaxY && entrance.Cells.Contains(key))
                    return true;
            }
            return false;
        }

        public void Invalidate()
        {
            regions.Clear(); regionOrder.Clear(); column.Clear();
            columnX=int.MinValue;
        }

        private void PrepareColumn(int x)
        {
            columnX=x; column.Clear();
            int size=Mathf.Clamp(settings.EntranceRegionSize,16,512);
            int length=Mathf.Clamp(Mathf.Max(settings.EntranceLength,settings.MountainEntranceLength),24,160);
            int reach=Mathf.CeilToInt(length*1.2f+48f);
            int first=FloorDiv(x-reach,size),last=FloorDiv(x+reach,size);
            for (int region=first;region<=last;region++)
            {
                if (!regions.TryGetValue(region,out Entrance entrance))
                {
                    entrance=Build(region,size);
                    regions.Add(region,entrance);
                    regionOrder.Enqueue(region);
                    while (regionOrder.Count>256) regions.Remove(regionOrder.Dequeue());
                }
                if (entrance!=null && x>=entrance.MinX && x<=entrance.MaxX)
                    column.Add(entrance);
            }
        }

        private SurfaceCaveEntranceProfile Profile(int x)
        { return profileAt!=null ? profileAt(x) : SurfaceCaveEntranceProfile.Default; }

        private int Height(int x)
        {
            if (!buildHeights.TryGetValue(x,out int height))
            { height=heightAt(x); buildHeights.Add(x,height); }
            return height;
        }

        private Entrance Build(int region,int size)
        {
            buildHeights.Clear(); nodes.Clear();
            int start=region*size;
            int entranceX=start+Mathf.Min(size-1,(int)(Hash01(region,0,1701)*size));
            SurfaceCaveEntranceProfile selected=Profile(entranceX);
            bool mountain=false;
            int side=1;
            float best=float.NegativeInfinity;

            if (settings.EnableMountainEntrances)
                for (int i=0;i<8;i++)
                {
                    int x=start+Mathf.Clamp((int)((i+Hash01(region,i,1703))*size/8f),0,size-1);
                    SurfaceCaveEntranceProfile profile=Profile(x);
                    if (!profile.Enabled || !profile.Mountain || profile.Multiplier<=0f || profile.Chance==0f)
                        continue;
                    const int span=8;
                    int left=Height(x-span),right=Height(x+span);
                    float slope=Mathf.Abs(right-left)/(span*2f);
                    if (slope<Mathf.Max(.05f,settings.MountainEntranceMinSlope)) continue;
                    float score=slope+Hash01(region,i,1707)*.15f;
                    if (score<=best) continue;
                    best=score; entranceX=x; selected=profile; mountain=true;
                    side=right>=left ? 1 : -1;
                }
            if (!selected.Enabled || selected.Multiplier<=0f || (selected.Mountain && !mountain))
                return null;
            float chance=selected.Chance>=0f ? selected.Chance :
                mountain ? settings.MountainEntranceChance : settings.SurfaceEntranceChance;
            chance=Mathf.Clamp01(chance*selected.Multiplier);
            if (Hash01(region,0,1711)>=chance) return null;

            float radius=Mathf.Clamp(settings.EntranceRadius,1.75f,6f);
            float endRadius=Mathf.Clamp(settings.EntranceRadiusBottom,1.75f,6f);
            int length=Mathf.Clamp(mountain ? settings.MountainEntranceLength : settings.EntranceLength,24,160);
            Vector2 mouth=new Vector2(entranceX+.5f,Height(entranceX)+.7f);
            if (mountain)
                nodes.Add(new Node(mouth+new Vector2(-side*(radius+1.5f),radius*.45f),radius));
            else nodes.Add(new Node(mouth+Vector2.up*1.5f,radius));
            nodes.Add(new Node(mouth,radius));

            Vector2 current=mouth;
            bool connected=false;
            float phase=Hash01(region,0,1713)*Mathf.PI*2f;
            for (int i=0;i<length;i++)
            {
                float t=i/(float)Mathf.Max(1,length-1);
                float wiggle=Mathf.Sin(phase+i*.19f)*Mathf.Clamp(settings.EntranceWander,0f,45f)*.3f;
                float angle=mountain
                    ? Mathf.Lerp(-4f,-48f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01(i/20f)))+wiggle*.55f
                    : -90f+wiggle;
                float radians=angle*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(radians)*(mountain ? side : 1),Mathf.Sin(radians));
                current+=direction*1.15f;
                float localRadius=Mathf.Lerp(radius,endRadius,t)*(.96f+.04f*Mathf.Sin(phase+i*.27f));
                if (i>12)
                    current.y=Mathf.Min(current.y,Height(Mathf.FloorToInt(current.x))-localRadius-2f);
                nodes.Add(new Node(current,localRadius));
                if (i>=16 && IsConnectedCave(current)) { connected=true; break; }
            }

            if (!connected)
            {
                if (!FindConnection(current,region,out Vector2 target)) return null;
                nodes.Add(new Node(target,endRadius));
            }
            Entrance result=new Entrance();
            for (int i=1;i<nodes.Count;i++) Stamp(result,nodes[i-1],nodes[i],region);
            return result.Cells.Count>0 ? result : null;
        }

        private bool IsConnectedCave(Vector2 point)
        {
            int x=Mathf.FloorToInt(point.x),y=Mathf.FloorToInt(point.y);
            int surface=Height(x);
            if (surface-y<Mathf.Max(1,settings.StartDepth)) return false;
            if (!naturalCave(x,y,surface)) return false;
            return naturalCave(x,y-1,surface) ||
                naturalCave(x+1,y,Height(x+1)) || naturalCave(x-1,y,Height(x-1));
        }

        private bool FindConnection(Vector2 from,int region,out Vector2 target)
        {
            float phase=Hash01(region,0,1717)*.4f;
            for (int ring=4;ring<=32;ring+=4)
                for (int i=0;i<12;i++)
                {
                    float angle=phase+i*(Mathf.PI*2f/12f);
                    Vector2 candidate=from+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*ring;
                    int x=Mathf.FloorToInt(candidate.x);
                    if (Height(x)-candidate.y<Mathf.Max(12,settings.StartDepth+4)) continue;
                    if (!IsConnectedCave(candidate)) continue;
                    target=candidate; return true;
                }
            target=default; return false;
        }

        private void Stamp(Entrance entrance,Node a,Node b,int region)
        {
            float maxRadius=Mathf.Max(a.Radius,b.Radius)+.2f;
            int minX=Mathf.FloorToInt(Mathf.Min(a.Position.x,b.Position.x)-maxRadius);
            int maxX=Mathf.CeilToInt(Mathf.Max(a.Position.x,b.Position.x)+maxRadius);
            int minY=Mathf.FloorToInt(Mathf.Min(a.Position.y,b.Position.y)-maxRadius);
            int maxY=Mathf.CeilToInt(Mathf.Max(a.Position.y,b.Position.y)+maxRadius);
            Vector2 segment=b.Position-a.Position;
            float denominator=Mathf.Max(.00001f,segment.sqrMagnitude);
            for (int x=minX;x<=maxX;x++)
                for (int y=minY;y<=maxY;y++)
                {
                    Vector2 point=new Vector2(x+.5f,y+.5f),delta=point-a.Position;
                    float t=Mathf.Clamp01((delta.x*segment.x+delta.y*segment.y)/denominator);
                    float radius=Mathf.Lerp(a.Radius,b.Radius,t)+(Hash01(x,y,region^1721)-.5f)*.22f;
                    if ((point-(a.Position+segment*t)).sqrMagnitude>radius*radius) continue;
                    entrance.Cells.Add(Pack(x,y));
                    entrance.MinX=Mathf.Min(entrance.MinX,x); entrance.MaxX=Mathf.Max(entrance.MaxX,x);
                    entrance.MinY=Mathf.Min(entrance.MinY,y); entrance.MaxY=Mathf.Max(entrance.MaxY,y);
                }
        }

        private float Hash01(int x,int y,int salt)
        {
            unchecked
            {
                uint hash=(uint)seed;
                hash^=(uint)x*374761393u; hash^=(uint)y*668265263u; hash^=(uint)salt*2246822519u;
                hash^=hash>>13; hash*=1274126177u; hash^=hash>>16;
                return (hash&0x00ffffffu)/16777216f;
            }
        }
        private static long Pack(int x,int y) { return ((long)x<<32)^(uint)y; }
        private static int FloorDiv(int value,int divisor)
        {
            int result=value/divisor;
            return value<0 && value%divisor!=0 ? result-1 : result;
        }
    }
}

