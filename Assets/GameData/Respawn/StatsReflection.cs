using System;
using System.Reflection;
using UnityEngine;

namespace Game.GameplaySystems.Respawn
{
    internal static class StatsReflection
    {
        private static readonly string[] HealthNames={"CurrentHealth","Health","health","currentHealth"};
        private static readonly string[] MaxHealthNames={"MaxHealth","maxHealth"};
        private static readonly string[] HungerNames={"CurrentHunger","Hunger","hunger","currentHunger"};
        private static readonly string[] MaxHungerNames={"MaxHunger","maxHunger"};

        public static bool TryGetHealth(GameObject go,out float value,out float max)
        {
            value=0; max=0; if(go==null)return false;
            MonoBehaviour[] cs=go.GetComponents<MonoBehaviour>();
            for(int i=0;i<cs.Length;i++)
            {
                if(cs[i]==null)continue;
                if(TryRead(cs[i],HealthNames,out value)) { TryRead(cs[i],MaxHealthNames,out max); return true; }
            }
            return false;
        }

        public static void RestoreFull(GameObject go)
        {
            if(go==null)return; MonoBehaviour[] cs=go.GetComponents<MonoBehaviour>();
            for(int i=0;i<cs.Length;i++)
            {
                object c=cs[i]; if(c==null)continue;
                if(TryRead(c,MaxHealthNames,out float mh)) TryWrite(c,HealthNames,mh);
                if(TryRead(c,MaxHungerNames,out float mhu)) TryWrite(c,HungerNames,mhu);
                InvokeNoArg(c,new[]{"RestoreFull","ResetStats","FullRestore"});
            }
        }

        private static bool TryRead(object o,string[] names,out float v)
        {
            v=0; Type t=o.GetType(); const BindingFlags f=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.IgnoreCase;
            for(int i=0;i<names.Length;i++)
            {
                PropertyInfo p=t.GetProperty(names[i],f); if(p!=null&&p.CanRead) try { v=Convert.ToSingle(p.GetValue(o,null)); return true; } catch { }
                FieldInfo fi=t.GetField(names[i],f); if(fi!=null) try { v=Convert.ToSingle(fi.GetValue(o)); return true; } catch { }
            }
            return false;
        }
        private static void TryWrite(object o,string[] names,float v)
        {
            Type t=o.GetType(); const BindingFlags f=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.IgnoreCase;
            for(int i=0;i<names.Length;i++)
            {
                PropertyInfo p=t.GetProperty(names[i],f); if(p!=null&&p.CanWrite) try { p.SetValue(o,Convert.ChangeType(v,p.PropertyType),null); return; } catch { }
                FieldInfo fi=t.GetField(names[i],f); if(fi!=null) try { fi.SetValue(o,Convert.ChangeType(v,fi.FieldType)); return; } catch { }
            }
        }
        private static void InvokeNoArg(object o,string[] names)
        {
            Type t=o.GetType(); const BindingFlags f=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.IgnoreCase;
            for(int i=0;i<names.Length;i++){ MethodInfo m=t.GetMethod(names[i],f,null,Type.EmptyTypes,null); if(m!=null) try{m.Invoke(o,null);return;}catch{} }
        }
    }
}
