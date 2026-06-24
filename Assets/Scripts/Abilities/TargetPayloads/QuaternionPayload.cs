using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IQuaternionPayload
    {
        public Quaternion Target { get; }
    }

    public record QuaternionPayload : ITargetPayload, IQuaternionPayload
    {

        protected Quaternion _target;

        public Quaternion Target => _target;

        public QuaternionPayload(Quaternion target)
        {
            _target = target;
        }
    }
}
