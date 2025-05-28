using System.Collections;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class PlayerData : MonoBehaviour
{
    // Переменные игрока
    //[SerializeField] private static float Health = 100;
    //[SerializeField] private static float MaxHealth = 100;
    //[SerializeField] private static Image healthBar;
    //private bool truedamage = true;
    [SerializeField] public static ParticleSystem bloodParticles;
    [SerializeField] public ParticleSystem bloodParticlesInspector;
    //// Окно смерти игрока
    //[SerializeField] private static GameObject deathPanel;
    [SerializeField] public GameObject deathPanelInInspector;

    void Start()
    {
        bloodParticles = bloodParticlesInspector;
        HelperClass.healthBar = GameObject.FindGameObjectWithTag("HealthBar").gameObject.GetComponent<Image>();
        HelperClass.deathPanel = deathPanelInInspector;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("g"))
        {
            SetDamage(10f);
        }
    }  

    public static void SetDamage(float damage)
    {
        if (HelperClass.Health > 0)
        {
            HelperClass.Health -= damage;
            Instantiate(bloodParticles.gameObject, HelperClass.playerGameObject.transform);
            HelperClass.healthBar.fillAmount = HelperClass.Health / HelperClass.MaxHealth;
            //CameraShakeCinemachine.Instance.ShakeCamera(0.2f, 0.2f);
            if (HelperClass.Health <= 0)
            {
                Kill();
            }
        }
    }

    public static void Kill()
    {
        HelperClass.deathPanel.SetActive(true);
    }
}


