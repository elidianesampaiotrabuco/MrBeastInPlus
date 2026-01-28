using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Raldi.NPCs
{
    public class MrBeast_Challenge : MrBeast_StateBase
    {
        private const float LEAVE_RADIUS = 441f;

        private const int MIN_POINTS = 50;
        private const int MAX_POINTS = 150;

        private List<PlayerManager> playersInCircle = new List<PlayerManager>();

        private List<ItemObject> validItems = new List<ItemObject>();

        private ItemMetaData[] suitableItems;

        private ItemObject nothing;

        private Coroutine challengeCoroutine;

        private bool quit;

        public MrBeast_Challenge(NPC chara, MrBeast mrBeast) : base(chara, mrBeast)
        {
            character = chara;
            beast = mrBeast;
        }

        public override void Enter()
        {
            base.Enter();
            quit = false;
            PrecomputeValidItems();
            ChangeNavigationState(new NavigationState_DoNothing(npc, 0));
        }

        public override void Update()
        {
            for (int i = playersInCircle.Count - 1; i >= 0; i--)
            {
                PlayerManager player = playersInCircle[i];
                if (player == null)
                {
                    playersInCircle.RemoveAt(i);
                    continue;
                }

                if (beast.redCircle != null && (player.transform.position - beast.redCircle.position).sqrMagnitude >= LEAVE_RADIUS)
                {
                    quit = true;
                    beast.EndChallenge(false, true);
                    break;
                }
            }
        }

        public override void Exit()
        {
            if (challengeCoroutine != null)
            {
                beast.StopCoroutine(challengeCoroutine);
                challengeCoroutine = null;
            }

            try
            {
                gauge?.Deactivate();
                gauge = null;

                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
                beast.redCircle?.gameObject.SetActive(false);
                if (!quit)
                {
                    GiveReward();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in Exit: {e.Message}");
            }
            finally
            {
                beast.EndChallenge(!quit, false);
            }
        }

        private void GiveReward()
        {
            if (playersInCircle?.Count != 1) return;

            var player = playersInCircle[0];
            if (player?.itm == null || beast.audioMan?.QueuedAudioIsPlaying == true) return;
            if (beast.plugin?._BeastWon == null) return;

            beast.Do(() => beast.audioMan.QueueAudio(beast.plugin._BeastWon),() => AddItem(GetRandomItem(player.itm), player));

            playersInCircle.Clear();
        }

        public override void PlayerEnteredCircle(PlayerManager player, MrBeast beast)
        {
            if (playersInCircle?.Contains(player) == true) return;
            if (player == null) return;

            beast.CreateChallengeCircle(enable: true);

            beast.audioMan?.FlushQueue(true);
            beast.audioMan?.QueueAudio(beast.plugin?._BeastChallengeIntro);

            challengeCoroutine = beast.StartCoroutine(ChallengeCountdown(UnityEngine.Random.Range(MrBeast.minChallengeTime, MrBeast.maxChallengeTime)));

            playersInCircle?.Add(player);
            beast.Navigator?.ClearDestination();
        }

        public void AddItem(ItemObject itemObj, PlayerManager pm)
        {
            if (beast == null) return;
            var cgm = beast.GetCGM();
            if (cgm == null) return;

            if (pm == null) return;
            ItemManager itm = pm.itm;
            if (itm == null) return;

            if (itm.InventoryFull())
            {
                cgm.AddPoints(points: UnityEngine.Random.Range(MIN_POINTS, MAX_POINTS), player: pm.playerNumber, playAnimation: true);
                return;
            }
            itm.AddItem(item: itemObj);
        }

        private void PrecomputeValidItems()
        {
            if (suitableItems == null)
            {
                suitableItems = ItemMetaStorage.Instance.GetAllWithoutFlags(ItemFlags.Persists | ItemFlags.RuntimeItem | ItemFlags.NoUses | ItemFlags.InstantUse);
            }

            validItems.Clear();
            foreach (var meta in suitableItems)
            {
                if (meta.itemObjects != null && meta.flags != ItemFlags.InstantUse && meta.flags != ItemFlags.RuntimeItem && meta.flags != ItemFlags.NoUses && meta.flags != ItemFlags.Persists)
                {
                    foreach (var itemObj in meta.itemObjects)
                    {
                        if (itemObj == null) continue;

                        if (itemObj.itemType != (Items.PentagonKey | Items.SquareKey | Items.TriangleKey | Items.WeirdKey | Items.HexagonKey | Items.CircleKey))
                        {
                            validItems.Add(itemObj);
                        }
                    }
                }
            }
        }

        public override ItemObject GetRandomItem(ItemManager itm)
        {
            try
            {
                if (itm == null)
                {
                    Debug.LogError("ItemManager is null!");
                    return null;
                }
                nothing = itm.nothing;

                if (validItems.Count <= 0)
                {
                    Debug.LogError("No valid items found.");
                    return null;
                }

                int rand = UnityEngine.Random.Range(0, validItems.Count);
                ItemObject selectedItem = validItems[rand];

                if ((selectedItem == beast.creditCard || selectedItem == nothing) && IvanLomAPI.CreateFlagInStatement(out bool flag, true))
                {
                    for (int i = 1; i < validItems.Count; i++)
                    {
                        int alternativeIndex = (rand + i) % validItems.Count;
                        if (validItems[alternativeIndex] != beast.creditCard && validItems[alternativeIndex] != nothing)
                        {
                            flag = true;
                            selectedItem = validItems[alternativeIndex];
                            break;
                        }
                    }
                    if (!flag)
                    {
                        Debug.LogWarning("Couldn't find a replacement for Credit Card and/or Nothing items.");
                    }
                }

                return selectedItem;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting random item: {e.Message}");
                return null;
            }
        }

        private IEnumerator ChallengeCountdown(float totalTime)
        {
            var cgm = beast.GetCGM();
            beast.Navigator?.ClearDestination();

            if (cgm == null)
            {
                Debug.LogError("CoreGameManager is null");
                yield break;
            }

            var hud = cgm.GetHud(0);
            if (hud?.gaugeManager == null) yield break;

            gauge ??= hud.gaugeManager.ActivateNewGauge("spr_challengegauge".GetSprite(), totalTime);

            float remainingTime = totalTime;

            while (remainingTime > 0f && !quit)
            {
                remainingTime -= Time.deltaTime;
                gauge?.SetValue(totalTime, remainingTime);
                yield return null;
            }

            if (!quit)
            {
                beast.EndChallenge(true, true);
            }
        }

        private HudGauge gauge;
    }
}