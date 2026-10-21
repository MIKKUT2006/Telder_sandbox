using System;
using System.Collections.Generic;
using Game.Blocks;
using Game.BlockTransforms;
using Game.Combat;
using Game.GameplaySystems.Multiblock;
using Game.World;
using Game.World.Furniture;
using Game.World.Rendering;
using UnityEngine;
using GameWorld = Game.World.World;

namespace Game.Visuals.PixelEffects
{
    public sealed class PixelFireParticles : IDisposable
    {
        private enum Kind { Torch, Campfire, Furnace }
        private sealed class Emitter
        {
            public Vector3Int Key;
            public BlockDefinition Definition;
            public Kind Kind;
            public Vector2 Position;
            public float NextSpark, NextSmoke;
            public int Scan;
        }
        private struct Particle
        {
            public Vector2 Position, Velocity;
            public float Age, Life, Size, Seed;
            public bool Smoke;
            public Color Color;
        }

        private readonly Dictionary<Vector3Int,Emitter> emitters = new Dictionary<Vector3Int,Emitter>();
        private readonly List<Vector3Int> remove = new List<Vector3Int>(96);
        private readonly List<Particle> particles = new List<Particle>(600);
        private readonly PixelEffectMesh sparks, smoke;
        private GameWorld world;
        private PixelWorldOcclusion occlusion;
        private int minX,minY,width,height,cursor,scan;
        private bool scanning;
        private float nextScan, nextHeldResolve, nextHeldSpark, nextHeldSmoke;
        private PlayerInventoryWeaponBridge inventory;
        private PlayerWeaponRigController playerRig;

        public PixelFireParticles(Transform parent)
        {
            smoke = new PixelEffectMesh("Pixel Fire Smoke",parent,3);
            sparks = new PixelEffectMesh("Pixel Fire Embers",parent,6);
        }

        public void Bind(GameWorld value, PixelWorldOcclusion sampler)
        {
            world=value; occlusion=sampler;
            Clear(); inventory=null; playerRig=null; nextHeldResolve=0f;
        }

        public void Clear()
        {
            particles.Clear(); emitters.Clear(); scanning=false; nextScan=0f;
            smoke.Begin(); smoke.Upload(); sparks.Begin(); sparks.Upload();
        }

        public void Tick(Camera camera, int particleLimit, int pixelDensity)
        {
            if (world==null || camera==null) return;
            Scan(camera);
            foreach (Emitter emitter in emitters.Values)
            {
                if (!IsActive(emitter)) continue;
                float now=Time.time;
                if (now>=emitter.NextSpark)
                {
                    emitter.NextSpark=now+UnityEngine.Random.Range(.75f,1.3f)/SparkRate(emitter.Kind);
                    Spawn(emitter.Position,false,emitter.Kind,particleLimit);
                }
                if (now>=emitter.NextSmoke)
                {
                    float rate=emitter.Kind==Kind.Torch ? 1.1f : emitter.Kind==Kind.Campfire ? 4f : 2.7f;
                    emitter.NextSmoke=now+UnityEngine.Random.Range(.8f,1.2f)/rate;
                    Spawn(emitter.Position,true,emitter.Kind,particleLimit);
                }
            }
            TickHeldTorch(particleLimit);
            UpdateParticles(Mathf.Min(Time.deltaTime,.05f),Mathf.Clamp(pixelDensity,8,32));
        }

        private static float SparkRate(Kind kind)
        { return kind==Kind.Campfire ? 15f : kind==Kind.Furnace ? 7f : 5f; }

        private void Scan(Camera camera)
        {
            if (!scanning)
            {
                if (Time.time<nextScan) return;
                float hh=camera.orthographicSize+2f, hw=hh*camera.aspect+2f;
                width=Mathf.Clamp(Mathf.CeilToInt(hw*2),1,192);
                height=Mathf.Clamp(Mathf.CeilToInt(hh*2),1,128);
                minX=Mathf.FloorToInt(camera.transform.position.x-width*.5f);
                minY=Mathf.FloorToInt(camera.transform.position.y-height*.5f);
                cursor=0; scan++; scanning=true;
            }
            FurnitureLayerManager furniture=FurnitureLayerManager.Instance;
            int end=Mathf.Min(cursor+384,width*height);
            for (;cursor<end;cursor++)
            {
                int x=minX+cursor%width, y=minY+cursor/width;
                if (!world.IsLoaded(x,y)) continue;
                BlockDefinition foreground=occlusion.Definition(world.GetBlock(x,y));
                Visit(x,y,foreground,BlockVisualLayer.Foreground);
                if (furniture!=null && furniture.TryGetDefinition(x,y,out BlockDefinition definition))
                    Visit(x,y,definition,BlockVisualLayer.Furniture);
            }
            if (cursor<width*height) return;
            remove.Clear();
            foreach (KeyValuePair<Vector3Int,Emitter> pair in emitters)
                if (pair.Value.Scan!=scan) remove.Add(pair.Key);
            for (int i=0;i<remove.Count;i++) emitters.Remove(remove[i]);
            scanning=false; nextScan=Time.time+.25f;
        }

