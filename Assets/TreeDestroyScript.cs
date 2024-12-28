using System.Collections;
using UnityEngine;

public class TreeDestroyScript : MonoBehaviour
{
    public float maxHealth = 5f;
    public float damagePerSecond = 1f;
    private float currentHealth;
    private bool isChopping = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void StartChopping()
    {
        isChopping = true;
    }

    public void StopChopping()
    {
        isChopping = false;
    }

    void Update()
    {
        if (isChopping)
        {
            if (HelperClass.eguipmentItem != null)
            {
                if (HelperClass.eguipmentItem.toolType == 2)
                {
                    currentHealth -= HelperClass.eguipmentItem.axePower * Time.deltaTime;
                }
                else
                {
                    currentHealth -= 1 * Time.deltaTime;
                }
            }
            else
            {
                currentHealth -= 1 * Time.deltaTime;
            }
            

            if (currentHealth <= 0)
            {
                ChopDown();
            }
        }
    }

    void ChopDown()
    {
        Destroy(gameObject);
        Debug.Log("Дерево срублено!");
    }
}
