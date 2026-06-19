using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class CameraManager : MonoBehaviour
    {
        public Transform myCam;
        public Transform spectateTarget;

        public void SetCam(Transform cam)
        {
            myCam = cam;
            enabled = true;
        }

        private void Update()
        {
            if (myCam == null)
            {
                NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                    myCam = localPlayer.transform.Find("CameraPos");

                if (myCam == null)
                    return;
            }

            if (spectateTarget == null)
            {
                transform.position = myCam.position;
                transform.rotation = myCam.rotation;
            }
            else
            {
                transform.position = spectateTarget.position;
                transform.rotation = spectateTarget.rotation;
            }
        }
    }
}
