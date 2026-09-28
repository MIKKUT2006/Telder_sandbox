using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Optional test target.
    /// Remove it when your real enemy health implements IWeaponDamageable
    /// or already exposes TakeDamage(float).
    /// </summary>
    public sealed class WeaponDebugDamageable :
        MonoBehaviour,
        IWeaponDamageable
    {
        [SerializeField]
        private float health = 100f;

        public void ReceiveWeaponDamage(
            WeaponDamageInfo damageInfo)
        {
            health -= damageInfo.Damage;

            Debug.Log(
                $"{name}: -{damageInfo.Damage} HP, " +
                $"left {health}"
            );

            if (health <= 0f)
                Destroy(gameObject);
        }
    }
}
