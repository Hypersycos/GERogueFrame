using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.InputSystem.XR;

namespace Hypersycos.RogueFrame
{
    public class PlayerMovementController : NetworkBehaviour
    {
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        //input fields
        public Controls ControlAsset { get; private set; }
        private InputAction move;

        //movement fields
        private CharacterController characterController;

        [SerializeField] private float gravityMultiplier = 1f;
        private float gravityForce {
            get
            {
                return Physics.gravity.y * gravityMultiplier
                     * (movementModifiers < 1 ? movementModifiers : 1);
            } }

        [SerializeField] private float movementSpeed = 4f;
        [SerializeField] private float airStrafeRate = 0.4f;
        [SerializeField] private float drag = 0.7f;
        [field: SerializeField] public float movementModifiers { get; private set; } = 1f;
        private Dictionary<string, float> movementModifierTracker = new Dictionary<string, float>();
        [SerializeField] private float smoothing = 0.1f;
        public float maxSpeed { get { return movementSpeed * movementModifiers * (crouching && IsGrounded() ? crouchSpeed : 1); } }
        private float moveForce { get { return maxSpeed / (smoothing / (movementModifiers < 1 ? movementModifiers : 1)) * (IsGrounded() ? 1 : airStrafeRate); } }
        [SerializeField] private float overspeedControl = 0.4f;
        [SerializeField] private float overspeedCarry = 0.25f;
        [SerializeField] private Vector3 velocity = Vector3.zero;
        [SerializeField] public Vector3 intendedVelocity { get { return velocity; } }
        public Vector3 horizontalVelocity { get { return new Vector3(velocity.x, 0, velocity.z); } }

        public bool crouching { get; private set; } = false;
        [SerializeField] public float crouchSpeed { get; private set; } = 0.4f;
        [SerializeField] private float slideThreshold = 0.5f;
        [SerializeField] private float slideImpulse = 0f;

        [SerializeField] private float jumpHeight = 1f;
        private float jumpForce { get { return Mathf.Sqrt(-2f * gravityForce * jumpHeight); } }
        private bool superJumpAvailable = false;
        [SerializeField] private float superJumpHeight = 4f;
        private float superJumpForce { get { return Mathf.Sqrt(-2f * gravityForce * superJumpHeight * (movementModifiers > 1 ? movementModifiers : 1)); } }
        [SerializeField] private int maxJumps = 2;
        private int jumpsAvailable = 0;
        private float lastJump = 0f;

        //other
        [SerializeField] Camera playerCamera;
        PlayerAnimatorScript animatorScript;

        public override void OnNetworkSpawn()
        {
            if (!IsLocalPlayer)
            {
                return;
            }
            enabled = true;
            animatorScript = GetComponent<PlayerAnimatorScript>();
            characterController = GetComponent<CharacterController>();
            animatorScript.enabled = true;
            characterController.enabled = true;
            ControlAsset = new Controls();
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            Cinemachine.CinemachineVirtualCamera cinemachineCam = playerCamera.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            cinemachineCam.Follow = transform.GetChild(0);
            cinemachineCam.LookAt = transform.GetChild(0);

            move = ControlAsset.Player.Move;

            ControlAsset.Player.Jump.started += DoJump;
            ControlAsset.Player.Crouch.started += DoCrouch;
            ControlAsset.Player.Crouch.canceled += DoCrouch;
            ControlAsset.Player.ToggleCrouch.started += DoCrouch;

            ControlAsset.Player.Enable();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void DoCrouch(InputAction.CallbackContext obj)
        {
            if (obj.action.name == "Toggle Crouch")
            {
                crouching = !crouching;
            }
            else
            {
                if (obj.phase == InputActionPhase.Started && !crouching)
                {
                    crouching = true;
                }
                else if (obj.phase == InputActionPhase.Canceled && crouching)
                {
                    crouching = false;
                }
            }
            if (crouching)
            {
                float oldMaxSpeed = (maxSpeed / crouchSpeed);
                if (horizontalVelocity.magnitude / oldMaxSpeed > slideThreshold && IsGrounded())
                { //slide
                    //TODO: should slide just use velocity? Should falling really far into slide work?
                    if (horizontalVelocity.magnitude < oldMaxSpeed * (1 + slideImpulse))
                    {
                        float magnitude = Mathf.Min(oldMaxSpeed * slideImpulse, oldMaxSpeed * (1 + slideImpulse) - horizontalVelocity.magnitude);
                        Vector3 impulse = horizontalVelocity.normalized * magnitude;
                        velocity.x += impulse.x;
                        velocity.z += impulse.z;
                    }
                }
            }
            animatorScript.SetCrouching(crouching);
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                ControlAsset.Player.Jump.started -= DoJump;
                ControlAsset.Player.Crouch.started -= DoCrouch;
                ControlAsset.Player.Crouch.canceled -= DoCrouch;
                ControlAsset.Player.ToggleCrouch.started -= DoCrouch;
            }
        }

