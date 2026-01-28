using UnityEngine;

public class SimpleBillboard : MonoBehaviour
{
    private Transform camTransform;

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            camTransform = Camera.main.transform;
            return;
        }

        if (camTransform != null)
            transform.rotation = camTransform.rotation;
    }
}