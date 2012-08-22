using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Segmentator
{
    [Serializable]
    public class PropertyValidationException : Exception
    {
        public PropertyValidationException()
        {
        }

        public PropertyValidationException(string message)
            : base(message)
        {
        }

        public PropertyValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected PropertyValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
