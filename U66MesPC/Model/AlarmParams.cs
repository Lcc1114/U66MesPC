using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class AlarmRequest
    {
        public static string _eventID = "Alarm";
        public AlarmRequest()
        {
        }
        public AlarmRequest(SysConfigs config,string alarmID, string sendTime = null, string resetTime = null)
        {
            EventID = _eventID;
            Line = config.Line;
            StationID = config.StationID;
            MachineID = config.MachineID;
            OPID = config.OPID;
            AlarmID = alarmID;
            var time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            SendTime = sendTime ?? time;
            ResetTime = resetTime ?? time;
            //ResetTime = resetTime ?? "";
        }
        public AlarmRequest(string line, string stationID, string machineID, string oPID,string alarmID, string sendTime=null, string resetTime=null)
        {
            EventID = _eventID;
            Line = line;
            StationID = stationID;
            MachineID = machineID;
            OPID = oPID;
            AlarmID = alarmID;
            SendTime = sendTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            ResetTime = resetTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }
        public string EventID { get; set; }
        public string Line { get; set; }
        public string StationID { get; set; }
        public string MachineID { get; set; }
        public string OPID { get; set; }
        public string AlarmID { get; set; }
        public string SendTime { get; set; }
        public string ResetTime { get; set; }
        public string GetValidData()
        {
            return $"Alarm:{AlarmID};";
        }
    }
    public class AlarmResponse : BaseResponseParams
    {
        public string AlarmID { get; set; }
        public AlarmResponse() { }

        public AlarmResponse(string eventID, string result, string msg, string alarmID) : base(eventID, result, msg)
        {
            AlarmID = alarmID;
        }
        public override string GetRetData()
        {
            return base.GetRetData() + $"Alarm:{AlarmID}";
        }
    }
}