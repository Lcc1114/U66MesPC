using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Dal
{
    public class SqlHelper

    {
        public DBContext db { get; set; }
        public SqlHelper()
        {
            db = new DBContext();
        }
        /// <summary>
        /// 插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Insert<T>(T entity) where T : class
        {
            db.Set<T>().Add(entity);
            return db.SaveChanges();
        }
        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcwhere"></param>
        /// <returns></returns>
        public IQueryable<T> Query<T>(Expression<Func<T, bool>> funcwhere) where T : class
        {
            return db.Set<T>().Where(funcwhere);
        }
        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int DeleteAtID<T>(int entity) where T : class
        {
            T entities = db.Set<T>().Find(entity);
            db.Set<T>().Remove(entities);
            return db.SaveChanges();
        }
        public int DeleteAtString<T>(string flag) where T : class
        {
            T entities = db.Set<T>().Find(flag);
            db.Set<T>().Remove(entities);
            return db.SaveChanges();
        }

    }
}
