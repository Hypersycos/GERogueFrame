using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class PlayerMovementController : MonoBehaviour
    {
        CharacterController characterController;
        GameObject playerCamera;
        InputAction move;
        Controls controls;

        [Header("Stats")]
        [SerializeField] float movementSpeed = 4f;
        [SerializeField] float airStrafeRate = 0.4f;
        [SerializeField] float drag = 0.7f;
        [SerializeField] float smoothing = 0.1f;
        [SerializeField] float overspeedControl = 0.4f;
        [SerializeField] float overspeedCarry = 0.25f;
        [SerializeField] float crouchSpeed = 0.4f;
        [SerializeField] float slideThreshold = 0.5f;
        [SerializeField] float slideImpulse = 0f;
        [SerializeField] float jumpHeight = 1f;
        [SerializeField] float airJumpHeight = 0.7f;
        [SerializeField] float superJumpHeight = 4f;
        [SerializeField] float airSuperJumpHeight = 3f;
        [SerializeField] int maxJumps = 2;
        [SerializeField] bool canSlide;
        [SerializeField] bool canSuperJump;

        [Header("Live Values")]
        [SerializeField] float movementModifiers = 1f;
        [SerializeField] float gravityMultiplier = 1f;
        [SerializeField] Vector3 velocity = Vector3.zero;
        [SerializeField] bool crouching = false;
        [SerializeField] bool superJumpAvailable = false;
        [SerializeField] int jumpsAvailable = 0;
        [SerializeField] float lastJump = 0f;

        Dictionary<string, float> movementModifierTracker = new Dictionary<string, float>();
        Dictionary<string, float> gravityModifierTracker = new Dictionary<string, float>();

        float maxSpeed => movementSpeed * movementModifiers * (grounded && crouching ? crouchSpeed : 1);
        float moveForce => maxSpeed / (smoothing / (movementModifiers < 1 ? movementModifiers : 1)) * (grounded ? 1 : airStrafeRate);
        float jumpForce => Mathf.Sqrt(-2f * gravityForce * (grounded ? jumpHeight : airJumpHeight));
        Vector3 intendedVelocity => velocity;
        Vector3 horizontalVelocity => new Vector3(velocity.x, 0, velocity.z);
        float superJumpForce => Mathf.Sqrt(-2f * gravityForce * (grounded ? superJumpHeight : airSuperJumpHeight) * (movementModifiers > 1 ? movementModifiers : 1));
        float gravityForce => Physics.gravity.y * gravityMultiplier;
        bool grounded
        {
            get
            {
                Ray ray = new Ray(transform.position + Vector3.up * 0.25f, Vector3.down);
                if (Physics.Raycast(ray, 0.35f, 0xFFFF ^ (1 << 6 | 1 << 7)))
                    return true;
                else
                    return characterController.isGrounded;
            }
        }

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
            controls = new();

            controls.Player.Jump.started += DoJump;
            controls.Player.Crouch.started += DoCrouch;
            controls.Player.Crouch.canceled += DoCrouch;
            controls.Player.ToggleCrouch.started += DoCrouch;

            move = controls.Player.Move;
            controls.Player.Enable();
        }

        private Vector3 GetHorizontalCameraForward(GameObject playerCamera)
        {
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        private Vector3 GetHorizontalCameraRight(GameObject playerCamera)
        {
            Vector3 right = playerCamera.transform.right;
            right.y = 0;
            return right.normalized;
        }

        private void DoCrouch(InputAction.CallbackContext context)
        {
            if (context.action.name == "Toggle Crouch")
                crouching = !crouching;
            else
                crouching = context.phase == InputActionPhase.Started;

            if (crouching)
            {
                float oldMaxSpeed = (maxSpeed / crouchSpeed);
                if (canSlide && horizontalVelocity.magnitude / oldMaxSpeed > slideThreshold && grounded)
                {
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
        }

        private void DoJump(InputAction.CallbackContext context)
        {
            if (jumpsAvailable == 0)
                return;

            if (canSuperJump && superJumpAvailable && crouching)
            {
                Vector3 direction = playerCamera.transform.forward;
                //check if something within jump height close below
                Ray ray = new Ray(this.transform.position + Vector3.up * 0.25f, Vector3.down);
                bool withinJumpHeight = Physics.Raycast(ray, out RaycastHit hit, 0.3f + jumpHeight);

                //looking at ground & superjumping mirrors camera
                if (direction.y < 0 && (withinJumpHeight || grounded))
                {
                    direction.y = -direction.y;
                }

                float horizontalMagnitude = Mathf.Min(horizontalVelocity.magnitude, maxSpeed / crouchSpeed * (1 + slideImpulse));
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
                inputForce += move.ReadValue<Vector2>().x * GetHorizontalCameraRight(playerCamera);
                inputForce += move.ReadValue<Vector2>().y * GetHorizontalCameraForward(playerCamera);
                inputForce = inputForce.normalized;

                //allow a normal jump to cancel up to half momentum away from travel direction
                //but keep all speed if jumping in direction of travel
                float component = (Vector3.Dot(horizontalVelocity.normalized, inputForce) + 3) / 4;
                float magnitude = horizontalVelocity.magnitude;
                velocity.x = inputForce.x * component * magnitude;
                velocity.z = inputForce.z * component * magnitude;
            }
            jumpsAvailable--;
            lastJump = 0.5f;
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

        public void AddGravityModifier(float modifier, string name)
        {
            RemoveGravityModifier(name);
            gravityMultiplier *= modifier;
            gravityModifierTracker.Add(name, modifier);
        }

        public void RemoveGravityModifier(string name)
        {
            if (gravityModifierTracker.TryGetValue(name, out float modifier))
            {
                gravityMultiplier /= modifier;
                gravityModifierTracker.Remove(name);
            }
        }

        private void FixedUpdate()
        {
            if (move == null)
            {
                return;
            }

            Vector3 inputForce = Vector3.zero;
            Vector2 moveInput = move.ReadValue<Vector2>();
            if (moveInput.magnitude > 1)
                moveInput = moveInput.normalized;

            float mult = moveForce * Time.fixedDeltaTime;
            inputForce += moveInput.x * mult * GetHorizontalCameraRight(playerCamera);
            inputForce += moveInput.y * mult * GetHorizontalCameraForward(playerCamera);

            inputForce *= grounded ? 1 : airStrafeRate;

            if (horizontalVelocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(horizontalVelocity, Vector3.up);
            }

            if (horizontalVelocity.magnitude > maxSpeed)
            { //reduce control if over max speed
                inputForce *= overspeedControl;
                Vector3 dragForce = -horizontalVelocity * drag * Time.fixedDeltaTime;

                float carryComponent = Vector3.Dot(-dragForce.normalized, inputForce);

                //slow down less if input is in direction of velocity
                if (carryComponent > overspeedCarry * dragForce.magnitude)
                {
                    Vector3 complement = new Vector3(-dragForce.normalized.z, 0, dragForce.normalized.x);
                    float otherMagnitude = Vector3.Dot(complement, inputForce);
                    inputForce = -dragForce * overspeedCarry + complement * otherMagnitude;
                }

                float heightChange;
                if (horizontalVelocity.magnitude - 0.0001f < maxSpeed || !grounded || lastJump > 0f)
                { //don't apply slide height velocity if user isn't pushing to max speed
                  //or isn't on the ground
                  //or jumped recently
                    heightChange = 0;
                }
                else
                    heightChange = characterController.velocity.y / horizontalVelocity.sqrMagnitude * 4;
                //TODO: acceleration increases with velocity? Seems liable to bug out
                Vector3 heightChangeAcceleration = dragForce.normalized * heightChange;
                velocity += inputForce + dragForce + heightChangeAcceleration;
            }
            else
            {
                if (inputForce == Vector3.zero)
                { //If no input, smooth towards 0 velocity
                    float force = moveForce * Time.fixedDeltaTime;
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
                {
                    Vector3 targetVelocity = Vector3.zero;
                    targetVelocity += moveInput.x * maxSpeed * GetHorizontalCameraRight(playerCamera);
                    targetVelocity += moveInput.y * maxSpeed * GetHorizontalCameraForward(playerCamera);
                    Vector3 diff = targetVelocity - horizontalVelocity;
                    Vector3 diffForce = diff.normalized * moveForce * Time.fixedDeltaTime;

                    if (diff.sqrMagnitude < diffForce.sqrMagnitude)
                    {
                        velocity.x = targetVelocity.x;
                        velocity.z = targetVelocity.z;
                    }
                    else
                    {
                        velocity += diffForce;
                    }
                    if (horizontalVelocity.magnitude > maxSpeed)
                        Debug.Log($"HMag: {horizontalVelocity.magnitude}, HVel: {horizontalVelocity}");
                }
            }

            Vector3 forcedMove = Vector3.zero;

            if (grounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y *= 0.7f;
                    if (velocity.y > -0.1f)
                    {
                        velocity.y = 0f;
                        jumpsAvailable = maxJumps;
                        superJumpAvailable = true;
                        Ray ray = new Ray(transform.position + Vector3.up * 0.25f, Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 0.3f, 0xFFFF ^ (1 << 6 | 1 << 7)))
                        {
                            forcedMove.y = 0.25f - hit.distance;
                            Debug.Log($"moving {forcedMove.y}, from {transform.position.y} to {transform.position.y + forcedMove.y}");
                        }
                    }
                }
            }
            else
            {
                if (velocity.y == 0f)
                { //check if stairs
                    Ray ray = new Ray(this.transform.position + Vector3.up * 0.25f, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 0.3f + characterController.stepOffset))
                    { //force player down fast
                        velocity.y = -1;
                    }
                }
                else
                {
                    velocity.y += gravityForce * Time.fixedDeltaTime;
                }
            }

            if (lastJump > 0f)
            {
                lastJump -= Time.fixedDeltaTime;
            }

            characterController.Move(velocity * Time.fixedDeltaTime + forcedMove);
        }
    }
}
