using HslCommunication.LogNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using U66MesPC.Model;

namespace U66MesPC.Common
{
    public class LogsUtil
    {
        private static readonly LogsUtil _instance;
        public EventHandler eventHandlerMESTextChang;
        public EventHandler eventHandlerLogTextChang;
        static LogsUtil()
        {
            _instance = new LogsUtil();
            string path = Application.StartupPath + "\\log";
            logNet = new LogNetDateTime(path, GenerateMode.ByEveryDay);//按每天
            logNet.SetMessageDegree(HslMessageDegree.INFO);//除DEBUG外，都存储
            logStatus = new LogNetDateTime(path + "\\Status", GenerateMode.ByEveryDay);
            logStatus.SetMessageDegree(HslMessageDegree.INFO);
        }
        public void InitMesEventNameList()
        {
            MesEventNameList = new List<string>();
            MesEventNameList.Add(CheckInRequest._eventID);
            MesEventNameList.Add(CheckOutRequest._eventID);
            MesEventNameList.Add(CarrierBindRequest._eventID);
            MesEventNameList.Add(CarrierCheckRequest._eventID);
            MesEventNameList.Add(FeedingCheckRequest._eventID);
            MesEventNameList.Add(FlowCheckRequest._eventID);
            MesEventNameList.Add(LoginRequest._eventID);
            MesEventNameList.Add(PackingMenuRequest._eventID);
            MesEventNameList.Add(DataCollectionRequest._eventID);
            MesEventNameList.Add(GlueCheckOutRequest._eventID);

            MesEventNameList.Add("合法性核对");
            MesEventNameList.Add("Cap合法性检查");
            MesEventNameList.Add("入站");
            MesEventNameList.Add("出站");
            MesEventNameList.Add("治具绑定");
            MesEventNameList.Add("登录MES");
        }
        private static ILogNet logNet;
        private static ILogNet logStatus;
        public static LogsUtil Instance
        {
            get
            {
                return _instance;
            }
        }
        /// <summary>
        /// MES信息
        /// </summary>
        private ObservableCollection<LogInfo> _logMesInfoList = new ObservableCollection<LogInfo>();
        public ObservableCollection<LogInfo> LogMesInfoList
        {
            get
            {
                return _logMesInfoList;
            }
            set
            {
                _logMesInfoList = value;
            }
        }
        /// <summary>
        /// log信息
        /// </summary>
        private ObservableCollection<LogInfo> _logMsgInfoList = new ObservableCollection<LogInfo>();
        public ObservableCollection<LogInfo> LogMsgInfoList
        {
            get
            {
                return _logMsgInfoList;
            }
            set
            {
                _logMsgInfoList = value;
            }
        }
        public List<string> MesEventNameList;
        public void AddEventParams(LogInfo logInfo)
        {
            lock (this)
            {
                if (MesEventNameList.Contains(logInfo.EventName))
                {
                    if (_logMesInfoList.Count >= 600)
                    {
                        for (int i = 0; i < _logMesInfoList.Count - 600; i++)
                            _logMesInfoList.RemoveAt(0);
                    }
                    _logMesInfoList.Add(logInfo);
                    eventHandlerMESTextChang?.Invoke(_logMesInfoList.Count - 1, null);
                }
                else
                {
                    if (_logMsgInfoList.Count >= 600)
                    {
                        for (int i = 0; i < _logMsgInfoList.Count - 600; i++)
                            _logMsgInfoList.RemoveAt(0);
                    }
                    _logMsgInfoList.Add(logInfo);
                    eventHandlerLogTextChang?.Invoke(_logMsgInfoList.Count - 1, null);
                }
                WriteInfo(logInfo.ToString());
            }
        }
        public void WriteDebug(string message)
        {
            logNet.WriteDebug(message);
        }
        public void WriteInfo(string message)
        {
            logNet.WriteInfo(message);
        }
        public void WriteWarn(string message)
        {
            logNet.WriteWarn(message);
        }
        public void WriteError(string message)
        {
            logNet.WriteError(message);
        }
        public void WriteException(Exception ex)
        {
            logNet.WriteException(null, ex);
        }
        public void WriteLogStatus(string flag, string msg)
        {
            msg = DateTime.Now.ToString("【HH:mm:ss】") + flag + "：" + msg;
            logStatus.WriteAnyString(msg);
        }
    }
}
