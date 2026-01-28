using UnityEngine;
using Raldi;

namespace RaldiItems
{
    public class BulletProjectile : MonoBehaviour
    {
        public float speed = 30f;
        public float maxLifetime = 2f;
        public const float stunDuration = 15f;
        private Plugin plugin;

        public void Initialize(Plugin pluginRef)
        {
            plugin = pluginRef;
            Destroy(gameObject, maxLifetime);
        }

        private void Update() => transform.Translate(Vector3.forward * speed * Time.deltaTime);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) return;

            if (other.TryGetComponent(out Entity entity) && SingletonExtension.TryGetSingleton(out CoreGameManager cgm))
            {
                plugin.StartCoroutine(plugin.StunEntity(entity, cgm.GetHud(0)));
                Destroy(gameObject);
            }
        }
    }
}