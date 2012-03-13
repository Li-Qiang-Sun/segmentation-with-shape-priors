using System.ComponentModel;

namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    class ProblemProperties
    {
        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Box1Min { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Box1Max { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Box2Min { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Box2Max { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Lambda { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Lambda1 { get; set; }

        [TypeConverter(typeof(VectorTypeConverter))]
        public Vector Lambda2 { get; set; }

        public double DistanceWeight { get; set; }

        public uint Iterations { get; set; }

        public ProblemProperties()
        {
            this.Box1Min = new Vector(0.1, 0.15);
            this.Box1Max = new Vector(0.8, 0.6);
            this.Box2Min = new Vector(0.7, 0.5);
            this.Box2Max = new Vector(1.1, 1.3);
            this.DistanceWeight = 1;
            this.Iterations = 100000;
        }
    }
}
