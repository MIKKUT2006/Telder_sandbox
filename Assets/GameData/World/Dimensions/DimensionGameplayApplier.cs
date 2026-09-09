using UnityEngine;

namespace Game.World.Dimensions
{
    /*
     * Необязательный компонент.
     *
     * Можно добавить на WorldManager или отдельный GameObject.
     * Уже сейчас делает LOW/HIGH GRAVITY настоящими gameplay modifiers.
     *
     * Остальные модификаторы будут подключаться своими системами:
     * AcidRainSystem, WeatherSystem, OreGenerator, Lighting и т.д.
     */
    public class DimensionGameplayApplier :
        MonoBehaviour
    {
        [Header("ГРАВИТАЦИЯ")]

        [SerializeField]
        private bool applyGravity =
            true;

        [SerializeField]
        private Vector2 normalGravity =
            new Vector2(
                0f,
                -9.81f
            );


        private void Awake()
        {
            ApplyCurrentDimension();
        }


        public void ApplyCurrentDimension()
        {
            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (applyGravity)
            {
                Physics2D.gravity =
                    normalGravity *
                    dimension.GravityMultiplier;
            }


            Debug.Log(
                "ИЗМЕРЕНИЕ: " +
                dimension.Name +
                " | СИД: " +
                dimension.Seed +
                " | ТИП: " +
                dimension.Type.DisplayName +
                " | МОДИФИКАТОРЫ: " +
                dimension.Modifiers.Count
            );
        }
    }
}