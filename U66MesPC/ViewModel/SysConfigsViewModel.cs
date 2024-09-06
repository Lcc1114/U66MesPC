using System;
using System.Collections.Generic;
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
    public class SysConfigsViewModel:ViewModelBase
    {
        public SysConfigsViewModel(SysConfigs sysConfigs) 
        {
            _config = sysConfigs ?? new SysConfigs();
            //LoadConfig();
        }
        private SysConfigs _config;
        public SysConfigs Config
        {
            get
            {
                return _config;
            }
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }
        private void LoadConfig()
        {
            try
            {
                using(var db=new DBContext())
                {
                    var config = db.SysConfigs?.FirstOrDefault();
                    Config = config ?? new SysConfigs();
                }
            }catch(Exception ex)
            {
                MessageBox.Show($"加载参数失败！{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public Action CloseWindowAction { get; set; }
        public ICommand OnSaveCommand
        {
            get
            {
                return new RelayCommand(o =>{
                    try
                    {
                        using (var db = new DBContext())
                        {
                            SysConfigs sysConfig = null;
                            if(Config==null || Config.ID==0)
                                db.SysConfigs.Add(Config);
                            else
                            {
                                sysConfig = db.SysConfigs?.FirstOrDefault(c => c.ID == Config.ID);
                                sysConfig.CloneFrom(Config);
                            }
                            db.SaveChanges();
                        }
                        MessageBox.Show($"保存参数成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        CloseWindowAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存参数失败！{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }
        public ICommand OnCancelCommand
        {
            get
            {
                return new RelayCommand(o => {
                        CloseWindowAction?.Invoke();
                });
            }
        }
    }
}
