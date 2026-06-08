using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hypersycos.SaveSystem;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    public class NetworkableValue<T> : TypedRegisteredValueSO<T>
    {
        [SerializeField] bool IsNetworked;
        [SerializeField] T ServerValue;

        [SerializeField] public string FriendlyName = "";
        [SerializeField] public string ShortDescription = "";
        [SerializeField] public string LongDescription = "";
        public bool UseLocal => !IsNetworked || NetworkManager.Singleton.IsServer;
        public override T Value
        {
            get => (UseLocal) ? base.Value : ServerValue;
            set
            {
                if (UseLocal)
                {
                    base.Value = value;
                    ServerValue = base.Value;
                }
                else
                {
                    ServerValue = value;
                    ValueUpdated?.Invoke(ServerValue);
                }
            }
        }

        public override object ObjectValue
        {
            get => (UseLocal) ? base.ObjectValue : ServerValue;
            set => Value = (T)value;
        }
    }
}
