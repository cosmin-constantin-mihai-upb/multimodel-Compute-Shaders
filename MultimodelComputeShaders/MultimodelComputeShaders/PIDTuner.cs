using MLApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public class MatlabPidTuner : IPIDTuner
    {
        MLAppClass matlabComServer;
        public MatlabPidTuner(MLAppClass _matlabComServer)
        {
            matlabComServer = _matlabComServer;
        }

        public PID TunePid(Siso1Degree system)
        {
            return TunePid((ISisoProcess)system);
        }

        public PID TunePid(ISisoProcess system)
        {
            const string TransferFunctionVarName = "H";

            var systemDefinition = $"{TransferFunctionVarName}={system.SerializeAsMatlabTfCommand()}";
            var tfDef = matlabComServer.Execute(systemDefinition);

            var pidTune = matlabComServer.Execute($"x = pidtune({TransferFunctionVarName}, 'pi')");
            var P = MatlabParser.ParseMatlabAnsString(matlabComServer.Execute("x.Kp"));
            var I = MatlabParser.ParseMatlabAnsString(matlabComServer.Execute("x.Ki"));
            var D = MatlabParser.ParseMatlabAnsString(matlabComServer.Execute("x.Kd"));

            return new PID(P, I, D);
        }

        public static MatlabPidTuner Create()
        {
            return new MatlabPidTuner(Program.MatlabInstance);
        }
    }
}
