using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public class SystemIdentification : ISystemIdentification
    {
        static Random r = new Random();
        public Siso1Degree Identify()
        {
            var Tf = r.Next(3, 6) * Constants.SamplingTime;
            double kfRoot = 0;
            float Kf = 1;
            do
            {
                kfRoot = r.NextDouble();
                Kf = (float)Math.Round(kfRoot * 10, 2);
            } while (Kf == 0);
            return new Siso1Degree(Kf, Tf);
        }

        public Task<Siso1Degree> IdentifyAsync()
        {
            return Task.Run(() => { return Identify(); });
        }
    }    
}
