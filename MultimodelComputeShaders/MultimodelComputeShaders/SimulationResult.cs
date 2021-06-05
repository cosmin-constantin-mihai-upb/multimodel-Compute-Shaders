using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public class SimulationResult
    {
        public double AverageComputeShaderExecuteTime { get; set; }
    }

    public class ResultSet
    {
        public List<string> SampledValues = new List<string>();

        public string MatlabCommand { get; } = "Plot4Arrays(A0, A1, A2, A3)";
    }
}
