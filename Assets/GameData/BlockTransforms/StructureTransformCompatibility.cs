using UnityEngine;

namespace Game.BlockTransforms
{
    public static class StructureTransformCompatibility
    {
        public static byte Rotate(byte value)
        {
            BlockTransformState state = new BlockTransformState(value);
            state.RotateClockwise();
            return state.Value;
        }

        public static byte Mirror(byte value)
        {
            BlockTransformState state = new BlockTransformState(value);
            state.ToggleMirror();
            return state.Value;
        }

        public static void ApplyToRectTransform(RectTransform rect, byte value)
        {
            if (rect == null)
                return;

            BlockTransformState state = new BlockTransformState(value);
            rect.localEulerAngles = new Vector3(0f, 0f, -state.RotationDegrees);

            Vector3 scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) * (state.Mirrored ? -1f : 1f);
            rect.localScale = scale;
        }
    }
}
