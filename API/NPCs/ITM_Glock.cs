using UnityEngine;
using Raldi;

namespace RaldiItems
{
    public class ITM_Glock : Item
    {
        private Plugin plugin;
        public int uses;

        public ItemObject[] variants = new ItemObject[6];

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            plugin = Plugin.Instance;

            if (plugin.bulletPref == null)
            {
                plugin.CreateBulletPrefab();
            }

            Vector3 spawnPos = pm.transform.position + pm.transform.forward * 0.5f;
            Quaternion spawnRot = pm.transform.rotation;

            GameObject bullet = Instantiate(plugin.bulletPref, spawnPos, spawnRot);
            bullet.SetActive(true);
            BulletProjectile projectile = bullet.GetComponent<BulletProjectile>();
            projectile.Initialize(plugin);

            plugin.AudMan?.PlaySingle(Plugin.assetMan.Get<SoundObject>("Gun_Shoot"));

            uses--;

            if (uses > 0 && uses - 1 < variants.Length && variants[uses - 1] != null)
            {
                pm.itm.SetItem(variants[uses - 1], pm.itm.selectedItem);
                return false;
            }

            if (uses <= 0)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }
}