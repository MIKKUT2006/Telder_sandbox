using UnityEngine;


namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatRigCalibration :
        MonoBehaviour
    {
        [Header("Status")]

        [SerializeField]
        private bool configured;


        [Header("Base Arm Positions")]

        [SerializeField]
        private Vector2 frontArmBaseLocalPosition;


        [SerializeField]
        private Vector2 backArmBaseLocalPosition;


        [SerializeField]
        private Vector2 handPointLocalPosition =
            new Vector2(
                0.28f,
                0f
            );


        [Header("Shoulder Points (arm.parent local space)")]

        [SerializeField]
        private Vector2 frontShoulderParentLocal;


        [SerializeField]
        private Vector2 backShoulderParentLocal;


        [Header("Fist Points (arm local space)")]

        [SerializeField]
        private Vector2 frontFistArmLocal =
            new Vector2(
                0.28f,
                0f
            );


        [SerializeField]
        private Vector2 backFistArmLocal =
            new Vector2(
                0.28f,
                0f
            );


        public bool Configured =>
            configured;


        public Vector2 FrontArmBaseLocalPosition =>
            frontArmBaseLocalPosition;


        public Vector2 BackArmBaseLocalPosition =>
            backArmBaseLocalPosition;


        public Vector2 HandPointLocalPosition =>
            handPointLocalPosition;


        public Vector2 FrontShoulderParentLocal =>
            frontShoulderParentLocal;


        public Vector2 BackShoulderParentLocal =>
            backShoulderParentLocal;


        public Vector2 FrontFistArmLocal =>
            frontFistArmLocal;


        public Vector2 BackFistArmLocal =>
            backFistArmLocal;


        public void CaptureCurrentRig(
            Transform frontArm,
            Transform backArm,
            Transform handPoint
        )
        {
            if (frontArm != null)
            {
                frontArmBaseLocalPosition =
                    new Vector2(
                        frontArm.localPosition.x,
                        frontArm.localPosition.y
                    );


                frontShoulderParentLocal =
                    frontArmBaseLocalPosition;
            }


            if (backArm != null)
            {
                backArmBaseLocalPosition =
                    new Vector2(
                        backArm.localPosition.x,
                        backArm.localPosition.y
                    );


                backShoulderParentLocal =
                    backArmBaseLocalPosition;
            }


            if (handPoint != null)
            {
                handPointLocalPosition =
                    new Vector2(
                        handPoint.localPosition.x,
                        handPoint.localPosition.y
                    );


                if (frontArm != null)
                {
                    Vector3 local =
                        frontArm.InverseTransformPoint(
                            handPoint.position
                        );


                    frontFistArmLocal =
                        new Vector2(
                            local.x,
                            local.y
                        );
                }
            }


            if (
                backFistArmLocal.sqrMagnitude <
                0.000001f
            )
            {
                backFistArmLocal =
                    frontFistArmLocal;
            }


            configured =
                true;
        }


        public void SetFrontArmBase(
            Vector2 value
        )
        {
            frontArmBaseLocalPosition =
                value;


            configured =
                true;
        }


        public void SetBackArmBase(
            Vector2 value
        )
        {
            backArmBaseLocalPosition =
                value;


            configured =
                true;
        }


        public void SetHandPoint(
            Vector2 value
        )
        {
            handPointLocalPosition =
                value;


            configured =
                true;
        }


        public void SetFrontShoulder(
            Vector2 value
        )
        {
            frontShoulderParentLocal =
                value;


            configured =
                true;
        }


        public void SetBackShoulder(
            Vector2 value
        )
        {
            backShoulderParentLocal =
                value;


            configured =
                true;
        }


        public void SetFrontFist(
            Vector2 value
        )
        {
            frontFistArmLocal =
                value;


            configured =
                true;
        }


        public void SetBackFist(
            Vector2 value
        )
        {
            backFistArmLocal =
                value;


            configured =
                true;
        }
    }
}
