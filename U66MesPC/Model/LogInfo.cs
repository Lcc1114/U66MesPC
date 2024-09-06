using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;

namespace U66MesPC.Model
{
    public enum EventIO
    {
        [Description("Send")]
        发送,
        [Description("Recv")]
        接收,
        [Description("Write")]
        写入,
        [Description("Read")]
        读取,
        [Description("Error")]
        错误,
        [Description("Warn")]
        Warn,
        [Description("Info")]
        信息
    }
    public class LogInfo
    {
        public LogInfo() { }
        public LogInfo(string stationID, string eventName, EventIO eventIO, string eventVal, object obj = null, string dateTime = null)
        {
            PrintDateTime = dateTime ?? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            if (InfoCHS.Instance().DicStationID.ContainsKey(stationID))
                StationID = InfoCHS.Instance().DicStationID[stationID];
            else
                StationID = stationID;
            if (InfoCHS.Instance().DicEventName.ContainsKey(eventName))
                EventName = InfoCHS.Instance().DicEventName[eventName];
            else
                EventName = eventName;
            EventIO = eventIO;
            EventVal = eventVal;
            if (obj != null)
                JsonStr = new JavaScriptSerializer().Serialize(obj);
            InitForeground();
        }
        private void InitForeground()
        {
            if (EventVal.ToLower().Contains("fail") || EventVal.ToLower().Contains("失败") || EventVal.ToLower().Contains("警告")
                || EventIO == EventIO.Warn || EventVal.ToLower().Contains("false") || EventVal.ToLower().Contains("NG"))
                FgColor = new SolidColorBrush(Colors.Blue);
            else if (EventVal.ToLower().Contains("error") || EventVal.ToLower().Contains("错误") || EventIO == EventIO.错误)
                FgColor = new SolidColorBrush(Colors.Red);
            else
                FgColor = new SolidColorBrush(Colors.Black);
        }
        public SolidColorBrush FgColor { get; set; }
        public string PrintDateTime { get; set; }
        public string StationID { get; set; }
        public string EventName { get; set; }
        public EventIO EventIO { get; set; }
        public string EventVal { get; set; }
        public string JsonStr { get; set; }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(JsonStr))
            {
                //return $"日志记录:工站：{StationID}；事件名称：{EventName}；事件IO：{EventIO}；内容：{EventVal}；时间：{PrintDateTime}；";
                return $"工站：{StationID}；事件名称：{EventName}；内容：{EventVal}";
            }
            else
            {
                return $"{EventIO}：{JsonStr}";
            }
        }
    }

    public class InfoCHS
    {
        public Dictionary<string, string> DicStationID = new Dictionary<string, string>();
        public Dictionary<string, string> DicEventName = new Dictionary<string, string>();


        private static InfoCHS cHS;
        private InfoCHS()
        {
            InnitDictionary();
        }

        private void InnitDictionary()
        {
            DicStationID.Add("Cap_Input", "载具绑定产品");
            DicStationID.Add("Print_Carton", "分BIN工站");
            DicStationID.Add("PSA1_Load", "PSA1机台上料");
            DicStationID.Add("PSA_Press", "PSA压合");
            DicStationID.Add("PSA2_Load", "Boat5&Boat6倒盘");
            DicStationID.Add("ChangeBoat4", "Boat6&Boat4倒盘");
            DicStationID.Add("PSA2_Unload", "PSA2机台下料");
            //DicStationID.Add("", "");
            //DicStationID.Add("", "");

            DicEventName.Add("Carrier_Check", "合法性核对");
            DicEventName.Add("SN_FeedingCheck", "Cap合法性检查");
            DicEventName.Add("SN_CheckIN", "入站");
            DicEventName.Add("SN_CheckOut", "出站");
            DicEventName.Add("SN_CarrierBind", "治具绑定");
            DicEventName.Add("Heartbeat", "心跳");
            DicEventName.Add("Process", "Process");
            DicEventName.Add("Login", "登录MES");
            DicEventName.Add("Packing_Menu", "Packing_Menu");
            DicEventName.Add("Data_Collection", "Data_Collection");
            DicEventName.Add("Glue_CheckOut", "Glue_CheckOut");
        }

        public static InfoCHS Instance()
        {
            if (cHS == null)
            {
                cHS = new InfoCHS();
            }
            return cHS;
        }

    }
}
