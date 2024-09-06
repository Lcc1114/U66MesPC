using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDBindingBot4Tool
    {
        public int Insert(CarrierIDBindingBot4 carrierIDBindingBot4)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDBindingBot4);
        }
        public CarrierIDBindingBot4 Query(string carrierID)
        {
            List<CarrierIDBindingBot4> list = new List<CarrierIDBindingBot4>();
            Expression<Func<CarrierIDBindingBot4, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        public int Delete(string carrierID)
        {
            List<CarrierIDBindingBot4> list = new List<CarrierIDBindingBot4>();
            Expression<Func<CarrierIDBindingBot4, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDBindingBot4 carrierIDBindingBot4 = new CarrierIDBindingBot4();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDBindingBot4 = list[i];
                    sqlHelper.DeleteAtID<CarrierIDBindingBot4>(carrierIDBindingBot4.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDBindingBot4 carrierIDBindingBot4)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDBindingBot4);
        }
    }
}
