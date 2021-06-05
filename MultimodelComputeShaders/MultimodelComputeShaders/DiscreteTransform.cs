using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public static class DiscreteTransform
    {
        /// <summary>
        /// Tranforms the tf defined as TK/(TF*s+1) with tustin method and sampling time T
        /// </summary>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <param name="T"></param>
        /// <returns></returns>
        public static float[][] TustinTFTransform(float KF, float TF, float T)
        {
            float[] numerator = new float[2];
            float[] denumerator = new float[2];

            float a0 = 1;
            float a1 = TF;
            float b0 = KF;

            numerator[0] = (float)Math.Round((T * b0 / ((2 * a1) + (T * a0))), 4);
            numerator[1] = (float)Math.Round((b0 * T) / ((2 * a1) + (T * a0)), 4);

            denumerator[1] = 1;
            denumerator[0] = (float)Math.Round((a0 - 2 * a1) / ((2 * a1) + (T * a0)), 4);

            float[][] result = new float[2][];
            result[0] = numerator;
            result[1] = denumerator;
            return result;
        }


        public static float[][] TustinTFTransformZminus(float KF, float TF, float T)
        {
            float[] numerator = new float[2];
            float[] denumerator = new float[2];

            float a0 = 1;

            numerator[0] = (float)Math.Round(KF * T, 4);
            numerator[1] = (float)Math.Round(KF, 4);
           
            denumerator[0] = (float)Math.Round((2 * TF) + (a0 * T), 4);
            denumerator[1] = (float)Math.Round((a0 * T) - (2 * TF), 4);

            float[][] result = new float[2][];
            result[0] = numerator;
            result[1] = denumerator;
            return result;
        }
        
        /// <summary>
        /// Transforms a 2nd order system in the shape of 
        /// w /(L*s^2 + K*s + W) to discrete representation
        /// </summary>
        /// <param name="L"></param>
        /// <param name="K"></param>
        /// <param name="w"></param>
        /// <param name="T"></param>
        /// <returns></returns>
        public static float[][] TustinTF2ndDegreeSystem(float L, float K, float w, float T)
        {
            float[][] returnValue = new float[2][];
            float[] a = new float[3];
            float[] b = new float[3];

            float TSquared = (float)Math.Pow(T, 2);

            a[2] = 4 * L - 2 * K * T + w * TSquared;
            a[1] = -8 * L + 0 + 2 * w * TSquared;
            a[0] = 4 * L + 2 * K * T + w * TSquared;

            b[2] = TSquared * w;
            b[1] = TSquared * 2 * w;
            b[0] = TSquared * w;

            returnValue[0] = a;
            returnValue[1] = b;
            return returnValue;
        }
    }
}
