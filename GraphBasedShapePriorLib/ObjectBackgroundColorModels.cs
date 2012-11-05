using System;
using System.IO;
using System.Runtime.Serialization;
using Research.GraphBasedShapePrior.Util;

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
            return Helper.LoadFromFile<ObjectBackgroundColorModels>(fileName, new ColorModelDataContractSurrogate());
        }

        public void SaveToFile(string fileName)
        {
            Helper.SaveToFile(fileName, this, new ColorModelDataContractSurrogate());
        }
    }
}
