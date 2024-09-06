using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDBindingNoNumber50Tool
    {
        /// <summary>
        /// 插入carrierIDBindingNoNumber数据
        /// </summary>
        /// <param name="carrierIDBindingNoNumber"></param>
        /// <returns></returns>
        public int Insert(CarrierIDBindingNoNumber50 carrierIDBindingNoNumber)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDBindingNoNumber);
        }
        /// <summary>
        /// 根据载具码查询CarrierIDBindingNoNumber数据
        /// </summary>
        /// <param name="carrierID"></param>
        /// <returns></returns>
        public CarrierIDBindingNoNumber50 Query(string carrierID)
        {
            List<CarrierIDBindingNoNumber50> list = new List<CarrierIDBindingNoNumber50>();
            Expression<Func<CarrierIDBindingNoNumber50, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        /// <summary>
        /// 根据载具码删除CarrierIDBindingNoNumber数据
        /// </summary>
        /// <param name="carrierID"></param>
        /// <returns></returns>
        public int Delete(string carrierID)
        {
            List<CarrierIDBindingNoNumber50> list = new List<CarrierIDBindingNoNumber50>();
            Expression<Func<CarrierIDBindingNoNumber50, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDBindingNoNumber50 carrierIDBindingNoNumber = new CarrierIDBindingNoNumber50();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDBindingNoNumber = list[i];
                    sqlHelper.DeleteAtID<CarrierIDBindingNoNumber50>(carrierIDBindingNoNumber.ID);
                }
                return 1;
            }
            else
                return -1;
        }
        public int InsertData(string carrierID, CarrierIDBindingNoNumber50 carrierIDBindingNoNumber)
        {
            if (Query(carrierID) != null)
                Delete(carrierID);
            return Insert(carrierIDBindingNoNumber);
        }
    }
}
