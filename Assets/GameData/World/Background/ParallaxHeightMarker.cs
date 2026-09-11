
using UnityEngine;


namespace Game.World.Background
{

    public class ParallaxHeightMarker :
        MonoBehaviour
    {

        [SerializeField]
        private float appliedOffsetY;


        public float AppliedOffsetY =>
            appliedOffsetY;


        public void SetAppliedOffset(
            float value
        )
        {

            appliedOffsetY =
                value;

        }

    }

}
