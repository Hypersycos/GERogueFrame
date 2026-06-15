using System;
using System.Collections.Generic;
using System.Text;
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
    public class CooldownAbility : Ability, ICooldownAbility
    {
        float _maxCooldown;
        float _currentCooldown;
        float _haste;

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
