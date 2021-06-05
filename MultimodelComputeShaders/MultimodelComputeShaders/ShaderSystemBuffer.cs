using System.Runtime.InteropServices;

namespace MultimodelComputeShaders
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderSystemBuffer
    {       
        public float K1, K2, K3;           //pid coeficients
        public float R;                    //reference
        public float u0, u1, u2, u3;       //past commands
        public float e0, e1, e2, e3;       //output errors
        public float y0, y1, y2, y3;       //past outputs
        public float m;                    //robustness coeficient
        public float P, I, D;              //P I and D of controller
        public float up0, up1, up2;        //previous commands issued by real controller
        public float ykn;                  //output of real process
        public float es0, es1, es2, es3;   //output errors
        public float c0, c1, c2, c3, c4;    //identified coeficients b0/a0, b1/a0, a1/a0 for model
        public float cp0, cp1, cp2, cp3, cp4;//identified coeficients bp0/ap0, bp1/ap0, ap1/ap0 for real process model
        public float DeltaY, DY1, DY2, DY3; //difference between real system output and yk
        public float us0, us1, us2, us3;   //past simulated commands
        public float plotOutput;
        public float yknAverage;
        public float yk0, yk1, yk2, yk3;   //past outputs of real controller + model
        public float ek0, ek1, ek2, ek3;   // output errors of yk
        public float yknAvg;               //average of real system outputs
        public float SwitchingCriterion;
       
        public static int Size()
        {
            return Marshal.SizeOf<ShaderSystemBuffer>();
        }

        public override string ToString()
        {
            return $"m={m} dy = {DeltaY} yknavg = {yknAverage} j = {SwitchingCriterion}";
        }

        public float[] GetCoeficientsArray()
        {
            return new float[] { c0, c1, c2, c3, c4 };
        }
    }
}
