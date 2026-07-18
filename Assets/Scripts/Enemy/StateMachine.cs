using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [Serializable]
    public class StateMachine
    {
        [Serializable]
        public struct Transition
        {
            public Func<CharacterState, bool> condition;
            public string target;
        }
        [Serializable]
        public struct State
        {
            public string name;
            public float duration;
            public List<Transition> transitions;
            public Action<CharacterState, float> behaviour;
            public bool IsOneShot;

            bool debounce;

            public void Tick(CharacterState owner, float dt)
            {
                if (behaviour != null && !debounce)
                {
                    behaviour(owner, dt);
                    debounce = IsOneShot;
                }
            }

            public string Transition(CharacterState state, Dictionary<string, int> stateDict)
            {
                foreach(Transition t in transitions)
                {
                    if ((t.condition == null || t.condition(state)) && stateDict.ContainsKey(t.target))
                    {
                        debounce = false;
                        return t.target;
                    }
                }
                duration = 0;
                return null;
            }

            public void ResetDebounce()
            {
                debounce = false;
            }
        }

        public List<State> states = new();
        private Dictionary<string, int> stateDict = new();
        public string entryState;
        [field: SerializeField] public string currentState { get; protected set; }
        float currentTime = 0;


        public void Start()
        {
            stateDict.Clear();
            for(int i = 0; i < states.Count; i++)
            {
                stateDict.Add(states[i].name, i);
            }

            Reset();
        }

        public void Reset()
        {
            if (currentState != null && stateDict.TryGetValue(currentState, out int index))
                states[index].ResetDebounce();

            if (stateDict.ContainsKey(entryState))
                currentState = entryState;
            else
                currentState = states[0].name;
            currentTime = 0;
        }

        public bool ForceState(string target)
        {
            if (currentState != null && stateDict.TryGetValue(currentState, out int index))
                states[index].ResetDebounce();

            if (stateDict.ContainsKey(target))
            {
                currentState = target;
                currentTime = 0;
                return true;
            }
            return false;
        }

        public void Tick(CharacterState owner, float dt)
        {
            currentTime += dt;

            if (currentTime < 0)
                return;

            bool loop = true;
            while(loop)
            {
                var state = states[stateDict[currentState]];
                state.Tick(owner, dt);
                if (currentTime >= state.duration)
                {
                    string target = state.Transition(owner, stateDict);
                    if (target != null)
                    {
                        currentState = target;
                        currentTime = 0;
                    }
                    else
                        loop = false;
                }
                else
                    loop = false;
            }
        }
    }
}
