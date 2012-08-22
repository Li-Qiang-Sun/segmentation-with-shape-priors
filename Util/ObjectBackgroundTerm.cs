namespace Research.GraphBasedShapePrior.Util
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

        public ObjectBackgroundTerm Shift(double shift)
        {
            return new ObjectBackgroundTerm(this.ObjectTerm + shift, this.BackgroundTerm + shift);
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
