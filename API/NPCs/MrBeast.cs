using RaldiItems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

namespace Raldi.NPCs
{
    public class MrBeast : ItemPoopingNPC
    {
        public const float CIRCLE_Y_OFFSET = 4.98f;

        public override string logPoop => "The last one who gets {0}, gets a free {0} in his itm.items array!";

        public const float minChallengeTime = 15f;
        public const float maxChallengeTime = 25f;

        public bool alwaysKnows;

        public float playcool;
        public Transform redCircle;

        protected Coroutine alwaysKnowsCoroutine;
        protected Coroutine beasticatorCoroutine;

        public ItemObject creditCard;
        public AudioManager audioMan;

        public MrBeast_StateBase currentState;
        public MrBeast_StateBase wanderState;
        public MrBeast_Challenge challengeState;
        public MrBeast_Chase angryState;

        public override void Initialize()
        {
            base.Initialize();
            wanderState = new MrBeast_Wander(this, this);
            challengeState = new MrBeast_Challenge(this, this);
            angryState = new MrBeast_Chase(this, this);
            currentState = wanderState;
            ChangeState(currentState);

            redCircle = CreateChallengeCircle(enable: false);

            if (audioMan == null)
            {
                audioMan = new GameObject("VoiceAudio").AddComponent<AudioManager>();
                audioMan.transform.SetParent(transform, false);
                audioMan.audioDevice = audioMan.gameObject.AddComponent<AudioSource>();
                audioMan.positional = true;
            }

            spriteRenderer[0].gameObject.AddComponent<YupThatsShakeBillboardThing>();

            if (plugin == null)
            {
                Debug.LogError("\"il.modded.raldi.Plugin\" was not found.");
                return;
            }
            itemToShit = plugin._BeastQuarter;
            creditCard = plugin._CreditCard;
            spriteRenderer[0].material = IvanLomAPI.GetDefaultSpriteMaterial();
            StartCoroutine(TimerCoroutine(15f, 25f, () => ShitItem(itemToShit)));
        }

        public Transform CreateChallengeCircle(bool enable)
        {
            if (redCircle != null)
            {
                redCircle.gameObject.SetActive(enable);
                redCircle.transform.localPosition = transform.position - (Vector3.up * CIRCLE_Y_OFFSET);
                return redCircle;
            }

            var newCircle = new GameObject("ChallengeCircle").transform;
            newCircle.SetPosAndRotation(pos: transform.position - (Vector3.up * CIRCLE_Y_OFFSET), rotation: Quaternion.Euler(new Vector3(90f, 0f, 0f)), local: true);
            newCircle.localScale = new Vector3(15f, 15f, 15f);

            SpriteRenderer circleRenderer = newCircle.gameObject.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = "spr_circle".GetSprite();
            circleRenderer.color = Color.red;

            newCircle.gameObject.SetActive(enable);
            return newCircle;
        }

        public void EndChallenge(bool won, bool changeState)
        {
            challengeState?.OnEndChallenge(this, won);
            if (changeState)
            {
                currentState = wanderState;
                ChangeState(wanderState);
                return;
            }

            StartCoroutine(Cooldown(15f, null));
        }

        public IEnumerator Cooldown(float time, Action action)
        {
            playcool = time;
            while (playcool > 0f)
            {
                playcool -= Time.deltaTime;
                yield return null;
            }

            playcool = 0f;

            action?.Invoke();

            audioMan.FlushQueue(endCurrent: true);
        }

        public int HaveCreditCards(ItemManager itm)
        {
            if (creditCard == null) return 0;

            int amount = 0;

            if (itm != null && itm.items != null)
            {
                for (int i = 0; i < itm.items.Length; i++)
                {
                    if (itm.items[i] == creditCard)
                    {
                        amount++;
                    }
                }
            }

            List<ITM_CreditCard> creditCards = ObjectPoolManager.FindAll<ITM_CreditCard>();
            amount += creditCards.Count;

            return amount;
        }

