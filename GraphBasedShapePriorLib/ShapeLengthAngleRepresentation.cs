using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class ShapeLengthAngleRepresentation
    {
        private readonly ExposableCollection<double> edgeLengths;

        private readonly ExposableCollection<double> edgeAngles;

        private readonly ExposableCollection<double> edgeWidths;

        public ShapeLengthAngleRepresentation(ShapeStructure structure, Vector origin, IEnumerable<double> edgeLengths, IEnumerable<double> edgeAngles, IEnumerable<double> edgeWidths)
        {
            if (edgeLengths == null)
                throw new ArgumentNullException("edgeLengths");
            if (edgeAngles == null)
                throw new ArgumentNullException("edgeAngles");
            if (edgeWidths == null)
                throw new ArgumentNullException("edgeWidths");
            if (structure == null)
                throw new ArgumentNullException("structure");

            this.edgeLengths = new ExposableCollection<double>(edgeLengths.ToList());
            this.edgeAngles = new ExposableCollection<double>(edgeAngles.ToList());
            this.edgeWidths = new ExposableCollection<double>(edgeWidths.ToList());
            this.Structure = structure;
            this.Origin = origin;

            if (this.edgeLengths.Count != this.Structure.Edges.Count)
                throw new ArgumentException("Edge lengths count should be equal to edge count.", "edgeLengths");
            if (this.edgeAngles.Count != this.Structure.Edges.Count)
                throw new ArgumentException("Edge angles count should be equal to edge count.", "edgeAngles");
            if (this.edgeWidths.Count != this.Structure.Edges.Count)
                throw new ArgumentException("Edge widths count should be equal to edge count.", "edgeWidths");
        }

        public ExposableCollection<double> EdgeLengths
        {
            get { return this.edgeLengths; }
        }

        public ExposableCollection<double> EdgeAngles
        {
            get { return this.edgeAngles; }
        }

        public ExposableCollection<double> EdgeWidths
        {
            get { return this.edgeWidths; }
        }
        
        public ShapeStructure Structure { get; private set; }

        public Vector Origin { get; private set; }
    }
}
