using System.Collections;
using UnityEngine;

public class DigManager : MonoBehaviour
{
    // Статический метод для запуска корутины
    public static void StartDigging()
    {
        // Проверяем, существует ли уже экземпляр
        if (instance == null)
        {
            Debug.LogWarning("Создан новый экземпляр DigManager");
            // Если нет, создаем его
            GameObject digManagerObject = new GameObject("DigManager");
            instance = digManagerObject.AddComponent<DigManager>();
        }
        instance.StartCoroutine(instance.DigTree());
    }
    private static DigManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }
    private IEnumerator DigTree()
    {
        while (true)
        {
            HelperClass.isDig = !HelperClass.isDig; // Меняем значение статической переменной
            yield return new WaitForSeconds(0.5f);
        }
    }
}
