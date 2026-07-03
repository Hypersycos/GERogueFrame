using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New ObjectiveDatabase", menuName = "GERogueFrame/Database/ObjectiveDatabase", order = 0)]
    public class ObjectiveDatabase : ModDatabase<ObjectiveSO>
    {
    }
}
