using UnityEngine;

namespace Game.Debugging
{
    /// <summary>
    /// Global debug lighting state.
    ///
    /// The ChunkLitSprite shader reads _DebugFullBright.
    /// Because it is a GLOBAL shader parameter, toggling it does
    /// not rebuild lighting and does not iterate through chunks.
    /// </summary>
    public static class DebugLightingState
    {
        private static bool fullBright;


        public static bool FullBright
        {
            get
            {
                return fullBright;
            }
        }


        public static void SetFullBright(
            bool enabled
        )
        {
            fullBright =
                enabled;


            Shader.SetGlobalFloat(
                "_DebugFullBright",
                enabled
                    ? 1f
                    : 0f
            );
        }


        public static void Toggle()
        {
            SetFullBright(
                !fullBright
            );
        }


        public static void Reset()
        {
            SetFullBright(
                false
            );
        }
    }
}
