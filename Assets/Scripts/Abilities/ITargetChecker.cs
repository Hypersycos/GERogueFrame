using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace Hypersycos.GERogueFrame
{
	public interface ITargetChecker
	{
        bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out object hit, out ICastEffect castEffect);
        ICastEffect GetEffect();
        ITargetChecker Clone();
    }
}