using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public interface IDrawIcon
    {
        void FullDrawIcon(Canvas container);
        void QuickDrawIcon(Canvas container);
    }
}