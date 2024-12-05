////using UnityEngine;
////using UnityEditor;


////[CustomEditor(typeof(ProceduralGeneration))]
//public class CustomScriptInscpector : Editor
//{

//    //ProceduralGeneration targetScript;

//    //void OnEnable()
//    //{
//    //    targetScript = target as ProceduralGeneration;
//    //}

//    //public override void OnInspectorGUI()
//    //{

//    //    ProceduralGeneration.X = EditorGUILayout.IntField(ProceduralGeneration.X);
//    //    ProceduralGeneration.Y = EditorGUILayout.IntField(ProceduralGeneration.Y);

//    //    EditorGUILayout.BeginHorizontal();
//    //    for (int y = 0; y < ProceduralGeneration.Y; y++)
//    //    {
//    //        EditorGUILayout.BeginVertical();
//    //        for (int x = 0; x < ProceduralGeneration.X; x++)
//    //        {

//    //            targetScript.columns[x].rows[y] = EditorGUILayout.Toggle(targetScript.columns[x].rows[y]);
//    //        }
//    //        EditorGUILayout.EndVertical();

//    //    }
//    //    EditorGUILayout.EndHorizontal();

//    //}
//}