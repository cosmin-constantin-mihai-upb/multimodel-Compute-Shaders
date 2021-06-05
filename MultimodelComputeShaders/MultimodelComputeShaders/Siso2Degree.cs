using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public class Siso2Degree : ISisoProcess
    {
        Random measurementNoiseGenerator = new Random();

        public float[] U { get; private set; } = new float[4];
        public float[] Y { get; private set; } = new float[4];
        public float[] A { get; private set; }
        public float[] B { get; private set; }
        public float[] C { get; private set; } = new float[5];

        public float L;
        public float K;
        public float W;

        public float SamplingTime
        {
            get
            {
                return Constants.SamplingTime;
            }
        }

        public Siso2Degree(float[] c)
        {
            this.C = c;
        }

        public Siso2Degree(float l, float k, float w)
        {
            L = l;
            K = k;
            W = w;

            var components = ToDiscrete();
            this.A = components[0];
            this.B = components[1];

            C[0] = (-1) * A[1] / A[0]; //(-1)
            C[1] = (-1) * A[2] / A[0]; //(-1)
            C[2] = B[0] / A[0];
            C[3] = B[1] / A[0];
            C[4] = B[2] / A[0];
        }

        public float GetOutput2(float uk, bool noisy = false)
        {
            U[3] = U[2];
            U[2] = U[1];
            U[1] = U[0];
            U[0] = uk;

            var yk = C[4] * U[2] + C[3] * U[1] + C[2] * U[0] + C[1] * Y[1] + C[0] * Y[0];

            Y[3] = Y[2];
            Y[2] = Y[1];
            Y[1] = Y[0];
            Y[0] = yk;

            if (noisy)
            {
                var ratio = measurementNoiseGenerator.Next(95, 106);
                yk = yk * ratio / 100;
            }

            return yk;
        }


        public float GetOutput(float uk, bool noisy = false)
        {
            U[3] = U[2];
            U[2] = U[1];
            U[1] = U[0];
            U[0] = uk;

            var yk = (B[2] * U[2] + B[1] * U[1] + B[0] * U[0] - A[2] * Y[1] - A[1] * Y[0]) / A[0];

            Y[3] = Y[2];
            Y[2] = Y[1];
            Y[1] = Y[0];
            Y[0] = yk;

            if (noisy)
            {
                var ratio = measurementNoiseGenerator.Next(95, 106);
                yk = yk * ratio / 100;
            }

            return yk;
        }

        public string SerializeAsMatlabTfCommand()
        {
            return $"c2d(tf([{W}], [{L}, {K}, {W}]), {SamplingTime}, 'tustin')";
        }

        public float GetOutputAverage()
        {
            return Y.Select(x => x).Average();
        }

        public float[][] ToDiscrete()
        {
            return DiscreteTransform.TustinTF2ndDegreeSystem(L: L, K: K, w: W, SamplingTime);
        }
    }
}
