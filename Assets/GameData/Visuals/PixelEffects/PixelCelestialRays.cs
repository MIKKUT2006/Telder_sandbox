using System;
using System.Collections.Generic;
using Game.World;
using UnityEngine;
using GameWorld = Game.World.World;

namespace Game.Visuals.PixelEffects
{
    // Polar mask: every column is a ray starting at the actual celestial sprite.
    // March each ray once, preserving holes in leaf textures and partial blocks.
    public sealed class PixelCelestialRays : IDisposable
    {
        private const float HalfFan = 1.48f;
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private readonly Material material;
        private readonly Vector3[] vertices = new Vector3[4];
        private readonly int[] triangles = { 0,2,1,0,3,2 };
        private readonly Dictionary<int,int> surfaceHeights = new Dictionary<int,int>(256);
        private GameWorld world;
        private PixelWorldOcclusion occlusion;
        private Texture2D mask;
        private Color32[] pixels;
        private int width,height,column;
        private float density,nextBuild,sourceEnergy;
        private bool building,sourceCanopy;
        private Rect buildView;
        private Vector2 buildSource;
        private Transform sun,moon;
        private SpriteRenderer sunRenderer,moonRenderer;
        private float nextCelestialSearch;

        public PixelCelestialRays(Transform parent)
        {
            root=new GameObject("Pixel Sun and Moon Rays");
            root.transform.SetParent(parent,false);
            mesh=new Mesh { name="Celestial Ray Background" };
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=root.AddComponent<MeshRenderer>();
            renderer.sortingOrder=-1;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelCelestialRays");
            if (shader!=null)
            {
                material=new Material(shader) { name="Pixel Celestial Rays" };
                renderer.sharedMaterial=material;
            }
            renderer.enabled=false;
        }

        public void Bind(GameWorld value,PixelWorldOcclusion sampler)
        {
            world=value; occlusion=sampler; Hide(); nextBuild=0;
            sun=moon=null; sunRenderer=moonRenderer=null; nextCelestialSearch=0;
            surfaceHeights.Clear();
        }
        public void Hide() { renderer.enabled=false; building=false; }

