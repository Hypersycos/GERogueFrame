using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class PlayerState : CharacterState
    {
        private class DamageTextInstance
        {
            public TMP_Text text;
            public float damage;
            public float timer;
            public float angle;

            public DamageTextInstance(TMP_Text text, float damage, float timer, float angle)
            {
                this.text = text;
                this.damage = damage;
                this.timer = timer;
                this.angle = angle;
            }
        }
        public TMP_Text DamageTickPrefab;
        ClientRpcParams clientRpcParams;

        float DamageNumberThreshold = 1;
        float DamageCumulative = 0;
        float HealCumulative = 0;

        Dictionary<StatBarRotator, DamageTextInstance> LastDamageNumbers = new();
        Dictionary<StatBarRotator, DamageTextInstance> LastHealNumbers = new();
        float TextMergeTimer = 0.15f;

        public BoundedStatInstance Energy;
        public DefenseStatInstance Health;
        public DefenseStatInstance Shields;
        public DefenseStatInstance OverHealth;

        Transform cameraTarget;
        public override Vector3 CentrePos => cameraTarget.position;

        void Start()
        {
            Team = 0;
            if (IsServer)
            {
                OnDamage.AddListener(CreateDamageNumber);

                OnHeal.AddListener(CreateHealNumber);

                clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                };
            }
            cameraTarget = transform.Find("CameraTarget");
        }

        public void ApplyDefensePool(List<DefenseStatInstance> stats)
        {
            HitPoints = new DefensePool(stats, this);
        }

        protected override void FixedUpdate()
        {
            if (IsServer)
            {
                base.FixedUpdate();
                Energy.Tick(Time.fixedDeltaTime);
            }
            if (IsOwner)
            {
                List<StatBarRotator> toRemove = new();
                foreach(StatBarRotator key in LastDamageNumbers.Keys)
                {
                    LastDamageNumbers[key].timer -= Time.fixedDeltaTime;
                    if (LastDamageNumbers[key].timer <= 0)
                    {
                        toRemove.Add(key);
                    }
                }
                foreach(StatBarRotator key in toRemove)
                {
                    LastDamageNumbers.Remove(key);
                }

                toRemove = new();
                foreach (StatBarRotator key in LastHealNumbers.Keys)
                {
                    LastHealNumbers[key].timer -= Time.fixedDeltaTime;
                    if (LastHealNumbers[key].timer <= 0)
                    {
                        toRemove.Add(key);
                    }
                }
                foreach (StatBarRotator key in toRemove)
                {
                    LastHealNumbers.Remove(key);
                }
            }
        }

        public bool UseEnergy(float amount)
        {
            return Energy.TryRemoveValue(amount);
        }

        public bool CanUseEnergy(float amount)
        {
            return Energy.CanRemoveValue(amount);
        }

        public void GiveEnergy(float amount)
        {
            Energy.AddValue(amount);
        }

        private void CreateDamageNumber(CharacterState victim, DamageInstance damage)
        {
            float damageNumber = damage.ActualAmount;
            if (damageNumber < DamageNumberThreshold)
            {
                DamageCumulative += damageNumber;
                if (DamageCumulative > DamageNumberThreshold)
                {
                    damageNumber = DamageCumulative;
                    DamageCumulative = 0;
                }
                else
                {
                    return;
                }
            }
            float coefficient = (damageNumber / victim.HitPoints.MaxValue * 4 + 1);
            Color c = victim.GetDamageColor();
            CreateDamageNumberClientRpc(victim.NetworkObject, damageNumber, c, clientRpcParams);
        }

        private void CreateHealNumber(CharacterState victim, DamageInstance heal)
        {
            float healNumber = heal.ActualAmount;
            if (healNumber < DamageNumberThreshold)
            {
                HealCumulative += healNumber;
                if (HealCumulative > DamageNumberThreshold)
                {
                    healNumber = HealCumulative;
                    HealCumulative = 0;
                }
                else
                {
                    return;
                }
            }
            Color c = victim.GetDamageColor();
            c = new Color(0, 1f, 0) * 0.7f + c * 0.3f;
            float coefficient = (healNumber / victim.HitPoints.MaxValue * 4 + 1);
            CreateHealNumberClientRpc(victim.NetworkObject, healNumber, c, clientRpcParams);
        }

        [ClientRpc]
        private void CreateHealNumberClientRpc(NetworkObjectReference victimRef, float number, Color c, ClientRpcParams clientRpcParams = default)
        {
            CreateDamageNumberCommon(victimRef, number, c, LastHealNumbers);
        }


        [ClientRpc]
        private void CreateDamageNumberClientRpc(NetworkObjectReference victimRef, float number, Color c, ClientRpcParams clientRpcParams = default)
        {
            CreateDamageNumberCommon(victimRef, number, c, LastDamageNumbers);
        }

        private void CreateDamageNumberCommon(NetworkObjectReference victimRef, float number, Color c, Dictionary<StatBarRotator, DamageTextInstance> dict)
        { 
            DamageTextInstance CreateDamageNumberCommonInner(StatBarRotator statBarRotator, float number, Dictionary<StatBarRotator, DamageTextInstance> dict)
            {
                if (dict.ContainsKey(statBarRotator))
                {
                    dict[statBarRotator].damage += number;
                    return dict[statBarRotator];
                }
                else
                {
                    TMP_Text text = Instantiate(DamageTickPrefab, statBarRotator.transform);
                    DamageTextInstance instance = new DamageTextInstance(text, number, TextMergeTimer, Random.Range(-1f, 1f));
                    dict.Add(statBarRotator, instance);
                    return instance;
                }
            }

            CharacterState victim = ((GameObject)victimRef).GetComponent<CharacterState>();
            StatBarRotator statBarRotator = victim.GetComponentInChildren<StatBarRotator>();
            DamageTextInstance instance = CreateDamageNumberCommonInner(statBarRotator, number, dict);
            float coefficient = (number / victim.HitPoints.MaxValue * 4 + 1);
            UpdateDamageText(instance, coefficient, c);
        }

        private void UpdateDamageText(DamageTextInstance inst, float coefficient, Color c)
        {
            TMP_Text instance = inst.text;
            instance.color = c;
            instance.text = ((int)inst.damage).ToString();
            instance.fontSize = coefficient * 50 + 50;

            DamageInstanceScript script = instance.GetComponent<DamageInstanceScript>();
            script.lifetime = 0.5f * (coefficient / 4 + 0.5f);
            script.speed = 500 * 0.5f / script.lifetime;
            float angle = (100 - coefficient * 20) / 360 * 2 * Mathf.PI * inst.angle;
            script.velocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        public override void Teleport(Vector3 NewPosition)
        {
            base.Teleport(NewPosition);
            Physics.SyncTransforms();
        }
    }
}