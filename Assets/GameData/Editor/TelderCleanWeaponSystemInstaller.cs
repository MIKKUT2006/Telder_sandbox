#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


public static class TelderCleanWeaponSystemInstaller
{
    [MenuItem(
        "Tools/Telder/Clean Weapon System/Remove Old Pose Components"
    )]
    private static void RemoveOldPoseComponents()
    {
        MonoBehaviour[] all =
            Object.FindObjectsByType<
                MonoBehaviour
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        int removed =
            0;


        for (
            int i = 0;
            i < all.Length;
            i++
        )
        {
            MonoBehaviour component =
                all[i];


            if (component == null)
                continue;


            System.Type type =
                component.GetType();


            if (
                type.Name ==
                "PlayerArmPoseController"
            )
            {
                Undo.DestroyObjectImmediate(
                    component
                );


                removed++;
            }
        }


        Debug.Log(
            "TELDER CLEAN WEAPON SYSTEM: removed old PlayerArmPoseController components = "
            +
            removed
            +
            ". Now delete the old PlayerArmPoseController.cs file from Assets."
        );
    }
}

#endif
