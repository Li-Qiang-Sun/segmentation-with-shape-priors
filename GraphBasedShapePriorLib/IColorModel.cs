using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public interface IColorModel
    {
        double LogProb(Color color);
    }
}
