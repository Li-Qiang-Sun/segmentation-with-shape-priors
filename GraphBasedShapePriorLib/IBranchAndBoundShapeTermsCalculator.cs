﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public interface IShapeTermsLowerBoundCalculator
    {
        void CalculateShapeTerms(ShapeConstraints constraints, Image2D<ObjectBackgroundTerm> result);
    }
}