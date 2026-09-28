#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


public static class HeldItemPoseSceneDirtyBridge
{
    public static void Mark(
        Scene scene
    )
    {
        if (
            EditorApplication.isPlaying
            ||
            EditorApplication.isPlayingOrWillChangePlaymode
        )
        {
            return;
        }


        if (!scene.IsValid())
            return;


        // Real Unity editor call lives OUTSIDE HeldItemPoseEditorWindow.cs.
        // Old V38 text replacement therefore cannot turn this into recursion.
        EditorSceneManager.MarkSceneDirty(
            scene
        );
    }
}

#endif
