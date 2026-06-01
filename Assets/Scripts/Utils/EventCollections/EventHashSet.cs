using System;
using System.Collections.Generic;
using System.Linq;

namespace Hypersycos.Utils
{
    public class EventHashSet<T> : HashSet<T>, ICollection<T>
    {
        /// <summary>This is called after the item is added. T is the new item.</summary>
        public Action<T> OnAdd;

        /// <summary>This is called after the item is removed. T is the OLD item</summary>
        public Action<T> OnRemove;

        /// <summary>
        /// This is called for all changes to the Set.
        /// <para>For OP_ADD, T is the NEW value of the entry.</para>
        /// <para>For OP_REMOVE, T is the OLD value of the entry.</para>
        /// <para>For OP_CLEAR, T is default.</para>
        /// </summary>
        public Action<Operation, T> OnChange;

        /// <summary>This is called BEFORE the data is cleared</summary>
        public Action OnClear;

        public enum Operation : byte
        {
            OP_ADD,
            OP_REMOVE,
            OP_CLEAR
        }

        void AddOperation(Operation op, T oldItem, T newItem, bool checkAccess)
        {
            switch (op)
            {
                case Operation.OP_ADD:
                    OnAdd?.Invoke(newItem);
                    OnChange?.Invoke(op, newItem);
                    break;
                case Operation.OP_REMOVE:
                    OnRemove?.Invoke(oldItem);
                    OnChange?.Invoke(op, oldItem);
                    break;
                case Operation.OP_CLEAR:
                    OnClear?.Invoke();
                    OnChange?.Invoke(op, default);
                    break;
            }
        }

        void AddOperation(Operation op, bool checkAccess) => AddOperation(op, default, default, checkAccess);

        public new bool Add(T item)
        {
            if (base.Add(item))
            {
                AddOperation(Operation.OP_ADD, default, item, true);
                return true;
            }
            return false;
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public new void Clear()
        {
            AddOperation(Operation.OP_CLEAR, true);
            base.Clear();
        }

        public new bool Remove(T item)
        {
            if (base.Remove(item))
            {
                AddOperation(Operation.OP_REMOVE, item, default, true);
                return true;
            }
            return false;
        }

        public new int RemoveWhere(Predicate<T> match)
        {
            T[] values = this.Where((o) => match(o)).ToArray();
            foreach (var value in values)
            {
                Remove(value);
            }
            return values.Length;
        }
    }
}
