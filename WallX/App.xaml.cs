using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NextGen.Controls.UI;
using NextGen.Controls.Licence;
using WallX.Helpers;
using WallX.ViewModel;
using NextGen.Controls;
using System.Reflection;
using System.IO;

namespace WallX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Log Exception

        /// <summary>
        /// Constructor
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.FirstChanceException += App_FirstChanceException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// Unknown exceptions handler event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            InsertException(e.Exception);
        }

        /// <summary>
        /// Unhandled exceptions handler event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            InsertException(e.Exception);
        }

        /// <summary>
        /// Log exceptions into files
        /// </summary>
        /// <param name="excep"></param>
        public static void InsertException(Exception excep)
        {
            try
            {
                if (excep.InnerException != null)
                {
                    InsertException(excep.InnerException);
                }
                else
                {
                    List<string> _knownList = new List<string> { "application identity", "a task was canceled", "safe", "unable to read beyond the end of the stream", "bitmapmetadata is not available on bitmapimage", "Could not load file or assembly", "audio data is being streamed too slow.", "the operation completed successfully", "imagesourceconverter cannot convert from (null)." };

                    if (!_knownList.Any(s => excep.Message.ToString().ToLower().Contains(s.ToLower())))
                    {
                        excep.InsertException();
                        //Current.Dispatcher.Invoke(() => { Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification"); });
                    }
                }
            }
            catch (Exception) { Current.Dispatcher.Invoke(() => { Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification"); }); }
        }

        #endregion

        #region Licence with Instances

        public static RegisterLicence _regLicence = null;

        /// <summary>
        /// override startup method
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _regLicence = new RegisterLicence();
            InstanceStatus instanceStatus = new InstanceStatus();
            //instanceStatus.IsSingleInstance(_regLicence, new MainWindow(), Constants.InternalLogoPath, "WallX", "Version 1.0");

            //instanceStatus.IsSingleInstance(new MainWindow(), Constants.InternalLogoPath, "WallX", "Version 1.0");
            _regLicence.IsLicenceValid(new MainWindow(), Constants.InternalLogoPath, "WallX", "Version 1.0");
            //new MainWindow().Show();
        }

        /// <summary>
        /// should update user log out information when you call licence validate in startup
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            if (_regLicence != null)
                _regLicence.UpdateUserLogInfo();

            base.OnExit(e);
        }

        #endregion

        #region Execute Async

        /// <summary>
        /// Execute method async for without loader
        /// </summary>
        /// <param name="task"></param>
        public static async void ExecuteMethod(Action task)
        {
            await Task.Run(task);
        }

        /// <summary>
        /// Execute method async for with loader
        /// </summary>
        /// <param name="task"></param>
        public static async void ExecuteMethod(Action task, bool loaderRequired)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Visible;
            });

            await Task.Run(task);

            App.Current.Dispatcher.Invoke(() =>
            {
                HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Execute method async for loader along with dispatcher
        /// </summary>
        /// <param name="task"></param>
        public static async void ExecuteDispatcherMethod(Action task, bool loaderRequired)
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (loaderRequired)
                        HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Visible;

                    task();

                    if (loaderRequired)
                        HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Collapsed;
                });
            });
        }

        #endregion
    }
}
