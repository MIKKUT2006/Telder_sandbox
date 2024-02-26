using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputScript : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private float playerSpeed = 4f;
    private bool isFlip = false;
    public GameObject bullet;

    private void Awake()
    {
        Camera.main.gameObject.transform.localPosition = transform.localPosition;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        rb.velocity = new Vector2 (Input.GetAxis("Horizontal") * playerSpeed, rb.velocity.y);

        if (Input.GetMouseButtonDown(0))
        {
            if (PlayerData.fireGun == true)
            {
                //Instantiate(bullet, );
            }
            GetComponent<Animator>().SetBool("Attack", true);
            StartCoroutine(attackCooldown());
        }

        //Vector3 pos = new Vector3(transform.position.x, transform.position.y, transform.position.z -10);
        //Camera.main.gameObject.transform.position = Vector3.Lerp(Camera.main.gameObject.transform.position, pos, playerSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (Input.GetAxis("Horizontal") < 0 && isFlip == false)
        {
            Flip();
        }
        if (Input.GetAxis("Horizontal") > 0 && isFlip == true)
        {
            Flip();
        }

        

        if (rb.velocity.x == 0)
        {
            GetComponent<Animator>().SetBool("Run", false);
        }
        else
        {
            GetComponent<Animator>().SetBool("Run", true);
        }
    }

    private void Flip()
    {
        isFlip = !isFlip;
        transform.localScale = new Vector3(transform.localScale.x * -1, 1, 1);
    }

    IEnumerator attackCooldown()
    {
        yield return new WaitForSeconds(0.3f);
        GetComponent<Animator>().SetBool("Attack", false);
    }
}
