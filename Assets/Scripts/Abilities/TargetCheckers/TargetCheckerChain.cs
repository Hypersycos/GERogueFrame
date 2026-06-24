using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public interface ISecondaryTargetChecker
    {
        bool HasValidTarget(ITargetPayload target, CharacterState myState, out ITargetPayload hit);
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

        public ITargetChecker Clone()
        {
            return new TargetCheckerChain(BaseChecker.Clone(), TargetCheckerList.Select((x) => x.Clone()).ToList());
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData)
        {
            bool success = BaseChecker.HasValidTarget(direction, position, camPosition, myState, out hit, out verifyData);
            if (success)
            {
                foreach (var target in TargetCheckerList)
                {
                    if (!target.HasValidTarget(hit, myState, out hit))
                    {
                        verifyData = null;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        bool ITargetChecker.VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit)
        {
            bool success = BaseChecker.VerifyTarget(target, myState, out hit);
            if (success)
            {
                foreach (var secondary in TargetCheckerList)
                {
                    if (!secondary.HasValidTarget(hit, myState, out hit))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
