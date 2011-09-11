using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public struct Circle
    {
        public Vector Center { get; set; }

        public double Radius { get; set; }

        public Circle(Vector center, double radius)
            : this()
        {
            Debug.Assert(radius >= 0);

            this.Center = center;
            this.Radius = radius;
        }

        public bool Contains(Circle circle)
        {
            return circle.Radius < this.Radius &&
                   (circle.Center - this.Center).LengthSquared < this.Radius * this.Radius;
        }
    }
}