        public void Tick(Camera camera,int pixelsPerBlock,float sunlightOpacity,float moonlightOpacity,int order)
        {
            if (material==null || world==null || camera==null ||
                !TryGetSource(camera,out Vector2 source,out bool solar)) { Hide(); return; }
            Vector2 center=camera.transform.position;
            Vector2 toward=source-center;
            float elevation=toward.normalized.y;
            float horizonFade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.35f,elevation));
            if (horizonFade<=.001f) { Hide(); return; }
            Color tint=solar ? new Color(1f,.88f,.57f,sunlightOpacity) : new Color(.57f,.73f,1f,moonlightOpacity);
            tint.a*=horizonFade;
            material.SetColor("_RayColor",tint);
            // Keep the visual fan attached to the moving source even between mask uploads.
            material.SetVector("_RaySource",new Vector4(source.x,source.y,0,0));
            renderer.sortingOrder=order;
            float hh=camera.orthographicSize+2,hw=hh*camera.aspect+2;
            Rect view=Rect.MinMaxRect(center.x-hw,center.y-hh,center.x+hw,center.y+hh);
            if (building && ((source-buildSource).sqrMagnitude>4 ||
                (center-buildView.center).sqrMagnitude>16 || Mathf.Abs(view.height-buildView.height)>1))
            { Hide(); nextBuild=0; }
            if (!building)
            {
                if (Time.time<nextBuild) return;
                Begin(view,source,pixelsPerBlock);
            }
            int end=Mathf.Min(width,column+96);
            float step=1/density;
            for (;column<end;column++)
            {
                float angle=((column+.5f)/width*2-1)*HalfFan;
                Vector2 direction=new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
                float energy=sourceEnergy;
                bool canopy=sourceCanopy;
                for (int row=0;row<height;row++)
                {
                    if (energy<.008f) break;
                    Vector2 at=buildSource+direction*((row+.5f)*step);
                    if (!world.IsLoaded(Mathf.FloorToInt(at.x),Mathf.FloorToInt(at.y)))
                    {
                        if (at.y<=Surface(Mathf.FloorToInt(at.x))+1) break;
                    }
                    else
                    {
                        float transmission=occlusion.Transmission(at,out bool foliage);
                        canopy|=foliage;
                        if (transmission<=.001f) break;
                        if (transmission<.999f) energy*=Mathf.Pow(transmission,step);
                    }
                    if (canopy) energy*=Mathf.Exp(-step*.025f);
                    pixels[column+row*width]=new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(energy)*255),
                        canopy ? (byte)255 : (byte)0,0,255);
                }
            }
            if (column<width) return;
            mask.SetPixels32(pixels); mask.Apply(false,false);
            material.SetTexture("_RayMask",mask);
            material.SetVector("_RayPolar",new Vector4(HalfFan,1/(height*step),0,0));
            material.SetFloat("_PixelsPerBlock",density);
            vertices[0]=new Vector3(buildView.xMin,buildView.yMin,.2f);
            vertices[1]=new Vector3(buildView.xMax,buildView.yMin,.2f);
            vertices[2]=new Vector3(buildView.xMax,buildView.yMax,.2f);
            vertices[3]=new Vector3(buildView.xMin,buildView.yMax,.2f);
            mesh.Clear(); mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateBounds();
            renderer.enabled=true; building=false; nextBuild=Time.time+.04f;
        }

        private void Begin(Rect view,Vector2 source,int ppb)
        {
            buildView=view; buildSource=source; density=Mathf.Clamp(ppb,2,8);
            float dx=Mathf.Max(Mathf.Abs(view.xMin-source.x),Mathf.Abs(view.xMax-source.x));
            float dy=Mathf.Max(Mathf.Abs(view.yMin-source.y),Mathf.Abs(view.yMax-source.y));
            float radius=Mathf.Sqrt(dx*dx+dy*dy)+2;
            width=Mathf.Clamp(Mathf.CeilToInt(radius*density*HalfFan*2/32)*32,96,384);
            height=Mathf.Clamp(Mathf.CeilToInt(radius*density/16)*16,16,768);
            if (mask==null || mask.width!=width || mask.height!=height)
            {
                if (mask!=null) UnityEngine.Object.Destroy(mask);
                mask=new Texture2D(width,height,TextureFormat.RGBA32,false,true)
                { name="Radial Celestial Visibility",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
                pixels=new Color32[width*height];
            }
            else Array.Clear(pixels,0,pixels.Length);
            occlusion.BeginMask(); surfaceHeights.Clear();
            sourceEnergy=TraceSky(source,out sourceCanopy);
            column=0; building=true;
        }

        private int Surface(int x)
        {
            if (!surfaceHeights.TryGetValue(x,out int h))
            {
                var manager=WorldManager.Instance;
                var generator=manager!=null ? manager.GetGenerator() : null;
                h=generator!=null ? generator.GetSurfaceHeight(x) : int.MaxValue/2;
                surfaceHeights[x]=h;
            }
            return h;
        }

        private float TraceSky(Vector2 source,out bool canopy)
        {
            canopy=false; float energy=1;
            int x=Mathf.FloorToInt(source.x), surface=Surface(x);
            for (int i=0;i<=128;i++)
            {
                Vector2 at=source+Vector2.up*(i*.5f);
                int y=Mathf.FloorToInt(at.y);
                if (!world.IsLoaded(x,y)) return y>surface+28 ? energy : 0;
                float transmission=occlusion.Transmission(at,out bool foliage);
                canopy|=foliage;
                if (transmission<.001f) return 0;
                energy*=Mathf.Sqrt(transmission);
                if (energy<.008f) return 0;
            }
            return source.y+64>surface+28 ? energy : 0;
        }

        private bool TryGetSource(Camera camera,out Vector2 position,out bool solar)
        {
            position=Vector2.zero; solar=true;
            if ((sun==null || moon==null) && Time.unscaledTime>=nextCelestialSearch)
            {
                nextCelestialSearch=Time.unscaledTime+1;
                var s=GameObject.Find("Sky Sun"); var m=GameObject.Find("Sky Moon");
                sun=s!=null ? s.transform : null; moon=m!=null ? m.transform : null;
                sunRenderer=s!=null ? s.GetComponent<SpriteRenderer>() : null;
                moonRenderer=m!=null ? m.GetComponent<SpriteRenderer>() : null;
            }
            float horizon=camera.transform.position.y;
            float sy=sun!=null && sunRenderer!=null && sunRenderer.enabled ? sun.position.y : float.NegativeInfinity;
            float my=moon!=null && moonRenderer!=null && moonRenderer.enabled ? moon.position.y : float.NegativeInfinity;
            if (Mathf.Max(sy,my)<=horizon+.01f) return false;
            solar=sy>=my; position=solar ? (Vector2)sun.position : (Vector2)moon.position;
            return true;
        }

        public void Dispose()
        {
            if (root!=null) UnityEngine.Object.Destroy(root);
            if (mesh!=null) UnityEngine.Object.Destroy(mesh);
            if (mask!=null) UnityEngine.Object.Destroy(mask);
            if (material!=null) UnityEngine.Object.Destroy(material);
        }
    }
}
