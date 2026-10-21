using UnityEngine;

namespace Game.World.Voxel3D
{
    // A real volume is visible from the rear; the side-view sprite rig is kept
    // solely as the shared inventory/skin source, not as a camera-facing card.
    public sealed class VoxelAvatar3D : MonoBehaviour
    {
        private Transform leftArm,rightArm,leftLeg,rightLeg;
        private MeshRenderer[] renderers;
        private readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
        private Vector3 previous;
        private MaterialPropertyBlock block;private Color[] baseColors;
        public Transform RightHand {get;private set;}
        public void Initialize(Transform source)
        {
            SpriteRenderer[] sprites=source.GetComponentsInChildren<SpriteRenderer>(true);
            Make("Head",new Vector3(0,1.57f,0),new Vector3(.48f,.48f,.4f),Find(sprites,"Head"));
            Make("Body",new Vector3(0,1.03f,0),new Vector3(.5f,.62f,.28f),Find(sprites,"Body"));
            leftArm=Make("Left arm",new Vector3(-.36f,1.05f,0),new Vector3(.19f,.7f,.22f),Find(sprites,"Arm"));
            rightArm=Make("Right arm",new Vector3(.36f,1.05f,0),new Vector3(.19f,.7f,.22f),Find(sprites,"Arm"));
            leftLeg=Make("Left leg",new Vector3(-.14f,.36f,0),new Vector3(.21f,.7f,.24f),Find(sprites,"Leg"));
            rightLeg=Make("Right leg",new Vector3(.14f,.36f,0),new Vector3(.21f,.7f,.24f),Find(sprites,"Leg"));
            var hand=new GameObject("Voxel Right Hand");hand.transform.SetParent(transform,false);hand.transform.localPosition=new Vector3(.47f,.9f,.2f);RightHand=hand.transform;
            renderers=GetComponentsInChildren<MeshRenderer>();baseColors=new Color[renderers.Length];for(int i=0;i<renderers.Length;i++)baseColors[i]=renderers[i].sharedMaterial.color;block=new MaterialPropertyBlock();previous=transform.position;
        }
        private static SpriteRenderer Find(SpriteRenderer[] sprites,string name)
        {foreach(var sprite in sprites)if(sprite.name.IndexOf(name,System.StringComparison.OrdinalIgnoreCase)>=0&&sprite.sprite!=null)return sprite;return null;}
        private Transform Make(string title,Vector3 position,Vector3 size,SpriteRenderer source)
        {
            GameObject part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=title;part.layer=2;
            var collider=part.GetComponent<Collider>();if(collider!=null)collider.enabled=false;Destroy(collider);part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=size;
            Shader shader=UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelLit")??Shader.Find("Unlit/Texture");
            var material=new Material(shader);material.SetFloat("_UseVertexLighting",0);if(source!=null&&source.sprite!=null){material.mainTexture=source.sprite.texture;Vector2 min=Vector2.one,max=Vector2.zero;foreach(var uv in source.sprite.uv){min=new Vector2(Mathf.Min(min.x,uv.x),Mathf.Min(min.y,uv.y));max=new Vector2(Mathf.Max(max.x,uv.x),Mathf.Max(max.y,uv.y));}material.mainTextureScale=max-min;material.mainTextureOffset=min;material.color=source.color;}
            else material.color=title=="Head"?new Color(.75f,.59f,.42f):new Color(.32f,.4f,.5f);
            part.GetComponent<MeshRenderer>().sharedMaterial=material;materials.Add(material);return part.transform;
        }
        private void LateUpdate()
        {
            float speed=(transform.position-previous).magnitude/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            float swing=Mathf.Sin(Time.time*10)*Mathf.Clamp(speed*4,0,30);
            if(leftArm!=null){leftArm.localRotation=Quaternion.Euler(swing,0,0);rightArm.localRotation=Quaternion.Euler(-swing,0,0);leftLeg.localRotation=Quaternion.Euler(-swing,0,0);rightLeg.localRotation=Quaternion.Euler(swing,0,0);}
            var rt=VoxelWorld3DRuntime.Instance;if(rt==null||renderers==null)return;
            bool eating=rt.Player!=null&&rt.Player.GetComponent<Game.PlayerStats.FoodUseController>()?.IsEating==true;
            if(rightArm!=null)
            {
                rightArm.localPosition=eating?new Vector3(.36f,1.2f,.26f):new Vector3(.36f,1.05f,0);
                if(eating)rightArm.localRotation=Quaternion.Euler(-78+Mathf.Sin(Time.time*18)*4,0,0);
            }
            if(RightHand!=null)RightHand.localPosition=eating?new Vector3(.43f,1.38f,.55f):new Vector3(.47f,.9f,.2f);
            Color light=rt.Lighting.Sample(transform.position+Vector3.up);
            for(int i=0;i<renderers.Length;i++)
            {block.SetColor("_Color",baseColors[i]*light);renderers[i].SetPropertyBlock(block);}
        }
        private void OnDestroy(){foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
