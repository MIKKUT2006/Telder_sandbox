using UnityEngine;

public class MenuBGScript : MonoBehaviour
{
    public float parallaxStrength = 0.02f;
    public float maxOffset = 30f; // �������� maxOffset ��� �������� �������
    public float smoothTime = 0.3f; // ����� ����������� ��������

    private Vector3 _initialPosition;
    private RectTransform _rectTransform;
    private Vector3 _currentVelocity; // ������� �������� ��� SmoothDamp
    private Vector2 _backgroundSize;   // ������ ����

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("MenuBackgroundParallax ������ ������ ���� �� Canvas UI ��������!");
            enabled = false;
            return;
        }

        _initialPosition = transform.position;

        // �������� ������ RectTransform ����
        _backgroundSize = _rectTransform.sizeDelta;

        // ����������� ������ ����, ����� �� ��� ������ ������ �� maxOffset
        _rectTransform.sizeDelta = new Vector2(_backgroundSize.x + 2 * maxOffset, _backgroundSize.y + 2 * maxOffset);

        // ���������� ��� � �����, ����� �������������� ���������� �������
        transform.position = _initialPosition; // + new Vector3(-maxOffset, -maxOffset, 0);

        _currentVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        // �������� ������� ���� � ������� �����������
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // ��������� ������� �������� �� ������ ������� ����
        Vector3 targetOffset = (mousePosition - _initialPosition) * parallaxStrength;

        // ������������ �������� ������������ ���������
        targetOffset.x = Mathf.Clamp(targetOffset.x, -maxOffset, maxOffset);
        targetOffset.y = Mathf.Clamp(targetOffset.y, -maxOffset, maxOffset);

        // ���������� SmoothDamp ��� �������� ��������
        Vector3 newPosition = _initialPosition + targetOffset;
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref _currentVelocity, smoothTime);
    }

    void OnDisable()
    {
        if (_rectTransform != null)
            _rectTransform.sizeDelta = _backgroundSize; //���������� �������� ������, ���� ������ �����������
    }
}
