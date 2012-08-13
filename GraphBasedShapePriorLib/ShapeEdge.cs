using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public struct ShapeEdge
    {
        [DataMember]
        public int Index1 { get; set; }

        [DataMember]
        public int Index2 { get; set; }

        public ShapeEdge(int index1, int index2)
            : this()
        {
            this.Index1 = index1;
            this.Index2 = index2;
        }
    }
}