using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public interface IBranchAndBoundShapeTermsCalculator
    {
        void CalculateShapeTerms(VertexConstraintSet constraints, Image2D<ObjectBackgroundTerm> result);
    }
}