        private void Visit(int x,int y,BlockDefinition definition,BlockVisualLayer layer)
        {
            if (definition==null || !TryKind(definition,out Kind kind)) return;
            if (kind==Kind.Furnace)
            {
                MultiBlockRecord record=MultiBlockStore.FindCell(x,y);
                if (record!=null && (record.X!=x || record.Y!=y)) return;
            }
            Vector3Int key=new Vector3Int(x,y,(int)layer);
            if (!emitters.TryGetValue(key,out Emitter emitter))
            {
                if (emitters.Count>=96) return;
                emitter=new Emitter { Key=key, NextSpark=Time.time+UnityEngine.Random.value*.2f,
                    NextSmoke=Time.time+UnityEngine.Random.value*.4f };
                emitters.Add(key,emitter);
            }
            emitter.Kind=kind; emitter.Definition=definition; emitter.Scan=scan;
            emitter.Position=Position(x,y,definition,kind,layer);
        }

        private static bool TryKind(BlockDefinition definition,out Kind kind)
        {
            string id=definition.ID ?? "";
            if (PixelWorldOcclusion.HasTag(definition,"furnace") || id.EndsWith(":furnace",StringComparison.OrdinalIgnoreCase))
            { kind=Kind.Furnace; return true; }
            if (PixelWorldOcclusion.HasTag(definition,"campfire") || id.EndsWith(":campfire",StringComparison.OrdinalIgnoreCase))
            { kind=Kind.Campfire; return true; }
            kind=Kind.Torch;
            return PixelWorldOcclusion.HasTag(definition,"torch") ||
                PixelWorldOcclusion.HasTag(definition,"fire") || id.EndsWith(":torch",StringComparison.OrdinalIgnoreCase);
        }

