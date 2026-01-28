using Raldi;
using Raldi.NPCs;
using System.Collections;
using UnityEngine;
using TMPro;

namespace RaldiItems
{
    public class ITM_CreditCard : Item
    {
        private MrBeast beast;
        private Plugin plugin;
        private HudGauge gauge;

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            plugin = Plugin.Instance;

            if (plugin == null)
            {
                Debug.LogError("Plugin Instance is null!");
                return false;
            }

            ObjectPoolManager.AddToCache(this);

            MrBeast mrBeast = ObjectPoolManager.Find<MrBeast>();
            if (mrBeast != null)
            {
                beast = mrBeast;
            }

            StartCoroutine(MakeSureCoroutine());
            return true;
        }

        private void OnDestroy()
        {
            ObjectPoolManager.RemoveFromCache(this);

            if (gauge != null)
            {
                gauge.Deactivate();
            }
        }

        private IEnumerator MakeSureCoroutine()
        {
            if (!SingletonExtension.TryGetSingleton<CoreGameManager>(out var cgm)) yield break;
            if (pm == null) yield break;

            var hud = cgm.GetHud(0);
            if (hud == null) yield break;
            float timer = 25f;
            float total = timer;
            if (gauge == null && hud != null && hud.gaugeManager != null)
            {
                gauge = hud.gaugeManager.ActivateNewGauge(plugin._CreditCard.itemSpriteSmall, total);
            }
            while (beast == null || pm == null || pm.plm == null)
            {
                if (beast == null)
                {
                    MrBeast mrBeast = ObjectPoolManager.Find<MrBeast>();
                    beast = mrBeast;
                }
                yield return null;
            }
            beast?.Hear(pm.plm.gameObject, pm.plm.transform.position, 100);
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (gauge != null)
                {
                    gauge.SetValue(totalTime: total, remainingTime: timer);
                }
                yield return null;
            }
            if (pm != null && cgm != null)
            {
                cgm.AddPoints(250, pm.playerNumber, true);
            }
            if (gauge != null)
            {
                gauge.Deactivate();
            }
            Destroy(gameObject);
        }
    }
}