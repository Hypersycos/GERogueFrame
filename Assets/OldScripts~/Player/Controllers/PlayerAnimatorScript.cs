using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class PlayerAnimatorScript : NetworkBehaviour
    {
        private Animator animator;
        private PlayerMovementController controllerScript;
        private CharacterController characterController;
        //What speed is 1x speed
        [SerializeField] float runSpeedNormal = 5f;
        private float airtime = 2f;
        // Start is called before the first frame update
        public override void OnNetworkSpawn()
        {
            animator = GetComponent<Animator>();
            if (IsLocalPlayer)
            {
                enabled = true;
            }
            else
            {
                return;
            }
            controllerScript = GetComponent<PlayerMovementController>();
            characterController = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 horizontalVelocity = controllerScript.horizontalVelocity;

            float speed = horizontalVelocity.magnitude / controllerScript.maxSpeed;
            bool isGrounded = controllerScript.IsGrounded();
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
            float animatorSpeed = controllerScript.movementModifiers;
            float velocity = horizontalVelocity.magnitude;
            float walkThreshold = Mathf.Min(runSpeedNormal / 2, controllerScript.maxSpeed / 2);

            if (horizontalVelocity != Vector3.zero && isGrounded)
            {
                if (controllerScript.crouching)
                {
                    animatorSpeed = velocity / runSpeedNormal / controllerScript.crouchSpeed;
                }
                else
                {
                    float speed1 = Mathf.Min(1f, velocity / (walkThreshold));
                    float speed2 = velocity / (runSpeedNormal);
                    animatorSpeed = Mathf.Lerp(speed1, speed2, 2 * (speed - 0.5f));
                }
            }
            animator.SetFloat("MotionSpeed", animatorSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("Grounded", isGrounded);
            if (Mathf.Abs(controllerScript.intendedVelocity.y) >= 0.5f && !animator.GetBool("Jump") && !isGrounded)
            {
                airtime += Time.deltaTime;
            }
            else if (isGrounded)
            {
                airtime = 0f;
                animator.SetBool("FreeFall", false);
            }
            if (airtime > 0.5f)
            {
                animator.SetBool("FreeFall", true);
            }
        }

        public void Jump()
        {
            animator.SetTrigger("Jump");
            JumpServerRpc();
        }

        [ServerRpc]
        private void JumpServerRpc()
        {
            JumpClientRpc();
        }

        [ClientRpc]
        private void JumpClientRpc()
        {
            if (!IsLocalPlayer)
            {
                animator.SetTrigger("Jump");
            }
        }

        public void SetCrouching(bool isCrouching)
        {
            Vector3 horizontalVelocity = controllerScript.horizontalVelocity;

            animator.SetFloat("Speed", horizontalVelocity.magnitude / controllerScript.maxSpeed);
            animator.SetBool("Crouching", isCrouching);
        }

        public void SetCanSuperJump(bool canSuperJump)
        {
            animator.SetBool("CanSuperJump", canSuperJump);
        }
    }
}