using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera cam1;
    public Camera cam2;
    private bool isCamera1Active = true;


    void Start()
    {
        cam1.enabled = true; 
        cam2.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        isCamera1Active = !isCamera1Active;
        
        cam1.enabled = isCamera1Active;
        cam2.enabled = !isCamera1Active;
    }
}
