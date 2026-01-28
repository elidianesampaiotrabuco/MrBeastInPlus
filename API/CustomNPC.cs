using RaldiItems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Raldi
{
    public struct CustomNPCData
    {
#pragma warning disable CS8618
        public AudioManager audMan;
        public AudioManager wahahAudMan;
#pragma warning restore CS8618

        public bool overrideSpeed;
    }

    public class CustomNPC : NPC
    {
        protected List<ItemLifetime> items = [];
        public CustomNPCData data;
        public Plugin plugin;
        public PlayerManager pm;
        private CoreGameManager cgm;

        public float timerTime;

        public delegate void OnInitialize();
        public delegate void WhenDestroyedNPC();

        public void Do(params Action[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                if (action == null) continue;
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        }

        public CoreGameManager GetCGM()
        {
            if (cgm == null)
            {
                cgm = Singleton<CoreGameManager>.Instance;
            }
            return cgm;
        }

        public IEnumerator SingleTimerCoroutine(float time, Action action)
        {
            yield return new WaitForSeconds(time);
            action?.Invoke();
        }

        public IEnumerator TimerCoroutine(float time, Action action)
        {
            while (true)
            {
                yield return new WaitForSeconds(time);
                action?.Invoke();
            }
        }

        public IEnumerator TimerCoroutine(float minRandom, float maxRandom, Action action)
        {
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(minRandom, maxRandom));
                action?.Invoke();
            }
        }

        public void ChangeState(NpcState newState)
        {
            if (behaviorStateMachine == null) return;
            if (behaviorStateMachine.CurrentState == newState) return;

            behaviorStateMachine.CurrentState?.Exit();
            behaviorStateMachine.ChangeState(newState);
        }

        public override void Initialize()
        {
            base.Initialize();

            behaviorStateMachine.ChangeState(new CustomNPC_Wander(this));
        }

        protected virtual new void Update()
        {
        }

        protected virtual void OnDestroy() => StopAllCoroutines();
    }

    public class CustomNPC_StateBase : NpcState
    {
        protected NPC character;

        public CustomNPC_StateBase(NPC chara) : base(chara)
        {
            character = chara;
        }
    }

    public class CustomNPC_Wander : CustomNPC_StateBase
    {
        public CustomNPC_Wander(NPC chara) : base(chara)
        {
        }

        public override void Enter()
        {
            base.Enter();
            if (!npc.Navigator.HasDestination)
            {
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
            }
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }
    }
}
