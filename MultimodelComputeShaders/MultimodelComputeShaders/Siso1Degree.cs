using System;
using System.Linq;

namespace MultimodelComputeShaders
{
    public class Siso1Degree : ISisoProcess
    {
        Random measurementNoiseGenerator = new Random();

        public float Kf
        {
            get;
            private set;
        }

        public float Tf
        {
            get;
            private set;
        }

        public float SampleTime
        {
            get
            {
                return Constants.SamplingTime;
            }
        }

        public float[] U { get; private set; } = new float[4];
        public float[] Y { get; private set; } = new float[4];
        public float[] A { get; private set; }
        public float[] B { get; private set; }
        public float[] C { get; private set; } = new float[5];

        public Siso1Degree(float kf, float tf)
        {
            Kf = kf;
            Tf = tf;
            float[][] discrete = DiscreteTransform.TustinTFTransformZminus(this.Kf, this.Tf, Constants.SamplingTime);
            B = discrete[0];
            A = discrete[1];

            C[4] = 0;
            C[3] = B[1] / A[0];
            C[2] = B[0] / A[0];
            C[1] = 0;
            C[0] = (-1) * A[1] / A[0];
        }

        public Siso1Degree(float[] c)
        {
            this.C = c;
            Kf = c[0] * (SampleTime - ((c[2] * SampleTime + SampleTime) / (c[2] * SampleTime - 1)));
            Tf = (-1) * ((c[2] * SampleTime + SampleTime) / (2 * c[2] * SampleTime - 2));
        }

        public float GetOutput(float uk, bool noisy = false)
        {
            U[3] = U[2];
            U[2] = U[1];
            U[1] = U[0];
            U[0] = uk;

            var yk = (B[0] * U[0] + (B[1] * U[1]) - (A[1] * Y[0])) / A[0];

            Y[3] = Y[2];
            Y[2] = Y[1];
            Y[1] = Y[0];
            Y[0] = yk;

            return yk;
        }


        public float GetOutput2(float uk, bool noisy = false)
        {
            U[3] = U[2];
            U[2] = U[1];
            U[1] = U[0];
            U[0] = uk;

            //var yk = 
            //    c[2] * u[0] + 
            //    c[1] * u[1] - 
            //    c[0] * y[0]; //(b[0] * u[0] + (b[1] * u[1]) - (a[1] * y[0])) / a[0];

            var yk =
                C[4] * U[2] +
                C[3] * U[1] +
                C[2] * U[0] +
                C[1] * Y[1] +
                C[0] * Y[0];

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

        public float GetOutputAverage()
        {
            //return y.Select(x => x != 0 ? x : y[0]).Average();
            return Y.Select(x => x).Average();
        }

        public override bool Equals(object obj)
        {
            return obj is Siso1Degree degree &&
                   Kf == degree.Kf &&
                   Tf == degree.Tf;
        }

        public override int GetHashCode()
        {
            int hashCode = -2076106397;
            hashCode = hashCode * -1521134295 + Kf.GetHashCode();
            hashCode = hashCode * -1521134295 + Tf.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"KF = {Kf}, TF ={Tf}";
        }

        public string SerializeAsMatlabTfCommand()
        {
            return $"c2d(tf([{Kf}], [{Tf}, 1]), {SampleTime}, 'tustin')";
        }

        public float[][] ToDiscrete()
        {
            return DiscreteTransform.TustinTFTransformZminus(Kf, Tf, Constants.SamplingTime);
        }
    }
}
