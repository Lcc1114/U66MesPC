using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using U66MesPC.Common;
using U66MesPC.Model;
using U66MesPC.View;

namespace U66MesPC
{
  
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Product.Initialize();
                WindowLogin windowLogin = new WindowLogin();
                windowLogin.ShowDialog();
                if (windowLogin.Model.LoginOk)
                {
                    var app = new Application();
                    app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    app.Run(new MainWindow());
                }
                else
                {
                    windowLogin.Close();
                }
            }catch(Exception ex)
            {
                LogsUtil.Instance.WriteException(ex);
            }
            
        }
    }
}
