using UnityEngine;

namespace Raldi
{
    public class ShakingObject : MonoBehaviour
    {
        private Vector3 originalPosition;

        private const float shakeIntensity = 2f;

        private float shakeSpeed = 15f;

        private float cornerTimer;
        private int currentCorner;

        private bool isInitialized;

        private RectTransform rectTransform;

        private void Update()
        {
            if (!isInitialized || rectTransform == null) return;

            cornerTimer += Time.deltaTime;

            Vector3[] corners = new Vector3[]
            {
                originalPosition + new Vector3(-shakeIntensity, shakeIntensity, 0), // top left
                originalPosition + new Vector3(shakeIntensity, -shakeIntensity, 0), // bottom right
                originalPosition + new Vector3(shakeIntensity, shakeIntensity, 0), // top right
                originalPosition + new Vector3(-shakeIntensity, -shakeIntensity, 0) // bottom left
            };

            rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, corners[currentCorner], Time.deltaTime * shakeSpeed);

            if (cornerTimer >= shakeSpeed / (shakeSpeed * 20f))
            {
                currentCorner = (currentCorner + 1) % 4;
                cornerTimer = 0f;
            }
        }

        public void Initialize(float speed = 15f)
        {
            rectTransform = GetComponent<RectTransform>() == null ? gameObject.AddComponent<RectTransform>() : GetComponent<RectTransform>();
            originalPosition = rectTransform.localPosition;
            shakeSpeed = speed;

            isInitialized = true;
        }

        public void OnDestroy() => isInitialized = false;
    }
}