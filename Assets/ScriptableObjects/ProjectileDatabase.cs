using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New ProjectileDatabase", menuName = "GERogueFrame/ProjectileDatabase", order = 0)]
    public class ProjectileDatabase : ScriptableObject
    {
        public static ProjectileDatabase singleton;

        public List<GameObject> dumbProjectileList;
        public List<GameObject> networkedProjectileList;

        public Dictionary<GameObject, int> dumbIDs;
        public Dictionary<GameObject, int> networkedIDs;

        private void OnEnable()
        {
            singleton = this;
            dumbIDs = new();
            networkedIDs = new();

            for(int i = 0; i < dumbProjectileList.Count; i++)
            {
                if (dumbProjectileList[i] != null)
                    dumbIDs.Add(dumbProjectileList[i], i);
            }

            for (int i = 0; i < networkedProjectileList.Count; i++)
            {
                if (networkedProjectileList[i] != null)
                    networkedIDs.Add(networkedProjectileList[i], i);
            }
        }
    }
}
