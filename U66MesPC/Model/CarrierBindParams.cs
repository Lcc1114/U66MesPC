using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class CarrierBindRequest:BaseRequestParams
    {
        public static string _eventID = "SN_CarrierBind";
        public string SN { get; set; }
        public string CarrierID { get; set; }
        //Bind/UNBIND 绑定或者解绑
        public string BindType { get; set; }
        //载具穴位
        public string ACPoint { get; set; }
        public CarrierBindRequest()
        {
        }

        public CarrierBindRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN, string carrierID,string bindType,string acPoint) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            SN = sN;
            CarrierID = carrierID;
            BindType = bindType;
            ACPoint = acPoint;
        }
        public CarrierBindRequest( SysConfigs config, string sN, string carrierID, string bindType, string acPoint) : base(config, _eventID)
        {
            SN = sN;
            CarrierID = carrierID;
            BindType = bindType;
            ACPoint = acPoint;
        }
        public override string GetVaidData()
        {
            return $"SN:{SN} CarrierID:{CarrierID} BindType:{BindType} ACPoint:{ACPoint}";
        }
    }
    public class CarrierBindResponse : BaseResponseParams
    {
        public CarrierBindResponse() { }

        public CarrierBindResponse(string eventID, string result, string msg) : base(eventID, result, msg)
        {
        }
        public override string GetRetData()
        {
            return base.GetRetData();
        }
    }
}