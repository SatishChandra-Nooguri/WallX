using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WallX.Services;
using System.Windows.Shapes;
using NextGen.Controls;
using WallX.Views;
using WallX.Helpers;
using NextGen.Controls.Animations;

namespace WallX.ViewModel
{
    public class CarouselViewModel : ViewModelBase
    {
        #region Variables

        private CarouselView _carouselView;
        private List<Class> _classList;
        private Stopwatch _caroselStopWatchClock;
        private Point _agendaItemTouchDownPoint, _agendaItemTouchUpPoint = new Point(0, 0);
        private int _selectedYear, _nineItemsScrollIndex, _caroselPointX, _caroselPointY, _caroselItemIndex, _currentTimeItemIndex;
        private double _caroselTouchDownPointX, _caroselTouchDownPointY;
        private bool _isDateChanged = false;
        private string _previousSelectedDate = string.Empty;

        public static List<Class> _classOverviewList;
        public static int _selectedClassId = -1;
        public static string _selectedDate = string.Empty;

        public Canvas rescheduleOrDeleteItem = null;
        public List<Employees> _contactsDbList;

        #region Carousel item variables

        public int _caroselFPS = 24;                            // fps of the on enter frame event

        private double _caroselTargetItemPosition;		        // Target moving position
        private double _caroselCurrentItemPositon;	            // Current position
        private double _caroselSpringPosition;		            // Temp used to store last moving 

        private const double _caroselItemWidth = 340;           // Image Width
        private const double _caroselItemHeight = 290;          // Image Height        
        private const double _caroselSpriness = 0.1;		    // Control the Spring Speed
        private const double _caroselDecay = 0.5;			    // Control the bounce Speed
        private const double _caroselScaleDownFactor = 0.2;     // Scale between images
        private const double _caroselOffcetFactor = 200;        // Distance between images
        private const double _caroselOpacityFactor = 0.3;       // Alpha between images  0.0
        private const double _caroselMaxScale = 2;              // Maximum Scale
        private const double _caroselCenterX = 960;             // Center of Carousel width get from carousel width / 2 
        private const double _caroselCenterY = 350;             // Center of Carousel height get from carousel height / 2

        #endregion

        #endregion

        #region PageLoad

        /// <summary>
        /// View loading event for handling default values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CarouselView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _carouselView = sender as CarouselView;

                Messenger.Default.Register<DateTime>(this, "CarouselTimer", timer_Tick);
                Messenger.Default.Register<string>(this, "DeletedClass", DeletedClass);
                Messenger.Default.Register<string>(this, "ResheduleMeeing", ResheduleClass);

                MonthNames = DateTimeFormatInfo.CurrentInfo.MonthNames.ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Select(k => DateTime.ParseExact(k, "MMMM", CultureInfo.InvariantCulture)).ToList();
                ListYears = Enumerable.Range(DateTime.Now.Year - 30, 60).ToList();

