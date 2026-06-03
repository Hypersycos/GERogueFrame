using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.CullingGroup;

namespace Hypersycos.RogueFrame
{
    public interface ISync
    {
        void StartSync(Action<int, SyncChange> syncFunc, int index);
        void ApplySync(SyncChange change);
    }

    public class SyncChange : INetworkSerializable
    {
        public bool IsValueChange;
        public float Change;
        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out IsValueChange);
                reader.ReadValueSafe(out Change);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(IsValueChange);
                writer.WriteValueSafe(Change);
            }
        }

        public SyncChange()
        {

        }

        public SyncChange(bool @bool, float change)
        {
            IsValueChange = @bool;
            Change = change;
        }

        public override string ToString()
        {
            return IsValueChange.ToString() + " " + Change.ToString();
        }
    }
}
