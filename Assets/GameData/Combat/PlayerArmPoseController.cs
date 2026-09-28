using UnityEngine;


namespace Game.Combat
{
    // =========================================================
    // OBSOLETE COMPATIBILITY STUB
    //
    // CLEAN V1 no longer uses PlayerArmPoseController.
    //
    // This empty component exists only so projects that still have the old
    // script/component reference can compile cleanly while the new runtime
    // uses PlayerWeaponRigController instead.
    //
    // It intentionally contains NO pose logic and NEVER modifies arms.
    // =========================================================

    [DisallowMultipleComponent]
    public sealed class PlayerArmPoseController :
        MonoBehaviour
    {
    }
}
