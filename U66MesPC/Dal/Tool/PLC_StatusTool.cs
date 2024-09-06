using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal.Tool
{
    public static class PLC_StatusTool
    {
        /// <summary>
        /// 插入CarrierIDBindingSN数据
        /// </summary>
        /// <param name="pLC_Status"></param>
        /// <returns></returns>
        public static int Insert(PLC_Status pLC_Status)
        {
            SqlHelper sqlHelper = new SqlHelper();
            return sqlHelper.Insert(pLC_Status);
        }
        /// <summary>
        /// 根据载具码查询CarrierIDBindingSN数据
        /// </summary>
        /// <param name="machineID"></param>
        /// <returns></returns>
        public static PLC_Status Query(string machineID)
        {
            List<PLC_Status> list = new List<PLC_Status>();
            Expression<Func<PLC_Status, bool>> funcwhere = t => t.MachineID == machineID;
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
        /// <param name="machineID"></param>
        /// <returns></returns>
        public static int Delete(string machineID)
        {
            List<PLC_Status> list = new List<PLC_Status>();
            Expression<Func<PLC_Status, bool>> funcwhere = t => t.MachineID == machineID;
            SqlHelper sqlHelper = new SqlHelper();
            list = sqlHelper.Query(funcwhere).ToList();
            if (list.Count > 0)
            {
                PLC_Status pLC_Status = new PLC_Status();
                for (int i = 0; i < list.Count; i++)
                {
                    pLC_Status = list[i];
                    sqlHelper.DeleteAtID<PLC_Status>(pLC_Status.ID);
                    //sqlHelper.DeleteAtString<PLC_Status>(pLC_Status.MachineID);
                }
                return 1;
            }
            else
                return -1;
        }

        public static int Update(string machineID, PLC_Status pLC_Status)
        {
            if (Query(machineID) != null)
                Delete(machineID);
            return Insert(pLC_Status);
        }
    }
}
