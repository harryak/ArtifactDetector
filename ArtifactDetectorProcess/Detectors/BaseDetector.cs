using System.Collections.Generic;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetectorProcess.Detectors
{
    internal class BaseDetector
    {
        /// <summary>
        /// Calculates how much (percentage) of the queriedWindow is visible other windows above.
        /// </summary>
        /// <param name="queriedWindow">Queried window.</param>
        /// <param name="windowsAbove">The windows above (z-index) the queried window.</param>
        /// <returns>The percentage of how much of the window is visible.</returns>
        protected int CalculateWindowVisibility(Rectangle queriedWindow, ICollection<Rectangle> windowsAbove)
        {
            // If there are no windows above: Return immediately.
            if (windowsAbove.Count < 1)
            {
                return 100;
            }

            // If there is no area of the window, return "no visibility".
            if (queriedWindow.Area < 1)
            {
                return 0;
            }

            int subtractArea = new RectangleUnionCalculator().CalculateRectangleUnion(queriedWindow, windowsAbove);
            // Calculate ratio as exactly as possible, force double division here.
            double visibleAreaRatio = 100d * ((queriedWindow.Area - subtractArea) / queriedWindow.Area);
            // Cut everything to integer.
            return (int)visibleAreaRatio;
        }
    }
}
