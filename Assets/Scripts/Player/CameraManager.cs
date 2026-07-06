using Hypersycos.SaveSystem;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    class CameraManager : MonoBehaviour
    {
        Transform myPlayer;
        Transform myCamera;

        Transform spectateCamera;
        Transform spectateTarget;
        bool lockedSpectate;

        [SerializeField] float cameraDistance = 3;
        [SerializeField] Vector3 cameraOffset = new(1, 0.5f, 0);
        private InputAction lookAction;
        Vector3 offsetWithDist;

        float yaw = 0;
        float pitch = 0;
        Vector3 lastPos = new();
        Vector3 targetPos = new();

        [SerializeField] TypedRegisteredValueSO<float> kbmSens;
        [SerializeField] TypedRegisteredValueSO<float> controllerXSens;
        [SerializeField] TypedRegisteredValueSO<float> controllerYSens;
        ControlsWrapper controlWrapper;

        bool inMenu = false;

        private InputDevice lastLookDevice;

        void OnEnable()
        {
            lookAction.performed += OnLook;
        }

        void OnDisable()
        {
            lookAction.performed -= OnLook;
        }

        void OnLook(InputAction.CallbackContext ctx)
        {
            lastLookDevice = ctx.control.device;
        }

        private void Awake()
        {
            offsetWithDist = cameraOffset + new Vector3(0, 0, -cameraDistance);

            controlWrapper = ControlsWrapper.Singleton;
            controlWrapper.MenuOpened += disableCam;
            controlWrapper.MenuClosed += enableCam;

            lookAction = controlWrapper.controls.Player.Look;
        }

        void disableCam() => inMenu = true;
        void enableCam() => inMenu = false;

        private void OnDestroy()
        {
            controlWrapper.MenuOpened -= disableCam;
            controlWrapper.MenuClosed -= enableCam;
        }

        public void SetSpectate(PlayerState target, bool locked)
        {
            if (target == null || target == NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>())
            {
                spectateCamera = null;
                spectateTarget = null;
            }
            else
            {
                spectateCamera = target.transform.Find("CameraPos");
                spectateTarget = target.transform.Find("CameraTarget");
            }
            if (!locked && lockedSpectate)
            {
                Vector3 euler = spectateCamera.transform.rotation.eulerAngles;
                yaw = euler.y;
                pitch = euler.x;
            }
            lockedSpectate = locked;
        }

        public void SetSpectate(PlayerState target)
        {
            SetSpectate(target, lockedSpectate);
        }

        public void SetSpectate()
        {
            SetSpectate(null, lockedSpectate);
        }

        public void SetMyCamera(PlayerState target)
        {
            myPlayer = target.cameraTarget;
            myCamera = target.transform.Find("CameraPos");
            enabled = true;
        }

        private void HandleLookInput(Transform target)
        {
            if (!inMenu)
            {
                Vector2 lookValue = lookAction.ReadValue<Vector2>();

                if (lastLookDevice is Gamepad)
                    lookValue = new Vector2(lookValue.x * controllerXSens.Value, lookValue.y * controllerYSens.Value);
                else
                    lookValue *= kbmSens.Value;

                yaw += lookValue.x;
                pitch += lookValue.y;

                if (pitch > 85)
                    pitch = 85;
                if (pitch < -85)
                    pitch = -85;
            }

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);

            offsetWithDist = cameraOffset + new Vector3(0, 0, -cameraDistance);

            //target.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 position = target.position + transform.rotation * offsetWithDist;

            Vector3 dir = position - target.position;
            dir.Normalize();

            LayerMask layerMask = 0xFFFF ^ (1 << 6 | 1 << 7);

            Debug.DrawRay(target.position, dir * cameraDistance, Color.hotPink);
            if (Physics.Raycast(target.position, dir, out RaycastHit hit, cameraDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                targetPos = hit.point;
                lastPos = hit.point;
            }
            else
                targetPos = position;
        }

        private void Update()
        {
            if (spectateTarget == null)
            {
                HandleLookInput(myPlayer);
                myCamera.transform.position = targetPos;
                myCamera.transform.rotation = transform.rotation;
            }
            else
            {
                if (lockedSpectate)
                {
                    targetPos = spectateCamera.position;
                    transform.rotation = spectateCamera.rotation;
                }
                else
                {
                    HandleLookInput(spectateTarget);
                }
            }

            if ((targetPos - lastPos).magnitude > 5f)
            {
                transform.position = targetPos;
                lastPos = targetPos;
            }
            float t = 1;// Mathf.Clamp01(Time.deltaTime * 20f);
            transform.position = targetPos * t + lastPos * (1 - t);
            lastPos = transform.position;
        }
    }
}
