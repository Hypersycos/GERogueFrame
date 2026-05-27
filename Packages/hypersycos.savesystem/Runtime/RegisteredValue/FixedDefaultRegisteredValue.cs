using System.Collections.Generic;

namespace Hypersycos.SaveSystem
{
    public class FixedDefaultRegisteredValue<T> : RegisteredValue<T>
    {
        public T Default;

        internal FixedDefaultRegisteredValue(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent, List<Validator<T>> validators, T @default = default) : base(isEphemeral, registeredSerializers, name, parent, validators)
        {
            Default = @default;
        }

        protected override T GetDefault()
        {
            return Default;
        }
    }
}
