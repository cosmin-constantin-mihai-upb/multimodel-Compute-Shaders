using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public static class MatlabParser
    {
        public static float ParseMatlabAnsString(string str)
        {
            str = str.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("ans=", "");
            return float.Parse(str);
        }
    }
}
