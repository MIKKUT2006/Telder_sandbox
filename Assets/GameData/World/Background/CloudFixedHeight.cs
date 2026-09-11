
using UnityEngine;


namespace Game.World.Background
{

    [DefaultExecutionOrder(32000)]
    public class CloudFixedHeight :
        MonoBehaviour
    {

        [SerializeField]
        private bool useCurrentHeightOnStart =
            true;


        [SerializeField]
        private float fixedWorldY;


        private bool initialized;


        private void Start()
        {

            if (
                useCurrentHeightOnStart
            )
            {

                fixedWorldY =
                    transform.position.y;

            }


            initialized =
                true;


            ApplyHeight();

        }


        private void LateUpdate()
        {

            if (
                !initialized
            )
            {

                return;

            }


            ApplyHeight();

        }


        private void ApplyHeight()
        {

            Vector3 position =
                transform.position;


            // Horizontal parallax is left untouched.
            // Only vertical movement is cancelled.
            position.y =
                fixedWorldY;


            transform.position =
                position;

        }


#if UNITY_EDITOR
        public void CaptureCurrentHeight()
        {

            fixedWorldY =
                transform.position.y;


            useCurrentHeightOnStart =
                false;

        }
#endif

    }

}
