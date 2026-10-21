using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Resources;

namespace Game.World.Structures.EditorRuntime
{
    /// <summary>
    /// Lightweight in-game 3D structure authoring mode. It uses the same
    /// StructureDefinition JSON as the 2D editor, with GeometryMode=Voxel3D and Z/Depth fields.
    /// </summary>
    public sealed class VoxelStructureEditor3D : MonoBehaviour
    {
        private StructureDefinition data = new StructureDefinition();
        private List<StructureEditorContentScanner.BlockInfo> blocks;
        private string selectedBlockId;
        private Camera editorCamera;
        private Transform cellRoot;
        private readonly Dictionary<Vector3Int,GameObject> visuals = new Dictionary<Vector3Int,GameObject>();
        private Vector2 paletteScroll;
        private string fileName="new_3d_structure";
        private bool looking;
        private float yaw=35f,pitch=25f;

        public static void Open()
        {
            if(FindFirstObjectByType<VoxelStructureEditor3D>()!=null)return;
            new GameObject("TELDER 3D Structure Editor").AddComponent<VoxelStructureEditor3D>();
        }

        private void Start()
        {
            data.GeometryMode=StructureGeometryMode.Voxel3D;data.Width=12;data.Height=8;data.Depth=12;data.SpawnType=StructureSpawnType.Any;
            blocks=StructureEditorContentScanner.LoadBlocks();if(blocks.Count>0)selectedBlockId=blocks[0].ID;
            cellRoot=new GameObject("3D Structure Cells").transform;cellRoot.SetParent(transform,false);
            GameObject cg=new GameObject("3D Structure Editor Camera");cg.transform.SetParent(transform,false);editorCamera=cg.AddComponent<Camera>();editorCamera.transform.position=new Vector3(10,10,-14);editorCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);editorCamera.clearFlags=CameraClearFlags.SolidColor;editorCamera.backgroundColor=new Color(.035f,.04f,.055f);
            BuildFloor();
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){Destroy(gameObject);return;}
            if(Input.GetKeyDown(KeyCode.F5))Save();
            HandleCamera();
            if(!IsPointerOverPalette())HandleEditing();
        }

        private void HandleCamera()
        {
            if(Input.GetMouseButtonDown(1)){looking=true;UnityEngine.Cursor.lockState=CursorLockMode.Locked;UnityEngine.Cursor.visible=false;}
            if(Input.GetMouseButtonUp(1)){looking=false;UnityEngine.Cursor.lockState=CursorLockMode.None;UnityEngine.Cursor.visible=true;}
            if(looking){yaw+=Input.GetAxis("Mouse X")*2.5f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2.5f,-89,89);editorCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);}
            float speed=Input.GetKey(KeyCode.LeftShift)?14f:7f;Vector3 move=editorCamera.transform.right*Input.GetAxisRaw("Horizontal")+editorCamera.transform.forward*Input.GetAxisRaw("Vertical");if(Input.GetKey(KeyCode.E))move+=Vector3.up;if(Input.GetKey(KeyCode.Q))move+=Vector3.down;editorCamera.transform.position+=move.normalized*speed*Time.deltaTime;
        }

        private void HandleEditing()
        {
            if(looking)return;
            Ray ray=editorCamera.ScreenPointToRay(Input.mousePosition);RaycastHit hit;
            if(Input.GetMouseButtonDown(0)&&UnityEngine.Physics.Raycast(ray,out hit,200f)){
                Vector3 p=hit.point+hit.normal*.01f;Vector3Int cell=Vector3Int.FloorToInt(p);if(InBounds(cell))SetCell(cell,selectedBlockId);
            }
            if(Input.GetMouseButtonDown(2)&&UnityEngine.Physics.Raycast(ray,out hit,200f)){
                Vector3Int cell=Vector3Int.FloorToInt(hit.point-hit.normal*.01f);RemoveCell(cell);
            }
        }

        private bool InBounds(Vector3Int c)=>c.x>=0&&c.y>=0&&c.z>=0&&c.x<data.Width&&c.y<data.Height&&c.z<data.Depth;
        private void SetCell(Vector3Int c,string id)
        {
            RemoveCell(c);var cell=new StructureCellDefinition{X=c.x,Y=c.y,Z=c.z,ForegroundId=id};data.Cells.Add(cell);
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=id;go.transform.SetParent(cellRoot,false);go.transform.localPosition=(Vector3)c+Vector3.one*.5f;go.GetComponent<Renderer>().sharedMaterial=MakeMaterial(id);visuals[c]=go;
        }
        private void RemoveCell(Vector3Int c)
        {
            data.Cells.RemoveAll(x=>x.X==c.x&&x.Y==c.y&&x.Z==c.z);GameObject go;if(visuals.TryGetValue(c,out go)){Destroy(go);visuals.Remove(c);}
        }
        private Material MakeMaterial(string id)
        {
            string tex=null;foreach(var b in blocks)if(string.Equals(b.ID,id,StringComparison.OrdinalIgnoreCase)){tex=b.Texture;break;}Shader s=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");Material m=new Material(s);Texture2D t=TextureManager.Get(tex);if(t!=null){m.mainTexture=t;t.filterMode=FilterMode.Point;}return m;
        }
        private void BuildFloor(){GameObject f=GameObject.CreatePrimitive(PrimitiveType.Cube);f.name="Editor Floor";f.transform.SetParent(transform,false);f.transform.position=new Vector3(data.Width*.5f,-.55f,data.Depth*.5f);f.transform.localScale=new Vector3(data.Width,.1f,data.Depth);}
        private void Save(){data.ID=fileName;data.DisplayName=fileName;string path=Path.Combine(StructurePaths.Folder,fileName+".json");File.WriteAllText(path,JsonUtility.ToJson(data,true));Debug.Log("3D STRUCTURE EDITOR: saved "+path);}
        private bool IsPointerOverPalette()=>Input.mousePosition.x<300f;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0,0,300,Screen.height),GUI.skin.box);GUILayout.Label("TELDER 3D STRUCTURE EDITOR");GUILayout.Label("WASD + Q/E: полёт | RMB: камера\nLMB: поставить | MMB: удалить | F5: сохранить | ESC: выйти");
            GUILayout.Label("Файл");fileName=GUILayout.TextField(fileName);
            GUILayout.BeginHorizontal();GUILayout.Label("W",GUILayout.Width(20));int.TryParse(GUILayout.TextField(data.Width.ToString(),GUILayout.Width(45)),out data.Width);GUILayout.Label("H",GUILayout.Width(20));int.TryParse(GUILayout.TextField(data.Height.ToString(),GUILayout.Width(45)),out data.Height);GUILayout.Label("D",GUILayout.Width(20));int.TryParse(GUILayout.TextField(data.Depth.ToString(),GUILayout.Width(45)),out data.Depth);GUILayout.EndHorizontal();
            if(GUILayout.Button("Сохранить 3D структуру [F5]"))Save();GUILayout.Space(8);GUILayout.Label("Блоки");paletteScroll=GUILayout.BeginScrollView(paletteScroll);
            foreach(var b in blocks){bool active=selectedBlockId==b.ID;GUI.backgroundColor=active?new Color(.45f,.75f,1f):Color.white;if(GUILayout.Button(b.Name+"\n"+b.ID,GUILayout.Height(44)))selectedBlockId=b.ID;}GUI.backgroundColor=Color.white;GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void OnDestroy(){UnityEngine.Cursor.lockState=CursorLockMode.None;UnityEngine.Cursor.visible=true;}
    }
}
