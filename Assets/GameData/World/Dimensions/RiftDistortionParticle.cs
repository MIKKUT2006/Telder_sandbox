using UnityEngine;

namespace Game.World.Dimensions
{
    /*
     * Это НЕ Renderer.
     *
     * Частица хранит только позицию и размер.
     * Само изображение + искажение реальности
     * рисует fullscreen URP pass.
     *
     * Благодаря этому частица может искажать
     * уже отрисованные foreground/background блоки.
     */
    public class RiftDistortionParticle :
        MonoBehaviour
    {
        private Vector2 basePosition;

        private float phase;

        private float speed;

        private float baseRadius;

        private float strength;

        private float collapse;


        public float BaseRadius =>
            baseRadius;

        public float Strength =>
            strength;


        public static RiftDistortionParticle Create(
            Transform parent,
            Vector2 localPosition
        )
        {
            GameObject go =
                new GameObject(
                    "RiftDistortionParticle"
                );

            go.transform.SetParent(
                parent,
                false
            );

            go.transform.localPosition =
                new Vector3(
                    localPosition.x,
                    localPosition.y,
                    0f
                );

            RiftDistortionParticle particle =
                go.AddComponent<
                    RiftDistortionParticle
                >();

            particle.Initialize(
                localPosition
            );

            return particle;
        }


        private void Initialize(
            Vector2 localPosition
        )
        {
            basePosition =
                localPosition;

            phase =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

            speed =
                Random.Range(
                    0.55f,
                    1.25f
                );

            baseRadius =
                Random.Range(
                    0.16f,
                    0.32f
                );

            strength =
                Random.Range(
                    0.65f,
                    1.15f
                );
        }


        private void Update()
        {
            float t =
                Time.time * speed +
                phase;

            Vector2 wobble =
                new Vector2(
                    Mathf.Sin(
                        t * 1.75f
                    ),
                    Mathf.Cos(
                        t * 1.31f
                    )
                )
                *
                0.20f;

            Vector2 target =
                basePosition +
                wobble;

            target =
                Vector2.Lerp(
                    target,
                    Vector2.zero,
                    collapse
                );

            transform.localPosition =
                new Vector3(
                    target.x,
                    target.y,
                    0f
                );
        }


        public void SetCollapse(
            float value
        )
        {
            collapse =
                Mathf.Clamp01(
                    value
                );
        }
    }
}