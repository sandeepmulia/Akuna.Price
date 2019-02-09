using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Akuna.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //Akuna.UI.MainWindow view = new Akuna.UI.MainWindow();
            //view.DataContext = new Akuna.UI.ViewModel.DisplayMainWindowViewModel();
            //view.Show();

            Akuna.UI.View.DisplayMainWindowGrid view = new Akuna.UI.View.DisplayMainWindowGrid();
            view.DataContext = new Akuna.UI.ViewModel.DisplayMainWindowViewModel();
            view.Show();

        }
    }
}