        private void FixedUpdate()
        {
            if (move == null)
            {
                return;
            }
            Vector3 inputForce = Vector3.zero;
            inputForce += move.ReadValue<Vector2>().x * moveForce * Time.fixedDeltaTime * GetCameraRight(playerCamera);
            inputForce += move.ReadValue<Vector2>().y * moveForce * Time.fixedDeltaTime * GetCameraForward(playerCamera);
            //TODO: does this do anything?
            float inputCap = move.ReadValue<Vector2>().magnitude * maxSpeed;
            inputForce *= IsGrounded() ? 1 : airStrafeRate;
            if (horizontalVelocity != Vector3.zero)
            {
                //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(inputForce, Vector3.up), Time.fixedDeltaTime / smoothing);
                transform.rotation = Quaternion.LookRotation(horizontalVelocity, Vector3.up);
                //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(horizontalVelocity, Vector3.up), Time.fixedDeltaTime / smoothing);
            }

            if (horizontalVelocity.magnitude > maxSpeed)
            {
                inputForce *= overspeedControl;
                Vector3 dragForce = -horizontalVelocity * drag * Time.fixedDeltaTime;
                float carryComponent = Vector3.Dot(-dragForce.normalized, inputForce);
                if (carryComponent > overspeedCarry * dragForce.magnitude)
                { //TODO: figure out what this does exactly
                    Vector3 complement = new Vector3(-dragForce.normalized.z, 0, dragForce.normalized.x);
                    float otherMagnitude = Vector3.Dot(complement, inputForce);
                    inputForce = -dragForce * overspeedCarry + complement * otherMagnitude;
                }
                float heightChange = characterController.velocity.y / horizontalVelocity.sqrMagnitude * 4;
                //TODO: losing height in air doesn't speed up slide? Should falling really far into slide work?
                if (horizontalVelocity.magnitude - 0.0001f < maxSpeed || !IsGrounded() || lastJump > 0f)
                { //don't apply slide height velocity if user isn't pushing to max speed
                  //or isn't on the ground
                  //or jumped recently
                    heightChange = 0;
                }
                //TODO: acceleration increases with velocity? Seems liable to bug out
                Vector3 heightChangeAcceleration = dragForce.normalized * heightChange;
                velocity += inputForce + dragForce + heightChangeAcceleration;
            }
            else
            {
                if (inputForce == Vector3.zero)
                { //If no input, smooth towards 0 velocity
                    float force = maxSpeed * Time.fixedDeltaTime / smoothing * (IsGrounded() ? 1 : airStrafeRate);
                    if (force > horizontalVelocity.magnitude)
                    {
                        velocity.x = 0;
                        velocity.z = 0;
                    }
                    else
                    {
                        velocity -= horizontalVelocity.normalized * force;
                    }
                }
                else
                { //TODO: does this allow basically instant stopping, completely bypassing smoothing?
                    Vector3 temp = horizontalVelocity + inputForce;
                    if (temp.magnitude > inputCap)
                    {
                        temp = temp.normalized * inputCap;
                    }
                    velocity.x = temp.x;
                    velocity.z = temp.z;
                }
            }

            characterController.Move(velocity * Time.fixedDeltaTime);

            if (!IsGrounded() && velocity.y == 0f)
            {
                { //check if stairs
                    Ray ray = new Ray(this.transform.position + Vector3.up * 0.25f, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 0.3f + characterController.stepOffset))
                    {
                        velocity.y -= 1f;
                    }
                }
            }
            if (IsGrounded() && velocity.y <= 0f)
            { //TODO: why multiply rather than set to 0?
              //To avoid floating?
                velocity.y *= 0.7f;
                if (velocity.y > -0.1f)
                    velocity.y = 0f;
                jumpsAvailable = maxJumps;
                superJumpAvailable = true;
            }
            else
            {
                velocity.y += gravityForce * Time.fixedDeltaTime;
            }
            if (lastJump > 0f)
            {
                lastJump -= Time.fixedDeltaTime;
            }
        }

