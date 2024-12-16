using System.Collections;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class PlayerData : MonoBehaviour
{
    // Переменные игрока
    [SerializeField] private float Health = 100;
    [SerializeField] private Image healthBar;
    private bool truedamage = true;
    [SerializeField] public ParticleSystem bloodParticles;
    public static bool fireGun = false;
    //[SerializeField] public GameObject player;
    // Переменные игрока
    void Start()
    {
        //healthBar = GameObject.FindGameObjectWithTag("HealthBar").gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("g"))
        {
            SetDamage(10f);
        }
    }

    public void SetDamage(float damage)
    {
        if (Health > 0)
        {
            Health -= damage;
            healthBar.fillAmount = Health / 100;
            if (Health <= 0)
            {
                Instantiate(bloodParticles, transform.position, Quaternion.identity);
                Destroy(this.gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy" && truedamage == true)
        {
            SetDamage(5);
            StartCoroutine(damageCooldown());
        }
    }

    IEnumerator damageCooldown()
    {
       truedamage = false;
       yield return new WaitForSeconds(1);
       truedamage = true;
    }
}
