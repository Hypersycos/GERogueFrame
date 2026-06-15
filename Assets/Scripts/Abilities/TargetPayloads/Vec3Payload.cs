using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IVec3Payload
    {
        public Vector3 Target { get; }
    }

    public record Vec3Payload : TargetPayload, IVec3Payload
    {

        protected Vector3 _target;

        public Vector3 Target => _target;

        public Vec3Payload(Vector3 target)
        {
            _target = target;
        }
    }
}