        private Vector3 GetCameraForward(Camera playerCamera)
        {
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        private Vector3 GetCameraRight(Camera playerCamera)
        {
            Vector3 right = playerCamera.transform.right;
            right.y = 0;
            return right.normalized;
        }

        private void DoJump(InputAction.CallbackContext obj)
        {
            if (jumpsAvailable > 0)
            {
                animatorScript.SetCanSuperJump(superJumpAvailable);
                if (superJumpAvailable && crouching)
                {
                    Vector3 direction = playerCamera.transform.forward;
                    //check if something within jump height close below
                    Ray ray = new Ray(this.transform.position + Vector3.up * 0.25f, Vector3.down);
                    bool withinJumpHeight = Physics.Raycast(ray, out RaycastHit hit, 0.3f + jumpHeight);
                    //looking at ground & superjumping mirrors camera
                    if (direction.y < 0 && (withinJumpHeight || IsGrounded()))
                    {
                        direction.y = -direction.y;
                    }
                    float horizontalMagnitude = Mathf.Min(horizontalVelocity.magnitude, maxSpeed/crouchSpeed * (1+slideImpulse));
                    //horizontalMagnitude used to allow jumping high, but horizontal jumps not faster than running
                    float jumpMagnitude = Mathf.Max(superJumpForce, horizontalMagnitude);
                    //superjump completely overrides currently velocity. Maybe should have some sort of scaling?
                    velocity = direction * jumpMagnitude;
                    superJumpAvailable = false;
                }
                else
                {
                    velocity.y = jumpForce;
                    Vector3 inputForce = Vector3.zero;
                    inputForce += move.ReadValue<Vector2>().x * GetCameraRight(playerCamera);
                    inputForce += move.ReadValue<Vector2>().y * GetCameraForward(playerCamera);
                    inputForce = inputForce.normalized;
                    //allow a normal jump to cancel up to half momentum away from travel direction
                    //but keep all speed if jumping in direction of travel
                    float component = (Vector3.Dot(horizontalVelocity.normalized, inputForce) + 3) / 4;
                    float magnitude = horizontalVelocity.magnitude;
                    velocity.x = inputForce.x * component * magnitude;
                    velocity.z = inputForce.z * component * magnitude;
                }
                jumpsAvailable -= 1;
                animatorScript.Jump();
                lastJump = 0.5f;
            }
        }

        public bool IsGrounded()
        {
            Ray ray = new Ray(this.transform.position + Vector3.up * 0.25f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.3f))
                return true;
            else
                return characterController.isGrounded; // || false
        }

        public void AddMovementModifier(float modifier, string name)
        {
            if (movementModifierTracker.ContainsKey(name))
            {
                RemoveMovementModifier(name);
            }
            movementModifiers *= modifier;
            movementModifierTracker.Add(name, modifier);
        }

        public void RemoveMovementModifier(string name)
        {
            if (movementModifierTracker.ContainsKey(name))
            {
                movementModifiers /= movementModifierTracker[name];
                movementModifierTracker.Remove(name);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (IsOwner)
            {
                if (animationEvent.animatorClipInfo.weight > 0.5f)
                {
                    if (FootstepAudioClips.Length > 0)
                    {
                        var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(characterController.center), FootstepAudioVolume);
                    }
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (IsOwner)
            {
                if (animationEvent.animatorClipInfo.weight > 0.5f)
                {
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(characterController.center), FootstepAudioVolume);
                }
            }
        }
    }
}