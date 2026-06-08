using System;

namespace Hypersycos.GERogueFrame
{
    public struct NonNullString
    {
        public string Value
        {
            get; private set;
        }
        NonNullString(string value)
        {
            Value = value ?? "";
        }

        public static implicit operator NonNullString(string value)
        {
            return new NonNullString(value);
        }

        public static implicit operator string(NonNullString value)
        {
            return value.Value;
        }
    }
}