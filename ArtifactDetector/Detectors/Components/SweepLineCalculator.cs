using System;
using System.Collections.Generic;
using System.Linq;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Detectors.Compontents
{
    /// <summary>
    /// Class to perform a sweep line algorithm to find the union of rectilinear rectangles.
    /// </summary>
    internal class RectangleUnionCalculator
    {
        /// <summary>
        /// Events of this sweep line calculator.
        /// </summary>
        private SweepLineEvents Events { get; set; }

        /// <summary>
        /// Segment tree of line segments in Y-direction.
        /// </summary>
        private SegmentTree SegmentTree { get; set; }

        /// <summary>
        /// Calculate the area of intersection of rectilinear rectangles.
        /// </summary>
        /// <param name="boundingRectangle">Bounding box for rectangle union.</param>
        /// <param name="overlayingRectangles">Rectangles of which we want to find the union.</param>
        /// <returns>The area of the intersection.</returns>
        public int CalculateRectangleUnion(Rectangle boundingRectangle, IList<Rectangle> overlayingRectangles)
        {
            // Get events filled for sweep line algorithm.
            FillAvailableEvents(boundingRectangle, overlayingRectangles);

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

        private void FillAvailableEvents(Rectangle boundingRectangle, IList<Rectangle> overlayingRectangles)
        {
            Events = new SweepLineEvents();

            Rectangle intersectionRectangle;
            foreach (var currentRectangle in overlayingRectangles)
            {
                // Try to find intersection, throws ArgumentException if there is none.
                try
                {
                    // Get rectangle intersection between overlapping rectangle and bounding box.
                    intersectionRectangle = Intersection(boundingRectangle, currentRectangle);

                    AddIntersectionRectangleEvents(intersectionRectangle);
                }
                catch (ArgumentException)
                { }
            }
        }

        /// <summary>
        /// Get intersection rectangle of the two rectangles.
        ///
        /// Throws an ArgumentException if there is none.
        /// </summary>
        /// <param name="firstRectangle">Order does not matter.</param>
        /// <param name="secondRectangle">Order does not matter.</param>
        /// <returns>The intersection rectangle.</returns>
        private Rectangle Intersection(Rectangle firstRectangle, Rectangle secondRectangle)
        {
            int left = Math.Max(firstRectangle.Left, secondRectangle.Left);
            int right = Math.Min(firstRectangle.Right, secondRectangle.Right);

            if (left >= right)
            {
                throw new ArgumentException("No intersection possible.");
            }

            int top = Math.Max(firstRectangle.Top, secondRectangle.Top);
            int bottom = Math.Min(firstRectangle.Bottom, secondRectangle.Bottom);

            if (top >= bottom)
            {
                throw new ArgumentException("No intersection possible.");
            }

            return new Rectangle(left, top, right, bottom);
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

            /// <summary>
            /// Constructor with values.
            /// </summary>
            /// <param name="segmentEntries">Array of segmentEntry.</param>
            public Segments(SegmentEntry[] segmentEntries)
            {
                Entries = segmentEntries.ToList();
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
