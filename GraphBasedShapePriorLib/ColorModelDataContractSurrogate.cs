using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    internal class GaussianMixtureSurrogated
    {
        [DataMember]
        private List<double> weights;

        [DataMember]
        private List<double[]> means;

        [DataMember]
        private List<double[][]> variances;

        public GaussianMixtureSurrogated(Mixture<VectorGaussian> mixture)
        {
            this.weights = new List<double>(mixture.Weights);

            this.means = new List<double[]>();
            this.variances = new List<double[][]>();
            for (int i = 0; i < mixture.Components.Count; ++i)
            {
                MicrosoftResearch.Infer.Maths.Vector mean = mixture.Components[i].GetMean();
                PositiveDefiniteMatrix variance = mixture.Components[i].GetVariance();

                this.means.Add(mean.ToArray());
                this.variances.Add(MatrixToJaggedArray(variance.ToArray()));
            }
        }

        public Mixture<VectorGaussian> ToMixture()
        {
            Mixture<VectorGaussian> mixture = new Mixture<VectorGaussian>();
            for (int i = 0; i < weights.Count; ++i)
                mixture.Add(
                    VectorGaussian.FromMeanAndVariance(
                        MicrosoftResearch.Infer.Maths.Vector.FromArray(this.means[i]),
                        new PositiveDefiniteMatrix(JaggedArrayToMatrix(this.variances[i]))),
                    this.weights[i]);
            return mixture;
        }

        private static double[][] MatrixToJaggedArray(double[,] matrix)
        {
            double[][] result = new double[matrix.GetLength(0)][];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = new double[matrix.GetLength(1)];
                for (int j = 0; j < result[i].Length; ++j)
                    result[i][j] = matrix[i, j];
            }
            return result;
        }

        private static double[,] JaggedArrayToMatrix(double[][] jaggedArray)
        {
            double[,] result = new double[jaggedArray.Length, jaggedArray[0].Length];
            for (int i = 0; i < jaggedArray.Length; ++i)
                for (int j = 0; j < jaggedArray[i].Length; ++j)
                    result[i, j] = jaggedArray[i][j];
            return result;
        }
    }
    
    internal class ColorModelDataContractSurrogate : IDataContractSurrogate
    {
        public Type GetDataContractType(Type type)
        {
            if (type == typeof(Mixture<VectorGaussian>))
                return typeof (GaussianMixtureSurrogated);

            return type;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj.GetType() == typeof(Mixture<VectorGaussian>))
            {
                Mixture<VectorGaussian> objCasted = (Mixture<VectorGaussian>) obj;
                return new GaussianMixtureSurrogated(objCasted);
            }

            return obj;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj.GetType() == typeof(GaussianMixtureSurrogated))
            {
                GaussianMixtureSurrogated objCasted = (GaussianMixtureSurrogated) obj;
                return objCasted.ToMixture();
            }

            return obj;
        }

        public object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
            throw new NotImplementedException();
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
    }
}
