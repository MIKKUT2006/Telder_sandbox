using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // Ссылка на SpriteRenderer
    public float cycleDuration = 10f; // Время одного цикла (день + ночь) в секундах
    public float duration = 1200f; // Продолжительность дня/ночи в секундах (20 минут)

    private void Start()
    {
        StartCoroutine(ChangeDayNightCycle());
    }

    private System.Collections.IEnumerator ChangeDayNightCycle()
    {
        while (true)
        {
            
            // Плавный переход дня (прозрачный)
            yield return StartCoroutine(ChangeSpriteAlpha(1f, 0f, cycleDuration / 2)); // 1 - прозрачный, 0 - непрозрачный
            yield return new WaitForSeconds(duration);
            // Плавный переход ночи (непрозрачный)
            yield return StartCoroutine(ChangeSpriteAlpha(0f, 1f, cycleDuration / 2)); // 0 - прозрачный, 1 - непрозрачный
            yield return new WaitForSeconds(duration);
        }
    }

    private System.Collections.IEnumerator ChangeSpriteAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Масштабируем альфа-значение от 0 до 1
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);

            // Применяем альфа-значение к цвету спрайта
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;

            elapsedTime += Time.deltaTime;
            yield return null; // ожидать следующий кадр
        }
        // Убедиться, что альфа-значение установлено в конечное
        Color finalColor = spriteRenderer.color;
        finalColor.a = toAlpha;
        spriteRenderer.color = finalColor;
    }
}