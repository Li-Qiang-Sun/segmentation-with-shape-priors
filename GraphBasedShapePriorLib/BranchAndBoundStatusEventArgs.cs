using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundStatusEventArgs : EventArgs
    {
        public double Energy { get; private set; }
        
        public int FrontSize { get; private set; }

        public double FrontItemsPerSecond { get; private set; }

        public Image StatusImage { get; private set; }

        public BranchAndBoundStatusEventArgs(double lowerBound, int frontSize, double frontItemsPerSecond, Image statusImage)
        {
            Debug.Assert(statusImage != null);
            
            this.Energy = lowerBound;
            this.FrontSize = frontSize;
            this.FrontItemsPerSecond = frontItemsPerSecond;
            this.StatusImage = statusImage;
        }
    }
}