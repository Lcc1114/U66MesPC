using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Common
{
    public class BaseRequestParams
    {
        public BaseRequestParams()
        {
        }

        public BaseRequestParams(string eventID, string line, string stationID, string machineID, string oPID, string token, string fixSN, string sendTime = null)
        {
            EventID = eventID;
            Line = line;
            StationID = stationID;
            MachineID = machineID;
            OPID = oPID;
            Token = token;
            FixSN = fixSN;
            SendTime = sendTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }
        public BaseRequestParams(SysConfigs config, string eventID, string sendTime = null, string info = "", string format = "#")
        {
            EventID = eventID;
            Line = config.Line;
            StationID = config.StationID;
            MachineID = config.MachineID;
            if (!string.IsNullOrEmpty(info))
                MachineID = $"{MachineID}{format}{info}"; //CapInput MachineID#流道ID Binning Machine#下料位
            OPID = config.OPID;
            Token = config.Token;
            FixSN = config.FixSN;
            SendTime = sendTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            // SendTime = sendTime ?? DateTime.Now.ToString("G");
        }
        public string EventID { get; set; }
        public string Line { get; set; }
        public string StationID { get; set; }
        public string MachineID { get; set; }
        public string OPID { get; set; }
        public string Token { get; set; }
        public string FixSN { get; set; }
        public string SendTime { get; set; }
        public virtual string GetVaidData() { return ""; }
    }
}
