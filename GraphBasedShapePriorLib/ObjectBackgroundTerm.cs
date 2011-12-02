using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public struct ObjectBackgroundTerm
    {
        public double ObjectTerm { get; set; }

        public double BackgroundTerm { get; set; }

        public static readonly ObjectBackgroundTerm Zero = new ObjectBackgroundTerm(0, 0);

        public ObjectBackgroundTerm(double objectTerm, double backgroundTerm)
            : this()
        {
            this.ObjectTerm = objectTerm;
            this.BackgroundTerm = backgroundTerm;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return this == (ObjectBackgroundTerm) obj;
        }

        public override int GetHashCode()
        {
            return this.ObjectTerm.GetHashCode() ^ this.BackgroundTerm.GetHashCode();
        }

        public static bool operator == (ObjectBackgroundTerm lhs, ObjectBackgroundTerm rhs)
        {
            return lhs.ObjectTerm == rhs.ObjectTerm && lhs.BackgroundTerm == rhs.BackgroundTerm;
        }

        public static bool operator !=(ObjectBackgroundTerm lhs, ObjectBackgroundTerm rhs)
        {
            return lhs.ObjectTerm != rhs.ObjectTerm || lhs.BackgroundTerm != rhs.BackgroundTerm;
        }
    }
}
