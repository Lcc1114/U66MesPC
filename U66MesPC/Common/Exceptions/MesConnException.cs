using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Common.Exceptions
{
    public class MesConnException:Exception
    {
        public MesConnException(string message) : base(message)
        {
        }
        public MesConnException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
