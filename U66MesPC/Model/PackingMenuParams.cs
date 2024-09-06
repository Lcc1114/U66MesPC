using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class PackingMenuRequest:BaseRequestParams
    {
        public static string _eventID = "Packing_Menu";
        public string Menu { get; set; }
        public string SN { get; set; }
        public string Password { get; set; }
        public PackingMenuRequest(SysConfigs config,string menu,string sn,string pwd):base(config,_eventID)
        {
            Menu = menu;
            SN = sn;
            Password = pwd;
        }
        public override string GetVaidData()
        {
            return $"Menu:{Menu};SN:{SN};Password:{Password};";
        }
    }
    public class PackingMenuResponse : BaseResponseParams
    {
        public string SN { get; set; }
        public PackingMenuResponse() { }

        public PackingMenuResponse(string eventID, string result, string msg,string sn) : base(eventID, result, msg)
        {
            SN = sn;
        }
        public override string GetRetData()
        {
            return base.GetRetData()+$"SN:{SN}";
        }
    }
}
