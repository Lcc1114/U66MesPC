using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class CarrierCheckRequest:BaseRequestParams
    {
        public static string _eventID = "Carrier_Check";
        public string CarrierID { get; set; }
        public CarrierCheckRequest()
        {
        }

        public CarrierCheckRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string carrierID) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            CarrierID = carrierID;
        }
        public CarrierCheckRequest(SysConfigs config, string carrierID) : base(config, _eventID)
        {
            CarrierID = carrierID;
        }
        public override string GetVaidData()
        {
            return $"CarrierID:{CarrierID};";
        }
    }
    public class CarrierCheckResponse : BaseResponseParams
    {
        public string CarrierID { get; set; }
        public List<DC_Info> DC_Info { get; set; }
        public CarrierCheckResponse() { }

        public CarrierCheckResponse(string eventID, string result, string msg, string carrierID,List<DC_Info> dcInfo) : base(eventID, result, msg)
        {
            CarrierID = carrierID;
            DC_Info = dcInfo;
        }
        public override string GetRetData()
        {
            return base.GetRetData()+$"CarrierID:{CarrierID}; DC_Info:{DC_Info?.Aggregate("",(s,b)=>s+b.ToString())};";
        }
    }
    public class DC_Info
    { 
        public string Item { get; set; }
        public string Value { get; set; }
        public DC_Info() { }

        public DC_Info(string item, string value)
        {
            Item = item;
            Value = value;
        }
        public override string ToString()
        {
            return $"({Item},{Value})";
        }
    }

}