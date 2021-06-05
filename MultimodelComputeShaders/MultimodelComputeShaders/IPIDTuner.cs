using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public interface IPIDTuner
    {
        PID TunePid(Siso1Degree system);
        
        PID TunePid(ISisoProcess system);
    }
}