                LoadClassAsCarousel();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To load class based on selected date
        /// </summary>
        /// <param name="selectedDate"></param>
        private void LoadClassAsCarousel()
        {
            try
            {
                DateTime currentDateTime = Convert.ToDateTime(_selectedDate);
                _selectedYear = currentDateTime.Year;
                SelectedMonth = currentDateTime.Month - 1;

                LoadSelectedMonthDates();

                App.ExecuteMethod(CreateCarouselItem);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Binding dates to listbox based on selected month
        /// </summary>
        /// <param name="monthNo"></param>
        private void LoadSelectedMonthDates()
        {
            try
            {
                _isDateChanged = true;
                DateTime dateTime = DateTime.ParseExact((SelectedMonth + 1).ToString("00"), "MM", CultureInfo.InvariantCulture);
                SelectedYearIndex = Convert.ToInt32(ListYears.IndexOf(_selectedYear));
                CalendarMonthName = dateTime.ToString("MMMM").ToUpper() + " " + _selectedYear;
                List<DateTime> selectedMonthDates = Enumerable.Range(1, DateTime.DaysInMonth(_selectedYear, dateTime.Month)).Select(day => new DateTime(_selectedYear, dateTime.Month, day)).ToList();
                ListDates = selectedMonthDates;

                int previousDate = -1;
                if (!string.IsNullOrWhiteSpace(_previousSelectedDate))
                    previousDate = int.Parse(_previousSelectedDate.Substring(8, 2));

                int date = previousDate != -1 ? previousDate : int.Parse(_selectedDate.Split('-')[2]);
                SelectedDateIndex = date - 1;
                _nineItemsScrollIndex = date - 1;

                _carouselView.listbox_dates.ScrollIntoView(_carouselView.listbox_dates.Items[SelectedDateIndex]);
                _carouselView.canv_Next_Date.Visibility = ((date - 1) == selectedMonthDates.Count - 1) ? Visibility.Hidden : Visibility.Visible;
                _carouselView.canv_Prev_Date.Visibility = (date - 1) > 3 ? Visibility.Visible : Visibility.Hidden;
                _isDateChanged = false;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Properties

        private List<int> _listYears;
        public List<int> ListYears
        {
            get { return this._listYears; }
            set
            {
                this._listYears = value;
                RaisePropertyChanged("ListYears");
            }
        }

        private List<DateTime> _monthNames;
        public List<DateTime> MonthNames
        {
            get { return this._monthNames; }
            set
            {
                this._monthNames = value;
                RaisePropertyChanged("MonthNames");
            }
        }

        private List<DateTime> _listDates;
        public List<DateTime> ListDates
        {
            get { return this._listDates; }
            set
            {
                this._listDates = value;
                RaisePropertyChanged("ListDates");
            }
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get { return this._selectedMonth; }
            set
            {
                this._selectedMonth = value;
                RaisePropertyChanged("SelectedMonth");
            }
        }

        private int _selectedYearIndex;
        public int SelectedYearIndex
        {
            get { return this._selectedYearIndex; }
            set
            {
                this._selectedYearIndex = value;
                RaisePropertyChanged("SelectedYearIndex");
            }
        }

        private int _selectedDateIndex;
        public int SelectedDateIndex
        {
            get { return this._selectedDateIndex; }
            set
            {
                this._selectedDateIndex = value;
                RaisePropertyChanged("SelectedDateIndex");
            }
        }

        private string _calendarMonthName;
        public string CalendarMonthName
        {
            get { return this._calendarMonthName; }
            set
            {
                this._calendarMonthName = value;
                RaisePropertyChanged("CalendarMonthName");
            }
        }

        private Class _classInfo;
        public Class ClassInfo
        {
            get { return this._classInfo; }
            set
            {
                this._classInfo = value;
                RaisePropertyChanged("ClassInfo");
            }
        }

        private Employees _selectedParticipant;
        public Employees SelectedParticipant
        {
            get { return this._selectedParticipant; }
            set
            {
                this._selectedParticipant = value;
                RaisePropertyChanged("SelectedParticipant");
            }
        }

        #endregion

        #region Timer for updating carousel position

        /// <summary>
        /// timer for updating the carousel position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(DateTime dateTime)
        {
            try
            {
                List<StackPanel> carouselItems = _carouselView.canv_carosel.Children.OfType<StackPanel>().ToList();
                for (int i = 0; i < carouselItems.Count(); i++)
                {
                    SetPositionToCaroselItem(carouselItems[i], i);
                }

                // compute the current position
                // added spring effect
                if (_caroselTargetItemPosition == carouselItems.Count())
                    _caroselTargetItemPosition = 0;
                _caroselSpringPosition = (_caroselTargetItemPosition - _caroselCurrentItemPositon) * _caroselSpriness + _caroselSpringPosition * _caroselDecay;
                _caroselCurrentItemPositon += _caroselSpringPosition;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Calendar

        /// <summary>
        /// Event for years selection changed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listbox_YearSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedIndex != -1 && !_isDateChanged)
                {
                    _selectedYear = Convert.ToInt32(listBox.SelectedItem);
                    _carouselView.txt_Calender_Month.Text = Convert.ToDateTime(_carouselView.listbox_months.SelectedItem).ToString("MMMM").ToUpper() + " " + _selectedYear;

                    _selectedDate = (SelectedMonth + 1) != DateTime.Now.Month || _selectedYear != DateTime.Now.Year ? _selectedYear + "-" + (SelectedMonth + 1).ToString("00") + "-" + "01" : DateTime.Now.ToString("yyyy-MM-dd");

                    LoadSelectedMonthDates();

                    App.ExecuteMethod(CreateCarouselItem);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// month selection changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listbox_MonthSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedIndex != -1 && !_isDateChanged)
                {
                    DateTime selectedMonth = Convert.ToDateTime(listBox.SelectedItem);
                    string month_selected = selectedMonth.ToString("MMMM").ToUpper();
                    _carouselView.txt_Calender_Month.Text = month_selected + " " + _selectedYear;
                    int month = int.Parse(selectedMonth.ToString("MM"));
                    SelectedMonth = month - 1;

                    _selectedDate = month != DateTime.Now.Month || _selectedYear != DateTime.Now.Year ? _selectedYear + "-" + month.ToString("00") + "-" + "01" : DateTime.Now.ToString("yyyy-MM-dd");

                    _isDateChanged = true;

                    LoadSelectedMonthDates();

                    App.ExecuteMethod(CreateCarouselItem);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// for month and year canvas visibility
        /// </summary>
        public void MonthSelection_MouseDown()
        {
            try
            {
                _carouselView.canv_MonthSelection.Visibility = Visibility.Visible;
                Animation.Scale(_carouselView.canv_MonthSelection, 0, 1, 0.5, 0, sbCalenderVisible_Completed, new CubicEase { EasingMode = EasingMode.EaseIn });
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// month & years display animation completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sbCalenderVisible_Completed()
        {
            try
            {
                _carouselView.canv_MonthSelection.Visibility = Visibility.Visible;
                _carouselView.listbox_years.ScrollIntoView(_carouselView.listbox_years.Items[_carouselView.listbox_years.SelectedIndex]);
                _carouselView.listbox_months.ScrollIntoView(_carouselView.listbox_months.Items[_carouselView.listbox_months.SelectedIndex]);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// date selection changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectedDate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _carouselView.canv_Next_Date.Visibility = _carouselView.listbox_dates.SelectedIndex == _carouselView.listbox_dates.Items.Count - 1 ? Visibility.Hidden : Visibility.Visible;
                _carouselView.canv_Prev_Date.Visibility = _carouselView.listbox_dates.SelectedIndex > 3 ? Visibility.Visible : Visibility.Hidden;

                DateTime selectedDate = Convert.ToDateTime((sender as Canvas).Tag);
                _previousSelectedDate = _selectedDate;
                _selectedDate = selectedDate.ToString("yyyy-MM-dd");

                App.ExecuteMethod(CreateCarouselItem);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Hide calendar data when touch on carousel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_carosel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_carouselView.canv_MonthSelection.Visibility == Visibility.Visible)
                {
                    Animation.Scale(_carouselView.canv_MonthSelection, 1, 0, 0.5, 0, () => _carouselView.canv_MonthSelection.Visibility = Visibility.Collapsed, new CubicEase { EasingMode = EasingMode.EaseIn });
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Event For Previous Date
        /// </summary>
        public void canv_Prev_Date_MouseUp()
        {
            NineItemsScrollPrevious(_carouselView.listbox_dates, _carouselView.canv_Prev_Date, _carouselView.canv_Next_Date);
        }

        /// <summary>
        /// Event For Next Date
        /// </summary>
        public void canv_Next_Date_MouseUp()
        {
            NineItemsScrollNext(_carouselView.listbox_dates, _carouselView.canv_Prev_Date, _carouselView.canv_Next_Date);
        }

        #region Scroll 9 items

        /// <summary>
        /// Method For Scroll 9 items Next 
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="prevButton"></param>
        /// <param name="nextButton"></param>
        private void NineItemsScrollNext(ListBox listBox, Canvas prevButton, Canvas nextButton)
        {
            try
            {
                _nineItemsScrollIndex += (_nineItemsScrollIndex == -1 ? 18 : (_nineItemsScrollIndex % 9 != 0 ? 9 : 17));

                prevButton.Visibility = Visibility.Visible;
                nextButton.Visibility = Visibility.Visible;

                if (_nineItemsScrollIndex != -1)
                {
                    if (_nineItemsScrollIndex >= listBox.Items.Count - 1)
                    {
                        _nineItemsScrollIndex = listBox.Items.Count - 1;
                        nextButton.Visibility = Visibility.Hidden;
                    }
                    listBox.ScrollIntoView(listBox.Items[_nineItemsScrollIndex]);
                }

                if (_nineItemsScrollIndex <= 8)
                    prevButton.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Method For Scroll 9 items Previous 
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="prevButton"></param>
        /// <param name="nextButton"></param>
        private void NineItemsScrollPrevious(ListBox listBox, Canvas prevButton, Canvas nextButton)
        {
            try
            {
                _nineItemsScrollIndex -= (_nineItemsScrollIndex == listBox.Items.Count - 1 ? (((listBox.Items.Count - 1) % 9) + 9) : ((_nineItemsScrollIndex + 1) % 9 == 0 ? 17 : 9));

                if (_nineItemsScrollIndex < -1)
                {
                    _nineItemsScrollIndex = 0;
                }

                listBox.ScrollIntoView(listBox.Items[_nineItemsScrollIndex]);

                nextButton.Visibility = Visibility.Visible;
                prevButton.Visibility = Visibility.Visible;
                if (_nineItemsScrollIndex == 0)
                {
                    prevButton.Visibility = Visibility.Hidden;
                    _nineItemsScrollIndex = -1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Scroll 9 items

        #endregion

        #region Carousel

        /// <summary>
        /// create carousel items
        /// </summary>
        /// <param name="lstNewClassInfo"></param>
        private void CreateCarouselItem()
        {
            try
            {
                DateTime currentDate = DateTime.Now;
                DateTime selectedDate = Convert.ToDateTime(_selectedDate + " 23:59:00");

                _caroselFPS = 24;
                _caroselTargetItemPosition = 0;

                _classList = Service.GetClassList(DateTime.Parse(_selectedDate), true);

                Application.Current.Dispatcher.InvokeAsync((Action)(() =>
                {
                    _carouselView.canv_noclass_future.Visibility = _carouselView.canv_noclass_past.Visibility = Visibility.Hidden;
                    _carouselView.canv_carosel.Children.Clear();

                    if (_classList == null || _classList.Count == 0)
                    {
                        _carouselView.canv_noclass_future.Visibility = selectedDate >= currentDate ? Visibility.Visible : Visibility.Hidden;
                        _carouselView.canv_noclass_past.Visibility = selectedDate <= currentDate ? Visibility.Visible : Visibility.Hidden;
                    }
                    else if (_classList != null && _classList.Count > 0)
                    {
                        HomePageViewModel._timer.Stop();
                        HomePageViewModel._timer.Interval = new TimeSpan(0, 0, 1 / _caroselFPS);
                        HomePageViewModel._timer.Start();

                        GetParticipantsFromUsers();



                        for (int i = 0; i < _classList.Count; i++)
                        {
                            _classList[i].PreviousClassEndTime = i == 0 ? DateTime.Parse(Constants.DayStartTime) : _classList[i - 1].EndTime;
                            _classList[i].NextClassStartTime = i == _classList.Count - 1 ? DateTime.Parse(Constants.DayEndTime) : _classList[i + 1].StartTime;

                            StackPanel stack = _carouselView.FindResource("stackpanel_Carosel") as StackPanel;
                            stack.DataContext = _classList[i];
                            stack.Tag = i;
                            _carouselView.canv_carosel.Children.Add(stack);
                            SetPositionToCaroselItem(stack, i);
                        }

                        int currentindex = _selectedDate != currentDate.ToString("yyyy-MM-dd") ? 0 : _classList.IndexOf(_classList.FirstOrDefault(s => (_selectedClassId == -1 ? s.StartTime > DateTime.Now : s.ClassId == _selectedClassId)));

                        currentindex = currentindex == -1 ? 0 : currentindex;
                        _caroselPointX = _caroselPointY = _currentTimeItemIndex = currentindex;

                        foreach (StackPanel panelItem in _carouselView.canv_carosel.Children.OfType<StackPanel>())
                        {
                            HandlingElementsCaroselItems(panelItem, _caroselPointX);
                        }
                        CaroselItemMoveIndex(currentindex);

                        HomePageViewModel._currentClassIndex = currentindex;
                    }
                }));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Get participants from users data
        /// </summary>
        private void GetParticipantsFromUsers()
        {
            try
            {
                _contactsDbList = Service.GetModuleDataList<Employees>(null);
                foreach (Class _currentClass in _classList)
                {
                    if (_currentClass.ParticipantList != null && _currentClass.ParticipantList.Count > 0)
                    {
                        foreach (Participants item in _currentClass.ParticipantList)
                        {
                            Employees participant = _contactsDbList.FirstOrDefault(k => k.EmployeeId == item.EmployeeId);
                            if (participant != null)
                            {
                                item.Employee = participant;
                            }
                        }

                        foreach (Agendas agendaItem in _currentClass.AgendaList)
                        {
                            Employees participant = _contactsDbList.FirstOrDefault(k => k.EmployeeId == agendaItem.EmployeeId);
                            if (participant != null)
                            {
                                agendaItem.EmployeeName = participant.Name;
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
        /// position alignment for carousel items
        /// </summary>
        /// <param name="image"></param>
        /// <param name="index"></param>
        private void SetPositionToCaroselItem(StackPanel image, int index)
        {
            try
            {
                double diffFactor = index - _caroselCurrentItemPositon;
                double diffFactor_absoute = Math.Round(diffFactor, 0);

                // scale and position the image according to their index and current position
                // the one who closer to the _current has the larger scale
                ScaleTransform scaleTransform = new ScaleTransform();
                scaleTransform.ScaleX = _caroselMaxScale - Math.Abs(diffFactor_absoute) * _caroselScaleDownFactor;
                scaleTransform.ScaleY = _caroselMaxScale - Math.Abs(diffFactor_absoute) * _caroselScaleDownFactor;
                image.RenderTransform = scaleTransform;

                // reposition the image
                double left = _caroselCenterX - (_caroselItemWidth * scaleTransform.ScaleX) / 2 + diffFactor * _caroselOffcetFactor;
                double top = _caroselCenterY - (_caroselItemHeight * scaleTransform.ScaleY) / 2;
                image.Opacity = 1 - Math.Abs(diffFactor) * _caroselOpacityFactor;

                image.SetValue(Canvas.LeftProperty, left);
                image.SetValue(Canvas.TopProperty, top);

                // order the element by the scaleX
                image.SetValue(Canvas.ZIndexProperty, (int)Math.Abs(scaleTransform.ScaleX * 100));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll agendas to selected agenda item
        /// </summary>
        /// <param name="agendaListBox"></param>
        /// <param name="selectedIndex"></param>
        private void ScrollAgendaToSelectedItem(ListBox agendaListBox, int selectedIndex)
        {
            try
            {
                if (agendaListBox != null && agendaListBox.Items.Count > 0)
                {
                    ListBox listview = ((agendaListBox.Parent is StackPanel ? (agendaListBox.Parent as StackPanel).Parent : ((agendaListBox.Parent as Canvas).Parent as Border).Child) as Canvas).Children[2] as ListBox;
                    agendaListBox.SelectedIndex = listview.SelectedIndex = selectedIndex;
                    agendaListBox.ScrollIntoView(agendaListBox.Items[selectedIndex]);
                    listview.ScrollIntoView(listview.Items[selectedIndex]);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// handling children elements visibility in carousel item
        /// </summary>
        /// <param name="carouselItem"></param>
        /// <param name="index"></param>
        private void HandlingElementsCaroselItems(StackPanel carouselItem, int index)
        {
            try
            {
                int stack_id = int.Parse(Convert.ToString((carouselItem as StackPanel).Tag));
                int _currentClassId = _classList[stack_id].ClassId;

                Canvas canv_Preview = TemplateModifier.FindVisualChild<Canvas>(carouselItem, "canv_Preview");
                Canvas canv_Play = TemplateModifier.FindVisualChild<Canvas>(carouselItem, "canv_Play");
                Canvas canv_Reshedule_Event = TemplateModifier.FindVisualChild<Canvas>(carouselItem, "canv_Reshedule_Event");
                Canvas canv_Delete_Event = TemplateModifier.FindVisualChild<Canvas>(carouselItem, "canv_Delete_Event");
                TextBlock icon_PLUS_Carosel_right = TemplateModifier.FindVisualChild<TextBlock>(carouselItem, "icon_PLUS_Carosel_right");
                TextBlock icon_PLUS_Carosel_left = TemplateModifier.FindVisualChild<TextBlock>(carouselItem, "icon_PLUS_Carosel_left");
                ListBox listboxAgenda = TemplateModifier.FindVisualChild<ListBox>(carouselItem, "listboxAgenda");
                ListBox listboxInvitees = TemplateModifier.FindVisualChild<ListBox>(carouselItem, "listboxInvitees");
                Canvas canvEmp = TemplateModifier.FindVisualChild<Canvas>(carouselItem, "cavn_emp_details");

                canv_Preview.Visibility = Visibility.Hidden;
                canv_Play.Visibility = Visibility.Hidden;
                canv_Reshedule_Event.Visibility = Visibility.Hidden;
                canv_Delete_Event.Visibility = Visibility.Hidden;

                if (stack_id == index)
                {
                    icon_PLUS_Carosel_right.Visibility = Visibility.Hidden;
                    icon_PLUS_Carosel_left.Visibility = Visibility.Hidden;

                    if ((Convert.ToDateTime(_selectedDate) > DateTime.Now) || ((carouselItem.DataContext as Class).ClassId == _currentClassId && _selectedDate == DateTime.Now.Date.ToString("yyyy-MM-dd") && DateTime.Now.Subtract(_classList[stack_id].EndTime).TotalMinutes <= 0))
                    {
                        canv_Preview.Visibility = Visibility.Hidden;
                        canv_Play.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        canv_Preview.Visibility = Visibility.Visible;
                        canv_Play.Visibility = Visibility.Hidden;
                    }
                    canv_Reshedule_Event.Visibility = Visibility.Visible;
                    canv_Delete_Event.Visibility = Visibility.Visible;
                }
                else if (stack_id < index)
                {
                    icon_PLUS_Carosel_right.Visibility = Visibility.Hidden;
                    canvEmp.Visibility = Visibility.Collapsed;

                    if (icon_PLUS_Carosel_left.Tag != null && Convert.ToString(icon_PLUS_Carosel_left.Tag) == "Visible")
                    {
                        icon_PLUS_Carosel_left.Visibility = Visibility.Visible;
                    }
                }
                else if (stack_id > index)
                {
                    icon_PLUS_Carosel_left.Visibility = Visibility.Hidden;
                    canvEmp.Visibility = Visibility.Collapsed;

                    if (Convert.ToString(icon_PLUS_Carosel_right.Tag) == "Visible")
                    {
                        icon_PLUS_Carosel_right.Visibility = Visibility.Visible;
                    }
                }

                listboxAgenda.IsHitTestVisible = listboxInvitees.IsHitTestVisible = stack_id == index;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Set carousel current move index
        /// </summary>
        /// <param name="value"></param>
        private void CaroselItemMoveIndex(int value)
        {
            try
            {
                _caroselTargetItemPosition += value;
                _caroselTargetItemPosition = Math.Max(0, _caroselTargetItemPosition);
                _caroselTargetItemPosition = Math.Min(_carouselView.canv_carosel.Children.OfType<StackPanel>().Count() - 1, _caroselTargetItemPosition);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// carousel item mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CaroselItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Point mousedownPos = e.GetPosition(_carouselView.canv_carosel);
                _caroselTouchDownPointX = mousedownPos.X;
                _caroselTouchDownPointY = mousedownPos.Y;
                _caroselItemIndex = (int)((sender as StackPanel).Tag);
                _caroselStopWatchClock = new Stopwatch();
                _caroselStopWatchClock.Start();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// carousel item mouse up event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CaroselItemMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_caroselStopWatchClock != null)
                {
                    _caroselStopWatchClock.Stop();
                    TimeSpan time = _caroselStopWatchClock.Elapsed;
                    double totalMilliSeconds = time.Seconds * 1000 + time.Milliseconds;
                    Point mouseupPos = e.GetPosition(_carouselView.canv_carosel);

                    double caroselTouchUpPointX = mouseupPos.X;
                    double caroselTouchUpPointY = mouseupPos.Y;
                    double distance = _caroselTouchDownPointX - caroselTouchUpPointX;
                    double velocity = distance / totalMilliSeconds;

                    _currentTimeItemIndex = _caroselPointY = _caroselItemIndex;

                    if ((velocity < 1 && velocity > 0) || (velocity < 0 && velocity > -1))
                    {
                        _caroselFPS = 5;
                    }
                    else if ((velocity < 2 && velocity > 1) || (velocity < -1 && velocity > -2))
                    {
                        _caroselFPS = 15;
                    }
                    else if ((velocity < 3 && velocity > 2) || (velocity < -2 && velocity > -3))
                    {
                        _caroselFPS = 25;
                    }
                    else if ((velocity < 4 && velocity > 3) || (velocity < -3 && velocity > -4))
                    {
                        _caroselFPS = 50;
                    }
                    else if ((velocity < 5 && velocity > 4) || (velocity < -4 && velocity > -5))
                    {
                        _caroselFPS = 85;
                    }
                    else if ((velocity < 6 && velocity > 5) || (velocity < -5 && velocity > -6))
                    {
                        _caroselFPS = 150;
                    }
                    else if ((velocity < 7 && velocity > 6) || (velocity < -6 && velocity > -7))
                    {
                        _caroselFPS = 225;
                    }
                    else if ((velocity < 8 && velocity > 7) || (velocity < -7 && velocity > -8))
                    {
                        _caroselFPS = 365;
                    }
                    else if ((velocity < 9 && velocity > 8) || (velocity < -8 && velocity > -9))
                    {
                        _caroselFPS = 500;
                    }
                    else
                    {
                        _caroselFPS = 800;
                    }

                    HomePageViewModel._timer.Stop();
                    HomePageViewModel._timer.Interval = new TimeSpan(0, 0, 1 / _caroselFPS);
                    HomePageViewModel._timer.Start();

                    if (_caroselPointY > _caroselPointX)
                    {
                        if ((caroselTouchUpPointX < _caroselTouchDownPointX) && ((_caroselTouchDownPointX - caroselTouchUpPointX) > 5))
                        {
                            CaroselItemMoveIndex(_caroselPointY - _caroselPointX);
                            _caroselPointX = _caroselPointY;
                        }
                        else if (_caroselTouchDownPointX < caroselTouchUpPointX)
                        {
                        }
                        else
                        {
                            CaroselItemMoveIndex(_caroselPointY - _caroselPointX);
                            _caroselPointX = _caroselPointY;
                        }
                    }
                    else if (_caroselPointY < _caroselPointX)
                    {
                        if ((_caroselTouchDownPointX < caroselTouchUpPointX) && ((caroselTouchUpPointX - _caroselTouchDownPointX) > 5))
                        {
                            CaroselItemMoveIndex(_caroselPointY - _caroselPointX);
                            _caroselPointX = _caroselPointY;
                        }
                        else if (caroselTouchUpPointX < _caroselTouchDownPointX)
                        {
                        }
                        else
                        {
                            CaroselItemMoveIndex(_caroselPointY - _caroselPointX);
                            _caroselPointX = _caroselPointY;
                        }
                    }

                    foreach (StackPanel stack in _carouselView.canv_carosel.Children.OfType<StackPanel>())
                    {
                        HandlingElementsCaroselItems(stack, _caroselPointX);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// carousel item touch move event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CaroselItemTouchMove(object sender, TouchEventArgs e)
        {
            try
            {
                Point point = e.GetTouchPoint(_carouselView.canv_carosel).Position;
                if (((point.Y - _caroselTouchDownPointY) > 60 || (_caroselTouchDownPointY - point.Y) > 60) && ((point.X - _caroselTouchDownPointX) < 10 || (_caroselTouchDownPointX - point.X) < 10) && (_caroselItemIndex == _caroselPointY))
                {
                    e.Handled = true;
                    Canvas img = (((sender as StackPanel).Children[0] as Border).Child as Canvas);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Agenda

        /// <summary>
        /// Touch down for scrolling of Agenda listbox items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listBox_TouchDown(object sender, TouchEventArgs e)
        {
            _agendaItemTouchDownPoint = e.GetTouchPoint((sender as ListBox)).Position;
        }

        /// <summary>
        /// Touch up for scrolling of Agenda listbox items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listBox_TouchUp(object sender, TouchEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                _agendaItemTouchUpPoint = e.GetTouchPoint(listBox).Position;
                if (_agendaItemTouchDownPoint.X != 0 && _agendaItemTouchDownPoint.Y != 0 && _agendaItemTouchUpPoint.X != 0 && _agendaItemTouchUpPoint.Y != 0)
                {
                    if (_agendaItemTouchUpPoint.X > _agendaItemTouchDownPoint.X && listBox.SelectedIndex > 0)
                    {
                        listBox.SelectedIndex--;
                    }
                    else if (_agendaItemTouchUpPoint.X < _agendaItemTouchDownPoint.X && listBox.SelectedIndex < listBox.Items.Count - 1)
                    {
                        listBox.SelectedIndex++;
                    }

                    ListBox listview = (((sender as ListBox).Parent as Canvas).Children[3] as StackPanel).Children[0] as ListBox;
                    listview.SelectedIndex = listBox.SelectedIndex;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Agenda list box items scroll with dot marks selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Agenda_ellipse_MouseUp(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedIndex != -1)
                {
                    ScrollAgendaToSelectedItem(listBox, listBox.SelectedIndex);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Participant

        /// <summary>
        /// Display employee full details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void employee_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;

                Participants empDetails = _classList[_currentTimeItemIndex].ParticipantList.FirstOrDefault(s => s.Employee.Email == Convert.ToString((sender as Ellipse).Tag));

                if (empDetails != null)
                {
                    List<StackPanel> carouselItems = _carouselView.canv_carosel.Children.OfType<StackPanel>().ToList();
                    carouselItems.ForEach(s => (((s.Children[0] as Border).Child as Canvas).Children[17] as Canvas).Visibility = Visibility.Collapsed);

                    Canvas canvItem = (((carouselItems[_currentTimeItemIndex].Children[0] as Border).Child as Canvas).Children[17] as Canvas);

                    canvItem.DataContext = empDetails;
                    canvItem.Visibility = Visibility.Visible;

                    Canvas.SetLeft(canvItem, e.GetPosition(((carouselItems[_currentTimeItemIndex].Children[0] as Border).Child as Canvas)).X + 5);
                    Canvas.SetTop(canvItem, 152);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To close employee details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_emp_details_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((sender as Canvas).Parent as Canvas).Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Events

        /// <summary>
        /// Open selected class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_Play_Event_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                HomePageViewModel._classList = _classList;
                HomePageViewModel._currentClassIndex = int.Parse(Convert.ToString(((((sender as Canvas).Parent as Canvas).Parent as Border).Parent as StackPanel).Tag));
                HomePageViewModel._navigateView = "ClassOpenEventInCalender";
                Messenger.Default.Send("Reshedule", "StartClass");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Reschedule selected class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_Reshedule_Event_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                HomePageViewModel._classList = _classList.ToList();
                HomePageViewModel._currentClassIndex = int.Parse(Convert.ToString(((((sender as Canvas).Parent as Canvas).Parent as Border).Parent as StackPanel).Tag));

                NewClassViewModel._isFromCarousel = true;

                HomePageViewModel._navigateView = "ClassResheduleEventInCalender";
                Messenger.Default.Send("Reshedule", "StartClass");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// reschedule class method
        /// </summary>
        /// <param name="param"></param>
        private void ResheduleClass(string param)
        {
            try
            {
                if (rescheduleOrDeleteItem != null)
                {
                    int classID = Convert.ToInt32(((((rescheduleOrDeleteItem as Canvas).Parent as Canvas).Parent as Border).Parent as StackPanel).Uid);

                    Class selectedItem = _classList.FirstOrDefault(s => s.ClassId == classID);

                    rescheduleOrDeleteItem = null;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Deleting selected Class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_Delete_Event_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                rescheduleOrDeleteItem = (sender as Canvas);
                HomePageViewModel._classList = _classList.ToList();
                HomePageViewModel._currentClassIndex = int.Parse(Convert.ToString(((((sender as Canvas).Parent as Canvas).Parent as Border).Parent as StackPanel).Tag));
                HomePageViewModel._navigateView = "ClassDeleteEventInCalender";
                Messenger.Default.Send("Delete", "StartClass");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// delete selected class method
        /// </summary>
        /// <param name="param"></param>
        private void DeletedClass(string param)
        {
            try
            {
                if (rescheduleOrDeleteItem != null)
                {
                    int classID = Convert.ToInt32(((rescheduleOrDeleteItem.Parent as Canvas).Parent as Border).Uid);
                    int index_SelectedItem = int.Parse(Convert.ToString((((rescheduleOrDeleteItem.Parent as Canvas).Parent as Border).Parent as StackPanel).Tag));
                    if (index_SelectedItem == _classList.Count)
                    {
                        _currentTimeItemIndex--;
                    }

                    if (Service.InsertOrUpdateDataToDB(_classList.FirstOrDefault(s => s.ClassId == classID), CrudActions.Delete, _classList.FirstOrDefault(s => s.ClassId == classID).ClassId) > 0)
                    {
                        Messenger.Default.Send("Class removed", "Notification");
                        rescheduleOrDeleteItem = null;
                        App.ExecuteMethod(CreateCarouselItem);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// create new class from carousel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_PLUS_Carosel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Class meet = _classList.FirstOrDefault(s => s.ClassId == Convert.ToInt32((((sender as TextBlock).Parent as Canvas).Parent as StackPanel).Uid));
                if (meet != null)
                {
                    string availableDuraction = (sender as TextBlock).Uid.ToString();
                    if (!string.IsNullOrWhiteSpace(availableDuraction))
                    {
                        if (availableDuraction.StartsWith("PreviousClassEndTime"))
                            availableDuraction = meet.PreviousClassEndTime.ToString("hh:mm tt") + "-" + meet.StartTime.ToString("hh:mm tt");
                        else if (availableDuraction.StartsWith("EndTime"))
                            availableDuraction = meet.EndTime.ToString("hh:mm tt") + "-" + meet.NextClassStartTime.ToString("hh:mm tt");

                        availableDuraction = _selectedDate == DateTime.Now.ToString("yyyy-MM-dd") && availableDuraction.Split('-').First().Trim() == Constants.DayStartTime ? DateTime.Now.ToString("hh:mm tt") + " - " + availableDuraction.Split('-').Last() : availableDuraction.Trim();

                        string startTime = Convert.ToDateTime(availableDuraction.Split('-')[0].Trim()).ToString("hh:mm tt");
                        string endTime = Convert.ToDateTime(availableDuraction.Split('-')[1].Trim()).ToString("hh:mm tt");

                        if (_selectedDate == DateTime.Now.ToString("yyyy-MM-dd"))
                        {
                            startTime = DateTime.Parse(startTime) < DateTime.Now ? DateTime.Now.ToString("hh:mm tt") : startTime;
                        }

                        if (Convert.ToInt32((DateTime.Parse(endTime) - DateTime.Parse(startTime)).TotalMinutes) > 15)
                        {
                            availableDuraction = Convert.ToInt32((DateTime.Parse(endTime) - DateTime.Parse(startTime)).TotalMinutes) > 30 ? _selectedDate + "&" + startTime + " @ " + _selectedDate + "&" + DateTime.Parse(startTime).AddMinutes(30).ToString("hh:mm tt") : _selectedDate + "&" + startTime + " @ " + _selectedDate + "&" + endTime;

                            DateTime startDateTime = DateTime.Parse(_selectedDate + " " + startTime);
                            startDateTime = startDateTime.AddMinutes((int)Math.Round((double)startDateTime.Minute / 5) * 5 - startDateTime.Minute);
                            NewClassViewModel._classStartDateTime = startDateTime;

                            DateTime endDateTime = DateTime.Parse(_selectedDate + " " + endTime);
                            endDateTime = endDateTime.AddMinutes((int)Math.Round((double)endDateTime.Minute / 5) * 5 - endDateTime.Minute);
                            NewClassViewModel._classEndDateTime = Convert.ToInt32((DateTime.Parse(endTime) - DateTime.Parse(startTime)).TotalMinutes) > 30 ? startDateTime.AddMinutes(30) : endDateTime;

                            NewClassViewModel._isFromCarousel = true;
                            Messenger.Default.Send("txt_add_home_newclass", "ShowContentControl");
                        }
                    }
                    else
                    {
                        Messenger.Default.Send("Available time is too less to conduct a class in this timeslot. Please select another free slot", "Notification");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To get today class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_today_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _previousSelectedDate = _selectedDate = DateTime.Now.ToString("yyyy-MM-dd");

            LoadClassAsCarousel();
        }

        /// <summary>
        /// refresh calendar event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void imgRefreshCalendar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            App.ExecuteMethod(CreateCarouselItem);
        }

        /// <summary>
        /// close carousel view and goto homepage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_carousel_newclass_MouseUp(object sender, MouseEventArgs e)
        {
            if (HomePageViewModel._timer != null)
            {
                HomePageViewModel._timer.Stop();
                HomePageViewModel._timer.Interval = new TimeSpan(0, 0, 1);
                HomePageViewModel._timer.Start();
            }
            Messenger.Default.Unregister(this);
            Messenger.Default.Send("Calendar", "CloseContentControlView");
        }

        /// <summary>
        /// Adding New class if no Class available for selected date
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_noclass_future_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                DateTime startDateTime = DateTime.Parse(_selectedDate + " " + DateTime.Now.ToString("hh:mm tt"));
                startDateTime = startDateTime.AddMinutes((int)Math.Round((double)startDateTime.Minute / 5) * 5 - startDateTime.Minute);
                NewClassViewModel._classStartDateTime = startDateTime;

                NewClassViewModel._classEndDateTime = startDateTime.AddMinutes(30);

                NewClassViewModel._isFromCarousel = true;
                Messenger.Default.Send("txt_add_home_newclass", "ShowContentControl");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion
    }
}
