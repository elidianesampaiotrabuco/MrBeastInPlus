using RaldiItems;
using UnityEngine;

namespace Raldi.NPCs
{
    public class ItemPoopingNPC : CustomNPC
    {
        public virtual string logPoop => "Spawned {0}.";
        protected ItemObject itemToShit;
        private Transform _transform;
        private int itemsPooped;

        public Transform Transform
        {
            get
            {
                if (_transform == null)
                {
                    _transform = GetComponent<Transform>();
                }
                return _transform;
            }
        }

        protected virtual void Start() { }

        protected override void Update()
        {
            base.Update();
            HandleItemWarnings();
        }

        protected virtual void HandleItemWarnings()
        {
            if (items.Count == 0) return;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.spriteRenderer == null || item.pickup != itemToShit) continue;

                float lifetime = item.lifetime;
                if (lifetime < ItemLifetime.warningTime)
                {
                    float flashSpeed = Mathf.Lerp(20f, 5f, lifetime / ItemLifetime.warningTime);
                    float lerpValue = Mathf.PingPong(Time.time * flashSpeed, 1f);
                    item.spriteRenderer.color = Color.Lerp(Color.red, Color.white, lerpValue);
                }
            }
        }

        public virtual void ShitItem(ItemObject item)
        {
            Vector3 position = Transform.position;

            var newItem = new GameObject("TempItem_NUMBER");
            newItem.transform.position = new Vector3(position.x, 5f, position.z);
            var collider = newItem.AddComponent<CapsuleCollider>();
            collider.radius = 3.5f;
            collider.isTrigger = true;
            newItem.AddComponent<SimpleBillboard>();

            var spriteObject = new GameObject("Sprite"); // create sprite thing
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>(); // add the real "sprite" thing
            spriteRenderer.sprite = item.itemSpriteLarge; // set the sprite to the sprite
            spriteObject.transform.SetParent(newItem.transform, false); // set the parent
            spriteObject.transform.localPosition = Vector3.zero;

            var itmPickup = newItem.AddComponent<Pickup>();
            itmPickup.itemSprite = spriteRenderer; // set the sprite thing to the sprite thing
            itmPickup.item = item; // set the item to the item
            itmPickup.price = item.price; // wait. you gotta pay for that!! wait a second its actually free
            itmPickup.free = true; // yup, its free, it shouldn't really should have a cost, right?
            if (itemsPooped <= 5)
            {
                itmPickup.showDescription = true; // show what item is it
            }
            itmPickup.sound = "item_pickup".GetSound("", Color.white, ".ogg", SoundType.Effect, hasSubtitle: false);

            var lifetime = newItem.AddComponent<ItemLifetime>(); // add lifetime thing to destroy the item when timer is ended
            lifetime.spriteRenderer = spriteRenderer;
            lifetime.pickup = itmPickup;

            items.Add(lifetime);

            spriteRenderer.gameObject.AddComponent<PickupBob>();

            itemsPooped++;

            Debug.Log(string.Format(logPoop, item.item.name));
        }
    }
}