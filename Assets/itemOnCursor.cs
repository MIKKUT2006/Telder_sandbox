using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class itemOnCursor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        HelperClass.itemOnCursorGameObject = gameObject;
        gameObject.GetComponent<Image>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
        transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        //transform.position.z = new Vector3();
    }
}
