using System;
using System.Collections.Generic;
using System.Linq;

namespace Hypersycos.SaveSystem
{
    public class ValueOperator
    {
        protected virtual IReadOnlyCollection<Type> SupportsTypes => new Type[0];
        protected virtual IReadOnlyCollection<Type> SupportsBaseTypes => new Type[0];
        protected ISet<Type> AddedTypes;
        protected ISet<Type> RemovedTypes;

        public IReadOnlyCollection<Type> SupportedTypes;

        protected ValueOperator(ISet<Type> addedTypes, ISet<Type> removedTypes)
        {
            AddedTypes = addedTypes ?? new HashSet<Type>();
            RemovedTypes = removedTypes ?? new HashSet<Type>();

            SupportedTypes = GenerateSupportedTypes().ToList().AsReadOnly();
        }

        public bool SupportsType(Type typeToCheck)
        {
            foreach (Type type in AddedTypes)
            {
                if (type.IsAssignableFrom(typeToCheck)) return true;
            }
            foreach (Type type in RemovedTypes)
            {
                if (type.IsAssignableFrom(typeToCheck)) return false;
            }
            if (SupportsTypes.Contains(typeToCheck))
            {
                return true;
            }
            else
            {
                foreach (Type type in SupportsBaseTypes)
                {
                    if (type.IsAssignableFrom(typeToCheck)) return true;
                }
            }
            return false;
        }

        IEnumerable<Type> GenerateSupportedTypes()
        {
            HashSet<Type> types = new(SupportsTypes);
            types.ExceptWith(RemovedTypes);
            types.UnionWith(AddedTypes);
            foreach (Type type in SupportsBaseTypes)
            {
                if (RemovedTypes.Contains(type)) continue;
                types.UnionWith(type.Assembly.GetTypes().Where(t => t.IsAssignableFrom(type)));
            }
            return types;
        }
    }
}