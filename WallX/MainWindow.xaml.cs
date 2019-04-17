using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using WallX.Helpers;
using WallX.Views;
using System.ComponentModel;
using System.IO;

namespace WallX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool _isSystemExited = false;

        #region Pageload

        /// <summary>
        /// Initialize default values
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Height = SystemParameters.PrimaryScreenHeight;
            Width = SystemParameters.PrimaryScreenWidth;
        }

        /// <summary>
        /// Handle main panel from unnecessary moving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManipulationBoundaryEvent(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Application closing event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Application_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            if (_isSystemExited)
                Messenger.Default.Send("Exit Meeting", "Exit Meeting");
        }

        /// <summary>
        /// Load event to initialize necessary fields 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Application_Loaded(object sender, RoutedEventArgs e)
        {
            homepage_control.Content = new HomePageView();

            Messenger.Default.Register<KeyValuePair<string, string>>(this, "Result", ShowResultMessageBox);
            Messenger.Default.Register<string>(this, "Notification", ShowNotificationMessageBox);
            Messenger.Default.Register<string>(this, "Information", ShowInformationMessageBox);

            Messenger.Default.Register<string>(this, "ShowLoader", ShowLoader);
            Messenger.Default.Register<string>(this, "HideLoader", HideLoader);

            Messenger.Default.Register<string>(this, "DesktopMode", GotoDesktopMode);
            Messenger.Default.Register<string>(this, "MinimizeWindow", MinimizeWindow);
        }

        #endregion Pageload

        #region Custom message boxes

        /// <summary>
        /// Display custom result yes or no message box with animation        
        /// </summary>
        /// <param name="result"></param>
        private void ShowResultMessageBox(KeyValuePair<string, string> result)
        {
            YesOrNoBox.ShowResultMessageBox(result, ResultMessageBoxResponse);
        }

        /// <summary>
        /// Display custom notification message box with animation
        /// </summary>
        /// <param name="notification"></param>
        private void ShowNotificationMessageBox(string notifyMsg)
        {
            NotificationBox.ShowNotifyMessageBox(notifyMsg);
        }

        /// <summary>
        /// Display custom information message box with animation
        /// </summary>
        /// <param name="notification"></param>
        private void ShowInformationMessageBox(string infoMsg)
        {
            InformationBox.ShowInfoMessageBox(infoMsg);
        }

        /// <summary>
        /// Result message box yes response
        /// </summary>
        /// <param name="param"></param>
        private void ResultMessageBoxResponse(KeyValuePair<string, string> param)
        {
            try
            {
                if (param.Value == "YesResponse")
                {
                    switch (param.Key)
                    {
                        case "Add Resource":
                            Messenger.Default.Send(true, "Add Resource");
                            break;
                        case "Exit Board":
                            Messenger.Default.Send("ExitBoard", "Exit Board");
                            break;
                        case "Add Canvas":
                            Messenger.Default.Send("Add Canvas", "Add Canvas");
                            break;
                        case "Close Application":
                            System.Windows.Application.Current.Shutdown();
                            break;
                        case "Duplicate Canvas":
                            Messenger.Default.Send("Duplicate Canvas", "Add Canvas");
                            break;
                        case "Delete Canvas":
                            Messenger.Default.Send("Delete Canvas", "Delete Canvas");
                            break;
                        case "Clear Board":
                            Messenger.Default.Send("Clear Board", "Clear Board");
                            break;
                        case "Delete Library Item":
                            Messenger.Default.Send("Delete Library Item", "Delete Library Item");
                            break;
                        case "Delete Audio File":
                            Messenger.Default.Send("DeleteSelectedAudioFile", "DeleteSelectedAudioFileFromDb");
                            break;
                        case "Complete current agenda":
                            Messenger.Default.Send("Yes", "CompleteCurrentAgenda");
                            break;
                        case "DeleteNote":
                            Messenger.Default.Send("DeleteNoteItem", "ConfirmedMainPage");
                            break;
                        case "Remove Board Item":
                            Messenger.Default.Send("RemoveBoardItem", "RemoveBoardItem");
                            break;
                        case "Delete Text Sticky":
                            Messenger.Default.Send("Delete Text Sticky", "Delete Text Sticky");
                            break;
                        case "Reschedule":
                            Messenger.Default.Send("RescheduleMeeting", "RescheduleMeeting");
                            break;
                        case "add_all_libraryitems":
                            Messenger.Default.Send("add_all_libraryitems", "ConfirmedMainPage");
                            break;
                        case "delete_all_libraryitems":
                            Messenger.Default.Send("delete_all_libraryitems", "ConfirmedMainPage");
                            break;
                        case "cancel_this_meeting":
                            Messenger.Default.Send("CancelThisMeeting", "cancel_this_meeting");
                            break;
                        case "cancel_all_meetings":
                            Messenger.Default.Send("CancelAllMeetings", "cancel_all_meetings");
                            break;
                        case "Cancel next meeting":
                            Messenger.Default.Send("CancelNextMeetingBoard", "cancel_next_meeting_board");
                            break;
                        case "reschedule_next_meeting_board":
                            Messenger.Default.Send("RescheduleNextMeetingBoard", "reschedule_next_meeting_board");
                            break;
                        case "Delete Agendas":
                            Messenger.Default.Send("Delete Agendas", "delete_agendas");
                            break;
                        case "Clear MindMapingTool Items":
                            Messenger.Default.Send("Clear MindMap Items", "clear_mindMap_items");
                            break;
                        case "Delete Confirm":
                            Messenger.Default.Send("Delete Confirm", "StartMeeting");
                            break;
                        case "Reschedule Confirm":
                            Messenger.Default.Send("Reschedule Confirm", "StartMeeting");
                            break;
                        case "Delete Agendas Confirm":
                            Messenger.Default.Send("Delete Agendas Confirm", "delete_agendas_confirm");
                            break;
                        case "Remove Selected Item(s)":
                            Messenger.Default.Send("Remove Selected Items", "RemoveSelectedItems");
                            break;
                    }
                }
                else if (param.Value == "NoResponse")
                {
                    switch (param.Key)
                    {
                        case "Add Resource":
                            Messenger.Default.Send(false, "Add Resource");
                            break;
                        case "Continue current agenda":
                            Messenger.Default.Send("Yes", "CompleteCurrentAgenda");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Desktop mode

        DesktopView desktopModeWindow = null;

        /// <summary>
        /// goto desktop mode
        /// </summary>
        /// <param name="param"></param>
        private void GotoDesktopMode(string param)
        {
            try
            {
                Hide();

                if (param.ToLower().StartsWith("browser"))
                {
                    desktopModeWindow = new DesktopView(this, param.ToLower());
                }
                else if (param.Contains("File"))
                {
                    param = Directory.GetFiles(Constants.AttachmentResources).FirstOrDefault(s => Path.GetFileNameWithoutExtension(s) == Path.GetFileNameWithoutExtension(param));

                    desktopModeWindow = new DesktopView(this, param);
                }
                else
                {
                    desktopModeWindow = new DesktopView(this, "");
                }

                if (desktopModeWindow != null)
                {
                    desktopModeWindow.Show();
                }
                else
                {
                    Show();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// get images from desktop mode
        /// </summary>
        /// <param name="libraryImages"></param>
        /// <param name="croppedImagePath"></param>
        /// <param name="cropedImageWidth"></param>
        /// <param name="cropedImageHeight"></param>
        public void DataFromDeskTopWindow(List<string> libraryImages, string croppedImagePath, double cropedImageWidth, double cropedImageHeight)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if ((libraryImages != null && libraryImages.Count > 0) || (croppedImagePath != null && croppedImagePath.ToString().Trim().Length > 0))
                    Messenger.Default.Send(new KeyValuePair<List<string>, string>(libraryImages, croppedImagePath), "GetImageFromDesktop");
                else
                    Messenger.Default.Send(new KeyValuePair<List<string>, string>(null, null), "GetImageFromDesktop");
            });
        }

        #endregion Desktop mode

        private void ShowLoader(string param)
        {
            //MeetingWallLoader.Visibility = Visibility.Visible;
        }

        private void HideLoader(string param)
        {
            //MeetingWallLoader.Visibility = Visibility.Collapsed;
        }

        private void MinimizeWindow(object obj)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