        private bool IsActive(Emitter emitter)
        {
            int x=emitter.Key.x,y=emitter.Key.y;
            if (!world.IsLoaded(x,y)) return false;
            BlockVisualLayer layer=(BlockVisualLayer)emitter.Key.z;
            BlockDefinition current=null;
            if (layer==BlockVisualLayer.Furniture)
            {
                FurnitureLayerManager furniture=FurnitureLayerManager.Instance;
                if (furniture!=null) furniture.TryGetDefinition(x,y,out current);
            }
            else current=occlusion.Definition(world.GetBlock(x,y));
            if (current==null || !string.Equals(current.ID,emitter.Definition.ID,StringComparison.OrdinalIgnoreCase))
                return false;
            string state=BlockVisualStateRuntime.GetState(x,y,layer);
            if (emitter.Kind==Kind.Furnace) return string.Equals(state,"burning",StringComparison.OrdinalIgnoreCase);
            return !string.Equals(state,"off",StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(state,"unlit",StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(state,"extinguished",StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 Position(int x,int y,BlockDefinition definition,Kind kind,BlockVisualLayer layer)
        {
            Vector2 point=kind==Kind.Torch ? new Vector2(.5f,.82f) : new Vector2(.5f,.48f);
            if (kind==Kind.Furnace)
            {
                int w=definition.MultiBlock!=null ? Mathf.Max(1,definition.MultiBlock.Width) : 1;
                int h=definition.MultiBlock!=null ? Mathf.Max(1,definition.MultiBlock.Height) : 1;
                int ax=definition.MultiBlock!=null ? definition.MultiBlock.AnchorX : 0;
                int ay=definition.MultiBlock!=null ? definition.MultiBlock.AnchorY : 0;
                point=new Vector2(w*.62f-ax,h+.025f-ay);
            }
            if (layer==BlockVisualLayer.Furniture)
            {
                FurnitureLayerManager manager=FurnitureLayerManager.Instance;
                byte state=manager!=null ? manager.GetFurnitureTransform(x,y) : (byte)0;
                Vector2 delta=(point-Vector2.one*.5f)*Mathf.Max(.01f,definition.VisualScale);
                if ((state&4)!=0) delta.x=-delta.x;
                delta=(Vector2)(Quaternion.Euler(0,0,-(state&3)*90f)*(Vector3)delta);
                point=Vector2.one*.5f+delta+new Vector2(definition.VisualOffsetX,definition.VisualOffsetY);
            }
            return new Vector2(x,y)+point;
        }

        private void TickHeldTorch(int limit)
        {
            if (Time.time>=nextHeldResolve)
            {
                nextHeldResolve=Time.time+.5f;
                if (inventory==null) inventory=UnityEngine.Object.FindObjectOfType<PlayerInventoryWeaponBridge>();
                if (playerRig==null && inventory!=null) playerRig=inventory.GetComponent<PlayerWeaponRigController>();
            }
            if (inventory==null || playerRig==null) return;
            string selected=inventory.GetSelectedItemId();
            if (string.IsNullOrEmpty(selected) || !selected.EndsWith(":torch",StringComparison.OrdinalIgnoreCase)) return;
            Transform hand=playerRig.GetRuntimeHandPoint();
            if (hand==null) return;
            Vector2 at=(Vector2)hand.position+(Vector2)hand.up*.35f;
            if (Time.time>=nextHeldSpark)
            { nextHeldSpark=Time.time+.17f; Spawn(at,false,Kind.Torch,limit); }
            if (Time.time>=nextHeldSmoke)
            { nextHeldSmoke=Time.time+.9f; Spawn(at,true,Kind.Torch,limit); }
        }

        private void Spawn(Vector2 source,bool isSmoke,Kind kind,int limit)
        {
            if (particles.Count>=Mathf.Clamp(limit,32,1500)) return;
            float width=kind==Kind.Campfire ? .22f : .08f;
            float seed=UnityEngine.Random.Range(0f,100f);
            Particle p=new Particle
            {
                Position=source+new Vector2(UnityEngine.Random.Range(-width,width),UnityEngine.Random.Range(0,.08f)),
                Velocity=new Vector2(UnityEngine.Random.Range(-.17f,.17f),UnityEngine.Random.Range(.5f,1.2f)),
                Life=isSmoke ? UnityEngine.Random.Range(1.1f,2.4f) : UnityEngine.Random.Range(.38f,.95f),
                Size=isSmoke ? UnityEngine.Random.Range(.07f,.13f) : UnityEngine.Random.Range(.035f,.085f),
                Smoke=isSmoke, Seed=seed,
                Color=isSmoke ? new Color(.46f,.46f,.50f,.38f) : new Color(1f,.78f,.30f,1f)
            };
            if (kind==Kind.Campfire) { p.Velocity.y*=1.3f; p.Life*=1.2f; }
            particles.Add(p);
        }

        private void UpdateParticles(float dt,int ppb)
        {
            sparks.Begin(); smoke.Begin();
            float size=1f/ppb;
            for (int i=particles.Count-1;i>=0;i--)
            {
                Particle p=particles[i];
                p.Age+=dt;
                if (p.Age>=p.Life)
                { particles[i]=particles[particles.Count-1]; particles.RemoveAt(particles.Count-1); continue; }
                Vector2 previous=p.Position;
                p.Position+=p.Velocity*dt;
                p.Position.x+=Mathf.Sin(p.Age*3f+p.Seed)*dt*(p.Smoke ? .12f : .055f);
                // A roof arrests rising particles instead of allowing smoke through the floor above.
                BlockDefinition roof=occlusion.Definition(world.GetBlock(
                    Mathf.FloorToInt(p.Position.x),Mathf.FloorToInt(p.Position.y)));
                if (roof!=null && roof.Solid && roof.BlocksLight && !PixelWorldOcclusion.HasTag(roof,"leaves"))
                {
                    p.Position=previous;
                    p.Velocity.y=0f;
                    p.Age+=dt*4f;
                }
                float age=Mathf.Clamp01(p.Age/p.Life);
                float s=Mathf.Max(1,Mathf.RoundToInt(p.Size*(p.Smoke ? 1f+age*1.5f : 1f-age*.65f)*ppb))*size;
                Color color=p.Smoke ? p.Color : Color.Lerp(p.Color,new Color(1f,.18f,.025f,1f),age);
                color.a*=Mathf.Ceil((1f-age)*5f)/5f;
                float x=Mathf.Floor(p.Position.x*ppb)*size;
                float y=Mathf.Floor(p.Position.y*ppb)*size;
                if (p.Smoke) smoke.Quad(x,y,s,s,color);
                else sparks.Quad(x,y,s,Mathf.Max(size,s*2f),color);
                particles[i]=p;
            }
            sparks.Upload(); smoke.Upload();
        }

        public void Dispose() { sparks.Dispose(); smoke.Dispose(); }
    }
}

