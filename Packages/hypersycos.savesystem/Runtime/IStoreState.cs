using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    public abstract class IStoreState
    {
        bool _HasBeenModified;
        public virtual bool HasBeenModified { get => _HasBeenModified; set
            {
                if (value && Parent is not null && !Parent.HasBeenModified)
                    Parent.HasBeenModified = true;
                _HasBeenModified = value;
            } }

        public bool IsEphemeral { get; protected set; }

        private string _name;
        public virtual string Name { get => _name; protected set { _name = value; SetFullyQualifiedName(); } }
        public string FullyQualifiedName { get; protected set; }
        public string FileQualifiedName { get; protected set; }

        private IRegisteredHolder _Parent;
        public virtual IRegisteredHolder Parent
        {
            get => _Parent;
            internal set
            {
                if (_Parent != null)
                    _Parent.RemoveChild(this);
                _Parent = value;
                SetFullyQualifiedName();
            }
        }

        protected IStoreState(bool isEphemeral, List<Serializer> registeredSerializers, string name, IRegisteredHolder parent)
        {
            IsEphemeral = isEphemeral;
            RegisteredSerializers = registeredSerializers;
            Name = name;
            Parent = parent;
            SetFullyQualifiedName();
            UpdateSerializerDict();
        }

        internal void SetFullyQualifiedName()
        {
            FullyQualifiedName = Parent?.FullyQualifiedName + Parent?.NameSeparator + Name;
            if (GetType() != typeof(RegisteredFile))
            {
                if (Parent is not null && Parent.GetType() == typeof(RegisteredFile))
                    FileQualifiedName = Name;
                else
                    FileQualifiedName = Parent?.FileQualifiedName + Parent?.NameSeparator + Name;
            }
            else
            {
                FileQualifiedName = "";
            }
        }

        public virtual void ResetToDefault()
        {
        }

        public virtual void ForceSave()
        {
            if (!HasBeenModified) return;
            Parent?.ForceSave();
        }

        //SERIALIZATION LOGIC

        protected List<Serializer> RegisteredSerializers = new();
        private Dictionary<Type, Serializer> typeToSerializer = new();
        void UpdateSerializerDict()
        {
            typeToSerializer = new();
            foreach (Serializer serializer in RegisteredSerializers)
            {
                foreach (Type type in serializer.SupportedTypes)
                {
                    typeToSerializer[type] = serializer;
                }
            }
        }
        private Serializer GetSerializerInner(Type type)
        {
            if (typeToSerializer.ContainsKey(type))
                return typeToSerializer[type];
            else if (Parent is not null)
                return Parent.GetSerializerInner(type);
            else
                return null;
        }

        public Serializer GetSerializer(Type type)
        {
            Serializer serializer = GetSerializerInner(type);
            if (serializer is null)
            {
                Debug.LogErrorFormat("No serializer found for type {0} for {1}", type, FullyQualifiedName);
                return null;
            }
            return serializer;
        }

        public abstract List<string> Serialize();
    }
}
