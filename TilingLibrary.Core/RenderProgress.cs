using System;
using System.Collections.Generic;

namespace Artwork
{
    /// <summary>
    /// Reports progress during artwork rendering operations.
    /// </summary>
    public class RenderProgress
    {
        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Current stage description (e.g., "Building QuadTree", "Subdividing").
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Intermediate polygon results for progressive rendering.
        /// May be null if not applicable to the current stage.
        /// </summary>
        public List<Tiling.Polygon> IntermediatePolygons { get; set; }

        /// <summary>
        /// Whether this progress report indicates the operation is complete.
        /// </summary>
        public bool IsComplete { get; set; }

        public RenderProgress() { }

        public RenderProgress(int percent, string stage)
        {
            PercentComplete = percent;
            Stage = stage;
        }
    }
}
