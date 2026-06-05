using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class CameraManager : MonoBehaviour
    {
        public GameObject target;

        [SerializeField] float cameraDistance = 4;
        [SerializeField] Vector3 cameraOffset = new( 1, 2, 0 );
        Vector3 offsetWithDist;

        float yaw = 0;
        float pitch = 0;

        InputAction lookAction;

        private void Awake()
        {
            offsetWithDist = cameraOffset + new Vector3(0, 0, -cameraDistance);

            InputSystem.actions.FindActionMap("Player").Enable();
            lookAction = InputSystem.actions.FindAction("Look");
        }

        public void SetTarget(GameObject obj)
        {
            target = obj;
            if (!enabled)
                enabled = true;
        }

        private void Update()
        {
            if (target == null)
            {
                NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                    target = localPlayer.transform.Find("CameraTarget").gameObject;

                if (target == null)
                    return;
            }

            Vector2 lookValue = lookAction.ReadValue<Vector2>();
            yaw += lookValue.x;
            pitch += lookValue.y;

            if (pitch > 85)
                pitch = 85;
            if (pitch < -85)
                pitch = -85;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);

            offsetWithDist = cameraOffset + new Vector3(0, 0, -cameraDistance);

            //target.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 position = target.transform.position + gameObject.transform.rotation * offsetWithDist;

            Vector3 dir = position - target.transform.position;
            dir.Normalize();

            LayerMask layerMask = 0xFFFF ^ (1 << 6 | 1 << 7);

            Debug.DrawRay(target.transform.position, dir * cameraDistance, Color.hotPink);
            if (Physics.Raycast(target.transform.position, dir, out RaycastHit hit, cameraDistance, layerMask))
            {
                gameObject.transform.position = hit.point;
                Debug.DrawLine(target.transform.position, hit.point, Color.red);
            }
            else
                gameObject.transform.position = position;
        }
    }
}
