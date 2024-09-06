using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDPressBindingSNTool
    {
        public int Insert(CarrierIDPressBindingSN carrierIDPressBindingSN)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDPressBindingSN);
        }
        public CarrierIDPressBindingSN Query(string carrierID)
        {
            List<CarrierIDPressBindingSN> list = new List<CarrierIDPressBindingSN>();
            Expression<Func<CarrierIDPressBindingSN, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        public int Delete(string carrierID)
        {
            List<CarrierIDPressBindingSN> list = new List<CarrierIDPressBindingSN>();
            Expression<Func<CarrierIDPressBindingSN, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDPressBindingSN carrierIDPressBindingSN = new CarrierIDPressBindingSN();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDPressBindingSN = list[i];
                    sqlHelper.DeleteAtID<CarrierIDPressBindingSN>(carrierIDPressBindingSN.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDPressBindingSN carrierIDPressBindingSN)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDPressBindingSN);
        }
    }
}
