using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;

namespace Hypersycos.GERogueFrame
{
    public interface IListTarget
    {
        IList List { get; }
        public IEnumerable<T> ListAs<T>() => (List as IEnumerable<object>).Select(x => (T)x).NotNull();
        public IEnumerable<T> ListAsObject<T>() where T : UnityEngine.Object => (List as IEnumerable<object>).Select(x => (T)x).NotUnityNull();
    }

    public interface IListTarget<T> : IListTarget
    {
        new IList<T> List { get; }
        IList IListTarget.List => (IList)List;
    }

    public record ListTarget : TargetPayload, IListTarget
    {
        private List<object> results;

        public ListTarget(List<object> results)
        {
            this.results = results;
        }

        public IList List => results;
    }

    public record ListTarget<T> : TargetPayload, IListTarget<T>
    {
        private List<T> results;

        public ListTarget(List<T> results)
        {
            this.results = results;
        }

        public IList<T> List => results;
    }
}
