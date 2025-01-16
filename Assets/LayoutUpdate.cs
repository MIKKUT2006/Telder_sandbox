using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutUpdate : MonoBehaviour
{
    public List<RectTransform> layoutTransforms; // ���������� � ���������� ��� RectTransforms � Layout Groups
    public static List<RectTransform> layoutStaticTransforms; //����������� ��������� ��� ������, ����� ������������ ������ ����� �����

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
