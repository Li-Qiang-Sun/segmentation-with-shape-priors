using System;
using System.ComponentModel;

namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    class VectorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(Vector) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                Vector v;
                if (!TryParseVector((string) value, out v))
                    throw new FormatException("Invalid vector format!");
                return v;
            }
            
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector)
            {
                Vector vector = (Vector) value;
                return string.Format("{0} {1}", vector.X, vector.Y);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        private bool TryParseVector(string str, out Vector v)
        {
            v = Vector.Zero;
            string[] parts = str.Split();
            double x, y;
            if (parts.Length == 2 && double.TryParse(parts[0], out x) && double.TryParse(parts[1], out y))
            {
                v = new Vector(x, y);
                return true;
            }

            return false;
        }
    }
}