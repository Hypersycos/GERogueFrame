using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IGameObjectPayload
    {
        public GameObject Target { get; }
    }

    public record ObjectPayload : TargetPayload, IGameObjectPayload
    {

        protected GameObject _target;

        public GameObject Target => _target;

        public ObjectPayload(GameObject target)
        {
            _target = target;
        }
    }
}
