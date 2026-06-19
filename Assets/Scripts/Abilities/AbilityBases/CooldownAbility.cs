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

    public interface ICooldownUpdatePayload : IServerTickPayload
    {
        public float CurrentCooldown { get; }
        public float Haste { get; }
    }

    public record CooldownPayload : AbilityPayload, ICooldownUpdatePayload
    {
        float _CurrentCooldown;
        public float CurrentCooldown => _CurrentCooldown;

        float _Haste;
        public float Haste => _Haste;

        int _ServerTime;
        public int ServerTick => _ServerTime;

        public CooldownPayload(float currentCooldown, float haste, int serverTime)
        {
            _CurrentCooldown = currentCooldown;
            _Haste = haste;
            _ServerTime = serverTime;
        }


        public override void Serialize(FastBufferWriter writer)
        {
            writer.TryBeginWrite(sizeof(float) * 3);
            writer.WriteValue(CurrentCooldown);
            writer.WriteValue(Haste);
            writer.WriteValue(ServerTick);
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.TryBeginRead(sizeof(float) * 3);
            reader.ReadValue(out float cd);
            reader.ReadValue(out float haste);
            reader.ReadValue(out int time);
            return new CooldownPayload(cd, haste, time);
        }
    }

    public class CooldownAbility : Ability, ICooldownAbility
    {
        float _maxCooldown;
        float _currentCooldown;
        float _haste;

        int lastUpdate = 0;

        public float CurrentCooldown { get => _currentCooldown; private set { _currentCooldown = value; lastUpdate = NetworkManager.Singleton.ServerTime.Tick; } }

        public float CurrentCooldownPercent => _currentCooldown / MaxCooldown;

        public float MaxCooldown => _maxCooldown * (this as ICooldownAbility).InverseCDR;

        public float Haste { get => _haste; private set { _haste = value; lastUpdate = NetworkManager.Singleton.ServerTime.Tick; } }

        public override bool IsDirty { get => lastUpdate == NetworkManager.Singleton.ServerTime.Tick; }

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
            return new CooldownPayload(_currentCooldown, _haste, lastUpdate);
        }

        public override void SyncClient(AbilityPayload payload)
        {
            var cd = payload as CooldownPayload;
            if (lastUpdate < cd.ServerTick)
            {
                _currentCooldown = cd.CurrentCooldown;
                _haste = cd.Haste;
                lastUpdate = cd.ServerTick;
            }
        }

        public void SetCooldown(float mult = 1)
        {
            CurrentCooldown = mult * MaxCooldown;
        }

        public void SetCooldownTo(float seconds)
        {
            CurrentCooldown = seconds;
        }

        public void ReduceCooldown(float delta)
        {
            CurrentCooldown -= delta;
        }

        public void ReduceCooldownByMult(float mult)
        {
            CurrentCooldown -= MaxCooldown * mult;
        }

        public void IncreaseCooldown(float delta)
        {
            CurrentCooldown += delta;
        }

        public void IncreaseCooldownByMult(float mult)
        {
            CurrentCooldown += MaxCooldown * mult;
        }

        public void AddHaste(float amount)
        {
            Haste += amount;
        }

        public void RemoveHaste(float amount)
        {
            Haste -= amount;
        }
    }
}
