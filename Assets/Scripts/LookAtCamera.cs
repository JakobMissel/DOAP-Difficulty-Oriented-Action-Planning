using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    GameObject mainCamera;

    void Awake()
    {
        mainCamera = Camera.main.gameObject;    
    }

    void Update()
    {
        transform.LookAt(mainCamera.transform.position);
    }
}
