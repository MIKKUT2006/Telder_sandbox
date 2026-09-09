using System.Collections.Generic;
using UnityEngine;
using Game.World.Generation;
using Game.World.Rendering;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class ParallaxTerrainSystem
    {
        private sealed class LayerSettings
        {
            public string Name;
            public float FactorX;
            public float FactorY;
            public float Scale;
            public float HeightAmplitude;
            public float NoiseScale;
            public float DetailAmplitude;
            public float DetailScale;
            public int Radius;
            public int SortingOrder;
            public Color Tint;
        }

        private sealed class ChunkVisual
        {
            public GameObject Root;
            public Texture2D Texture;
            public Sprite Sprite;
        }

        private sealed class LayerRuntime
        {
            public LayerSettings Settings;
            public Transform Root;
            public Material Material;
            public Dictionary<int, ChunkVisual> Chunks = new Dictionary<int, ChunkVisual>();
            public int LastCenter = int.MinValue;
        }

        private const int ChunkWidthBlocks = 20;
        private const int TextureHeightBlocks = 96;
        private const int TextureBottomOffset = -42;

        private readonly Camera camera;
        private readonly WorldSettings worldSettings;
        private readonly WorldGenerator generator;
        private readonly ParallaxBiomeSampler biomeSampler;
        private readonly Vector3 initialCameraPosition;
        private readonly int dimensionSeed;
        private readonly Transform root;
        private readonly List<LayerRuntime> layers = new List<LayerRuntime>();
        private readonly Shader shader;

        public ParallaxTerrainSystem(Transform parent, Camera camera, WorldSettings settings, WorldGenerator generator)
        {
            this.camera = camera;
            worldSettings = settings;
            this.generator = generator;
            biomeSampler = new ParallaxBiomeSampler(generator);
            initialCameraPosition = camera.transform.position;
            dimensionSeed = DimensionTravelRuntime.Current != null ? DimensionTravelRuntime.Current.Seed : settings.Seed;

            GameObject rootObject = new GameObject("ProceduralBlockLandscape");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;

            shader = Shader.Find("Game/ParallaxBlockUnlit");
            if (shader == null)
            {
                Debug.LogError("PARALLAX: Game/ParallaxBlockUnlit shader not found.");
                return;
            }

            AddLayer("Far", 0.16f, 0.08f, 0.52f, 45f, 0.0022f, 11f, 0.013f, 2, -58, new Color(0.31f,0.34f,0.40f,0.72f));
            AddLayer("Mid", 0.36f, 0.15f, 0.68f, 31f, 0.0035f, 8f, 0.018f, 3, -42, new Color(0.46f,0.49f,0.56f,0.80f));
            AddLayer("Near",0.68f, 0.28f, 0.84f, 20f, 0.0058f, 5f, 0.027f, 3, -26, new Color(0.61f,0.63f,0.68f,0.88f));
        }

        private void AddLayer(string name,float fx,float fy,float scale,float amp,float nScale,float detailAmp,float detailScale,int radius,int order,Color tint)
        {
            LayerSettings s = new LayerSettings();
            s.Name=name; s.FactorX=fx; s.FactorY=fy; s.Scale=scale; s.HeightAmplitude=amp; s.NoiseScale=nScale;
            s.DetailAmplitude=detailAmp; s.DetailScale=detailScale; s.Radius=radius; s.SortingOrder=order; s.Tint=tint;

            GameObject go = new GameObject("Terrain_" + name);
            go.transform.SetParent(root, false);

            Material mat = new Material(shader);
            mat.name = "Parallax Terrain " + name;
            mat.SetColor("_Tint", tint);

            LayerRuntime r = new LayerRuntime();
            r.Settings=s; r.Root=go.transform; r.Material=mat;
            layers.Add(r);
        }

        public void Tick()
        {
            for (int i=0;i<layers.Count;i++)
                TickLayer(layers[i]);
        }

        private void TickLayer(LayerRuntime layer)
        {
            Vector3 current = camera.transform.position;
            Vector3 delta = current - initialCameraPosition;
            LayerSettings s = layer.Settings;

            layer.Root.position = new Vector3(delta.x*(1f-s.FactorX), delta.y*(1f-s.FactorY), 0f);

            float virtualCenterX = (initialCameraPosition.x + delta.x*s.FactorX) / Mathf.Max(0.01f,s.Scale);
            int center = FloorDiv(Mathf.FloorToInt(virtualCenterX), ChunkWidthBlocks);

            if (center != layer.LastCenter)
            {
                layer.LastCenter = center;
                UnloadFar(layer, center);
            }

            // Max one new chunk per layer per frame.
            for (int d=0; d<=s.Radius; d++)
            {
                int left=center-d;
                if (!layer.Chunks.ContainsKey(left)) { CreateChunk(layer,left); return; }
                if (d==0) continue;
                int right=center+d;
                if (!layer.Chunks.ContainsKey(right)) { CreateChunk(layer,right); return; }
            }
        }

        private void CreateChunk(LayerRuntime layer, int chunkIndex)
        {
            int pixelSize = BlockRenderer.BlockPixelSize;
            int textureWidth = ChunkWidthBlocks * pixelSize;
            int textureHeight = TextureHeightBlocks * pixelSize;

            Texture2D tex = new Texture2D(textureWidth,textureHeight,TextureFormat.RGBA32,false);
            tex.name = "Parallax_"+layer.Settings.Name+"_"+chunkIndex;
            tex.filterMode=FilterMode.Point;
            tex.wrapMode=TextureWrapMode.Clamp;
            tex.SetPixels32(new Color32[textureWidth*textureHeight]);

            int startX=chunkIndex*ChunkWidthBlocks;
            int bottomY=Mathf.RoundToInt(worldSettings.SurfaceHeight)+TextureBottomOffset;

            for (int lx=0; lx<ChunkWidthBlocks; lx++)
            {
                int x=startX+lx;
                ParallaxBiomeSampler.Palette palette=biomeSampler.GetPalette(x);
                int surface=GenerateSurface(layer.Settings,x,palette);

                for (int ly=0; ly<TextureHeightBlocks; ly++)
                {
                    int y=bottomY+ly;
                    ushort block=GetBlock(y,surface,palette);
                    if (block!=0)
                        BlockRenderer.DrawBlock(tex,lx,ly,block);
                }
            }

            tex.Apply(false,false);
            Sprite sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.zero,pixelSize);

            GameObject go=new GameObject("ParallaxChunk_"+chunkIndex);
            go.transform.SetParent(layer.Root,false);
            float scale=layer.Settings.Scale;
            float anchor=(1f-scale)*worldSettings.SurfaceHeight;
            go.transform.localPosition=new Vector3(startX*scale,bottomY*scale+anchor,0f);
            go.transform.localScale=new Vector3(scale,scale,1f);

            SpriteRenderer sr=go.AddComponent<SpriteRenderer>();
            sr.sprite=sprite;
            sr.sortingOrder=layer.Settings.SortingOrder;
            sr.sharedMaterial=layer.Material;

            ChunkVisual cv=new ChunkVisual();
            cv.Root=go; cv.Texture=tex; cv.Sprite=sprite;
            layer.Chunks.Add(chunkIndex,cv);
        }

        private int GenerateSurface(LayerSettings layer,int x,ParallaxBiomeSampler.Palette p)
        {
            float salt=layer.SortingOrder*17.371f;
            float warpedX=x;

            if (Mathf.Abs(p.DistortionStrength)>0.001f)
            {
                float n=Mathf.PerlinNoise((x+dimensionSeed*0.3187f+salt)*Mathf.Max(0.000001f,p.DistortionScale),71.37f+salt);
                warpedX+=(n-0.5f)*2f*p.DistortionStrength*(1f+(1f-layer.FactorX));
            }

            float macro=Mathf.PerlinNoise((warpedX+dimensionSeed*0.14731f+salt*13f)*layer.NoiseScale,17.17f+salt);
            float detail=Mathf.PerlinNoise((warpedX-dimensionSeed*0.2711f+salt*41f)*layer.DetailScale,93.71f-salt);

            float h=worldSettings.SurfaceHeight+p.HeightOffset*0.45f;
            h+=(macro-0.5f)*2f*layer.HeightAmplitude*Mathf.Max(0.35f,p.HillHeightMultiplier);
            h+=(detail-0.5f)*2f*layer.DetailAmplitude*Mathf.Max(0.4f,p.TerrainVariationMultiplier);

            if (Mathf.Abs(p.RidgeStrength)>0.001f)
            {
                float rn=Mathf.PerlinNoise((warpedX+dimensionSeed*0.8917f+salt)*Mathf.Max(0.000001f,p.RidgeScale),141.3f);
                float ridge=1f-Mathf.Abs(rn*2f-1f); ridge*=ridge;
                h+=(ridge-0.38f)*p.RidgeStrength*(1.15f+(1f-layer.FactorX));
            }

            if (Mathf.Abs(p.WaveStrength)>0.001f)
            {
                float wave=Mathf.Sin(warpedX*p.WaveScale*(0.7f+layer.FactorX)+salt);
                wave=Mathf.Sign(wave)*Mathf.Pow(Mathf.Abs(wave),1.5f);
                h+=wave*p.WaveStrength;
            }

            return Mathf.RoundToInt(h);
        }

        private ushort GetBlock(int y,int surface,ParallaxBiomeSampler.Palette p)
        {
            if (y>surface) return 0;
            if (y==surface) return p.Top;
            int depth=surface-y;
            if (depth<=Mathf.Max(1,p.SoilDepth)) return p.Soil;
            return p.Stone;
        }

        private void UnloadFar(LayerRuntime layer,int center)
        {
            List<int> remove=null;
            foreach (KeyValuePair<int,ChunkVisual> pair in layer.Chunks)
            {
                if (Mathf.Abs(pair.Key-center)<=layer.Settings.Radius+2) continue;
                if (remove==null) remove=new List<int>();
                remove.Add(pair.Key);
            }
            if (remove==null) return;
            for (int i=0;i<remove.Count;i++)
            {
                int key=remove[i];
                DestroyChunk(layer.Chunks[key]);
                layer.Chunks.Remove(key);
            }
        }

        private void DestroyChunk(ChunkVisual c)
        {
            if (c==null) return;
            if (c.Root!=null) Object.Destroy(c.Root);
            if (c.Sprite!=null) Object.Destroy(c.Sprite);
            if (c.Texture!=null) Object.Destroy(c.Texture);
        }

        public void Dispose()
        {
            for (int i=0;i<layers.Count;i++)
            {
                LayerRuntime layer=layers[i];
                foreach (KeyValuePair<int,ChunkVisual> pair in layer.Chunks) DestroyChunk(pair.Value);
                layer.Chunks.Clear();
                if (layer.Material!=null) Object.Destroy(layer.Material);
                if (layer.Root!=null) Object.Destroy(layer.Root.gameObject);
            }
            layers.Clear();
            if (root!=null) Object.Destroy(root.gameObject);
        }

        private int FloorDiv(int value,int divisor)
        {
            int result=value/divisor;
            int remainder=value%divisor;
            if (remainder!=0 && value<0) result--;
            return result;
        }
    }
}
