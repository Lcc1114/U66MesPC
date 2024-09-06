using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDBindingBot5Tool
    {
        public int Insert(CarrierIDBindingBot5 carrierIDBindingBot5)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDBindingBot5);
        }
        public CarrierIDBindingBot5 Query(string carrierID)
        {
            List<CarrierIDBindingBot5> list = new List<CarrierIDBindingBot5>();
            Expression<Func<CarrierIDBindingBot5, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        public int Delete(string carrierID)
        {
            List<CarrierIDBindingBot5> list = new List<CarrierIDBindingBot5>();
            Expression<Func<CarrierIDBindingBot5, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDBindingBot5 carrierIDBindingBot5 = new CarrierIDBindingBot5();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDBindingBot5 = list[i];
                    sqlHelper.DeleteAtID<CarrierIDBindingBot5>(carrierIDBindingBot5.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDBindingBot5 carrierIDBindingBot5)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDBindingBot5);
        }
    }
}