        protected override void Update()
        {
            base.Update();
            if (behaviorStateMachine.CurrentState != angryState && plugin != null)
            {
                if (plugin.hardMode.Value && SingletonExtension.TryGetSingleton(out CoreGameManager cgm) && cgm.GetPlayer(0) != null && HaveCreditCards(cgm.GetPlayer(0).itm) > 0)
                {
                    ChangeState(angryState);
                }
            }
            else if (pm != null && alwaysKnows)
            {
                TargetPosition(pm.plm.transform.position);
            }
            behaviorStateMachine.Update();
        }

        private void OnDespawn()
        {
            if (plugin != null)
            {
                plugin.ResetAudio();
                if (plugin.beasticator != null)
                {
                    plugin.beasticator.transform.SetPosAndRotation(pos: beasticatorPos, rotation: Quaternion.identity, local: true);
                    plugin.beasticator.transform.localScale = Vector3.one;
                }
            }

            ChangeState(new MrBeast_StateBase(this, this));
            StopAllCoroutines();

            if (audioMan != null)
            {
                Destroy(audioMan.gameObject);
                audioMan = null;
            }

            if (redCircle != null)
            {
                Destroy(redCircle.gameObject);
                redCircle = null;
            }

            alwaysKnowsCoroutine = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnDespawn();
        }

        public override void Despawn()
        {
            base.Despawn();
            OnDespawn();
        }

        public void OnTriggerEnter(Collider other) => behaviorStateMachine?.CurrentState?.OnStateTriggerEnter(other, true);

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            if (behaviorStateMachine.CurrentState != angryState && value < 100) return;

            base.Hear(source, position, value);
            Navigator?.FindPath(position);
            if (!gameObject.activeSelf) return;

            if (beasticatorCoroutine != null)
                StopCoroutine(beasticatorCoroutine);
            beasticatorCoroutine = StartCoroutine(Beasticator());
        }

        public IEnumerator Beasticator(float elapsedTime = 0f, float moveUpDuration = 0.3f, float scaleUpDuration = 0.15f, float scaleDownDuration = 0.15f, float moveDownDuration = 0.3f)
        {
            if (plugin == null) yield break;
            if (!SingletonExtension.TryGetSingleton<CoreGameManager>(out var cgm)) yield break;

            if (plugin.beasticator == null)
            {
                plugin.beasticator = new GameObject("Beasticator");
                plugin.beasticator.transform.SetParent(cgm.GetHud(0).transform, false);

                Image image = plugin.beasticator.AddComponent<Image>();
                image.preserveAspect = true;
            }

            plugin.beasticator.transform.SetPosAndRotation(pos: beasticatorPos, rotation: Quaternion.identity, local: true);
            plugin.beasticator.transform.localScale = Vector3.one;

            Image indicatorImage = plugin.beasticator.GetComponent<Image>();
            Vector3 originalPosition = plugin.beasticator.transform.localPosition;
            Vector3 originalScale = plugin.beasticator.transform.localScale;

            indicatorImage.sprite = "spr_indicator_idle".GetSprite();
            plugin.beasticator.gameObject.SetActive(true);

            Vector3 targetPosition = new Vector3(originalPosition.x, -100f, originalPosition.z);

            while (elapsedTime < moveUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveUpDuration;
                plugin.beasticator.transform.localPosition = Vector3.Lerp(originalPosition, targetPosition, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            indicatorImage.sprite = "spr_indicator_hear".GetSprite();
            Vector3 enlargedScale = originalScale * 1.2f;

            elapsedTime = 0f;

            while (elapsedTime < scaleUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleUpDuration;
                plugin.beasticator.transform.localScale = Vector3.Lerp(originalScale, enlargedScale, t);
                yield return null;
            }

            elapsedTime = 0f;

            while (elapsedTime < scaleDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleDownDuration;
                plugin.beasticator.transform.localScale = Vector3.Lerp(enlargedScale, originalScale, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            elapsedTime = 0f;

            while (elapsedTime < moveDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveDownDuration;
                plugin.beasticator.transform.localPosition = Vector3.Lerp(targetPosition, originalPosition, t);
                yield return null;
            }

            plugin.beasticator.transform.localPosition = originalPosition;
            plugin.beasticator.transform.localScale = originalScale;
            plugin.beasticator.gameObject.SetActive(false);
        }

        private Vector3 beasticatorPos = new Vector3(150, -500, 0);
    }
}