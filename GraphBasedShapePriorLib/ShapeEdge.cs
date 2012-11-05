using System;
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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            ShapeEdge objCasted = (ShapeEdge) obj;
            return this.Index1 == objCasted.Index1 && this.Index2 == objCasted.Index2;
        }

        public override int GetHashCode()
        {
            return this.Index1.GetHashCode() ^ this.Index2.GetHashCode();
        }

        public static bool operator == (ShapeEdge lhs, ShapeEdge rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ShapeEdge lhs, ShapeEdge rhs)
        {
            return !(lhs == rhs);
        }
    }
}