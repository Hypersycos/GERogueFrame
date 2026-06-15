using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public interface ISecondaryTargetChecker
    {
        bool HasValidTarget(TargetPayload target, CharacterState myState, out TargetPayload hit);
        ISecondaryTargetChecker Clone();
    }

    class TargetCheckerChain : ITargetChecker
    {
        [ShowInInspector]
        [OdinSerialize] ITargetChecker BaseChecker;

        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true)]
        [OdinSerialize] List<ISecondaryTargetChecker> TargetCheckerList = new();

        public TargetCheckerChain(ITargetChecker baseChecker, List<ISecondaryTargetChecker> targetCheckerList)
        {
            BaseChecker = baseChecker;
            TargetCheckerList = targetCheckerList;
        }

        public ICastEffect Effect { get => BaseChecker.Effect; set => BaseChecker.Effect = value; }

        public ITargetChecker Clone()
        {
            return new TargetCheckerChain(BaseChecker.Clone(), TargetCheckerList.Select((x) => x.Clone()).ToList());
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out TargetPayload hit, out ICastEffect castEffect)
        {
            bool success = BaseChecker.HasValidTarget(direction, position, camPosition, myState, out hit, out castEffect);
            if (success)
            {
                foreach (var target in TargetCheckerList)
                {
                    if (!target.HasValidTarget(hit, myState, out hit))
                    {
                        castEffect = null;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
