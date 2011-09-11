using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Maths;

namespace Research.GraphBasedShapePrior
{
    public static class ColorUtils
    {
        public static MicrosoftResearch.Infer.Maths.Vector ToInferNetVector(this Color color)
        {
            return MicrosoftResearch.Infer.Maths.Vector.FromArray(color.R / 255.0, color.G / 255.0, color.B / 255.0);
        }
    }
}
