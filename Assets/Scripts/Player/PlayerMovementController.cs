using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class PlayerMovementController : NetworkBehaviour
    {
        CharacterController characterController;
        Transform playerCamera;
        InputAction move;
        ControlsWrapper controlWrapper;
        Controls controls => controlWrapper.controls;

        [Header("Stats")]
        [SerializeField] public float movementSpeed = 4f;
        [SerializeField] float airStrafeRate = 0.4f;
        [SerializeField] float drag = 0.7f;
        [SerializeField] float smoothing = 0.1f;
        [SerializeField] float overspeedControl = 0.4f;
        [SerializeField] float overspeedCarry = 0.25f;
        [field: SerializeField] public float crouchSpeed { get; private set; } = 0.4f;
        [SerializeField] float slideThreshold = 0.5f;
        [SerializeField] float slideImpulse = 0f;
        [SerializeField] float jumpHeight = 1f;
        [SerializeField] float airJumpHeight = 0.7f;
        [SerializeField] float superJumpHeight = 4f;
        [SerializeField] float airSuperJumpHeight = 3f;
        [SerializeField] int maxJumps = 2;
        [field: SerializeField] public bool canSuperJump { get; private set; }

        [Header("Live Values")]
        [field: SerializeField] public float movementModifiers { get; private set; } = 1f;
        [SerializeField] float gravityMultiplier = 1f;
        [SerializeField] Vector3 velocity = Vector3.zero;
        [field: SerializeField] public bool crouching { get; private set; } = false;
        [field: SerializeField] public bool superJumpAvailable { get; private set; } = false;
        [SerializeField] int jumpsAvailable = 0;
        [SerializeField] float lastJump = 0f;
        public bool lockedToCamera;

        public NetworkVariable<Vector3> networkVelocity = new(writePerm: NetworkVariableWritePermission.Owner);
        Queue<Vector3> pastVelocities = new();
        [SerializeField] float velocityAvgPeriod = 0.5f;
        int velocityAvgCount;

        Dictionary<string, float> movementModifierTracker = new Dictionary<string, float>();
        Dictionary<string, float> gravityModifierTracker = new Dictionary<string, float>();

        public float maxSpeed => movementSpeed * movementModifiers * (grounded && crouching ? crouchSpeed : 1);
        float moveForce => maxSpeed / (smoothing / (movementModifiers < 1 ? movementModifiers : 1)) * (grounded ? 1 : airStrafeRate);
        float jumpForce => Mathf.Sqrt(-2f * gravityForce * (grounded ? jumpHeight : airJumpHeight));
        public Vector3 intendedVelocity => velocity;
        public Vector3 horizontalVelocity => new Vector3(velocity.x, 0, velocity.z);
        float superJumpForce => Mathf.Sqrt(-2f * gravityForce * (grounded ? superJumpHeight : airSuperJumpHeight) * (movementModifiers > 1 ? movementModifiers : 1));
        float gravityForce => Physics.gravity.y * gravityMultiplier;
        public bool grounded
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

        public event Action OnJump;
        public event Action<bool> OnCrouch;
        public event Action<bool> CanSuperJumpChanged;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = GameObject.FindWithTag("MainCamera").transform;
            controlWrapper = ControlsWrapper.Singleton;

            controls.Player.Jump.started += DoJump;
            controls.Player.Crouch.started += DoCrouch;
            controls.Player.Crouch.canceled += DoCrouch;
            controls.Player.ToggleCrouch.started += DoCrouch;

            move = controls.Player.Move;
            velocityAvgCount = Mathf.CeilToInt(velocityAvgPeriod / Time.fixedDeltaTime);
        }

        public override void OnDestroy()
        {
            controls.Player.Jump.started -= DoJump;
            controls.Player.Crouch.started -= DoCrouch;
            controls.Player.Crouch.canceled -= DoCrouch;
            controls.Player.ToggleCrouch.started -= DoCrouch;
        }

        private Vector3 GetHorizontalCameraForward(Transform playerCamera)
        {
            Vector3 forward = playerCamera.forward;
            forward.y = 0;
            return forward.normalized;
        }

        private Vector3 GetHorizontalCameraRight(Transform playerCamera)
        {
            Vector3 right = playerCamera.right;
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
                if (horizontalVelocity.magnitude <= oldMaxSpeed && context.action.name == "Toggle Crouch")
                {
                    velocity.x *= crouchSpeed;
                    velocity.z *= crouchSpeed;
                }
                else if (horizontalVelocity.magnitude / oldMaxSpeed > slideThreshold && grounded)
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

            OnCrouch?.Invoke(crouching);
        }

        private void DoJump(InputAction.CallbackContext context)
        {
            if (jumpsAvailable == 0)
                return;

            jumpsAvailable--;
            lastJump = 0.5f;

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

                OnJump?.Invoke();
                CanSuperJumpChanged?.Invoke(superJumpAvailable);
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

                OnJump?.Invoke();
            }
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
            Vector3 inputForce = Vector3.zero;
            Vector2 moveInput = move.ReadValue<Vector2>();
            if (moveInput.magnitude > 1)
                moveInput = moveInput.normalized;

            float mult = moveForce * Time.fixedDeltaTime;
            inputForce += moveInput.x * mult * GetHorizontalCameraRight(playerCamera);
            inputForce += moveInput.y * mult * GetHorizontalCameraForward(playerCamera);

            inputForce *= grounded ? 1 : airStrafeRate;

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
                    {
                        Vector3 corrected = horizontalVelocity * maxSpeed / horizontalVelocity.magnitude * 0.999f;
                        velocity.x = corrected.x;
                        velocity.z = corrected.z;
                    }
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
                        if (!superJumpAvailable)
                        {
                            superJumpAvailable = true;
                            CanSuperJumpChanged?.Invoke(superJumpAvailable);
                        }
                        Ray ray = new Ray(transform.position + Vector3.up * 0.05f, Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 0.05f + characterController.stepOffset, 0xFFFF ^ (1 << 6 | 1 << 7)))
                        {
                            forcedMove.y = -(hit.distance - .05f);
                        }
                    }
                }
            }
            else
            {
                if (velocity.y == 0f)
                { //check if stairs
                    Ray ray = new Ray(this.transform.position + Vector3.up * 0.05f, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 0.05f + characterController.stepOffset))
                    { //force player down fast
                        forcedMove.y = -(hit.distance - 0.05f);
                    }
                    else
                    {
                        velocity.y += gravityForce * Time.fixedDeltaTime;
                    }
                }
                else
                {
                    velocity.y += gravityForce * Time.fixedDeltaTime;
                }
            }

            characterController.Move(velocity * Time.fixedDeltaTime + forcedMove);
            pastVelocities.Enqueue(characterController.velocity);
            networkVelocity.Value += characterController.velocity / velocityAvgCount;
            if (pastVelocities.Count > velocityAvgCount)
                networkVelocity.Value -= pastVelocities.Dequeue() / velocityAvgCount;

            if (lastJump > 0f)
            {
                lastJump -= Time.fixedDeltaTime;
            }

            if (lockedToCamera)
            {
                transform.rotation = Quaternion.LookRotation(GetHorizontalCameraForward(playerCamera), Vector3.up);
            }
            else
            {
                if (horizontalVelocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(horizontalVelocity, Vector3.up);
                }
            }
        }

        [Rpc(SendTo.Owner)]
        void TeleportRpc(Vector3 position, bool carryMomentum)
        {
            characterController.enabled = false;
            transform.position = position;
            if (!carryMomentum)
                velocity = new();
            characterController.enabled = true;
        }

        public void Teleport(Vector3 position, bool carryMomentum)
        {
            TeleportRpc(position, carryMomentum);
        }

        [Rpc(SendTo.Owner)]
        void RotationRpc(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetRotation(Quaternion rotation)
        {
            RotationRpc(rotation);
        }
    }
}
