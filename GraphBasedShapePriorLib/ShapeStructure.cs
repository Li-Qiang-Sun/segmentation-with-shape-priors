using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeStructure
    {
        [DataMember]
        private List<ShapeEdge> edges;

        public ShapeStructure(IList<ShapeEdge> edges)
        {
            if (edges == null)
                throw new ArgumentNullException("edges");
            if (edges.Count == 0)
                throw new ArgumentException("Model should contain at least one edge.", "edges");

            // Check if all the edge vertex indices are valid
            if (edges.Any(shapeEdge => shapeEdge.Index1 < 0 || shapeEdge.Index2 < 0))
                throw new ArgumentException("Some of the edges have negative vertex indices.", "edges");
            
            this.edges = new List<ShapeEdge>(edges);

            // Vertex count is implicitly defined by the maximum index
            this.VertexCount = edges.Max(e => Math.Max(e.Index1, e.Index2)) + 1;
        }

        [DataMember]
        public int VertexCount { get; private set; }

        public ReadOnlyCollection<ShapeEdge> Edges
        {
            get { return this.edges.AsReadOnly(); }
        }
    }
}
