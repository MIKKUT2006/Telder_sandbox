using TMPro;
using UnityEngine;

public class CursorFollow : MonoBehaviour
{
    public float offsetY = -30f;

    private RectTransform rectTransform;
    private Canvas canvas;


    void Start()
    {
        GetComponent<TextMeshProUGUI>().text = "";
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("FollowMouseCanvas script needs to be a child of a Canvas", this);
            enabled = false;
            return;
        }
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Debug.LogError("Canvas should not be in WorldSpace render mode. Use 'Screen Space - Overlay' or 'Screen Space - Camera'.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        Vector2 mousePosition = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             canvas.transform as RectTransform,
             mousePosition,
             canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
             out Vector2 localPoint);

        localPoint.y += offsetY;
        rectTransform.anchoredPosition = localPoint;
    }
}
