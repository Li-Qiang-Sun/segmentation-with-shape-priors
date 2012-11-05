using System;
using System.IO;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior.Util
{
    public static class Helper
    {
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T tmp = item1;
            item1 = item2;
            item2 = tmp;
        }

        public static T LoadFromFile<T>(string fileName, IDataContractSurrogate surrogate)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                DataContractSerializer serializer = CreateSerializer<T>(surrogate);
                return (T)serializer.ReadObject(stream);
            }
        }

        public static T LoadFromFile<T>(string fileName)
        {
            return LoadFromFile<T>(fileName, null);
        }

        public static void SaveToFile<T>(string fileName, T saveWhat, IDataContractSurrogate surrogate)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                DataContractSerializer serializer = CreateSerializer<T>(surrogate);
                serializer.WriteObject(stream, saveWhat);
            }
        }

        public static void SaveToFile<T>(string fileName, T saveWhat)
        {
            SaveToFile(fileName, saveWhat, null);
        }

        private static DataContractSerializer CreateSerializer<T>(IDataContractSurrogate surrogate)
        {
            return new DataContractSerializer(typeof(T), new Type[] { }, Int32.MaxValue, false, true, surrogate);
        }
    }
}
