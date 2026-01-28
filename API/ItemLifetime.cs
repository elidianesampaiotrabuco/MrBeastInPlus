using UnityEngine;

namespace RaldiItems
{
    public class ItemLifetime : MonoBehaviour
    {
        public const float totalLifetime = 30f;
        public const float warningTime = 8f;

        public Pickup pickup;
        public SpriteRenderer spriteRenderer;
        public float lifetime;

        private void Start() => lifetime = totalLifetime;

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}