using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class StatBarRotator : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] Transform target;
        void Start()
        {
            Camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            transform.LookAt(target.position + Camera.transform.rotation * Vector3.forward, Camera.transform.rotation * Vector3.up);
        }
    }
}
