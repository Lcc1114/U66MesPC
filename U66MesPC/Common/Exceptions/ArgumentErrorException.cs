using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Common.Exceptions
{
    public class ArgumentErrorException : Exception 
    {
        public string EventID { get; set; }
        public ArgumentErrorException(string message,string eventID="SN_Checkout") : base(message)
        {
            EventID = eventID;
        }
        public ArgumentErrorException(string message, string eventID, Exception innerException) : base(message, innerException)
        {
            EventID = eventID;
        }
    }
}
