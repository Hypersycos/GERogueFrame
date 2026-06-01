using UnityEngine;

namespace Hypersycos.Utils
{
    public static class ColorComplementExtension
    {
        public static Color ContrastColor(this Color iColor)
        {
            // Calculate the perceptive luminance (aka luma) - human eye favors green color... 
            double luma = (0.299 * iColor.r) + (0.587 * iColor.g) + (0.114 * iColor.b);

            // Return black for bright colors, white for dark colors
            return luma > 0.5 ? Color.black : Color.white;
        }
    }
}