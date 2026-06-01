using System;
using System.Collections.Generic;

namespace Hypersycos.Utils
{
    [Serializable]
    public class EventList<T> : List<T>
    {
        public enum Operation : byte
        {
            OP_ADD,
            OP_SET,
            OP_INSERT,
            OP_REMOVEAT,
            OP_CLEAR
        }

        struct Change
        {
            internal Operation operation;
            internal int index;
            internal T item;
        }

        /// <summary>This is called after the item is added with index</summary>
        public Action<int> OnAdd;

        /// <summary>This is called after the item is inserted with index</summary>
        public Action<int> OnInsert;

        /// <summary>This is called after the item is set with index and OLD Value</summary>
        public Action<int, T> OnSet;

        /// <summary>This is called after the item is removed with index and OLD Value</summary>
        public Action<int, T> OnRemove;

        /// <summary>
        /// This is called for all changes to the List.
        /// <para>For OP_ADD and OP_INSERT, T is the NEW value of the entry.</para>
        /// <para>For OP_SET and OP_REMOVE, T is the OLD value of the entry.</para>
        /// <para>For OP_CLEAR, T is default.</para>
        /// </summary>
        public Action<Operation, int, T> OnChange;

        /// <summary>This is called before the list is cleared so the list can be iterated</summary>
        public Action OnClear;

        void AddOperation(Operation op, int itemIndex, T oldItem, T newItem, bool checkAccess)
        {
            Change change = new Change
            {
                operation = op,
                index = itemIndex,
                item = newItem
            };

            switch (op)
            {
                case Operation.OP_ADD:
                    OnAdd?.Invoke(itemIndex);
                    OnChange?.Invoke(op, itemIndex, newItem);
                    break;
                case Operation.OP_INSERT:
                    OnInsert?.Invoke(itemIndex);
                    OnChange?.Invoke(op, itemIndex, newItem);
                    break;
                case Operation.OP_SET:
                    OnSet?.Invoke(itemIndex, oldItem);
                    OnChange?.Invoke(op, itemIndex, oldItem);
                    break;
                case Operation.OP_REMOVEAT:
                    OnRemove?.Invoke(itemIndex, oldItem);
                    OnChange?.Invoke(op, itemIndex, oldItem);
                    break;
                case Operation.OP_CLEAR:
                    OnClear?.Invoke();
                    OnChange?.Invoke(op, itemIndex, default);
                    break;
            }
        }

        public new void Add(T item)
        {
            base.Add(item);
            AddOperation(Operation.OP_ADD, Count - 1, default, item, true);
        }

        public new void Clear()
        {
            AddOperation(Operation.OP_CLEAR, 0, default, default, true);
            base.Clear();
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);
            AddOperation(Operation.OP_INSERT, index, default, item, true);
        }

        public new bool Remove(T item)
        {
            int index = IndexOf(item);
            bool result = index >= 0;
            if (result)
                RemoveAt(index);

            return result;
        }

        public new void RemoveAt(int index)
        {
            T oldItem = base[index];
            base.RemoveAt(index);
            AddOperation(Operation.OP_REMOVEAT, index, oldItem, default, true);
        }

        public new T this[int i]
        {
            get => base[i];
            set
            {
                if (!base[i].Equals(value))
                {
                    T oldItem = base[i];
                    base[i] = value;
                    AddOperation(Operation.OP_SET, i, oldItem, value, true);
                }
            }
        }
    }
}