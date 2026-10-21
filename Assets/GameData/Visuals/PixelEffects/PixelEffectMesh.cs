using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Visuals.PixelEffects
{
    // One draw buffer; no GameObject or material per particle/pixel.
    public sealed class PixelEffectMesh : IDisposable
    {
        public readonly GameObject Root;
        public readonly MeshRenderer Renderer;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly List<Vector3> vertices = new List<Vector3>(4096);
        private readonly List<Color32> colors = new List<Color32>(4096);
        private readonly List<int> indices = new List<int>(6144);

        public PixelEffectMesh(string name, Transform parent, int order)
        {
            Root = new GameObject(name);
            Root.transform.SetParent(parent, false);
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            Root.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer = Root.AddComponent<MeshRenderer>();
            Renderer.sortingOrder = order;
            Shader shader = UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelEffect");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader) { name = name + " Material" };
                Renderer.sharedMaterial = material;
            }
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
        }

        public void Begin() { vertices.Clear(); colors.Clear(); indices.Clear(); }

        public void Quad(float x, float y, float width, float height, Color32 color)
        {
            if (vertices.Count > 64000 || color.a == 0) return;
            int start = vertices.Count;
            vertices.Add(new Vector3(x, y, -0.3f));
            vertices.Add(new Vector3(x + width, y, -0.3f));
            vertices.Add(new Vector3(x + width, y + height, -0.3f));
            vertices.Add(new Vector3(x, y + height, -0.3f));
            for (int i = 0; i < 4; i++) colors.Add(color);
            indices.Add(start); indices.Add(start + 2); indices.Add(start + 1);
            indices.Add(start); indices.Add(start + 3); indices.Add(start + 2);
        }

        public void Upload()
        {
            mesh.Clear(false);
            Renderer.enabled = vertices.Count > 0;
            if (vertices.Count == 0) return;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root);
            if (mesh != null) UnityEngine.Object.Destroy(mesh);
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}

