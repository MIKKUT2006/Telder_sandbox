using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SpecialWorlds
{
    // Clone only the presentation hierarchy. Loading the player prefab must not
    // awaken its survival controller, inventory, health, save or weapon scripts.
    public sealed class HarpoonChallengeAvatar : MonoBehaviour
    {
        private Transform graphic;
        private Transform frontLeg,backLeg,frontArm,backArm;
        private Quaternion frontLegRest,backLegRest,frontArmRest,backArmRest;
        private SpriteRenderer[] renderers;
        private Material material;
        private Texture2D fallbackTexture;
        private Sprite fallbackSprite;
        private float stride;
        private int facing=1;
        public bool UsesPrefabVisual {get;private set;}

        public void Initialize()
        {
            Shader shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")??
                Shader.Find("Sprites/Default")??UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");
            if(shader!=null)material=new Material(shader){name="Harpoon avatar unlit"};
            var prefab=UnityEngine.Resources.Load<GameObject>("PlayerCustomizer/PlayerPreview");
            graphic=new GameObject("Player visual").transform;graphic.SetParent(transform,false);
            if(prefab!=null)CopyVisual(prefab.transform,graphic,true);
            renderers=graphic.GetComponentsInChildren<SpriteRenderer>(true);
            bool any=false;Bounds bounds=default;
            foreach(var sprite in renderers)
            {
                if(sprite.sprite==null)continue;
                if(!any){bounds=sprite.bounds;any=true;}else bounds.Encapsulate(sprite.bounds);
            }
            if(any&&bounds.size.y>.01f)
            {
                UsesPrefabVisual=true;
                float scale=1.52f/bounds.size.y;
                Vector3 center=graphic.InverseTransformPoint(bounds.center);
                graphic.localScale=Vector3.one*scale;graphic.localPosition=-center*scale;
                frontLeg=Find("FrontLegPivot");backLeg=Find("BackLegPivot");
                frontArm=Find("FrontArmPivot");backArm=Find("BackArmPivot");
                if(frontLeg!=null)frontLegRest=frontLeg.localRotation;
                if(backLeg!=null)backLegRest=backLeg.localRotation;
                if(frontArm!=null)frontArmRest=frontArm.localRotation;
                if(backArm!=null)backArmRest=backArm.localRotation;
            }
            else BuildFallback();
            EnsureVisible();
        }
        private void CopyVisual(Transform source,Transform parent,bool root=false)
        {
            var go=new GameObject(source.name);go.layer=0;go.transform.SetParent(parent,false);
            go.transform.localPosition=root?Vector3.zero:source.localPosition;
            go.transform.localRotation=root?Quaternion.identity:source.localRotation;
            go.transform.localScale=root?Vector3.one:source.localScale;
            var original=source.GetComponent<SpriteRenderer>();
            if(original!=null&&original.sprite!=null)
            {
                var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=original.sprite;
                sprite.color=new Color(original.color.r,original.color.g,original.color.b,1);
                sprite.flipX=original.flipX;sprite.flipY=original.flipY;
                sprite.sortingLayerName="Default";sprite.sortingOrder=100+original.sortingOrder;
                if(material!=null)sprite.sharedMaterial=material;
            }
            for(int i=0;i<source.childCount;i++)CopyVisual(source.GetChild(i),go.transform);
        }
        private Transform Find(string name)
        {foreach(var child in graphic.GetComponentsInChildren<Transform>(true))if(string.Equals(child.name,name,StringComparison.OrdinalIgnoreCase))return child;return null;}
        private void BuildFallback()
        {
            foreach(var sprite in renderers)if(sprite!=null)Destroy(sprite.gameObject);
            fallbackTexture=new Texture2D(16,32,TextureFormat.RGBA32,false){name="Harpoon fallback player",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[16*32];
            Paint(pixels,4,0,4,10,new Color32(32,46,67,255));Paint(pixels,9,0,4,10,new Color32(32,46,67,255));
            Paint(pixels,3,0,5,3,new Color32(87,55,38,255));Paint(pixels,9,0,5,3,new Color32(87,55,38,255));
            Paint(pixels,3,9,10,13,new Color32(54,139,190,255));Paint(pixels,0,10,3,11,new Color32(220,166,119,255));
            Paint(pixels,13,10,3,11,new Color32(220,166,119,255));Paint(pixels,3,22,10,9,new Color32(239,186,139,255));
            Paint(pixels,3,28,10,4,new Color32(69,44,34,255));Paint(pixels,10,25,2,2,new Color32(24,31,43,255));
            fallbackTexture.SetPixels32(pixels);fallbackTexture.Apply(false,true);
            fallbackSprite=Sprite.Create(fallbackTexture,new Rect(0,0,16,32),new Vector2(.5f,.5f),21);
            var visible=new GameObject("Visible player fallback");visible.transform.SetParent(graphic,false);
            var sr=visible.AddComponent<SpriteRenderer>();sr.sprite=fallbackSprite;sr.sortingLayerName="Default";sr.sortingOrder=105;
            if(material!=null)sr.sharedMaterial=material;
            renderers=new[]{sr};
        }
        private static void Paint(Color32[] data,int x,int y,int width,int height,Color32 color)
        {for(int py=y;py<y+height;py++)for(int px=x;px<x+width;px++)data[py*16+px]=color;}
        public void Animate(Vector2 velocity,bool attached,Vector2 aim)
        {
            if(Mathf.Abs(aim.x)>.15f)facing=aim.x>0?1:-1;
            transform.localScale=new Vector3(facing,1,1);
            stride+=Time.deltaTime*Mathf.Min(20,Mathf.Abs(velocity.x)*2);
            float swing=Mathf.Sin(stride)*Mathf.Clamp01(Mathf.Abs(velocity.x)/5)*24;
            if(frontLeg!=null)frontLeg.localRotation=frontLegRest*Quaternion.Euler(0,0,swing);
            if(backLeg!=null)backLeg.localRotation=backLegRest*Quaternion.Euler(0,0,-swing);
            float arm=attached?-115:-swing*.55f;
            if(frontArm!=null)frontArm.localRotation=frontArmRest*Quaternion.Euler(0,0,arm);
            if(backArm!=null)backArm.localRotation=backArmRest*Quaternion.Euler(0,0,attached?-95:swing*.55f);
        }
        public void EnsureVisible()
        {if(renderers!=null)foreach(var sprite in renderers)if(sprite!=null){sprite.enabled=true;if(!sprite.gameObject.activeSelf)sprite.gameObject.SetActive(true);}}
        private void OnDestroy()
        {if(material!=null)Destroy(material);if(fallbackSprite!=null)Destroy(fallbackSprite);if(fallbackTexture!=null)Destroy(fallbackTexture);}
    }
}
