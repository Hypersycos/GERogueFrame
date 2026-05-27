using System.Collections.Generic;

namespace Hypersycos.SaveSystem
{
    public class GeneratorDefaultRegisteredValue<T> : RegisteredValue<T>
    {
        public System.Func<T> DefaultGenerator;

        internal GeneratorDefaultRegisteredValue(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent, List<Validator<T>> validators, System.Func<T> @default) : base(isEphemeral, registeredSerializers, name, parent, validators)
        {
            DefaultGenerator = @default;
        }

        protected override T GetDefault()
        {
            if (DefaultGenerator is null) return default;
            return DefaultGenerator();
        }
    }
}
