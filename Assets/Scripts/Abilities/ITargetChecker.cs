using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace Hypersycos.GERogueFrame
{
	public interface ITargetChecker
	{
        ICastEffect HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState);
        ICastEffect GetEffect();
        ITargetChecker Clone();
    }
}