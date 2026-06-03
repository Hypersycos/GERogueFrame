using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace Hypersycos.RogueFrame
{
    [Serializable]
    public class StatInstance
    {
        [SerializeField, ReadOnly] protected List<StatModifier> StatModifiers = new List<StatModifier>();
        protected Dictionary<string, List<StatModifier>> SingletonStatModifiers = new Dictionary<string, List<StatModifier>>();
        protected class ByPriority : IComparer<StatModifier>
        {
            public int Compare(StatModifier x, StatModifier y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }

        protected class ByStrength : IComparer<StatModifier>
        {
            public int Compare(StatModifier x, StatModifier y)
            {
                return Mathf.Abs(y.Value).CompareTo(Mathf.Abs(x.Value));
            }
        }

        public virtual void AddModifier(StatModifier modifier)
        {
            if (modifier.CanSelfStack)
            {
                int index = StatModifiers.BinarySearch(modifier, new ByPriority());
                //If priority not found, returns bitwise complement of the index to insert at
                index = index < 0 ? ~index : index;
                StatModifiers.Insert(index, modifier);
            }
            else
            {
                if (SingletonStatModifiers.ContainsKey(modifier.SourceName))
                {
                    List<StatModifier> modifiers = SingletonStatModifiers[modifier.SourceName];
                    int index = modifiers.BinarySearch(modifier, new ByStrength());
                    index = index < 0 ? ~index : index;
                    if (index == 0)
                    { //if strongest, replace current active instance with new instance
                        int oldIndex = StatModifiers.BinarySearch(modifiers[0], new ByPriority());
                        StatModifiers.RemoveAt(oldIndex);
                        StatModifiers.Insert(oldIndex, modifier);
                    }
                    StatModifiers.Insert(index, modifier);
                }
                else
                {
                    SingletonStatModifiers[modifier.SourceName] = new List<StatModifier> { modifier };
                    int index = StatModifiers.BinarySearch(modifier, new ByPriority());
                    index = index < 0 ? ~index : index;
                    StatModifiers.Insert(index, modifier);
                }
            }
        }

        public virtual void RemoveModifier(StatModifier modifier)
        {
            if (modifier.CanSelfStack)
            {
                StatModifiers.Remove(modifier);
            }
            else
            {
                List<StatModifier> modifiers = SingletonStatModifiers[modifier.SourceName];
                if (modifiers[0] == modifier)
                {
                    int oldIndex = StatModifiers.BinarySearch(modifiers[0], new ByPriority());
                    StatModifiers.RemoveAt(oldIndex);
                    if (modifiers.Count > 1)
                    {
                        StatModifiers.Insert(oldIndex, modifiers[1]);
                        SingletonStatModifiers[modifier.SourceName].RemoveAt(0);
                    }
                    else
                    {
                        SingletonStatModifiers.Remove(modifier.SourceName);
                    }
                }
                else
                {
                    SingletonStatModifiers[modifier.SourceName].Remove(modifier);
                }
            }
        }

        protected float ApplyModifiers(float start, float flatMultiplier=1)
        {
            //TODO: This will break if there are multiple consecutive layers of multiplicativeadditive
            float multTemp = 1;
            foreach (StatModifier modifier in StatModifiers)
            {
                if (multTemp != 1 && modifier.StackBehaviour != StatModifier.StackType.MultiplicativeAdditive)
                {
                    start *= multTemp;
                    multTemp = 1;
                }
                switch (modifier.StackBehaviour)
                {
                    case StatModifier.StackType.Flat:
                        //FlatMultiplier is used for smooth regeneration
                        start += modifier.Value * flatMultiplier;
                        break;
                    case StatModifier.StackType.MultiplicativeAdditive:
                        multTemp += modifier.Value;
                        break;
                    case StatModifier.StackType.Multiplicative:
                        start *= modifier.Value;
                        break;
                }
            }
            return start;
        }
    }
}
