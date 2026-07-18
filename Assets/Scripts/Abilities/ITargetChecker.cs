using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace Hypersycos.GERogueFrame
{
	public interface ITargetChecker
	{
        bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out ITargetPayload hit, out AbilityPayload verifyData);
        bool VerifyTarget(AbilityPayload target, CharacterState myState, out ITargetPayload hit);
        ITargetChecker Clone();
    }
}