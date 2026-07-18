using System;
using System.Collections.Generic;

namespace Hypersycos.Utils
{
    public class EventDict<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public enum Operation : byte
        {
            OP_ADD,
            OP_CLEAR,
            OP_REMOVE,
            OP_SET
        }

        struct Change
        {
            internal Operation operation;
            internal TKey key;
            internal TValue item;
        }

        /// <summary>This is called after the item is added with TKey</summary>
        public Action<TKey> OnAdd;

        /// <summary>This is called after the item is changed with TKey. TValue is the OLD item</summary>
        public Action<TKey, TValue> OnSet;

        /// <summary>This is called after the item is removed with TKey. TValue is the OLD item</summary>
        public Action<TKey, TValue> OnRemove;

        /// <summary>
        /// This is called for all changes to the Dictionary.
        /// <para>For OP_ADD, TValue is the NEW value of the entry.</para>
        /// <para>For OP_SET and OP_REMOVE, TValue is the OLD value of the entry.</para>
        /// <para>For OP_CLEAR, both TKey and TValue are default.</para>
        /// </summary>
        public Action<Operation, TKey, TValue> OnChange;

        /// <summary>This is called before the data is cleared</summary>
        public Action OnClear;

        void AddOperation(Operation op, TKey key, TValue item, TValue oldItem, bool checkAccess)
        {
            Change change = new Change
            {
                operation = op,
                key = key,
                item = item
            };

            switch (op)
            {
                case Operation.OP_ADD:
                    OnAdd?.Invoke(key);
                    OnChange?.Invoke(op, key, item);
                    break;
                case Operation.OP_SET:
                    OnSet?.Invoke(key, oldItem);
                    OnChange?.Invoke(op, key, oldItem);
                    break;
                case Operation.OP_REMOVE:
                    OnRemove?.Invoke(key, oldItem);
                    OnChange?.Invoke(op, key, oldItem);
                    break;
                case Operation.OP_CLEAR:
                    OnClear?.Invoke();
                    OnChange?.Invoke(op, default, default);
                    break;
            }
        }
        public new TValue this[TKey i]
        {
            get => base[i];
            set
            {
                if (ContainsKey(i))
                {
                    TValue oldItem = base[i];
                    base[i] = value;
                    AddOperation(Operation.OP_SET, i, value, oldItem, true);
                }
                else
                {
                    base[i] = value;
                    AddOperation(Operation.OP_ADD, i, value, default, true);
                }
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            AddOperation(Operation.OP_ADD, key, value, default, true);
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            bool result = base.TryAdd(key, value);
            if (result)
                AddOperation(Operation.OP_ADD, key, value, default, true);
            return result;
        }

        public new bool Remove(TKey key)
        {
            if (TryGetValue(key, out TValue oldItem) && base.Remove(key))
            {
                AddOperation(Operation.OP_REMOVE, key, oldItem, oldItem, true);
                return true;
            }
            return false;
        }

        public new void Clear()
        {
            AddOperation(Operation.OP_CLEAR, default, default, default, true);
            base.Clear();
        }
    }
}