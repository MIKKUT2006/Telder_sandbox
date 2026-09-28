#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


public static class TelderHeldItemPosePlayModeFixInstaller
{
    [MenuItem(
        "Tools/Game/Fix Held Item Pose Play Mode GUI"
    )]
    public static void Apply()
    {
        EditorUtility.DisplayDialog(
            "Held Item Pose",
            "Старый V38 текстовый патчер отключён.\n\n" +
            "Он больше не выполняет глобальный Replace, который мог создавать рекурсию.\n" +
            "Используй актуальный Stack Overflow fix.",
            "OK"
        );
    }
}

#endif
