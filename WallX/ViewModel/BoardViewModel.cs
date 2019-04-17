using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Markup;
using System.IO;
using System.IO.Packaging;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.ComponentModel;
using WallX.Services;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using WallX.Views;
using System.Xml;
using NextGen.Controls;
using NextGen.Controls.InkTools;
using NextGen.Controls.InkRecognizer;
using WallX.Helpers;
using System.Threading.Tasks;
using WallX.Views;
using System.Threading;
using NextGen.Controls.GoogleAPIVoice;
//using Google.Cloud.Speech.V1;
//using NAudio.Wave;

using ShapePath = System.Windows.Shapes.Path;
using Ellipse = System.Windows.Shapes.Ellipse;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;

namespace WallX.ViewModel
{
    class BoardViewModel : InkToolBar, INotifyPropertyChanged
    {
        #region Variables

        public static Class _currentClass = null;
        public static Class _actualClass = null;

        private List<BoardAnnotations> _pagesList;
        private List<LibraryThumbs> _mediaAnnotaionsList;
        private List<ImageAnnotations> _annotationsModuleList;
        private List<ImageAnnotations> _imageAnnotaionsList;
        private BoardView _boardview;
        private int _nextPageIndex, _ActualDuration = 0;
        private int _selectedStickyColorIndex = -1, _selectedPageIndex = -1;
        private double _boardManipulationPointX, _boardManipulationPointY = 0;
        private bool _isFromPageChange, _isExistingClass, _isFromLibrary, _isFirstStepOver, _agendaEndingAlertDisplayed, _agendaOverAlertDisplayed, _classEndingAlertDisplayed;
        private string _nextProjectSaveTime, _currentClassDuration, _actualStartTime, _recognizedClassName, _recognizedEmail, _deletelibraryItem;
        private string[] selectedFiles;
        private Grid _selectedCanvasItem;
        private Canvas _selectedToolMenuItem;
        private StackPanel _selectedLibraryItem;
        private Border _selectedDoubleTapItem;
        private FrameworkElement _selectedBoardChildren;
        private Point _selectedChildPosition, _dragItemPosition, _dragItemPositionFromScreen;
        private InkToolName _inkToolName;
        private NxgInputType _adhocInputMethodName = NxgInputType.Keyboard;

        private Grid _canvDragItemParent = null;

        bool _isFromEmailSelected = false;

        private RethinkService _rethinkColService = null;
        private static bool _isPageSelectionChangedByOthers = false;

        #endregion

        #region Pageload

        /// <summary>
        /// Load event to initialize necessary fields 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BoardView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _boardview = sender as BoardView;

                TouchPositions = new Dictionary<int, Point>();
                TouchEllipses = new Dictionary<int, Ellipse>();

                Messenger.Default.Register<string>(this, "Add Canvas", AddPage);
                Messenger.Default.Register<bool>(this, "Add Resource", AddItemsToLibrary);
                Messenger.Default.Register<string>(this, "Exit Board", ExitBoard);
                Messenger.Default.Register<string>(this, "Exit Class", ExitClass);
                Messenger.Default.Register<string>(this, "Delete Canvas", DeleteCanvas);
                Messenger.Default.Register<string>(this, "Clear Board", ClearBoard);
                Messenger.Default.Register<DateTime>(this, "Timer", timer_Tick);
                Messenger.Default.Register<string>(this, "CompleteCurrentAgenda", StartAgendaItem);
                Messenger.Default.Register<string>(this, "RemoveBoardItem", RemoveBoardItem);
                Messenger.Default.Register<string>(this, "Delete Library Item", DeleteLibraryItem);
                Messenger.Default.Register<string>(this, "close_screen_recording", CloseScreenRecordingView);
                Messenger.Default.Register<KeyValuePair<List<string>, string>>(this, "GetImageFromDesktop", AddDesktopImgToLibrary);
                Messenger.Default.Register<KeyValuePair<string, string>>(this, "AddColBoardItem", AddColBoardItem);
                Messenger.Default.Register<KeyValuePair<object, KeyValuePair<string, string>>>(this, "UpdateColBoardItem", UpdateColBoardItem);
                Messenger.Default.Register<int>(this, "ChangePage", ChangeToSelectedPage);
                Messenger.Default.Register<string>(this, "ChangeBoardBackground", ChangeBoardBackgroundFromOtherUser);
                Messenger.Default.Register<string>(this, "cancel_next_class_board", CancelNextClassBoard);
                Messenger.Default.Register<string>(this, "DeleteSelectedAudioFileFromDb", DeleteSelectedAudioFileFromDb);
                Messenger.Default.Register<string>(this, "RemoveSelectedItems", RemoveSelectedItems);

                DragAndDrop.RegisterElement(_boardview.libraryBackground, null, DragDirection.LeftNRight, null, HideDragItemParent);
                DragAndDrop.RegisterElement(_boardview.agendaBackground, null, DragDirection.LeftNRight, null, HideDragItemParent);
                DragAndDrop.RegisterElement(_boardview.inkCanvas, DropElement, DragDirection.LeftNRight, null, HideDragItemParent);

                _speechToText = new SpeechToText();

                App.ExecuteMethod(LoadClassDataonBoard);

                _boardview.canv_recurring_class.Visibility = Visibility.Collapsed;

                _boardview.canv_Gestures.Visibility = _boardview.canv_Zoom.Visibility = Constants.ExtraFeatures ? Visibility.Visible : Visibility.Collapsed; // pending (aparanj)

                NxgUtilities.CollapseElements(new List<FrameworkElement> { _boardview.canv_addmedia, _boardview.canv_mom, _boardview.canv_voicenotes, _boardview.canv_menu_browser_, _boardview.canv_desktop, _boardview.canv_library_item, _boardview.canv_email, _boardview.canv_screen_recording, _boardview.canv_sticky, _boardview.canv_video_call_zoomus, _boardview.canv_WallX_Settings_Menu, _boardview.canv_magnifier_slider });

                foreach (string option in NextGen.Controls.Licence.RegisterLicence.MenuOptions)
                {
                    switch (option.Trim())
                    {
                        case "Attachment":
                            _boardview.canv_addmedia.Visibility = Visibility.Visible;
                            break;
                        //case "Audio Conversion":
                        //    _boardview.canv_mom.Visibility = Visibility.Visible;
                        //    break;
                        //case "Audio Recording":
                        //    _boardview.canv_voicenotes.Visibility = Visibility.Visible;
                        //    break;
                        //case "Browser":
                        //    _boardview.canv_menu_browser_.Visibility = Visibility.Visible;
                        //    break;
                        //case "Desktop Mode":
                        //    _boardview.canv_desktop.Visibility = Visibility.Visible;
                        //    break;
                        //case "Library":
                        //    _boardview.canv_library_item.Visibility = Visibility.Visible;
                        //    break;
                        //case "Pdf Generation":
                        //    _boardview.canv_email.Visibility = Visibility.Visible;
                        //    break;
                        //case "Screen Recording":
                        //    _boardview.canv_screen_recording.Visibility = Visibility.Visible;
                        //    break;
                        case "Sticky Note":
                            _boardview.canv_sticky.Visibility = Visibility.Visible;
                            break;
                            //case "Zoom":
                            //    _boardview.canv_video_call_zoomus.Visibility = Visibility.Visible;
                            //    break;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Load & Bind class data to board
        /// </summary>
        private void LoadClassDataonBoard()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AddElement(_boardview.inkCanvas, _boardview.inkCanvas);
                });

                ResetClassinBoard();

                if (Service._isServerConnected)
                    _rethinkColService = new RethinkService(_currentClass.UniqueClassId, _boardview.inkCanvas, _boardview.inkCanvas_Guest);

                NxgUtilities.CreateDirectory(Constants.AttachmentResources);

                _currentClassDuration = Convert.ToInt32(TimeSpan.Parse(_currentClass.Duration).TotalMinutes).ToString();
                DateTime classDate = _currentClass.StartTime;

                DateTime boardDateTime = DateTime.Now;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentMonth = boardDateTime.ToString("MMM") + "\n" + boardDateTime.Year.ToString();
                    CurrentDate = boardDateTime.Date.Day.ToString();
                    _actualStartTime = boardDateTime.ToString("hh:mm tt");
                    ClassTitle = _currentClass.ClassName;

                    CountDownMinutes = "00";
                    CountDownMinutesSuffix = "";
                    ClassDate = NxgUtilities.GetDateExtension(classDate.Day.ToString()) + " " + classDate.ToString("MMMM") + " " + classDate.Year;

