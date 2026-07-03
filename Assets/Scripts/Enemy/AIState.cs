using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Hypersycos.GERogueFrame
{
    public class AIState : MonoBehaviour
    {
        protected StateMachine state;
        protected CharacterState myChar;
        protected Animator animator;
        protected NavMeshAgent agent;

        Vector2 smoothDeltaPosition = Vector2.zero;
        Vector2 velocity = Vector2.zero;

        public void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            myChar = GetComponent<CharacterState>();
            animator = GetComponent<Animator>();

            if (animator != null)
                agent.updatePosition = false;
        }
        public void Start()
        {
            state.Start();
        }

        public void FixedUpdate()
        {
            state.Tick(myChar, Time.fixedDeltaTime);

            if (animator != null && false)
            {

                Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

                // Map 'worldDeltaPosition' to local space
                float dx = Vector3.Dot(transform.right, worldDeltaPosition);
                float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
                Vector2 deltaPosition = new Vector2(dx, dy);

                // Low-pass filter the deltaMove
                float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
                smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

                // Update velocity if time advances
                if (Time.deltaTime > 1e-5f)
                    velocity = smoothDeltaPosition / Time.deltaTime;

                bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

                // Update animation parameters
                animator.SetBool("move", shouldMove);
                animator.SetFloat("velx", velocity.x);
                animator.SetFloat("vely", velocity.y);
            }
        }


        void OnAnimatorMove()
        {
            // Update position to agent position
            if (enabled)
                transform.position = agent.nextPosition;
        }
    }
}
