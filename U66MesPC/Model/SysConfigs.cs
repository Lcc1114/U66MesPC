using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common.Station;

namespace U66MesPC.Model
{
    [Table("SysConfigs")]
    public class SysConfigs
    {
        [Column("ID")]
        public int ID { get; set; }
        [Column("Url")]
        public string Url { get; set; }
        [Column("Line")]
        public string Line { get; set; }
        [Column("StationID")]
        public string StationID { get; set; }
        [Column("MachineID")]
        public string MachineID { get; set; }
        [Column("Token")]
        public string Token { get; set; }
        [Column("OPID")]
        public string OPID { get; set; }
        [Column("FixSN")]
        public string FixSN { get; set; }
        [Column("StationType")]
        public StationType StationType{get;set;}
        [Column("PlcIP")]
        public string PlcIP { get; set; }
        [Column("PlcPort")]
        public string PlcPort { get; set; }
        [Column("Mold")]
        public string Mold { get; set; }
        [Column("AlarmCode")]
        public string AlarmCode { get; set; }
        public void CloneFrom(SysConfigs sysConfigs)
        {
            Url = sysConfigs.Url;
            Line = sysConfigs.Line;
            StationID = sysConfigs.StationID;
            MachineID = sysConfigs.MachineID;
            Token = sysConfigs.Token;
            OPID = sysConfigs.OPID;
            FixSN = sysConfigs.FixSN;
            StationType = sysConfigs.StationType;
            PlcIP = sysConfigs.PlcIP;
            PlcPort = sysConfigs.PlcPort;
            Mold = sysConfigs.Mold;
            AlarmCode = sysConfigs.AlarmCode;
        }
        public SysConfigs CloneSysConfigs()
        {
            return new SysConfigs()
            {
                ID = ID,
                Url = Url,
                Line = Line,
                StationID = StationID,
                MachineID = MachineID,
                Token = Token,
                OPID = OPID,
                FixSN = FixSN,
                StationType = StationType,
                PlcIP = PlcIP,
                PlcPort = PlcPort,
                Mold=Mold,
                AlarmCode = AlarmCode
            };
        }
        public SysConfigs() { }

        public SysConfigs(string url, string line, string stationID, string machineID, string token, string opID,string fixSN,StationType stationType,string plcIP,string plcPort,string mold,string alarm)
        {
            Url = url;
            Line = line;
            StationID = stationID;
            MachineID = machineID;
            Token = token;
            OPID = opID;
            FixSN = fixSN;
            StationType = stationType;
            PlcIP = plcIP;
            PlcPort = plcPort;
            Mold = mold;
            AlarmCode = alarm;
        }
        public override string ToString()
        {
            return $"ID:{ID};StationType:{StationType};PlcIP:{PlcIP};PlcPort:{PlcPort};Url:{Url};Line:{Line};StationID:{StationID};MachineID:{MachineID};Token:{Token};OPID:{OPID};FixSN:{FixSN};Mold={Mold};Alarm:{AlarmCode};";
        }
    }
}
