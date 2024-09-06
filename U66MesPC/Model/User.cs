using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Model
{
    [Table("User")]
    public class User
    {
        [Column("ID")]
        public int ID { get; set; }
        [Column("UserName")]
        public string UserName { get; set; }
        [Column("Password")]
        public string Password { get; set; }
        [Column("Role")]
        public Role Role { get; set; }
        public User() { }
        public User(string userName, string pwd, Role role)
        {
            UserName = userName;
            Password = pwd;
            Role = role;
        }
        public override string ToString()
        {
            return $"Id={ID};UserName={UserName};Password={Password};Role={Role};";
        }
    }
    public enum Role
    {
        [Description("操作员")]
        Operator = 0,
        [Description("工程师")]
        Engineer = 1,
        [Description("管理员")]
        Manager = 2,
        [Description("供应商")]
        Vendor = 8
    }
}
