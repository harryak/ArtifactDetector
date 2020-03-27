namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Implementation of a segment tree for union of rectangles sweep line algorithm.
    /// </summary>
    internal class SegmentTree
    {
        /// <summary>
        /// Encapsulation of TreeNode data type for access control.
        /// </summary>
        private readonly TreeNode rootNode;

        /// <summary>
        /// Copy of the values for class access.
        /// </summary>
        private readonly int[] values;

        /// <summary>
        /// Construct a new segment tree given the values.
        /// </summary>
        /// <param name="values"></param>
        public SegmentTree(int[] values)
        {
            this.values = values;

            rootNode = BuildTree(0, values.Length - 1);
        }

        /// <summary>
        /// Activate the interval between the given values.
        /// </summary>
        /// <param name="intervalStart"></param>
        /// <param name="intervalEnd"></param>
        public void ActivateInterval(int intervalStart, int intervalEnd)
        {
            rootNode.ActivateInterval(intervalStart, intervalEnd);
        }

        /// <summary>
        /// Deactivate the interval between the given values.
        /// </summary>
        /// <param name="intervalStart"></param>
        /// <param name="intervalEnd"></param>
        public void DeactivateInterval(int intervalStart, int intervalEnd)
        {
            rootNode.DeactivateInterval(intervalStart, intervalEnd);
        }

        /// <summary>
        /// Get the length of the currently active interval (summed up).
        /// </summary>
        /// <returns>The length.</returns>
        public int GetActiveLength()
        {
            return rootNode.ActiveLength;
        }

        /// <summary>
        /// Build tree from values array.
        /// </summary>
        /// <param name="indexMin"></param>
        /// <param name="indexMax"></param>
        /// <returns></returns>
        private TreeNode BuildTree(int indexMin, int indexMax)
        {
            if (indexMax - indexMin < 1)
            {
                // Leaf node.
                return new TreeNode()
                {
                    ValueStart = values[indexMin],
                    ValueEnd = values[indexMax],
                    IntervalLength = values[indexMax] - values[indexMin]
                };
            }
            else
            {
                // Common tree node.
                return new TreeNode()
                {
                    ValueStart = values[indexMin],
                    ValueEnd = values[indexMax],
                    IntervalLength = values[indexMax] - values[indexMin],
                    ChildLeft = BuildTree(indexMin, indexMax / 2),
                    ChildRight = BuildTree(indexMax / 2, indexMax)
                };
            }
        }

        /// <summary>
        /// Encapsulate actual segment tree for easier internal, but no external access of properties.
        /// </summary>
        private class TreeNode
        {
            /// <summary>
            /// Counter of how many times this node is active.
            /// </summary>
            public int ActiveCounter { get; set; } = 0;

            /// <summary>
            /// Length of the single parts of this interval which are active.
            /// </summary>
            public int ActiveLength { get; set; } = 0;

            /// <summary>
            /// Left subtree, null on leaf nodes.
            /// </summary>
            public TreeNode ChildLeft { get; set; }

            /// <summary>
            /// Right subtree, null on leaf nodes.
            /// </summary>
            public TreeNode ChildRight { get; set; }

            /// <summary>
            /// Total length of this interval (only used for leaf nodes).
            /// </summary>
            public int IntervalLength { get; set; }

            /// <summary>
            /// Ending value of this interval
            /// </summary>
            public int ValueEnd { get; set; }

            /// <summary>
            /// Starting value of this interval.
            /// </summary>
            public int ValueStart { get; set; }

            /// <summary>
            /// Update the interval by incrementing the counters.
            /// </summary>
            /// <param name="intervalStart"></param>
            /// <param name="intervalEnd"></param>
            public void ActivateInterval(int intervalStart, int intervalEnd)
            {
                UpdateInterval(intervalStart, intervalEnd, true);
            }

            /// <summary>
            /// Update the interval by decrementing the counters.
            /// </summary>
            /// <param name="intervalStart"></param>
            /// <param name="intervalEnd"></param>
            public void DeactivateInterval(int intervalStart, int intervalEnd)
            {
                UpdateInterval(intervalStart, intervalEnd, false);
            }

            /// <summary>
            /// Update the given interval or sub-interval.
            /// </summary>
            /// <param name="intervalStart"></param>
            /// <param name="intervalEnd"></param>
            /// <param name="increment"></param>
            public void UpdateInterval(int intervalStart, int intervalEnd, bool increment)
            {
                // Check if we are on a leaf. Since this is a private class omit check for ChildRight == null.
                if (ChildLeft == null)
                {
                    // Choose action based on flag.
                    if (increment)
                    {
                        ActiveCounter++;
                    }
                    else
                    {
                        ActiveCounter--;
                    }

                    // Set active length "on" or "off" based on counter.
                    if (ActiveCounter < 1)
                    {
                        ActiveLength = 0;
                    }
                    else
                    {
                        ActiveLength = IntervalLength;
                    }

                    return;
                }

                // Not on a leaf node!
                // Do we need to update the left child? Then update it.
                if (intervalStart < ChildLeft.ValueEnd)
                {
                    ChildLeft.UpdateInterval(intervalStart, intervalEnd, increment);
                }

                // Do we need to update the right child? Then update it.
                if (intervalEnd > ChildRight.ValueStart)
                {
                    ChildRight.UpdateInterval(intervalStart, intervalEnd, increment);
                }

                // Set active length "recursively" to have value readily available.
                ActiveLength = ChildLeft.ActiveLength + ChildRight.ActiveLength;
            }
        }
    }
}
