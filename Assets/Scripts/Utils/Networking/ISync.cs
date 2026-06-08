using System;
using System.Collections;
using Unity.Netcode;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
	public interface ISyncStat
	{
        public void StartSync(Action<int, SyncChange> syncFunc, int index);
		public void StopSync();
		public void ApplySync(SyncChange change);
    }

	public struct SyncChange : INetworkSerializable
	{
		public bool IsValueChange;
		public float NewValue;

        public SyncChange(bool isValueChange, float change)
        {
            IsValueChange = isValueChange;
            NewValue = change;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IsValueChange);
            serializer.SerializeValue(ref NewValue);
        }
    }
}