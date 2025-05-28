using UnityEngine;

public class ItemScript : MonoBehaviour
{
    public float knockbackForce = 20f;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            EnemyAI enemyHealth = collision.GetComponent<EnemyAI>();
            if (enemyHealth != null)
            {
                Vector2 direction = (enemyHealth.transform.position - transform.position).normalized;
                Vector2 knockback = direction * 100f; // ← Увеличь множитель
                enemyHealth.TakeDamage(HelperClass.playerInventory[HelperClass.selectedInventoryCell].damage, knockback);
            }
        }
    }
}
