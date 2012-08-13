using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    static class Helper
    {
        public static void Subsample<T>(IList<T> items, int count)
        {
            if (items.Count <= count)
                return;
            
            Random random = new Random();
            for (int i = 0; i < count; ++i)
            {
                int swapIndex = i + random.Next(items.Count - i);
                T tmp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = tmp;
            }
        }
    }
}
