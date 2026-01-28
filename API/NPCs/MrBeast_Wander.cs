using System;
using UnityEngine;

namespace Raldi.NPCs
{
    public class MrBeast_Wander : MrBeast_StateBase
    {
        public MrBeast_Wander(NPC chara, MrBeast mrBeast) : base(chara, mrBeast)
        {
            character = chara;
            beast = mrBeast;
        }

        public override void Enter() => OnEmpty(base.Enter);

        public override void DestinationEmpty() => OnEmpty(base.DestinationEmpty);

        private void OnEmpty(Action toDo)
        {
            toDo?.Invoke();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);

            beast.pm = player;
            if (!beast.audioMan.AnyAudioIsPlaying && !beast.Entity.Squished)
            {
                if (beast.HaveCreditCards(player.itm) <= 0)
                {
                    if (beast.playcool > 0f) return;

                    beast.audioMan.QueueAudio(beast.plugin._BeastChallengeInvite);
                    npc.TargetPosition(player.plm.transform.position);
                }
                else
                {
                    beast.ChangeState(beast.angryState);
                }
            }
        }

        public override void OnStateTriggerEnter(Collider other, bool validCollision)
        {
            if (other.CompareTag("Player") && beast.playcool <= 0f && !beast.Entity.Squished && other.TryGetComponent<PlayerManager>(out var component))
            {
                beast.behaviorStateMachine.ChangeState(beast.challengeState);
                beast.challengeState.PlayerEnteredCircle(component, beast);
            }
        }
    }
}