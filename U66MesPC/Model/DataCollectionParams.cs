using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class DataCollectionRequest:BaseRequestParams
    {
        public static string _eventID = "Data_Collection";
        public string SN { get; set; }
        public string CarrierID { get; set; }
        public List<DC_Info> DC_Info { get; set; }
        public List<CompInfo> CompList { get; set; }
        public DataCollectionRequest()
        {
        }

        public DataCollectionRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN, string carrierID,List<DC_Info> dcInfo,List<CompInfo> compInfos) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            SN = sN;
            CarrierID = carrierID;
            DC_Info = dcInfo;
            CompList = compInfos;
        }
        public DataCollectionRequest(SysConfigs config, string sN, string carrierID, List<DC_Info> dcInfo, List<CompInfo> compInfos) : base(config, _eventID)
        {
            SN = sN;
            CarrierID = carrierID;
            DC_Info = dcInfo;
            CompList = compInfos;
        }
        public override string GetVaidData()
        {
            string strCompList = CompList.Aggregate("(CompID,Qty)", (s1, b1) => s1 + $"({b1.CompID},{b1.Qty})");
            string strDCInfoList = DC_Info.Aggregate("(Item,Value)", (s1, b1) => s1 + $"({b1.Item},{b1.Value})");
            return $"SN:{SN} CarrierID:{CarrierID} DC_Info:{strDCInfoList} CompList:{strCompList}";
        }
    }
    public class DataCollectionResponse : BaseResponseParams
    {
        public DataCollectionResponse() { }

        public DataCollectionResponse(string eventID, string result, string msg) : base(eventID, result, msg)
        {
        }
        public override string GetRetData()
        {
            return base.GetRetData();
        }
    }
}