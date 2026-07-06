using UnityEngine;
using UnityEngine.AI;

namespace Hypersycos.GERogueFrame
{
    public class ResetZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CharacterState state))
            {
                if (state.IsServer)
                {
                    Vector3 pos = state.CentrePos;
                    pos.y = 0;
                    NavMesh.SamplePosition(pos, out NavMeshHit hit, 200, 1);
                    state.Teleport(hit.position + Vector3.up * .2f);
                }
            }
        }
    }
}
