using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Hypersycos.RogueFrame
{
    public class SpawnEffectObject : NetworkBehaviour
    {
        [SerializeField] public float Timer;
        public CharacterState Owner;
        [SerializeField] UnityEvent<SpawnEffectObject> OnExpire;
        [SerializeField] public float ExpiryLength;

        protected void FixedUpdate()
        {
            if (IsServer)
            {
                Timer -= Time.fixedDeltaTime;
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
    }
}
