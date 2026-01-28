using UnityEngine;
using System.Collections;

namespace Raldi.NPCs
{
    public class MrBeast_Chase : MrBeast_StateBase
    {
        private bool thinking;

        public MrBeast_Chase(NPC chara, MrBeast mrBeast) : base(chara, mrBeast)
        {
            character = chara;
            beast = mrBeast;
        }

        public override void Enter()
        {
            base.Enter();
            beast.StartCoroutine(WaitASecond(beast.pm));
        }

        public override void Update()
        {
            if (beast.plugin.LoopAudio != null && !beast.plugin.LoopAudio.AnyAudioIsPlaying && !thinking)
            {
                beast.plugin.LoopAudio?.PlaySingle("mus_chase".GetSound(string.Empty, Color.clear, ".ogg", SoundType.Music, hasSubtitle: false, folder: "Music"));
            }
        }

        public override void DestinationEmpty()
        {
            if (thinking) return;
            if (beast != null && beast.looker != null && (!beast.looker.PlayerInSight() || beast.Entity.Squished)) return;

            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (thinking) return;

            beast.pm = player;
            if (!beast.Entity.Squished)
            {
                beast.TargetPosition(player.plm.transform.position);
            }
        }

        public override void OnStateTriggerEnter(Collider other, bool validCollision)
        {
            if (other.CompareTag("Player") && SingletonExtension.TryGetSingleton(out BaseGameManager bgm) && beast.plugin != null && !beast.Entity.Squished && !thinking)
            {
                if (SingletonExtension.TryGetSingleton(out CoreGameManager cgm))
                {
                    beast.plugin.LoopAudio.FlushQueue(true);
                    if (cgm.currentMode == Mode.Free && other.TryGetComponent<PlayerManager>(out var player))
                    {
                        beast.Navigator.ClearCurrentDirs();
                        beast.audioMan.FlushQueue(true);
                        beast.plugin.ResetAudio();
                        cgm.AddPoints(-2500, player.playerNumber, true);
                        beast.audioMan.QueueAudio(beast.plugin._BeastExplorer);
                        beast.ChangeState(new MrBeast_Praise(beast, beast));
                        return;
                    }
                    cgm.AddPoints(-1000, 0, false);
                }
                var baldi = bgm.Ec.GetBaldi();
                if (baldi == null) return;
                bgm.EndGame(other.transform, baldi);
                baldi.spriteRenderer[0].sprite = beast.spriteRenderer[0].sprite;
            }
        }

        public IEnumerator WaitWhereIsIt(MrBeast beast)
        {
            if (beast.behaviorStateMachine.CurrentState == beast.angryState || beast.audioMan == null) yield break;
            if (beast.pm != null && beast.looker.PlayerInSight() && beast.HaveCreditCards(beast.pm.itm) > 0) yield break;

            thinking = true;
            yield return new WaitForSeconds(1f);

            SayAngrySpeech(true, Plugin.assetMan.Get<SoundObject>("Beast_STOLE"));

            yield return new WaitForSeconds(4.5f);

            beast.audioMan.positional = true;

            EnterAngryState(null);
        }

        public IEnumerator WaitASecond(PlayerManager pm)
        {
            if (pm == null)
            {
                pm = beast.pm;
            }
            if (beast.audioMan == null || pm == null) yield break;

            thinking = true;
            PreRealizeAngryState();

            yield return new WaitForSeconds(UnityEngine.Random.Range(0.25f, 1.25f));

            SayAngrySpeech(true, Plugin.assetMan.Get<SoundObject>("Beast_WHYHAVE"));

            yield return new WaitForSeconds(4.5f);

            EnterAngryState(pm);
        }

        public void SayAngrySpeech(bool sayWait, SoundObject stoleSpeech)
        {
            if (beast.audioMan == null)
            {
                Debug.LogError("Q: Why audioMan variable is null?\n A: \"I don't know how this thing can become null.\n That's literally impossible.\"");
                return;
            }

            beast.audioMan.FlushQueue(endCurrent: true);

            if (sayWait)
                beast.audioMan.QueueAudio(beast.plugin._BeastWait);

            beast.audioMan.QueueAudio(stoleSpeech);
        }

        public void PreRealizeAngryState() => beast.Do(() => beast.Navigator?.SetSpeed(0f), () => beast.audioMan.QueueAudio(beast.plugin._BeastChallengeInvite), () => ChangeNavigationState(new NavigationState_DoNothing(beast, 0, false)));

        public void EnterAngryState(PlayerManager pm)
        {
            if (pm == null && SingletonExtension.TryGetSingleton(out CoreGameManager cgm))
                pm = cgm.GetPlayer(0);
            if (pm == null) return;

            thinking = false;
            beast.pm = pm;
            beast.currentState = this;
            beast.wanderState = this;

            bool hasMultipleCards = beast.HaveCreditCards(pm.itm) > 1;
            beast.alwaysKnows = hasMultipleCards;
            beast.Navigator?.SetSpeed(hasMultipleCards ? 24f : 18f);

            beast.TargetPosition(pm.plm.transform.position);
            Hear(pm.gameObject, pm.transform.position, 127);
        }
    }
}