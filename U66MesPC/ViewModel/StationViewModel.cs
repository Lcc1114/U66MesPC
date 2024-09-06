using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using U66MesPC.Common;
using U66MesPC.Common.Station;
using U66MesPC.Model;
using U66MesPC.View;

namespace U66MesPC.ViewModel
{
    public class StationViewModel : ViewModelBase
    {
        private StationBase _station;
        public StationBase Station
        {
            get
            {
                return _station;
            }
            set
            {
                _station = value;
                OnPropertyChanged();
            }
        }
        public StationViewModel(StationType stationType, SysConfigs sysConfig)
        {
            try
            {
                if (stationType == StationType.CapInput)
                    _station = new CapInput(sysConfig);
                else if (stationType == StationType.PCBALoad)
                    _station = new PCBALoad(sysConfig);
                else if (stationType == StationType.PCBAUnload)
                    _station = new PCBAUnLoad(sysConfig);
                else if (stationType == StationType.EFlexLoad)
                    _station = new EFlexLoad(sysConfig);
                else if (stationType == StationType.EFlexUnload)
                    _station = new EFlexUnLoad(sysConfig);
                else if (stationType == StationType.TrimInput)
                    _station = new TrimInput(sysConfig);
                else if (stationType == StationType.FlowCheck)
                    _station = new FlowCheck(sysConfig);
                else if (stationType == StationType.PSA1Load)
                    _station = new PSA1Load(sysConfig);
                else if (stationType == StationType.PSAPress)
                    _station = new PSA2Press(sysConfig);
                else if (stationType == StationType.PSA2Load)
                    _station = new PSA2Load(sysConfig);
                else if (stationType == StationType.PSA2Unload)
                    _station = new PSA2Unload(sysConfig);
                else if (stationType == StationType.ChangeBoat4)
                    _station = new ChangeBoat4(sysConfig);
                else if (stationType == StationType.Binning)
                    _station = new Binning(sysConfig);
                else if (stationType == StationType.SortFlow)
                    _station = new SortFlow(sysConfig);
            }
            catch (Exception ex)
            {
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "Initialize", null, $"{ex.Message}");
            }

        }
        public string GetStationType()
        {
            return _station.GetStaionType();
        }
        public bool GetConnected()
        {
            return _station.Connected;
        }
        public void Release()
        {
            _station?.Release();
        }
        public ICommand SNCheckInCommand => new RelayCommand(o => _station?.SNCheckInAsync());
        public ICommand SNFeedingCheckCommand => new RelayCommand(o => _station?.FeedingCheck());
        public ICommand SNCheckOutCommand => new RelayCommand(o => _station?.SNCheckOut());
        public ICommand CarrierCheckCommand => new RelayCommand(o => _station?.SNCarrierCheck());
        public ICommand GlueCheckOutCommand => new RelayCommand(o => _station?.GlueCheckOut());
        public ICommand DataCollectionCommand => new RelayCommand(o => _station?.DataCollection());
        public ICommand SNCarrierBindCommand => new RelayCommand(o => _station?.SNCarrierBind());
        public ICommand StatusCommand => new RelayCommand(o => _station?.Status());
        public ICommand AlarmCommand => new RelayCommand(o => _station?.Alarm());
        public ICommand OperateCommands
        {
            get
            {
                return new RelayCommand(obj =>
                {
                    WindowPackingMenuLogin window = new WindowPackingMenuLogin(Station);
                    window.ShowDialog();
                });
            }
        }
    }
}
