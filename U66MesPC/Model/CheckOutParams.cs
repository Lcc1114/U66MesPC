using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using U66MesPC.Common;
using U66MesPC.Common.Station;

namespace U66MesPC.Model
{
    public class DCInfo
    {
        public DCInfo() { }
        public DCInfo(string item, string value, string up, string down, string result)
        {
            Item = item;
            Value = value;
            Up = up;
            Down = down;
            Result = result;
        }

        //测试项目
        public string Item { get; set; }
        //Value值
        public string Value { get; set; }
        //上限值
        public string Up { get; set; }
        //下限值
        public string Down { get; set; }
        //PASS/FAIL
        public string Result { get; set; }

    }
    public class CompInfo
    {
        public CompInfo() { }
        public CompInfo(string compID, int qty)
        {
            CompID = compID;
            Qty = qty;
            //ToolingSN = "0";
        }
        //public CompInfo( string toolingSN)
        //{
        //    ToolingSN = toolingSN;
        //}

        //材料Barcode
        public string CompID { get; set; } = "0";
        //新增
        //public string ToolingSN { get; set; } = "0";
        //使用数量
        public int Qty { get; set; } = 0;
    }
    public class SNInfo
    {
        public SNInfo() { }
        public SNInfo(string sN, string result, List<DCInfo> dC_Info, List<CompInfo> compList)
        {
            SN = sN;
            Result = result;
            DC_Info = dC_Info;
            CompList = compList;
            Slot = "0";
        }
        public SNInfo(string sN, string result, string slot, List<DCInfo> dC_Info, List<CompInfo> compList)
        {
            SN = sN;
            Result = result;
            DC_Info = dC_Info;
            CompList = compList;
            Slot = slot;
        }
        public void CheckSlot()
        {
            if (string.IsNullOrEmpty(Slot))
            {
                Slot = "0";
            }
        }

        public string SN { get; set; }
        public string Result { get; set; }

        public List<DCInfo> DC_Info { get; set; }
        public List<CompInfo> CompList { get; set; }
        public string Slot { get; set; } = "0";
    }

    public class CheckOutRequest : BaseRequestParams
    {
        public static string _eventID = "SN_CheckOut";
        //public string SN { get; set; }
        public string CarrierID { get; set; }
        /// <summary>
        /// 模号
        /// </summary>
        public string Mold { get; set; }
        //综合使用数量
        public int Qty { get; set; }

        private string _version;// = ConfigurationManager.AppSettings["LineID"] + "_" + Assembly.GetExecutingAssembly().GetName().Name;
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;// ConfigurationManager.AppSettings["LineID"] + "_" + Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        public List<SNInfo> SNInfo { get; set; }
        public CheckOutRequest()
        {
        }

        public CheckOutRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, /*string sN,*/ string carrierID, string mold, int qty, List<SNInfo> snInfo) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            //SN = sN;
            CarrierID = carrierID;
            Mold = mold;
            Qty = qty;
            snInfo = snInfo.Where(x => x.Result.ToUpper() == "PASS" || x.Result.ToUpper() == "ALARM").ToList();
            snInfo.ForEach(s => s.CheckSlot());
            SNInfo = snInfo;
        }
        public CheckOutRequest(SysConfigs config, /*string sN,*/ string carrierID, string mold, int qty, List<SNInfo> snInfo, string info = "", string format = "#") : base(config, _eventID, null, info, format)
        {
            //SN = sN;
            CarrierID = carrierID;
            Mold = mold;
            Qty = qty;
            snInfo = snInfo.Where(x => x.Result.ToUpper() == "PASS" || x.Result.ToUpper() == "ALARM").ToList();
            snInfo.ForEach(s => s.CheckSlot());
            SNInfo = snInfo;
            string date = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyyMMdd");
            Version = ConfigurationManager.AppSettings["LineID"] + "_" + config.StationID + "_A_" + "20240820";
        }
        public override string GetVaidData()
        {
            string strCompList = SNInfo?.Aggregate("", (s, b) => s + $"{b.CompList?.Aggregate("", (s1, b1) => s1 + $"({b1.CompID},{b1.Qty})")}");
            string strDCInfoList = SNInfo?.Aggregate("", (s, b) => s + $"{b.DC_Info?.Aggregate("", (s1, b1) => s1 + $"({b1.Item},{b1.Value})")}");
            return $"CarrierID:{CarrierID} Mold:{Mold} Qty:{Qty} SNInfo:{strCompList},{strDCInfoList}";

        }
    }
    public class CheckOutResponse : BaseResponseParams
    {
        //PASS：继续执行；STOP：停机
        public string Need_Work { get; set; }
        public List<SNInfoResponse> SN_Info { get; set; }
        public CheckOutResponse() { }

        public CheckOutResponse(string eventID, string result, string msg, string needWork, List<SNInfoResponse> snInfo) : base(eventID, result, msg)
        {
            Need_Work = needWork;
            SN_Info = snInfo;
        }
        public override string GetRetData()
        {
            return base.GetRetData() + $"Need_Work:{Need_Work} SN_Info:{SN_Info?.Aggregate("(SN,SNResult,MSG_ID)", (s, b) => s + $"({b.SN},{b.SNResult},{b.MSG_ID})")}";
        }
    }
    public class SNInfoResponse
    {
        public SNInfoResponse()
        { }
        public string SN { get; set; }
        public string SNResult { get; set; }
        public string MSG_ID { get; set; }
        public SNInfoResponse(string sn, string snResult, string msgID)
        {
            SN = sn;
            SNResult = snResult;
            MSG_ID = msgID;
        }
    }

}