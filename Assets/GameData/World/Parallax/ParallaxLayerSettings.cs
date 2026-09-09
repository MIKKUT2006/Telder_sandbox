using System;
using UnityEngine;

namespace Game.World.Parallax
{
    [Serializable]
    public class ParallaxLayerSettings
    {
        public string Name;

        public float FactorX;
        public float FactorY;

        /*
         * Size of one visual background block in world units.
         *
         * V10.4 values are ~2.5x smaller than V10.3.
         */
        public float BlockWorldScale;

        public float HeightAmplitude;
        public float NoiseScale;

        public float DetailAmplitude;
        public float DetailScale;

        public int LoadRadius;
        public int SortingOrder;

        public Color Tint;

        /*
         * Negative value moves the entire background horizon down.
         */
        public float VerticalOffset;

        /*
         * How many blocks below the generated surface are actually visible.
         *
         * This prevents a huge exposed stone wall from being visible behind
         * the player's ground.
         */
        public int VisibleDepth;


        public ParallaxLayerSettings(
            string name,
            float factorX,
            float factorY,
            float blockWorldScale,
            float heightAmplitude,
            float noiseScale,
            float detailAmplitude,
            float detailScale,
            int loadRadius,
            int sortingOrder,
            Color tint,
            float verticalOffset,
            int visibleDepth
        )
        {
            Name = name;

            FactorX = factorX;
            FactorY = factorY;

            BlockWorldScale = blockWorldScale;

            HeightAmplitude = heightAmplitude;
            NoiseScale = noiseScale;

            DetailAmplitude = detailAmplitude;
            DetailScale = detailScale;

            LoadRadius = loadRadius;
            SortingOrder = sortingOrder;

            Tint = tint;

            VerticalOffset = verticalOffset;

            VisibleDepth = Mathf.Max(
                3,
                visibleDepth
            );
        }
    }
}