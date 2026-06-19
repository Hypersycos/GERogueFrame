using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.GERogueFrame
{
    public class AbilityObject : NetworkBehaviour
    {
        public virtual CharacterState SpawnedBy { get; set; }

        public float Timer;
        public CharacterState Owner;
        public UnityEvent<AbilityObject> OnExpire;
        public float ExpiryLength;

        protected void DoTimer(float scale = 1)
        {
            if (IsServer)
            {
                Timer -= Time.fixedDeltaTime * scale;
                if (Timer <= 0)
                {
                    OnExpire.Invoke(this);
                }
                if (Timer <= -ExpiryLength)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            DoTimer();
        }
    }
}
