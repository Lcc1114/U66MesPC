using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace U66MesPC.Common
{
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// 要执行的操作
        /// </summary>
        private Action<object> executeActions;
        /// <summary>
        /// 是否可以执行的操作
        /// </summary>
        private Func<object, bool> canExecuteFunc;

        public event EventHandler CanExecuteChanged;

        public RelayCommand() { }
        /// <summary>
        /// 通过执行的委托构造
        /// </summary>
        /// <param name="execute"></param>
        public RelayCommand(Action<object> execute) : this(execute,null)
        {

        }
        /// <summary>
        /// 通过执行的操作与是否可执行的委托
        /// </summary>
        /// <param name="execute">要执行的操作</param>
        /// <param name="canExecute">是否可以被执行</param>
        public RelayCommand(Action<object> execute,Func<object,bool> canExecute) 
        {
            this.executeActions = execute;
            this.canExecuteFunc = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
            {
                return canExecuteFunc(parameter);
            }
            else
                return true;
        }

        public void Execute(object parameter)
        {
            if(executeActions != null)
            {
                executeActions(parameter);
            }
        }
        public void OnCanExecuteChanged()
        {
            if(CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
