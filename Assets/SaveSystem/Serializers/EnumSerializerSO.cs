using Hypersycos.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class EnumSerializerSO : SerializerSO
    {
        //[System.NonSerialized] EnumSerializer mySerializer;
        public ScriptableObject mySetting;
        public override Serializer serializer => new EnumSerializer((mySetting as IEnumSetting).EnumType);

        public void SetType(System.Type t)
        {
            //mySerializer = new(t);
        }
    }
}
