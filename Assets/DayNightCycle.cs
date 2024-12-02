using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // ������ �� SpriteRenderer
    public float cycleDuration = 10f; // ����� ������ ����� (���� + ����) � ��������
    public float duration = 60f; // ����������������� ���/���� � ��������

    private void Start()
    {
        StartCoroutine(ChangeDayNightCycle());
    }

    private System.Collections.IEnumerator ChangeDayNightCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(duration);
            // ������� ������� ��� (����������)
            yield return StartCoroutine(ChangeSpriteAlpha(1f, 0f, cycleDuration / 2)); // 1 - ����������, 0 - ������������
            yield return new WaitForSeconds(duration);
            // ������� ������� ���� (������������)
            yield return StartCoroutine(ChangeSpriteAlpha(0f, 1f, cycleDuration / 2)); // 0 - ����������, 1 - ������������
        }
    }

    private System.Collections.IEnumerator ChangeSpriteAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // ������������ �����-�������� �� 0 �� 1
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);

            // ��������� �����-�������� � ����� �������
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;

            elapsedTime += Time.deltaTime;
            yield return null; // ������� ��������� ����
        }
        // ���������, ��� �����-�������� ����������� � ��������
        Color finalColor = spriteRenderer.color;
        finalColor.a = toAlpha;
        spriteRenderer.color = finalColor;
    }
}