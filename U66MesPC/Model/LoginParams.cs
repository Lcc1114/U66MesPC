using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class LoginRequest:BaseRequestParams
    {
        public static string _eventID = "Login";
        public string STRUser { get; set; }
        public string STRPwd { get; set; }
        public LoginRequest() { }
        public LoginRequest(SysConfigs config, string strUser,string strPwd):base(config,_eventID)
        {
            STRUser = strUser;
            STRPwd = strPwd;
        }
        public override string GetVaidData()
        {
            LogsUtil.Instance.WriteInfo($"StrUser:{STRUser} STRPwd:{STRPwd}");
            return $"StrUser:{STRUser} STRPwd:XXXX";
        }
    }
    public class LoginResponse:BaseResponseParams
    {
        public string StatusCode { get; set; }
        public List<DataInfo> Data { get; set; }
        public string ErrorMessage { get; set; }
        public string IsSuccess { get; set; }
        public LoginResponse() { }
        public LoginResponse(string statusCode,List<DataInfo> data, string errorMsg, string isSuccess)
        {
            StatusCode = statusCode;
            Data = data;
            ErrorMessage = errorMsg;
            IsSuccess = isSuccess;
        }
        public override string GetRetData()
        {
            return base.GetRetData()+$"StatusCode:{StatusCode}; Data:{Data.Aggregate("",(s,b)=>s+$"{b.ToString()};ErrorMessage:{ErrorMessage};IsSuccess:{IsSuccess};")}";
        }
    }
    public class DataInfo
    {
        public string bRes { get; set; }
        public string Ticket { get; set; }
        public string rightList { get; set; }
        
        public DataInfo() { }
        public DataInfo(string bRes,string ticket,string rightList)
        {
            this.bRes = bRes;
            Ticket = ticket;
            this.rightList = rightList;
        }
        public override string ToString()
        {
            return $"bRes:{bRes};Ticket:{Ticket};RightList:{rightList};";
        }
    }
}
