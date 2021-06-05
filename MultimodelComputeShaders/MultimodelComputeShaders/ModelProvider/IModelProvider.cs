namespace MultimodelComputeShaders.ModelProvider
{
    public interface IModelProvider
    {
        ShaderSystemBuffer ModelControllerIdentification();

        int NoModels { get; }
    }

    //this will generate random models. Not very good for corectness testing, but good for performance testing
    public class RandomModelProvider : IModelProvider
    {
        public int NoModels => 10000;

        public ShaderSystemBuffer ModelControllerIdentification()
        {
            var system = (new SystemIdentification()).Identify();

            return ModelIdenCommon.GetDataForKnowledeBase(system);
        }
    }

    //this generates a predetermined set of models. this was used to generate the figures in the paper
    public class DeterministicModelProvider : IModelProvider
    {
        int callCount = -1;
        const int maxModels = 3;

        public int NoModels => maxModels;


        ISisoProcess[] models = new ISisoProcess[maxModels];

        /// <summary>
        /// Adjust the values here to change the models generated
        /// </summary>
        public DeterministicModelProvider()
        {
            models[0] = new Siso1Degree(1.2f, 2);
            models[1] = new Siso2Degree(1, 1, 1);
            models[2] = new Siso1Degree(1, 10);
        }

        public ShaderSystemBuffer ModelControllerIdentification()
        {
            callCount++;
            return ModelIdenCommon.GetDataForKnowledeBase(models[callCount]);
        }
    }

    public static class ModelIdenCommon
    {
        public static ShaderSystemBuffer GetDataForKnowledeBase(ISisoProcess system)
        {
            var pidFactory = MatlabPidTuner.Create();
            PID pid;

            pid = pidFactory.TunePid(system);

            var dataBufferi = new ShaderSystemBuffer();
          
            dataBufferi.K1 = (float)(pid.KR + pid.TI + pid.TD);
            dataBufferi.K2 = (float)(-pid.KR - (2 * pid.TD));
            dataBufferi.K3 = (float)(pid.TD);
           
            dataBufferi.P = (float)pid.KR;
            dataBufferi.I = (float)pid.TI;
            dataBufferi.D = (float)pid.TD;

            dataBufferi.c0 = (float)system.C[0];
            dataBufferi.c1 = (float)system.C[1];
            dataBufferi.c2 = (float)system.C[2];
            dataBufferi.c3 = (float)system.C[3];
            dataBufferi.c4 = (float)system.C[4];

            return dataBufferi;
        }
    }
}
