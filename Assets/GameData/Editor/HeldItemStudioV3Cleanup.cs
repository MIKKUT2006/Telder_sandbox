#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeldItemStudioV3Cleanup
{
    [MenuItem("Tools/Game/Held Item Pose Studio V3/Remove old V39 weapon extension")]
    public static void RemoveOldExtension()
    {
        const string extension = "Assets/GameData/Editor/HeldItemPoseEditorWindow.Weapons.cs";
        const string installer = "Assets/GameData/Editor/HeldItemPoseWeaponEditorInstaller.cs";

        if (!EditorUtility.DisplayDialog(
                "Held Item Studio V3",
                "Удалить старую V39/V2 weapon-панель из прежнего Held Item Pose Editor?\n\n" +
                "Новый Studio V3 работает независимо. Исходный HeldItemPoseEditorWindow.cs удалён не будет.",
                "Удалить старое расширение",
                "Отмена"))
        {
            return;
        }

        if (File.Exists(extension)) File.Delete(extension);
        if (File.Exists(extension + ".meta")) File.Delete(extension + ".meta");
        if (File.Exists(installer)) File.Delete(installer);
        if (File.Exists(installer + ".meta")) File.Delete(installer + ".meta");

        AssetDatabase.Refresh();
        Debug.Log("Held Item Studio V3: old V39 weapon extension removed.");
    }
}
#endif
