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
        protected float CalculateWindowVisibility(Rectangle queriedWindow, ICollection<Rectangle> windowsAbove)
        {
            // If there are no windows above: Return immediately.
            if (windowsAbove.Count < 1)
            {
                return 100f;
            }

            // If there is no area of the window, return "no visibility".
            if (queriedWindow.Area < 1)
            {
                return 0f;
            }

            int subtractArea = new RectangleUnionCalculator().CalculateRectangleUnion(queriedWindow, windowsAbove);

            return (float)(queriedWindow.Area - subtractArea) / queriedWindow.Area * 100f;
        }
    }
}
