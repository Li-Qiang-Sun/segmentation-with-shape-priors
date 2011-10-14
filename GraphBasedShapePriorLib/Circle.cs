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
            return circle.Radius <= this.Radius &&
                   (circle.Center - this.Center).LengthSquared <= MathHelper.Sqr(this.Radius - circle.Radius);
        }

        public override string ToString()
        {
            return string.Format("C={0} R={1}", this.Center, this.Radius);
        }
    }
}
