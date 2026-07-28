using UnityEngine;

public class CameraClearTest :
    MonoBehaviour
{

    private Camera cam;


    private void Awake()
    {

        cam =
            GetComponent<Camera>();


        if (
            cam == null
        )
        {
            return;
        }


        cam.clearFlags =
            CameraClearFlags.SolidColor;


        cam.backgroundColor =
            Color.black;

    }

}