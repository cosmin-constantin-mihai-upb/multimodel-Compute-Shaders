using MultimodelComputeShaders.ModelProvider;
using MultimodelComputeShaders.Pipelines;
using MLApp;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MultimodelComputeShaders
{
    class Program
    {
        public static MLAppClass MatlabInstance
        {
            get;
            private set;
        }

        static void Main(string[] args)
        {
            MatlabInstance = new MLAppClass();
            MatlabInstance.Visible = 1;
            GameWindow program = new UniformSamplingTimePipeline();
            program.Run();

            Console.ReadLine();
        }
    }
}
