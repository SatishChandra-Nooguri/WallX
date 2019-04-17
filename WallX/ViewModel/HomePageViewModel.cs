using GalaSoft.MvvmLight.Messaging;
using WallX.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WallX.Views;
using System.Windows.Ink;
using System.Windows.Media;
using NextGen.Controls;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Data;
using Excel;
using System.Windows.Forms;
using System.Windows.Data;
using System.Xml;
using NextGen.Controls.InkRecognizer;
using WallX.Helpers;

using FilePath = System.IO;
using Task = System.Threading.Tasks.Task;

namespace WallX.ViewModel
{
    public partial class HomePageViewModel : INotifyPropertyChanged
    {
        #region Variables

        public static HomePageView _homePageView;
        public static DispatcherTimer _timer;
        public static List<Employees> _contactsDbList;
        public static List<Class> _classList;

        private double _minutes = 0;
        private NxgInputType _passcodeInput = NxgInputType.Keyboard;

        public static int _currentClassIndex = 0;
        public static string _navigateView = string.Empty;

        #endregion

        #region Pageload

        /// <summary>
        /// Load event to initialize necessary fields 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HomePageView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _homePageView = sender as HomePageView;

                Messenger.Default.Register<string>(this, "ShowContentControl", ShowContentControl);
                Messenger.Default.Register<string>(this, "CloseContentControlView", CloseContentControl);
                Messenger.Default.Register<string>(this, "StartClass", StartClass);
                Messenger.Default.Register<string>(this, "RescheduleClass", RescheduleClass);

                _homePageView.grid_settings_page.Visibility = Visibility.Collapsed;
                _homePageView.mediaelement_bg.Visibility = Visibility.Collapsed;

                StartTimer();
                PlayBackGroundVideo();
                App.ExecuteMethod(DisplayDateTimeAndAddress);
                App.ExecuteMethod(() => { _contactsDbList = Service.GetModuleDataList<Employees>(null); GetTodayClass(); }, true);
                _homePageView.mediaelement_bg.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #region Property Change event

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #endregion

        #region Properties

        private Uri _weatherVideoSource;
        public Uri WeatherVideoSource
        {
            get { return this._weatherVideoSource; }
            set
            {
                this._weatherVideoSource = value;
                OnPropertyChanged("WeatherVideoSource");
            }
        }

        private string _currentTime;
        public string CurrentTime
        {
            get { return this._currentTime; }
            set
            {
                this._currentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }

        private string _currentDateWithDay;
        public string CurrentDateWithDay
        {
            get { return this._currentDateWithDay; }
            set
            {
                this._currentDateWithDay = value;
                OnPropertyChanged("CurrentDateWithDay");
            }
        }

        private string _countDownTime;
        public string CountDownTime
        {
            get { return this._countDownTime; }
            set
            {
                this._countDownTime = value;
                OnPropertyChanged("CountDownTime");
            }
        }

        private string _displayingClassTime;
        public string DisplayingClassTime
        {
            get { return this._displayingClassTime; }
            set
            {
                this._displayingClassTime = value;
                OnPropertyChanged("DisplayingClassTime");
            }
        }

        private string _classTile;
        public string ClassTile
        {
            get { return this._classTile; }
            set
            {
                this._classTile = value;
                OnPropertyChanged("ClassTile");
            }
        }

        private string _selectedDayTemperature;
        public string SelectedDayTemperature
        {
            get { return this._selectedDayTemperature; }
            set
            {
                this._selectedDayTemperature = value;
                OnPropertyChanged("SelectedDayTemperature");
            }
        }

        private string _classPassword;
        public string ClassPassword
        {
            get { return _classPassword; }
            set
            {
                _classPassword = value;
                OnPropertyChanged("ClassPassword");
            }
        }

        private object _ccNewClass;
        public object CCNewClass
        {
            get { return _ccNewClass; }
            set
            {
                _ccNewClass = value;
                OnPropertyChanged("CCNewClass");
            }
        }

        private object _ccCalendar;
        public object CCCalendar
        {
            get { return _ccCalendar; }
            set
            {
                _ccCalendar = value;
                OnPropertyChanged("CCCalendar");
            }
        }

        private object _ccBoard;
        public object CCBoard
        {
            get { return _ccBoard; }
            set
            {
                _ccBoard = value;
                OnPropertyChanged("CCBoard");
            }
        }

        private string _locationName;
        public string LocationName
        {
            get { return _locationName; }
            set
            {
                _locationName = value;
                OnPropertyChanged("LocationName");
            }
        }

        private string _cityName;
        public string CityName
        {
            get { return _cityName; }
            set
            {
                _cityName = value;
                OnPropertyChanged("CityName");
            }
        }

        private string _countryName;
        public string CountryName
        {
            get { return _countryName; }
            set
            {
                _countryName = value;
                OnPropertyChanged("CountryName");
            }
        }

        private string _selectedResourceDirectory;
        public string SelectedResourceDirectory
        {
            get { return _selectedResourceDirectory; }
            set
            {
                _selectedResourceDirectory = value;
                OnPropertyChanged("SelectedResourceDirectory");
            }
        }

        #endregion

        #region Timer for updating time

        /// <summary>
        /// timer for app
        /// </summary>
        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Start();
        }

