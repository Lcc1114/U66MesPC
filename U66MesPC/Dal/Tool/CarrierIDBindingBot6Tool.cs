using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDBindingBot6Tool
    {
        public int Insert(CarrierIDBindingBot6 carrierIDBindingBot6)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDBindingBot6);
        }
        public CarrierIDBindingBot6 Query(string carrierID)
        {
            List<CarrierIDBindingBot6> list = new List<CarrierIDBindingBot6>();
            Expression<Func<CarrierIDBindingBot6, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        public int Delete(string carrierID)
        {
            List<CarrierIDBindingBot6> list = new List<CarrierIDBindingBot6>();
            Expression<Func<CarrierIDBindingBot6, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDBindingBot6 carrierIDBindingBot6 = new CarrierIDBindingBot6();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDBindingBot6 = list[i];
                    sqlHelper.DeleteAtID<CarrierIDBindingBot6>(carrierIDBindingBot6.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDBindingBot6 carrierIDBindingBot6)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDBindingBot6);
        }
    }
}
