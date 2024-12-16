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
        if (Input.GetAxis("Jump") == 1 && jump == true && rb.linearVelocity.y < 5)
        {
            rb.AddForce(new Vector2(0, 5f), ForceMode2D.Impulse);
            jump = false;
        }

        if (rb.linearVelocity.y > 5)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 5);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // ���� �����
        if (collision.gameObject.layer == 3)
        {
            jump = true;
        }
        

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ���� ����
        //if (collision.gameObject.layer == 4)
        //{
        //    rb.gravityScale = 0.5f;
        //}
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // ���� �����
        if (collision.gameObject.layer == 3)
        {
            jump = false;
        }
        // ���� ����
        //if (collision.gameObject.layer == 4)
        //{
        //    rb.gravityScale = 1;
        //}
    }
}
