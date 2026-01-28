using HarmonyLib;

namespace Raldi
{
    [HarmonyPatch(typeof(Notebook), "Clicked")]
    public class Notebook_Pre012
    {
        static void Prefix()
        {
            if (SingletonExtension.TryGetSingleton<CoreGameManager>(out var cgm) && cgm.GetPlayer(0) != null)
            {
                cgm.GetPlayer(0).plm.AddStamina(cgm.GetPlayer(0).plm.staminaMax, true);
            }
        }
    }
}