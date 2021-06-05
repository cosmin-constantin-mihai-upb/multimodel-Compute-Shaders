using MultimodelComputeShaders.ModelProvider;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MultimodelComputeShaders.Pipelines
{
    public class UniformSamplingTimePipeline : GameWindow
    {

        bool useNoisy = false;
        ISisoProcess realPlant;
        ISisoProcess nominalPlant;
        const int numberOfAdditionalSystems = 100;
        Stack<int> modelSwitchQueue = new Stack<int>(new int[] { 1, 2, 1, 3, 1, 2, 3, 2, 1, 2, 3, 2, 1 });
        IModelProvider modelProvider = new RandomModelProvider();

        PID realController;
        IPIDTuner controllerFactory = MatlabPidTuner.Create();

        float[] e = new float[4];
        float reference = 10;
        uint ssbo = 0;

        List<ShaderSystemBuffer> knowledgeBase = new List<ShaderSystemBuffer>(100000);
        private int gComputeProgram;
        long frameCount = 0;
        bool fastSwitch = false;
        int systemSettlingTimeGracePeriod = 0;

        const int performanceDragradationGracePeriodDefault = 4;

        int performanceDragradationGracePeriod = performanceDragradationGracePeriodDefault;

        List<List<float>> matlabOutputArrays = new List<List<float>>();
        Queue<double> uHistory = new Queue<double>();
        Queue<double> yHistory = new Queue<double>();

        protected override void OnLoad(EventArgs ex)
        {
            base.OnLoad(ex);

            gComputeProgram = GL.CreateProgram();
            var shaderHandle = GL.CreateShader(ShaderType.ComputeShader);
            var shaderText = ReadShaderCode();

            GL.ShaderSource(shaderHandle, shaderText);
            GL.CompileShader(shaderHandle);
            int rvalue;

            GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out rvalue);
            var log = GL.GetShaderInfoLog(shaderHandle);
            if (!string.IsNullOrWhiteSpace(log))
            {
                Debugger.Break();
            }
            GL.AttachShader(gComputeProgram, shaderHandle);

            GL.LinkProgram(gComputeProgram);
            GL.GetProgram(gComputeProgram, GetProgramParameterName.LinkStatus, out rvalue);

            GL.UseProgram(gComputeProgram);

            /*define the simulated system*/
            InitRealPlant();
            matlabOutputArrays.Add(new List<float>());
            for (int i = 0; i < numberOfAdditionalSystems; i++)
            {
                ShaderSystemBuffer dataBufferi = modelProvider.ModelControllerIdentification();
                knowledgeBase.Add(dataBufferi);
                matlabOutputArrays.Add(new List<float>());
            }


            GL.GenBuffers(1, out ssbo);
        }

        /// <summary>
        /// probes the real system, and performs supervisory activities, such as detecting
        /// performance degradation, switching models, etc
        /// </summary>
        private void ProbeRealSystem()
        {
            var uk = realController.GetCommand(realPlant.U, e, Constants.SamplingTime);

            var yk = realPlant.GetOutput2(uk, useNoisy);
            var ykn = nominalPlant.GetOutput2(uk);

            var ykavg = realPlant.GetOutputAverage();
            var yknavg = nominalPlant.GetOutputAverage();

            matlabOutputArrays[0].Add(RoundResult(yk));
            var m = (float)Math.Abs((ykavg / (1 + ykavg)) - (yknavg / (1 + yknavg)));
            systemSettlingTimeGracePeriod = systemSettlingTimeGracePeriod == 0 ? 0 : systemSettlingTimeGracePeriod - 1;

            ProcessKnowledgeBase(ykavg);

            if (m > Constants.RobustnessMargin && systemSettlingTimeGracePeriod == 0)
            {
                performanceDragradationGracePeriod = performanceDragradationGracePeriod == 0 ? 0 : performanceDragradationGracePeriod - 1;
                Debug.WriteLine($"performanceDragradationGracePeriod = {performanceDragradationGracePeriod}");
                if (performanceDragradationGracePeriod == 0)
                {
                    Debug.WriteLine($"m = {m}");

                    int smallestIndex = 0;
                    double smallestE = knowledgeBase[0].SwitchingCriterion;
                    for (int i = 0; i < knowledgeBase.Count; i++)
                    {
                        if (knowledgeBase[i].SwitchingCriterion < smallestE)
                        {
                            smallestE = knowledgeBase[i].SwitchingCriterion;
                            smallestIndex = i;
                        }
                    }

                    performanceDragradationGracePeriod = performanceDragradationGracePeriodDefault;
                    Debug.WriteLine($"Model {smallestIndex}");
                    var cacheModel = knowledgeBase[smallestIndex];
                    nominalPlant = ISisoFactory.CreateProcess(cacheModel.GetCoeficientsArray());

                    nominalPlant.Y[3] = cacheModel.y3;
                    nominalPlant.Y[2] = cacheModel.y2;
                    nominalPlant.Y[1] = cacheModel.y1;
                    nominalPlant.Y[0] = cacheModel.y0;

                    realController = new PID(cacheModel.P, cacheModel.I, cacheModel.D);
                    systemSettlingTimeGracePeriod = 0;
                    //fast switch controller context
                    if (fastSwitch)
                    {
                        nominalPlant.Y[3] = cacheModel.y3;
                        nominalPlant.Y[2] = cacheModel.y2;
                        nominalPlant.Y[1] = cacheModel.y1;
                        nominalPlant.Y[0] = cacheModel.y0;

                        e[3] = cacheModel.es3;
                        e[2] = cacheModel.es2;
                        e[1] = cacheModel.es1;
                        e[0] = cacheModel.es0;
                    }
                }
            }
            else
            {
                performanceDragradationGracePeriod = performanceDragradationGracePeriodDefault;
            }

            e[3] = e[2];
            e[2] = e[1];
            e[1] = e[0];
            e[0] = reference - yk;

            yHistory.Enqueue(yk);
            uHistory.Enqueue(uk);

            if (Single.IsNaN(e[0]) || Single.IsInfinity(e[0]))
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// On sampling period
        /// </summary>
        /// <param name="e"></param>
        protected override void OnRenderFrame(FrameEventArgs ex)
        {
            base.OnRenderFrame(ex);
            ProbeRealSystem();

            frameCount++;
            if (frameCount % 600 == 0)
            {
                for (int i = 0; i < matlabOutputArrays.Count; i++)
                {
                    var md = matlabOutputArrays[i];
                    Debug.WriteLine(md.PrintAsMatlabArrayDefinition($"A{i}"));
                }

                Debug.WriteLine("Plot4Arrays(A0, A1, A2, A3)");
                Random r = new Random();
                if (modelSwitchQueue.Count == 0) this.Close(); // simulation is over, close the thing
                var s = modelSwitchQueue.Pop();
                Debug.WriteLine($"Changing system model to {s}");

                var realPlant2 = ISisoFactory.CreateProcess(knowledgeBase[s].GetCoeficientsArray());
                realPlant2.Y[0] = realPlant.Y[0];
                realPlant2.Y[1] = realPlant.Y[1];
                realPlant2.Y[2] = realPlant.Y[2];
                realPlant2.Y[3] = realPlant.Y[3];
                realPlant = realPlant2;
            }
        }

        /// <summary>
        /// Advances the knowledge base simulation by 1 sampling time, using inputs from the real system
        /// Also runs the compute shader, reads the results.
        /// </summary>
        /// <param name="ykavg"></param>
        private void ProcessKnowledgeBase(float ykavg)
        {
            ShaderSystemBuffer[] dataBuffer = knowledgeBase.ToArray();
            var structSize = ShaderSystemBuffer.Size();
            for (int i = 0; i < dataBuffer.Length; i++)
            {
                //real plan is ahead 1 sample periods
                dataBuffer[i].up0 = (float)realPlant.U[1];
                dataBuffer[i].up1 = (float)realPlant.U[2];
                dataBuffer[i].up2 = (float)realPlant.U[3];

                dataBuffer[i].R = (float)reference;
                dataBuffer[i].ykn = (float)realPlant.Y[0];
                dataBuffer[i].yk1 = (float)realPlant.Y[2];

                dataBuffer[i].cp0 = nominalPlant.C[0];
                dataBuffer[i].cp1 = nominalPlant.C[1];
                dataBuffer[i].cp2 = nominalPlant.C[2];
                dataBuffer[i].cp3 = nominalPlant.C[3];
                dataBuffer[i].cp4 = nominalPlant.C[4];

                dataBuffer[i].e0 = e[0];
                dataBuffer[i].e1 = e[1];
                dataBuffer[i].e2 = e[2];

                dataBuffer[i].yknAverage = ykavg;
            }


            ssbo = CreateInputBuffer(gComputeProgram, dataBuffer, structSize, ssbo);
            
            Stopwatch performanceWatchDog = new Stopwatch();
            
            performanceWatchDog.Start();
            GL.DispatchCompute(1, 1, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            Debug.WriteLine($"Compute shader executed in {performanceWatchDog.ElapsedMilliseconds} ms");

            var outPoint = GL.MapNamedBuffer(ssbo, BufferAccess.ReadWrite);

            var cachedData = Helpers.MarshalUnmananagedArray2Struct<ShaderSystemBuffer>(outPoint, dataBuffer.Length);
            knowledgeBase.Clear();
            knowledgeBase.AddRange(cachedData);
            performanceWatchDog.Stop();

            Debug.WriteLine($"Compute shader executed and memory copied in {performanceWatchDog.ElapsedMilliseconds} ms");
            
            for (int i = 1; i < numberOfAdditionalSystems + 1; i++)
            {
                matlabOutputArrays[i].Add(RoundResult(cachedData[i].plotOutput));
            }

            GL.UnmapNamedBuffer(ssbo);

        }

        /// <summary>
        /// rounds float numbers
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private float RoundResult(float y)
        {
            return (float)Math.Round(y, 5, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Initializes the real plant and its nominal model. Also adds it to the knowledge base
        /// </summary>
        private void InitRealPlant()
        {
            realPlant = new Siso1Degree(2, 1.2f);
            nominalPlant = new Siso1Degree(2, 1.2f);
            realController = controllerFactory.TunePid(nominalPlant);
            knowledgeBase.Add(ModelIdenCommon.GetDataForKnowledeBase(realPlant));
            e[0] = 0;
        }

        /// <summary>
        /// Creates an input / output buffer to send data to and from the compute shader
        /// </summary>
        /// <param name="gComputeProgram"></param>
        /// <param name="dataBuffer"></param>
        /// <param name="structSize"></param>
        /// <param name="ssbo"></param>
        /// <returns></returns>
        private static uint CreateInputBuffer(int gComputeProgram, ShaderSystemBuffer[] dataBuffer, int structSize, uint ssbo)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

            GL.BufferData(BufferTarget.ShaderStorageBuffer, structSize * dataBuffer.Length, dataBuffer, BufferUsageHint.DynamicRead);
            var mask = BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit;

            var points = GL.MapBufferRange(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, structSize * dataBuffer.Length, mask);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ssbo);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ssbo);
            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

            var blockIndex = GL.GetProgramResourceIndex(gComputeProgram, ProgramInterface.ShaderStorageBlock, "IObuffer");
            GL.ShaderStorageBlockBinding(gComputeProgram, blockIndex, 3);

            return ssbo;
        }

        /// <summary>
        /// reads the embeded glsl shader code
        /// </summary>
        /// <returns></returns>
        private string ReadShaderCode()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MultimodelComputeShaders.ComputeShaderCode.glsl"))
            {
                using (TextReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
