using System.Collections.Generic;
using Random = Research.GraphBasedShapePrior.Util.Random;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    static class Helper
    {
        public static void Subsample<T>(IList<T> items, int count)
        {
            if (items.Count <= count)
                return;
            
            for (int i = 0; i < count; ++i)
            {
                int swapIndex = Random.Int(i, items.Count - i);
                T tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }
    }
}
