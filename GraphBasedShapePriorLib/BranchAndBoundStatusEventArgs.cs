using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BreadthFirstBranchAndBoundStatusEventArgs : EventArgs
    {
        public double LowerBound { get; private set; }
        
        public int FrontSize { get; private set; }

        public double FrontItemsPerSecond { get; private set; }

        public Image StatusImage { get; private set; }

        public BreadthFirstBranchAndBoundStatusEventArgs(double lowerBound, int frontSize, double frontItemsPerSecond, Image statusImage)
        {
            Debug.Assert(statusImage != null);
            
            this.LowerBound = lowerBound;
            this.FrontSize = frontSize;
            this.FrontItemsPerSecond = frontItemsPerSecond;
            this.StatusImage = statusImage;
        }
    }
}