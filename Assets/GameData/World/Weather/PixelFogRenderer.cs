using System.Collections.Generic;
using UnityEngine;
using Game.World.Generation;

namespace Game.World.Weather
{
    // Independent one-block columns cannot interpolate fog down through mountain slopes.
    public sealed class PixelFogRenderer : MonoBehaviour
    {
        [SerializeField] private float fogHeight=8f;
        private Camera targetCamera;
        private WorldGenerator generator;
        private GameObject root;
        private Mesh mesh;
        private Material material;
        private MeshRenderer meshRenderer;
        private bool activeFog;
        private float nextRebuild;
        private readonly List<Vector3> vertices=new List<Vector3>(1024);
        private readonly List<Vector2> uv=new List<Vector2>(1024);
        private readonly List<int> triangles=new List<int>(1536);

        public void Initialize(Camera camera,WorldGenerator worldGenerator)
        {
            targetCamera=camera; generator=worldGenerator; nextRebuild=0;
            if (meshRenderer!=null) return;
            Material template=UnityEngine.Resources.Load<Material>("PixelFX/PixelFog");
            Shader shader=template!=null ? template.shader : Shader.Find("Game/TelderPixelFog");
            if (shader==null) { Debug.LogWarning("FOG: Game/TelderPixelFog shader unavailable"); return; }
            // Mesh vertices are absolute world positions, independent of the weather host/camera.
            root=new GameObject("[Runtime] Surface Pixel Fog");
            mesh=new Mesh { name="Surface Pixel Fog Columns" }; mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            meshRenderer=root.AddComponent<MeshRenderer>();
            material=template!=null ? new Material(template) : new Material(shader);
            material.SetFloat("_PixelSize",.25f); material.SetFloat("_Speed",.15f);
            meshRenderer.sharedMaterial=material;
            meshRenderer.sortingLayerName="Default";
            meshRenderer.sortingOrder=1;
            root.SetActive(activeFog);
        }

        public void SetFog(bool enabled,Color color,float intensity)
        {
            if (enabled && !activeFog) nextRebuild=0;
            activeFog=enabled;
            if (root!=null) root.SetActive(enabled);
            if (material!=null)
            {
                material.SetColor("_FogColor",color);
                material.SetFloat("_Intensity",Mathf.Clamp(intensity,0,1.5f));
            }
        }

        private void LateUpdate()
        {
            if (!activeFog || mesh==null || generator==null || targetCamera==null || !targetCamera.orthographic) return;
            if (Time.unscaledTime<nextRebuild) return;
            nextRebuild=Time.unscaledTime+.35f;
            float halfWidth=targetCamera.orthographicSize*targetCamera.aspect;
            int left=Mathf.FloorToInt(targetCamera.transform.position.x-halfWidth-3);
            int right=Mathf.CeilToInt(targetCamera.transform.position.x+halfWidth+3);
            vertices.Clear(); uv.Clear(); triangles.Clear();
            var manager=WorldManager.Instance;
            var world=manager!=null ? manager.GetWorld() : null;
            for (int x=left;x<right;x++)
            {
                float ground=ResolveSurfaceHeight(x,world);
                float bottom=targetCamera.transform.position.y-targetCamera.orthographicSize;
                float top=targetCamera.transform.position.y+targetCamera.orthographicSize;
                if (ground>top+1 || ground+fogHeight<bottom) continue;
                int k=vertices.Count;
                vertices.Add(new Vector3(x,ground,0)); vertices.Add(new Vector3(x+1,ground,0));
                vertices.Add(new Vector3(x+1,ground+fogHeight,0)); vertices.Add(new Vector3(x,ground+fogHeight,0));
                uv.Add(new Vector2(0,0)); uv.Add(new Vector2(1,0)); uv.Add(new Vector2(1,1)); uv.Add(new Vector2(0,1));
                triangles.Add(k); triangles.Add(k+2); triangles.Add(k+1);
                triangles.Add(k); triangles.Add(k+3); triangles.Add(k+2);
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
        }

        private float ResolveSurfaceHeight(int x,Game.World.World world)
        {
            int estimate=generator.GetSurfaceHeight(x);
            if (world==null) return estimate+1;
            for (int y=estimate+48;y>=estimate-20;y--)
            {
                if (!world.IsLoaded(x,y)) continue;
                ushort id=world.GetBlock(x,y);
                var definition=world.GetBlockDefinition(id);
                if (id==0 || definition==null || !definition.Solid) continue;
                // Leaves, trunks and furniture do not lift the fog onto tree crowns.
                if (!generator.IsNaturalSurfaceTerrainBlock(x,id)) continue;
                return y+1;
            }
            return estimate+1;
        }

        private void OnDisable() { if (root!=null) root.SetActive(false); }
        private void OnEnable() { if (root!=null) root.SetActive(activeFog); nextRebuild=0; }
        private void OnDestroy()
        {
            if (root!=null) Destroy(root);
            if (material!=null) Destroy(material);
            if (mesh!=null) Destroy(mesh);
        }
    }
}
