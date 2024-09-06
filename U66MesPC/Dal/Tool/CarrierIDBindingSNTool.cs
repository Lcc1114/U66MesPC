using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public class CarrierIDBindingSNTool
    {
        /// <summary>
        /// 插入CarrierIDBindingSN数据
        /// </summary>
        /// <param name="carrierIDBindingSN"></param>
        /// <returns></returns>
        public int Insert(CarrierIDBindingSN carrierIDBindingSN)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(carrierIDBindingSN);
        }
        /// <summary>
        /// 根据载具码查询CarrierIDBindingSN数据
        /// </summary>
        /// <param name="carrierID"></param>
        /// <returns></returns>
        public CarrierIDBindingSN Query(string carrierID)
        {
            List<CarrierIDBindingSN> list = new List<CarrierIDBindingSN>();
            Expression<Func<CarrierIDBindingSN, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
                return list[0];
            else
                return null;
        }
        /// <summary>
        /// 根据载具码删除CarrierIDBindingSN数据
        /// </summary>
        /// <param name="carrierID"></param>
        /// <returns></returns>
        public int Delete(string carrierID)
        {
            List<CarrierIDBindingSN> list = new List<CarrierIDBindingSN>();
            Expression<Func<CarrierIDBindingSN, bool>> funcwhere = t => t.CarrierID == carrierID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                CarrierIDBindingSN carrierIDBindingSN = new CarrierIDBindingSN();
                for (int i = 0; i < list.Count; i++)
                {
                    carrierIDBindingSN = list[i];
                    sqlHelper.DeleteAtID<CarrierIDBindingSN>(carrierIDBindingSN.ID);
                }
                return 1;
            }
            else
                return -1;
        }
    }
}
