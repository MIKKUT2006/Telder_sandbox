using System.Collections;
using UnityEngine;

public class DigManager : MonoBehaviour
{
    // ����������� ����� ��� ������� ��������
    public static void StartDigging()
    {
        // ���������, ���������� �� ��� ���������
        if (instance == null)
        {
            Debug.LogWarning("������ ����� ��������� DigManager");
            // ���� ���, ������� ���
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
            HelperClass.isDig = !HelperClass.isDig; // ������ �������� ����������� ����������
            yield return new WaitForSeconds(0.5f);
        }
    }
}
