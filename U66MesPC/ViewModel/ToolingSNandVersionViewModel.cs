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


namespace U66MesPC.ViewModel
{
    public class ToolingSNandVersionViewModel: ViewModelBase
    {
        public ToolingSNandVersionViewModel()
        {
            ToolingSNandVersionModelColl = new ObservableCollection<ToolingSNandVersionModel>();
            //using (var db = new DBContext())
            //{
            //    ToolingSNandVersionModelColl = new ObservableCollection<ToolingSNandVersionModel>(db.DbToolingSNAndVersion?.ToList());
            //}
        }
        //view-model
        private ToolingSNandVersionModel toolingSNandVersionModel;
        public ToolingSNandVersionModel ToolingSNandVersionModel
        {
            get
            {
                return toolingSNandVersionModel;
            }
            set
            {
                toolingSNandVersionModel = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ToolingSNandVersionModel> toolingSNandVersionModelColl;

        public ObservableCollection<ToolingSNandVersionModel> ToolingSNandVersionModelColl
        {
            get => toolingSNandVersionModelColl;
            set
            {
                toolingSNandVersionModelColl = value;
                OnPropertyChanged(nameof(ToolingSNandVersionModelColl));
            }
        }
        public void AddToolingSNandVersionModel(string header)
        {
            ToolingSNandVersionModelColl.Add(new ToolingSNandVersionModel { Header = header });
        }
        public void AddToolingSNandVersionModelList(ToolingSNandVersionModel tvModel,string newstring)
        {
            tvModel.Strings.Add(newstring);
            OnPropertyChanged("ToolingSNandVersionModelColl");
        }
        public void DeleteToolingSNandVersionModelList(ToolingSNandVersionModel tvModel, string newstring)
        {
            tvModel.Strings.Remove(newstring);
            OnPropertyChanged("ToolingSNandVersionModelColl");
        }
        //public void AddStringGroup()
        //{
        //    ToolingSNandVersionModels.Add(new ToolingSNandVersionModel());
        //}
        public ICommand OnSaveToolingSNCommand
        {
            get
            {
                return new RelayCommand(o => {
                    try
                    {
                        using (var db = new DBContext())
                        {
                            //ToolingSNandVersionModel.CloneTSNVM();
                            //foreach (var item in ToolingSNandVersionModelColl)
                            //{
                            //    if (item != null || item.ID == 0)
                            //        db.DbToolingSNAndVersion.Add(item);
                            //    else
                            //    {

                            //    }
                            //}

                            //ToolingSNandVersionModel tv = null;
                            //tv.CloneTSNVM();
                            //if (ToolingSNandVersionModel == null || ToolingSNandVersionModel.ID == 0)
                            //    db.DbToolingSNAndVersion.Add(ToolingSNandVersionModel);
                            //else
                            //{
                            //    tv = db.DbToolingSNAndVersion?.FirstOrDefault(c => c.ID == ToolingSNandVersionModel.ID);
                            //    tv.CloneFrom(ToolingSNandVersionModel);
                            //}
                            //db.SaveChanges();
                        }
                        MessageBox.Show($"保存参数成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        //OnRefreshSysConfigCommand.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存参数失败！{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }
        //public ICommand OnRefreshSysConfigCommand
        //{
        //    get
        //    {
        //        return new RelayCommand(o => {
        //            using (var db = new DBContext())
        //            {
        //                toolingSNandVersionModelColl = new ObservableCollection<ToolingSNandVersionModel>(db.DbToolingSNAndVersion?.ToList());
        //            }
        //            OnPropertyChanged(nameof(ToolingSNandVersionModelColl));
        //        });
        //    }
        //}
    }
}
