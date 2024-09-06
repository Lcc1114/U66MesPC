using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using U66MesPC.Dal;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageBoxOptions = System.Windows.Forms.MessageBoxOptions;

namespace U66MesPC.Common
{
    public static class Product
    {
        private static Mutex _mutex;
        public static void Initialize()
        {
            RegisterApplicationExceptionHandler();
            var name = Assembly.GetEntryAssembly().ManifestModule.Name;
            _mutex = new Mutex(true, name, out var flag);
            if (!flag)
            {
                MessageBox.Show($"另一程序【{name}】正在运行，请先关闭另一程序！", "系统提示");
                Environment.Exit(1);
            }
            //在应用程序初始化时一次性触发所有的DbContext进行mapping views的生成操作——调用StorageMappingItemCollection的GenerateViews()方法
            using (var dbcontext = new DBContext())
            {
                var objectContext = ((IObjectContextAdapter)dbcontext).ObjectContext;
                var mappingCollection = (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                mappingCollection.GenerateViews(new List<EdmSchemaError>());
            }
            CleanFile();
            LogsUtil.Instance.InitMesEventNameList();
        }
        public static void CleanFile()
        {
            string path = Application.StartupPath + "\\log";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles("*.txt", SearchOption.AllDirectories);
            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime < DateTime.Now.AddDays(-30))
                {
                    file.Delete();
                }
            }
        }
        /// <summary>
        /// The _b app exception handled.
        /// </summary>
        private static bool _bAppExceptionHandled;

        /// <summary>
        /// The register application exception handler.
        /// </summary>
        public static void RegisterApplicationExceptionHandler()
        {
            if (!_bAppExceptionHandled)
            {
                Application.ThreadException += OnApplicationUIException;
                Application.ApplicationExit += OnApplicationExit;
                AppDomain.CurrentDomain.UnhandledException += OnAppUnhandledException;
                //Task线程内未捕获异常处理事件
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                //UI线程未捕获异常处理事件(UI主线程)
                Dispatcher.CurrentDispatcher.UnhandledException += CurrentDispatcher_UnhandledException;
                _bAppExceptionHandled = true;
            }
        }
        private static void CurrentDispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception as Exception);
            e.Handled = true;
        }
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception as Exception);
            e.SetObserved();
        }
        private static void HandleException(Exception e)
        {
            if (e == null) return;
            //string msg = "程序异常：" + e.Source + "@@" + Environment.NewLine + e.StackTrace + "##" + Environment.NewLine + e.InnerException?.Message ?? e.Message;
            string errorInfo = e.InnerException == null ? e.Message : e.InnerException.Message;
            string msg = "程序异常：" + errorInfo;
            MessageBox.Show(msg, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //Log.Error(e, msg);
            LogsUtil.Instance.WriteError(msg);
        }
        /// <summary>
        /// The on application exit.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public static void OnApplicationExit(object sender, EventArgs e)
        {
            //Log.Info(typeof(Product), "Application exit.");
            LogsUtil.Instance.WriteError("Application exit.");
        }

        /// <summary>
        /// The on app unhandled exception.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public static void OnAppUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                string errorMsg = ex.Message + Environment.NewLine + ex.StackTrace;
                LogsUtil.Instance.WriteError(errorMsg);
                MessageBox.Show(errorMsg, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            catch
            {
                MessageBox.Show("不可恢复的系统异常，应用程序将退出！");
            }
        }

        /// <summary>
        /// The on application ui exception.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public static void OnApplicationUIException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                LogsUtil.Instance.WriteException(e.Exception);
                //Log.Error(typeof(Product), e.Exception.Message, e.Exception);
                MessageBox.Show(e.Exception.ToString());
            }
            catch
            {
                MessageBox.Show("不可恢复的系统异常，应用程序将退出！");
            }
        }
    }
}
