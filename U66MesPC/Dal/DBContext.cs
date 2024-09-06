using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Model;

namespace U66MesPC.Dal
{
    public class DBContext : DbContext
    {
        public DBContext() : base("SQLiteConn") { }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new SqliteDropCreateDatabaseWhenModelChanges<DBContext>(modelBuilder));
        }
        public DbSet<User> Users { get; set; }
        public DbSet<SysConfigs> SysConfigs { get; set; }
        public DbSet<CarrierIDBindingNoNumber50> CarrierIDBindingNoNumbers { get; set; }
        public DbSet<CarrierIDBindingSN> CarrierIDBindingSNs { get; set; }
        public DbSet<CarrierIDBindingBot5> CarrierIDBindingBot5s { get; set; }
        public DbSet<CarrierIDBindingBot6> CarrierIDBindingBot6s { get; set; }
        public DbSet<CarrierIDBindingBot4> CarrierIDBindingBot4s { get; set; }
        public DbSet<CarrierIDProductUnload> CarrierIDProductUnloads { get; set; }
        public DbSet<CarrierIDBindingNoNumber140> CarrierIDBindingNoNumber140s { get; set; }
        public DbSet<CarrierIDPressBindingSN> CarrierIDPressBindingSNs { get; set; }
        //public DbSet<ToolingSNandVersionModel> DbToolingSNAndVersion { get; set; }
        //
        public DbSet<PLC_Status> PLC_Status { get; set; }
    }
}
