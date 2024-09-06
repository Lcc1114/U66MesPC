using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using U66MesPC.Common;
using U66MesPC.Dal;
using U66MesPC.Model;
using U66MesPC.View;

namespace U66MesPC.ViewModel
{
   public class SysconfigsManagementViewModel:ViewModelBase
    {
        private ObservableCollection<SysConfigs> _sysConfigsList = new ObservableCollection<SysConfigs>();
        public ObservableCollection<SysConfigs> SysConfigsList
        {
            get
            {
                return _sysConfigsList;
            }
            set
            {
                _sysConfigsList = value;
                OnPropertyChanged();
            }
        }
        public SysconfigsManagementViewModel()
        {
            using(var db=new DBContext())
            {
                _sysConfigsList = new ObservableCollection<SysConfigs>(db.SysConfigs?.ToList()) ;
            }
        }
        public ICommand OnAddSysConfigCommand
        {
            get
            {
                return new RelayCommand(o => {
                    WindowSysConfigs window = new WindowSysConfigs(null);
                    window.ShowDialog();
                    OnRefreshSysConfigCommand.Execute(null);
                });
            }
        }
        public ICommand OnEditSysConfigCommand
        {
            get
            {
                return new RelayCommand(o => {
                    
                    WindowSysConfigs window = new WindowSysConfigs(Selected.CloneSysConfigs());
                    window.ShowDialog();
                    OnRefreshSysConfigCommand.Execute(null);
                });
            }
        }
        private SysConfigs _selected;
        public SysConfigs Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }
       
        public ICommand OnRefreshSysConfigCommand
        {
            get
            {
                return new RelayCommand(o => {
                    using (var db = new DBContext())
                    {
                        _sysConfigsList = new ObservableCollection<SysConfigs>(db.SysConfigs?.ToList());
                    }
                    OnPropertyChanged(nameof(SysConfigsList));
                });
            }
        }
        public ICommand OnDeleteSysConfigCommand
        {
            get
            {
                return new RelayCommand(o => {
                    if (Selected == null) return;
                    if(MessageBox.Show("确定删除吗？删除后无法恢复！","提示",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
                    {
                        using (var db = new DBContext())
                        {
                            db.SysConfigs.ToList().ForEach(s => Console.WriteLine(s.ToString()));
                            var deleteSysConfig = db.SysConfigs?.FirstOrDefault(s => s.ID == Selected.ID);
                            if (deleteSysConfig != null)
                            {
                                db.SysConfigs?.Remove(deleteSysConfig);
                                db.SaveChanges();
                            }
                        }
                        OnRefreshSysConfigCommand.Execute(null);
                    }    
                });
            }
        }
    }
}
