using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Update()
    {
        // Dynamically find the main camera each frame if it's null or destroyed
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            
            // If still no camera found, exit early
            if (mainCamera == null)
            {
                return;
            }
        }
        
        transform.LookAt(mainCamera.transform.position);
    }
}
