using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelPortal3D : MonoBehaviour
    {
        private static VoxelPortal3D current;
        private VoxelWorld3DRuntime rt;private string destination;private float readyAt,expires;
        private Mesh mesh;private Material material;
        public static void Open(VoxelWorld3DRuntime runtime,string target)
        {
            if(current!=null)Destroy(current.gameObject);
            var go=new GameObject("Voxel dimension portal");go.transform.SetParent(runtime.transform,false);
            Vector3 forward=runtime.Motor.transform.forward,feet=runtime.Motor.transform.position;
            go.transform.position=feet+forward*3.2f+Vector3.up*1.1f;go.transform.rotation=runtime.Motor.transform.rotation;
            var portal=go.AddComponent<VoxelPortal3D>();portal.rt=runtime;portal.destination=target;portal.readyAt=Time.time+.6f;portal.expires=Time.time+60;portal.Build();current=portal;
            Game.Audio.TelderAudioSystem.Play("portal.open");
        }
        private void Build()
        {
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            for(int x=-12;x<=12;x++)for(int y=-18;y<=18;y++)
            {
                float r=new Vector2(x/12f,y/18f).magnitude;if(r>1)continue;
                int first=vertices.Count;float s=1/16f;
                vertices.Add(new Vector3(x*s,y*s,0));vertices.Add(new Vector3((x+1)*s,y*s,0));vertices.Add(new Vector3((x+1)*s,(y+1)*s,0));vertices.Add(new Vector3(x*s,(y+1)*s,0));
                Color c=r>.83f?new Color(.65f,.45f,1):new Color(.1f,.035f,.24f);for(int i=0;i<4;i++)colors.Add(c);
                indices.Add(first);indices.Add(first+1);indices.Add(first+2);indices.Add(first);indices.Add(first+2);indices.Add(first+3);
            }
            mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderPixelEffect");if(shader!=null){material=new Material(shader);gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;}
        }
        private void Update()
        {
            if(rt==null||Time.time>=expires){Destroy(gameObject);return;}
            if(Time.time>=readyAt&&!VoxelWorld3DRuntime.UIBlocked)
            {
                Vector3 local=transform.InverseTransformPoint(rt.Motor.transform.position+Vector3.up*.9f);
                if(Mathf.Abs(local.x)<.8f&&Mathf.Abs(local.y)<1.2f&&Mathf.Abs(local.z)<.45f)
                {rt.SaveState();Game.World.Dimensions.DimensionTravelRuntime.TravelTo(destination);readyAt=float.PositiveInfinity;}
            }
        }
        private void OnDestroy(){if(current==this)current=null;if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
