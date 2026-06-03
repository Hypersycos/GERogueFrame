using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class EnemyState : CharacterState
    {
        void Start()
        {
            Team = 1;
            if (IsServer)
            {
                Health.OnEmpty.AddListener((_, _, _) => StartCoroutine(FullHealAfter(3)));
            }
            SyncedInstances = new List<ISync> { Health };
            HitPoints = new DefensePool(new List<DefenseStatInstance>() { Health }, this);
            GetComponentInChildren<StatBarScript>().AddStats(new List<BoundedStatInstance>() { Health });
        }

        IEnumerator FullHealAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Health.AddValue(10000000);
        }

        [SerializeField] DefenseStatInstance Health = new DefenseStatInstance(100);
    }
}