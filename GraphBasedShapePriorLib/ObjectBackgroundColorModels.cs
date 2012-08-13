using System;
using System.IO;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    [KnownType(typeof(GaussianMixtureColorModel))]
    public class ObjectBackgroundColorModels
    {
        [DataMember]
        public IColorModel ObjectColorModel { get; private set; }

        [DataMember]
        public IColorModel BackgroundColorModel { get; private set; }

        public ObjectBackgroundColorModels(IColorModel objectColorModel, IColorModel backgroundColorModel)
        {
            if (objectColorModel == null)
                throw new ArgumentNullException("objectColorModel");
            if (backgroundColorModel == null)
                throw new ArgumentNullException("backgroundColorModel");

            this.ObjectColorModel = objectColorModel;
            this.BackgroundColorModel = backgroundColorModel;
        }

        public static ObjectBackgroundColorModels LoadFromFile(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                DataContractSerializer serializer = CreateSerializer();
                return (ObjectBackgroundColorModels)serializer.ReadObject(stream);
            }
        }

        public void SaveToFile(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                DataContractSerializer serializer = CreateSerializer();
                serializer.WriteObject(stream, this);
            }
        }

        private static DataContractSerializer CreateSerializer()
        {
            return new DataContractSerializer(
                typeof(ObjectBackgroundColorModels), new Type[] { }, Int32.MaxValue, false, true, new ColorModelDataContractSurrogate());
        }
    }
}
