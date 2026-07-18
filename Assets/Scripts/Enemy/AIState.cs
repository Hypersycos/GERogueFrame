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

        public void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            myChar = GetComponent<CharacterState>();
            animator = GetComponent<Animator>();
        }
        public void Start()
        {
            state.Start();
        }

        public void FixedUpdate()
        {
            state.Tick(myChar, Time.fixedDeltaTime);
        }
    }
}
