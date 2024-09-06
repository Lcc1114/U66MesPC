using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class FeedingCheckRequest : BaseRequestParams
    {
        public static string _eventID = "SN_FeedingCheck";
        public string SN { get; set; }
        public List<string> SNALL { get; set; }

        public FeedingCheckRequest()
        {
        }

        public FeedingCheckRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            SN = sN;
        }
        public FeedingCheckRequest(SysConfigs config, string sN) : base(config, _eventID)
        {
            SN = sN;
        }
        public FeedingCheckRequest(SysConfigs config, List<string> sN) : base(config, _eventID)
        {
            SNALL = sN;
        }
        public override string GetVaidData()
        {
            return $"SN:{SN}";
        }
    }
    public class FeedingCheckResponse : BaseResponseParams
    {
        public string SN { get; set; }
        public FeedingCheckResponse() { }

        public FeedingCheckResponse(string eventID, string result, string msg, string sN) : base(eventID, result, msg)
        {
            SN = sN;        
        }
        public override string GetRetData()
        {
            return base.GetRetData()+$"SN:{SN}";
        }
    }
}