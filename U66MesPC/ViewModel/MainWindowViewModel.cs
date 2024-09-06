using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using U66MesPC.Common;
using U66MesPC.Common.Station;
using U66MesPC.View;

namespace U66MesPC.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static int Int_Count = 0;
        private string _allCount = "生产数量：" + Int_Count;
        public string AllCount
        {
            get
            {
                return _allCount;
            }
            set
            {
                _allCount = value;
                OnPropertyChanged();
            }
        }
        private string _mES_IP;
        public string MES_IP
        {
            get
            {
                return _mES_IP;
            }
            set
            {
                _mES_IP = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<StationBase> StationInfoList;

        public MainWindowViewModel()
        {
            StationInfoList = new ObservableCollection<StationBase>();
        }
        public void AddStationInfo(StationBase station)
        {
            if (station != null)
                StationInfoList.Add(station);
        }
        public ICommand OnEditConfigCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    WindowAuthority window = new WindowAuthority();
                    window.ShowDialog();
                });
            }
        }
        public ICommand OnDeleteMesAllCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    LogsUtil.Instance.LogMesInfoList.Clear();
                });
            }
        }
        public ICommand OnDeleteLogAllCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    LogsUtil.Instance.LogMsgInfoList.Clear();
                });
            }
        }

        public ICommand OnToolingSNCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    string fileCode = "_" + ConfigurationManager.AppSettings["LineID"] + "_" + ConfigurationManager.AppSettings["Tooling"];
                    string dirPath = Directory.GetCurrentDirectory() + $"\\PressHreadToolingSN";
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                    string path = dirPath + $"\\PressHreadToolingSN{fileCode}.txt";
                    if (!File.Exists(path))
                        File.Create(path).Close();
                    //File.Open(path, FileMode.OpenOrCreate);
                    //Process.Start(path);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo(path);
                    process.Start();
                    process.WaitForExit();
                    process.Dispose();
                    //ToolingSNView window = new ToolingSNView();
                    //window.ShowDialog();
                });
            }
            //get
            //{
            //    if (onToolingSNCommand == null)
            //    {
            //        onToolingSNCommand = new RelayCommand(OnToolingSN);
            //    }

            //    return onToolingSNCommand;
            //}
        }
        public ICommand OnPSAUnloadToolingSNCommand
        {
            get
            {
                return new RelayCommand(o =>
                {
                    string fileCode = "_" + ConfigurationManager.AppSettings["LineID"] + "_" + ConfigurationManager.AppSettings["Tooling"];
                    string dirPath = Directory.GetCurrentDirectory() + $"\\PSA2UnloadToolingSN";
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                    string path = dirPath + $"\\PSA2UnloadToolingSN{fileCode}.txt";
                    if (!File.Exists(path))
                        File.Create(path).Close();
                    //File.Open(path, FileMode.OpenOrCreate);
                    //Process.Start(path);
                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo(path);
                    process.Start();
                    process.WaitForExit();
                    process.Dispose();
                    //ToolingSNView window = new ToolingSNView();
                    //window.ShowDialog();
                });
            }
            //private void OnToolingSN(object commandParameter)
            //{
            //}
        }
    }
}
