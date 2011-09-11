namespace Research.GraphBasedShapePrior
{
    public struct ShapeEdge
    {
        public int Index1 { get; set; }

        public int Index2 { get; set; }

        public ShapeEdge(int index1, int index2)
            : this()
        {
            this.Index1 = index1;
            this.Index2 = index2;
        }
    }
}