using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using U66MesPC.Common;
using U66MesPC.Dal;
using U66MesPC.Model;

namespace U66MesPC.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        //DBContext db = new DBContext();
        public LoginViewModel()
        {
        }
        private string _userName = "test";
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
        private string _password = "123456";
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
        public bool LoginOk = false;
        public Action CloseWindowAction { get; set; }
        public ICommand LoginCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    using (DBContext db = new DBContext())
                    {
                        if (db.Users == null || db.Users.Count() == 0)
                        {
                            db.Users.Add(new User(UserName, Password, Role.Operator));
                            if (db.SaveChanges() != 1)
                                MessageBox.Show($"添加用户（{UserName}）失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            else
                            {
                                LoginOk = true;
                                CloseWindowAction?.Invoke();
                            }
                        }
                        else
                        {
                            var user = db.Users.FirstOrDefault(u => u.UserName == UserName && u.Password == Password);
                            LoginOk = user != null ? true : false;
                            if (user != null)
                            {
                                CloseWindowAction?.Invoke();
                                LogsUtil.Instance.WriteInfo($"用户：{UserName}登录了系统......");
                            }
                            else
                                MessageBox.Show("登录失败，用户名或密码错误！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                });
            }
        }
        public ICommand LogoutCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    CloseWindowAction?.Invoke();
                });
            }
        }
        public ICommand RegisterCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                    {
                        MessageBox.Show("用户名或密码不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (DBContext db = new DBContext())
                    {
                        if (db.Users.Any(u => u.UserName.Equals(UserName)))
                        {
                            MessageBox.Show("用户名已存在，请重新输入用户名！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        db.Users.Add(new User(UserName, Password, Role.Operator));
                        if (db.SaveChanges() != 1)
                            MessageBox.Show($"注册用户（{UserName}）失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        else
                            MessageBox.Show($"注册用户（{UserName}）成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            }
        }

    }
}
