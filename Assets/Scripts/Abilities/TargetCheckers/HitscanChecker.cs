using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class HitscanChecker : ITargetChecker
    {
        [SerializeField] float MaxRange;
        [SerializeField] float StartEpsilon = 0.01f;
        [SerializeField] float EndEpsilon = 0;
        [SerializeField] LayerMask HitLayerMask;
        [SerializeField] LayerMask VerifyLayerMask;
        [SerializeField] LayerMask TargetLayerMask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;
        [SerializeField] bool NoHitIsSuccess;

        public HitscanChecker(float maxRange, LayerMask hitLayerMask, LayerMask targetLayerMask, QueryTriggerInteraction triggerInteraction, float startEpsilon, float endEpsilon)
        {
            MaxRange = maxRange;
            HitLayerMask = hitLayerMask;
            TargetLayerMask = targetLayerMask;
            TriggerInteraction = triggerInteraction;
            StartEpsilon = startEpsilon;
            EndEpsilon = endEpsilon;
        }

        public ITargetChecker Clone()
        {
            return new HitscanChecker(MaxRange, HitLayerMask, TargetLayerMask, TriggerInteraction, StartEpsilon, EndEpsilon);
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            bool didHit = Physics.Raycast(camPosition + direction * StartEpsilon, direction, out RaycastHit info, MaxRange - EndEpsilon, HitLayerMask, TriggerInteraction);
            if (didHit)
            {
                if ((TargetLayerMask & (1 << info.collider.gameObject.layer)) != 0)
                {
                    Debug.DrawLine(camPosition, info.point, Color.green, 3);
                    hit = new HitscanPayload(info);
                    verifyData = new HitscanNetworkPayload(camPosition, info.point, info.collider.gameObject);
                    return true;
                }
                else
                {
                    Debug.DrawLine(camPosition, info.point, Color.yellow, 3);
                    hit = null;
                    verifyData = null;
                    return false;
                }
            }
            else
            {
                if (NoHitIsSuccess)
                {
                    Debug.DrawLine(camPosition, camPosition + direction * MaxRange, Color.green, 3);
                    hit = new Vec3Payload(camPosition + direction * MaxRange);
                    verifyData = new HitscanNetworkPayload(camPosition, info.point, null);
                    return true;
                }

                Debug.DrawLine(camPosition, camPosition + direction * MaxRange, Color.red, 3);
                hit = null;
                verifyData = null;
                return false;
            }
        }

        public bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            var payload = target as HitscanNetworkPayload;
            if (payload.hit != null)
            {
                //TODO: actually verify LoS to networked objects
                hit = payload;
                return true;
            }
            else
            {
                //TODO: verify LoS to non-networked hits
                hit = payload;
                return true;
            }
        }
    }

    public record HitscanNetworkPayload : AbilityPayload, ITargetPayload, IVec3Payload, IGameObjectPayload
    {
        public Vector3 cameraPos;
        public Vector3 targetPos;
        public NetworkObject hit;

        public Vector3 Target => targetPos;

        GameObject IGameObjectPayload.Target => hit.gameObject;

        public HitscanNetworkPayload(Vector3 cameraPos, Vector3 targetPos, GameObject obj)
        {
            this.cameraPos = cameraPos;
            this.targetPos = targetPos;
            if (obj != null && obj.TryGetComponent(out NetworkObject netObj))
                hit = netObj;
        }

        public override void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(cameraPos);
            writer.WriteValueSafe(targetPos);
            writer.WriteValueSafe(new NetworkObjectReference(hit));
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector3 cameraPos);
            reader.ReadValueSafe(out Vector3 targetPos);
            reader.ReadValueSafe(out NetworkObjectReference hitRef);
            if (hitRef.TryGet(out NetworkObject netObj))
                return new HitscanNetworkPayload(cameraPos, targetPos, netObj.gameObject);
            else
                return new HitscanNetworkPayload(cameraPos, targetPos, null);
        }
    }

    public record HitscanPayload : ITargetPayload, IComponentPayload<Collider>, IVec3Payload, IGameObjectPayload
    {
        public RaycastHit hit;

        public HitscanPayload(RaycastHit hit)
        {
            this.hit = hit;
        }

        public Vector3 Target => hit.point;

        Collider IComponentPayload<Collider>.Component => hit.collider;

        GameObject IGameObjectPayload.Target => hit.collider.gameObject;
    }
}
