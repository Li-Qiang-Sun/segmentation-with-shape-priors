using System.Collections;
using System.Collections.Generic;

namespace Research.GraphBasedShapePrior
{
    public class ExposableCollection<T> : IEnumerable<T>
    {
        private readonly IList<T> exposeWhat;

        public ExposableCollection(IList<T> exposeWhat)
        {
            this.exposeWhat = exposeWhat;
        }

        public T this[int index]
        {
            get { return this.exposeWhat[index]; }

            set { this.exposeWhat[index] = value; }
        }

        public int Count
        {
            get { return this.exposeWhat.Count; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.exposeWhat.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}