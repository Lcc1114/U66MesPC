using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDProductUnloadTool
    {
        public int Insert(CarrierIDProductUnload carrierIDProductUnload)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDProductUnload);
        }
        public CarrierIDProductUnload Query(string carrierID)
        {
            List<CarrierIDProductUnload> list = new List<CarrierIDProductUnload>();
            Expression<Func<CarrierIDProductUnload, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        public int Delete(string carrierID)
        {
            List<CarrierIDProductUnload> list = new List<CarrierIDProductUnload>();
            Expression<Func<CarrierIDProductUnload, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDProductUnload carrierIDProductUnload = new CarrierIDProductUnload();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDProductUnload = list[i];
                    sqlHelper.DeleteAtID<CarrierIDProductUnload>(carrierIDProductUnload.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDProductUnload carrierIDProductUnload)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDProductUnload);
        }
    }
}
