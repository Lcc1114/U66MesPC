using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using U66MesPC.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace U66MesPC.Model
{
    [Table("ToolingSNAndVersion")]
    public class ToolingSNandVersionModel: ViewModelBase
    {
        public ToolingSNandVersionModel()
        {
            Strings = new ObservableCollection<string>();
        }
        [Column("ID")]
        public int ID { get; set; }
        [Column("Header")]
        public string Header { get; set; }
        [Column("Strings")]
        public ObservableCollection<string> Strings { get; set; }
        public ToolingSNandVersionModel CloneTSNVM()
        {
            return new ToolingSNandVersionModel()
            {
                ID = ID,
                Header= Header,
                Strings=Strings
            };
        }
        public void CloneFrom(ToolingSNandVersionModel tsnm)
        {
            Header = tsnm.Header;
        }
        //public string header;
        //public string Header
        //{
        //    get
        //    {
        //        return header;
        //    }
        //    set
        //    {
        //        header = value;
        //        OnPropertyChanged(nameof(Header));
        //    }
        //}

        //public ObservableCollection<string> Strings
        //{
        //    get
        //    {
        //        return strings;
        //    }
        //    set
        //    {
        //        strings = value;
        //        //OnPropertyChanged(nameof(Strings));
        //    }
        //}
        //public List<string> Strings { get; set; }
    }
}