                    AgendasList = null;
                });

                foreach (Participants item in _currentClass.ParticipantList)
                {
                    Employees participant = HomePageViewModel._contactsDbList.FirstOrDefault(k => k.EmployeeId == item.EmployeeId);
                    if (participant != null)
                    {
                        item.Employee = participant;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ParticipantsList = _currentClass.ParticipantList;
                    CurrentTime = boardDateTime.ToString("hh:mm tt");
                    AttendanceCount = _currentClass.ParticipantList != null ? _currentClass.ParticipantList.Count().ToString() : "0";
                    _boardview.canv_agenda_details.Visibility = Visibility.Collapsed;
                });

                _nextProjectSaveTime = DateTime.Now.Add(TimeSpan.FromMinutes(Constants.AutoSaveTimeFrequency)).ToString("HH:mm:ss");

                if (_currentClassDuration != "0")
                {
                    if (_currentClass.AgendaList != null && _currentClass.AgendaList.Count > 0)
                    {
                        foreach (Agendas item in _currentClass.AgendaList)
                        {
                            Employees emp = HomePageViewModel._contactsDbList.FirstOrDefault(k => k.EmployeeId == item.EmployeeId);
                            if (emp != null)
                            {
                                item.EmployeeEmail = emp.Name;
                                item.EmployeeEmail = emp.Email;
                            }
                            item.IsRunning = TimeSpan.Parse(item.ActualDuration).TotalSeconds > 0 ? "completed" : "";
                            item.IsSelected = false;
                            item.IsLast = _currentClass.AgendaList.IndexOf(item) == _currentClass.AgendaList.Count - 1 ? "Collapsed" : "Visible";
                        }

                        _currentClass.AgendaList.First().IsSelected = true;
                        Application.Current.Dispatcher.InvokeAsync((Action)(() =>
                        {
                            AgendasList = _currentClass.AgendaList;
                            SelectedAgenda = AgendasList[0];
                            _boardview.canv_agenda_details.Visibility = Visibility.Visible;
                            if (SelectedAgenda != null && (SelectedAgenda.ActualStartTime != default(DateTime) || SelectedAgenda.ActualEndTime != default(DateTime)))
                            {
                                (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Collapsed;
                                _boardview.canv_current_agenda_completed.Visibility = Visibility.Visible;
                                _boardview.txt_current_agenda_actualtime.Text = TimeSpan.Parse(SelectedAgenda.ActualDuration).TotalMinutes.ToString("00");
                            }
                        }));
                    }

                    _ActualDuration = !string.IsNullOrWhiteSpace(_currentClass.ActualDuration) ? Convert.ToInt32(TimeSpan.Parse(_currentClass.ActualDuration).TotalMinutes) : 0;

                    if (_ActualDuration >= 0 && (_currentClass.ActualStartTime != null || _currentClass.ActualStartTime != default(DateTime)) && TimeSpan.Parse(_currentClass.ActualStartTime.ToString("HH:mm:ss")).TotalMinutes > 0)
                    {
                        _isExistingClass = true;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CountDownMinutes = _ActualDuration.ToString("00");
                        });
                    }
                    else if (_ActualDuration <= 0 && _currentClass.ActualStartTime != default(DateTime) && TimeSpan.Parse(_currentClass.ActualStartTime.ToString("HH:mm:ss")).TotalMinutes <= 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CountDownMinutes = _ActualDuration.ToString();
                        });
                    }
                    else
                    {
                        _currentClass.ActualStartTime = boardDateTime;
                        //StartAgendaItem();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CountDownMinutes = _ActualDuration.ToString("00");
                            CountDownMinutesSuffix = "mins left";
                        });
                    }
                    //  _currentMeeting.Actual_StartTime = boardDateTime;
                    //  GetMeetingInfoData();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CountDownMinutesSuffix = "mins";
                        AttendanceCount = ParticipantsList.Count.ToString();
                    });
                }

                if (_currentClass.BoardAnnotationList != null && _currentClass.BoardAnnotationList.Count > 0)
                {
                    _pagesList = _currentClass.BoardAnnotationList.OrderBy(s => s.PageIndex).ToList();
                    _selectedPageIndex = _pagesList[0].PageIndex;
                    LoadPageDatafromDB(_selectedPageIndex);
                    CheckBoardInsertorUpdate();
                    _nextPageIndex = _pagesList.Last().PageIndex;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CanvasCount = _pagesList.Count;
                    });
                }
                else if (_currentClass.BoardAnnotationList == null || _currentClass.BoardAnnotationList.Count == 0)
                {
                    _pagesList = new List<BoardAnnotations>();
                    _selectedPageIndex = -1; // for adding new page
                    AddPage();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _boardview.listBox_Pages.ItemsSource = _pagesList;

                    if (_actualClass == null && _currentClass.ClassType != NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) && _currentClass.ClassType != ClassScheduleType.OneTimeClass.ToString())
                    {
                        LoadRecurringClass(_currentClass.RecurringClassId);
                        _actualClass = _currentClass;
                    }

                    if (_currentClass.ClassType != NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) && _currentClass.ClassType != ClassScheduleType.OneTimeClass.ToString())
                    {
                        if (_currentClass.ClassId == _actualClass.ClassId)
                        {
                            _actualClass = _currentClass;
                            new List<UIElement>() { _boardview.stack_tool_menu, _boardview.canv_addpage, _boardview.canv_show_participants, _boardview.stack_bottombar, _boardview.canv_inkcanvas_parent }.ToList().ForEach(k => k.IsEnabled = true);
                        }
                        else
                        {
                            new List<UIElement>() { _boardview.stack_tool_menu, _boardview.canv_addpage, _boardview.canv_show_participants, _boardview.stack_bottombar, _boardview.canv_inkcanvas_parent }.ToList().ForEach(k => k.IsEnabled = false);

                            Messenger.Default.Send("You can only view this Class, cannot update or edit.....!", "Notification");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// timer tick for update datetime & class details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(DateTime dateTime)
        {
            try
            {
                CurrentTime = dateTime.ToString("hh:mm tt");

                if (!string.IsNullOrWhiteSpace(_currentClassDuration) && TimeSpan.Parse(_currentClassDuration.Split('.').First()).TotalSeconds > -1 && _currentClass != null && (int)TimeSpan.Parse(_currentClass.ActualDuration).TotalSeconds == 0 && _ActualDuration <= 0 && !string.IsNullOrWhiteSpace(_actualStartTime))
                {
                    string currentDuration = DateTime.Now.Subtract(DateTime.Parse(_actualStartTime)).ToString(@"hh\:mm");
                    if (_currentClassDuration == "0")
                    {
                        CountDownMinutes = ((int)DateTime.Now.Subtract(DateTime.Parse(_actualStartTime)).TotalMinutes).ToString();
                    }
                    else
                    {
                        CountDownMinutes = ((int)TimeSpan.Parse(_currentClass.Duration).Subtract(TimeSpan.Parse(currentDuration)).TotalMinutes).ToString();
                    }

                    if (int.Parse(CountDownMinutes) <= 5 && !_classEndingAlertDisplayed && !_currentClass.IsFromAdhoc)
                    {
                        Messenger.Default.Send("Hurry up, only 5 mins. left for the class to end", "Notification");
                        _classEndingAlertDisplayed = true;
                    }

                    if (_pagesList != null && _pagesList.Count > 0 && !string.IsNullOrWhiteSpace(_nextProjectSaveTime) && DateTime.Now > Convert.ToDateTime(_nextProjectSaveTime))
                    {
                        CheckBoardInsertorUpdate();
                        ClearSelectedBoardItem(false);
                        _nextProjectSaveTime = DateTime.Now.Add(TimeSpan.FromMinutes(Constants.AutoSaveTimeFrequency)).ToString("HH:mm:ss");
                    }
                }

                if (SelectedAgenda != null && SelectedAgenda.IsRunning == "true")
                {
                    _boardview.txt_current_agenda_actualtime.Text = ((int)dateTime.Subtract(SelectedAgenda.ActualStartTime).TotalMinutes).ToString("00");

                    int minutes = (int)dateTime.Subtract(SelectedAgenda.ActualStartTime).TotalMinutes;
                    if (((Convert.ToInt32(TimeSpan.Parse(SelectedAgenda.Duration).TotalMinutes) * 70) / 100) < minutes && !_agendaEndingAlertDisplayed)
                    {
                        Messenger.Default.Send("Hurry up, only few minutes left for the Class plan item to end", "Notification");
                        _agendaEndingAlertDisplayed = true;
                    }
                    else if (TimeSpan.Parse(SelectedAgenda.Duration).TotalMinutes == minutes && !_agendaOverAlertDisplayed)
                    {
                        Messenger.Default.Send(new KeyValuePair<string, string>("Continue current agenda", "Class plan time completed. Would you like to continue ?"), "Result");
                        _agendaOverAlertDisplayed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Properties

        private BitmapImage _boardBackground;
        public BitmapImage BoardBackground
        {
            get { return _boardBackground; }
            set
            {
                _boardBackground = value;
                OnPropertyChanged("BoardBackground");
            }
        }

        private string _classTitle;
        public string ClassTitle
        {
            get { return _classTitle; }
            set
            {
                _classTitle = value;
                OnPropertyChanged("ClassTitle");
            }
        }

        private string _classDate;
        public string ClassDate
        {
            get { return _classDate; }
            set
            {
                _classDate = value;
                OnPropertyChanged("ClassDate");
            }
        }

        private string _currentMonth;
        public string CurrentMonth
        {
            get { return _currentMonth; }
            set
            {
                _currentMonth = value;
                OnPropertyChanged("CurrentMonth");
            }
        }

        private string _currentDate;
        public string CurrentDate
        {
            get { return _currentDate; }
            set
            {
                _currentDate = value;
                OnPropertyChanged("CurrentDate");
            }
        }

        private string _currentTime;
        public string CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }

        private string _countDownMinutes;
        public string CountDownMinutes
        {
            get { return _countDownMinutes; }
            set
            {
                _countDownMinutes = value;
                OnPropertyChanged("CountDownMinutes");
            }
        }

        private string _countDownMinutesSuffix;
        public string CountDownMinutesSuffix
        {
            get { return _countDownMinutesSuffix; }
            set
            {
                _countDownMinutesSuffix = value;
                OnPropertyChanged("CountDownMinutesSuffix");
            }
        }

        private string _agendaHeader;
        public string AgendaHeader
        {
            get { return _agendaHeader; }
            set
            {
                _agendaHeader = value;
                OnPropertyChanged("AgendaHeader");
            }
        }

        private int _canvasCount;
        public int CanvasCount
        {
            get { return _canvasCount; }
            set
            {
                _canvasCount = value;
                OnPropertyChanged("CanvasCount");
            }
        }

        private List<Participants> _participantsList;
        public List<Participants> ParticipantsList
        {
            get { return _participantsList; }
            set
            {
                _participantsList = value;
                OnPropertyChanged("ParticipantsList");
            }
        }

        private string _attendanceCount;
        public string AttendanceCount
        {
            get { return _attendanceCount; }
            set
            {
                _attendanceCount = value;
                OnPropertyChanged("AttendanceCount");
            }
        }

        private List<Agendas> _agendasList;
        public List<Agendas> AgendasList
        {
            get { return _agendasList; }
            set
            {
                _agendasList = value;
                OnPropertyChanged("AgendasList");
            }
        }

        private Agendas _selectedAgenda;
        public Agendas SelectedAgenda
        {
            get { return _selectedAgenda; }
            set
            {
                _selectedAgenda = value;
                OnPropertyChanged("SelectedAgenda");
            }
        }


        public List<LibraryThumbs> _mediaList;
        public List<LibraryThumbs> MediaList
        {
            get { return _mediaList; }
            set
            {
                _mediaList = value;
                OnPropertyChanged("MediaList");
            }
        }

        public List<LibraryThumbs> _captureList;
        public List<LibraryThumbs> CaptureList
        {
            get { return _captureList; }
            set
            {
                _captureList = value;
                OnPropertyChanged("CaptureList");
            }
        }

        private List<LibraryThumbs> _agendaNoteList;
        public List<LibraryThumbs> AgendaNoteList
        {
            get
            {
                return this._agendaNoteList;
            }
            set
            {
                this._agendaNoteList = value;
                OnPropertyChanged("AgendaNoteList");
            }
        }

        private List<LibraryThumbs> _agendaTaskList;
        public List<LibraryThumbs> AgendaTaskList
        {
            get
            {
                return this._agendaTaskList;
            }
            set
            {
                this._agendaTaskList = value;
                OnPropertyChanged("AgendaTaskList");
            }
        }

        private List<LibraryThumbs> _agendaDecisionList;
        public List<LibraryThumbs> AgendaDecisionList
        {
            get
            {
                return this._agendaDecisionList;
            }
            set
            {
                this._agendaDecisionList = value;
                OnPropertyChanged("AgendaDecisionList");
            }
        }

        private string _emaildocument;
        public string EmailDocument
        {
            get { return _emaildocument; }
            set
            {
                _emaildocument = value;
                OnPropertyChanged("EmailDocument");
            }
        }

        private List<Class> _recurringClassList;
        public List<Class> RecurringClassList
        {
            get
            {
                return _recurringClassList;
            }
            set
            {
                _recurringClassList = value;
                OnPropertyChanged("RecurringClassList");
            }
        }

        private Class _nextClass;
        public Class NextClass
        {
            get
            {
                return _nextClass;
            }
            set
            {
                _nextClass = value;
                OnPropertyChanged("NextClass");
            }
        }

        private List<Employees> _searchContactList;
        public List<Employees> SearchContactList
        {
            get { return _searchContactList; }
            set
            {
                _searchContactList = value;
                OnPropertyChanged("SearchContactList");
            }
        }

        private Employees _searchedContact;
        public Employees SearchedContact
        {
            get { return _searchedContact; }
            set
            {
                _searchedContact = value;
                if (_searchedContact != null)
                {
                    _isFromEmailSelected = true;
                    _boardview.listbox_Participants.Visibility = Visibility.Visible;
                    _boardview.lb_task_members.Visibility = Visibility.Visible;
                    SearchedContactText = _searchedContact.Email;
                }
                OnPropertyChanged("SearchedContact");
            }
        }

        private string _searchedContactText;
        public string SearchedContactText
        {
            get { return _searchedContactText; }
            set
            {
                _searchedContactText = value;
                OnPropertyChanged("SearchedContactText");
            }
        }

        private string _audioText;
        public string AudioText
        {
            get { return _audioText; }
            set
            {
                _audioText = value;
                OnPropertyChanged("AudioText");
            }
        }

        private string _audioFileTotalTime;
        public string AudioFileTotalTime
        {
            get { return this._audioFileTotalTime; }
            set
            {
                this._audioFileTotalTime = value;
                OnPropertyChanged("AudioFileTotalTime");
            }
        }

        private string _audioFileCurrentTime;
        public string AudioFileCurrentTime
        {
            get { return this._audioFileCurrentTime; }
            set
            {
                this._audioFileCurrentTime = value;
                OnPropertyChanged("AudioFileCurrentTime");
            }
        }

        private string _audioFileText;
        public string AudioFileText
        {
            get { return this._audioFileText; }
            set
            {
                this._audioFileText = value;
                OnPropertyChanged("AudioFileText");
            }
        }

        public List<LibraryThumbs> _audioList;
        public List<LibraryThumbs> AudioList
        {
            get { return _audioList; }
            set
            {
                _audioList = value;
                OnPropertyChanged("AudioList");
            }
        }

        public List<LibraryThumbs> _screenrecordList;
        public List<LibraryThumbs> ScreenrecordList
        {
            get { return _screenrecordList; }
            set
            {
                _screenrecordList = value;
                OnPropertyChanged("ScreenrecordList");
            }
        }

        #endregion

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

        #region Board

        #region Circle for Tap

        private double CircleWidth = 100;
        private Dictionary<int, Point> TouchPositions;
        private Dictionary<int, Ellipse> TouchEllipses;

        public void inkCanvas_TouchDown(object sender, TouchEventArgs args)
        {
            if (_selectedBoardChildren != null)
            {
                InkCanvas inkcanv = _selectedBoardChildren as InkCanvas;
                Border border = (inkcanv.Parent as Grid).Parent as Border;
                if (border.TouchesOver.Count() == 1)
                {
                    List<ImageAnnotations> imgAnno = _imageAnnotaionsList.Where(s => Convert.ToInt32(border.Tag) == s.AnnotationId).ToList();
                    if (imgAnno != null && imgAnno.Count >= 1)
                    {
                        MatrixTransform matrix = XamlReader.Parse(imgAnno[0].Manipulation) as MatrixTransform;

                        TouchPoint tp = args.GetTouchPoint(null);
                        imgAnno[0].Manipulation = XamlWriter.Save(new MatrixTransform(1, 0, 0, 1, ((_boardManipulationPointX) + tp.Position.X) - (border.Width / 2), ((_boardManipulationPointY) + tp.Position.Y) - (border.Height / 2)));

                        AddChildToBoard(_imageAnnotaionsList.Where(s => Convert.ToInt32(border.Tag) == s.AnnotationId).ToList(), true);
                    }
                }
                ToolShowMenuComponent(_boardview.canv_hand);
            }
            else
            if (_isLongPressEnabled && !_longPressDeviceId.Contains(args.TouchDevice.Id) && _boardview.inkCanvas.TouchesOver.Count() == 3)
            {
                _boardview.inkCanvas.CaptureTouch(args.TouchDevice);

                TouchPoint tp = args.TouchDevice.GetTouchPoint(null);

                Ellipse el = AddEllipseAt(_boardview.inkCanvas, tp.Position, Brushes.White);

                if (!TouchPositions.ContainsKey(args.TouchDevice.Id))
                {
                    TouchPositions.Add(args.TouchDevice.Id, tp.Position);
                    TouchEllipses.Add(args.TouchDevice.Id, el);
                }
                args.Handled = true;
            }
        }

        public void inkCanvas_TouchMove(object sender, TouchEventArgs args)
        {
            if (_isLongPressEnabled && !_longPressDeviceId.Contains(args.TouchDevice.Id))
            {
                TouchPoint tp = args.GetTouchPoint(null);
                if (TouchEllipses.ContainsKey(args.TouchDevice.Id))
                {
                    InkCanvas.SetLeft(TouchEllipses[args.TouchDevice.Id], ((1920 + _boardManipulationPointX) + tp.Position.X) - (CircleWidth / 2));
                    InkCanvas.SetTop(TouchEllipses[args.TouchDevice.Id], ((1080 + _boardManipulationPointY) + tp.Position.Y) - (CircleWidth / 2));
                }
                args.Handled = true;
            }
        }

        public void inkCanvas_TouchUp(object sender, TouchEventArgs args)
        {
            if (_isLongPressEnabled && !_longPressDeviceId.Contains(args.TouchDevice.Id))
            {
                TouchPoint tp = args.GetTouchPoint(null);

                if (TouchPositions.ContainsKey(args.TouchDevice.Id))
                {
                    TouchPositions.Remove(args.TouchDevice.Id);
                    _boardview.inkCanvas.Children.Remove(TouchEllipses[args.TouchDevice.Id]);
                    TouchEllipses.Remove(args.TouchDevice.Id);
                }

                _boardview.inkCanvas.ReleaseTouchCapture(args.TouchDevice);
                args.Handled = true;
            }

            if (_isLongPressEnabled && _boardview.inkCanvas.TouchesOver.Count() == 0)
            {
                _isLongPressEnabled = false;
                ToolShowMenuComponent(_boardview.canv_marker);
            }
        }

        private Ellipse AddEllipseAt(InkCanvas canv, Point pt, Brush brush)
        {
            Ellipse el = new Ellipse();
            el.Stroke = brush;
            el.Fill = brush;
            el.Width = CircleWidth;
            el.Height = CircleWidth;

            Canvas.SetLeft(el, pt.X - (CircleWidth / 2));
            Canvas.SetTop(el, pt.Y - (CircleWidth / 2));

            canv.Children.Add(el);

            return el;
        }

        #endregion Circle for Tap

        //private bool _isCurrentUserStroke = true;
        //private bool _isCurrentUserChildStroke = true;
        //private Stroke _erasingStroke = null;
        public void inkCanvas_MouseUp(object sender, MouseEventArgs args)
        {
            try
            {
                if (_boardview.inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    if (_rethinkColService != null)
                        _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvas", XamlWriter.Save(_boardview.inkCanvas.Strokes));

                    ClearChildFromEditing();
                    _boardview.canv_colors.Visibility = Visibility.Collapsed;
                    _boardview.canv_strokes.Visibility = Visibility.Collapsed;

                    Monitor(_boardview.inkCanvas, _boardview.canv_Undo, _boardview.canv_Redo);

                    if (_isLongPressEnabled && _boardview.inkCanvas.TouchesOver.Count() == 0)
                    {
                        _isLongPressEnabled = false;
                        ToolShowMenuComponent(_boardview.canv_marker);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// collecting stroke in ink mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs args)
        {
            try
            {
                if (_selectedBoardChildren == null)
                {
                    args.Stroke.AddPropertyData(Guid.NewGuid(), RethinkService._macAddress);

                    if (_rethinkColService != null)
                        _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvas", XamlWriter.Save(_boardview.inkCanvas.Strokes));

                    ClearChildFromEditing();

                    _boardview.canv_colors.Visibility = Visibility.Collapsed;
                    _boardview.canv_strokes.Visibility = Visibility.Collapsed;

                    Monitor(_boardview.inkCanvas, _boardview.canv_Undo, _boardview.canv_Redo);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Manipulation starting event for inkcanvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs args)
        {
            try
            {
                if (_selectedBoardChildren == null)
                {
                    args.Handled = true;
                    args.ManipulationContainer = sender is Border ? _boardview.canv_board_zoom_display : _boardview.canv_main;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        double offsetXLimit = -7680, offsetYLimit = -4320;
        public void canv_element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            try
            {
                if ((sender as FrameworkElement) != null && (sender as FrameworkElement).TouchesOver.Count() == 3)
                    Tap_3_TapMethod(sender as UIElement, null);

                if (_selectedBoardChildren == null)
                {
                    FrameworkElement fe = e.Source as FrameworkElement;
                    ManipulationDelta md = e.DeltaManipulation;
                    Vector trans = md.Translation;
                    var transformation = fe.RenderTransform as MatrixTransform;
                    Matrix m = transformation == null ? Matrix.Identity : transformation.Matrix;

                    Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                    center = m.Transform(center);

                    m.Translate(trans.X, trans.Y);

                    if (_isZoomEnabled)
                    {
                        if (m.M11 > 1.1 || m.M22 > 1.1)
                        {
                            m.ScaleAt(0.9, 0.9, center.X, center.Y);
                            e.Cancel();
                        }
                        else if (m.M11 < 0.2034 || m.M22 < 0.2034)
                        {
                            m.M11 = m.M22 = 0.2034;
                            e.Cancel();
                        }
                        else
                        {
                            m.ScaleAt(e.DeltaManipulation.Scale.X, e.DeltaManipulation.Scale.Y, center.X, center.Y);
                        }
                    }

                    if (m.OffsetX > 0)
                    {
                        e.Cancel();
                        m.OffsetX = 0;
                        if (m.OffsetY > 0)
                            m.OffsetY = 0;
                    }
                    else if (m.OffsetX < offsetXLimit)
                    {
                        e.Cancel();
                        m.OffsetX = offsetXLimit;
                        if (m.OffsetY < offsetYLimit)
                            m.OffsetY = offsetYLimit;
                    }
                    else if (m.OffsetY > 0)
                    {
                        e.Cancel();
                        m.OffsetY = 0;
                        if (m.OffsetX > 0)
                            m.OffsetX = 0;
                    }
                    else if (m.OffsetY < offsetYLimit)
                    {
                        e.Cancel();
                        m.OffsetY = offsetYLimit;
                        if (m.OffsetX < offsetXLimit)
                            m.OffsetX = offsetXLimit;
                    }

                    fe.RenderTransform = new MatrixTransform(m);

                    m.OffsetX = (m.OffsetX * -1);
                    m.OffsetY = (m.OffsetY * -1);

                    if (fe.Name == "inkCanvas")
                    {
                        _boardview.border_zoom.RenderTransform = new MatrixTransform(m);
                    }
                    else
                    {
                        _boardview.canv_inkcanvas_parent.RenderTransform = new MatrixTransform(m);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Mainpulate delta event for main ink canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void inkCanvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            try
            {
                if ((sender as FrameworkElement) != null && (sender as FrameworkElement).TouchesOver.Count() == 3)
                    Tap_3_TapMethod(sender as UIElement, null);

                FrameworkElement sourceElement = e.Source as FrameworkElement;
                FrameworkElement element = (sender is Border ? sourceElement : sourceElement.Parent) as FrameworkElement;
                MatrixTransform xform = element.RenderTransform as MatrixTransform;
                Matrix matrix = xform.Matrix;
                ManipulationDelta delta = e.DeltaManipulation;
                Point center = e.ManipulationOrigin;
                if (_isZoomEnabled && !(sender is Border))
                {
                    matrix.Translate(delta.Translation.X, delta.Translation.Y);
                    matrix.ScaleAt(delta.Scale.X, delta.Scale.Y, center.X, center.Y);
                    matrix.RotateAt(delta.Rotation, center.X, center.Y);

                    element.RenderTransform = new MatrixTransform(matrix);
                }
                else
                {
                    if (matrix.OffsetX >= -1910 && matrix.OffsetX <= 1910 && matrix.OffsetY >= -1070 && matrix.OffsetY <= 1070)
                    {
                        matrix.Translate(delta.Translation.X, delta.Translation.Y);
                    }
                    else if (matrix.OffsetX > 1910)
                    {
                        e.Cancel();
                        matrix.OffsetX = 1910;
                        if (matrix.OffsetY > 1070)
                            matrix.OffsetY = 1070;
                    }
                    else if (matrix.OffsetX < -1910)
                    {
                        e.Cancel();
                        matrix.OffsetX = -1910;
                        if (matrix.OffsetY < -1070)
                            matrix.OffsetY = -1070;
                    }
                    else if (matrix.OffsetY > 1070)
                    {
                        e.Cancel();
                        matrix.OffsetY = 1070;
                        if (matrix.OffsetX > 1910)
                            matrix.OffsetX = 1910;
                    }
                    else if (matrix.OffsetY < -1070)
                    {
                        e.Cancel();
                        matrix.OffsetY = -1070;
                        if (matrix.OffsetX < -1910)
                            matrix.OffsetX = -1910;
                    }

                    //xform.Matrix = matrix;
                    element.RenderTransform = new MatrixTransform(matrix);

                    matrix.OffsetX = matrix.OffsetX * -1;
                    matrix.OffsetY = matrix.OffsetY * -1;

                    if (element.Name == "canv_inkcanvas_parent")
                    {
                        _boardview.border_zoom.RenderTransform = new MatrixTransform(matrix);
                    }
                    else
                    {
                        _boardview.canv_inkcanvas_parent.RenderTransform = new MatrixTransform(matrix);
                    }

                    _boardManipulationPointX = matrix.OffsetX;
                    _boardManipulationPointY = matrix.OffsetY;
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// manipulations completed event for board inkcanvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void inkCanvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            try
            {
                if (_rethinkColService != null)
                    _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvasManipulated", null, manipulation: XamlWriter.Save(_boardview.canv_inkcanvas_parent.RenderTransform));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Exit current class to home screen 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e != null)
                    e.Handled = true;
                _isCloseApplication = false;
                if (_boardview.content_record_control.Visibility == Visibility.Visible)
                {
                    Messenger.Default.Send("Screen recording is Inprocess.Please make sure to close it", "Notification");
                }
                else
                {
                    ExitClass();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Handle Board closing from unhandled event
        /// </summary>
        /// <param name="data"></param>
        private void ExitClass(string data = "")
        {
            try
            {
                _boardview.canv_wallx_logo_menu.Visibility = Visibility.Collapsed;

                _boardview.audioplayer.Stop();
                _boardview.audioplayer.Source = null;

                if (_currentClassDuration != "0")
                    Messenger.Default.Send(new KeyValuePair<string, string>("Exit Board", "Are you sure you want to exit this class now ?"), "Result");
                else
                {
                    HomePageViewModel._contactsDbList = Service.GetModuleDataList<Employees>(null);
                    ResetAdhocDetails();
                    _boardview.canv_adhoc_class_details.Visibility = Visibility.Visible;
                    Canv_adhoc_inputoptions_MouseUp(_boardview.canv_adhoc_keyboard, null);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// exit board 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private void ExitBoard(string param)
        {
            if (param == "Exit Board")
            {
                ExitClass();
            }
            else
            {
                App.ExecuteMethod(() =>
                {
                    try
                    {
                        if (_actualClass != null)
                        {
                            Class dataObject = Service.GetClassData(_actualClass);

                            _currentClass = dataObject;

                            App.ExecuteMethod(LoadClassDataonBoard);
                        }

                        App.ExecuteMethod(() => { BindCurrentAgendaTime(); });

                        CheckBoardInsertorUpdate();
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ClearChildFromEditing();
                        });

                        if (!_isExistingClass)
                        {
                            DateTime dateTime = DateTime.Now;
                            _currentClass.ActualEndTime = dateTime;
                        }

                        int isUpdatedItem = Service.InsertOrUpdateDataToDB(_currentClass, CrudActions.Update);

                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _boardview.listBox_Pages.ItemsSource = null;
                            _boardview.listBox_Pages.SelectedIndex = -1;
                        });

                        // ResetMeetinginBoard();
                        if (_isCloseApplication)
                        {
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Messenger.Default.Send(new KeyValuePair<string, string>("Close Application", "Closing the application "), "Result");
                            });
                        }
                        else
                        {
                            Messenger.Default.Unregister(this);

                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Messenger.Default.Send("board", "CloseContentControlView");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        App.InsertException(ex);
                    }
                }, true);
            }
        }

        /// <summary>
        /// Set board to default status (reset all in board)
        /// </summary>
        private void ResetClassinBoard()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    ResetAll();
                    Clear();

                    Canvas canvSelectedBG = _boardview.stack_backgrounds.Children.OfType<Canvas>().ToList().FirstOrDefault(s => s.Name == Constants.DefaultBoardBG.ToLower() + "Bg") ?? _boardview.lightblueBg;

                    BoardBackground = new BitmapImage(new Uri(canvSelectedBG.Tag.ToString()));

                    _boardview.inkCanvas.Strokes.Clear();
                    _boardview.inkCanvas_Guest.Strokes.Clear();
                    _boardview.inkCanvas.Children.OfType<Border>().ToList().ForEach(s => _boardview.inkCanvas.Children.Remove(s));

                    _boardview.canv_inkcanvas_parent.RenderTransform = new MatrixTransform();
                    _boardview.border_zoom.RenderTransform = new MatrixTransform();
                    //Canvas.SetLeft(_boardview.border_zoom, 1920);
                    //Canvas.SetTop(_boardview.border_zoom, 1080);

                    FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, false, false);
                    FillColortoSelectedUtility(_boardview.canv_show_participants.Children[0] as ShapePath, false, false);
                    _boardview.stack_bottombar.Children.OfType<Canvas>().ToList().ForEach(s => FillColortoSelectedUtility(s.Children[0] as ShapePath, false, false));
                    _boardview.stack_backgrounds.Children.OfType<Canvas>().ToList().ForEach(s => s.Children[1].Visibility = Visibility.Collapsed);
                    List<Grid> colorsList = _boardview.canv_colors.Children.OfType<Grid>().ToList();
                    colorsList.ForEach(s => { (s.Children[0] as Rectangle).Tag = ""; (s.Children[1] as Image).Visibility = colorsList.IndexOf(s) != 0 ? Visibility.Collapsed : Visibility.Visible; });

                    NxgUtilities.CollapseElements(new List<FrameworkElement> { _boardview.canv_sticky_colors, _boardview.canv_wallx_logo_menu, _boardview.stackpanel_multipages, _boardview.canv_participants, _boardview.canv_library, _boardview.canv_adhoc_class_details, _boardview.canv_agenda_info, _boardview.viewbox_pdf_send, _boardview.canv_voicerecording, _boardview.canv_audioparent });

                    _boardview.audioplayer.Stop();
                    _boardview.audioplayer.Source = null;

                    ToolShowMenuComponent(_boardview.canv_marker);

                    NxgUtilities.VisibleElements(new List<FrameworkElement> { canvSelectedBG.Children[1] as Image });
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            });
        }

        #endregion

        #region Board Background

        /// <summary>
        /// go to homescreen event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="touchEventArgs"></param>
        public void canv_Logo_TouchDown(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_wallx_logo_menu.Visibility = _boardview.canv_wallx_logo_menu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// change background based on selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canv_Changebg_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas selectedItem = e.OriginalSource is Canvas ? (e.OriginalSource as Canvas) : (e.OriginalSource as FrameworkElement).Parent as Canvas;
                int SelectedIndex = (sender as StackPanel).Children.IndexOf(selectedItem);

                _boardview.stack_backgrounds.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[1] as Image).Visibility = Visibility.Collapsed);

                if (selectedItem.Children[0] is Image)
                {
                    _boardview.rect_Selected_Layer.Visibility = Visibility.Collapsed;
                    _boardview.img_Selected_Layer.Visibility = Visibility.Visible;
                    (selectedItem.Children[1] as Image).Visibility = Visibility.Visible;
                    _boardview.img_Selected_Layer.Source = (selectedItem.Children[0] as Image).Source;
                }
                else if (selectedItem.Children[0] is Rectangle)
                {
                    _boardview.img_Selected_Layer.Visibility = Visibility.Collapsed;

                    (selectedItem.Children[1] as Image).Visibility = Visibility.Visible;
                    _boardview.rect_Selected_Layer.Visibility = Visibility.Visible;
                    _boardview.rect_Selected_Layer.Fill = (selectedItem.Children[0] as Rectangle).Fill;
                }

                BoardBackground = new BitmapImage(new Uri(selectedItem.Tag.ToString()));

                _boardview.canv_wallx_logo_menu.Visibility = Visibility.Collapsed;
                //_boardview.canv_Settings.Visibility = Visibility.Collapsed;

                if (_rethinkColService != null)
                    _rethinkColService.AddorUpdateStrokeDataintoDB("BackgroundChanged", SelectedIndex.ToString(), null, _nextPageIndex);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Click On close button then closes the settings pop-up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="touchEventArgs"></param>
        public void Canv_Close_Settings_PointerPressed(object sender, MouseButtonEventArgs touchEventArgs)
        {
            _boardview.canv_WallX_Settings_Menu.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Click On close button then shows the settings pop-up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="touchEventArgs"></param>
        public void Canv_Show_Settings_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _boardview.canv_WallX_Settings_Menu.Visibility = Visibility.Visible;
            _boardview.canv_wallx_logo_menu.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// To minimize application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_show_desktop_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                e.Handled = true;
                Messenger.Default.Send("MinimizeWindow", "MinimizeWindow");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// change background color of board when other user 
        /// in collaboration change his board background  based on selected index
        /// </summary>
        /// <param name="selectedIndex"></param>
        void ChangeBoardBackgroundFromOtherUser(string selectedIndex)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Canvas selectedItem = _boardview.stack_backgrounds.Children[Convert.ToInt32(selectedIndex)] as Canvas;
                    _boardview.stack_backgrounds.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[1] as Image).Visibility = Visibility.Collapsed);
                    (selectedItem.Children[1] as Image).Visibility = Visibility.Visible;
                    BoardBackground = new BitmapImage(new Uri(selectedItem.Tag.ToString()));
                    _boardview.canv_wallx_logo_menu.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Adhoc Save

        /// <summary>
        /// close adhoc class without saving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_skip_saving_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Service.InsertOrUpdateDataToDB(_currentClass, CrudActions.Delete, _currentClass.ClassId);
                ResetAdhocDetails();
                if (_isCloseApplication)
                {
                    Messenger.Default.Send(new KeyValuePair<string, string>("Close Application", "Closing the application "), "Result");
                }
                else
                {
                    App.ExecuteMethod(ResetClassinBoard);
                    Messenger.Default.Unregister(this);
                    Messenger.Default.Send("board", "CloseContentControlView");
                }
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
        /// <param name="e"></param>
        public void Canv_clear_adhocdetails_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _boardview.txt_class_datail.Text = "";
            _boardview.txt_class_detail_email.Text = "";
            _boardview.inkcanv_class_datail.Strokes.Clear();
        }

        /// <summary>
        /// back to adhoc class title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canv_back_adhocdetails_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetAdhocDetails();
            _boardview.canv_adhoc_class_details.Visibility = Visibility.Visible;
            Canv_adhoc_inputoptions_MouseUp(_boardview.canv_adhoc_keyboard, null);
        }

        /// <summary>
        /// To select whether Hand or keyboard for entering passcode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canv_adhoc_inputoptions_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.canv_adhoc_inputoptions.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Ellipse).Visibility = Visibility.Collapsed);
                _adhocInputMethodName = (sender as Canvas).Name == "canv_adhoc_keyboard" ? NxgInputType.Keyboard : NxgInputType.Hand;
                (sender as Canvas).Children[0].Visibility = Visibility.Visible;

                switch (_adhocInputMethodName)
                {
                    case NxgInputType.Keyboard:
                        if (!_isFirstStepOver)
                        {
                            if (_boardview.inkcanv_class_datail.Strokes.Count > 0)
                                _recognizedClassName = RecognizeStrokes.RecognizeText(_boardview.inkcanv_class_datail, null);

                            if (!string.IsNullOrWhiteSpace(_recognizedClassName))
                                _boardview.txt_class_datail.Text = _recognizedClassName;

                            _boardview.txt_class_datail.Visibility = Visibility.Visible;
                            _boardview.txt_class_datail.CaretIndex = _boardview.txt_class_datail.Text.Length;
                            Keyboard.Focus(_boardview.txt_class_datail);
                        }
                        else
                        {
                            if (_boardview.inkcanv_class_datail.Strokes.Count > 0)
                                _recognizedEmail = RecognizeStrokes.RecognizeText(_boardview.inkcanv_class_datail, null);

                            if (!string.IsNullOrWhiteSpace(_recognizedEmail))
                                _boardview.txt_class_detail_email.Text = _recognizedEmail;

                            _boardview.txt_class_detail_email.Visibility = Visibility.Visible;
                            _boardview.txt_class_detail_email.CaretIndex = _boardview.txt_class_detail_email.Text.Length;
                            Keyboard.Focus(_boardview.txt_class_detail_email);
                        }
                        NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
                        _boardview.inkcanv_class_datail.Visibility = Visibility.Collapsed;
                        break;
                    case NxgInputType.Hand:
                        _boardview.inkcanv_class_datail.Visibility = Visibility.Visible;
                        _boardview.txt_class_datail.Visibility = Visibility.Collapsed;
                        _boardview.txt_class_detail_email.Visibility = Visibility.Collapsed;
                        DrawingAttributes inkIrawingAttributes = new DrawingAttributes { Color = Colors.Black, Height = 6, Width = 6 };
                        _boardview.inkcanv_class_datail.DefaultDrawingAttributes = inkIrawingAttributes;
                        break;
                }
                _boardview.inkcanv_class_datail.Strokes.Clear();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To submit class name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_class_datail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Canv_submit_details_MouseUp(null, null);
                return;
            }
        }

        /// <summary>
        ///Touch Key board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StartTouchKeyboard(object sender, RoutedEventArgs e)
        {
            NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
        }

        /// <summary>
        /// Email ids intellisense
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_email_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchEmailFromList(sender);
        }

        /// <summary>
        /// To Submit passcode an to go to further steps 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canv_submit_details_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!_isFirstStepOver)
                {
                    if (!string.IsNullOrWhiteSpace(_boardview.txt_class_datail.Text))
                    {
                        _isFirstStepOver = true;
                        _recognizedClassName = _boardview.txt_class_datail.Text;
                        _boardview.tbk_adhoc_classname.Visibility = Visibility.Collapsed;
                        _boardview.txt_class_datail.Visibility = Visibility.Collapsed;
                        _boardview.tbk_adhoc_email.Visibility = Visibility.Visible;
                        _boardview.txt_class_detail_email.Visibility = Visibility.Visible;
                        _boardview.canv_back_adhocdetails.Visibility = Visibility.Visible;
                        _boardview.txt_class_detail_email.CaretIndex = _boardview.txt_class_detail_email.Text.Length;
                        Keyboard.Focus(_boardview.txt_class_detail_email);
                        NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
                    }
                    else
                        Messenger.Default.Send("We need a title to the current class to save", "Notification");
                }
                else
                {
                    _recognizedEmail = _boardview.txt_class_detail_email.Text.ToLower();
                    if (!string.IsNullOrWhiteSpace(_recognizedEmail) & NxgUtilities.IsValidEmail(_recognizedEmail.Trim()))
                    {
                        _boardview.canv_submit_details.IsHitTestVisible = false;
                        CheckBoardInsertorUpdate();
                        ClearChildFromEditing();
                        App.ExecuteMethod(() =>
                        {
                            InsertAdhocClassDetails(_recognizedClassName, _recognizedEmail);
                            ResetAdhocDetails();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (_isCloseApplication)
                                {
                                    Messenger.Default.Send(new KeyValuePair<string, string>("Close Application", "Closing the application "), "Result");
                                }
                                else
                                {
                                    _boardview.canv_submit_details.IsHitTestVisible = true;
                                    Messenger.Default.Unregister(this);
                                    Messenger.Default.Send("board", "CloseContentControlView");
                                }
                            });
                        }, true);
                    }
                    else
                    {
                        Messenger.Default.Send("We need a valid email to proceed further", "Notification");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// insert adhoc class details data into db
        /// </summary>
        /// <param name="className"></param>
        /// <param name="toEmail"></param>
        /// <returns></returns>
        private void InsertAdhocClassDetails(string className, string toEmail)
        {
            try
            {
                int contactId = -1;
                if (!HomePageViewModel._contactsDbList.Any(s => s.Email == toEmail))
                {
                    Employees emp = new Employees { Email = toEmail, FirstName = toEmail.Substring(0, toEmail.IndexOf("@")) };
                    contactId = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                }
                else
                    contactId = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == toEmail).EmployeeId;

                List<string> dateItems = NxgUtilities.GetDateTimeasStringsList(DateTime.Now);

                int generated_class_id = _currentClass.ClassId;
                string class_date = (dateItems[2] + "-" + dateItems[1] + "-" + dateItems[0]);
                string canv_class_time = NxgUtilities.GetTimeFromDate(_currentClass.StartTime);
                string endingTime = DateTime.Now.ToString("hh:mm tt");

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    _currentClass.ClassName = className;
                    _currentClass.EndTime = (DateTime.Now - _currentClass.StartTime).TotalMinutes >= 1 ? DateTime.Now : DateTime.Now.AddMinutes(1);
                    _currentClass.ActualStartTime = _currentClass.StartTime;
                    _currentClass.ActualEndTime = (DateTime.Now - _currentClass.StartTime).TotalMinutes >= 1 ? DateTime.Now : DateTime.Now.AddMinutes(1);
                    _currentClass.ClassCategory = ClassCategoryType.Others.ToString();
                    _currentClass.ClassType = ClassScheduleType.OneTimeClass.ToString();
                    _currentClass.OrganizerMailId = toEmail;
                    _currentClass.Password = _isPasswordRequired ? NxgUtilities.GetRandomPassword(6) : "";
                }));

                int selectedClassDbId = Service.InsertOrUpdateDataToDB(_currentClass, CrudActions.Update);

                if (selectedClassDbId > 0)
                {
                    bool isInserted = NewClassViewModel.GenerateEmail(className, _currentClass.Password, class_date, _currentClass.StartTime.ToString("hh:mm tt"), _currentClass.EndTime.ToString("hh:mm tt"), Constants.LocationName, ParticipantsList, AgendasList, toEmail, generated_class_id, "accessed");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// reset adhoc class details
        /// </summary>
        public void ResetAdhocDetails()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _boardview.canv_adhoc_class_details.Visibility = Visibility.Collapsed;
                    _boardview.txt_class_datail.Visibility = Visibility.Visible;
                    _boardview.inkcanv_class_datail.Visibility = Visibility.Collapsed;
                    _boardview.canv_back_adhocdetails.Visibility = Visibility.Collapsed;
                    _boardview.canv_participants.Visibility = Visibility.Collapsed;
                    _boardview.canv_agenda_popup.Visibility = Visibility.Collapsed;

                    _boardview.txt_class_detail_email.Text = "";

                    _recognizedClassName = string.Empty;
                    _recognizedEmail = string.Empty;
                    _isFirstStepOver = false;
                    _nextProjectSaveTime = string.Empty;
                    _boardview.tbk_adhoc_classname.Visibility = Visibility.Visible;
                    _boardview.txt_class_datail.Visibility = Visibility.Visible;
                    _boardview.tbk_adhoc_email.Visibility = Visibility.Collapsed;
                    _boardview.txt_class_detail_email.Visibility = Visibility.Collapsed;
                    _boardview.inkcanv_class_datail.Strokes.Clear();
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            });
        }

        #endregion

        #region Agendas

        List<Employees> _taskMembersList = new List<Employees>();

        /// <summary>
        /// show selected agenda details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CurrentAgendaEllipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.canv_agenda_details.SetValue(Canvas.LeftProperty, e.GetPosition(_boardview.canv_main).X - 180);
                _boardview.canv_current_agenda_completed.Visibility = Visibility.Collapsed;
                if (SelectedAgenda != null && (SelectedAgenda.ActualStartTime == default(DateTime) || SelectedAgenda.ActualEndTime == default(DateTime)))
                {
                    _currentClass.AgendaList.ToList().ForEach(s => s.IsSelected = false);
                    _currentClass.AgendaList.FirstOrDefault(s => s.AgendaId == int.Parse(Convert.ToString((sender as Canvas).Tag))).IsSelected = true;
                    AgendasList = null;
                    AgendasList = _currentClass.AgendaList;
                    SelectedAgenda = _currentClass.AgendaList.FirstOrDefault(s => s.AgendaId == int.Parse(Convert.ToString((sender as Canvas).Tag)));

                    if (NxgUtilities.GetTimeFromDate(SelectedAgenda.ActualStartTime) != "12:00 AM" && SelectedAgenda.IsRunning != "completed" && !string.IsNullOrWhiteSpace((sender as Canvas).Name))
                    {
                        App.ExecuteMethod(() => { BindCurrentAgendaTime(); });

                        (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Visible;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[0].Visibility = Visibility.Collapsed;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[1].Visibility = Visibility.Visible;
                        _boardview.txt_current_agenda_actualtime.Text = TimeSpan.Parse(SelectedAgenda.ActualDuration).TotalMinutes.ToString("00");
                    }
                    else if (SelectedAgenda.ActualEndTime == default(DateTime) && SelectedAgenda.IsRunning == "true")
                    {
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Visible;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[0].Visibility = Visibility.Collapsed;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[1].Visibility = Visibility.Visible;
                        _boardview.txt_current_agenda_actualtime.Text = TimeSpan.Parse(SelectedAgenda.ActualDuration).TotalMinutes.ToString("00");
                    }
                    else if (SelectedAgenda.ActualEndTime == default(DateTime) && SelectedAgenda.IsRunning != "completed")
                    {
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Visible;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[0].Visibility = Visibility.Visible;
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Children[1].Visibility = Visibility.Collapsed;
                        _boardview.txt_current_agenda_actualtime.Text = "0";
                    }
                    else if (SelectedAgenda.IsRunning != "true")
                    {
                        (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Collapsed;
                        _boardview.canv_current_agenda_completed.Visibility = Visibility.Visible;
                        _boardview.txt_current_agenda_actualtime.Text = TimeSpan.Parse(SelectedAgenda.ActualDuration).TotalMinutes.ToString("00");
                    }
                }
                else
                {
                    (_boardview.canv_agenda_details.Children[1] as Canvas).Visibility = Visibility.Collapsed;
                    _boardview.canv_current_agenda_completed.Visibility = Visibility.Visible;
                    _boardview.txt_current_agenda_actualtime.Text = TimeSpan.Parse(SelectedAgenda.ActualDuration).TotalMinutes.ToString("00");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// start selected agenda item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CurrentAgenda_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentClass.AgendaList.Any(s => s.IsRunning == "true") && _currentClass.AgendaList.FirstOrDefault(s => s.IsRunning == "true").AgendaId != SelectedAgenda.AgendaId)
            {
                Messenger.Default.Send(new KeyValuePair<string, string>("Complete current agenda", "Would you like to complete current agenda ?"), "Result");
            }
            else
            {
                App.ExecuteMethod(() => { StartAgendaItem(); });
            }
        }

        private void StartAgendaItem(string param = "")
        {
            Application.Current.Dispatcher.InvokeAsync((Action)(() =>
            {
                if (SelectedAgenda != null)
                {
                    object sender = _boardview.canv_current_agenda_start;

                    if (Convert.ToString((sender as Canvas).Tag) == SelectedAgenda.AgendaId.ToString() && SelectedAgenda.ActualEndTime != default(DateTime))
                    {
                        return;
                    }

                    Agendas presentAgenda = SelectedAgenda;
                    if (_currentClass.AgendaList.Any(s => s.IsRunning == "true"))
                    {
                        SelectedAgenda = _currentClass.AgendaList.FirstOrDefault(s => s.IsRunning == "true");
                    }

                    if (SelectedAgenda != null && SelectedAgenda.IsRunning == "true")
                    {
                        App.ExecuteMethod(() => { BindCurrentAgendaTime(); });
                        if (presentAgenda.AgendaId != SelectedAgenda.AgendaId)
                        {
                            SelectedAgenda = presentAgenda;
                            App.ExecuteMethod(() => { StartAgendaItem(); });
                        }
                        else
                        {
                            (sender as Canvas).Visibility = Visibility.Collapsed;
                            _boardview.canv_current_agenda_completed.Visibility = Visibility.Visible;
                        }
                    }
                    else if (SelectedAgenda != null)
                    {
                        _currentClass.AgendaList.FirstOrDefault((Agendas s) => s.AgendaId == SelectedAgenda.AgendaId).ActualStartTime = SelectedAgenda.ActualStartTime = DateTime.Now;
                        _currentClass.AgendaList.FirstOrDefault(s => s.AgendaId == SelectedAgenda.AgendaId).IsRunning = SelectedAgenda.IsRunning = "true";

                        (sender as Canvas).Visibility = Visibility.Visible;
                        _boardview.canv_current_agenda_completed.Visibility = Visibility.Collapsed;
                        (sender as Canvas).Children[0].Visibility = Visibility.Collapsed;
                        (sender as Canvas).Children[1].Visibility = Visibility.Visible;

                        if (_boardview.listbox_AgendaItems.SelectedIndex > 0)
                        {
                            App.ExecuteMethod(() => { AddPage(); });
                        }
                    }

                    _agendaEndingAlertDisplayed = false;
                    _agendaOverAlertDisplayed = false;
                }
            }));
        }

        /// <summary>
        /// bind current agenda actul start time, end time & duration
        /// </summary>
        void BindCurrentAgendaTime(string param = "")
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                if (SelectedAgenda != null && _currentClass.AgendaList.Any(s => s.IsRunning == "true"))
                {
                    Agendas item = _currentClass.AgendaList.FirstOrDefault(s => s.IsRunning == "true");
                    item.ActualEndTime = dateTime;
                    item.IsRunning = "completed";
                    Service.InsertOrUpdateDataToDB(item, CrudActions.Update);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ContentPresenter contentPresenter = TemplateModifier.GetContentPresenter(_boardview.listbox_AgendaItems, _currentClass.AgendaList.IndexOf(item));
                        if (contentPresenter != null)
                        {
                            Canvas canv = contentPresenter.ContentTemplate.FindName("canv_agenda", contentPresenter) as Canvas;
                            if (canv != null)
                            {
                                ((canv.Children[2] as Canvas).Children[0] as Ellipse).Fill = (Brush)new BrushConverter().ConvertFromString("#FF4C9658");
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// mouse down event for display class details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AgendaInfoPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            App.ExecuteMethod(GetAgendaInfoData, true);
        }

        /// <summary>
        /// mouse down event for hide class details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AgendaInfoCloseMouseDown(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_agenda_info.RenderTransform = new MatrixTransform(1, 0, 0, 1, 0, 0);
            _boardview.canv_agenda_info.Visibility = Visibility.Collapsed;
        }

        private void GetAgendaInfoData()
        {
            Application.Current.Dispatcher.InvokeAsync((Action)(() =>
            {
                if (SelectedAgenda != null)
                {
                    //AgendaNoteList = _currentMeeting.NoteList.Where(s => s.Note_Type.ToLower() == "note" && s.Agenda_pk_id == SelectedAgenda.AgendaId).ToList();
                    //AgendaTaskList = _currentMeeting.TaskList.Where(s => s.Agenda_pk_id == SelectedAgenda.AgendaId).ToList();

                    AgendaHeader = SelectedAgenda.AgendaName + "  " + SelectedAgenda.StartTime.ToString("hh:mm tt") + " TO " + SelectedAgenda.EndTime.ToString("hh:mm tt") + " (" + (!string.IsNullOrEmpty(Convert.ToString(SelectedAgenda.Duration)) ? TimeSpan.Parse(SelectedAgenda.Duration).TotalMinutes.ToString("00") : "00") + " MINS)";

                    Task.Run(() => { GetlibraryThumbnails(); });

                    _boardview.canv_agenda_info.Visibility = Visibility.Visible;
                    _boardview.canv_agenda_popup.Visibility = Visibility.Collapsed;
                }
            }));
        }

        public void SaveDecision_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.ExecuteMethod((Action)(() =>
                {
                    App.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        string openedWindowName = _boardview.tbk_agenda_window_name.Text;

                        if (openedWindowName == "Decisions")
                        {
                            if (_boardview.inkcanv_decision.Strokes.Count == 0)
                            {
                                Messenger.Default.Send("No Ink data found..!", "Notification");
                                return;
                            }
                        }
                        else if (openedWindowName == "Notes")
                        {
                            if (string.IsNullOrWhiteSpace(_boardview.txt_note.Text))
                            {
                                Messenger.Default.Send("No data found..!", "Notification");
                                return;
                            }
                        }
                        else if (openedWindowName == "Tasks")
                        {
                            if (string.IsNullOrWhiteSpace(_boardview.txt_note.Text))
                            {
                                Messenger.Default.Send("No data found..!", "Notification");
                                return;
                            }
                            else if (_taskMembersList == null || _taskMembersList.Count == 0)
                            {
                                Messenger.Default.Send("Please assign to at least one Class room", "Notification");
                                return;
                            }
                        }

                        _boardview.canv_agenda_popup.Visibility = Visibility.Collapsed;

                        LibraryThumbs classNoteData = new LibraryThumbs()
                        {
                            TextInfo = openedWindowName == "Decisions" ? RecognizeStrokes.RecognizeText(_boardview.inkcanv_decision, null) : _boardview.txt_note.Text,
                            StrokeData = openedWindowName == "Decisions" ? XamlWriter.Save(_boardview.inkcanv_decision.Strokes) : null,
                            AttachmentType = openedWindowName == "Notes" ? AttachmentType.Note.ToString() : openedWindowName == "Tasks" ? AttachmentType.Task.ToString() : AttachmentType.Decision.ToString(),
                            ParticipantId = _currentClass.ParticipantList.FirstOrDefault(s => s.EmployeeId == SelectedAgenda.EmployeeId).ParticipantId,
                            AgendaId = SelectedAgenda.AgendaId,
                            ClassId = _currentClass.ClassId,
                            CreatedDateTime = DateTime.Now,
                            AssignedEmployeePKIDs = openedWindowName == "Tasks" ? string.Join(",", _taskMembersList.Select((Func<Employees, int>)(s => (int)s.EmployeeId))) : null,
                            LibraryThumbId = !string.IsNullOrWhiteSpace(Convert.ToString(_boardview.txt_note.Tag)) ? Convert.ToInt32(_boardview.txt_note.Tag) : 0
                        };

                        classNoteData.AttachmentTypeId = (int)Enum.Parse(typeof(AttachmentType), classNoteData.AttachmentType);

                        int insertedId = Service.InsertOrUpdateDataToDB(classNoteData, classNoteData.LibraryThumbId == 0 ? CrudActions.Create : CrudActions.Update);

                        if (insertedId > 0)
                        {
                            classNoteData.LibraryThumbId = insertedId;
                            _taskMembersList = new List<Employees>();
                            _boardview.txt_note.Tag = "";
                            App.ExecuteMethod(this.GetlibraryThumbnails);
                        }
                        else
                        {
                            Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification");
                        }
                    }));
                }), true);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void ClearDecision_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _boardview.inkcanv_decision.Strokes.Clear();
            _boardview.txt_note.Text = "";
            SearchedContactText = "";
            _boardview.lb_task_members.ItemsSource = null;
            _taskMembersList = new List<Employees>();
        }

        public void CloseDecision_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_agenda_popup.Visibility = Visibility.Collapsed;
            _boardview.lb_task_members.Visibility = Visibility.Visible;
            _taskMembersList = new List<Employees>();
            SearchedContactText = "";
        }

        private void AddTaskMember(string taskMemberEmailID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taskMemberEmailID))
                {
                    Messenger.Default.Send("Email should not be Empty", "Notification");
                    return;
                }
                if (!NxgUtilities.IsValidEmail(taskMemberEmailID))
                {
                    Messenger.Default.Send("Please enter Valid Email", "Notification");
                    return;
                }
                if (_taskMembersList != null && _taskMembersList.Count > 0 && _taskMembersList.Any(s => s.Email == taskMemberEmailID))
                {
                    Messenger.Default.Send("Already assigned to this Class room..!", "Notification");
                    return;
                }
                if (!HomePageViewModel._contactsDbList.Any(s => s.Email == taskMemberEmailID))
                {
                    string name = taskMemberEmailID.Substring(0, taskMemberEmailID.IndexOf("@"));
                    Employees emp = new Employees { Email = taskMemberEmailID, FirstName = name };
                    int contactId = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                    if (contactId > 0)
                    {
                        emp.EmployeeId = contactId;
                        HomePageViewModel._contactsDbList.Add(emp);
                    }
                }

                Employees taskMember = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email.ToLower() == taskMemberEmailID);
                if (taskMember != null)
                {
                    _taskMembersList.Add(taskMember);

                    _boardview.lb_task_members.ItemsSource = null;
                    _boardview.lb_task_members.ItemsSource = _taskMembersList;
                    _boardview.lb_task_contact_search.SelectedIndex = -1;
                    SearchedContactText = "";
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void txt_task_member_KeyDown(object sender, KeyEventArgs e)
        {
            SearchEmailFromList(sender);
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AddTaskMember(SearchedContactText.ToLower().Trim());
                return;
            }
        }

        public void tbk_add_task_member_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string taskMemberEmailID = SearchedContactText.ToLower().Trim();
                AddTaskMember(taskMemberEmailID);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void tbk_cancel_taskmember_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Employees selectedEmp = ((sender as Canvas).Tag as Employees);
                if (selectedEmp != null)
                {
                    _taskMembersList.Remove(selectedEmp);

                    _boardview.lb_task_members.ItemsSource = null;
                    _boardview.lb_task_members.ItemsSource = _taskMembersList;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Agendas

        #region Pages

        /// <summary>
        /// Add page event to create new page with saving current page data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddPage_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Messenger.Default.Send(new KeyValuePair<string, string>("Add Canvas", "This option will add a new canvas, do you wish to proceed"), "Result");
        }

        private bool _addPageInProgress = false;
        /// <summary>
        /// Adding board data into a page in pages list
        /// </summary>
        /// <param name="isFirstTime"></param>
        /// <returns></returns>
        private async void AddPage(string param = null)
        {
            try
            {
                if (!_addPageInProgress)
                {
                    await Task.Run(() =>
                    {
                        _addPageInProgress = true;
                        BoardAnnotations addedPage = null;
                        try
                        {
                            if (param != null && param != "Add Canvas" && param != "Duplicate Canvas")
                                addedPage = XamlReader.Parse(param) as BoardAnnotations;
                        }
                        catch (Exception)
                        {
                            addedPage = null;
                        }
                        CheckBoardInsertorUpdate(param);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ClearChildFromEditing();
                        });
                        if (_selectedPageIndex != -1)
                        {
                            ResetClassinBoard();
                            _selectedPageIndex = -1;
                            _nextPageIndex++;
                            CheckBoardInsertorUpdate(param);

                            if (_rethinkColService != null && addedPage == null)
                            {
                                BoardAnnotations page = _pagesList.Last();
                                if (param != null && param.ToLower().StartsWith("duplicate"))
                                    page.Tag = "DuplcatePage";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (_selectedCanvasItem != null)
                                        page.Index = Convert.ToInt32(_selectedCanvasItem.Tag);
                                });
                                string pageData = XamlWriter.Save(page);
                                _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvasAdded", pageData, null, _nextPageIndex);
                            }
                        }
                        _selectedPageIndex = _nextPageIndex;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CanvasCount = _pagesList.Count;
                        });
                        if ((param != null && (param.ToLower().StartsWith("duplicate")) || (addedPage != null && addedPage.Tag == "DuplcatePage")))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _boardview.listBox_Pages.ItemsSource = null;
                                _boardview.listBox_Pages.ItemsSource = _pagesList;
                                if (addedPage != null)
                                    LoadPageDatafromDB(addedPage.Index);
                                else
                                    LoadPageDatafromDB(Convert.ToInt32(_selectedCanvasItem.Tag), true);
                                FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, false, false);
                            });
                        }
                        _addPageInProgress = false;
                    });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// check board data insert or update in db and send data to db
        /// </summary>
        /// <returns></returns>
        private bool CheckBoardInsertorUpdate(string param = null)
        {
            int datasaved = -1;

            try
            {
                BoardAnnotations addedPage = null;
                try
                {
                    if (param != null && param != "Add Canvas" && param != "Duplicate Canvas")
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            addedPage = XamlReader.Parse(param) as BoardAnnotations;
                        });
                    }
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                    addedPage = null;
                }

                BoardAnnotations page = null;
                if (_selectedPageIndex == -1)
                    page = new BoardAnnotations { PageIndex = _nextPageIndex, ClassId = _currentClass.ClassId };
                else if (_selectedPageIndex > -1)
                {
                    page = _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex);
                    if (page == null)
                        page = _pagesList.FirstOrDefault(s => s.PageIndex == Convert.ToInt32((_boardview.listBox_Pages.Items[_selectedPageIndex] as Grid).Tag));
                }

                DispatcherOperation disOp = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    page.InkStrokes = XamlWriter.Save(_boardview.inkCanvas);
                    page.Manipulation = XamlWriter.Save(_boardview.canv_inkcanvas_parent.RenderTransform);

                    StrokeCollection allStrokes = _boardview.inkCanvas_Guest.Strokes.Clone();
                    allStrokes.Add(_boardview.inkCanvas.Strokes.Clone());
                    page.TotalInkStrokes = XamlWriter.Save(allStrokes);
                });

                DispatcherOperationStatus status = disOp.Status;
                while (disOp.Status != DispatcherOperationStatus.Completed)
                    status = disOp.Wait(TimeSpan.FromMilliseconds(1000));

                disOp = Application.Current.Dispatcher.InvokeAsync((Action)(() =>
                {
                    if (addedPage != null)
                    {
                        page = addedPage;
                        datasaved = Service.InsertOrUpdateDataToDB(page, CrudActions.Update);
                    }
                    else
                    {
                        datasaved = Service.InsertOrUpdateDataToDB(page, _selectedPageIndex == -1 ? CrudActions.Create : CrudActions.Update);
                    }

                    if (_pagesList != null && datasaved != -1 && _selectedPageIndex == -1)
                    {
                        page.AnnotationId = datasaved;
                        _pagesList.Add(page);
                    }
                }));

                status = disOp.Status;
                while (disOp.Status != DispatcherOperationStatus.Completed)
                    status = disOp.Wait(TimeSpan.FromMilliseconds(1000));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return (datasaved != -1);
        }

        /// <summary>
        /// load data from db
        /// </summary>
        /// <param name="PageIndex"></param>
        /// <returns></returns>
        private void LoadPageDatafromDB(int PageIndex, bool isFromDuplicate = false, BoardAnnotations boardAnnotation = null)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                try
                {
                    BoardAnnotations page = _pagesList.FirstOrDefault(s => s.PageIndex == PageIndex);
                    if (page != null)
                    {
                        if (boardAnnotation == null)
                        {
                            InkCanvas inkcanv = XamlReader.Parse(page.InkStrokes) as InkCanvas;
                            StrokeCollection allInkStrokes = null;
                            if (page.TotalInkStrokes != null)
                                allInkStrokes = XamlReader.Parse(page.TotalInkStrokes) as StrokeCollection;
                            else
                                allInkStrokes = inkcanv.Strokes;

                            if (allInkStrokes != null && allInkStrokes.Count > 0)
                            {
                                _boardview.inkCanvas.Strokes = new StrokeCollection(allInkStrokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == RethinkService._macAddress).ToList());
                                _boardview.inkCanvas_Guest.Strokes = new StrokeCollection(allInkStrokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) != RethinkService._macAddress).ToList());
                            }
                        }

                        List<ImageAnnotations> imageAnnotationList = Service.GetModuleDataList<ImageAnnotations>(_currentClass, (int)page.AnnotationId).Where((Func<ImageAnnotations, bool>)(s => (bool)(s.BoardAnnotationId == page.AnnotationId && !_boardview.inkCanvas.Children.OfType<Border>().Any(j => Convert.ToInt32(j.Tag) == s.AnnotationId)))).ToList();
                        if (imageAnnotationList != null && imageAnnotationList.Count > 0)
                        {
                            AddChildToBoard(imageAnnotationList.Where((Func<ImageAnnotations, bool>)(s => s.BoardAnnotationId == page.AnnotationId)).ToList(), isFromDuplicate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            }));
        }

        /// <summary>
        /// To show pages list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void MultiPages_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await Task.Run(() =>
            {
                try
                {
                    CheckBoardInsertorUpdate();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _boardview.listBox_Pages.ItemsSource = null;
                        _boardview.listBox_Pages.ItemsSource = _pagesList;
                        CanvasCount = _pagesList.Count;
                        _boardview.listBox_Pages.SelectedIndex = _pagesList.IndexOf(_pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex));
                        _boardview.stackpanel_multipages.Visibility = Visibility.Visible;
                        Canvas.SetLeft(_boardview.stackpanel_multipages, e.GetPosition(_boardview.canv_inkcanvas_parent).X - 160);
                        FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, true, false);
                    });
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            });
        }

        /// <summary>
        /// to close pages list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClosePages_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                _boardview.stackpanel_multipages.Visibility = Visibility.Collapsed;
                FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, false, false);
                if (_boardview.listBox_Pages.Items.Count > 0)
                    _nextPageIndex = ((WallX.Services.BoardAnnotations)(_boardview.listBox_Pages.Items[_boardview.listBox_Pages.Items.Count - 1])).PageIndex;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>MultiPages_PointerPressed
        /// Pages listbox selected changed event to add pagedata on board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;

            App.ExecuteMethod(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        BoardAnnotations selectedItem = _boardview.listBox_Pages.SelectedItem as BoardAnnotations;
                        if (selectedItem != null && _selectedPageIndex != selectedItem.PageIndex)
                        {
                            CheckBoardInsertorUpdate();
                            _isFromPageChange = true;
                            ResetClassinBoard();
                            _isFromPageChange = false;
                            _selectedPageIndex = selectedItem.PageIndex;
                            LoadPageDatafromDB(_selectedPageIndex);
                            _boardview.stackpanel_multipages.Visibility = Visibility.Visible;
                            if (_rethinkColService != null && !_isPageSelectionChangedByOthers)
                                _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvasSelectionChanged", null, null, -1, _selectedPageIndex);
                            _isPageSelectionChangedByOthers = false;
                            FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, true, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.InsertException(ex);
                    }
                });
            });
        }

        private void ChangeToSelectedPage(int pageIndex)
        {
            try
            {
                _isPageSelectionChangedByOthers = true;
                // _selectedPageIndex = pageIndex;
                App.Current.Dispatcher.Invoke(() =>
                {
                    _boardview.listBox_Pages.SelectedIndex = _boardview.listBox_Pages.Items.OfType<BoardAnnotations>().ToList().FindIndex(a => a.PageIndex == pageIndex);
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// page delete options pointer pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PageDelete_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_actualClass == null || (_actualClass != null && _currentClass != null && _currentClass.ClassId == _actualClass.ClassId))
                {
                    Grid item = (((sender as TextBlock).Parent as Grid).Parent as Grid).Children[2] as Grid;
                    if (item != null)
                    {
                        if (item.Visibility == Visibility.Visible)
                            item.Visibility = Visibility.Collapsed;
                        else
                            item.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// page delete pointer pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CanvDeletePage_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            _selectedCanvasItem = ((sender as TextBlock).Parent as Grid).Parent as Grid;
            if (_boardview.listBox_Pages.Items.Count > 1)
                Messenger.Default.Send(new KeyValuePair<string, string>("Delete Canvas", "This action will delete the canvas with all the strokes, images and annotations completely. Do you wish to proceed?"), "Result");
            else
                Messenger.Default.Send("We need at least one canvas in place.", "Notification");
        }

        /// <summary>
        /// Page delete method with confirmation
        /// </summary>
        private void DeleteCanvas(string param)
        {
            App.ExecuteMethod(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_selectedCanvasItem != null || param == "DeletePageFromOthers")
                        {
                            BoardAnnotations page = _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex);
                            if (page != null)
                            {
                                if (Service.InsertOrUpdateDataToDB(page, CrudActions.Delete, page.AnnotationId) > 0)
                                {
                                    int itemIndex = _boardview.listBox_Pages.SelectedIndex;
                                    _pagesList.Remove(page);
                                    _boardview.listBox_Pages.ItemsSource = null;
                                    _boardview.listBox_Pages.ItemsSource = _pagesList;
                                    CanvasCount = _pagesList.Count;
                                    _boardview.listBox_Pages.SelectedIndex = _pagesList.IndexOf(_pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex));
                                    ResetClassinBoard();
                                    int newPageIndex = _boardview.listBox_Pages.Items.Count - 1 >= itemIndex ? itemIndex : itemIndex - 1;
                                    _selectedPageIndex = Convert.ToInt32((_boardview.listBox_Pages.Items[newPageIndex] as BoardAnnotations).PageIndex);
                                    LoadPageDatafromDB(_selectedPageIndex);
                                    if (_rethinkColService != null && param != "DeletePageFromOthers")
                                        _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvasDeleted", null, null, -1, itemIndex);
                                    FillColortoSelectedUtility(_boardview.canv_show_pages.Children[0] as ShapePath, false, false);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.InsertException(ex);
                    }
                });
            });
        }

        /// <summary>
        /// duplicate canvas create event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CanvDuplicatePage_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            _selectedCanvasItem = ((sender as TextBlock).Parent as Grid).Parent as Grid;
            Messenger.Default.Send(new KeyValuePair<string, string>("Duplicate Canvas", "This action will duplicate the canvas with all the strokes, images and annotations completely. Do you wish to proceed?"), "Result");
        }

        #endregion

        #region Participants

        /// <summary>
        /// display class participants data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_show_participants_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (_boardview.canv_participants.Visibility == Visibility.Visible)
                {
                    _boardview.canv_participants.Visibility = Visibility.Collapsed;
                    _boardview.listbox_Participants.Visibility = Visibility.Visible;
                    FillColortoSelectedUtility(_boardview.canv_show_participants.Children[0] as ShapePath, false, false);
                    Canvas.SetLeft(_boardview.canv_participants, e.GetPosition(_boardview.canv_inkcanvas_parent).X - 150);
                }
                else if (_boardview.canv_participants.Visibility == Visibility.Collapsed)
                {
                    _boardview.canv_participants.Visibility = Visibility.Visible;
                    _boardview.canv_agenda_popup.Visibility = Visibility.Collapsed;
                    _boardview.txtAddParticipant.Text = "";
                    FillColortoSelectedUtility(_boardview.canv_show_participants.Children[0] as ShapePath, true, false);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// update participant attendance 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void participant_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Participants currentitem = _currentClass.ParticipantList.FirstOrDefault(s => s.Employee.Email == Convert.ToString(((sender as Canvas).Children[6] as TextBlock).Tag).Trim());
                if (currentitem != null)
                {
                    currentitem.IsAttended = ((sender as Canvas).Children[3] as Image).Visibility == Visibility.Visible ? false : true;
                    int isUpdated = Service.InsertOrUpdateDataToDB(currentitem, CrudActions.Update);
                    if (isUpdated > 0)
                    {
                        ParticipantsList = null;
                        ParticipantsList = _currentClass.ParticipantList;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Handle event for pressing enter key in participants adding box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txtAddParticipant_KeyDown(object sender, KeyEventArgs e)
        {
            SearchEmailFromList(sender);
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AddParticipant(_boardview.txtAddParticipant.Text);
                return;
            }
        }

        /// <summary>
        /// add new participant pointer pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txtAddParticipant_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AddParticipant(_boardview.txtAddParticipant.Text);
        }

        /// <summary>
        /// add new participant pointer pressed event
        /// </summary>
        /// <param name="newParticiantEmail"></param>
        private void AddParticipant(string newParticiantEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newParticiantEmail))
                {
                    Messenger.Default.Send("Class room email id should not empty", "Notification");
                }
                else
                {
                    string participantEmail = newParticiantEmail.ToLower().Trim();

                    if (NxgUtilities.IsValidEmail(participantEmail))
                    {
                        HomePageViewModel._contactsDbList = Service.GetModuleDataList<Employees>(null);

                        Participants participant = null;

                        int participantUserInsertedId = -1;
                        string name = participantEmail.Substring(0, participantEmail.IndexOf("@"));
                        if (!HomePageViewModel._contactsDbList.Any(s => s.Email == participantEmail))
                        {
                            Employees emp = new Employees { Email = participantEmail, FirstName = name };
                            int contactId = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                            if (contactId > 0)
                            {
                                emp.EmployeeId = contactId;
                                HomePageViewModel._contactsDbList.Add(emp);
                                participant = new Participants { EmployeeId = contactId, ClassId = _currentClass.ClassId, Employee = emp };
                                participantUserInsertedId = Service.InsertOrUpdateDataToDB(participant, CrudActions.Create);
                            }
                        }
                        else
                        {
                            if (ParticipantsList == null || (ParticipantsList != null && !ParticipantsList.Any(s => s.Employee.Email == participantEmail)))
                            {
                                Employees emp = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == participantEmail);
                                if (emp != null)
                                {
                                    participant = new Participants { EmployeeId = emp.EmployeeId, ClassId = _currentClass.ClassId, Employee = emp };
                                    participantUserInsertedId = Service.InsertOrUpdateDataToDB(participant, CrudActions.Create);
                                }
                            }
                            else
                            {
                                _boardview.txtAddParticipant.Text = "";
                                Messenger.Default.Send("The Class room already onboard", "Notification");
                                return;
                            }
                        }

                        if (participantUserInsertedId > 0 && participant != null)
                        {
                            if (_currentClass.ParticipantList != null && _currentClass.ParticipantList.Count == 0)
                                participant.IsOrganizer = true;
                            participant.ParticipantId = participantUserInsertedId;
                            _currentClass.ParticipantList.Add(participant);
                            ParticipantsList = null;
                            ParticipantsList = _currentClass.ParticipantList;
                            _boardview.txtAddParticipant.Text = "";
                            AttendanceCount = Convert.ToString(ParticipantsList.Count);
                        }
                    }
                    else
                    {
                        Messenger.Default.Send("A valid email is required to add the Class room", "Notification");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// participants options pointer pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void participant_options_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Border borderDelete = ((sender as Canvas).Parent as Canvas).Children[8] as Border;
            if (borderDelete != null)
            {
                if (borderDelete.Visibility == Visibility.Collapsed)
                    borderDelete.Visibility = Visibility.Visible;
                else
                    borderDelete.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// delete selected participants from board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void delete_participant_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                Participants emp = _currentClass.ParticipantList.FirstOrDefault(s => s.Employee.Email == Convert.ToString((((sender as Border).Parent as Canvas).Children[6] as TextBlock).Tag));
                if (Service.InsertOrUpdateDataToDB(emp, CrudActions.Delete, emp.ParticipantId) > 0)
                {
                    _currentClass.ParticipantList.Remove(emp);
                    ParticipantsList = null;
                    ParticipantsList = _currentClass.ParticipantList;
                    AttendanceCount = Convert.ToString(ParticipantsList.Count);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Editing tools

        /// <summary>
        /// tool selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Toolsmenu_Selection_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _selectedToolMenuItem = e.OriginalSource is Canvas ? e.OriginalSource as Canvas : (e.OriginalSource as FrameworkElement).Parent as Canvas;
                if (_selectedToolMenuItem != null)
                {
                    ToolShowMenuComponent(_selectedToolMenuItem);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void Toolsmenu_Duplicate_Stroke(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ToolShowMenuComponent(_boardview.canv_duplicate);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// show menu component based on selected tool
        /// </summary>
        /// <param name="selectedMenuName"></param>
        public void ToolShowMenuComponent(Canvas menuitem, string senderType = null)
        {
            try
            {
                string selectedMenuName = string.Empty;
                DisableMagnifier();
                if (menuitem != null)
                {
                    if (!(new List<string>() { "canv_menu_browser_", "canv_video_call_zoomus", "canv_save", "canv_clearboard", "canv_delete", "canv_Redo", "canv_Undo", "canv_show_mindmap", "canv_duplicate" }.Contains(menuitem.Name)))
                    {
                        //_boardview.UndoRedo.Children.OfType<Canvas>().ToList().ForEach(k => FillColortoSelectedUtility(k.Children[0] as ShapePath, false, true));
                        //_boardview.canv_saveNclear.Children.OfType<Canvas>().ToList().ForEach(k => FillColortoSelectedUtility(k.Children[0] as ShapePath, false, true));
                        //_boardview.stackpanel_tool_menu.Children.OfType<Canvas>().Where(s => !(new List<string>() { "canv_menu_browser_" }.Contains(s.Name))).ToList().ForEach(k => FillColortoSelectedUtility(k.Children[0] as ShapePath, false, true));
                        (new List<Canvas>() { _boardview.canv_marker, _boardview.canv_eraser, _boardview.canv_highlighter, _boardview.canv_hand, _boardview.canv_eraser_stroke }).ForEach(k => FillColortoSelectedUtility(k.Children[0] as ShapePath, false, true));

                        FillColortoSelectedUtility(menuitem.Children[0] as ShapePath, true, true);

                        _boardview.vb_board_zoom_display.Visibility = Visibility.Collapsed;
                    }

                    if (!new List<string> { "canv_marker", "canv_eraser", "canv_highlighter", "canv_eraser_stroke", "canv_duplicate" }.Contains(menuitem.Name))
                    {
                        _boardview.canv_colors.Visibility = Visibility.Collapsed;
                        _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                    }

                    selectedMenuName = menuitem.Name;
                }
                else if (senderType != null)
                {
                    selectedMenuName = senderType;
                }

                switch (selectedMenuName)
                {
                    case "canv_marker":
                        Inkcolor_Selection_Pointerpressed(_boardview.canv_colors.Children.Cast<Grid>().FirstOrDefault(s => (s as Grid).Children[1].Visibility == Visibility.Visible).Children[0], null);
                        Marker(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas, SetStrokeSelectionbySize);
                        if (_boardview.canv_colors.Visibility != Visibility.Visible && !_isFromPageChange)
                        {
                            _boardview.canv_colors.Visibility = Visibility.Visible;
                            _boardview.canv_strokes.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            _boardview.canv_colors.Visibility = Visibility.Collapsed;
                            _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                        }
                        //_boardview.canv_strokes.SetValue(Canvas.TopProperty, (double)39);
                        _boardview.canv_strokes.Margin = new Thickness(-215, -225, 0, 0);

                        _inkToolName = InkToolName.Marker;
                        break;
                    case "canv_eraser":
                        Eraser(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas, SetStrokeSelectionbySize);

                        if (_boardview.canv_strokes.Visibility == Visibility.Visible && _boardview.canv_colors.Visibility == Visibility.Collapsed && Canvas.GetTop(_boardview.canv_strokes) == 39)
                            _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                        else
                            _boardview.canv_strokes.Visibility = Visibility.Visible;

                        _boardview.canv_colors.Visibility = Visibility.Collapsed;
                        //_boardview.canv_strokes.SetValue(Canvas.TopProperty, (double)39);
                        _boardview.canv_strokes.Margin = new Thickness(-215, -225, 0, 0);
                        _inkToolName = InkToolName.Eraser;
                        break;
                    case "canv_highlighter":
                        Highlighter(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas, SetStrokeSelectionbySize);

                        if (_boardview.canv_strokes.Visibility == Visibility.Visible && _boardview.canv_colors.Visibility == Visibility.Collapsed && Canvas.GetTop(_boardview.canv_strokes) == 90)
                            _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                        else
                            _boardview.canv_strokes.Visibility = Visibility.Visible;

                        _boardview.canv_colors.Visibility = Visibility.Collapsed;
                        //_boardview.canv_strokes.SetValue(Canvas.TopProperty, (double)90);
                        _boardview.canv_strokes.Margin = new Thickness(-215, -140, 0, 0);
                        _inkToolName = InkToolName.Highlighter;
                        break;
                    case "canv_hand":
                        Pan();
                        ClearChildFromEditing();
                        if (!_isZoomEnabled)
                        {
                            _boardview.vb_board_zoom_display.Visibility = Visibility.Visible;
                            _boardview.inkcanv_zoom.Strokes = _boardview.inkCanvas.Strokes;
                        }
                        _inkToolName = InkToolName.Pan;
                        break;
                    case "canv_menu_browser_":
                        Messenger.Default.Send("browser" + "www.google.com", "DesktopMode");
                        break;
                    case "canv_save":
                        bool isSaved = CheckBoardInsertorUpdate();
                        if (isSaved)
                        {
                            ClearChildFromEditing();
                            Messenger.Default.Send("Information saved", "Notification");
                        }
                        break;
                    case "canv_clearboard":
                        Messenger.Default.Send(new KeyValuePair<string, string>("Clear Board", "This will erase all the strokes from " + (_selectedBoardChildren == null ? "the Canvas" : "Selected Item") + ", Do you want to proceed?"), "Result");
                        break;
                    case "canv_video_call_zoomus":
                        Messenger.Default.Send("Please wait for a while zoom instance is opening", "Notification");
                        StartOrJoinZoomVideoCall();
                        break;
                    case "canv_delete":
                        if (_selectedBoardChildren != null)
                            Messenger.Default.Send(new KeyValuePair<string, string>("Remove Board Item", "The item will be deleted from the canvas with all the annotations. Do you want to proceed?"), "Result");
                        else if (_boardview.inkCanvas.GetSelectedElements().Count > 0 || _boardview.inkCanvas.GetSelectedStrokes().Count() > 0)
                        {
                            Messenger.Default.Send(new KeyValuePair<string, string>("Remove Selected Item(s)", "The selected item(s) will be deleted from the canvas with all the annotations. Do you want to proceed?"), "Result");
                        }
                        else
                            Messenger.Default.Send("Nothing selected, select an item to delete", "Notification");
                        break;
                    case "canv_Undo":
                        Undo(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas);
                        UpdateUndoRedoToDb(_selectedBoardChildren == null ? "ParentInk" : "ChildInk");
                        break;
                    case "canv_Redo":
                        Redo(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas);
                        UpdateUndoRedoToDb(_selectedBoardChildren == null ? "ParentInk" : "ChildInk");
                        break;
                    case "canv_eraser_stroke":
                        EraserStroke(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas);

                        _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                        _boardview.canv_colors.Visibility = Visibility.Collapsed;
                        _inkToolName = InkToolName.Eraser;
                        break;
                    case "canv_duplicate":
                        if (_boardview.inkCanvas.GetSelectedElements().Count > 0)
                        {
                            int selectedElementsCount = _boardview.inkCanvas.GetSelectedElements().Count;
                            for (int i = 0; i < selectedElementsCount; i++)
                            {
                                Border selectedElement = _boardview.inkCanvas.GetSelectedElements()[i] as Border;
                                AddChildToBoard(_imageAnnotaionsList.Where(s => Convert.ToInt32(selectedElement.Tag) == s.AnnotationId).ToList(), true);
                            }
                        }

                        if (_boardview.inkCanvas.GetSelectedStrokes().Count > 0)
                        {

                            StrokeCollection strokes = _boardview.inkCanvas.GetSelectedStrokes().Clone();
                            _boardview.inkCanvas.Strokes.Add(strokes);
                            
                        }
                        break;
                }

                if (menuitem != null && (new List<string>() { "canv_save", "canv_clearboard", "canv_delete", "canv_Redo", "canv_Undo", "canv_show_product_review" }.Contains(menuitem.Name)))
                {
                    FillColortoSelectedUtility(menuitem.Children[0] as ShapePath, false, true);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }


        public void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (_boardview.inkCanvas.GetSelectedElements().Count > 0 || _boardview.inkCanvas.GetSelectedStrokes().Count > 0)
            {
                _boardview.canv_duplicate.Visibility = Visibility.Visible;
            }
            else
            {
                _boardview.canv_duplicate.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateUndoRedoToDb(string param)
        {
            if (_rethinkColService != null)
                if (param == "ParentInk")
                    _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvas", XamlWriter.Save(_boardview.inkCanvas.Strokes));
                else if (param == "ChildInk")
                    _rethinkColService.AddorUpdateStrokeDataintoDB("ChildUpdated", XamlWriter.Save((_selectedBoardChildren as InkCanvas).Strokes), annoId: Convert.ToInt32((((_selectedBoardChildren as InkCanvas).Parent as Grid).Parent as Border).Tag));
        }

        /// <summary>
        /// stroke color changing based on selected color
        /// </summary>
        public void Inkcolor_Selection_Pointerpressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Rectangle selectedItem = (e != null && !(e.Source is Image)) ? e.Source is Rectangle ? e.Source as Rectangle : (e.Source as Grid).Children[0] as Rectangle : sender as Rectangle;

                if (selectedItem != null && _boardview.inkCanvas.DefaultDrawingAttributes.IsHighlighter == false)
                {
                    _boardview.canv_colors.Children.OfType<Grid>().ToList().ForEach(s => { (s.Children[0] as Rectangle).Tag = ""; (s.Children[1] as Image).Visibility = Visibility.Collapsed; });

                    string colorcode = ((SolidColorBrush)selectedItem.Fill).Color.ToString();
                    selectedItem.Tag = colorcode;
                    ((selectedItem.Parent as Grid).Children[1] as Image).Visibility = Visibility.Visible;

                    if (colorcode.Length > 0)
                        StrokeColor(colorcode);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// stroke size changing based on size selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Inksize_Selection_Pointerpressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Grid element = e != null ? e.OriginalSource is Rectangle ? (e.OriginalSource as Rectangle).Parent as Grid : e.OriginalSource as Grid : sender as Grid;
                double size = -1;
                if (element != null)
                {
                    switch ((element.Children[0] as Rectangle).Name)
                    {
                        case "img_small_size":
                            size = _inkToolName == InkToolName.Highlighter ? 15 : 5;
                            break;
                        case "img_medium_size":
                            size = _inkToolName == InkToolName.Highlighter ? 25 : 10;
                            break;
                        case "img_larze_size":
                            size = _inkToolName == InkToolName.Highlighter ? 35 : 20;
                            break;
                        case "img_extralarze_size":
                            size = _inkToolName == InkToolName.Highlighter ? 45 : 40;
                            break;
                    }
                    StrokeSize(_selectedBoardChildren as InkCanvas ?? _boardview.inkCanvas, _inkToolName, size);
                    SetStrokeSelectionbySize((int)size);
                    CircleWidth = size * 5;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// display stroke size selected icon based on selection size
        /// </summary>
        /// <param name="size"></param>
        private void SetStrokeSelectionbySize(double size)
        {
            try
            {
                if (size != -1)
                {
                    _boardview.stack_strokes.Children.OfType<Grid>().ToList().ForEach(s => s.Children[1].Visibility = Visibility.Collapsed);
                    int index = (size == 10 || size == 25) ? 1 : (size == 20 || size == 35) ? 2 : (size == 40 || size == 45) ? 3 : 0;
                    (_boardview.stack_strokes.Children[index] as Grid).Children[1].Visibility = Visibility.Visible;
                }
                else if (size == -1 && _boardview.inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    List<Grid> sizes = _boardview.stack_strokes.Children.OfType<Grid>().ToList();
                    Grid sizeValue = sizes.FirstOrDefault(s => s.Children[1].Visibility == Visibility.Visible);
                    Inksize_Selection_Pointerpressed(sizes.FirstOrDefault(s => s.Children[1].Visibility == Visibility.Collapsed), null);
                    sizes.ForEach(s => s.Children[1].Visibility = Visibility.Collapsed);
                    Inksize_Selection_Pointerpressed(sizeValue, null);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear all elements & strokes on board or selected child
        /// </summary>
        private void ClearBoard(string param)
        {
            try
            {
                int selectedChildTag = -1;
                int childIndex;
                bool isIndex = int.TryParse(param, out childIndex);
                if (!isIndex)
                {
                    if (_selectedBoardChildren == null)
                    {
                        ClearStrokes(_boardview.inkCanvas);
                        CheckBoardInsertorUpdate();
                        if (_rethinkColService != null)
                            _rethinkColService.AddorUpdateStrokeDataintoDB("InkCanvas", XamlWriter.Save(_boardview.inkCanvas.Strokes), null, _nextPageIndex);
                    }
                    else
                    {
                        ClearStrokes(_selectedBoardChildren is InkCanvas ? _selectedBoardChildren as InkCanvas : null);
                        selectedChildTag = Convert.ToInt32(((_selectedBoardChildren.Parent as Grid).Parent as Border).Tag);
                        if (_rethinkColService != null)
                            _rethinkColService.AddorUpdateStrokeDataintoDB("ClearBoard", selectedChildTag.ToString(), null, _nextPageIndex);
                    }
                    Clear();
                }
                else
                {
                    if (childIndex <= 0)
                    {
                        ClearStrokes(_boardview.inkCanvas);
                        CheckBoardInsertorUpdate();
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Border boarder = _boardview.inkCanvas.Children.OfType<Border>().FirstOrDefault(s => Convert.ToInt32(s.Tag) == childIndex);
                            ((boarder.Child as Grid).Children[1] as InkCanvas).Strokes.Clear();
                            Clear();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// fill color to selected utility item path
        /// </summary>
        /// <param name="uiElement"></param>
        /// <param name="isSelected"></param>
        private void FillColortoSelectedUtility(ShapePath uiElement, bool isSelected, bool isEditTool)
        {
            uiElement.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom(isSelected ? "#FFA13939" : isEditTool ? "#FF313131" : "#FFACACAC"));
        }

        #endregion

        #region Utility tools

        private void FillColortoSelectedUtility(ShapePath uiElement, bool isSelected)
        {
            try
            {
                if (isSelected)
                    uiElement.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA13939"));
                else
                    uiElement.Fill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFACACAC"));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// utility menu tool selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Utilitymenu_Selection_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas selectedItem = e.OriginalSource is Canvas ? e.OriginalSource as Canvas : (e.OriginalSource as FrameworkElement).Parent as Canvas;
                if (selectedItem != null)
                {
                    FillColortoSelectedUtility(selectedItem.Children[0] as ShapePath, true, false);
                    switch (selectedItem.Name)
                    {
                        case "canv_desktop":
                            Messenger.Default.Send("GotoDesktopMode", "DesktopMode");
                            break;
                        case "canv_addmedia":
                            break;
                        case "canv_library_item":
                            _boardview.canv_library.Visibility = _boardview.canv_library.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                            App.ExecuteMethod(GetlibraryThumbnails, true);
                            break;
                        case "canv_email":
                            FillColortoSelectedUtility(selectedItem.Children[0] as ShapePath, true);
                            ShowEmailSendingOption();
                            break;
                        case "canv_mom":
                            _speechToText.StartAudioRecording(RecordedCommandText, false, null);
                            break;
                        case "canv_screen_recording":
                            CheckingForScreenRecording();
                            break;
                        case "canv_sticky":
                            _boardview.canv_sticky_colors.Visibility = _boardview.canv_sticky_colors.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                            break;
                        case "canv_voicenotes":
                            _voiceFilePath = Constants.AttachmentResources + "audio_" + NxgUtilities.GetCurrentTime() + ".wav";
                            _speechToText.StartAudioRecording(RecordedText, false, _voiceFilePath);
                            _boardview.canv_txt_bubble.Visibility = _boardview.audio_pause.Visibility = Visibility.Collapsed;
                            _boardview.canv_voicerecording.Visibility = _boardview.audio_play.Visibility = Visibility.Visible;
                            break;
                    }

                    if (new List<string> { "canv_desktop", "canv_screen_recording", "canv_addmedia" }.Contains(selectedItem.Name))
                    {
                        FillColortoSelectedUtility(selectedItem.Children[0] as ShapePath, false, false);
                    }
                }
                else
                {
                    if (selectedItem.Name == "canv_library_item")
                    {
                        FillColortoSelectedUtility(selectedItem.Children[0] as ShapePath, true, false);
                        _boardview.canv_library.Visibility = _boardview.canv_library.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                        App.ExecuteMethod(GetlibraryThumbnails, true);
                    }
                }
            }
            catch (NoMicrophoneException)
            {
                Messenger.Default.Send("No microphone!", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_addmedia_TouchUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ImportMediaItems("fromToolMenu");
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

        /// <summary>
        /// sticky color changing based on selected color
        /// </summary>
        public void Stickycolor_Selection_Pointerpressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Rectangle selectedItem = e.Source as Rectangle;
                if (selectedItem != null)
                {
                    _selectedStickyColorIndex = (selectedItem.Parent as StackPanel).Children.IndexOf(selectedItem);
                    AddChildToBoard(selectedItem.Fill.ToString(), "Sticky_" + DateTime.Now.ToString("hhmmssfff") + "__@__" + Guid.NewGuid().ToString(), AttachmentType.Sticky);
                }
                _selectedStickyColorIndex = -1;
                _boardview.canv_sticky_colors.Visibility = Visibility.Collapsed;
                FillColortoSelectedUtility(_boardview.canv_sticky.Children[0] as ShapePath, false, false);
                if (_inkToolName != InkToolName.Pan)
                    ToolShowMenuComponent(_boardview.canv_hand);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Media

        /// <summary>
        /// import media items
        /// </summary>
        /// <param name="senderName"></param>
        private void ImportMediaItems(string senderName)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Multiselect = true;
                _isFromLibrary = senderName == "fromLibrary";
                openFileDialog.Filter = !_isFromLibrary ? "Files (*.jpg;*.JPG,*.jpg,*.JPEG,*.png,*.PNG)|*.jpg;*.JPG;*.jpg;*.JPEG;*.png;*.PNG" : "Files (*.jpg;*.JPG,*.jpg,*.JPEG,*.png,*.PNG,*.pdf,*.Pdf,*.wmv,*.Wmv,*.mp4,*.MP4,*.docx,*.DOCX,*.doc,*.DOC,*.xlsx,*.XLSX,*.xls,*.XLS,*.pptx,*.PPTX, *.ppt,*.PPT)| *.jpg;*.JPG;*.jpg;*.JPEG;*.png;*.PNG;*.pdf;*.Pdf;*.wmv;*.Wmv;*.mp4;*.MP4;*.docx;*.DOCX;*.doc;*.DOC;*.xlsx;*.XLSX;*.xls;*.XLS;*.pptx;*.PPTX;*.ppt;*.PPT";

                if (openFileDialog.ShowDialog() == true)
                {
                    selectedFiles = openFileDialog.FileNames;
                    if (openFileDialog.FileNames.ToList().Any(s => NxgUtilities.IsValidImageExtension(Path.GetExtension(s).ToLower()) && new FileInfo(s).Length > 1000000))
                        Messenger.Default.Send(new KeyValuePair<string, string>("Add Resource", "Some resources are bigger than required size, Would you wish to compress"), "Result");
                    else
                        AddItemsToLibrary(false);
                }
                FillColortoSelectedUtility(_boardview.canv_addmedia.Children[0] as ShapePath, false, false);
                if (!_isFromLibrary)
                {
                    if (_inkToolName != InkToolName.Pan)
                        ToolShowMenuComponent(_boardview.canv_hand);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// add media item to library
        /// </summary>
        /// <param name="senderName"></param>
        private void AddItemsToLibrary(bool compressRequired)
        {
            App.ExecuteMethod(() =>
            {
                try
                {
                    if (selectedFiles != null && selectedFiles.Count() > 0)
                    {
                        foreach (string filePath in selectedFiles.Where(s => File.Exists(s)))
                        {
                            string tempPath = filePath;
                            if (compressRequired && NxgUtilities.IsValidImageExtension(Path.GetExtension(filePath).ToLower()) && new FileInfo(filePath).Length > 1000000)
                            {
                                tempPath = Path.GetTempPath() + Path.GetFileName(filePath);
                                System.Drawing.Image myImage = System.Drawing.Image.FromFile(filePath);
                                ImageCompressor.SaveJpeg(tempPath, myImage, 80);
                            }

                            string response = Service.UploadFile(tempPath);
                            if (!string.IsNullOrWhiteSpace(response))
                            {
                                string fileExt = Path.GetExtension(tempPath);

                                AttachmentType fileType = NxgUtilities.IsValidImageExtension(fileExt) ? AttachmentType.Media_Image : NxgUtilities.IsValidVideoExtension(fileExt) ? AttachmentType.Media_Video : NxgUtilities.IsValidPdfExtension(fileExt) ? AttachmentType.Media_Pdf : fileExt == ".docx" || fileExt == ".doc" ? AttachmentType.Word : fileExt == ".xlsx" || fileExt == ".xls" ? AttachmentType.Excel : AttachmentType.Power_Point;

                                AddChildToBoard(tempPath, response, fileType);
                            }
                        }

                        if (_isFromLibrary)
                            Task.Run(() => { GetlibraryThumbnails(); });
                    }
                    _isFromLibrary = false;
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            }, true);
        }

        #endregion

        #region Board Children

        /// <summary>
        /// add children to board using bytes from database 
        /// </summary>
        /// <param name="listAnnotations"></param>
        /// <param name="canv"></param>
        /// <returns></returns>
        private void AddChildToBoard(List<ImageAnnotations> listAnnotations, bool isFromDuplicate = false)
        {
            try
            {
                foreach (ImageAnnotations annotations in listAnnotations)
                {
                    int pk_id = annotations.AnnotationId;
                    if (!isFromDuplicate)
                        annotations.AnnotationId = -1;

                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass).Where(s => !string.IsNullOrWhiteSpace(s.AttachmentName) && s.ClassId == _currentClass.ClassId).ToList();

                    if (_mediaAnnotaionsList != null && _mediaAnnotaionsList.Count > 0 && _mediaAnnotaionsList.Any(s => s.LibraryThumbId == annotations.LibraryThumbId))
                    {
                        if (new List<string> { "media_image", "capture", "sticky" }.Contains(_mediaAnnotaionsList.FirstOrDefault(s => s.LibraryThumbId == annotations.LibraryThumbId).AttachmentType.ToLower()))
                            AddChildToBoard(annotations, updatePKID: pk_id);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// add children to board using storage file
        /// false -- board
        /// true -- to library
        /// </summary>
        /// <param name="StorageFile"></param>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        public void AddChildToBoard(string filePath, string attachedFile, AttachmentType type, string textData = "")
        {
            try
            {
                LibraryThumbs libraryItem = new LibraryThumbs { AttachmentTypeId = (int)type, AttachmentType = type.ToString(), Attachment = type != AttachmentType.Sticky ? attachedFile : null, TextInfo = type == AttachmentType.Sticky ? attachedFile : textData, ClassId = _currentClass.ClassId };

                string localPath = type != AttachmentType.Sticky ? libraryItem.AttachmentUid : "sticky";

                localPath = InsertDataIntoDB(libraryItem, filePath, localPath);

                if (!_isFromLibrary && new List<AttachmentType> { AttachmentType.Sticky, AttachmentType.Media_Image, AttachmentType.Capture }.Contains(type))
                {
                    string attachUid = !string.IsNullOrWhiteSpace(libraryItem.AttachmentUid) ? Path.GetFileName(Directory.GetFiles(Constants.AttachmentResources).FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(libraryItem.AttachmentUid)))) : "";

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MatrixTransform matrix = _boardview.inkCanvas.RenderTransform as MatrixTransform;
                        double leftPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetX < 0 ? (int)Math.Abs(matrix.Matrix.OffsetX) : 0, GetRangeValue(matrix.Matrix.M11, 'x'));
                        double topPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetY < 0 ? (int)Math.Abs(matrix.Matrix.OffsetY) : 0, GetRangeValue(matrix.Matrix.M11, 'y'));
                        //double leftPosition = NxgUtilities.GetRandomPosition(0, GetRangeValue(matrix.Matrix.M11, 'x'));
                        //double topPosition = NxgUtilities.GetRandomPosition(0, GetRangeValue(matrix.Matrix.M11, 'y'));

                        ImageAnnotations annotations = new ImageAnnotations { Manipulation = XamlWriter.Save(new MatrixTransform(1, 0, 0, 1, leftPosition, topPosition)), LibraryThumbId = libraryItem.LibraryThumbId };

                        AddChildToBoard(annotations);
                    });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// get range value based on zoom factor
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private int GetRangeValue(double zoomFactor, char position)
        {
            if (position == 'x')
                return zoomFactor <= 0.25 ? 3000 : zoomFactor <= 0.5 ? 2000 : 1000;
            else if (position == 'y')
                return zoomFactor <= 0.25 ? 2000 : zoomFactor <= 0.5 ? 1000 : 500;
            else
                return 0;
        }

        /// <summary>
        /// insert library thumb data
        /// </summary>
        private string InsertDataIntoDB<T>(T dataObject, string filePath, string localPath)
        {
            try
            {
                int pk_id = Service.InsertOrUpdateDataToDB(dataObject, CrudActions.Create);
                LibraryThumbs dataItem = dataObject as LibraryThumbs;

                if (pk_id > 0)
                {
                    dataItem.LibraryThumbId = pk_id;
                    if (dataItem.AttachmentType != "Sticky")
                    {
                        localPath = Constants.AttachmentResources + "File_" + pk_id + "_" + Path.GetFileNameWithoutExtension(localPath) + Path.GetExtension(filePath);
                        if (File.Exists(filePath))
                        {
                            if (dataObject is LibraryThumbs && new List<string> { AttachmentType.Capture.ToString() }.Contains(dataItem.AttachmentType) || filePath.ToLower().Contains("local\\temp"))
                                File.Move(filePath, localPath);
                            else
                                File.Copy(filePath, localPath);
                        }

                        GenerateThumb.GenerateThumbnail(localPath, Constants.AttachmentResourceThumbs, ".png");
                    }
                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass).Where(s => !string.IsNullOrWhiteSpace(s.AttachmentName) && s.ClassId == _currentClass.ClassId).ToList();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return localPath;
        }

        /// <summary>
        /// add children to board using bitmap image & bytes
        /// </summary>
        /// <param name="image"></param>
        /// <param name="childName"></param>
        private void AddChildToBoard(ImageAnnotations annotations, Canvas existingCanvas = null, bool isFromDrop = false, int updatePKID = -1)
        {
            try
            {
                LibraryThumbs libraryItem = (_mediaAnnotaionsList != null && _mediaAnnotaionsList.Count > 0) ? _mediaAnnotaionsList.FirstOrDefault(s => s.LibraryThumbId == annotations.LibraryThumbId) : null;

                if (libraryItem != null)
                {
                    BitmapImage bitmap = Assets.GetBitmapImage(annotations, libraryItem.AttachmentUid);
                    if (bitmap != null || libraryItem.AttachmentType.ToLower() == "sticky")
                    {
                        Matrix matrix = !string.IsNullOrWhiteSpace(annotations.Manipulation) ? (XamlReader.Parse(annotations.Manipulation) as MatrixTransform).Matrix : new Matrix(1, 0, 0, 1, 1920, 1080);
                        if (bitmap != null)
                        {
                            matrix.OffsetX = matrix.OffsetX - (bitmap.PixelWidth / 2);
                            matrix.OffsetY = matrix.OffsetY - (bitmap.PixelHeight / 2);
                        }

                        Border border = new Border { Uid = libraryItem.AttachmentUid, BorderThickness = new Thickness(5), BorderBrush = Brushes.Transparent, Background = Brushes.Transparent, IsManipulationEnabled = true, AllowDrop = true, RenderTransform = new MatrixTransform(matrix), RenderTransformOrigin = new Point(0, 0) };

                        Grid grid = new Grid { Tag = libraryItem.AttachmentType, Uid = libraryItem.AttachmentName };

                        Image img = new Image { Source = bitmap, Stretch = Stretch.Fill };

                        DrawingAttributes da = new DrawingAttributes { Color = Colors.White, Height = 5, Width = 5, IsHighlighter = false, IgnorePressure = false, FitToCurve = true };

                        InkCanvas inkcanv = new InkCanvas { Background = Brushes.Transparent, EditingMode = InkCanvasEditingMode.None, DefaultDrawingAttributes = da };
                        InkCanvas guestinkcanv = new InkCanvas { Background = Brushes.Transparent, EditingMode = InkCanvasEditingMode.None, DefaultDrawingAttributes = da };
                        if (!string.IsNullOrWhiteSpace(annotations.InkStrokes))
                        {
                            InkCanvas ink = (InkCanvas)XamlReader.Load(new XmlTextReader(new StringReader(annotations.InkStrokes)));
                            if (ink != null)
                            {
                                StrokeCollection allInkStrokes = XamlReader.Parse(annotations.TotalInkStrokes) as StrokeCollection;
                                inkcanv.Strokes = new StrokeCollection(allInkStrokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == RethinkService._macAddress).ToList());
                                guestinkcanv.Strokes = new StrokeCollection(allInkStrokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) != RethinkService._macAddress).ToList());
                                guestinkcanv.Background = inkcanv.Background = ink.Background;
                            }
                        }

                        if (libraryItem.AttachmentType.ToLower() == "sticky")
                        {
                            border.Height = grid.Height = img.Height = inkcanv.Height = guestinkcanv.Height = border.Width = grid.Width = img.Width = inkcanv.Width = 400;
                            grid.Tag = "sticky";

                            if (string.IsNullOrWhiteSpace(annotations.InkStrokes))
                                guestinkcanv.Background = (_boardview.canv_sticky_colors.Children[(_selectedStickyColorIndex != -1 ? _selectedStickyColorIndex : 0)] as Rectangle).Fill;
                        }
                        else
                        {
                            border.Height = bitmap.PixelHeight + 10;
                            border.Width = bitmap.PixelWidth + 10;

                            img.Height = grid.Height = inkcanv.Height = guestinkcanv.Height = bitmap.PixelHeight;
                            img.Width = grid.Width = inkcanv.Width = guestinkcanv.Width = bitmap.PixelWidth;
                        }
                        grid.Children.Add(img);
                        grid.Children.Add(guestinkcanv);
                        grid.Children.Add(inkcanv);

                        border.Child = grid;

                        if (existingCanvas == null)
                        {
                            Stylus.SetIsPressAndHoldEnabled(border, false);
                            inkcanv.StrokeCollected += Child_StrokeCollected;
                            inkcanv.MouseUp += ChildItem_MouseUp;
                            border.ManipulationStarting += Child_ManipulationStarting;
                            border.ManipulationDelta += Child_ManipulationDelta;
                            border.ManipulationCompleted += Child_ManipulationCompleted;
                            border.PreviewStylusDown += Child_StylusDown;
                            border.PreviewStylusMove += Child_StylusMove;
                            border.PreviewStylusUp += Child_StylusUp;
                            //border.Drop += LibraryItem_Drop;
                            DragAndDrop.RegisterElement(border, null, DragDirection.LeftNRight);

                            if (annotations.AnnotationId != -1)
                            {
                                annotations.BoardAnnotationId = _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex).AnnotationId;
                                annotations.ClassId = _currentClass.ClassId;
                                annotations.InkStrokes = XamlWriter.Save(inkcanv);
                                StrokeCollection totalStrokes = new StrokeCollection(guestinkcanv.Strokes.Clone());
                                totalStrokes.Add(inkcanv.Strokes.Clone());
                                annotations.TotalInkStrokes = XamlWriter.Save(totalStrokes);
                                int insertedIndex = Service.InsertOrUpdateDataToDB(annotations, CrudActions.Create);
                                border.Tag = insertedIndex;

                                if (_rethinkColService != null)
                                    _rethinkColService.AddorUpdateStrokeDataintoDB("ChildAdded", annotations.InkStrokes, annotations.Manipulation, insertedIndex);
                            }
                            else
                                border.Tag = updatePKID;
                            BoardAnnotations board = _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex);
                            if (board != null)
                                _imageAnnotaionsList = Service.GetModuleDataList<ImageAnnotations>(_currentClass, board.AnnotationId);
                            Panel.SetZIndex(border, 99999);
                            _boardview.inkCanvas.Children.Add(border);
                            
                        }
                        else
                        {
                            existingCanvas.Children.Add(border);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// collecting stroke in ink mode of board child
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Child_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs args)
        {
            Border border = ((sender as InkCanvas).Parent as Grid).Parent as Border;
            args.Stroke.AddPropertyData(Guid.NewGuid(), RethinkService._macAddress);
            if (_rethinkColService != null)
                _rethinkColService.AddorUpdateStrokeDataintoDB("ChildUpdated", XamlWriter.Save((sender as InkCanvas).Strokes), annoId: Convert.ToInt32(border.Tag));

            Monitor(_selectedBoardChildren as InkCanvas, _boardview.canv_Undo, _boardview.canv_Redo);
            _boardview.canv_colors.Visibility = _boardview.canv_strokes.Visibility = Visibility.Collapsed;

            Task.Run(() => { UpdateChild(border); });
        }

        /// <summary>
        /// ollecting stroke in eraser mode of board child
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChildItem_MouseUp(object sender, MouseEventArgs args)
        {
            args.Handled = true;
            InkCanvas inkCanv = sender as InkCanvas;
            if (inkCanv.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                Border border = (inkCanv.Parent as Grid).Parent as Border;
                if (_rethinkColService != null)
                    _rethinkColService.AddorUpdateStrokeDataintoDB("ChildUpdated", XamlWriter.Save((sender as InkCanvas).Strokes), annoId: Convert.ToInt32(border.Tag));

                Monitor(_selectedBoardChildren as InkCanvas, _boardview.canv_Undo, _boardview.canv_Redo);
                _boardview.canv_colors.Visibility = _boardview.canv_strokes.Visibility = Visibility.Collapsed;
                Task.Run(() => { UpdateChild(border); });
            }
        }

        /// <summary>
        /// Manipulation starting event for board child
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void Child_ManipulationStarting(object sender, ManipulationStartingEventArgs args)
        {
            try
            {
                args.Handled = true;
                args.ManipulationContainer = _boardview.inkCanvas;

                // Adjust Z-order
                FrameworkElement element = args.Source as FrameworkElement;
                if (!(element is ShapePath))
                {
                    Panel pnl = element.Parent as Panel;

                    if (pnl == null)
                        pnl = (element.Parent as InkCanvas).Parent as Panel;
                    if (pnl != null && pnl.Children != null)
                    {
                        for (int i = 0; i < pnl.Children.Count; i++)
                            Panel.SetZIndex(pnl.Children[i], pnl.Children[i] == element ? pnl.Children.Count : i);
                    }
                }

                if (sender is Border)
                {
                    Border selectedItem = (sender as Border);
                    if (selectedItem.RenderTransform is ScaleTransform)
                    {
                        selectedItem.RenderTransformOrigin = new Point(0, 0);
                        selectedItem.RenderTransform = new MatrixTransform(1, 0, 0, 1, Canvas.GetLeft(selectedItem), Canvas.GetTop(selectedItem));
                        Canvas.SetLeft(selectedItem, 0);
                        Canvas.SetTop(selectedItem, 0);
                    }
                }

            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// manipulations for board children
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Child_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            try
            {
                FrameworkElement fe = e.Source as FrameworkElement;
                ManipulationDelta md = e.DeltaManipulation;
                Vector trans = md.Translation;
                var transformation = fe.RenderTransform as MatrixTransform;
                Matrix m = transformation == null ? Matrix.Identity : transformation.Matrix;

                Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                center = m.Transform(center);

                m.Translate(trans.X, trans.Y);

                //if (Convert.ToString(((sender as Border).Child as FrameworkElement).Tag).ToLower() == "sticky" && (m.M11 >= 2.0 || m.M11 <= 0.95))
                //{
                //    e.Cancel();
                //    m = m.M11 >= 2.0 ? new Matrix(1.95, 0, 0, 1.95, transformation.Matrix.OffsetX, transformation.Matrix.OffsetY) : new Matrix(1, 0, 0, 1, transformation.Matrix.OffsetX, transformation.Matrix.OffsetY);
                //}
                //else
                m.ScaleAt(e.DeltaManipulation.Scale.X, e.DeltaManipulation.Scale.Y, center.X, center.Y);
                //  || (sender is Border && Convert.ToString(((sender as Border).Child as FrameworkElement).Tag).ToLower() != "sticky")
                //if (sender is RichTextBox)
                m.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);

                fe.RenderTransform = new MatrixTransform(m);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// manipulations completed event for board children
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Child_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            try
            {
                if (_rethinkColService != null)
                    _rethinkColService.AddorUpdateStrokeDataintoDB("ChildManipulated", null, manipulation: XamlWriter.Save((sender as Border).RenderTransform), annoId: Convert.ToInt32((sender as Border).Tag));

                Task.Run(() => { UpdateChild(sender as Border); });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }

        }

        private void UpdateChild(Border sender)
        {
            try
            {
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    Border dataitem = sender as Border;
                    if (dataitem != null && dataitem.RenderTransform != null)
                    {
                        LibraryThumbs manipulateItem = _mediaAnnotaionsList.FirstOrDefault(k => !string.IsNullOrWhiteSpace((dataitem.Child as Grid).Uid) && !string.IsNullOrWhiteSpace(k.AttachmentName) && k.AttachmentName.StartsWith((dataitem.Child as Grid).Uid));
                        if (manipulateItem != null)
                        {
                            ImageAnnotations imageAnnotation = _imageAnnotaionsList.FirstOrDefault((Func<ImageAnnotations, bool>)(s => Convert.ToInt32(dataitem.Tag) == s.AnnotationId));
                            if (imageAnnotation != null)
                            {
                                StrokeCollection totalStrokes = new StrokeCollection(((dataitem.Child as Grid).Children[2] as InkCanvas).Strokes.Clone());
                                totalStrokes.Add(((dataitem.Child as Grid).Children[1] as InkCanvas).Strokes.Clone());
                                ((dataitem.Child as Grid).Children[2] as InkCanvas).Background = ((dataitem.Child as Grid).Children[1] as InkCanvas).Background;
                                imageAnnotation.InkStrokes = XamlWriter.Save((dataitem.Child as Grid).Children[1] as InkCanvas);
                                imageAnnotation.TotalInkStrokes = XamlWriter.Save(totalStrokes);
                                imageAnnotation.Manipulation = XamlWriter.Save(dataitem.RenderTransform);
                                ((dataitem.Child as Grid).Children[2] as InkCanvas).Background = Brushes.Transparent;
                                int isUpdatedId = Service.InsertOrUpdateDataToDB(imageAnnotation, CrudActions.Update);
                            }
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Stylus down event for selecting board child to edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Child_StylusDown(object sender, StylusDownEventArgs e)
        {
            try
            {
                Border border = sender as Border;
                if (border != null && ((border.Child as Grid).Children[1] as InkCanvas).EditingMode == InkCanvasEditingMode.None)
                {
                    ImageAnnotations imgAnnotation = _imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32(border.Tag) == s.AnnotationId);
                    if (imgAnnotation != null)
                    {
                        _selectedChildPosition = e.GetPosition(sender as UIElement);
                        NxgDynamicGestures.StartLongTapTimer(SelectChildToEdit, sender);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Stylus move event for cancel board child editing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Child_StylusMove(object sender, StylusEventArgs e)
        {
            try
            {
                Point currentTouchPoint = e.GetPosition(sender as UIElement);
                if (currentTouchPoint.X > _selectedChildPosition.X + 5 || currentTouchPoint.X < _selectedChildPosition.X - 5 || currentTouchPoint.Y > _selectedChildPosition.Y + 5 || currentTouchPoint.Y < _selectedChildPosition.Y - 5)
                    NxgDynamicGestures.StopLongTapTimer();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        ///  Stylus up event for cancel board child editing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Child_StylusUp(object sender, StylusEventArgs e)
        {
            try
            {
                NxgDynamicGestures.StopLongTapTimer();
                if ((sender as Border).Uid == "")
                    (sender as Border).BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D3D3D3"));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// select board child to annotations mode
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private void SelectChildToEdit(object sender)
        {
            try
            {
                ClearChildFromEditing();

                Border border = sender as Border;

                border.StylusDown -= Child_StylusDown;
                border.StylusMove -= Child_StylusMove;
                border.StylusUp -= Child_StylusUp;

                InkCanvas inkcanv = (border.Child as Grid).Children[2] as InkCanvas;
                inkcanv.EditingMode = InkCanvasEditingMode.None;

                _selectedBoardChildren = inkcanv as FrameworkElement;
                AddElement(inkcanv, border);

                Monitor(inkcanv, _boardview.canv_Undo, _boardview.canv_Redo);

                border.BorderBrush = new SolidColorBrush(Colors.Red);

                ToolShowMenuComponent(_boardview.canv_marker);

                //_imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32(border.Tag) == s.BoardAnnotationId).IsEditEnabled = 1;

                Inkcolor_Selection_Pointerpressed(_boardview.rect_red, null);
                Inksize_Selection_Pointerpressed(_boardview.stack_strokes.Children[0], null);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// clear selection of board child
        /// </summary>a
        private void ClearChildFromEditing()
        {
            try
            {
                if (_selectedBoardChildren != null && _mediaAnnotaionsList != null && _mediaAnnotaionsList.Count > 0)
                {
                    Border border = (_selectedBoardChildren.Parent as Grid).Parent as Border;
                    if (border != null && _mediaAnnotaionsList.Any(k => k.AttachmentType != "Decision" && k.AttachmentName.StartsWith((border.Child as Grid).Uid)))
                    {
                        Grid item = border.Child as Grid;

                        ImageAnnotations imageAnnotation = _imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32(border.Tag) == s.AnnotationId);
                        if (imageAnnotation != null)
                        {
                            imageAnnotation.InkStrokes = XamlWriter.Save(item.Children[1]);
                            imageAnnotation.Manipulation = XamlWriter.Save(border.RenderTransform);

                            int isUpdatedId = Service.InsertOrUpdateDataToDB(imageAnnotation, CrudActions.Update);

                            border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                            border.IsManipulationEnabled = true;
                            _selectedBoardChildren = null;

                            Clear();

                            border.StylusDown += Child_StylusDown;
                            border.StylusMove += Child_StylusMove;
                            border.StylusUp += Child_StylusUp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// delete selected child item from board
        /// </summary>
        private void RemoveBoardItem(string param = "")
        {
            int selectedChildTag = -1;
            int childIndex;
            bool isIndex = int.TryParse(param, out childIndex);
            if (!isIndex)
            {
                if (_selectedBoardChildren != null)
                {
                    Grid grid = _selectedBoardChildren is TextBlock ? null : _selectedBoardChildren.Parent as Grid;
                    bool isMediaDeleted = false;
                    if (_selectedBoardChildren is InkCanvas)
                    {
                        LibraryThumbs manipulateItem = _mediaAnnotaionsList.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.AttachmentUid) && Path.GetFileNameWithoutExtension(k.AttachmentUid).Contains(Path.GetFileNameWithoutExtension((grid.Parent as Border).Uid)));
                        if (manipulateItem != null)
                        {
                            ImageAnnotations child = _imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32((grid.Parent as Border).Tag) == s.AnnotationId);
                            if (child != null)
                            {
                                isMediaDeleted = Service.InsertOrUpdateDataToDB(child, CrudActions.Delete, child.AnnotationId) > 0;
                            }
                        }
                        Messenger.Default.Send("Item Deleted successfully.", "Notification");
                    }
                    selectedChildTag = Convert.ToInt32(((_selectedBoardChildren.Parent as Grid).Parent as Border).Tag);
                    if (_rethinkColService != null)
                        _rethinkColService.AddorUpdateStrokeDataintoDB("DeleteBoardChild", selectedChildTag.ToString(), null, _nextPageIndex);
                    _boardview.inkCanvas.Children.Remove((grid.Parent as Border) ?? _selectedBoardChildren);
                    _selectedBoardChildren = null;
                    ToolShowMenuComponent(_boardview.canv_marker);
                }
            }
            else if (childIndex > 0)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Border boarder = _boardview.inkCanvas.Children.OfType<Border>().FirstOrDefault(s => Convert.ToInt32(s.Tag) == childIndex);
                    _boardview.inkCanvas.Children.Remove(boarder);
                });
            }
        }

        /// <summary>
        /// delete selected item from board
        /// </summary>
        private void RemoveSelectedItems(string param = "")
        {
            try
            {
                int selectedChildTag = -1;
                if (_boardview.inkCanvas.GetSelectedElements().Count > 0)
                {
                    int selectedElementsCount = _boardview.inkCanvas.GetSelectedElements().Count;
                    for (int i = 0; i < selectedElementsCount; i++)
                    {
                        Border selectedElement = _boardview.inkCanvas.GetSelectedElements()[0] as Border;
                        LibraryThumbs manipulateItem = _mediaAnnotaionsList.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.AttachmentUid) && Path.GetFileNameWithoutExtension(k.AttachmentUid).Contains(Path.GetFileNameWithoutExtension((selectedElement).Uid)));
                        if (manipulateItem != null)
                        {
                            ImageAnnotations child = _imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32((selectedElement).Tag) == s.AnnotationId);
                            if (child != null)
                            {
                                Service.InsertOrUpdateDataToDB(child, CrudActions.Delete, child.AnnotationId);
                            }
                        }
                        selectedChildTag = Convert.ToInt32((selectedElement).Tag);
                        if (_rethinkColService != null)
                            _rethinkColService.AddorUpdateStrokeDataintoDB("DeleteBoardChild", selectedChildTag.ToString(), null, _nextPageIndex);
                        _boardview.inkCanvas.Children.Remove(selectedElement);
                        _selectedBoardChildren = null;
                    }
                }

                if (_boardview.inkCanvas.GetSelectedStrokes().Count > 0)
                {
                    _boardview.inkCanvas.Strokes.Remove(_boardview.inkCanvas.GetSelectedStrokes());
                }

                ToolShowMenuComponent(_boardview.canv_marker);
                Messenger.Default.Send("Selected Items Deleted successfully.", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// delete selected child item from board
        /// </summary>
        private void AddColBoardItem(KeyValuePair<string, string> param)
        {
            try
            {
                _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass).Where(s => !string.IsNullOrWhiteSpace(s.AttachmentName) && s.ClassId == _currentClass.ClassId).ToList();

                _imageAnnotaionsList = Service.GetModuleDataList<ImageAnnotations>(_currentClass, _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex).AnnotationId);

                if (_imageAnnotaionsList != null && _imageAnnotaionsList.Count > 0)
                {
                    ImageAnnotations annotations = _imageAnnotaionsList.FirstOrDefault(s => s.AnnotationId == Convert.ToInt32(param.Value));
                    if (_mediaAnnotaionsList != null && _mediaAnnotaionsList.Count > 0 && _mediaAnnotaionsList.Any(s => s.LibraryThumbId == annotations.LibraryThumbId))
                    {
                        int pk_id = annotations.AnnotationId;
                        annotations.AnnotationId = -1;
                        if (new List<string> { "media_image", "capture", "sticky" }.Contains(_mediaAnnotaionsList.FirstOrDefault(s => s.LibraryThumbId == annotations.LibraryThumbId).AttachmentType.ToLower()))
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                AddChildToBoard(annotations, updatePKID: pk_id);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// delete selected child item from board
        /// </summary>
        private void UpdateColBoardItem(KeyValuePair<object, KeyValuePair<string, string>> param)
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Border item = _boardview.inkCanvas.Children.OfType<Border>().FirstOrDefault(s => Convert.ToInt32(s.Tag) == Convert.ToInt32(param.Value.Key));
                    if (item != null)
                    {
                        InkCanvas inkCanv = ((item.Child as Grid).Children[1] as InkCanvas);
                        if (param.Key is StrokeCollection)
                        {
                            inkCanv.Strokes.Remove(new StrokeCollection(inkCanv.Strokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == param.Value.Value).ToList()));
                            inkCanv.Strokes.Add(new StrokeCollection(param.Key as StrokeCollection));
                        }
                        else
                            item.RenderTransform = XamlReader.Parse(Convert.ToString(param.Key)) as MatrixTransform;
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_selection_tool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ToolShowMenuComponent(_boardview.canv_marker);

                _boardview.inkCanvas.EditingMode = InkCanvasEditingMode.None;
                _boardview.inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void AddRichTextBox(string input = null)
        {
            RichTextBox inkCanvasTextBox = new RichTextBox();
            //inkCanvasTextBox.SelectionChanged += inkCanvasTextBox_SelectionChanged;
            inkCanvasTextBox.GotFocus += (obj, args) => { };

            inkCanvasTextBox.Visibility = Visibility.Visible;
            inkCanvasTextBox.Background = Brushes.WhiteSmoke;
            if (!string.IsNullOrWhiteSpace(input))
                inkCanvasTextBox.AppendText(input);

            inkCanvasTextBox.MaxWidth = 500;
            inkCanvasTextBox.MaxHeight = 200;
            inkCanvasTextBox.BorderThickness = new Thickness(5);
            inkCanvasTextBox.BorderBrush = Brushes.Black;
            inkCanvasTextBox.FontSize = 30;
            inkCanvasTextBox.FontFamily = new FontFamily("2peas");
            Panel.SetZIndex(inkCanvasTextBox, 99999);
            inkCanvasTextBox.CaretBrush = Brushes.Black;
            inkCanvasTextBox.BorderThickness = new Thickness(0);
            inkCanvasTextBox.Foreground = Brushes.Black;

            MatrixTransform matrix = _boardview.inkCanvas.RenderTransform as MatrixTransform;
            double leftPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetX < 0 ? (int)Math.Abs(matrix.Matrix.OffsetX) : 0, GetRangeValue(matrix.Matrix.M11, 'x'));
            double topPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetY < 0 ? (int)Math.Abs(matrix.Matrix.OffsetY) : 0, GetRangeValue(matrix.Matrix.M11, 'y'));

            inkCanvasTextBox.RenderTransform = new MatrixTransform(1, 0, 0, 1, leftPosition, topPosition);

            _boardview.inkCanvas.Children.Add(inkCanvasTextBox);
            inkCanvasTextBox.Focus();
            inkCanvasTextBox.UpdateLayout();
        }

        public void canv_add_textblock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ToolShowMenuComponent(_boardview.canv_marker);

                _boardview.inkCanvas.EditingMode = InkCanvasEditingMode.None;
                _boardview.inkCanvas.EditingMode = InkCanvasEditingMode.Select;
                AddRichTextBox();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }


        private bool _isCloseApplication = false;
        public void canv_exit_application_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                e.Handled = true;
                canv_close_class_MouseDown(null, null);
                _isCloseApplication = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_ink_to_text_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DisableMagnifier();
                string recognizedText = string.Empty;

                if (_boardview.inkCanvas.GetSelectedStrokes().Count > 0)
                {
                    InkCanvas selectedInkCanvas = new InkCanvas();
                    selectedInkCanvas.Strokes = _boardview.inkCanvas.GetSelectedStrokes();
                    recognizedText = RecognizeStrokes.RecognizeText(selectedInkCanvas, null);
                }
                if (!string.IsNullOrWhiteSpace(recognizedText))
                    AddRichTextBox(recognizedText);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void DisableMagnifier()
        {
            if (_boardview.MyMagnifier.ZoomFactor == 0.5)
            {
                _boardview.MyMagnifier.ZoomFactor = 0;
                ToolShowMenuComponent(_boardview.canv_marker);
                _boardview.MyMagnifier.Visibility = Visibility.Collapsed;
                _boardview.canv_magnifier_slider.Visibility = Visibility.Collapsed;

                _isGestureEnabled = _currentGestureStatus;
                if (_isGestureEnabled)
                {
                    _boardview.gestures_switch_on.Visibility = Visibility.Visible;
                    _boardview.gestures_switch_off.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _boardview.gestures_switch_on.Visibility = Visibility.Collapsed;
                    _boardview.gestures_switch_off.Visibility = Visibility.Visible;
                }
            }
        }

        private bool _currentGestureStatus = false;

        public void canv_magnifier_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_boardview.MyMagnifier.ZoomFactor == 0.5)
                {
                    DisableMagnifier();
                }
                else
                {
                    ToolShowMenuComponent(_boardview.canv_marker);

                    _boardview.MyMagnifier.ZoomFactor = 0.5;
                    _boardview.inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    _boardview.MyMagnifier.Visibility = Visibility.Visible;
                    _boardview.canv_magnifier_slider.Visibility = Visibility.Visible;

                    _currentGestureStatus = _isGestureEnabled;

                    _isGestureEnabled = false;
                    _boardview.gestures_switch_on.Visibility = Visibility.Collapsed;
                    _boardview.gestures_switch_off.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void slider_magnifier_size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                _boardview.MyMagnifier.Radius = (sender as Slider).Value;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Library

        /// <summary>
        /// get library data from db
        /// </summary>
        private void GetlibraryThumbnails()
        {
            try
            {
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass).Where(s => (!string.IsNullOrWhiteSpace(s.AttachmentName) || s.AttachmentType == AttachmentType.Decision.ToString() || s.AttachmentType == AttachmentType.Note.ToString() || s.AttachmentType == AttachmentType.Task.ToString()) && s.ClassId == _currentClass.ClassId).ToList();

                    //pending
                    foreach (LibraryThumbs item in _mediaAnnotaionsList.Where(s => s.ParticipantId != 0))
                    {
                        if (ParticipantsList != null)
                        {
                            Participants emp = ParticipantsList.FirstOrDefault(s => s.ParticipantId == item.ParticipantId);
                            if (emp != null)
                            {
                                item.Participant = item.AttachmentType == AttachmentType.Task.ToString() ? "Assigned to " + string.Join(",", HomePageViewModel._contactsDbList.Where((Func<Employees, bool>)(s => item.AssignedEmployeePKIDs.Split(',').ToList().Any((Func<string, bool>)(k => Convert.ToInt32(k) == s.EmployeeId)))).ToList().Select(l => l.Name)) : emp.Employee.FirstName;
                            }
                        }
                    }

                    if (_mediaAnnotaionsList != null)
                    {
                        CaptureList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Capture.ToString()).ToList();
                        AudioList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Audio.ToString()).ToList();
                        ScreenrecordList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Screen_Record.ToString()).ToList();
                        MediaList = _mediaAnnotaionsList.Where(s => new List<string> { AttachmentType.Excel.ToString(), AttachmentType.Word.ToString(), AttachmentType.Power_Point.ToString() }.Contains(s.AttachmentType) || s.AttachmentType.ToLower().StartsWith("media")).ToList();

                        if (SelectedAgenda != null)
                        {
                            AgendaTaskList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Task.ToString() && s.AgendaId == SelectedAgenda.AgendaId).ToList();

                            AgendaNoteList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Note.ToString() && s.AgendaId == SelectedAgenda.AgendaId).ToList();

                            AgendaDecisionList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Decision.ToString() && s.AgendaId == SelectedAgenda.AgendaId).ToList();
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        //public void LibraryItem_TouchDown(object sender, TouchEventArgs e)
        //{
        //    try
        //    {
        //        _canvDragItemParent = null;

        //        Border selectedItem = sender as Border;
        //        string attachmentType = Convert.ToString(selectedItem.Tag);
        //        if (!string.IsNullOrWhiteSpace(attachmentType) && attachmentType == AttachmentType.Task.ToString())
        //        {
        //            LibraryThumbs taskitem = (sender as Border).DataContext as LibraryThumbs;
        //            if (taskitem != null)
        //            {
        //                _boardview.tbk_agenda_window_name.Text = "Tasks";

        //                _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;
        //                _boardview.inkcanv_decision.Strokes.Clear();

        //                _boardview.txt_note.Visibility = Visibility.Visible;
        //                _boardview.canv_agenda_popup.Visibility = Visibility.Visible;

        //                _boardview.sp_task.Visibility = Visibility.Visible;

        //                SearchedContactText = "";
        //                _boardview.txt_note.Text = taskitem.TextInfo;
        //                _boardview.txt_note.Tag = taskitem.PKID;
        //                _taskMembersList = HomePageViewModel._contactsDbList.Where(s => taskitem.AssignedEmployeePKIDs.Split(',').ToList().Any(k => Convert.ToInt32(k) == s.PKID)).ToList();
        //                _boardview.lb_task_members.ItemsSource = null;
        //                _boardview.lb_task_members.ItemsSource = _taskMembersList;
        //            }
        //            return;
        //        }
        //        else if (!string.IsNullOrWhiteSpace(attachmentType) && attachmentType == AttachmentType.Note.ToString())
        //        {
        //            LibraryThumbs taskitem = (sender as Border).DataContext as LibraryThumbs;
        //            if (taskitem != null)
        //            {
        //                _boardview.tbk_agenda_window_name.Text = "Notes";

        //                _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;
        //                _boardview.inkcanv_decision.Strokes.Clear();

        //                _boardview.txt_note.Visibility = Visibility.Visible;
        //                _boardview.canv_agenda_popup.Visibility = Visibility.Visible;

        //                _boardview.sp_task.Visibility = Visibility.Collapsed;

        //                _boardview.txt_note.Text = taskitem.TextInfo;
        //                _boardview.txt_note.Tag = taskitem.PKID;
        //            }
        //            return;
        //        }

        //        if (selectedItem != null && !string.IsNullOrWhiteSpace(attachmentType) && new List<string> { "media_image", "capture" }.Contains(attachmentType.ToLower()) && (!(selectedItem.Child is Grid) || _selectedBoardChildren != (selectedItem.Child as Grid).Children[1] as InkCanvas) && NxgUtilities.IsValidImageExtension(Path.GetExtension((selectedItem.Child as StackPanel).Uid)))
        //        {
        //            _dragItemPosition = e.GetTouchPoint(sender as UIElement).Position;
        //            _dragItemPositionFromScreen = e.GetTouchPoint(_boardview.canv_main).Position;
        //            if (!string.IsNullOrWhiteSpace(selectedItem.Uid))
        //                NxgDynamicGestures.StartLongTapTimer(SelectChildToEdit, selectedItem);
        //            else
        //                NxgDynamicGestures.StartLongTapTimer(SelectItemToDrag, selectedItem);
        //        }
        //        else
        //        {
        //            NxgDynamicGestures.StopLongTapTimer();
        //            if (new List<string> { "media_video", "screen_record", "media_pdf", "excel", "word", "power_point" }.Contains(attachmentType.ToLower()) && selectedItem.TouchesOver.Count() == 1 &&  _selectedDoubleTapItem == sender as Border)
        //            {//IsDoubleTap(sender, e) &&
        //                Canvas selectedItemCanvas = (selectedItem.Child as StackPanel).Children[0] as Canvas;
        //                if (selectedItemCanvas != null)
        //                {
        //                    string attachment = Convert.ToString(selectedItemCanvas.Tag);

        //                    List<string> filesList = Directory.GetFiles(Constants.AttachmentResources).ToList();
        //                    attachment = Path.GetFileName(filesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(attachment))));

        //                    if (!string.IsNullOrWhiteSpace(attachment))
        //                        Messenger.Default.Send(attachment, "DesktopMode");
        //                }
        //                _selectedDoubleTapItem = null;
        //                _lastTapLocation = new Point(0, 0);
        //                _doubleTapStopWatch.Stop();
        //            }
        //            else if (_selectedDoubleTapItem == null || _selectedDoubleTapItem != selectedItem)
        //            {
        //                _selectedDoubleTapItem = selectedItem;
        //                _doubleTapStopWatch.Restart();
        //            }
        //            else
        //            {
        //                _selectedDoubleTapItem = null;
        //                _lastTapLocation = new Point(0, 0);
        //                _doubleTapStopWatch.Stop();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        App.InsertException(ex);
        //    }
        //}

        //public void LibraryItem_StylusMove(object sender, StylusEventArgs e)
        //{
        //    try
        //    {
        //        Border selectedItem = sender as Border;
        //        Point currentScreenTouchPoint = e.GetPosition(_boardview.canv_main);
        //        double currentTouchPointX = e.GetPosition(sender as UIElement).X;
        //        double currentTouchPointY = e.GetPosition(sender as UIElement).Y;
        //        if (currentTouchPointX > _dragItemPosition.X + 5 || currentTouchPointX < _dragItemPosition.X - 5 || currentTouchPointY > _dragItemPosition.Y + 5 || currentTouchPointY < _dragItemPosition.Y - 5 || currentScreenTouchPoint.X > _dragItemPositionFromScreen.X + 5 || currentScreenTouchPoint.X < _dragItemPositionFromScreen.X - 5 || currentScreenTouchPoint.Y > _dragItemPositionFromScreen.Y + 5 || currentScreenTouchPoint.Y < _dragItemPositionFromScreen.Y - 5)
        //        {
        //            NxgDynamicGestures.StopLongTapTimer();
        //        }
        //        else if (new List<string> { "media_image", "capture" }.Contains(Convert.ToString(selectedItem.Tag).ToLower()) && (selectedItem.Child is StackPanel) && libraryDragItem != null)
        //        {
        //            Image img = (((selectedItem.Child as StackPanel).Children[0] as Canvas).Children[0] as Canvas).Children[0] as Image;
        //            _dragItemPosition = e.GetPosition(sender as UIElement);
        //            DragMoveContact(img as UIElement, _dragItemPosition);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        App.InsertException(ex);
        //    }
        //}

        //public void LibraryItem_StylusUp(object sender, StylusEventArgs e)
        //{
        //    NxgDynamicGestures.StopLongTapTimer();
        //    if ((sender as Border).Uid == "")
        //    {
        //        (sender as Border).BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D3D3D3"));
        //    }
        //}

        ///// <summary>
        ///// Select item to drag from library to board
        ///// </summary>
        ///// <param name="sender"></param>
        //public void SelectItemToDrag(object sender)
        //{
        //    string fileName = Convert.ToString((((sender as Border).Child as StackPanel).Children[0] as Canvas).Tag);

        //    List<string> filesList = Directory.GetFiles(Constants.AttachmentResourceThumbs).ToList();
        //    fileName = Path.GetFileName(filesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(fileName))));

        //    string extension = Path.GetExtension(fileName);

        //    if (!NxgUtilities.IsValidVideoExtension(extension) && !NxgUtilities.IsValidPdfExtension(extension))
        //    {
        //        mediaDragItem = sender as Border;

        //        libraryDragItem = (sender as Border).DataContext as LibraryThumbs;
        //        string name = libraryDragItem.AttachmentLocalPath;
        //        (sender as Border).BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#148F77"));
        //    }
        //}

        //public static void DragMoveContact(UIElement sender, Point point)
        //{
        //    //DataObject data = new DataObject(new DragDropLib.DataObject());
        //    //data.SetDragImage(sender, point);
        //    //DragDrop.DoDragDrop(sender, data, DragDropEffects.Move);
        //}

        /// <summary>
        /// close event of Library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void library_Close_Tapped(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_library.Visibility = Visibility.Collapsed;
            _boardview.canv_library.RenderTransform = new MatrixTransform(1, 0, 0, 1, 0, 0);
            FillColortoSelectedUtility(_boardview.canv_library_item.Children[0] as ShapePath, false);
        }

        /// <summary>
        /// manipulations for library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Library_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            e.Handled = true;

            FrameworkElement fe = (sender as ShapePath).Parent as FrameworkElement;
            ManipulationDelta md = e.DeltaManipulation;
            Vector trans = md.Translation;
            var transformation = fe.RenderTransform as MatrixTransform;
            Matrix m = transformation == null ? Matrix.Identity : transformation.Matrix;

            m.Translate(trans.X, trans.Y);
            fe.RenderTransform = new MatrixTransform(m);
        }

        public void libraryitem_audio_StylusDown(object sender, StylusDownEventArgs e)
        {
            try
            {
                LibraryThumbs selectedAudioItem = AudioList.FirstOrDefault(s => s.LibraryThumbId == Convert.ToInt32((sender as FrameworkElement).Tag));
                if (selectedAudioItem != null && File.Exists(selectedAudioItem.AttachmentLocalPath))
                {
                    string filePath = selectedAudioItem.AttachmentLocalPath;
                    _boardview.canv_audioparent.Visibility = Visibility.Visible;
                    _boardview.txt_result_voicenotes.Visibility = Visibility.Visible;
                    _boardview.canv_audiofiles.Visibility = Visibility.Collapsed;

                    _boardview.audioplayer.Source = new Uri(filePath);
                    _boardview.txt_result_voicenotes.Text = selectedAudioItem.TextInfo;

                    _boardview.audio_play.Visibility = Visibility.Hidden;
                    _boardview.audio_pause.Visibility = Visibility.Visible;
                    _boardview.txt_recordingitem_name.Text = selectedAudioItem.AttachmentName;
                    PlayAudioFile();
                }
                else
                {
                    Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Add media item to library along with board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_new_addmedia_TouchUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.canv_participants.Visibility = Visibility.Collapsed;
                Canvas selectedItem = sender as Canvas;
                if (selectedItem != null)
                {
                    if (Convert.ToString(selectedItem.Tag) == "MEDIA")
                    {
                        ImportMediaItems("fromLibrary");
                        UIElement uielement = sender as UIElement;
                        uielement.ReleaseAllTouchCaptures();
                        uielement.ReleaseMouseCapture();
                        uielement.ReleaseStylusCapture();
                    }
                    else if (Convert.ToString(selectedItem.Tag) == "DECISIONS")
                    {
                        _boardview.tbk_agenda_window_name.Text = "Decisions";

                        _boardview.txt_note.Visibility = Visibility.Collapsed;

                        _boardview.txt_note.Text = "";
                        _boardview.inkcanv_decision.Strokes.Clear();

                        _boardview.inkcanv_decision.Visibility = Visibility.Visible;
                        _boardview.canv_agenda_popup.Visibility = Visibility.Visible;
                        _boardview.sp_task.Visibility = Visibility.Collapsed;
                    }
                    else if (Convert.ToString(selectedItem.Tag) == "TASKS")
                    {
                        _boardview.tbk_agenda_window_name.Text = "Tasks";

                        _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;

                        _boardview.txt_note.Text = "";
                        _boardview.inkcanv_decision.Strokes.Clear();

                        _boardview.txt_note.Visibility = Visibility.Visible;
                        _boardview.canv_agenda_popup.Visibility = Visibility.Visible;

                        _boardview.sp_task.Visibility = Visibility.Visible;
                        SearchedContactText = "";
                        _boardview.lb_task_members.ItemsSource = null;
                        _boardview.txt_note.Tag = "";
                    }
                    else if (Convert.ToString(selectedItem.Tag) == "NOTES")
                    {
                        _boardview.tbk_agenda_window_name.Text = "Notes";

                        _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;

                        _boardview.txt_note.Text = "";
                        _boardview.inkcanv_decision.Strokes.Clear();

                        _boardview.txt_note.Visibility = Visibility.Visible;
                        _boardview.canv_agenda_popup.Visibility = Visibility.Visible;
                        _boardview.sp_task.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Open library item options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void libraryitem_OptionsTapped(object sender, StylusDownEventArgs e)
        {
            e.Handled = true;
            Border borderLibraryOption = (((sender as Canvas).Parent as Canvas).Children[4] as Border);
            borderLibraryOption.Visibility = borderLibraryOption.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Delete library item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void libraryitem_DeleteTapped(object sender, StylusDownEventArgs e)
        {
            try
            {
                e.Handled = true;
                _selectedLibraryItem = ((((sender as Canvas).Parent as StackPanel).Parent as Border).Parent as Canvas).Parent as StackPanel;
                if (_selectedLibraryItem != null)
                {
                    Canvas canvItem = (_selectedLibraryItem).Children[0] as Canvas;
                    _deletelibraryItem = Convert.ToString(canvItem.Tag);

                    List<string> filesList = Directory.GetFiles(Constants.AttachmentResources).ToList();

                    if (!_deletelibraryItem.ToLower().StartsWith("file") && !string.IsNullOrWhiteSpace(_deletelibraryItem))
                        _deletelibraryItem = Path.GetFileName(filesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(_deletelibraryItem)));

                    if (!string.IsNullOrWhiteSpace(_deletelibraryItem) && NxgUtilities.IsValidImageExtension(Path.GetExtension(_deletelibraryItem).ToLower()))
                        _annotationsModuleList = Service.GetModuleDataList<ImageAnnotations>(null);
                    else
                        _annotationsModuleList = null;

                    int count = _annotationsModuleList != null && _annotationsModuleList.Count > 0 ? _annotationsModuleList.Where(s => s.LibraryThumbId == _mediaAnnotaionsList.FirstOrDefault(k => k.AttachmentUid != null && _deletelibraryItem.Contains(Path.GetFileNameWithoutExtension(k.AttachmentUid))).LibraryThumbId).Count() : 0;
                    string data = "This item placed in " + count + " Location" + (count > 1 ? "s, " : ", ");
                    Messenger.Default.Send(new KeyValuePair<string, string>("Delete Library Item", (count > 0 ? data : "") + "This item will be permanently deleted from the Wall-X. Do you want to proceed?"), "Result");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// delete item from library along with db
        /// </summary>
        private void DeleteLibraryItem(string param)
        {
            Application.Current.Dispatcher.InvokeAsync((Action)(() =>
            {
                Canvas sender = (_selectedLibraryItem.Children[2] as Canvas).Children[2] as Canvas;
                if (sender != null)
                {
                    LibraryThumbs deleteItem = _selectedLibraryItem.DataContext as LibraryThumbs;

                    deleteItem = deleteItem == null ? _mediaAnnotaionsList.Where(s => !string.IsNullOrWhiteSpace(s.AttachmentUid)).ToList().FirstOrDefault(s => Path.GetFileNameWithoutExtension(_deletelibraryItem).EndsWith(s.AttachmentUid)) : deleteItem;
                    if (Service.InsertOrUpdateDataToDB(deleteItem, CrudActions.Delete, deleteItem.LibraryThumbId) > 0)
                    {
                        if (deleteItem.AttachmentType != AttachmentType.Decision.ToString())
                        {
                            foreach (Border item in _boardview.inkCanvas.Children.OfType<Border>().Where(j => j.Uid != null && Path.GetFileNameWithoutExtension(deleteItem.AttachmentUid).Contains(Path.GetFileNameWithoutExtension(j.Uid))).ToList())
                            {
                                _selectedBoardChildren = (item.Child as Grid).Children[1] as FrameworkElement;
                                RemoveBoardItem();
                            }
                            NxgUtilities.DeleteFile((string)(Constants.AttachmentResources + _deletelibraryItem));
                            NxgUtilities.DeleteFile(Constants.AttachmentResourceThumbs + Path.GetFileNameWithoutExtension(_deletelibraryItem) + ".png");
                        }

                        Task.Run(() => { GetlibraryThumbnails(); });
                    }
                    _selectedLibraryItem = null;
                }
            }));
        }

        private void HideDragItemParent(object dropElement, object dragElement)
        {
            if (_boardview.canv_library.IsVisible)
            {
                _boardview.canv_library.Visibility = Visibility.Collapsed;
                _canvDragItemParent = _boardview.canv_library;
            }

            if (_boardview.canv_agenda_info.IsVisible)
            {
                _boardview.canv_agenda_info.Visibility = Visibility.Collapsed;
                _canvDragItemParent = _canvDragItemParent != null ? null : _boardview.canv_agenda_info;
            }
        }

        private void VisibleDragItemParent()
        {
            if (_canvDragItemParent == null)
            {
                _boardview.canv_library.Visibility = Visibility.Visible;
                _boardview.canv_agenda_info.Visibility = Visibility.Visible;
            }
            else if (_canvDragItemParent != null)
            {
                _canvDragItemParent.Visibility = Visibility.Visible;
                _canvDragItemParent = null;
            }
        }

        #region DoubleTap

        Stopwatch _doubleTapStopWatch = new Stopwatch();
        Point _lastTapLocation;
        private bool IsDoubleTap(object sender, StylusEventArgs e)
        {
            try
            {
                Point currentTapPosition = e.GetPosition(sender as UIElement);
                bool tapsAreCloseInDistance = GetDistanceBetweenPoints(currentTapPosition, _lastTapLocation) < 40;
                _lastTapLocation = currentTapPosition;

                TimeSpan elapsed = _doubleTapStopWatch.Elapsed;
                bool tapsAreCloseInTime = (elapsed != TimeSpan.Zero && elapsed < TimeSpan.FromSeconds(1));

                return tapsAreCloseInDistance && tapsAreCloseInTime;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public double GetDistanceBetweenPoints(Point p, Point q)
        {
            try
            {
                double a = p.X - q.X;
                double b = p.Y - q.Y;
                double distance = Math.Sqrt(a * a + b * b);
                return distance;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Child animation

        Storyboard sb_boardchild_animate;
        Border _uiElement = null;

        private void StartScaleAnimation(Border uiElement)
        {
            sb_boardchild_animate = new Storyboard();
            _uiElement = uiElement;

            QuarticEase qe = new QuarticEase();
            qe.EasingMode = EasingMode.EaseOut;

            DoubleAnimationUsingKeyFrames daukf1 = new DoubleAnimationUsingKeyFrames();
            EasingDoubleKeyFrame edkf1 = new EasingDoubleKeyFrame(0.250, TimeSpan.FromSeconds(0));
            EasingDoubleKeyFrame edkf2 = new EasingDoubleKeyFrame(1, TimeSpan.FromSeconds(0.5), qe);
            daukf1.KeyFrames.Add(edkf1);
            daukf1.KeyFrames.Add(edkf2);

            MatrixTransform childMatrixTransform = uiElement.RenderTransform as MatrixTransform;
            Canvas.SetLeft(uiElement, childMatrixTransform.Matrix.OffsetX);
            Canvas.SetTop(uiElement, childMatrixTransform.Matrix.OffsetY);

            ScaleTransform st = new ScaleTransform();
            uiElement.RenderTransform = st;
            uiElement.RenderTransformOrigin = new Point(0.5, 0.5);

            st.BeginAnimation(ScaleTransform.ScaleXProperty, daukf1);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, daukf1);
            Storyboard.SetTarget(daukf1, uiElement);
            Storyboard.SetTargetProperty(daukf1, new PropertyPath(ScaleTransform.ScaleXProperty));
            Storyboard.SetTargetProperty(daukf1, new PropertyPath(ScaleTransform.ScaleYProperty));

            sb_boardchild_animate.Completed += Sb_boardchild_animate_Completed;
            sb_boardchild_animate.Begin();
        }

        private void Sb_boardchild_animate_Completed(object sender, object e)
        {
            if (sb_boardchild_animate != null)
            {
                sb_boardchild_animate.Stop();
                sb_boardchild_animate.Remove();
                sb_boardchild_animate.Completed -= Sb_boardchild_animate_Completed;
                sb_boardchild_animate = null;
            }
            VisibleDragItemParent();
        }

        #endregion

        #endregion

        #region Desktop Mode

        /// <summary>
        /// Adding desktop image to library
        /// </summary>
        /// <param name="param"></param>
        public void AddDesktopImgToLibrary(KeyValuePair<List<string>, string> param)
        {
            try
            {
                List<string> listImages = param.Key;
                if (listImages != null && listImages.Count > 0)
                {
                    _isFromLibrary = true;
                    foreach (string filePath in listImages)
                    {
                        string response = Service.UploadFile(filePath);
                        if (!string.IsNullOrWhiteSpace(response))
                            AddChildToBoard(filePath, response, AttachmentType.Capture);
                    }
                }

                _isFromLibrary = false;
                string imgFilePath = param.Value;
                if (imgFilePath != null && imgFilePath.Length > 0)
                {
                    string response = Service.UploadFile(imgFilePath);
                    if (!string.IsNullOrWhiteSpace(response))
                        AddChildToBoard(imgFilePath, response, AttachmentType.Capture);
                }

                if (listImages != null && listImages.Count > 0 && _boardview.canv_library.IsVisible)
                    App.ExecuteMethod(GetlibraryThumbnails, true);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Desktop Mode

        #region Email

        #region Variables


        List<string> emailInvitees = new List<string>();
        string emailSubject = string.Empty;
        string emailBody = string.Empty;

        #endregion Variables

        #region Events

        /// <summary>
        /// To maximize email pdf view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_max_pdf_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_minimize_pdf.Visibility = Visibility.Visible;
            _boardview.canv_maximize_pdf.Visibility = Visibility.Collapsed;
            _boardview.viewbox_pdf_send.Height = 1080;
            _boardview.viewbox_pdf_send.Width = 1920;
            Canvas.SetLeft(_boardview.viewbox_pdf_send, 0);
            Canvas.SetTop(_boardview.viewbox_pdf_send, 0);
        }
        /// <summary>
        /// To minimize email pdf view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_min_pdf_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            _boardview.canv_minimize_pdf.Visibility = Visibility.Collapsed;
            _boardview.canv_maximize_pdf.Visibility = Visibility.Visible;
            _boardview.viewbox_pdf_send.Height = 569;
            _boardview.viewbox_pdf_send.Width = 1069;
            Canvas.SetLeft(_boardview.viewbox_pdf_send, 426);
            Canvas.SetTop(_boardview.viewbox_pdf_send, 259);
        }

        /// <summary>
        /// close email pdf view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_pdf_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.viewbox_pdf_send.Visibility = Visibility.Collapsed;
                App.ExecuteMethod(ClearPdfData);
                FillColortoSelectedUtility(_boardview.canv_email.Children[0] as ShapePath, false);
                FillColortoSelectedUtility(_boardview.canv_mom.Children[0] as ShapePath, false);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// if we press enter add  new textbox to listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_address_item_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                App.ExecuteMethod(() =>
                {
                    e.Handled = true;
                    if (e.Key == Key.Enter)
                    {
                        string emailId = "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            emailId = ((sender as Border).Child as TextBox).Text;
                        });
                        if (NxgUtilities.IsValidEmail(emailId))
                        {
                            App.ExecuteMethod(AddItemtoAddress);
                        }
                        else
                        {
                            Messenger.Default.Send("A valid email is required to send pdf", "Notification");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To enter new email address, add textbox to listbox 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listbox_pdf_to_address_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_boardview.listbox_pdf_to_address.Items.Count == 0)
                    App.ExecuteMethod(AddItemtoAddress);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_add_mail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.ExecuteMethod(AddItemtoAddress);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// cancel particular email address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void img_cancel_email_PointerPressed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                List<Employees> items = _boardview.listbox_pdf_to_address.Items.Cast<Employees>().ToList();
                int participantIndex = items.IndexOf(items.FirstOrDefault(s => s.Email == Convert.ToString((sender as Image).Tag)));
                _boardview.listbox_pdf_to_address.Items.RemoveAt(participantIndex);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// send email 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_send_mail_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.ExecuteDispatcherMethod(() =>
                {
                    List<Employees> items = _boardview.listbox_pdf_to_address.Items.Cast<Employees>().ToList();
                    if (items != null && items.Count == 1 && string.IsNullOrWhiteSpace(items[0].Email))
                        Messenger.Default.Send("Please add at least one recipients email address to post the message", "Notification");
                    else if (items != null && items.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(_boardview.txt_subject.Text))
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                if (items[i].Email == "" || !NxgUtilities.IsValidEmail(Convert.ToString(items[i].Email.ToLower().Trim())))
                                {
                                    ContentPresenter content = TemplateModifier.GetContentPresenter(_boardview.listbox_pdf_to_address, i);
                                    if (content != null)
                                    {
                                        StackPanel stack = content.ContentTemplate.FindName("stack_email", content) as StackPanel;
                                        if (stack != null)
                                        {
                                            items[i].Email = ((stack.Children[0] as Border).Child as TextBox).Text;
                                            if (items[i].Email == "" || !NxgUtilities.IsValidEmail(Convert.ToString(items[i].Email.ToLower().Trim())))
                                            {
                                                items.RemoveAt(i);
                                                _boardview.listbox_pdf_to_address.Items.RemoveAt(i);
                                                i--;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    emailInvitees.Add(items[i].Email);
                                }
                            }

                            CheckBoardInsertorUpdate();

                            App.ExecuteMethod(() => { ClearSelectedBoardItem(); });

                            emailSubject = _boardview.txt_subject.Text;

                            bool isFromMinutesOfClass = Convert.ToBoolean(_boardview.cbIsForMinutesOfClass.IsChecked);

                            emailSubject = (isFromMinutesOfClass ? "Minutes of class " + emailSubject : "Class " + emailSubject) + " " + _currentClass.StartTime.ToString("dd MMM yyyy hh:mm tt");
                            emailBody = new TextRange(_boardview.rtb_email_pdf.Document.ContentStart, _boardview.rtb_email_pdf.Document.ContentEnd).Text;

                            if (emailInvitees.Count > 0)
                            {
                                Messenger.Default.Send("Your request taken. Pdf link will send to the mentioned recipients", "Notification");

                                if (isFromMinutesOfClass)
                                    App.ExecuteMethod(() => { GenerateMinutesOfClass(); });
                                else
                                    App.ExecuteMethod(() => { GenerateXPS(); });
                                //App.ExecuteMethod(() => { GenerateMinutesOfMeeting(); });
                            }

                            _boardview.viewbox_pdf_send.Visibility = Visibility.Collapsed;
                            //_boardview.txt_subject.Text = "Meeting wall generated PDF document";
                            _boardview.rtb_email_pdf.Document.Blocks.Clear();
                            _boardview.listbox_pdf_to_address.Items.Clear();
                            FillColortoSelectedUtility(_boardview.canv_email.Children[0] as ShapePath, false);
                            FillColortoSelectedUtility(_boardview.canv_mom.Children[0] as ShapePath, false);
                        }
                        else
                        {
                            Messenger.Default.Send("subject of the message is empty. Please add something there to post it", "Notification");
                        }
                    }
                    else
                    {
                        Messenger.Default.Send("Please add at least one recipients email address to post the message", "Notification");
                    }
                }, true);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// To show email options to enter email ID's
        /// </summary>
        private void ShowEmailSendingOption(string param = "")
        {
            try
            {
                if (_boardview.viewbox_pdf_send.Visibility != Visibility.Visible)
                {
                    _boardview.viewbox_pdf_send.Height = 569;
                    _boardview.viewbox_pdf_send.Width = 1069;
                    Canvas.SetLeft(_boardview.viewbox_pdf_send, 426);
                    Canvas.SetTop(_boardview.viewbox_pdf_send, 259);
                    _boardview.canv_minimize_pdf.Visibility = Visibility.Collapsed;
                    _boardview.canv_maximize_pdf.Visibility = Visibility.Visible;
                    LoadPreEmailAddresses();
                    _boardview.viewbox_pdf_send.Visibility = Visibility.Visible;
                    _boardview.txt_pdfemail_from_address.Text = Constants.UserName;

                    _boardview.txt_subject.Text = "Report";

                    EmailDocument = _boardview.txt_class_title.Text + "_" + _currentClass.StartTime.ToString("dd MMM_hh:mm tt");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear pdf data
        /// </summary>
        private void ClearPdfData()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _boardview.txt_subject.Text = "Wall-X generated PDF document";
                    _boardview.rtb_email_pdf.Document.Blocks.Clear();
                    _boardview.listbox_pdf_to_address.Items.Clear();
                });
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// To load email addresses in current class
        /// </summary>
        private void LoadPreEmailAddresses()
        {
            try
            {
                string[] emailAdrs = ParticipantsList.Select(s => s.Employee.Email).ToArray();
                for (int i = 0; i < emailAdrs.Length; i++)
                {
                    Employees participant = new Employees();
                    participant.Email = emailAdrs[i];
                    _boardview.listbox_pdf_to_address.Items.Add(participant);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// adding singel item to listbox
        /// </summary>
        private void AddItemtoAddress()
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _boardview.listbox_pdf_to_address.Items.Add(new Employees() { Email = "" });
                    _boardview.listbox_pdf_to_address.UpdateLayout();
                    _boardview.listbox_pdf_to_address.ScrollIntoView(_boardview.listbox_pdf_to_address.Items[_boardview.listbox_pdf_to_address.Items.Count - 1]);
                    ContentPresenter content = TemplateModifier.GetContentPresenter(_boardview.listbox_pdf_to_address, _boardview.listbox_pdf_to_address.Items.Count - 1);
                    if (content != null)
                    {
                        TextBox textbox = (content.ContentTemplate.FindName("txt_address", content) as TextBox);
                        if (textbox != null)
                        {
                            textbox.Focus();
                            Keyboard.Focus(textbox);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// generate xps document from board data
        /// </summary>
        public void GenerateXPS(string param = "")
        {
            try
            {
                if (_pagesList != null && _pagesList.Count > 0)
                {
                    List<PageContent> pagesListData = new List<PageContent>();
                    _annotationsModuleList = Service.GetModuleDataList<ImageAnnotations>(null);
                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass);
                    DispatcherOperation disOp = Application.Current.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        Package package = Package.Open(Constants.ProjectResources + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle) + ".xps", FileMode.Create);
                        XpsDocument doc = new XpsDocument(package);
                        XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                        FixedDocument fixedDocumentChild = new FixedDocument();

                        for (int i = 0; i < 4; i++)
                        {
                            Canvas childCanvas = null;
                            if (i == 0)
                            {
                                childCanvas = _boardview.FindResource("canv_pdf_mainpage") as Canvas;
                                if (childCanvas != null)
                                {
                                    ((childCanvas.Children[1] as Grid).Children[0] as TextBlock).Text = ClassTitle;
                                    ((childCanvas.Children[2] as StackPanel).Children[0] as TextBlock).Text = _currentClass.StartTime.ToString("dd MMMM, yyyy - hh:mm tt");

                                    if (_participantsList.Where(s => s.IsOrganizer == true).Count() > 0)
                                        ((childCanvas.Children[2] as StackPanel).Children[1] as TextBlock).Text = (_participantsList != null && _participantsList.Count > 0) ? ("Organized by\n\n" + _participantsList.FirstOrDefault(s => s.IsOrganizer == true).Employee.Name) : "";
                                }
                                if (childCanvas != null)
                                {
                                    Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                    FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                    fp.Children.Add(vb);
                                    fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                    childCanvas = null;
                                }
                            }
                            else if (i == 1)
                            {
                                childCanvas = _boardview.FindResource("canv_pdf_contents") as Canvas;
                                StackPanel sp = childCanvas.Children[1] as StackPanel;
                                if (AgendasList == null || AgendasList.Count <= 0)
                                {
                                    (sp.Children[1] as FrameworkElement).Visibility = Visibility.Collapsed;
                                    (sp.Children[2] as FrameworkElement).Visibility = Visibility.Collapsed;

                                }
                                if (ParticipantsList == null || ParticipantsList.Count <= 0)
                                {
                                    (sp.Children[3] as FrameworkElement).Visibility = Visibility.Collapsed;
                                    (sp.Children[4] as FrameworkElement).Visibility = Visibility.Collapsed;
                                }
                                if (_mediaAnnotaionsList == null || _mediaAnnotaionsList.Where(s => s.AttachmentType.ToLower() == "decision").ToList().Count <= 0)
                                {
                                    (sp.Children[7] as FrameworkElement).Visibility = Visibility.Collapsed;
                                    (sp.Children[8] as FrameworkElement).Visibility = Visibility.Collapsed;
                                }
                                if (childCanvas != null)
                                {
                                    Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                    FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                    fp.Children.Add(vb);
                                    fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                    childCanvas = null;
                                }
                            }
                            else if (i == 2 && AgendasList != null && AgendasList.Count > 0)
                            {
                                for (int j = 0; j < AgendasList.Count; j++)
                                {
                                    if (j % 6 == 0)
                                    {
                                        childCanvas = _boardview.FindResource("canv_pdf_agendapage") as Canvas;
                                    }
                                    Canvas canv = _boardview.FindResource("canv_agendapage") as Canvas;
                                    (canv.Children[0] as TextBlock).Text = AgendasList[j].StartTime.ToString("hh:mm tt");
                                    (canv.Children[2] as TextBlock).Text = _participantsList.FirstOrDefault(s => s.EmployeeId == AgendasList[j].EmployeeId).Employee.FirstName;
                                    (canv.Children[3] as TextBlock).Text = AgendasList[j].AgendaName;
                                    (childCanvas.Children[1] as StackPanel).Children.Add(canv);
                                    if ((j + 1) % 6 == 0 || j == AgendasList.Count - 1)
                                    {
                                        if (childCanvas != null)
                                        {
                                            Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                            FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                            fp.Children.Add(vb);
                                            fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                            childCanvas = null;
                                        }
                                    }
                                }
                            }
                            else if (i == 3 && ParticipantsList != null && ParticipantsList.Count > 0)
                            {
                                childCanvas = _boardview.FindResource("canv_pdf_participants") as Canvas;
                                var organizer = ParticipantsList.FirstOrDefault(s => s.IsOrganizer);
                                if (organizer == null)
                                {
                                    organizer = ParticipantsList[0];
                                }
                                if (organizer != null)
                                {
                                    (((childCanvas.Children[1] as StackPanel).Children[0] as Ellipse).Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(organizer.Employee.Image, UriKind.Absolute));
                                    (((childCanvas.Children[1] as StackPanel).Children[1] as StackPanel).Children[1] as TextBlock).Text = organizer.Employee.Name;
                                }
                                var participants = ParticipantsList;
                                if (organizer != null)
                                    participants = ParticipantsList.Where(s => s.ParticipantId != organizer.ParticipantId).ToList();
                                for (int j = 0; j < participants.Count; j++)
                                {
                                    var participant = participants[j];
                                    if (j < 12)
                                    {
                                        StackPanel stackPanel = _boardview.FindResource("sp_pdf_participants") as StackPanel;
                                        ((stackPanel.Children[0] as Ellipse).Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(participant.Employee.Image, UriKind.Absolute));
                                        (stackPanel.Children[1] as TextBlock).Text = participant.Employee.Name;
                                        (childCanvas.Children[2] as WrapPanel).Children.Add(stackPanel);
                                        if (j == 11 || j == participants.Count - 1)
                                        {
                                            Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                            FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                            fp.Children.Add(vb);
                                            fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                            childCanvas = null;
                                        }
                                    }
                                    else
                                    {
                                        if (j == 12 || (j - 12) % 16 == 0)
                                        {
                                            childCanvas = _boardview.FindResource("canv_pdf_participants_next_page") as Canvas;
                                        }
                                        StackPanel stackPanel = _boardview.FindResource("sp_pdf_participants") as StackPanel;
                                        ((stackPanel.Children[0] as Ellipse).Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(participant.Employee.Image, UriKind.Absolute));
                                        (stackPanel.Children[1] as TextBlock).Text = participant.Employee.Name;
                                        (childCanvas.Children[1] as WrapPanel).Children.Add(stackPanel);
                                        if ((j - 11) % 16 == 0 || j == participants.Count - 1)
                                        {
                                            Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                            FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                            fp.Children.Add(vb);
                                            fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                            childCanvas = null;
                                        }
                                    }
                                }
                                if (participants.Count == 0 && organizer != null)
                                {
                                    Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                    FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                    fp.Children.Add(vb);
                                    fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                    childCanvas = null;
                                }
                            }
                        }

                        for (int i = 0; i < _pagesList.Count; i++)
                        {
                            Canvas childCanvas = new Canvas();
                            childCanvas.Height = 3240;
                            childCanvas.Width = 5760;
                            childCanvas.Background = Brushes.Transparent;
                            childCanvas.ClipToBounds = true;

                            InkCanvas inkcanv = XamlReader.Parse(_pagesList[i].InkStrokes) as InkCanvas;
                            inkcanv.RenderTransform = new MatrixTransform(new Matrix(1, 0, 0, 1, 0, 0));
                            inkcanv.EditingMode = InkCanvasEditingMode.None;
                            inkcanv.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(Convert.ToString(_boardview.lightblueBg.Tag), UriKind.Absolute)) };

                            childCanvas.Children.Add(inkcanv);

                            //List<ImageAnnotations> imageAnnotationList = _annotationsModuleList.Where(s => s.BoardAnnotationId == _pagesList[i].AnnotationId).ToList();
                            //foreach (ImageAnnotations annotations in imageAnnotationList)
                            //{
                            //    AddChildToBoard(annotations, childCanvas);
                            //}
                            Viewbox footerImage = _boardview.FindResource("vb_pdf_footer") as Viewbox;
                            childCanvas.Children.Add(footerImage);
                            Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                            FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                            fp.Children.Add(vb);

                            fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                            //  childCanvas.Children.Remove(footerImage);
                        }

                        if (_mediaAnnotaionsList.Where(s => s.AttachmentType.ToLower() == "decision").ToList().Count > 0)
                        {
                            Canvas childCanvas = _boardview.FindResource("canv_pdf_decisions") as Canvas;
                            List<IGrouping<int, LibraryThumbs>> agendas = _mediaAnnotaionsList.Where(s => s.AttachmentType.ToLower() == "decision").ToList().GroupBy(s => s.AgendaId).ToList();
                            int rowCount = 0;
                            bool islastItem = false;
                            foreach (var agenda in agendas)
                            {
                                WrapPanel wp = _boardview.FindResource("wp_pdf_decision") as WrapPanel;
                                (wp.Children[0] as StackPanel).Visibility = Visibility.Visible;

                                int k = 0;
                                int j = 0;
                                islastItem = true;
                                var thumbs = _mediaAnnotaionsList.Where(s => s.AttachmentType.ToLower() == "decision" && s.AgendaId == agenda.Key).ToList();
                                if (thumbs.Count > 0)
                                {
                                    ((wp.Children[0] as StackPanel).Children[0] as TextBlock).Text = AgendasList.FirstOrDefault(s => s.AgendaId == agenda.Key).AgendaName;
                                    (((wp.Children[0] as StackPanel).Children[1] as Ellipse).Fill as ImageBrush).ImageSource = new BitmapImage(new Uri(ParticipantsList.FirstOrDefault(s => s.ParticipantId == thumbs[0].ParticipantId).Employee.Image, UriKind.Absolute));
                                    ((wp.Children[0] as StackPanel).Children[2] as TextBlock).Text = ParticipantsList.FirstOrDefault(s => s.ParticipantId == thumbs[0].ParticipantId).Employee.Name;
                                }

                                foreach (LibraryThumbs thumb in thumbs)
                                {
                                    k++;
                                    j++;
                                    StackPanel stack = _boardview.FindResource("sp_decision") as StackPanel;
                                    ((stack.Children[0] as Viewbox).Child as InkCanvas).Strokes = XamlReader.Parse(thumb.StrokeData) as StrokeCollection;
                                    (stack.Children[1] as TextBlock).Text = thumb.TextInfo;
                                    (wp.Children[1] as WrapPanel).Children.Add(stack);
                                    if (k % 4 == 0 || j == thumbs.Count)
                                    {
                                        rowCount++;
                                        if (rowCount % 3 == 0)
                                        {
                                            if (childCanvas != null)
                                            {
                                                (childCanvas.Children[2] as StackPanel).Children.Add(wp);
                                                Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                                FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                                fp.Children.Add(vb);
                                                fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                                childCanvas = null;
                                                childCanvas = _boardview.FindResource("canv_pdf_decisions") as Canvas;
                                                if (j != thumbs.Count)
                                                {
                                                    wp = _boardview.FindResource("wp_pdf_decision") as WrapPanel;
                                                    (wp.Children[0] as StackPanel).Visibility = Visibility.Collapsed;
                                                }
                                            }
                                        }
                                        k = 0;
                                    }
                                }
                                if (rowCount % 3 != 0)
                                {
                                    (childCanvas.Children[2] as StackPanel).Children.Add(wp);
                                    islastItem = false;
                                }
                            }
                            if (!islastItem && childCanvas != null)
                            {
                                Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                fp.Children.Add(vb);
                                fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                childCanvas = null;
                            }
                        }

                        {
                            Canvas childCanvas = _boardview.FindResource("canv_pdf_lastpage") as Canvas;
                            if (childCanvas != null)
                            {
                                Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                                FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                                fp.Children.Add(vb);
                                fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                                childCanvas = null;
                            }
                        }
                        writer.Write(fixedDocumentChild);
                        doc.Close();
                        package.Close();
                    }));

                    DispatcherOperationStatus status = disOp.Status;
                    while (disOp.Status != DispatcherOperationStatus.Completed)
                    {
                        status = disOp.Wait(TimeSpan.FromMilliseconds(1000));
                    }

                    string fileNameWithOutExtension = (Constants.ProjectResources + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle));

                    Aspose.Pdf.XpsLoadOptions options = new Aspose.Pdf.XpsLoadOptions();
                    Aspose.Pdf.Document document = new Aspose.Pdf.Document(fileNameWithOutExtension + ".xps", options);
                    document.Encrypt(_currentClass.Password, "the@1234", Aspose.Pdf.Permissions.AssembleDocument, Aspose.Pdf.CryptoAlgorithm.AESx128);
                    document.Save(fileNameWithOutExtension + ".pdf");

                    NxgUtilities.DeleteFile(fileNameWithOutExtension + ".xps");

                    string fileName = fileNameWithOutExtension + ".pdf";

                    string localFileResponse = Service.UploadFile(fileName);

                    string serverAttachmentLink = App._regLicence.UploadFile(new List<string> { fileName }, "Attachments");

                    if (!string.IsNullOrWhiteSpace(serverAttachmentLink))
                    {
                        App.ExecuteMethod(() => { SendEmailToParticipants(serverAttachmentLink); }, true);
                    }

                    if (!string.IsNullOrWhiteSpace(localFileResponse))
                    {
                        LibraryThumbs attachedMedia = new LibraryThumbs { AttachmentTypeId = (int)AttachmentType.Media_Pdf, AttachmentType = AttachmentType.Media_Pdf.ToString(), Attachment = localFileResponse, ClassId = _currentClass.ClassId };
                        int pk_id = Service.InsertOrUpdateDataToDB(attachedMedia, CrudActions.Create);
                        if (pk_id > 0)
                        {
                            string localPath = Constants.AttachmentResources + "File_" + pk_id + "_" + attachedMedia.AttachmentUid + ".pdf";
                            File.Move(Constants.ProjectResources + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle) + ".pdf", localPath);

                            GenerateThumb.GenerateThumbnail(localPath, Constants.AttachmentResourceThumbs, ".png");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Sending email to participants
        /// </summary>
        /// <returns></returns>
        public void SendEmailToParticipants(string fileName)
        {
            try
            {
                if (NxgUtilities.IsInternetAvailable())
                {
                    if (EMailer.SendEmailToClient(Constants.UserName, Constants.Password, string.Join(",", emailInvitees), emailSubject, emailBody + "<br/> PFA <br/><br/>" + fileName, null))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messenger.Default.Send("Your message was posted to the mentioned recipients", "Notification");
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Messenger.Default.Send("Either there is no internet connection or it is too slow. Please check it and try again", "Notification");
                        });
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messenger.Default.Send("Either there is no internet connection or it is too slow. Please check it and try again", "Notification");
                    });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ClearSelectedBoardItem(bool borRemoveRequired = true)
        {
            try
            {
                if (_selectedBoardChildren != null)
                {
                    if (_selectedBoardChildren is TextBlock)
                    {
                        (_selectedBoardChildren as TextBlock).Foreground = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        Border dataitem = (_selectedBoardChildren.Parent as Grid).Parent as Border;
                        if (dataitem != null)
                        {
                            Grid item = dataitem.Child as Grid;
                            Image img_Ink = item.Children[0] as Image;
                            InkCanvas img_InkCanvas = item.Children[1] as InkCanvas;
                            MatrixTransform matrix = dataitem.RenderTransform as MatrixTransform;
                            if (matrix != null)
                            {
                                ImageAnnotations imageAnnotation = _imageAnnotaionsList.FirstOrDefault(s => Convert.ToInt32(dataitem.Tag) == s.AnnotationId);
                                if (imageAnnotation != null)
                                {
                                    imageAnnotation.InkStrokes = XamlWriter.Save(img_InkCanvas);
                                    imageAnnotation.Manipulation = XamlWriter.Save(matrix);

                                    int isUpdatedId = Service.InsertOrUpdateDataToDB(imageAnnotation, CrudActions.Update);
                                }
                            }

                            if (borRemoveRequired)
                            {
                                dataitem.BorderBrush = new SolidColorBrush(Colors.Transparent);
                                ((_selectedBoardChildren.Parent as Grid).Parent as Border).IsManipulationEnabled = true;
                                _selectedBoardChildren = null;
                                Clear();
                                //  ResetAll();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// generate Minutes of class 
        /// </summary>
        public void GenerateMinutesOfClass()
        {
            try
            {
                if (_pagesList != null && _pagesList.Count > 0)
                {
                    List<LibraryThumbs> taskList = null;
                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass);

                    if (_mediaAnnotaionsList != null && _mediaAnnotaionsList.Count > 0)
                    {
                        taskList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Task.ToString()).ToList();
                    }
                    DispatcherOperation disOp = Application.Current.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        Package package = Package.Open(Constants.ProjectResources + "MOM_" + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle) + ".xps", FileMode.Create);
                        XpsDocument doc = new XpsDocument(package);
                        XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                        FixedDocument fixedDocumentChild = new FixedDocument();

                        Canvas childCanvas = _boardview.FindResource("canv_mom_mainpage") as Canvas;

                        if (childCanvas != null)
                        {
                            ((childCanvas.Children[1] as Grid).Children[0] as TextBlock).Text = ClassTitle;
                            ((childCanvas.Children[2] as StackPanel).Children[0] as TextBlock).Text = _currentClass.StartTime.ToString("dd MMMM, yyyy - hh:mm tt");

                            if (_participantsList.Where(s => s.IsOrganizer == true).Count() > 0)
                                ((childCanvas.Children[2] as StackPanel).Children[1] as TextBlock).Text = (_participantsList != null && _participantsList.Count > 0) ? ("Organized by\n\n" + _participantsList.FirstOrDefault(s => s.IsOrganizer == true).Employee.Name) : "";

                            AddChildToPage(fixedDocumentChild, childCanvas);

                        }

                        childCanvas = _boardview.FindResource("canv_mom") as Canvas;
                        if (childCanvas != null)
                        {
                            ((childCanvas.Children[1] as Canvas).Children[1] as TextBlock).Text = _currentClass.StartTime.ToString("dd MMM, yyyy");
                            ((childCanvas.Children[1] as Canvas).Children[3] as TextBlock).Text = _currentClass.StartTime.ToString("hh:mm tt") + " - " + _currentClass.EndTime.ToString("hh:mm tt");
                            ((childCanvas.Children[1] as Canvas).Children[5] as TextBlock).Text = Constants.LocationName;

                            ((childCanvas.Children[2] as Canvas).Children[0] as TextBlock).Text = (_currentClass.ParticipantList != null && _currentClass.ParticipantList.Count > 0) ? string.Join(",", _currentClass.ParticipantList.Select(s => s.Employee.Name)) : "-";
                        }

                        if (taskList != null && taskList.Count > 0)
                        {
                            (childCanvas.Children[3] as Canvas).Visibility = Visibility.Visible;
                            List<IGrouping<int, LibraryThumbs>> agendas = taskList.GroupBy(s => s.AgendaId).ToList();
                            bool isfirstPage = true;
                            int rowCount = 0;

                            if (childCanvas != null)
                            {
                                foreach (var agenda in agendas)
                                {
                                    StackPanel spAgendas = _boardview.FindResource("sp_mom_agenda") as StackPanel;
                                    var thumbs = taskList.Where(s => s.AgendaId == agenda.Key).ToList();

                                    if (spAgendas != null)
                                    {
                                        if (thumbs.Count > 0)
                                        {
                                            (((spAgendas.Children[0] as StackPanel).Children[0] as StackPanel).Children[1] as TextBlock).Text = AgendasList.FirstOrDefault(s => s.AgendaId == agenda.Key).AgendaName;

                                            ((((spAgendas.Children[0] as StackPanel).Children[1] as Grid).Children[0] as StackPanel).Children[1] as TextBlock).Text = ParticipantsList.FirstOrDefault(s => s.ParticipantId == thumbs[0].ParticipantId).Employee.Name;
                                        }

                                        (childCanvas.Children[4] as StackPanel).Children.Add(spAgendas);
                                        rowCount++;

                                        if ((rowCount % 4 == 0 && isfirstPage) || (rowCount % 9 == 0 && !isfirstPage))
                                        {
                                            rowCount = 0;
                                            AddChildToPage(fixedDocumentChild, childCanvas);
                                            isfirstPage = false;
                                            childCanvas = _boardview.FindResource("canv_mom") as Canvas;
                                        }
                                    }

                                    if (!isfirstPage && childCanvas != null)
                                    {
                                        childCanvas.Children.OfType<Canvas>().ToList().ForEach(s => s.Visibility = Visibility.Collapsed);
                                        Canvas.SetTop((childCanvas.Children[4] as StackPanel), 150);
                                        (childCanvas.Children[4] as StackPanel).Height = 860;
                                    }

                                    foreach (LibraryThumbs thumb in thumbs)
                                    {
                                        Grid gridTasks = _boardview.FindResource("grid_mom_task") as Grid;
                                        if (gridTasks != null)
                                        {
                                            (gridTasks.Children[0] as TextBlock).Text = thumb.TextInfo;
                                            (gridTasks.Children[1] as TextBlock).Text = string.Join(",", HomePageViewModel._contactsDbList.Where(s => thumb.AssignedEmployeePKIDs.Split(',').ToList().Any(x => Convert.ToInt32(x) == s.EmployeeId)).ToList().Select(s => s.Name));

                                            (childCanvas.Children[4] as StackPanel).Children.Add(gridTasks);
                                            rowCount++;
                                        }

                                        if ((rowCount % 4 == 0 && isfirstPage) || (rowCount % 9 == 0 && !isfirstPage))
                                        {
                                            rowCount = 0;
                                            AddChildToPage(fixedDocumentChild, childCanvas);
                                            isfirstPage = false;
                                            childCanvas = _boardview.FindResource("canv_mom") as Canvas;
                                        }
                                    }
                                }

                                if (!isfirstPage)
                                {
                                    childCanvas.Children.OfType<Canvas>().ToList().ForEach(s => s.Visibility = Visibility.Collapsed);
                                    Canvas.SetTop((childCanvas.Children[4] as StackPanel), 150);
                                    (childCanvas.Children[4] as StackPanel).Height = 860;
                                }

                                if ((childCanvas.Children[4] as StackPanel).Children.Count > 0 || agendas == null || agendas.Count == 0)
                                    AddChildToPage(fixedDocumentChild, childCanvas);
                            }
                        }
                        else
                        {
                            (childCanvas.Children[3] as Canvas).Visibility = Visibility.Collapsed;
                            AddChildToPage(fixedDocumentChild, childCanvas);
                        }

                        writer.Write(fixedDocumentChild);
                        doc.Close();
                        package.Close();
                    }));

                    DispatcherOperationStatus status = disOp.Status;
                    while (disOp.Status != DispatcherOperationStatus.Completed)
                    {
                        status = disOp.Wait(TimeSpan.FromMilliseconds(1000));
                    }

                    string fileNameWithOutExtension = (Constants.ProjectResources + "MOM_" + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle));

                    Aspose.Pdf.XpsLoadOptions options = new Aspose.Pdf.XpsLoadOptions();
                    Aspose.Pdf.Document document = new Aspose.Pdf.Document(fileNameWithOutExtension + ".xps", options);
                    document.Save(fileNameWithOutExtension + ".pdf");

                    string fileName = fileNameWithOutExtension + ".pdf";

                    string localFileResponse = Service.UploadFile(fileName);

                    string serverAttachmentLink = App._regLicence.UploadFile(new List<string> { fileName }, "Attachments");

                    if (!string.IsNullOrWhiteSpace(serverAttachmentLink))
                    {
                        App.ExecuteMethod(() => { SendEmailToParticipants(serverAttachmentLink); }, true);
                    }

                    if (!string.IsNullOrWhiteSpace(localFileResponse))
                    {
                        LibraryThumbs attachedMedia = new LibraryThumbs { AttachmentTypeId = (int)AttachmentType.Media_Pdf, AttachmentType = AttachmentType.Media_Pdf.ToString(), Attachment = localFileResponse, ClassId = _currentClass.ClassId };
                        int pk_id = Service.InsertOrUpdateDataToDB(attachedMedia, CrudActions.Create);
                        if (pk_id > 0)
                        {
                            string localPath = Constants.AttachmentResources + "File_" + pk_id + "_" + attachedMedia.AttachmentUid + ".pdf";
                            File.Move(Constants.ProjectResources + "MOM_" + (ClassTitle.Contains("_") ? ClassTitle.Remove(ClassTitle.IndexOf('_')) : ClassTitle) + ".pdf", localPath);

                            GenerateThumb.GenerateThumbnail(localPath, Constants.AttachmentResourceThumbs, ".png");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void AddChildToPage(FixedDocument fixedDocumentChild, Canvas childCanvas)
        {
            if (childCanvas != null)
            {
                Viewbox vb = new Viewbox() { Height = 1080, Width = 1920, Child = childCanvas, Stretch = Stretch.Fill };
                FixedPage fp = new FixedPage() { Height = 1080, Width = 1920 };
                fp.Children.Add(vb);
                fixedDocumentChild.Pages.Add(new PageContent() { Child = fp });
                childCanvas = null;
            }
        }


        #endregion Methods

        #endregion Email

        #region Recurring Class

        private List<Class> _recurringClassesList;

        public void canv_recurring_dropdown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                if (RecurringClassList != null && RecurringClassList.Count > 0)
                {
                    if (_boardview.canv_recurring_class.IsVisible)
                    {
                        _boardview.canv_recurring_class.Visibility = Visibility.Collapsed;
                        FillColortoSelectedUtility(_boardview.canv_recurring_dropdown.Children[0] as ShapePath, false, false);
                    }
                    else
                    {
                        _boardview.canv_recurring_class.Visibility = Visibility.Visible;
                        FillColortoSelectedUtility(_boardview.canv_recurring_dropdown.Children[0] as ShapePath, true, false);
                    }
                }
                else
                {
                    Messenger.Default.Send("This option only for Recurring Class..!", "Notification");
                }
            }
            catch (NoMicrophoneException)
            {
                Messenger.Default.Send("No microphone!", "Notification");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }
        SpeechToText _speechToText = null;
        private void LoadRecurringClass(string recurringClassID)
        {
            try
            {
                _recurringClassesList = Service.GetRecurringClassById(recurringClassID);
                if (_recurringClassesList != null && _recurringClassesList.Count > 0)
                {
                    RecurringClassList = null;
                    RecurringClassList = _recurringClassesList;

                    _boardview.lb_recurring_class.SelectedIndex = RecurringClassList.IndexOf(RecurringClassList.FirstOrDefault(s => s.ClassId == _currentClass.ClassId));
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private Class _selectedMultiClass = null;
        public void lb_recurring_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_boardview.lb_recurring_class.SelectedIndex > -1)
                {
                    NextClass = null;
                    _boardview.canv_next_class.Visibility = Visibility.Collapsed;
                    _boardview.canv_add_to_currentClass.Visibility = Visibility.Collapsed;
                    _boardview.canv_cancel_currentClass.Visibility = Visibility.Collapsed;

                    _selectedMultiClass = RecurringClassList[_boardview.lb_recurring_class.SelectedIndex];

                    if (_actualClass != null && _selectedMultiClass.StartTime > _actualClass.StartTime && _selectedMultiClass.ClassId != _actualClass.ClassId)
                    {
                        _boardview.canv_cancel_currentClass.Visibility = Visibility.Visible;
                    }

                    if (_boardview.lb_recurring_class.SelectedIndex != RecurringClassList.Count - 1)
                    {
                        _boardview.canv_next_class.Visibility = Visibility.Visible;
                        NextClass = RecurringClassList[_boardview.lb_recurring_class.SelectedIndex + 1];
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_view_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_selectedMultiClass != null)
                {
                    _pagesList = Service.GetModuleDataList<BoardAnnotations>(_currentClass).Where(s => s.ClassId == _currentClass.ClassId).ToList();

                    if (_pagesList != null && _pagesList.Count > 0)
                    {
                        bool isSaved = CheckBoardInsertorUpdate();
                        if (isSaved)
                        {
                            ClearChildFromEditing();
                        }
                    }

                    if (_currentClass.ClassId != _selectedMultiClass.ClassId) // To open selected class, If it is already selected, we will not going to open...
                    {
                        Class dataObject = Service.GetClassData(_selectedMultiClass);
                        _currentClass = dataObject;

                        App.ExecuteMethod(LoadClassDataonBoard);
                    }

                    _boardview.canv_add_to_currentClass.Visibility = Visibility.Collapsed;

                    if (_actualClass.ClassId != _currentClass.ClassId)
                    {
                        _boardview.canv_add_to_currentClass.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_cancel_next_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_actualClass != null && RecurringClassList[_boardview.lb_recurring_class.SelectedIndex].ClassId == _actualClass.ClassId)
            {
                _boardview.canv_cancel_currentClass.Visibility = Visibility.Visible;
                Messenger.Default.Send("Can't delete Current Class....!", "Notification");
                return;
            }
            Messenger.Default.Send(new KeyValuePair<string, string>("Cancel next class", "Do you want to Cancel This Class ?"), "Result");
        }

        private void CancelNextClassBoard(string param)
        {
            try
            {
                if (NextClass != null)
                {
                    if (Service.InsertOrUpdateDataToDB(RecurringClassList[_boardview.lb_recurring_class.SelectedIndex], CrudActions.Delete, RecurringClassList[_boardview.lb_recurring_class.SelectedIndex].ClassId) > 0)
                    {
                        _recurringClassesList.Remove(RecurringClassList.FirstOrDefault(s => s.ClassId == RecurringClassList[_boardview.lb_recurring_class.SelectedIndex].ClassId));

                        RecurringClassList = null;
                        RecurringClassList = _recurringClassesList;

                        //show canceled notification
                        //ShowNextMeeting(RecurringMeetingList, BindedRecurringMeetingsList.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_add_to_currentClass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_actualClass.ClassId != _currentClass.ClassId)
                {
                    bool isAdded = AddCurrentPageToActualProject();
                    if (isAdded)
                    {
                        Messenger.Default.Send("Canvas Successfully added to Actual Project....!", "Notification");
                    }
                    else
                    {
                        Messenger.Default.Send("Ooops, something went wrong...!", "Notification");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private bool AddCurrentPageToActualProject()
        {
            int datasaved = -1;
            try
            {
                BoardAnnotations boardAnnotations = NxgUtilities.GetDuplicateOfObject(_pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex));

                if (boardAnnotations != null)
                {
                    boardAnnotations.PageIndex = _actualClass.BoardAnnotationList.Count > 0 ? (_actualClass.BoardAnnotationList.Last().PageIndex) + 1 : 1;
                    boardAnnotations.ClassId = _actualClass.ClassId;

                    datasaved = Service.InsertOrUpdateDataToDB(boardAnnotations, CrudActions.Create);

                    if (datasaved != -1)
                    {
                        boardAnnotations.AnnotationId = datasaved;
                        _actualClass.BoardAnnotationList.Add(boardAnnotations);

                        List<ImageAnnotations> imageAnnotationList = Service.GetModuleDataList<ImageAnnotations>(_currentClass, _pagesList.FirstOrDefault(s => s.PageIndex == _selectedPageIndex).AnnotationId);

                        if (imageAnnotationList != null && imageAnnotationList.Count > 0)
                        {
                            foreach (ImageAnnotations imageAnn in imageAnnotationList)
                            {
                                LibraryThumbs libItem = _mediaAnnotaionsList.FirstOrDefault(s => s.LibraryThumbId == imageAnn.LibraryThumbId);
                                string filePath = "";
                                if (libItem != null && !string.IsNullOrWhiteSpace(libItem.AttachmentLocalPath))
                                {
                                    filePath = Directory.GetFiles(Constants.AttachmentResources).ToList().FirstOrDefault(s => s.Contains(libItem.AttachmentLocalPath));
                                }

                                if (!string.IsNullOrWhiteSpace(filePath))
                                {
                                    if (NxgUtilities.IsValidImageExtension(Path.GetExtension(filePath).ToLower()))
                                    {
                                        string response = Service.UploadFile(filePath);
                                        if (!string.IsNullOrWhiteSpace(response))
                                        {
                                            LibraryThumbs libraryItem = new LibraryThumbs { AttachmentTypeId = (int)AttachmentType.Media_Image, AttachmentType = AttachmentType.Media_Image.ToString(), Attachment = response, ClassId = _actualClass.ClassId };

                                            string localPath = null;
                                            int librarythumbPkId = Service.InsertOrUpdateDataToDB(libraryItem, CrudActions.Create);

                                            if (librarythumbPkId > 0)
                                            {
                                                localPath = Constants.AttachmentResources + "File_" + librarythumbPkId + "_" + libraryItem.AttachmentUid + Path.GetExtension(filePath);

                                                if (File.Exists(filePath))
                                                {
                                                    if (typeof(LibraryThumbs) == typeof(LibraryThumbs) && (libraryItem as LibraryThumbs).AttachmentType.ToLower() == AttachmentType.Capture.ToString())
                                                        File.Move(filePath, localPath);
                                                    else
                                                        File.Copy(filePath, localPath);
                                                }
                                                GenerateThumb.GenerateThumbnail(localPath, Constants.AttachmentResourceThumbs, ".png");
                                            }
                                            imageAnn.LibraryThumbId = librarythumbPkId;
                                            imageAnn.BoardAnnotationId = datasaved;
                                            imageAnn.ClassId = _actualClass.ClassId;
                                        }
                                    }
                                }
                                else
                                {
                                    LibraryThumbs libraryStickyItem = new LibraryThumbs { AttachmentTypeId = (int)AttachmentType.Sticky, AttachmentType = AttachmentType.Sticky.ToString(), Attachment = null, TextInfo = "Sticky_" + DateTime.Now.ToString("hhmmssfff") + "__@__" + Guid.NewGuid().ToString(), ClassId = _actualClass.ClassId };

                                    int insertedId = Service.InsertOrUpdateDataToDB(libraryStickyItem, CrudActions.Create);

                                    if (insertedId > 0)
                                    {
                                        imageAnn.LibraryThumbId = insertedId;
                                        imageAnn.BoardAnnotationId = datasaved;
                                        imageAnn.ClassId = _actualClass.ClassId;
                                    }
                                }

                                int isUpdatedId = Service.InsertOrUpdateDataToDB(imageAnn, CrudActions.Create);
                            }
                        }

                    }
                }
                else
                {
                    Messenger.Default.Send("Ooops...! Page not found", "Notification");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }

            return (datasaved != -1);
        }


        public void canv_close_recurring_class_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.canv_recurring_class.Visibility = Visibility.Collapsed;
                FillColortoSelectedUtility(_boardview.canv_recurring_dropdown.Children[0] as ShapePath, false, false);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Recurring Class

        #region Gestures

        private bool _isFromInkMode = false;
        private bool _isFromHighlighter = false;
        private bool _isLongPressEnabled = false;
        private List<int> _longPressDeviceId = new List<int>();

        public void Tap_2_TapMethod(UIElement ele, TouchEventArgs e)
        {
            if (_isGestureEnabled && !_isLongPressEnabled)
            {
                e.Handled = true;
                if (_boardview.inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    _isFromHighlighter = _boardview.inkCanvas.DefaultDrawingAttributes.IsHighlighter;
                }
                ToolShowMenuComponent(_boardview.inkCanvas.EditingMode == InkCanvasEditingMode.Ink ? _boardview.canv_eraser : !_isFromHighlighter ? _boardview.canv_marker : _boardview.canv_highlighter);
            }
        }

        public void Tap_3_TapMethod(UIElement ele, TouchEventArgs e)
        {
            if (_isGestureEnabled && !_isLongPressEnabled)
            {
                if (e != null)
                    e.Handled = true;

                if (_boardview.inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    _isFromInkMode = true;
                    _isFromHighlighter = _boardview.inkCanvas.DefaultDrawingAttributes.IsHighlighter;
                }

                ToolShowMenuComponent(_boardview.inkCanvas.EditingMode != InkCanvasEditingMode.None ? _boardview.canv_hand : !_isFromInkMode ? _boardview.canv_eraser : !_isFromHighlighter ? _boardview.canv_marker : _boardview.canv_highlighter);
            }
        }

        public void Tap_4_TapMethod(UIElement ele, TouchEventArgs e)
        {
            if (_isGestureEnabled && !_isLongPressEnabled)
            {
                e.Handled = true;
                Messenger.Default.Send("GotoDesktopMode", "DesktopMode");
            }
        }

        /// <summary>
        /// Longpress gesture event with single finger
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="e"></param>
        public void LongPress_2_method(UIElement ele, TouchEventArgs e)
        {
            FrameworkElement frame = ele as FrameworkElement;

            _isLongPressEnabled = true;
            _longPressDeviceId = (ele as Canvas).TouchesOver.ToList().Select(s => s.Id).ToList();
            ToolShowMenuComponent(_boardview.canv_eraser);
        }

        #endregion

        #region SearchEmails

        /// <summary>
        /// Search email from contact db list
        /// </summary>
        /// <param name="emailText"></param>
        private void SearchEmailFromList(object emailTextSender)
        {
            try
            {
                SearchContactList = null;
                TextBox emailTextBox = (emailTextSender as TextBox);
                if (HomePageViewModel._contactsDbList != null && HomePageViewModel._contactsDbList.Count > 0 && emailTextBox != null && !string.IsNullOrWhiteSpace(emailTextBox.Text) && !_isFromEmailSelected)
                {
                    List<Employees> searchedContacts = HomePageViewModel._contactsDbList.Where(s => !string.IsNullOrWhiteSpace(s.Email) && s.Email.ToLower().StartsWith(emailTextBox.Text.ToString().ToLower())).ToList();
                    SearchContactList = searchedContacts.Count > 0 ? searchedContacts : null;

                    if (_boardview.canv_participants.IsVisible && SearchContactList != null && SearchContactList.Count > 0)
                        _boardview.listbox_Participants.Visibility = Visibility.Collapsed;

                    if (_boardview.canv_agenda_popup.IsVisible && SearchContactList != null && SearchContactList.Count > 0)
                        _boardview.lb_task_members.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _boardview.listbox_Participants.Visibility = Visibility.Visible;
                    _boardview.lb_task_members.Visibility = Visibility.Visible;
                }
                _isFromEmailSelected = false;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region zoomus call

        public static ZoomMeeting zoomMeetingData { get; set; }

        /// <summary>
        /// Start zoom meeting by using zoom start uri
        /// </summary>
        /// <param name="startURL"></param>
        private void StartZoomMeeting(string startURL)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (startURL.ToString().Trim().Length > 0)
                    {
                        Process.Start(startURL);
                        //_boardview.webBrowser.Navigate(new Uri(startURL));
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To create new zoom meeting
        /// </summary>
        /// <param name="meetingName"></param>
        /// <param name="startTime"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static ZoomMeeting CreateInsertZoomUsMeetingId(string meetingName, DateTime startTime, int duration = 60)
        {
            try
            {
                duration = duration == 0 ? 60 : duration;

                if (zoomMeetingData == null)
                    zoomMeetingData = ZoomAPIService.addMeeting(Constants.ZoomUserId, meetingName, startTime, duration);

                //zooomMeetingData = ZoomAPIService.CreateMeeting(meetingName, startTime, duration);
                // && ZoomAPIService.SignIn()
                if (zoomMeetingData != null)
                {
                    if (_currentClass != null && _currentClass != null && string.IsNullOrWhiteSpace(_currentClass.ZoomStartUri))
                    {
                        _currentClass.ZoomStartUri = zoomMeetingData.start_url;
                        if (string.IsNullOrWhiteSpace(_currentClass.ZoomHostId))
                            _currentClass.ZoomHostId = zoomMeetingData.host_id;
                    }
                    return zoomMeetingData;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
                return null;
            }
            return zoomMeetingData;
        }

        public async void StartOrJoinZoomVideoCall()
        {
            //Application.Current.Dispatcher.InvokeAsync(() =>
            //{ });
            // Messenger.Default.Send( "Please wait for a while zoom instance is getting start...!", "Notification");
            if (Utilities.IsInternetAvailable())
            {
                Class currentMeeting = _currentClass;

                if (currentMeeting != null)
                {
                    //updating the values of zoom from current db values to get latest values to other partner to join meeting
                    _currentClass.ZoomStartUri = currentMeeting.ZoomStartUri;
                    _currentClass.ZoomHostId = currentMeeting.ZoomHostId;
                    _currentClass.IsZoomStarted = currentMeeting.IsZoomStarted;

                    if (!string.IsNullOrWhiteSpace(_currentClass.ZoomStartUri))
                    {
                        string ZoomJoinUri = _currentClass.ZoomJoinUri;
                        if (ZoomJoinUri != null && ZoomJoinUri.ToString().Trim().Length > 0)
                        {
                            string ZoomMeetingID = ZoomJoinUri.Substring((ZoomJoinUri.IndexOf("/j/") + 3), 9);
                            string ZoomHostId = _currentClass.ZoomHostId;
                            if (_currentClass.ZoomHostId == null)
                            {
                                ZoomHostId = Constants.ZoomToken.ToString();
                            }

                            //  ZoomMeeting meeting = ZoomAPIService.GetMeetingInfo(ZoomMeetingID, ZoomHostId);

                            //Pending
                            if (_currentClass.IsZoomStarted == 1)
                            {
                                JoinZoomMeeting();
                                return;
                            }

                            ZoomMeeting meeting = ZoomAPIService.getMeeting(Convert.ToUInt32(ZoomMeetingID));

                            if (meeting.host_id == Constants.ZoomToken && meeting.status == "waiting")
                            {
                                StartZoomMeeting();
                                //if (_currentMeeting.OverviewInfo.IsZoomStarted == 0)
                                //{
                                //    StartZoomMeeting(); // pending by sat for local db
                                //}
                                //else if (_currentMeeting.OverviewInfo.IsZoomStarted == 1)
                                //{
                                //    JoinZoomMeeting();
                                //}
                            }
                            else if (meeting.host_id == Constants.ZoomToken && meeting.status != "waiting")
                            {
                                JoinZoomMeeting();
                            }
                            else
                            {
                                if (_currentClass.IsZoomStarted == 1)
                                {
                                    Task task = new Task(() =>
                                    {
                                        for (int i = 0; i < 5; i++)
                                        {
                                            Thread.Sleep(1000);
                                            meeting = ZoomAPIService.GetMeetingInfo(ZoomMeetingID, ZoomHostId);
                                            if (meeting.status == "waiting" && i == 4)
                                            {
                                                StartZoomMeeting();
                                            }
                                            else if (meeting.status != "waiting")
                                            {
                                                JoinZoomMeeting();
                                                break;
                                            }
                                        }
                                    });
                                    task.Start();
                                    Messenger.Default.Send("Please wait for few seconds..!", "Notification");
                                    await task;
                                }
                                else if (_currentClass.IsZoomStarted == 0)
                                {
                                    if (meeting.status == "waiting")
                                    {
                                        StartZoomMeeting();
                                    }
                                    else if (meeting.status != "waiting")
                                    {
                                        JoinZoomMeeting();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        StartZoomMeeting();
                    }
                }
                else
                {
                    Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification");
                }
            }
            else
            {
                Messenger.Default.Send("Oops..! No internet connection available", "Notification");
            }
        }

        void StartZoomMeeting()
        {
            Messenger.Default.Send("Video Call is getting ready...!", "Notification");

            ZoomMeeting zoomMeeting = null;

            if (string.IsNullOrWhiteSpace(_currentClass.ZoomStartUri) || string.IsNullOrWhiteSpace(_currentClass.ZoomJoinUri))
            {
                zoomMeeting = CreateInsertZoomUsMeetingId(_currentClass.ClassName, _currentClass.StartTime.ToLocalTime(), Convert.ToInt32(TimeSpan.Parse(_currentClass.Duration).TotalMinutes));
                _currentClass.ZoomStartUri = zoomMeeting.start_url;
                _currentClass.ZoomHostId = zoomMeeting.host_id;
                _currentClass.ZoomJoinUri = zoomMeeting.join_url;
            }
            _currentClass.IsZoomStarted = 1;
            int isUpdated = Service.InsertOrUpdateDataToDB(_currentClass, CrudActions.Update);
            Thread.Sleep(1000);
            App.ExecuteMethod(() => StartZoomMeeting(_currentClass.ZoomStartUri));
        }

        void JoinZoomMeeting()
        {
            Messenger.Default.Send("Video Call is getting ready...!", "Notification");

            //ZoomMeeting zoomMeeting = BoardViewModel.CreateInsertZoomUsMeetingId(_currentMeeting.OverviewInfo.MeetingName, _currentMeeting.OverviewInfo.Start_Time.ToLocalTime(), Convert.ToInt32(TimeSpan.Parse(_currentMeeting.OverviewInfo.Duration).TotalMinutes));
            //_currentMeeting.OverviewInfo.IsZoomStarted = 0;
            //int isUpdated = Service.InsertOrUpdateDataToDB(_currentMeeting.OverviewInfo, CrudActions.Update);

            App.ExecuteMethod(() => StartZoomMeeting(_currentClass.ZoomJoinUri));
        }

        #endregion //zoomus call

        #region AudioRecording

        private void RecordedCommandText(string audioText)
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(audioText))
                    {
                        switch (audioText.ToLower().Trim())
                        {
                            case "speak now.":
                                HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Visible;
                                break;
                            case "open browser":
                                Messenger.Default.Send("browser" + "www.google.com", "DesktopMode");
                                break;
                            case "new page":
                            case "new pez":
                                Messenger.Default.Send(new KeyValuePair<string, string>("Add Canvas", "This option will add a new canvas, do you wish to proceed"), "Result");
                                break;
                            case "go to desktop":
                                Messenger.Default.Send("GotoDesktopMode", "DesktopMode");
                                break;
                            case "open youtube":
                                Messenger.Default.Send("browser" + "www.youtube.com", "DesktopMode");
                                break;
                            case "open cisco":
                                Messenger.Default.Send("browser" + "www.cisco.com", "DesktopMode");
                                break;
                            case "start recording":
                                CheckingForScreenRecording();
                                break;
                        }

                        if (audioText.ToLower().Trim() != "speak now.")
                            _speechToText.StopAudioRecording();
                    }
                    else
                        Messenger.Default.Send("Text not found, Please try again.", "Notification");

                    if (string.IsNullOrWhiteSpace(audioText) || audioText.ToLower() != "speak now.")
                    {
                        HomePageViewModel._homePageView.WallXLoader.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (Exception ex)
            {
                ex.InsertException();
            }
        }

        private void StopAudioRecording()
        {
            try
            {
                AudioFileTotalTime = " / " + "00:00:00";
                AudioFileCurrentTime = "00:00:00";
                _currentRecordingTime = "00:00:00";
                _boardview.timer_voicerecord.Text = _currentRecordingTime;

                if (_stopWatch != null)
                {
                    _stopWatch.Stop();
                }

                if (_dispatcherTimer != null)
                {
                    _dispatcherTimer.Stop();
                    _dispatcherTimer.Tick -= new EventHandler(dt_Tick);
                }
                _dispatcherTimer = null;
                _stopWatch = null;

                _textConversionEnabled = false;

                string textData = (_boardview.tbk_voice_txt.Text.Trim() != "Please start speaking.") ? _boardview.tbk_voice_txt.Text : "";

                string response = Service.UploadFile(_voiceFilePath);

                App.Current.Dispatcher.Invoke(() =>
                {
                    AddChildToBoard(_voiceFilePath, response, AttachmentType.Audio, textData);

                    FillColortoSelectedUtility(_boardview.canv_voicenotes.Children[0] as ShapePath, false);

                    GetAudioList();
                    if (_boardview.listbox_audiofiles.Items != null && _boardview.listbox_audiofiles.Items.Count > 0)
                    {
                        isFromRecording = true;
                        _boardview.listbox_audiofiles.SelectedIndex = _boardview.listbox_audiofiles.Items.Count - 1;

                        _boardview.canv_audioparent.Visibility = _boardview.canv_audio_player.Visibility = _boardview.audio_play.Visibility = Visibility.Visible;
                        _boardview.canv_voicerecording.Visibility = _boardview.canv_audiofiles.Visibility = _boardview.txt_result_voicenotes.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void RecordedText(string audioText)
        {
            try
            {
                switch (audioText.ToLower())
                {
                    case "speak now.":
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            StartAudioRecordingTimer();
                            //FillColortoSelectedUtility(_boardview.canv_assistance.Children[0] as ShapePath, false); //check naidu
                        });
                        break;
                    default:
                        if (_textConversionEnabled)
                        {
                            if (string.IsNullOrWhiteSpace(AudioText) || AudioText == "Please start speaking")
                                AudioText = audioText;
                            else
                                AudioText = _audioText += audioText;

                            if (!string.IsNullOrWhiteSpace(audioText))
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    _boardview.tbk_voice_txt.SelectionStart = audioText.Length - 1;
                                });
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.InsertException();
            }
        }

        DispatcherTimer dispatcherTimerPlayer = null;
        DispatcherTimer _dispatcherTimer = null;
        Stopwatch _stopWatch = null;
        bool _textConversionEnabled = false;
        bool _isRecordingEnglish = false;
        private void StartAudioRecordingTimer()
        {
            try
            {
                _stopWatch = new Stopwatch();
                _stopWatch.Start();

                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += new EventHandler(dt_Tick);
                _dispatcherTimer.Start();
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                _isRecordingEnglish = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (_stopWatch != null && _stopWatch.IsRunning)
            {
                TimeSpan ts = _stopWatch.Elapsed;
                _currentRecordingTime = string.Format("{0:00}:{1:00}:{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                _boardview.timer_voicerecord.Text = _currentRecordingTime;
            }
        }

        private void StartAudioPlayerTimer()
        {
            dispatcherTimerPlayer = new DispatcherTimer();
            dispatcherTimerPlayer.Interval = TimeSpan.FromMilliseconds(200);
            dispatcherTimerPlayer.Tick += dispatcherTimerPlayer_Tick;
        }

        void dispatcherTimerPlayer_Tick(object sender, EventArgs e)
        {
            if (_boardview.audioplayer.Source != null && _boardview.audioplayer.NaturalDuration.HasTimeSpan)
            {
                _boardview.slider_audioplayer.Value = _boardview.audioplayer.Position.TotalSeconds;
                AudioFileTotalTime = " / " + _boardview.audioplayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
                AudioFileCurrentTime = _boardview.audioplayer.Position.ToString(@"hh\:mm\:ss");
            }
        }

        public void canv_voicerecord_stop_MouseDown(object sender, MouseEventArgs e)
        {
            _speechToText.StopAudioRecording();
            StopAudioRecording();
        }

        public void canv_txt_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                isInDrag = false;
                if (!_textConversionEnabled)
                {
                    _textConversionEnabled = true;
                    AudioText = "Please start speaking";
                    _boardview.canv_txt_bubble.Visibility = Visibility.Visible;
                }

                if (_isRecordingEnglish && (sender as Canvas).Name == "canv_txt_btn_telugu")
                {
                    _speechToText.ChangeLanguage(true);
                    _isRecordingEnglish = false;
                }
                else if (!_isRecordingEnglish && (sender as Canvas).Name == "canv_txt_btn_english")
                {
                    _speechToText.ChangeLanguage(false);
                    _isRecordingEnglish = true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_clear_txt_audio_MouseUp(object sender, MouseEventArgs e)
        {
            AudioText = string.Empty;
        }

        public void canv_close_txt_audio_MouseUp(object sender, MouseEventArgs e)
        {
            _textConversionEnabled = false;
            _boardview.canv_txt_bubble.Visibility = Visibility.Collapsed;
        }

        Point anchorPoint;
        Point currentPoint;
        bool isInDrag = false;
        bool isFromRecording = false;
        TranslateTransform transform = new TranslateTransform();

        string _voiceFilePath = string.Empty;
        string _currentRecordingTime = string.Empty;
        string _audioNameDelete = string.Empty;
        public LibraryThumbs _selectedAudioNotesItem = null;

        public void move_audioplayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                FrameworkElement element = sender as FrameworkElement;
                anchorPoint = e.GetPosition(null);
                element.CaptureMouse();
                isInDrag = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void move_audioplayer_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (isInDrag)
                {
                    var element = sender as FrameworkElement;
                    currentPoint = e.GetPosition(null);

                    transform.X += currentPoint.X - anchorPoint.X;
                    transform.Y += (currentPoint.Y - anchorPoint.Y);
                    _boardview.canv_audioparent.RenderTransform = transform;
                    anchorPoint = currentPoint;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void move_audioplayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (isInDrag)
                {
                    FrameworkElement element = sender as FrameworkElement;
                    element.ReleaseMouseCapture();
                    isInDrag = false;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void close_audio_list_player_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _boardview.canv_audioparent.Visibility = Visibility.Collapsed;
                _boardview.audioplayer.Stop();
                _boardview.audioplayer.Source = null;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_show_audiofiles_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _boardview.txt_result_voicenotes.Visibility = Visibility.Collapsed;
                if (_boardview.canv_audiofiles.Visibility == Visibility.Visible)
                {
                    _boardview.img_ellipse_selected.Visibility = Visibility.Collapsed;
                    _boardview.canv_audiofiles.Visibility = Visibility.Collapsed;
                }
                else if (_boardview.canv_audiofiles.Visibility == Visibility.Collapsed)
                {
                    _boardview.img_ellipse_selected.Visibility = Visibility.Visible;
                    _boardview.canv_audiofiles.Visibility = Visibility.Visible;
                }
                Point relativePoint = _boardview.canv_audio_player.TransformToAncestor(_boardview.canv_main)
                                  .Transform(new Point(0, 0));
                if (relativePoint.Y < 511)
                {
                    Canvas.SetTop(_boardview.canv_audiofiles, 530);
                }
                else
                {
                    Canvas.SetTop(_boardview.canv_audiofiles, 10);
                }
                GetAudioList();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_play_audiofile_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (_boardview.audioplayer.Source == null)
                {
                    return;
                }

                if (_boardview.audio_play.Visibility == Visibility.Visible)
                {
                    _boardview.txt_recordingitem_name.Text = System.IO.Path.GetFileNameWithoutExtension(_voiceFilePath);
                    _boardview.audio_play.Visibility = Visibility.Collapsed;
                    _boardview.audio_pause.Visibility = Visibility.Visible;
                    _boardview.txt_result_voicenotes.Visibility = Visibility.Visible;
                    _boardview.canv_audiofiles.Visibility = Visibility.Collapsed;
                    PlayAudioFile();
                }
                else
                {
                    _boardview.txt_result_voicenotes.Visibility = Visibility.Collapsed;
                    _boardview.canv_audiofiles.Visibility = Visibility.Collapsed;
                    _boardview.audio_pause.Visibility = Visibility.Collapsed;
                    _boardview.audio_play.Visibility = Visibility.Visible;
                    _boardview.audioplayer.LoadedBehavior = MediaState.Manual;
                    _boardview.audioplayer.Pause();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_volume_leveler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas senderElement = sender as Canvas;
                _boardview.volume_leveler.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as System.Windows.Shapes.Path).Fill = (Brush)new BrushConverter().ConvertFromString("#FFB7B7B7"));
                Brush fillBrush = new BrushConverter().ConvertFromString("#FF1397EB") as Brush;
                Brush brush = new BrushConverter().ConvertFromString("#FF6C6B6B") as Brush;
                if (senderElement.Name == "canv_vol1")
                {
                    _boardview.volume1.Fill = fillBrush;
                    _boardview.volume2.Fill = brush;
                    _boardview.volume3.Fill = brush;
                    _boardview.volume4.Fill = brush;
                    _boardview.audioplayer.Volume = 0.3;
                }
                else if (senderElement.Name == "canv_vol2")
                {
                    _boardview.volume1.Fill = fillBrush;
                    _boardview.volume2.Fill = fillBrush;
                    _boardview.volume3.Fill = brush;
                    _boardview.volume4.Fill = brush;
                    _boardview.audioplayer.Volume = 0.6;
                }
                else if (senderElement.Name == "canv_vol3")
                {
                    _boardview.volume1.Fill = fillBrush;
                    _boardview.volume2.Fill = fillBrush;
                    _boardview.volume3.Fill = fillBrush;
                    _boardview.volume4.Fill = brush;
                    _boardview.audioplayer.Volume = 0.8;
                }
                else if (senderElement.Name == "canv_vol4")
                {
                    _boardview.volume1.Fill = fillBrush;
                    _boardview.volume2.Fill = fillBrush;
                    _boardview.volume3.Fill = fillBrush;
                    _boardview.volume4.Fill = fillBrush;
                    _boardview.audioplayer.Volume = 1.0;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void slider_audioplayer_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
        }
        public void slider_audioplayer_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_boardview.audioplayer.HasVideo)
            {
                _boardview.audioplayer.Position = TimeSpan.FromSeconds((sender as Slider).Value);
            }
        }
        public void slider_audioplayer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan timespan = TimeSpan.FromSeconds((sender as Slider).Value);
            _boardview.audioplayer.Position = timespan;
            _boardview.slider_audioplayer.Value = _boardview.audioplayer.Position.TotalSeconds;
        }

        public void audioplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _boardview.audioplayer.LoadedBehavior = MediaState.Manual;
            _boardview.audioplayer.Stop();
            _boardview.audioplayer.Position = TimeSpan.FromSeconds(0);
            _boardview.audio_pause.Visibility = Visibility.Collapsed;
            _boardview.audio_play.Visibility = Visibility.Visible;
        }

        public void Audioplayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            _boardview.slider_audioplayer.Minimum = 0;
            _boardview.slider_audioplayer.Maximum = _boardview.audioplayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        public void listbox_audiofiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_boardview.listbox_audiofiles != null && _boardview.listbox_audiofiles.SelectedIndex > -1)
                {
                    _selectedAudioNotesItem = (LibraryThumbs)_boardview.listbox_audiofiles.SelectedItem;
                    _boardview.audioplayer.Source = null;
                    string filePath = _selectedAudioNotesItem.AttachmentLocalPath;
                    if (_selectedAudioNotesItem != null && File.Exists(filePath))
                    {
                        _boardview.audioplayer.Source = new Uri(filePath);
                        AudioFileText = string.IsNullOrWhiteSpace(_selectedAudioNotesItem.TextInfo) ? "" : _selectedAudioNotesItem.TextInfo;

                        if (!isFromRecording)
                        {
                            _boardview.txt_result_voicenotes.Visibility = AudioFileText.Trim().Length > 0 ? Visibility.Visible : Visibility.Collapsed;

                            _boardview.canv_audiofiles.Visibility = Visibility.Collapsed;
                            _boardview.audio_play.Visibility = Visibility.Collapsed;
                            _boardview.audio_pause.Visibility = Visibility.Visible;
                            _boardview.txt_recordingitem_name.Text = _selectedAudioNotesItem.AttachmentName;
                            PlayAudioFile();
                        }
                        else
                            isFromRecording = false;
                    }
                    else
                    {
                        Messenger.Default.Send("ooops, something went wrong. We will get it back working as soon as possible.", "Notification");
                    }
                }
                _boardview.listbox_audiofiles.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_remove_audio_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                _boardview.audioplayer.Stop();
                _audioNameDelete = Convert.ToString((sender as Canvas).Tag);
                KeyValuePair<string, string> notificationClear = new KeyValuePair<string, string>("Delete Audio File", "This  action will delete the selected audio notes completely. Do you want to proceed?");
                Messenger.Default.Send<KeyValuePair<string, string>>(notificationClear, "Result");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void PlayAudioFile()
        {
            try
            {
                _boardview.audioplayer.LoadedBehavior = MediaState.Manual;
                _boardview.audioplayer.Play();

                if (dispatcherTimerPlayer == null)
                    StartAudioPlayerTimer();

                if (dispatcherTimerPlayer != null)
                {
                    dispatcherTimerPlayer.Start();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void GetAudioList()
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentClass).Where(s => (!string.IsNullOrWhiteSpace(s.AttachmentName) || s.AttachmentType == AttachmentType.Decision.ToString()) && s.ClassId == _currentClass.ClassId).ToList();

                    //_mediaAnnotaionsList = Service.GetModuleDataList<LibraryThumbs>(_currentMeeting).Where(s => !string.IsNullOrWhiteSpace(s.AttachmentName)).ToList();
                    if (_mediaAnnotaionsList != null)
                        AudioList = _mediaAnnotaionsList.Where(s => s.AttachmentType == AttachmentType.Audio.ToString()).ToList();
                });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void DeleteSelectedAudioFileFromDb(string param)
        {
            try
            {
                LibraryThumbs item = _mediaAnnotaionsList.FirstOrDefault(s => s.AttachmentName == _audioNameDelete);
                if (item != null)
                {
                    _boardview.audioplayer.Stop();
                    _boardview.audioplayer.Source = null;

                    if (Service.InsertOrUpdateDataToDB(item, CrudActions.Delete, item.LibraryThumbId) >= 1)
                    {
                        _mediaAnnotaionsList.Remove(item);
                        AudioList = _mediaAnnotaionsList.Where(s => s.AttachmentType.ToLower() == "audio").ToList();
                        NxgUtilities.DeleteFile(Constants.AttachmentResources + item.AttachmentUid);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Screen recording

        void CheckingForScreenRecording(string param = "")
        {
            if (_boardview.content_record_control.Visibility != Visibility.Visible)
                StartScreenRecording("");
        }

        void StartScreenRecording(string param)
        {
            _boardview.content_record_control.Content = new ScreenRecordingView(_currentClass);
            _boardview.content_record_control.Visibility = Visibility.Visible;
            Messenger.Default.Send("ScreenRecording", "screen_recording");
        }

        /// <summary>
        /// after close recording, have to highlight recent item
        /// </summary>
        /// <param name="param"></param>
        void CloseScreenRecordingView(string param)
        {
            _boardview.content_record_control.Visibility = Visibility.Collapsed;
            _boardview.canv_library.Visibility = _boardview.canv_library.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            App.ExecuteMethod(GetlibraryThumbnails);
        }

        #endregion Screen recording

        #region Drag & Drop

        /// <summary>
        /// Mouse move event to move position of drag element 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActionUIElement_MouseMove(object sender, MouseEventArgs e)
        {
            DragAndDrop.ActionUIElement_MouseMove(sender, e);
        }

        /// <summary>
        /// Stylus down event to move position of drag element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActionUIElement_StylusDown(object sender, StylusEventArgs e)
        {
            try
            {
                _canvDragItemParent = null;

                Border selectedItem = sender as Border;
                string attachmentType = Convert.ToString(selectedItem.Tag);
                if (!string.IsNullOrWhiteSpace(attachmentType) && attachmentType == AttachmentType.Task.ToString())
                {
                    LibraryThumbs taskitem = (sender as Border).DataContext as LibraryThumbs;
                    if (taskitem != null)
                    {
                        _boardview.tbk_agenda_window_name.Text = "Tasks";

                        _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;
                        _boardview.inkcanv_decision.Strokes.Clear();

                        _boardview.txt_note.Visibility = Visibility.Visible;
                        _boardview.canv_agenda_popup.Visibility = Visibility.Visible;

                        _boardview.sp_task.Visibility = Visibility.Visible;

                        SearchedContactText = "";
                        _boardview.txt_note.Text = taskitem.TextInfo;
                        _boardview.txt_note.Tag = taskitem.LibraryThumbId;
                        _taskMembersList = HomePageViewModel._contactsDbList.Where(s => taskitem.AssignedEmployeePKIDs.Split(',').ToList().Any(k => Convert.ToInt32(k) == s.EmployeeId)).ToList();
                        _boardview.lb_task_members.ItemsSource = null;
                        _boardview.lb_task_members.ItemsSource = _taskMembersList;
                    }
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(attachmentType) && attachmentType == AttachmentType.Note.ToString())
                {
                    LibraryThumbs taskitem = (sender as Border).DataContext as LibraryThumbs;
                    if (taskitem != null)
                    {
                        _boardview.tbk_agenda_window_name.Text = "Notes";

                        _boardview.inkcanv_decision.Visibility = Visibility.Collapsed;
                        _boardview.inkcanv_decision.Strokes.Clear();

                        _boardview.txt_note.Visibility = Visibility.Visible;
                        _boardview.canv_agenda_popup.Visibility = Visibility.Visible;

                        _boardview.sp_task.Visibility = Visibility.Collapsed;

                        _boardview.txt_note.Text = taskitem.TextInfo;
                        _boardview.txt_note.Tag = taskitem.LibraryThumbId;
                    }
                    return;
                }

                if (selectedItem != null && !string.IsNullOrWhiteSpace(attachmentType) && new List<string> { "media_image", "capture" }.Contains(attachmentType.ToLower()) && (!(selectedItem.Child is Grid) || _selectedBoardChildren != (selectedItem.Child as Grid).Children[1] as InkCanvas) && NxgUtilities.IsValidImageExtension(Path.GetExtension((selectedItem.Child as StackPanel).Uid)))
                {
                    _dragItemPosition = e.GetPosition(sender as UIElement);
                    _dragItemPositionFromScreen = e.GetPosition(_boardview.canv_main);
                    DragAndDrop.ActionUIElement_StylusDown(sender, e);
                }
                else
                {
                    if (new List<string> { "media_video", "screen_record", "media_pdf", "excel", "word", "power_point" }.Contains(attachmentType.ToLower()) && e.GetStylusPoints(sender as UIElement).Count == 1 && IsDoubleTap(sender, e) && _selectedDoubleTapItem == sender as Border)
                    {
                        Canvas selectedItemCanvas = (selectedItem.Child as StackPanel).Children[0] as Canvas;
                        if (selectedItemCanvas != null)
                        {
                            string attachment = Convert.ToString(selectedItemCanvas.Tag);

                            List<string> filesList = Directory.GetFiles(Constants.AttachmentResources).ToList();
                            attachment = Path.GetFileName(filesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(attachment))));

                            if (!string.IsNullOrWhiteSpace(attachment))
                                Messenger.Default.Send(attachment, "DesktopMode");
                        }
                        _selectedDoubleTapItem = null;
                        _lastTapLocation = new Point(0, 0);
                        _doubleTapStopWatch.Stop();
                    }
                    else if (_selectedDoubleTapItem == null || _selectedDoubleTapItem != selectedItem)
                    {
                        _selectedDoubleTapItem = selectedItem;
                        _doubleTapStopWatch.Restart();
                    }
                    else
                    {
                        _selectedDoubleTapItem = null;
                        _lastTapLocation = new Point(0, 0);
                        _doubleTapStopWatch.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Stylus move event to move position of drag element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActionUIElement_StylusMove(object sender, StylusEventArgs e)
        {
            Border selectedItem = sender as Border;
            string attachmentType = Convert.ToString(selectedItem.Tag);
            if (selectedItem != null && !string.IsNullOrWhiteSpace(attachmentType) && new List<string> { "media_image", "capture" }.Contains(attachmentType.ToLower()) && (!(selectedItem.Child is Grid) || _selectedBoardChildren != (selectedItem.Child as Grid).Children[1] as InkCanvas) && NxgUtilities.IsValidImageExtension(Path.GetExtension((selectedItem.Child as StackPanel).Uid)))
            {
                DragAndDrop.ActionUIElement_StylusMove(sender, e);
            }
        }

        /// <summary>
        /// add contact into invitee list
        /// </summary>
        /// <param name="dropElement"></param>
        /// <param name="dragElement"></param>
        private void DropElement(object dropElement, object dragElement)
        {
            try
            {
                Border canv = dragElement as Border;
                if (canv != null)
                {
                    LibraryThumbs libraryDragItem = canv.DataContext as LibraryThumbs;
                    if (libraryDragItem != null && File.Exists(libraryDragItem.AttachmentLocalPath) && dropElement is InkCanvas)
                    {
                        MatrixTransform matrix = _boardview.inkCanvas.RenderTransform as MatrixTransform;
                        double leftPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetX < 0 ? (int)Math.Abs(matrix.Matrix.OffsetX) : 0, GetRangeValue(matrix.Matrix.M11, 'x'));
                        double topPosition = NxgUtilities.GetRandomPosition(matrix.Matrix.OffsetY < 0 ? (int)Math.Abs(matrix.Matrix.OffsetY) : 0, GetRangeValue(matrix.Matrix.M11, 'y'));

                        //BitmapImage img = NxgUtilities.GetBitmapImageFromFile(libraryDragItem.AttachmentLocalPath);
                        ImageAnnotations annotations = new ImageAnnotations { Manipulation = XamlWriter.Save(new MatrixTransform(1, 0, 0, 1, leftPosition, topPosition)), LibraryThumbId = libraryDragItem.LibraryThumbId };

                        //ImageAnnotations annotations = new ImageAnnotations { Manipulation = XamlWriter.Save(new MatrixTransform(1, 0, 0, 1, DragAndDrop.DroppedPosition.X - (canv.Width / 2), DragAndDrop.DroppedPosition.Y - (canv.Height / 2))), LibraryThumbId = libraryDragItem.LibraryThumbId };

                        AddChildToBoard(annotations, isFromDrop: true);

                        Border childGrid = _boardview.inkCanvas.Children[_boardview.inkCanvas.Children.Count - 1] as Border;
                        if (childGrid != null)
                            StartScaleAnimation(childGrid);
                    }
                    else
                    {
                        if (_canvDragItemParent != null)
                            VisibleDragItemParent();
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Drag & Drop

        #region Share Online

        public void canv_online_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Service.ConnectToServer())
                {
                    Service._isServerConnected = true;

                    int classId = Service.InsertOrUpdateDataToDB(_currentClass, CrudActions.Create);

                    if (classId > 0)
                    {
                        _boardview.tbk_share_class.Text = classId.ToString();

                        _rethinkColService = new RethinkService(_currentClass.UniqueClassId, _boardview.inkCanvas, _boardview.inkCanvas_Guest);

                        if (_currentClass.AgendaList != null && _currentClass.AgendaList.Count > 0)
                        {
                            foreach (Agendas agenda in _currentClass.AgendaList)
                            {
                                Service.InsertOrUpdateDataToDB(agenda, CrudActions.Create);
                            }
                        }

                        if (_currentClass.ParticipantList != null && _currentClass.ParticipantList.Count > 0)
                        {
                            foreach (Participants participant in _currentClass.ParticipantList)
                            {
                                Service.InsertOrUpdateDataToDB(participant, CrudActions.Create);
                            }
                        }

                        if (_currentClass.BoardAnnotationList != null && _currentClass.BoardAnnotationList.Count > 0)
                        {
                            foreach (BoardAnnotations boardAnnotation in _currentClass.BoardAnnotationList)
                            {
                                Service.InsertOrUpdateDataToDB(boardAnnotation, CrudActions.Create);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Share Online

        #region Extra features

        private bool _isGestureEnabled = true;
        private bool _isZoomEnabled = true;

        /// <summary>
        /// Click On close button then shows the settings pop-up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="touchEventArgs"></param>
        public void Canv_Zoom_Switch_On_Off_PointerPressed(object sender, MouseButtonEventArgs touchEventArgs)
        {
            if (!_boardview.zoom_switch_on.IsVisible)
            {
                _isZoomEnabled = true;
                _boardview.vb_board_zoom_display.Visibility = Visibility.Collapsed;
            }
            else
            {
                _isZoomEnabled = false;

                _boardview.inkCanvas.RenderTransform = new MatrixTransform(1, 0, 0, 1, -3840, -2160);
                _boardview.border_zoom.RenderTransform = new MatrixTransform(1, 0, 0, 1, 3840, 2160);
                if (_inkToolName == InkToolName.Pan)
                {
                    _boardview.vb_board_zoom_display.Visibility = Visibility.Visible;
                    _boardview.inkcanv_zoom.Strokes = _boardview.inkCanvas.Strokes;
                }
            }
            _boardview.zoom_switch_on.Visibility = _boardview.zoom_switch_on.IsVisible ? Visibility.Collapsed : Visibility.Visible;
            _boardview.zoom_switch_off.Visibility = _boardview.zoom_switch_off.IsVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Click On close button then shows the settings pop-up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="touchEventArgs"></param>
        public void Canv_Gestures_Switch_On_Off_PointerPressed(object sender, MouseButtonEventArgs touchEventArgs)
        {
            if (!_boardview.gestures_switch_off.IsVisible)
            {
                _isGestureEnabled = false;
            }
            else
            {
                _isGestureEnabled = true;
            }
            _boardview.gestures_switch_on.Visibility = _boardview.gestures_switch_on.IsVisible ? Visibility.Collapsed : Visibility.Visible;
            _boardview.gestures_switch_off.Visibility = _boardview.gestures_switch_off.IsVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion Extra features

        #region

        private bool _isPasswordRequired = false;

        public void password_req_yes_no_MouseDown(object sender, MouseButtonEventArgs touchEventArgs)
        {
            try
            {
                if (!_boardview.password_req_no.IsVisible)
                {
                    _isPasswordRequired = false;
                }
                else
                {
                    _isPasswordRequired = true;
                }
                _boardview.password_req_yes.Visibility = _boardview.password_req_yes.IsVisible ? Visibility.Collapsed : Visibility.Visible;
                _boardview.password_req_no.Visibility = _boardview.password_req_no.IsVisible ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }
        #endregion
    }
}
