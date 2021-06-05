using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public interface ISystemIdentification
    {
        Task<Siso1Degree> IdentifyAsync();
        Siso1Degree Identify();
    }
}
