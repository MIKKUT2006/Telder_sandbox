using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpScript : MonoBehaviour
{
    [SerializeField] private bool jump = false;
    [SerializeField] private Rigidbody2D rb;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Jump") == 1 && jump == true && rb.velocity.y < 5)
        {
            rb.AddForce(new Vector2(0, 5f), ForceMode2D.Impulse);
            jump = false;
        }

        if (rb.velocity.y > 5)
        {
            rb.velocity = new Vector2(rb.velocity.x, 5);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3)
        {
            jump = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3)
        {
            jump = false;
        }
    }
}
