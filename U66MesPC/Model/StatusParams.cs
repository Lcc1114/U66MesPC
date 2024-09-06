using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class StatusRequest : BaseRequestParams
    {
        public static string _eventID = "Status";
        public string Status { get; set; }

        public StatusRequest()
        {
        }
        public StatusRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string status) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            Status = status;
        }
        public StatusRequest(SysConfigs config, string status) : base(config, _eventID)
        {
            Status = status;
        }
        public override string GetVaidData()
        {
            return $"Status:{Status}";
        }
    }
    public class StatusResponse : BaseResponseParams
    {
        public string Status { get; set; }
        //public string Need_Work { get; set; }
        public StatusResponse() { }

        public StatusResponse(string eventID, string result, string msg, string status) : base(eventID, result, msg)
        {
            Status = status;
        }
        public override string GetRetData()
        {
            return base.GetRetData()+$"Status:{Status}";
        }
    }
}