        /// <summary>
        /// timer tick for update datetime & class details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, object e)
        {
            try
            {
                CurrentTime = DateTime.Now.ToString("hh:mm tt");

                if (_classList != null && _classList.Count > 0 && _currentClassIndex >= 0 && _currentClassIndex < _classList.Count)
                {
                    if (!string.IsNullOrWhiteSpace(DisplayingClassTime) && _currentClassIndex != _classList.Count && (_classList[_currentClassIndex].EndTime - DateTime.Now).TotalMinutes >= 0)
                    {
                        CountDownTime = (_classList[_currentClassIndex].StartTime - DateTime.Now).ToString("hh\\:mm");
                    }
                    else if (_currentClassIndex != _classList.Count && !string.IsNullOrWhiteSpace(DisplayingClassTime) && (DateTime.Now - _classList[_currentClassIndex].StartTime).TotalMinutes < int.Parse(Regex.Match(_classList[_currentClassIndex].Duration, @"\d+").Value))
                    {
                        NxgUtilities.CollapseElements(new List<FrameworkElement> { _homePageView.canv_home_countdown, _homePageView.canv_home_play, _homePageView.txt_add_home_newclass, _homePageView.canv_home_visible });
                        _homePageView.canv_home_play.Visibility = Visibility.Visible;
                    }
                    else if (_currentClassIndex != _classList.Count && !string.IsNullOrWhiteSpace(DisplayingClassTime) && (_classList[_currentClassIndex].EndTime - DateTime.Now).TotalMinutes < 0)
                    {
                        NxgUtilities.CollapseElements(new List<FrameworkElement> { _homePageView.canv_home_countdown, _homePageView.canv_home_play, _homePageView.txt_add_home_newclass });
                        _homePageView.canv_home_visible.Visibility = Visibility.Visible;
                    }
                }

                if (CCBoard != null)
                    Messenger.Default.Send(DateTime.Now, "Timer");

                if (CCCalendar != null)
                    Messenger.Default.Send(DateTime.Now, "CarouselTimer");

                if (CCNewClass != null)
                    Messenger.Default.Send(DateTime.Now, "AgendaTimer");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region DateTime and weather display

        /// <summary>
        /// Display date, time, city & country
        /// </summary>
        private void DisplayDateTimeAndAddress()
        {
            try
            {
                DateTime currentDateTime = DateTime.Now;
                CurrentTime = currentDateTime.ToString("hh:mm tt");
                CurrentDateWithDay = currentDateTime.DayOfWeek + " " + NxgUtilities.GetDateExtension(currentDateTime.Date.Day.ToString()) + " " + currentDateTime.ToString("MMMM") + " " + currentDateTime.Year + "\n" + Constants.CityName + ", " + Constants.CountryName;

                LocationName = Constants.LocationName;
                CityName = Constants.CityName;
                CountryName = Constants.CountryName;

                NxgUtilities.CreateDirectory(Constants.ProjectResources);
                NxgUtilities.CreateDirectory(Constants.AttachmentResources);
                NxgUtilities.CreateDirectory(Constants.AttachmentResourceThumbs);

                SelectedResourceDirectory = Constants.ProjectResources;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Play home screen video based on weather
        /// </summary>
        /// <param name="weatherConditionMode"></param>
        private void PlayBackGroundVideo()
        {
            try
            {
                string videoPath = Constants.ProjectResources + "File_61_9a0ad75f-aeef-411e-87df-6a035ac8a216.mp4";
                if (!FilePath.File.Exists(videoPath))
                {
                    App._regLicence.DownloadFile("Attachments", "Nature.mp4__@__File_61_9a0ad75f-aeef-411e-87df-6a035ac8a216.mp4", Constants.ProjectResources);
                }

                if (!FilePath.File.Exists(Constants.SampleContactsExcel))
                {
                    App._regLicence.DownloadFile("Attachments", "sample_contacts_excel.xls__@__File_62_347980e7-686a-4073-9bfe-356845fdde18.xls", Constants.ProjectResources);
                }

                if (FilePath.File.Exists(videoPath))
                {
                    WeatherVideoSource = new Uri(videoPath);

                    if (!string.IsNullOrWhiteSpace(videoPath) && FilePath.File.Exists(videoPath))
                    {
                        _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
                        _homePageView.mediaelement_bg.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Play weather video repedly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WeatherVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            _homePageView.mediaelement_bg.Position = TimeSpan.FromSeconds(0);
            _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
            _homePageView.mediaelement_bg.Play();
        }

        #endregion

        #region Class binding in Home

        /// <summary>
        /// Get today class & bind to homescreen
        /// </summary>
        /// <param name="param"></param>
        private void GetTodayClass(bool isFromRetrieve = false)
        {
            try
            {
                DateTime currentTime = DateTime.Now;

                _classList = Service.GetClassList(currentTime);

                if (_classList != null && _classList.Count > 0)
                {
                    int currentIndex = _classList.IndexOf(_classList.FirstOrDefault(s => s.EndTime > currentTime));
                    _currentClassIndex = currentIndex != -1 ? currentIndex - 1 : _classList.Count - 1;
                }

                if (!isFromRetrieve)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ChangeDisplayClassinHome("canv_next");
                    });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// change display class in homescreen method
        /// </summary>
        /// <param name="param"></param>
        private void ChangeDisplayClassinHome(string param)
        {
            _currentClassIndex = param == "canv_next" ? ++_currentClassIndex : --_currentClassIndex;

            if (_currentClassIndex >= 0)
            {
                NxgUtilities.CollapseElements(new List<FrameworkElement> { _homePageView.txt_add_home_newclass, _homePageView.canv_home_play, _homePageView.canv_home_visible, _homePageView.canv_more, _homePageView.canv_home_countdown });

                if (_classList != null && _currentClassIndex != -1 && _currentClassIndex < _classList.Count)
                {
                    ClassTile = _classList[_currentClassIndex].ClassName;
                    DisplayingClassTime = _classList[_currentClassIndex].StartTime.ToString("hh:mm tt") + " - " + _classList[_currentClassIndex].EndTime.ToString("hh:mm tt");

                    _homePageView.txt_home_canv_class_time.Visibility = Visibility.Visible;
                    _homePageView.canv_prev.Visibility = _currentClassIndex > 0 && _classList.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    _homePageView.canv_next.Visibility = _currentClassIndex <= _classList.Count ? Visibility.Visible : Visibility.Collapsed;

                    if ((_classList[_currentClassIndex].EndTime - DateTime.Now).TotalMinutes > -1 && _classList[_currentClassIndex].ActualDuration == "00:00:00")
                    {
                        CountDownTime = (_classList[_currentClassIndex].StartTime - DateTime.Now).ToString("hh\\:mm");
                        _homePageView.canv_home_play.Visibility = Visibility.Visible;
                        _homePageView.canv_home_countdown.Visibility = Visibility.Visible;
                    }
                    else if ((_classList[_currentClassIndex].EndTime - DateTime.Now).TotalMinutes > 0)
                    {
                        _homePageView.canv_home_countdown.Visibility = Visibility.Collapsed;
                        _homePageView.canv_home_play.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CountDownTime = "00:00";
                        _homePageView.canv_home_visible.Visibility = Visibility.Visible;
                        _homePageView.canv_home_countdown.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    AddNewClass();

                    if (_classList != null)
                        _homePageView.canv_prev.Visibility = _classList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    else
                        _currentClassIndex = 0;
                }
            }
        }

        /// <summary>
        /// add new class if class not available or no class after this time
        /// </summary>
        private void AddNewClass()
        {
            ClassTile = "No Classes scheduled today \n Schedule a Class";

            NxgUtilities.CollapseElements(new List<FrameworkElement> {_homePageView.txt_home_canv_class_time,
            _homePageView.canv_home_countdown, _homePageView.canv_more, _homePageView.canv_home_play,
            _homePageView.canv_home_visible, _homePageView.canv_prev, _homePageView.canv_next });

            if (_classList != null && _classList.Count > 0)
            {
                _homePageView.canv_prev.Visibility = Visibility.Visible;
                ClassTile = "No further Classes scheduled today \n Schedule a Class";
            }

            _homePageView.txt_add_home_newclass.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// change display class in homescreen event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ChangeDisplayClassinHome_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeDisplayClassinHome((sender as Canvas).Name);
        }

        #endregion

        #region Events

        /// <summary>
        /// Close application event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void logo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowSettingsPage();
        }

        /// <summary>
        /// Display content control view event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowContentControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ShowContentControl((sender as FrameworkElement).Name);
        }

        /// <summary>
        /// Display content control based param method
        /// </summary>
        /// <param name="param"></param>
        private void ShowContentControl(string param)
        {
            try
            {
                switch (param.ToLower())
                {
                    case "txt_add_home_newclass":
                        CCNewClass = new NewClassView();
                        _homePageView.cc_calendar.Visibility = Visibility.Collapsed;
                        _homePageView.cc_new_class.Visibility = Visibility.Visible;
                        if (!NewClassViewModel._isFromCarousel)
                        {
                            DateTime currentTime = DateTime.Now;
                            currentTime = currentTime.AddMinutes((int)Math.Round((double)currentTime.Minute / 5) * 5 - currentTime.Minute);
                            NewClassViewModel._classStartDateTime = currentTime;
                            NewClassViewModel._classEndDateTime = currentTime.AddMinutes(30);
                        }
                        break;
                    case "canv_show_calendar":
                        CarouselViewModel._classOverviewList = _classList;
                        if (!NewClassViewModel._isFromCarousel)
                            CarouselViewModel._selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                        CCCalendar = new CarouselView();
                        _homePageView.cc_calendar.Visibility = Visibility.Visible;
                        NewClassViewModel._isFromCarousel = false;
                        break;
                    case "canv_show_board":
                        _homePageView.cc_board.Visibility = Visibility.Visible;
                        CreateAdhocClass();
                        break;
                    default:
                        CCCalendar = new CarouselView();
                        _homePageView.cc_calendar.Visibility = Visibility.Visible;
                        CarouselViewModel._classOverviewList = null;
                        CarouselViewModel._selectedDate = param.Split(',').First();
                        CarouselViewModel._selectedClassId = Convert.ToInt32(param.Split(',').Last());
                        break;
                }
                _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
                _homePageView.mediaelement_bg.Stop();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Close content control view based on passing parameter
        /// </summary>
        /// <param name="param"></param>
        private void CloseContentControl(string param)
        {
            try
            {
                switch (param.ToLower())
                {
                    case "newclass":
                        _homePageView.cc_new_class.Visibility = Visibility.Collapsed;

                        if (!NewClassViewModel._isFromCarousel)
                            GoToHomeScreen();
                        else
                        {
                            NewClassViewModel._isFromCarousel = false;
                            CarouselViewModel._classOverviewList = _classList;
                            CarouselViewModel._selectedDate = NewClassViewModel._classStartDateTime.ToString("yyyy-MM-dd");
                            CCCalendar = new CarouselView();
                            _homePageView.cc_calendar.Visibility = Visibility.Visible;
                        }
                        break;
                    case "calendar":
                        _homePageView.cc_new_class.Visibility = Visibility.Collapsed;
                        _homePageView.cc_calendar.Visibility = Visibility.Collapsed;
                        GoToHomeScreen();
                        break;
                    case "board":
                        _homePageView.cc_board.Visibility = Visibility.Collapsed;
                        if (_homePageView.cc_calendar.IsVisible)
                            CCCalendar = new CarouselView();
                        else
                            GoToHomeScreen();
                        break;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Create adhoc class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CreateAdhocClass()
        {
            try
            {
                _homePageView.WallXLoader.Visibility = Visibility.Visible;
                Class dataObject = null;
                Task task = Task.Run(() =>
                {
                    DateTime date = DateTime.Now;

                    Class overView = new Class
                    {
                        ClassName = "Untitled_" + date.ToString("hh:mm tt"),
                        StartTime = date,
                        EndTime = date,
                        ClassCategoryId = (int)ClassCategoryType.Mathematics,
                        ClassCategory = ClassCategoryType.Mathematics.ToString(),
                        ClassTypeId = (int)ClassScheduleType.OneTimeClass,
                        ClassType = ClassScheduleType.OneTimeClass.ToString(),
                        Password = "",
                        IsFromAdhoc = true,
                        UniqueClassId = "NXG" + NxgUtilities.GetRandomPassword(6)
                    };

                    int generatedClassId = Service.InsertOrUpdateDataToDB(overView, CrudActions.Create);
                    overView.ClassId = generatedClassId;
                    dataObject = Service.GetClassData(overView);
                });

                await task;

                if (dataObject != null)
                {
                    MainWindow._isSystemExited = true;
                    CCBoard = new BoardView();
                    BoardViewModel._currentClass = dataObject;
                }
                _homePageView.WallXLoader.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Passcode Validations

        /// <summary>
        /// Opening password box for display class event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StartClass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _navigateView = "ClassOpenEventInCalender";
            StartClass();
        }

        /// <summary>
        /// Opening password box for display class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartClass(string param = "")
        {
            try
            {
                if (_classList != null && _classList.Count > 0 && _navigateView == "ClassOpenEventInCalender")
                {
                    _minutes = DateTime.Now.Subtract(_classList[_currentClassIndex].StartTime).TotalMinutes;
                    if (_minutes < -5 && _classList[_currentClassIndex].ActualDuration == "00:00:00")
                    {
                        Messenger.Default.Send("This Class can't be started now. Please try 5 mins. before the scheduled time.", "Notification");
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(_classList[_currentClassIndex].Password))
                {
                    _homePageView.canv_passcode.Visibility = Visibility.Visible;
                    _homePageView.pwb_passcode.Visibility = Visibility.Visible;
                    ClearPasscode();
                    if (_passcodeInput == NxgInputType.Keyboard)
                    {
                        Keyboard.Focus(_homePageView.pwb_passcode);
                        NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
                    }
                    else if (_passcodeInput == NxgInputType.Hand)
                    {
                        Keyboard.ClearFocus();
                    }
                }
                else
                {
                    if (_classList != null && _classList.Count > 0 && _navigateView == "ClassOpenEventInCalender" && string.IsNullOrWhiteSpace(_classList[_currentClassIndex].Password))
                    {
                        OpenExistingClass();
                    }
                    else if(_navigateView == "ClassDeleteEventInCalender" && param == "Delete")
                    {
                        Messenger.Default.Send(new KeyValuePair<string, string>("Delete Confirm", "Are you sure want to delete the Class?"), "Result");
                    }
                    else if (_navigateView == "ClassResheduleEventInCalender" && param == "Reshedule")
                    {
                        Messenger.Default.Send(new KeyValuePair<string, string>("Reschedule Confirm", "Are you sure want to reschedule the Class?"), "Result");
                    }
                    else if (_navigateView == "ClassDeleteEventInCalender" && param.ToLower() == "delete confirm")
                    {
                        _navigateView = string.Empty;
                        Messenger.Default.Send("DeletedClass", "DeletedClass");
                        _homePageView.canv_passcode.Visibility = Visibility.Collapsed;
                        return;
                    }
                    else if (_navigateView == "ClassResheduleEventInCalender" && param.ToLower() == "reschedule confirm")
                    {
                        _navigateView = string.Empty;

                        Class dataObject = Service.GetClassData(_classList[_currentClassIndex]);
                        NewClassViewModel._classDetails = dataObject;
                        ShowContentControl("txt_add_home_newclass");

                        _homePageView.canv_passcode.Visibility = Visibility.Collapsed;

                        return;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(_classList[_currentClassIndex].ActualDuration) && Convert.ToInt32(TimeSpan.Parse(_classList[_currentClassIndex].ActualDuration).TotalMinutes) > 0)
                        {
                            int meetId = _classList[_currentClassIndex].ClassId;
                            _classList[_currentClassIndex].ClassTypeId = 5;
                            _classList[_currentClassIndex].ClassType = "Single Day Multiple Classes";
                            string recId = Guid.NewGuid().ToString();
                            _classList[_currentClassIndex].RecurringClassId = recId;
                            
                            Service.InsertOrUpdateDataToDB(_classList[_currentClassIndex], CrudActions.Update);

                            _classList[_currentClassIndex].FrequencyStartTime = DateTime.Now;
                            _classList[_currentClassIndex].FrequencyEndTime = DateTime.Now.AddMinutes(30);

                            int classDbId = Service.InsertOrUpdateDataToDB(_classList[_currentClassIndex], CrudActions.Create);

                            Class classItem = Service.GetClassData(_classList[_currentClassIndex]);
                            foreach (Participants participant in classItem.ParticipantList)
                            {
                                participant.ClassId = classDbId;
                                Service.InsertOrUpdateDataToDB(participant, CrudActions.Create);
                            }

                            _classList[_currentClassIndex].ClassId = classDbId;
                        }

                        ValidatePasswordandOpenBoard(null);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Opening the class to reschedule
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RescheduleClass(string param = "")
        {
            try
            {
                Class dataObject = Service.GetClassData(_classList[_currentClassIndex]);
                NewClassViewModel._classDetails = dataObject;
                ShowContentControl("txt_add_home_newclass");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear passcode value
        /// </summary>
        private void ClearPasscode()
        {
            ClassPassword = "";
            _homePageView.pwb_passcode.Password = "";
            _homePageView.inkcanv_passcode.Strokes.Clear();
        }

        /// <summary>
        /// closing passcode option 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClosePasscode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.canv_passcode.Visibility = Visibility.Collapsed;
                _navigateView = string.Empty;
                ClearPasscode();

                if (CCCalendar != null)
                    ((CCCalendar as CarouselView).DataContext as CarouselViewModel).rescheduleOrDeleteItem = null;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// clear passcode
        /// </summary>
        /// <param name="sender"></param>
        public void ClearPasscode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ClearPasscode();
        }

        /// <summary>
        /// To select either Hand or keyboard for entering passcode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PasscodeInputOptions_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.canv_passcode_inputoptions.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Image).Visibility = Visibility.Collapsed);
                _passcodeInput = (sender as Canvas).Name == "canv_passcode_keyboard" ? NxgInputType.Keyboard : NxgInputType.Hand;

                (sender as Canvas).Children[0].Visibility = Visibility.Visible;
                switch (_passcodeInput)
                {
                    case NxgInputType.Keyboard:
                        if (_homePageView.inkcanv_passcode.Strokes != null && _homePageView.inkcanv_passcode.Strokes.Count > 0)
                        {
                            _homePageView.pwb_passcode.Password = ClassPassword = RecognizeStrokes.RecognizeText(_homePageView.inkcanv_passcode, null);
                        }

                        _homePageView.inkcanv_passcode.Visibility = Visibility.Collapsed;
                        _homePageView.pwb_passcode.Visibility = Visibility.Visible;
                        _homePageView.inkcanv_passcode.Strokes.Clear();

                        Keyboard.Focus(_homePageView.pwb_passcode);
                        NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
                        break;
                    case NxgInputType.Hand:
                        PasscodeDefaultInkSettings(_homePageView.inkcanv_passcode, "inkcanv_passcode", Colors.Black, new Size(5, 5));
                        _homePageView.inkcanv_passcode.Visibility = Visibility.Visible;
                        _homePageView.pwb_passcode.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// set default ink settings to passcode
        /// </summary>
        /// <param name="inkCanvas"></param>
        /// <param name="inkcanvasName"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        private void PasscodeDefaultInkSettings(InkCanvas inkCanvas, string inkcanvasName, Color color, Size size)
        {
            DrawingAttributes drawingAttributes = new DrawingAttributes();
            drawingAttributes.Color = color;
            drawingAttributes.Height = size.Height;
            drawingAttributes.Width = size.Width;
            drawingAttributes.IsHighlighter = false;
            //drawingAttributes.IgnorePressure = false;
            //drawingAttributes.FitToCurve = true;
            inkCanvas.DefaultDrawingAttributes = drawingAttributes;
        }

        /// <summary>
        /// validate password and navigate to navigateView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenClass_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ClassPassword = _homePageView.pwb_passcode.Password;
                ValidatePasswordandOpenBoard(ClassPassword);
            }
        }

        /// <summary>
        /// validate password and navigate to navigateView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenClass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_passcodeInput == NxgInputType.Keyboard)
                {
                    ClassPassword = _homePageView.pwb_passcode.Password;
                    ValidatePasswordandOpenBoard(ClassPassword);
                }
                else if (_passcodeInput == NxgInputType.Hand && _homePageView.inkcanv_passcode.Strokes != null && _homePageView.inkcanv_passcode.Strokes.Count > 0)
                {
                    ClassPassword = RecognizeStrokes.RecognizeText(_homePageView.inkcanv_passcode, null);
                    ValidatePasswordandOpenBoard(ClassPassword);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// validate password and navigate to navigateView
        /// </summary>
        /// <param name="password"></param>
        private void ValidatePasswordandOpenBoard(string password)
        {
            try
            {
                if (_currentClassIndex <= -1)
                    _currentClassIndex = 0;
                if (_classList != null && _classList.Count > 0 && (password == _classList[_currentClassIndex].Password) || (password == "123456" && System.Diagnostics.Debugger.IsAttached))
                {
                    if (_navigateView == "ClassDeleteEventInCalender")
                    {
                        _navigateView = string.Empty;
                        Messenger.Default.Send("DeletedClass", "DeletedClass");
                        _homePageView.canv_passcode.Visibility = Visibility.Collapsed;
                        return;
                    }
                    else if (_navigateView == "ClassResheduleEventInCalender")
                    {
                        _navigateView = string.Empty;

                        Class dataObject = Service.GetClassData(_classList[_currentClassIndex]);
                        NewClassViewModel._classDetails = dataObject;
                        ShowContentControl("txt_add_home_newclass");

                        _homePageView.canv_passcode.Visibility = Visibility.Collapsed;

                        return;
                    }
                    else if (_navigateView == "ClassOpenEventInCalender")
                    {
                        OpenExistingClass();
                    }
                }
                else
                {
                    Messenger.Default.Send("The passcode you have entered is invalid", "Notification");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void OpenExistingClass()
        {
            _navigateView = string.Empty;
            ClassPassword = "";

            //if (_minutes > TimeSpan.Parse(_meetingsList[_currentMeetingIndex].Duration).TotalMinutes && _meetingsList[_currentMeetingIndex].ActualDuration == "00:00:00")
            //{
            //    Messenger.Default.Send(new KeyValuePair<string, string>("Reschedule", "The meeting time is over. \n Do you want to reschedule it to another date or time?"), "Result");
            //    return;
            //} //

            Class dataObject = Service.GetClassData(_classList[_currentClassIndex]);
            if (dataObject != null)
            {
                _homePageView.canv_passcode.Visibility = Visibility.Collapsed;
                _homePageView.cc_board.Visibility = Visibility.Visible;

                BoardViewModel._actualClass = null;
                BoardViewModel._currentClass = dataObject;
                CCBoard = new BoardView();
                ClearPasscode();
                MainWindow._isSystemExited = true;

                _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
                _homePageView.mediaelement_bg.Stop();
            }
            else
            {
                Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification");
            }
        }

        /// <summary>
        /// Get today class & play weather video once content view existed
        /// </summary>
        /// <param name="param"></param>
        private void GoToHomeScreen(string param = "")
        {
            try
            {
                _homePageView.canv_home.Visibility = Visibility.Visible;
                _homePageView.mediaelement_bg.Position = TimeSpan.FromSeconds(0);
                _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
                _homePageView.mediaelement_bg.Play();

                MainWindow._isSystemExited = false;
                _navigateView = string.Empty;

                App.ExecuteMethod(() => GetTodayClass());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// resend available password using class id
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResendPasscode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.ExecuteMethod(() =>
                {
                    Class dataObject = Service.GetClassData(_classList[_currentClassIndex]);
                    Class overviewInfo = _classList[_currentClassIndex];

                    if (!string.IsNullOrWhiteSpace(overviewInfo.OrganizerMailId))
                    {
                        string subject = overviewInfo.ClassName + " " + overviewInfo.StartTime.ToString("dd MMM hh:mm tt");
                        string body = "Your Class passcode is <b>" + overviewInfo.Password + "</b><br/><br/>";

                        if (EMailer.SendEmailToClient(Constants.UserName, Constants.Password, overviewInfo.OrganizerMailId, subject, body, null))
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                Messenger.Default.Send("Passcode has been sent to your E-mail", "Notification");
                            });
                        }
                    }
                    else
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Messenger.Default.Send(" ", "Notification");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Settings page

        private void ShowHomePage()
        {
            _homePageView.grid_settings_page.Visibility = Visibility.Collapsed;

            _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
            _homePageView.mediaelement_bg.Play();
            _homePageView.canv_home.Effect = null;
        }

        private void ShowSettingsPage()
        {
            _homePageView.grid_settings_page.Visibility = Visibility.Visible;

            _homePageView.canv_home.Effect = new BlurEffect() { Radius = 15 };
            _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
            _homePageView.mediaelement_bg.Pause();

            HideAllSettingsPages();

            List<Grid> settingsOptions = _homePageView.sp_setting_options_menu.Children.OfType<Grid>().ToList();
            settingsOptions.ForEach(s => { s.Visibility = Visibility.Collapsed; });
            settingsOptions.Last().Visibility = Visibility.Visible;
            settingsOptions.Last().Opacity = 1;
            ((settingsOptions.Last().Children[0] as Grid).Children[0] as Ellipse).StrokeThickness = 5;

            _homePageView.canv_settings_login.Visibility = Visibility.Visible;
            _homePageView.txt_settings_userid.Text = "";
            _homePageView.pwb_settings_pwd.Password = "";

            _homePageView.canv_contacts_upload.Visibility = Visibility.Visible;
            _homePageView.sp_contacts_list.Visibility = Visibility.Collapsed;

            _homePageView.canv_update_unavailable.Visibility = Visibility.Visible;
            _homePageView.canv_update_available.Visibility = Visibility.Collapsed;

            _homePageView.sp_contacts_options.Visibility = Visibility.Visible;
            _homePageView.grid_contact_creation.Visibility = Visibility.Collapsed;
        }

        private void HideAllSettingsPages()
        {
            _homePageView.canv_settings_login.Visibility = Visibility.Collapsed;
            _homePageView.canv_version_update.Visibility = Visibility.Collapsed;
            _homePageView.grid_import_contacts.Visibility = Visibility.Collapsed;
            _homePageView.grid_licence_upgrade.Visibility = Visibility.Collapsed;
            _homePageView.grid_location_details.Visibility = Visibility.Collapsed;
        }

        private void SelectSettingsOption(Grid selectedOption)
        {
            if (selectedOption.Name != "grid_close_option")
            {
                _homePageView.sp_setting_options_menu.Children.OfType<Grid>().ToList().ForEach(s => { s.Opacity = 0.5; ((s.Children[0] as Grid).Children[0] as Ellipse).StrokeThickness = 0; }); HideAllSettingsPages();
                selectedOption.Opacity = 1;
                ((selectedOption.Children[0] as Grid).Children[0] as Ellipse).StrokeThickness = 5;
            }
            switch (selectedOption.Name)
            {
                case "grid_version_update_option":
                    _homePageView.canv_version_update.Visibility = Visibility.Visible;
                    _homePageView.canv_update_available.Visibility = _homePageView.canv_update_unavailable.Visibility = Visibility.Collapsed;
                    _homePageView.tbk_from_version.Text = "V" + Constants.AppVersion;
                    double updateInfo = App._regLicence.ValidateVersion("ClassWall", Constants.AppVersion);
                    if (updateInfo > Constants.AppVersion)
                    {
                        _homePageView.canv_update_available.Visibility = Visibility.Visible;
                        _homePageView.tbk_to_version.Text = "V" + updateInfo;
                    }
                    else
                        _homePageView.canv_update_unavailable.Visibility = Visibility.Visible;
                    break;
                case "grid_import_option":
                    _homePageView.grid_import_contacts.Visibility = Visibility.Visible;
                    break;
                case "grid_licence_upgrade_option":
                    _homePageView.grid_licence_upgrade.Visibility = Visibility.Visible;
                    break;
                case "grid_logout_option":
                    ShowHomePage();
                    break;
                case "grid_close_option":
                    Messenger.Default.Send(new KeyValuePair<string, string>("Close Application", "Closing the application "), "Result");
                    break;
                case "grid_location_details_option":
                    _homePageView.grid_location_details.Visibility = Visibility.Visible;
                    break;
            }
        }

        public void grid_settings_options_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SelectSettingsOption(sender as Grid);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_update_version_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App._regLicence.UpdateVersion();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #region Login page

        public void pwb_settings_pwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                _homePageView.tbk_settings_pwd.Visibility = !string.IsNullOrWhiteSpace(_homePageView.pwb_settings_pwd.Password) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_settings_close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.grid_settings_page.Visibility = Visibility.Collapsed;

                _homePageView.mediaelement_bg.LoadedBehavior = MediaState.Manual;
                _homePageView.mediaelement_bg.Play();
                _homePageView.canv_home.Effect = null;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_settings_login_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string userName = _homePageView.txt_settings_userid.Text;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    Messenger.Default.Send("Please enter User name..!", "Notification");
                    return;
                }

                string userPassword = _homePageView.pwb_settings_pwd.Password;
                if (string.IsNullOrWhiteSpace(userPassword))
                {
                    Messenger.Default.Send("Please enter Password..!", "Notification");
                    return;
                }
                int password;
                int.TryParse(userPassword, out password);

                if (App._regLicence.IsLoginValid(userName, password))
                {
                    _homePageView.canv_settings_login.Visibility = Visibility.Collapsed;

                    List<Grid> settingsOptions = _homePageView.sp_setting_options_menu.Children.OfType<Grid>().ToList();
                    settingsOptions.ForEach(s => { s.Visibility = Visibility.Visible; });

                    SelectSettingsOption(_homePageView.grid_location_details_option);
                }
                else
                    Messenger.Default.Send("Credentials are not matched. Please try again..!", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Licence registration passcode validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void password_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = NxgUtilities.IsDigitAllowedText(e.Text);
        }

        public void grid_forgot_password_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (App._regLicence.IsLoginValid("Resend Credentials", 0))
                    Messenger.Default.Send("User credentials are sent to your registered email id.", "Notification");
                else
                    Messenger.Default.Send("Something went wrong. Please contact at application support..!", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Login page

        #region Import contacts

        private List<KeyValuePair<string, string>> ReadDataExcelFile(string filePath)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            try
            {
                DataTable addresstable = ReadDataFromExcel(filePath);
                if (addresstable != null && addresstable.Rows != null)
                {
                    foreach (DataRow dataRow in addresstable.Rows)
                    {
                        var values = dataRow.ItemArray;
                        if (values.Length > 1)
                            result.Add(new KeyValuePair<string, string>(values[0].ToString(), values[1].ToString()));
                        else if (values.Length > 0)
                            result.Add(new KeyValuePair<string, string>(null, values[0].ToString()));
                    }
                }
                else
                {
                    Messenger.Default.Send("File Not supported, please select *.xls files only", "Notification");
                }
                return result;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
                return result;
            }
        }

        public DataTable ReadDataFromExcel(string fileName)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fileName) && !IsFileOpen(fileName))
                {
                    string fileType = (System.IO.Path.GetExtension(fileName).ToLower());
                    FilePath.FileStream stream = FilePath.File.Open(fileName, FilePath.FileMode.Open, FilePath.FileAccess.Read);
                    IExcelDataReader excelReader = null;
                    if (fileType == ".xls")
                        excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                    else if (fileType == ".xlsx")
                        excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    if (excelReader != null)
                    {
                        excelReader.IsFirstRowAsColumnNames = true;
                        DataSet result = null;
                        result = excelReader.AsDataSet();
                        if (result != null && result.Tables[0].Rows.Count > 0)
                        {
                            long rows = result.Tables[excelReader.Name].Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is System.DBNull)).LongCount();
                            if ((int)rows != 0)
                            {
                                DataTable excelDT = result.Tables[excelReader.Name];
                                excelDT = excelDT.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is System.DBNull || string.Compare((field.ToString()).Trim(), string.Empty) == 0)).CopyToDataTable();
                                return excelDT;
                            }
                        }
                        excelReader.Close();
                        return null;
                    }
                    stream.Close();
                    stream.Dispose();
                }
                return null;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
                return null;
            }
        }

        bool IsFileOpen(string fileName)
        {
            try
            {
                System.IO.FileStream stream = System.IO.File.Open(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                stream.Close();
                stream.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Messenger.Default.Send("The Excel file can't open because it is open in another application", "Notification");
                App.InsertException(ex);
                return true;
            }
        }

        public void grid_upload_excel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Files (*.xls;)|*.XLS;)";
                List<KeyValuePair<string, string>> contacts = new List<KeyValuePair<string, string>>();
                if (openFileDialog.ShowDialog() == true)
                {
                    string[] selectedFiles = openFileDialog.FileNames;
                    foreach (string file in selectedFiles)
                    {
                        if ((System.IO.Path.GetExtension(file).ToLower() == ".xlsx") || (System.IO.Path.GetExtension(file).ToLower() == ".xls"))
                            ReadDataExcelFile(file).Where(s => NxgUtilities.IsValidEmail(s.Value)).ToList().ForEach(k => contacts.Add(k));
                    }

                    int addedEmployeesCount = 0;
                    foreach (var contact in contacts)
                    {
                        if (NxgUtilities.IsValidEmail(contact.Value) && !HomePageViewModel._contactsDbList.Any(s => s.Email.Trim().ToLower() == contact.Value))
                        {
                            Employees emp = new Employees { Email = contact.Value, FirstName = (string.IsNullOrEmpty(contact.Key) ? contact.Value.Substring(0, contact.Value.IndexOf("@")) : contact.Key) };
                            int id = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                            if (id > -1)
                            {
                                emp.EmployeeId = id;
                                _contactsDbList.Add(emp);
                                addedEmployeesCount++;
                            }
                        }
                    }
                    if (addedEmployeesCount > 0)
                    {
                        Messenger.Default.Send(addedEmployeesCount + " Contacts added successfully..!", "Notification");
                        //_newMeetingView.lb_alphabets.SelectedIndex = 0;
                    }
                    else
                    {
                        Messenger.Default.Send("No contacts have added..!", "Notification");
                    }

                    _homePageView.canv_contacts_upload.Visibility = Visibility.Collapsed;
                    _homePageView.sp_contacts_list.Visibility = Visibility.Visible;

                    ContactsList = null;
                    ContactsList = _contactsDbList;
                    //  _selectedIndexs = _contactsDbList;
                }

                UIElement uielement = sender as UIElement;
                uielement.ReleaseAllTouchCaptures();
                uielement.ReleaseMouseCapture();
                uielement.ReleaseStylusCapture();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void tbk_download_sample_file_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.ShowNewFolderButton = true;
                folderDlg.SelectedPath = FilePath.Directory.GetCurrentDirectory();

                if (folderDlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.Copy(Constants.SampleContactsExcel, folderDlg.SelectedPath + "/" + System.IO.Path.GetFileName(Constants.SampleContactsExcel), true);
                    Messenger.Default.Send("Sample contacts Excel saved to selected folder successfully..!", "Notification");
                }

                UIElement uielement = sender as UIElement;
                uielement.ReleaseAllTouchCaptures();
                uielement.ReleaseMouseCapture();
                uielement.ReleaseStylusCapture();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void tbk_create_new_contact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.canv_contacts_upload.Visibility = Visibility.Collapsed;
                _homePageView.sp_contacts_list.Visibility = Visibility.Visible;

                ContactsList = null;
                ContactsList = _contactsDbList;
                //  _selectedIndexs = _contactsDbList;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }


        #region Contacts List

        public void grid_select_all_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Border borderSelectAll = (sender as Grid).Children[0] as Border;
                Path selectedPath = (sender as Grid).Children[1] as Path;

                bool isAllSelected = selectedPath.IsVisible ? true : false;

                if (_homePageView.lb_contacts != null && _homePageView.lb_contacts.Items.Count > 0)
                {
                    if (!isAllSelected)
                    {
                        borderSelectAll.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF52AB62"));
                        selectedPath.Visibility = Visibility.Visible;
                        for (int i = 0; i < _homePageView.lb_contacts.Items.Count; i++)
                        {
                            _homePageView.lb_contacts.SelectedItems.Add(_homePageView.lb_contacts.Items[i]);
                        }
                    }
                    else
                    {
                        borderSelectAll.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA9ABAD"));
                        selectedPath.Visibility = Visibility.Collapsed;
                        for (int i = 0; i < _homePageView.lb_contacts.Items.Count; i++)
                        {
                            _homePageView.lb_contacts.SelectedItems.Remove(_homePageView.lb_contacts.Items[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_show_create_contact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.sp_contacts_options.Visibility = Visibility.Collapsed;
                _homePageView.grid_contact_creation.Visibility = Visibility.Visible;

                _homePageView.lb_contacts.Height = 538;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_delete_selected_contacts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                List<Employees> selectedEmployees = _homePageView.lb_contacts.SelectedItems.Cast<Employees>().ToList();

                if (selectedEmployees == null || selectedEmployees.Count == 0)
                {
                    Messenger.Default.Send("Please select atlease one contact to delete..!", "Notification");
                    return;
                }

                int deletedContacts = 0;
                foreach (var emp in selectedEmployees)
                {
                    deletedContacts++;
                    Service.InsertOrUpdateDataToDB(emp, CrudActions.Delete, emp.EmployeeId);
                    _contactsDbList.Remove(emp);
                }

                Messenger.Default.Send(deletedContacts + " contact(s) deleted successfully..!", "Notification");

                ContactsList = null;
                ContactsList = _contactsDbList;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private bool _isItemSelectionChanged = true;

        private bool CustomFilter(object obj)
        {
            if (string.IsNullOrEmpty(_homePageView.txt_search_contact.Text))
            {
                return true;
            }
            else
            {
                bool istrue = ((obj as Employees).Email.ToString().IndexOf(_homePageView.txt_search_contact.Text, StringComparison.OrdinalIgnoreCase) >= 0);
                return istrue;
            }
        }

        public void txt_search_contact_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _isItemSelectionChanged = false;
                CollectionView view = CollectionViewSource.GetDefaultView(_homePageView.lb_contacts.ItemsSource) as CollectionView;
                view.Filter = CustomFilter;
                foreach (Employees emp in ContactsList.Where(s => s.IsSelected == true))
                {
                    _homePageView.lb_contacts.SelectedItems.Add(_homePageView.lb_contacts.Items.Cast<Employees>().ToList().FirstOrDefault(k => k.EmployeeId == emp.EmployeeId && k.IsSelected));
                }
                _isItemSelectionChanged = true;

                //if (string.IsNullOrWhiteSpace(_homePageView.txt_search_contact.Text))
                //{
                //    Keyboard.Focus(_jobCardView.canv_ecu_parameters);
                //}
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void lb_contacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isItemSelectionChanged && ContactsList != null)
                {
                    if (e.AddedItems.Count > 0 && ContactsList.FirstOrDefault(s => s.EmployeeId == (e.AddedItems[0] as Employees).EmployeeId) != null)
                        ContactsList.FirstOrDefault(s => s.EmployeeId == (e.AddedItems[0] as Employees).EmployeeId).IsSelected = true;
                    if (e.RemovedItems.Count > 0 && ContactsList.FirstOrDefault(s => s.EmployeeId == (e.RemovedItems[0] as Employees).EmployeeId) != null)
                        ContactsList.FirstOrDefault(s => s.EmployeeId == (e.RemovedItems[0] as Employees).EmployeeId).IsSelected = false;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_create_new_contact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string contactName = _homePageView.txt_new_contact_name.Text;
                string contactEmail = _homePageView.txt_new_contact_email.Text.ToLower().Trim();
                string contactPhoneNum = _homePageView.txt_new_contact_phone.Text.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(contactName))
                {
                    Messenger.Default.Send("Please enter Name..!", "Notification");
                    return;
                }
                if (string.IsNullOrWhiteSpace(contactEmail))
                {
                    Messenger.Default.Send("Please enter Email..!", "Notification");
                    return;
                }
                if (!NxgUtilities.IsValidEmail(contactEmail))
                {
                    Messenger.Default.Send("Please enter valid Email address..!", "Notification");
                    return;
                }

                if (ContactsList != null && ContactsList.Count > 0 && ContactsList.Any(s => s.Email == contactEmail))
                {
                    Messenger.Default.Send("Contact already exist..!", "Notification");
                    return;
                }
                Employees emp = new Employees() { FirstName = contactName, Email = contactEmail, Phone = contactPhoneNum };
                int contactId = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                if (contactId > 0)
                {
                    emp.EmployeeId = contactId;
                    _contactsDbList.Add(emp);

                    ContactsList = null;
                    ContactsList = _contactsDbList;

                    _homePageView.txt_new_contact_name.Text = "";
                    _homePageView.txt_new_contact_email.Text = "";
                    _homePageView.txt_new_contact_phone.Text = "";
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_cacel_new_contact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.sp_contacts_options.Visibility = Visibility.Visible;
                _homePageView.grid_contact_creation.Visibility = Visibility.Collapsed;

                _homePageView.lb_contacts.Height = 644;

                _homePageView.txt_new_contact_name.Text = "";
                _homePageView.txt_new_contact_email.Text = "";
                _homePageView.txt_new_contact_phone.Text = "";
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_close_contacts_list_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _homePageView.canv_contacts_upload.Visibility = Visibility.Visible;
                _homePageView.sp_contacts_list.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public List<Employees> _contactsList;
        public List<Employees> ContactsList
        {
            get
            {
                return _contactsList;
            }
            set
            {
                _contactsList = value;
                _homePageView.tbk_total_contacts.Text = (_contactsList != null && _contactsList.Count > 0) ? Convert.ToString(_contactsList.Count) : "0";
                OnPropertyChanged("ContactsList");
            }
        }

        #endregion Contacts List


        #endregion Import contacts

        public void grid_verify_activation_key_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string activationKey = _homePageView.txt_activation_key.Text.Trim();
                if (string.IsNullOrWhiteSpace(activationKey))
                {
                    Messenger.Default.Send("Please enter Activation Key..!", "Notification");
                    return;
                }

                string response = App._regLicence.CheckForLicenceUpgrade(activationKey);
                if (!string.IsNullOrWhiteSpace(response) && response == "True")
                {
                    response = "Activation is successfully completed";
                    _homePageView.txt_activation_key.Text = "";
                }

                Messenger.Default.Send(response, "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #region Location Details

        private void SaveConfigFile()
        {
            XmlDocument myxml = new XmlDocument();
            myxml.Load(Constants.SettingsFilePath);

            myxml.ChildNodes[0].SelectSingleNode("Resource").InnerText = SelectedResourceDirectory;
            myxml.ChildNodes[0].SelectSingleNode("LocationName").InnerText = LocationName;
            myxml.ChildNodes[0].SelectSingleNode("CityName").InnerText = CityName;
            myxml.ChildNodes[0].SelectSingleNode("CountryName").InnerText = CountryName;

            myxml.Save(Constants.SettingsFilePath);
        }

        public void grid_save_location_details_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LocationName))
                {
                    Messenger.Default.Send("Please enter location name..!", "Notification");
                    return;
                }

                if (string.IsNullOrWhiteSpace(CityName))
                {
                    Messenger.Default.Send("Please enter city name..!", "Notification");
                    return;
                }

                if (string.IsNullOrWhiteSpace(CountryName))
                {
                    Messenger.Default.Send("Please enter country name..!", "Notification");
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedResourceDirectory))
                {
                    Messenger.Default.Send("Please select resource directory..!", "Notification");
                    return;
                }

                Constants.LocationName = LocationName;
                Constants.CityName = CityName;
                Constants.CountryName = CountryName;

                DateTime currentDateTime = DateTime.Now;
                CurrentDateWithDay = currentDateTime.DayOfWeek + " " + NxgUtilities.GetDateExtension(currentDateTime.Date.Day.ToString()) + " " + currentDateTime.ToString("MMMM") + " " + currentDateTime.Year + "\n" + Constants.CityName + ", " + Constants.CountryName;

                Constants.ProjectResources = SelectedResourceDirectory;

                NxgUtilities.CreateDirectory(Constants.ProjectResources);
                NxgUtilities.CreateDirectory(Constants.AttachmentResources);
                NxgUtilities.CreateDirectory(Constants.AttachmentResourceThumbs);

                SaveConfigFile();

                Messenger.Default.Send("Location settings have updated successfully..!", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void grid_select_resource_directory_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.ShowNewFolderButton = true;
                folderDlg.SelectedPath = FilePath.Directory.GetCurrentDirectory();

                if (folderDlg.ShowDialog() == DialogResult.OK)
                    SelectedResourceDirectory = folderDlg.SelectedPath;

            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #endregion Settings page

        #region Retrieve class

        public void btn_retrieve_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Service.ConnectToServer())
                {
                    Service._isServerConnected = true;

                    GetTodayClass(true);

                    Class currentClass = _classList.FirstOrDefault(s => s.ClassId == Convert.ToInt32(_homePageView.tbk_class_id.Text));

                    if (currentClass != null)
                    {
                        _currentClassIndex = _classList.IndexOf(currentClass);
                        currentClass.ActualStartTime = default(DateTime);
                        StartClass_MouseUp(null, null);
                    }
                    else
                    {
                        Messenger.Default.Send("There is no Class with the provided id", "Notification");
                        Service._isServerConnected = false;

                        GetTodayClass(true);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Retrieve class
    }

}