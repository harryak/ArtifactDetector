using System;
using System.Collections.Generic;
using System.Linq;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Utilities
{
    /// <summary>
    /// Class to perform a sweep line algorithm to find the union of rectilinear rectangles.
    /// </summary>
    public class RectangleUnionCalculator
    {
        /// <summary>
        /// Events of this sweep line calculator.
        /// </summary>
        private SweepLineEvents Events { get; set; }

        /// <summary>
        /// Calculate the area of intersection of rectilinear rectangles.
        /// </summary>
        /// <param name="boundingRectangle">Bounding box for rectangle union.</param>
        /// <param name="overlayingRectangles">Rectangles of which we want to find the union.</param>
        /// <returns>The area of the intersection.</returns>
        public int CalculateRectangleUnion(Rectangle boundingRectangle, ICollection<Rectangle> overlayingRectangles)
        {
            // Get events filled for sweep line algorithm.
            if (!FillAvailableEvents(boundingRectangle, overlayingRectangles))
            {
                return boundingRectangle.Area;
            }

            // Rule out trivial case of no events.
            if (Events.XEvents.Count < 1 || Events.YEvents.Count < 1)
            {
                return 0;
            }

            // Sweep line over ordered events on X-axis.
            return PerformSweepAlgorithm();
        }

        private void AddIntersectionRectangleEvents(Rectangle intersectionRectangle)
        {
            var verticalLineSegment = new LineSegment(intersectionRectangle.Top, intersectionRectangle.Bottom);

            // Add left X-coordinate entry to events list.
            Events.AddXEventSegment(intersectionRectangle.Left, new SegmentEntry(true, verticalLineSegment));

            // Add right X-coordinate entry to events list.
            Events.AddXEventSegment(intersectionRectangle.Right, new SegmentEntry(false, verticalLineSegment));

            // Add Y-coordinates, if necessary.
            Events.AddYEvent(intersectionRectangle.Top);
            Events.AddYEvent(intersectionRectangle.Bottom);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boundingRectangle"></param>
        /// <param name="overlayingRectangles"></param>
        /// <returns>False if we already know the overlaying rectangles fill the whole area, true per default.</returns>
        private bool FillAvailableEvents(Rectangle boundingRectangle, ICollection<Rectangle> overlayingRectangles)
        {
            Events = new SweepLineEvents();

            Rectangle intersectionRectangle;
            foreach (var currentRectangle in overlayingRectangles)
            {
                // Get rectangle intersection between overlapping rectangle and bounding box.
                int intersectionLeft = Math.Max(currentRectangle.Left, boundingRectangle.Left);
                int intersectionRight = Math.Min(currentRectangle.Right, boundingRectangle.Right);

                if (intersectionLeft >= 0 && intersectionLeft >= intersectionRight)
                {
                    continue;
                }

                int intersectionTop = Math.Max(currentRectangle.Top, boundingRectangle.Top);
                int intersectionBottom = Math.Min(currentRectangle.Bottom, boundingRectangle.Bottom);

                if (intersectionTop >= intersectionBottom)
                {
                    continue;
                }

                // If we arrive here there is a valid intersection.
                intersectionRectangle = new Rectangle(intersectionLeft, intersectionTop, intersectionRight, intersectionBottom);

                if (intersectionRectangle.Area >= boundingRectangle.Area)
                {
                    return false;
                }

                AddIntersectionRectangleEvents(intersectionRectangle);
            }

            return true;
        }

        /// <summary>
        /// Performs the actial sweep line algorithm and returns the found union area.
        /// </summary>
        /// <returns>Union area for Events.</returns>
        private int PerformSweepAlgorithm()
        {
            var segmentTree = new SegmentTree(Events.YEvents.ToArray());
            int unionArea = 0;
            int previousXEvent = int.MinValue;
            foreach (var xEvent in Events.XEvents)
            {
                // If we are not at the first abscissa:
                if (previousXEvent > int.MinValue)
                {
                    // Add rectangle to total.
                    unionArea += (xEvent.Key - previousXEvent) * segmentTree.GetActiveLength();
                }

                // For all intervals in list of this event: Activate in segment tree.
                foreach (var interval in xEvent.Value.Entries)
                {
                    if (interval.LeftEdge)
                    {
                        segmentTree.ActivateInterval(interval.Line.Start, interval.Line.End);
                    }
                    else
                    {
                        segmentTree.DeactivateInterval(interval.Line.Start, interval.Line.End);
                    }
                }

                previousXEvent = xEvent.Key;
            }

            return unionArea;
        }

        /// <summary>
        /// Data class for a 1D line from start point to end point.
        /// </summary>
        private class LineSegment
        {
            /// <summary>
            /// Constructor of a line segment.
            /// </summary>
            /// <param name="start">1D start point.</param>
            /// <param name="end">1D end point.</param>
            public LineSegment(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int End { get; private set; }
            public int Start { get; private set; }
        }

        /// <summary>
        /// An entry for a line segment.
        /// </summary>
        private class SegmentEntry
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="leftEdge">Flag whether this is the left edge (true) of a rectangle.</param>
            /// <param name="line">Line object of this entry.</param>
            public SegmentEntry(bool leftEdge, LineSegment line)
            {
                LeftEdge = leftEdge;
                Line = line;
            }

            public bool LeftEdge { get; private set; }
            public LineSegment Line { get; private set; }
        }

        /// <summary>
        /// Container for a list of segmentEntries
        /// </summary>
        private class Segments
        {
            /// <summary>
            /// Empty constructor.
            /// </summary>
            public Segments()
            {
                Entries = new List<SegmentEntry>();
            }

            public List<SegmentEntry> Entries { get; private set; }
        }

        /// <summary>
        /// Wrapper for sweep line events.
        /// </summary>
        private class SweepLineEvents
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public SweepLineEvents()
            {
                XEvents = new SortedList<int, Segments>();
                YEvents = new SortedSet<int>();
            }

            /// <summary>
            /// Add a segment at the specified event point.
            /// </summary>
            /// <param name="newEvent">Event point.</param>
            /// <param name="segment">Segment to add to the event point.</param>
            public void AddXEventSegment(int newEvent, SegmentEntry segment)
            {
                if (!XEvents.ContainsKey(newEvent))
                {
                    XEvents.Add(newEvent, new Segments());
                }
                XEvents[newEvent].Entries.Add(segment);
            }

            /// <summary>
            /// Add an event point (if it does not exist).
            /// </summary>
            /// <param name="newEvent">Event point to add.</param>
            public void AddYEvent(int newEvent)
            {
                YEvents.Add(newEvent);
            }

            public SortedList<int, Segments> XEvents { get; private set; }
            public SortedSet<int> YEvents { get; private set; }
        }
    }
}
