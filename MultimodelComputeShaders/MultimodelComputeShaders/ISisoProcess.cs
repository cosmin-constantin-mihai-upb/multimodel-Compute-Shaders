using System;

namespace MultimodelComputeShaders
{
    public interface ISisoProcess
    {
        float[] Y { get; }
        float[] C { get; }
        float[] U { get; }
        float[] A { get; }
        float[] B { get; }

        float GetOutput(float uk, bool noisy = false);
        float GetOutput2(float uk, bool noisy = false);
        string SerializeAsMatlabTfCommand();
        float GetOutputAverage();
        float[][] ToDiscrete();
    }

    public static class ISisoFactory
    {
        public static ISisoProcess CreateProcess(float[] c)
        {
            if (c[1] == 0 && c[4] == 0) return new Siso1Degree(c);
            if (c.Length == 5) return new Siso2Degree(c);
            throw new NotSupportedException();
        }
    }
}