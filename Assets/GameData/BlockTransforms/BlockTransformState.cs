using System;

namespace Game.BlockTransforms
{
    public enum BlockTransformMode
    {
        None = 0,
        Rotate = 1,
        Mirror = 2
    }

    [Serializable]
    public struct BlockTransformState
    {
        private const byte RotationMask = 0b00000011;
        private const byte MirrorMask = 0b00000100;

        public byte Value;

        public int Rotation => Value & RotationMask;
        public bool Mirrored => (Value & MirrorMask) != 0;
        public int RotationDegrees => Rotation * 90;

        public BlockTransformState(byte value)
        {
            Value = value;
        }

        public void RotateClockwise()
        {
            int rotation = (Rotation + 1) & 3;
            Value = (byte)((Value & ~RotationMask) | rotation);
        }

        public void ToggleMirror()
        {
            Value ^= MirrorMask;
        }

        public static BlockTransformState Identity =>
            new BlockTransformState(0);
    }
}
