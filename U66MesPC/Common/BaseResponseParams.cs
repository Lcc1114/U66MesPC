using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Common
{
    public class BaseResponseParams
    {
        public BaseResponseParams()
        {
        }
        public BaseResponseParams(string eventID, string result, string msg)
        {
            EventID = eventID;
            Result = result;
            Msg = msg;
        }
        public string EventID { get; set; }
        public string Result { get; set; }
        public string Msg { get; set; }
        public virtual string  GetRetData()
        {
            return $"Result:{Result}; Msg:{Msg};";
        }
    }
}
