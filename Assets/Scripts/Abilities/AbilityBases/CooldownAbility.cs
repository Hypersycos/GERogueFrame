using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface ICooldownAbility
    {
        public float CurrentCooldown { get; }
        public float CurrentCooldownPercent { get; }
        public float MaxCooldown { get; }
        public float Haste { get; }
        public float CDR => (Haste + 100) / 100;
        public float InverseCDR => 100 / (Haste + 100);

        public void SetCooldown(float mult = 1);
        public void SetCooldownTo(float seconds);
        public void ReduceCooldown(float delta);
        public void ReduceCooldownByMult(float mult);
        public void IncreaseCooldown(float delta);
        public void IncreaseCooldownByMult(float mult);
        public void AddHaste(float amount);
        public void RemoveHaste(float amount);
    }

    public interface ICooldownUpdatePayload
    {
        public float CurrentCooldown { get; }
        public float Haste { get; }
        public float ServerTime { get; }
    }

    public record CooldownPayload : AbilityPayload, ICooldownUpdatePayload
    {
        float _CurrentCooldown;
        public float CurrentCooldown => _CurrentCooldown;

        float _Haste;
        public float Haste => _Haste;

        float _ServerTime;

        public CooldownPayload(float currentCooldown, float haste, float serverTime)
        {
            _CurrentCooldown = currentCooldown;
            _Haste = haste;
            _ServerTime = serverTime;
        }

        public float ServerTime => _ServerTime;

        public override void Serialize(FastBufferWriter writer)
        {
            writer.TryBeginWrite(sizeof(float) * 3);
            writer.WriteValue(CurrentCooldown);
            writer.WriteValue(Haste);
            writer.WriteValue(ServerTime);
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.TryBeginRead(sizeof(float) * 3);
            reader.ReadValue(out float cd);
            reader.ReadValue(out float haste);
            reader.ReadValue(out float time);
            return new CooldownPayload(cd, haste, time);
        }
    }

    public class CooldownAbility : Ability, ICooldownAbility
    {
        float _maxCooldown;
        float _currentCooldown;
        float _haste;

        bool isDirty = false;

        public float CurrentCooldown => _currentCooldown;

        public float CurrentCooldownPercent => _currentCooldown / MaxCooldown;

        public float MaxCooldown => _maxCooldown * (this as ICooldownAbility).InverseCDR;

        public float Haste => _haste;

        public CooldownAbility(IEnumerable<ICastCostChecker> targets, float cooldown, bool targetOnStart) : base(targets, targetOnStart)
        {
            _maxCooldown = cooldown;
        }

        public override void FixedUpdate(CharacterState myState)
        {
            if (myState.IsServer)
                _currentCooldown = Mathf.Max(0, _currentCooldown - Time.fixedDeltaTime);
        }

        public override AbilityPayload Sync()
        {
        }

        public void SetCooldown(float mult = 1)
        {
            _currentCooldown = mult * MaxCooldown;
        }

        public void SetCooldownTo(float seconds)
        {
            _currentCooldown = seconds;
        }

        public void ReduceCooldown(float delta)
        {
            _currentCooldown -= delta;
        }

        public void ReduceCooldownByMult(float mult)
        {
            _currentCooldown -= MaxCooldown * mult;
        }

        public void IncreaseCooldown(float delta)
        {
            _currentCooldown += delta;
        }

        public void IncreaseCooldownByMult(float mult)
        {
            _currentCooldown += MaxCooldown * mult;
        }

        public void AddHaste(float amount)
        {
            _haste += amount;
        }

        public void RemoveHaste(float amount)
        {
            _haste -= amount;
        }
    }
}
