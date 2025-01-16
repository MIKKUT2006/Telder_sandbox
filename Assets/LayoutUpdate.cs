using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutUpdate : MonoBehaviour
{
    public List<RectTransform> layoutTransforms; // Перетащите в инспекторе все RectTransforms с Layout Groups
    public static List<RectTransform> layoutStaticTransforms; //специальный статичный тип данных, чтобы использовать данный метод везде

    private void Awake()
    {
        layoutStaticTransforms = layoutTransforms;
    }

    public static void RefreshLayout()
    {
        foreach (RectTransform rectTransform in layoutStaticTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
