using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultimodelComputeShaders
{
    public static class Extensions
    {
        public static string PrintAsMatlabArrayDefinition(this IEnumerable<float> data, string varName)
        {
            StringBuilder builder = new StringBuilder($"{varName} = [");
            builder.Append(string.Join(",", data.Select(x => x.ToString())));
            builder.Append("];");
            return builder.ToString();
        }

        public static string PrintAsMatlabArrayDefinition(this IEnumerable<IEnumerable<float>> data, string varName)
        {
            StringBuilder builder = new StringBuilder($"{varName} = [");
            foreach (var r in data)
            {
                builder.Append(string.Join(" ", r.Select(x => x.ToString())));
                builder.Append(";");
            }
            builder.Append("];");
            return builder.ToString();
        }

        public static List<float> MeanCenter(this List<float> data)
        {
            var mean = data.Sum(x => x) / data.Count;
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = data[i] - mean;
            }

            return data;
        }

        public static List<List<float>> MeanCenter(this List<List<float>> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = data[i].MeanCenter();
            }

            return data;
        }
    }
}
