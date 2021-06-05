using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MultimodelComputeShaders
{
    public class PID
    {
        /// <summary>
        /// P
        /// </summary>
        public float KR
        {
            get;
            private set;
        }

        /// <summary>
        /// I
        /// </summary>
        public float TI
        {
            get;
            private set;
        }

        /// <summary>
        /// D
        /// </summary>
        public float TD
        {
            get;
            private set;
        }

        public PID(float P, float I, float D)
        {
            KR = P;
            TI = I;
            TD = D;
        }

        public float GetCommand(float[] u, float[] e, float T)
        {
            var q0 = KR + TI + TD;
            var q1 = -KR - (2 * TD);
            var q2 = TD;

            var uk = u[0] + q0 * e[0] + q1 * e[1] + q2 * e[2];
            return uk;
        }

        public override bool Equals(object obj)
        {
            return obj is PID pID &&
                   KR == pID.KR &&
                   TI == pID.TI &&
                   TD == pID.TD;
        }

        public override int GetHashCode()
        {
            int hashCode = -190810816;
            hashCode = hashCode * -1521134295 + KR.GetHashCode();
            hashCode = hashCode * -1521134295 + TI.GetHashCode();
            hashCode = hashCode * -1521134295 + TD.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"P={KR}, I={TI}, D={TD}";
        }
    }
}
