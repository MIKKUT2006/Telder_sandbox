using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SpecialWorlds
{
    public enum HarpoonPowerup { Shield,Speed,Feather,Winch }
    public enum HarpoonModifier { None,LowGravity,QuickPace,Tailwind,ObstacleRush }

    public sealed class HarpoonRunRules
    {
        private readonly float[] until=new float[4];
        private System.Random random;
        private float nextModifier,modifierUntil;
        private bool shield;
        public float Elapsed {get;private set;}
        public HarpoonModifier Modifier {get;private set;}
        public int Difficulty=>Mathf.Min(6,1+Mathf.FloorToInt(Elapsed/50));
        public float Gravity=>(Active(HarpoonPowerup.Feather)||Modifier==HarpoonModifier.LowGravity)? .5f : 1f;
        public float Speed=>(Active(HarpoonPowerup.Speed)?1.5f:1f)*(Modifier==HarpoonModifier.QuickPace?1.25f:1f);
        public float Grapple=>Active(HarpoonPowerup.Winch)?1.35f:1f;
        public bool HasShield=>shield&&Active(HarpoonPowerup.Shield);
        public float ModifierRemaining=>Mathf.Max(0,modifierUntil-Elapsed);
        public HarpoonRunRules(){Reset(0);}
        public void Reset(int seed)
        {random=new System.Random(seed);Elapsed=0;Modifier=HarpoonModifier.None;modifierUntil=0;nextModifier=35;shield=false;Array.Clear(until,0,until.Length);}
        public bool Advance(float delta)
        {
            Elapsed+=Mathf.Max(0,delta);
            if(Modifier!=HarpoonModifier.None&&Elapsed>=modifierUntil)Modifier=HarpoonModifier.None;
            if(Elapsed<nextModifier)return false;
            var previous=Modifier;Modifier=(HarpoonModifier)(1+random.Next(4));
            if(Modifier==previous)Modifier=(HarpoonModifier)(1+(int)Modifier%4);
            modifierUntil=Elapsed+14;nextModifier=Elapsed+38;return true;
        }
        public bool Active(HarpoonPowerup power)=>until[(int)power]>Elapsed;
        public float Remaining(HarpoonPowerup power)=>Mathf.Max(0,until[(int)power]-Elapsed);
        public void Grant(HarpoonPowerup power)
        {float duration=power==HarpoonPowerup.Shield?25:power==HarpoonPowerup.Speed?10:14;until[(int)power]=Elapsed+duration;if(power==HarpoonPowerup.Shield)shield=true;}
        public bool ConsumeShield(){if(!HasShield)return false;shield=false;until[0]=0;return true;}
        public static string PowerName(HarpoonPowerup power)
        {switch(power){case HarpoonPowerup.Shield:return "Щит";case HarpoonPowerup.Speed:return "Ускорение";case HarpoonPowerup.Feather:return "Перо";default:return "Быстрая лебёдка";}}
        public static string ModifierName(HarpoonModifier modifier)
        {switch(modifier){case HarpoonModifier.LowGravity:return "Низкая гравитация";case HarpoonModifier.QuickPace:return "Быстрый темп";case HarpoonModifier.Tailwind:return "Попутный ветер";case HarpoonModifier.ObstacleRush:return "Полоса препятствий";default:return "";}}
    }

    public sealed class HarpoonRunDirector : MonoBehaviour
    {
        private sealed class CourseObject
        {public GameObject Root;public SpriteRenderer Sprite;public Vector2 Home,Half;public float Phase,Amplitude;public bool Hazard;public HarpoonPowerup Power;}
        public HarpoonRunRules Rules {get;}=new HarpoonRunRules();
        public int Lives {get;private set;}
        public string Notice=>Rules.Elapsed<noticeUntil?notice:"";
        private readonly List<CourseObject> objects=new List<CourseObject>(24);
        private readonly Dictionary<int,Sprite> sprites=new Dictionary<int,Sprite>();
        private readonly List<Texture2D> textures=new List<Texture2D>();
        private HarpoonCaveGenerator caves;
        private Material material;
        private System.Random random;
        private float nextPickup,nextHazard,invulnerableUntil,noticeUntil;
        private string notice;
        private int pickupsCreated;
        public int ActiveObjectCount=>objects.Count;
        public void Initialize(HarpoonCaveGenerator generator)
        {
            caves=generator;
            Shader shader=Shader.Find("Sprites/Default")??UnityEngine.Resources.Load<Shader>("Shaders/TelderVoxelSprite");
            if(shader!=null)material=new Material(shader){name="Harpoon pickups and hazards"};
        }
        public void ResetRun(int seed)
        {
            foreach(var item in objects)if(item.Root!=null){foreach(var c in item.Root.GetComponents<Collider2D>())c.enabled=false;Destroy(item.Root);}
            objects.Clear();Rules.Reset(seed);random=new System.Random(seed^167371);Lives=3;
            nextPickup=18;nextHazard=34;invulnerableUntil=0;noticeUntil=6;notice="Подбирай усиления; опасности отмечены красным";pickupsCreated=0;
        }
        public bool Tick(float delta,Rigidbody2D player)
        {
            if(Rules.Advance(delta)){notice=HarpoonRunRules.ModifierName(Rules.Modifier);noticeUntil=Rules.Elapsed+5;}
            float x=player.position.x;
            // At most one new object of each kind per frame; no spawning under
            // the character when difficulty changes or a long swing skips ahead.
            if(nextPickup<x-10)nextPickup=x+18;
            if(nextHazard<x+12)nextHazard=x+18;
            if(nextPickup<x+55){Spawn(nextPickup,false);nextPickup+=19+random.Next(9);}
            if(nextHazard<x+65){Spawn(nextHazard,true);nextHazard+=Mathf.Max(11,24-Rules.Difficulty*2)/(Rules.Modifier==HarpoonModifier.ObstacleRush?1.5f:1);}
            for(int i=objects.Count-1;i>=0;i--)
            {
                var item=objects[i];
                if(item.Root==null||item.Home.x<x-42)
                {if(item.Root!=null)Destroy(item.Root);objects.RemoveAt(i);continue;}
                var p=item.Home+Vector2.up*(Mathf.Sin(Rules.Elapsed*(item.Hazard?1.5f:2.5f)+item.Phase)*item.Amplitude);
                item.Root.transform.position=new Vector3(p.x,p.y,-.15f);
                if(!item.Hazard)item.Root.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Rules.Elapsed*2+item.Phase)*12);
                Vector2 distance=player.position-p;
                if(Mathf.Abs(distance.x)>item.Half.x+.34f||Mathf.Abs(distance.y)>item.Half.y+.67f)continue;
                if(!item.Hazard)
                {
                    Rules.Grant(item.Power);notice=HarpoonRunRules.PowerName(item.Power)+" +";noticeUntil=Rules.Elapsed+3;
                    Destroy(item.Root);objects.RemoveAt(i);continue;
                }
                if(Rules.Elapsed<invulnerableUntil)continue;invulnerableUntil=Rules.Elapsed+1.4f;
                if(Rules.ConsumeShield()){notice="Щит поглотил удар";noticeUntil=Rules.Elapsed+3;}
                else
                {
                    Lives--;notice="Осторожно! Осталось жизней: "+Lives;noticeUntil=Rules.Elapsed+3;
                    player.velocity=new Vector2(distance.x>=0?7:-7,8);
                    if(Lives<=0)return true;
                }
            }
            return false;
        }
        private void Spawn(float x,bool hazard)
        {
            if(objects.Count>=32)return;
            int column=Mathf.FloorToInt(x);float floor=caves.GetFloorHeight(column),ceiling=caves.GetCeilingHeight(column);
            if(floor<-12)floor=-5;
            float low=floor+1.5f,high=Mathf.Max(low,ceiling-2);
            var item=new CourseObject{Hazard=hazard,Phase=(float)random.NextDouble()*6.28f};
            int kind;
            if(hazard)
            {
                bool gate=Rules.Difficulty>1&&random.Next(3)==0;
                kind=gate?5:4;item.Half=gate?new Vector2(.55f,1.3f):new Vector2(.55f,.55f);
                item.Home=new Vector2(x,gate?floor+1.3f:Mathf.Lerp(low,high,.55f));
                item.Amplitude=gate?0:Mathf.Min(1.7f,Mathf.Max(0,(high-low)*.3f));
            }
            else
            {
                item.Power=pickupsCreated==0?HarpoonPowerup.Shield:(HarpoonPowerup)random.Next(4);pickupsCreated++;
                kind=(int)item.Power;item.Half=new Vector2(.35f,.35f);
                item.Home=new Vector2(x,pickupsCreated==1?low:Mathf.Lerp(low,high,(float)random.NextDouble()));item.Amplitude=.15f;
            }
            item.Root=new GameObject(hazard?"Harpoon obstacle":"Powerup "+item.Power);item.Root.transform.SetParent(transform,false);
            item.Root.transform.position=item.Home;item.Root.transform.localScale=new Vector3(item.Half.x*2,item.Half.y*2,1);
            item.Sprite=item.Root.AddComponent<SpriteRenderer>();item.Sprite.sprite=GetSprite(kind);item.Sprite.sortingLayerName="Default";item.Sprite.sortingOrder=70;
            if(material!=null)item.Sprite.sharedMaterial=material;
            if(hazard)
            {
                if(kind==5)item.Root.AddComponent<BoxCollider2D>().size=Vector2.one;
                else item.Root.AddComponent<CircleCollider2D>().radius=.48f;
            }
            else item.Root.layer=2; // pick-ups cannot intercept the harpoon ray
            objects.Add(item);
        }
        private Sprite GetSprite(int kind)
        {
            if(sprites.TryGetValue(kind,out var sprite))return sprite;
            Color32[] colors={new Color32(84,185,255,255),new Color32(255,209,62,255),new Color32(122,246,201,255),new Color32(206,134,255,255),new Color32(248,97,76,255),new Color32(248,97,76,255)};
            var color=colors[kind];var data=new Color32[16*16];
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                int dx=Mathf.Abs(x-7),dy=Mathf.Abs(y-7);bool inside=kind==5||dx+dy<10;
                if(!inside)continue;
                bool edge=kind==5?(x<2||x>13||y<2||y>13):dx+dy>=8;
                data[y*16+x]=edge?new Color32(30,38,53,255):color;
                bool mark=kind==0?(dx==3&&y>3&&y<12||y==4&&dx<3):kind==1?(x+y>11&&x+y<15&&y>3&&y<13):kind==2?(dx<=1&&y>3&&y<13||dy==1&&x>4&&x<11):kind==3?(dx==3&&dy<4||dy==3&&dx<4):((x+y)%5<2);
                if(mark&&!edge)data[y*16+x]=new Color32(255,248,226,255);
            }
            var texture=new Texture2D(16,16,TextureFormat.RGBA32,false){name="Harpoon pixel icon "+kind,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(data);texture.Apply(false,true);textures.Add(texture);
            sprite=Sprite.Create(texture,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);sprites[kind]=sprite;return sprite;
        }
        public string Status()
        {
            string text="Жизни: "+Lives+"    Сложность: "+Rules.Difficulty;
            if(Rules.Modifier!=HarpoonModifier.None)text+="    "+HarpoonRunRules.ModifierName(Rules.Modifier)+" "+Mathf.CeilToInt(Rules.ModifierRemaining)+"с";
            string powers="";
            for(int i=0;i<4;i++)if(Rules.Active((HarpoonPowerup)i))powers+="    "+HarpoonRunRules.PowerName((HarpoonPowerup)i)+" "+Mathf.CeilToInt(Rules.Remaining((HarpoonPowerup)i))+"с";
            if(powers.Length>0)text+="\n"+powers.Trim();
            return text;
        }
        private void OnDestroy()
        {foreach(var sprite in sprites.Values)if(sprite!=null)Destroy(sprite);foreach(var texture in textures)if(texture!=null)Destroy(texture);if(material!=null)Destroy(material);}
    }
}
