using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface IComponentPayload
    {
        public Component Component { get; }
    }

    public interface IComponentPayload<T> : IComponentPayload where T : Component
    {
        public new T Component { get; }

        Component IComponentPayload.Component => Component;
    }

    public record ComponentPayload : TargetPayload, IGameObjectPayload, IComponentPayload
    {
        protected Component _target;

        public GameObject Target => _target.gameObject;
        public Component Component => _target;

        public ComponentPayload(Component target)
        {
            _target = target;
        }
    }

    public record ComponentPayload<T> : TargetPayload, IGameObjectPayload, IComponentPayload<T> where T : Component
    {

        protected T _target;

        public GameObject Target => _target.gameObject;
        public T Component => _target;

        public ComponentPayload(T target)
        {
            _target = target;
        }
    }
}
