using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using U66MesPC.Common;
using U66MesPC.Common.Station;
using U66MesPC.Model;

namespace U66MesPC.ViewModel
{
    public class PackingMenuOperateViewModel : ViewModelBase
    {
        public PackingMenuOperateViewModel(UserInfo userInfo, StationBase station)
        {
            UserInfo = userInfo;
            Station = station;
            _userName = UserInfo.UserName;
            _enableForceFullBox = UserInfo.RightList.Contains("强满");
            _enableReCount = UserInfo.RightList.Contains("清箱");
            _enableRepeatPrint = UserInfo.RightList.Contains("补印");
            _enableCancelBox = UserInfo.RightList.Contains("砍箱");
            LocationIDList = new List<string>() { "6#", "7#", "8#" };
        }
        public List<string> LocationIDList;
        public UserInfo UserInfo { get; set; }
        public StationBase Station { get; set; }
        public SysConfigs Config
        {
            get
            {
                return Station?.Configs;
            }
        }
        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
        private string _boxID;
        public string BoxID
        {
            get
            {
                return _boxID;
            }
            set
            {
                _boxID = value;
                OnPropertyChanged();
            }
        }
        private bool _enableReCount;
        public bool EnableReCount
        {
            get
            {
                return _enableReCount;
            }
            set
            {
                _enableReCount = value;
                OnPropertyChanged();
            }
        }
        private bool _enableForceFullBox;
        public bool EnableForceFullBox
        {
            get
            {
                return _enableForceFullBox;
            }
            set
            {
                _enableForceFullBox = value;
                OnPropertyChanged();
            }
        }
        private bool _enableRepeatPrint;
        public bool EnableRepeatPrint
        {
            get
            {
                return _enableRepeatPrint;
            }
            set
            {
                _enableRepeatPrint = value;
                OnPropertyChanged();
            }
        }
        private bool _enableCancelBox;
        public bool EnableCancelBox
        {
            get
            {
                return _enableCancelBox;
            }
            set
            {
                _enableCancelBox = value;
                OnPropertyChanged();
            }
        }
        public string _selectedLocationName;
        public string SelectionLocationID
        {
            get
            {
                return _selectedLocationName;
            }
            set
            {
                _selectedLocationName = value;
                OnPropertyChanged();
            }
        }
        private bool PackingMenu(string menu,string boxID="")
        {
            PackingMenuRequest request = new PackingMenuRequest(Config, menu, boxID, UserInfo.Password);
            Task<PackingMenuResponse> response = HttpClientHelper.PackingMenuAsync(request, UserInfo.Ticket, Config.Url);
            if (response.Result != null && response.Result.Result.ToLower().Equals("pass"))
            {
                MessageBox.Show($"操作成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            else
            {
                MessageBox.Show($"操作失败，{response?.Result?.Msg}！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public ICommand PackingMenuCommand
        {
            get
            {
                return new RelayCommand(param =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(SelectionLocationID))
                        {
                            MessageBox.Show($"请先选择下料位再操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        if (Config == null)
                        {
                            MessageBox.Show($"Config为空，请先设置参数后再操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        string menu = param.ToString();

                        if (menu.Equals("1") || menu.Equals("2"))
                        {
                            PackingMenu(menu);
                            Station.ReCount(SelectionLocationID);
                        }
                        else if (menu.Equals("3") || menu.Equals("4"))
                        {
                            if (string.IsNullOrEmpty(BoxID))
                            {
                                MessageBox.Show($"请先输入BoxID/SN再操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                            PackingMenu(menu, BoxID);
                        }
                    }
                    catch(Exception ex)
                    {
                        string msg = ex?.InnerException?.Message ?? ex.Message;
                        MessageBox.Show($"操作失败：{msg}！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                });
            }
        }
    }
}
