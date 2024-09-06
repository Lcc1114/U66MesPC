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
using U66MesPC.View;

namespace U66MesPC.ViewModel
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Ticket { get; set; } //密钥
        public string RightList { get; set; } //权限
        public UserInfo(string userName,string pwd,string ticket,string rightList)
        {
            UserName = userName;
            Password = pwd;
            Ticket = ticket;
            RightList = rightList;
        }
    }
    public class PackingMenuViewModel : ViewModelBase
    {
        public PackingMenuViewModel(StationBase station)
        {
            Station = station;
        }
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
        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }
        public Action CloseWindowAction { get; set; }
        public ICommand LoginCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    UserInfo userInfo1 = new UserInfo(UserName, Password, "TICKETXXX", "清箱，砍箱");
                    CloseWindowAction?.Invoke();
                    WindowPackingMenuOperate window1 = new WindowPackingMenuOperate(userInfo1, Station);
                    window1.ShowDialog();
                    return;
                    LoginRequest request = new LoginRequest(Config,UserName,Password);
                    Task<LoginResponse> response =HttpClientHelper.LoginAsync(request,Config.Url);
                    response.Wait();
                    if(response.Result!=null && response.Result.IsSuccess.Equals("true"))
                    {
                        var data = response.Result.Data.FirstOrDefault();
                        if (data!=null && data.bRes.Equals("true"))
                        {
                            UserInfo userInfo = new UserInfo(UserName,Password,data.Ticket,data.rightList);
                            CloseWindowAction?.Invoke();
                            WindowPackingMenuOperate window = new WindowPackingMenuOperate(userInfo, Station);
                            window.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show($"登录失败，{response?.Result?.ErrorMessage}！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"登录失败，{response?.Result?.ErrorMessage}！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }

    }
}
