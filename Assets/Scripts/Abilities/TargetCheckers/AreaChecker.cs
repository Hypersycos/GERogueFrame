using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Hypersycos.GERogueFrame
{
    class AreaChecker : ISecondaryTargetChecker
    {
        [SerializeField] float Radius;
        [SerializeField] LayerMask Mask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;
        [SerializeField] bool EmptyIsSuccess = false;

        public AreaChecker(float radius, LayerMask mask, QueryTriggerInteraction triggerInteraction, bool emptyIsSuccess)
        {
            Radius = radius;
            Mask = mask;
            TriggerInteraction = triggerInteraction;
            EmptyIsSuccess = emptyIsSuccess;
        }

        public ISecondaryTargetChecker Clone()
        {
            return new AreaChecker(Radius, Mask, TriggerInteraction, EmptyIsSuccess);
        }

        public bool HasValidTarget(TargetPayload payload, CharacterState myState, out TargetPayload hit)
        {
            Collider[] colliders = Physics.OverlapSphere((payload as IVec3Payload).Target, Radius, Mask);
            if (colliders.Length == 0)
            {
                hit = null;
                return EmptyIsSuccess;
            }
            else
            {
                hit = new AreaPayload(colliders);
                return true;
            }
        }
    }

    class FilteredAreaChecker : ISecondaryTargetChecker
    {
        [SerializeField] ISecondaryTargetChecker Filter;
        [SerializeField] float Radius;
        [SerializeField] LayerMask Mask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;
        [SerializeField] bool EmptyIsSuccess = false;

        public FilteredAreaChecker(float radius, LayerMask mask, QueryTriggerInteraction triggerInteraction, bool emptyIsSuccess, ISecondaryTargetChecker filter)
        {
            Radius = radius;
            Mask = mask;
            TriggerInteraction = triggerInteraction;
            EmptyIsSuccess = emptyIsSuccess;
            Filter = filter;
        }

        public ISecondaryTargetChecker Clone()
        {
            return new FilteredAreaChecker(Radius, Mask, TriggerInteraction, EmptyIsSuccess, Filter);
        }

        public bool HasValidTarget(TargetPayload payload, CharacterState myState, out TargetPayload hit)
        {
            Collider[] colliders = Physics.OverlapSphere((payload as IVec3Payload).Target, Radius, Mask);
            colliders = colliders.Where(x => Filter.HasValidTarget(new ComponentPayload<Collider>(x), myState, out _)).ToArray();
            if (colliders.Length == 0)
            {
                hit = null;
                return EmptyIsSuccess;
            }
            else
            {
                hit = new AreaPayload(colliders);
                return true;
            }
        }
    }

    class FilteredPayloadAreaChecker : ISecondaryTargetChecker
    {
        [SerializeField] ISecondaryTargetChecker Filter;
        [SerializeField] float Radius;
        [SerializeField] LayerMask Mask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;
        [SerializeField] bool EmptyIsSuccess = false;

        public FilteredPayloadAreaChecker(float radius, LayerMask mask, QueryTriggerInteraction triggerInteraction, bool emptyIsSuccess, ISecondaryTargetChecker filter)
        {
            Radius = radius;
            Mask = mask;
            TriggerInteraction = triggerInteraction;
            EmptyIsSuccess = emptyIsSuccess;
            Filter = filter;
        }

        public ISecondaryTargetChecker Clone()
        {
            return new FilteredPayloadAreaChecker(Radius, Mask, TriggerInteraction, EmptyIsSuccess, Filter);
        }

        public bool HasValidTarget(TargetPayload payload, CharacterState myState, out TargetPayload hit)
        {
            Collider[] colliders = Physics.OverlapSphere((payload as IVec3Payload).Target, Radius, Mask);
            List<TargetPayload> result = new();
            foreach(Collider coll in colliders)
            {
                if (Filter.HasValidTarget(new ComponentPayload<Collider>(coll), myState, out TargetPayload outPayload))
                    result.Add(outPayload);
            }
            if (result.Count == 0)
            {
                hit = null;
                return EmptyIsSuccess;
            }
            else
            {
                hit = new ListTarget<TargetPayload>(result);
                return true;
            }
        }
    }

    public record AreaPayload : TargetPayload, IListTarget<Collider>, IListTarget<ComponentPayload<Collider>>
    {
        public Collider[] colliders;

        public AreaPayload(Collider[] list)
        {
            colliders = list;
        }

        public IList<Collider> List => colliders;

        IList<ComponentPayload<Collider>> IListTarget<ComponentPayload<Collider>>.List => colliders.Select(x => new ComponentPayload<Collider>(x)).ToList();

        IList IListTarget.List => colliders;
    }
}
