
using UnityEngine;


namespace Game.Mining
{

    public static class MiningVisualSignal
    {

        private static int lastMiningFrame =
            -1000;


        public static void Pulse()
        {

            lastMiningFrame =
                Time.frameCount;

        }


        public static bool IsMining =>
            Time.frameCount -
            lastMiningFrame
            <=
            1;

    }

}
