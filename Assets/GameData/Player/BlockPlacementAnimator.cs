using Game.Resources;
using Game.World;
using System.Collections.Generic;
using UnityEngine;


public class BlockPlacementAnimator :
    MonoBehaviour
{

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Animation")]

    [SerializeField]
    private float animationDuration =
        0.5f;


    [SerializeField]
    private float startScale =
        0.05f;


    [SerializeField]
    private AnimationCurve scaleCurve =
        null;


    // =====================================================
    // ACTIVE ANIMATIONS
    // =====================================================

    private readonly List<
        AnimationData
    >
    animations =
        new List<
            AnimationData
        >();


    // =====================================================
    // DATA
    // =====================================================

    private class AnimationData
    {

        public GameObject Object;

        public float Time;

        public float Duration;

    }


    // =====================================================
    // PLAY
    // =====================================================

    public void Play(
        UnityEngine.Vector2Int blockPosition,
        ushort blockID
    )
    {

        if (
            blockID == 0
        )
        {
            return;
        }


        if (
            !BlockDatabase.Contains(
                blockID
            )
        )
        {
            return;
        }


        var block =
            BlockDatabase.Get(
                blockID
            );


        Texture2D texture =
            TextureManager.Get(
                block.Texture
            );


        if (
            texture == null
        )
        {
            return;
        }


        // =================================================
        // GAME OBJECT
        // =================================================

        GameObject animationObject =
            new GameObject(
                "BlockPlacementAnimation"
            );


        animationObject.transform.position =
            new Vector3(
                blockPosition.x +
                0.5f,

                blockPosition.y +
                0.5f,

                -0.5f
            );


        animationObject.transform.localScale =
            Vector3.one *
            startScale;


        // =================================================
        // SPRITE
        // =================================================

        SpriteRenderer renderer =
            animationObject.AddComponent<
                SpriteRenderer
            >();


        renderer.sprite =
            Sprite.Create(
                texture,

                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height
                ),

                new Vector2(
                    0.5f,
                    0.5f
                ),

                16f
            );


        renderer.sortingOrder =
            10;


        // =================================================
        // ANIMATION DATA
        // =================================================

        AnimationData data =
            new AnimationData
            {

                Object =
                    animationObject,

                Time =
                    0f,

                Duration =
                    animationDuration

            };


        animations.Add(
            data
        );

    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {

        for (
            int i =
                animations.Count -
                1;

            i >= 0;

            i--
        )
        {

            AnimationData data =
                animations[i];


            if (
                data.Object == null
            )
            {

                animations.RemoveAt(
                    i
                );

                continue;

            }


            data.Time +=
                Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    data.Time /
                    data.Duration
                );


            float curveValue =
                progress;


            if (
                scaleCurve != null &&
                scaleCurve.length > 0
            )
            {

                curveValue =
                    scaleCurve.Evaluate(
                        progress
                    );

            }


            float scale =
                Mathf.Lerp(
                    startScale,
                    1f,
                    curveValue
                );


            data.Object.transform.localScale =
                Vector3.one *
                scale;


            if (
                progress >=
                1f
            )
            {

                Destroy(
                    data.Object
                );


                animations.RemoveAt(
                    i
                );

            }

        }

    }


    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {

        for (
            int i = 0;
            i < animations.Count;
            i++
        )
        {

            if (
                animations[i].Object != null
            )
            {

                Destroy(
                    animations[i].Object
                );

            }

        }


        animations.Clear();

    }

}