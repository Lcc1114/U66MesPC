using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Threading;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;

namespace U66MesPC.Common
{
    public class TimeoutHandler : DelegatingHandler
    {
        private int _timeout1; //第一次超时时间
        private int _timeout2; //第二次超时时间
        private int _max_count;
        ///
        /// 超时重试
        ///
        ///重试次数
        ///超时时间
        public TimeoutHandler(int max_count = 2, int timeout1 = 3000, int timeout2 = 5000)
        {
            base.InnerHandler = new HttpClientHandler();
            _timeout1 = timeout1;
            _timeout2 = timeout2;
            _max_count = max_count;
        }
        private void GetRequestEntity(HttpRequestMessage request, int count)
        {
            if (request == null) return;
            BaseRequestParams baseRequest = null;
            AlarmRequest alarmRequest = null;
            var ss = request.Content.ReadAsStringAsync();
            if (request.RequestUri.ToString().Contains("SN_CheckIN"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<CheckInRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("SN_CheckOut"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<CheckOutRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("SN_FeedingCheck"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<FeedingCheckRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("SN_CarrierBind"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<CarrierBindRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Carrier_Check"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<CarrierCheckRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Data_Collection"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<DataCollectionRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Glue_CheckOut"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<GlueCheckOutRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Status"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<StatusRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Login"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<LoginRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Packing_Menu"))
            {
                baseRequest = new JavaScriptSerializer().Deserialize<PackingMenuRequest>(ss.Result);
            }
            else if (request.RequestUri.ToString().Contains("Alarm"))
            {
                alarmRequest = new JavaScriptSerializer().Deserialize<AlarmRequest>(ss.Result);
            }
            string retryStr = count != 1 ? $"重发_第{count - 1}次;" : "";
            object obj = null;
            if (baseRequest != null)
                obj = baseRequest;
            else if (alarmRequest != null)
                obj = alarmRequest;
            //Application.Current?.Dispatcher.Invoke(new Action(() =>
            //{
            //    LogsUtil.Instance.AddEventParams(new LogInfo(baseRequest?.StationID ?? alarmRequest?.StationID, baseRequest?.EventID ?? alarmRequest?.EventID, EventIO.发送, retryStr + (baseRequest?.GetVaidData() ?? alarmRequest?.GetValidData()), obj));
            //}));
        }
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 1; i <= _max_count; i++)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(i == 1 ? _timeout1 : _timeout2);
                try
                {
                    GetRequestEntity(request, i);
                    response = await base.SendAsync(request, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    //请求超时
                    if (ex is TaskCanceledException)
                    {
                        LogsUtil.Instance.WriteError($"接口{request.RequestUri.AbsoluteUri}，第{i}次请求超时，{ex.ToString()}");
                        if (i >= _max_count)
                        {
                            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                            {
                                Content = new StringContent("{\"code\":-1,\"Result\":\"ERROR\",\"data\":\"\",\"msg\":\"接口请求超时，MES系统连接异常\"}", Encoding.UTF8, "text/json")
                            };
                        }
                    }
                    else
                    {
                        LogsUtil.Instance.WriteError($"接口{request.RequestUri.AbsoluteUri}，第{i}次请求出错，{ex.ToString()}");
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("{\"code\":-1,\"Result\":\"ERROR\",\"data\":\"\",\"msg\":\"接口请求出错\"}", Encoding.UTF8, "text/json")
                        };
                    }
                }
            }
            return response;
        }
    }
    public class HttpClientHelper
    {
        public static HttpClient client;
        static HttpClientHelper()
        {
            HttpMessageHandler handler = new TimeoutHandler(2, 2000, 5000);
            //client = new HttpClient(handler);
            client = new HttpClient();
        }
        public static StringContent GetStringContent(object request, string ticket = null)
        {
            StringContent content = new StringContent(new JavaScriptSerializer().Serialize(request));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (!string.IsNullOrEmpty(ticket))
                content.Headers.Add("Authorization", $"key {ticket}");
            return content;
        }
        //产品进站核对
        public static async Task<CheckInResponse> SNCheckInAsync(CheckInRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<CheckInResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                else
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //检查材料的合法性
        public static async Task<FeedingCheckResponse> SNFeedingCheckAsync(FeedingCheckRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<FeedingCheckResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //出站记录产品信息
        public static async Task<CheckOutResponse> SNCheckOutAsync(CheckOutRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<CheckOutResponse>(ret);
                resp.SN_Info.ForEach(p => p.MSG_ID = p.MSG_ID.Trim());
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        public static async Task<HttpResponseMessage> PostDataAsync(BaseRequestParams request, string url, AlarmRequest alarmRequest = null, string ticket = null)
        {
            try
            {
                object obj = null;
                if (request != null)
                    obj = request;
                else
                    obj = alarmRequest;
                var a = Environment.TickCount;
                HttpResponseMessage response = await client.PostAsync(url, GetStringContent(obj, ticket));
                if (request != null && request.EventID != "Status") //状态信息,报警信息上传不写入信息流与日志
                {
                    Application.Current?.Dispatcher.Invoke(new Action(() =>
                    {
                        LogsUtil.Instance.AddEventParams(new LogInfo(request?.StationID ?? alarmRequest?.StationID, request?.EventID ?? alarmRequest?.EventID, EventIO.发送, request?.GetVaidData() ?? alarmRequest?.GetValidData(), request));
                    }));
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void AddErrorLogInfo(string stationID, string eventID, BaseResponseParams resp = null, string msg = null)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, EventIO.错误, resp?.GetRetData() ?? msg, resp));
            }));
        }
        public static void AddOutputLogInfo(string stationID, string eventID, BaseResponseParams resp = null, string msg = null)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, EventIO.发送, resp?.GetRetData() ?? msg, resp));
            }));
        }
        public static void AddInputLogInfo(string stationID, string eventID, BaseResponseParams resp = null, string msg = null)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
           {
               LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, EventIO.接收, resp?.GetRetData() ?? msg, resp));
           }));
        }
        //核对载具或Tray盘
        public static async Task<CarrierCheckResponse> CarrierCheckAsync(CarrierCheckRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<CarrierCheckResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }
        }
        //胶水上下线注册
        public static async Task<GlueCheckOutResponse> GlueCheckOutAsync(GlueCheckOutRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<GlueCheckOutResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //温湿度数据上报
        public static async Task<DataCollectionResponse> DataCollectionAsync(DataCollectionRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<DataCollectionResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //产品绑定载具
        public static async Task<CarrierBindResponse> SNCarrierBindAsync(CarrierBindRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<CarrierBindResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //机台状态及停机code上报
        public static async Task<StatusResponse> StatusAsync(StatusRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<StatusResponse>(ret);
                //AddInputLogInfo(request.StationID, request.EventID, resp);
                #region 报警地址记录
                string alarm = null;
                try
                {
                    int code = int.Parse(request.Status.Substring(request.Status.Length - 3));
                    int split = (code - 1) / 2;
                    int last = (code - 1) % 2;
                    int add = 11000 + split;
                    if (last == 0)
                        alarm = "，低位报警，地址：" + add;
                    else
                        alarm = "，高位报警，地址：" + add;
                }
                catch { }
                #endregion
                if (response.IsSuccessStatusCode)
                {
                    LogsUtil.Instance.WriteLogStatus(request.MachineID + "，上传完成", request.Status + alarm);
                    return resp;
                }
                else
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    //LogsUtil.Instance.WriteError(msg);
                    LogsUtil.Instance.WriteLogStatus(request.MachineID + "，上传失败", request.Status + alarm);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                //if (!(ex is MesConnException))
                //AddInputLogInfo(request.StationID, request.EventID, null, ret);
                LogsUtil.Instance.WriteLogStatus(request.MachineID + "，异常", ex.Message);
                throw ex;
            }

        }
        //机台未停机报警ID上报
        public static async Task<AlarmResponse> AlarmAsync(AlarmRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                Console.WriteLine($"报警发送：{request.StationID};{request.EventID}->{new JavaScriptSerializer().Serialize(request)}");
                HttpResponseMessage response = await PostDataAsync(null, url + request.EventID, request);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<AlarmResponse>(ret);
                Console.WriteLine($"报警接收：{request.StationID};{request.EventID}->{resp}");
                //AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    //LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                //if (!(ex is MesConnException))
                //AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        /// <summary>
        /// 核对前一个平行工站是否有做过，不更新状态，仅核对流程
        //        MES回复：
        //1、Pass：当前boat没有作业，可在下一个平行工站作业
        //2、Alarm：当前boat已经做过，不能再重复作业
        //3、Fail：当前baot有部分产品已做过，部分产品未做，需报警停机取出确认
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<FlowCheckResponse> FlowCheckAsync(FlowCheckRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + "SN_FeedingCheck");
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<FlowCheckResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        //登录验证
        public static async Task<LoginResponse> LoginAsync(LoginRequest request, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID);
                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<LoginResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
        public static async Task<PackingMenuResponse> PackingMenuAsync(PackingMenuRequest request, string ticket, string url)
        {
            string ret = string.Empty;
            try
            {
                HttpResponseMessage response = await PostDataAsync(request, url + request.EventID, null, ticket);

                ret = await response.Content.ReadAsStringAsync();
                var resp = new JavaScriptSerializer().Deserialize<PackingMenuResponse>(ret);
                AddInputLogInfo(request.StationID, request.EventID, resp);
                if (response.IsSuccessStatusCode)
                {
                    return resp;
                }
                else
                {
                    string msg = $"工站:{request.StationID},{request.EventID}请求失败，状态码:{response.StatusCode}，信息：{resp.Msg}";
                    LogsUtil.Instance.WriteError(msg);
                    throw new MesConnException(msg);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MesConnException))
                    AddInputLogInfo(request.StationID, request.EventID, null, ret);
                throw ex;
            }

        }
    }
}
