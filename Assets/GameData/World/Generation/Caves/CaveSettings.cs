using System;

namespace Game.World.Generation
{
    [Serializable]
    public class CaveSettings
    {
        // =====================================================
        // SURFACE
        // =====================================================

        // Сколько блоков под поверхностью защищено от обычных пещер.
        public int StartDepth;

        // Ширина перехода от защищённой зоны
        // к нормальной генерации.
        public int SurfaceTransition;


        // =====================================================
        // MAIN CAVE FIELD
        // =====================================================

        // Большие структуры.
        // Именно этот шум формирует крупные пещерные пространства.
        public float LargeScale;

        // Средние структуры.
        public float MediumScale;

        // Мелкие неровности.
        public float DetailScale;


        // =====================================================
        // FIELD WEIGHTS
        // =====================================================

        public float LargeWeight;
        public float MediumWeight;
        public float DetailWeight;


        // =====================================================
        // THRESHOLD
        // =====================================================

        // Чем больше — тем меньше пещер.
        public float CaveThreshold;


        // =====================================================
        // DOMAIN WARP
        // =====================================================

        // Частота warp.
        public float WarpScale;

        // Сила искривления координат.
        public float WarpStrength;


        // =====================================================
        // TUNNELS
        // =====================================================

        // Частота длинных извилистых тоннелей.
        public float TunnelScale;

        // Порог ridge noise.
        //
        // Больше = тоннелей меньше.
        public float TunnelThreshold;

        // Насколько сильно тоннели
        // усиливают существующее поле.
        public float TunnelStrength;


        // =====================================================
        // DEPTH
        // =====================================================

        // Насколько увеличивается вероятность пещер
        // после StartDepth.
        public float DeepCaveBoost;


        // =====================================================
        // SURFACE ENTRANCES
        // =====================================================

        public bool EnableSurfaceEntrances;

        // Вероятность выхода в каждом регионе.
        public float SurfaceEntranceChance;

        // Размер региона поиска выхода.
        public int EntranceRegionSize;

        // Длина входного тоннеля.
        public int EntranceLength;

        // Начальный радиус.
        public float EntranceRadius;

        // Конечный радиус.
        public float EntranceRadiusBottom;

        // Насколько сильно тоннель отклоняется от вертикали.
        public float EntranceWander;

        // Частота изменения направления.
        public float EntranceWanderScale;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public CaveSettings()
        {
            // -------------------------------------------------
            // SURFACE
            // -------------------------------------------------

            StartDepth =
                12;

            SurfaceTransition =
                28;


            // -------------------------------------------------
            // MAIN FIELD
            // -------------------------------------------------

            // Большие пространства.
            LargeScale =
                0.014f;

            // Средние соединения.
            MediumScale =
                0.038f;

            // Неровность стен.
            DetailScale =
                0.095f;


            // -------------------------------------------------
            // WEIGHTS
            // -------------------------------------------------

            LargeWeight =
                0.55f;

            MediumWeight =
                0.30f;

            DetailWeight =
                0.15f;


            // -------------------------------------------------
            // THRESHOLD
            // -------------------------------------------------

            // Хорошая стартовая плотность.
            CaveThreshold =
                0.62f;


            // -------------------------------------------------
            // WARP
            // -------------------------------------------------

            WarpScale =
                0.010f;

            WarpStrength =
                22f;


            // -------------------------------------------------
            // TUNNELS
            // -------------------------------------------------

            TunnelScale =
                0.026f;

            TunnelThreshold =
                0.84f;

            TunnelStrength =
                0.32f;


            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            DeepCaveBoost =
                0.08f;


            // -------------------------------------------------
            // SURFACE ENTRANCES
            // -------------------------------------------------

            EnableSurfaceEntrances =
                true;

            SurfaceEntranceChance =
                0.025f;

            EntranceRegionSize =
                140;

            EntranceLength =
                65;

            EntranceRadius =
                2.5f;

            EntranceRadiusBottom =
                3.2f;

            EntranceWander =
                20f;

            EntranceWanderScale =
                0.045f;
        }
    }
}