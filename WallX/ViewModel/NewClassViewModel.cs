using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Globalization;
using System.Linq;
using WallX.Services;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Data;
using WallX.Views;
using NextGen.Controls;
using System.Diagnostics;
using NextGen.Controls.InkRecognizer;
using WallX.Helpers;
using NextGen.Controls.Animations;

using Task = System.Threading.Tasks.Task;

namespace WallX.ViewModel
{
    public class NewClassViewModel : INotifyPropertyChanged
    {
        #region Variables

        private NewClassView _newClassView;
        private int lbDatesScrollIndex = 0;
        private bool _isFromReschedule = false;

        public static string _classDuration = string.Empty;
        public static bool _isFromCarousel = false;
        public static DateTime _classStartDateTime = DateTime.Now;
        public static DateTime _classEndDateTime = DateTime.Now.AddMinutes(30);

        public static Class _classDetails;

        #endregion Variables

        #region PageLoad

        /// <summary>
        /// View loading event for handling default values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewClassView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Messenger.Default.Register<string>(this, "delete_agendas", DeleteAgendas);
                Messenger.Default.Register<string>(this, "delete_agendas_confirm", DeleteAgendasConfirm);

                _newClassView = sender as NewClassView;
                _agendaList = new List<Agendas>();

                DragAndDrop.RegisterElement(_newClassView.canv_inviteParticipants, (dropElement, dragElement) => { _newClassView.canv_drophere_icon.Visibility = Visibility.Collapsed; }, DragDirection.Right, null, (dropElement, dragElement) => { _newClassView.canv_drophere_icon.Visibility = Visibility.Visible; });

                DragAndDrop.RegisterElement(_newClassView.canv_drop_here, DropElement, DragDirection.Right, null, (dropElement, dragElement) => { _newClassView.canv_drophere_icon.Visibility = Visibility.Visible; });

                RecognizeStrokes classTitle = new RecognizeStrokes(_newClassView.inkcanv_class_title, GotoKeyBoardClassTitle);

                Task.Run(() =>
                {
                    if (HomePageViewModel._contactsDbList == null)
                    {
                        HomePageViewModel._contactsDbList = Service.GetModuleDataList<Employees>(null);
                    }
                });

                HoursList = Enumerable.Range(1, 12).ToList();
                MinutesList = Enumerable.Range(0, 12).Select(s => s * 5).ToList();
                YearsList = Enumerable.Range(DateTime.Now.Year - 30, 60).ToList();

                MonthsList = DateTimeFormatInfo.CurrentInfo.MonthNames.ToList().Where(s => !string.IsNullOrWhiteSpace(s)).Select(k => DateTime.ParseExact(k, "MMMM", CultureInfo.InvariantCulture)).ToList();

                AlphabetsList = new List<string> { "All", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

                HideAllSteps();
                HideStepOnePopups();

                _newClassView.wp_status.Visibility = Visibility.Collapsed;
                _newClassView.canv_newclass.Visibility = Visibility.Visible;
                _newClassView.canv_confirmed_class.Visibility = Visibility.Collapsed;

                _newClassView.canv_nextstep.Visibility = Visibility.Visible;
                _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => c.IsHitTestVisible = false);
                EnableHitTestVisible(_newClassView.canv_ellipse_1);

                ClassCategoryList = Enum.GetNames(typeof(ClassCategoryType)).Select(s => NxgUtilities.GetStringUpperCharwithAddedSpace(s).Replace(" And ", " & ")).ToList();
                ClassTypeList = Enum.GetNames(typeof(ClassScheduleType)).Select(s => NxgUtilities.GetStringUpperCharwithAddedSpace(s)).ToList();
                RecurranceClassFrequencyTypeList = Enum.GetNames(typeof(RecurranceClassFrequencyType)).Select(s => NxgUtilities.GetStringUpperCharwithAddedSpace(s)).ToList();

                ClassList = Service.GetClassList(ClassFromDateTime);

                if (_classDetails != null)
                {
                    foreach (Participants participant in _classDetails.ParticipantList)
                    {
                        Employees emp = HomePageViewModel._contactsDbList.FirstOrDefault(k => k.EmployeeId == participant.EmployeeId);
                        if (participant != null)
                        {
                            participant.Employee = emp;
                        }
                    }

                    _classFixedStartDateTime = ClassFromDateTime = _classDetails.StartTime;
                    _classFixedEndDateTime = ClassToDateTime = _classDetails.EndTime;
                    ClassCategory = _classDetails.ClassCategory ?? ClassCategoryType.Mathematics.ToString();
                    ClassType = NxgUtilities.GetStringUpperCharwithAddedSpace(_classDetails.ClassType);
                    ClassTitle = _classDetails.ClassName;
                    Frequency = _classDetails.ClassFrequency;

                    _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
                    _invitedParticipantsList.AddRange(_classDetails.ParticipantList);

                    if (_invitedParticipantsList != null && _invitedParticipantsList.Count > 0)
                    {
                        Participants organizer = _invitedParticipantsList.FirstOrDefault(s => s.IsOrganizer == true);

                        Recognizedemail = organizer != null ? organizer.Employee.Email : _invitedParticipantsList.First().Employee.Email;

                        _invitedParticipantsList.ForEach(s => _newClassView.lb_invited_participants.Items.Add(s));
                        _newClassView.canv_drophere_icon.Visibility = _invitedParticipantsList.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    }
                    _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => c.IsHitTestVisible = true);

                    _classDetails.AgendaList.ToList().ForEach(s => { _agendaList.Add(s); _completedDuration += s.Minutes; });

                    _agendaList.ForEach(s => { s.Participants = _invitedParticipantsList; s.AgendaDuration = Convert.ToInt32(TimeSpan.Parse(s.Duration).TotalMinutes); s.Presenter = _invitedParticipantsList.FirstOrDefault(k => k.EmployeeId == s.EmployeeId); });

                    _newClassView.canv_classtype.Visibility = Visibility.Collapsed;
                    _newClassView.canv_singleday_multi_class.Visibility = Visibility.Collapsed;
                    _newClassView.wp_scheduled_slots.Visibility = Visibility.Collapsed;

                    _isFromReschedule = true;

                    //Messenger.Default.Register<DateTime>(this, "AgendaTimer", timer_Tick);
                }
                else
                {
                    _classFixedStartDateTime = ClassFromDateTime = _classStartDateTime;
                    _classFixedEndDateTime = ClassToDateTime = _classEndDateTime;

                    _newClassView.canv_classtype.Visibility = Visibility.Visible;
                    _isFromReschedule = false;

                }
                _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;

                _newClassView.lb_confirmed_participants.ItemsSource = null;
                _newClassView.lb_confirmed_participants.ItemsSource = _invitedParticipantsList;

                _newClassView.lb_agenda_presenter.ItemsSource = null;
                _newClassView.lb_agenda_presenter.ItemsSource = _invitedParticipantsList;

                _newClassView.canv_add_agenda.Visibility = Visibility.Collapsed;
                _newClassView.canv_agendas_list.Visibility = Visibility.Visible;

            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Hide all steps
        /// </summary>
        private void HideAllSteps()
        {
            NxgUtilities.CollapseElements(new List<FrameworkElement> { _newClassView.canv_newclass, _newClassView.canv_inviteParticipants, _newClassView.canv_classtitle, _newClassView.canv_class_confirmation, _newClassView.canv_total_class, _newClassView.canv_agenda });
        }

        /// <summary>
        /// To enable Ellipse hitTestvisible property
        /// </summary>
        /// <param name="canvasEllipse"></param>
        private void EnableHitTestVisible(Canvas canvasEllipse)
        {
            canvasEllipse.IsHitTestVisible = true;
        }

        /// <summary>
        /// Bind yes or no visibility & organizer opacity for existing class
        /// </summary>
        private void BindInvitedParticipants()
        {
            try
            {
                List<Participants> employeeList = _newClassView.lb_invited_participants.Items.Cast<Participants>().ToList();

                if (employeeList != null && employeeList.Count > 0)
                {
                    foreach (Participants item in employeeList.Where(s => s.IsOptional == true || s.IsOrganizer == true).ToList())
                    {
                        if (item.IsOrganizer && item.Employee != null)
                            Recognizedemail = item.Employee.Email;

                        int index = employeeList.IndexOf(item);
                        if (index > -1)
                        {
                            ContentPresenter myContentPresenter = TemplateModifier.GetContentPresenter(_newClassView.lb_invited_participants, index);
                            _newClassView.lb_invited_participants.SelectedIndex = 0;

                            if (myContentPresenter != null)
                            {
                                Canvas canvOrganizer = null, canvOptional = null;
                                if (item.IsOrganizer == true)
                                {
                                    canvOrganizer = (myContentPresenter.ContentTemplate as DataTemplate).FindName("canv_organizer", myContentPresenter) as Canvas;
                                    if (canvOrganizer != null)
                                    {
                                        Participant_optional(canvOrganizer);
                                    }
                                }
                                if (item.IsOptional == true)
                                {
                                    canvOptional = (myContentPresenter.ContentTemplate as DataTemplate).FindName("canv_no_optional", myContentPresenter) as Canvas;
                                    if (canvOptional != null)
                                    {
                                        if (item.IsOptional == true)
                                            ((canvOptional.Parent as Canvas).Children[5] as Slider).Value = 0.1;
                                        Participant_optional(canvOptional);
                                    }
                                }
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
        /// close the new class view event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_newclass_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                _classDetails = null;
                _recurringClassDatesList = null;
                conflictClassList = null;
                _newClassView.txt_from_date.IsHitTestVisible = true;
                _isFromConflictResolve = false;
                _isRecurrentOrSingleDayMultiClass = string.Empty;
                Messenger.Default.Unregister(this);
                Messenger.Default.Send("newclass", "CloseContentControlView");

            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion PageLoad

        #region Step 1

        private int _classTotalDuration = 0;
        private bool _isFromConflictResolve = false;
        private string _isRecurrentOrSingleDayMultiClass = string.Empty;
        private List<MultiClass> _recurringClassDatesList = new List<MultiClass>();
        private List<Class> conflictClassList = new List<Class>();

        public static DateTime _classFixedStartDateTime = DateTime.MinValue;
        public static DateTime _classFixedEndDateTime = DateTime.MinValue;

        private bool ValidateStep1()
        {
            try
            {
                ConflictClassList = null;
                if (ClassType != NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.SingleDayMultipleClass.ToString()) && ClassFromDateTime.Date < DateTime.Today)
                {
                    Messenger.Default.Send("You can't create Class in the Past time..!", "Notification");
                }
                else if (ClassType != NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.SingleDayMultipleClass.ToString()) && ClassFromDateTime > ClassToDateTime)
                {
                    Messenger.Default.Send("Class 'StartTime' is More than 'EndTime'", "Notification");
                }
                else
                {
                    if (_agendaList.Count > 0 && (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) ? (_classFixedStartDateTime.ToString("MM/dd/yyyy hh:mm tt") != ClassFromDateTime.ToString("MM/dd/yyyy hh:mm tt") || _classFixedEndDateTime.ToString("MM/dd/yyyy hh:mm tt") != ClassToDateTime.ToString("MM/dd/yyyy hh:mm tt")) : (_classFixedStartDateTime.ToString("MM/dd/yyyy hh:mm tt") != ClassFromDateTime.ToString("MM/dd/yyyy hh:mm tt") && _classFixedEndDateTime.ToString("MM/dd/yyyy hh:mm tt") != ClassToDateTime.ToString("MM/dd/yyyy hh:mm tt"))))
                    {
                        Messenger.Default.Send(new KeyValuePair<string, string>("Delete Agendas", "Class time has changed, Would you like to delete all its plans ?"), "Result");
                        return false;
                    }

                    if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.RecurringClass.ToString()) && !_isFromReschedule)
                    {
                        _newClassView.txt_from_date.IsHitTestVisible = false;
                        if (ClassFromDateTime > RecurringFromDateTime)
                        {
                            Messenger.Default.Send("Recurring Class 'StartTime' is More than 'EndTime'", "Notification");
                            return false;
                        }

                        List<DateTime> datesList = GetRecurringClassDatesList();

                        if (conflictClassList == null)
                        {
                            conflictClassList = new List<Class>();
                        }
                        if (_recurringClassDatesList == null || conflictClassList == null || conflictClassList.Count == 0)
                        {
                            _recurringClassDatesList = new List<MultiClass>();
                        }

                        if (!_isFromConflictResolve)
                        {
                            foreach (var classStartDateTime in datesList)
                            {
                                _recurringClassDatesList.Add(new MultiClass() { from_date_time = DateTime.Parse(classStartDateTime.ToString("yyyy-MM-dd") + " " + ClassFromDateTime.ToString("hh:mm:ss tt")), to_date_time = DateTime.Parse(classStartDateTime.ToString("yyyy-MM-dd") + " " + ClassToDateTime.ToString("hh:mm:ss tt")) });
                            }
                        }

                        if (_recurringClassDatesList.Count > 0)
                        {
                            foreach (var classStartDateTime in datesList)
                            {
                                if (conflictClassList.Where(s => s.StartTime.ToString("yyyy-MM-dd") == classStartDateTime.ToString("yyyy-MM-dd")).ToList().Count > 0)
                                    _recurringClassDatesList.Where(s => s.from_date_time.ToString("yyyy-MM-dd") == classStartDateTime.ToString("yyyy-MM-dd")).ToList().ForEach(k => { k.from_date_time = classStartDateTime; k.to_date_time = DateTime.Parse(classStartDateTime.ToString("yyyy-MM-dd") + " " + ClassToDateTime.ToString("hh:mm:ss tt")); });
                            }
                            conflictClassList = new List<Class>();
                        }

                        foreach (MultiClass multiClassItem in _recurringClassDatesList)
                        {
                            ClassFromDateTime = multiClassItem.from_date_time;
                            ClassToDateTime = multiClassItem.to_date_time;
                            ClassList = Service.GetClassList(ClassFromDateTime);
                            if (ClassList != null && ClassList.Count > 0)
                            {
                                List<Class> coflictClass = ClassList.Where(s => ((s.StartTime >= ClassToDateTime && s.EndTime <= ClassToDateTime) || (s.StartTime <= ClassToDateTime && s.StartTime >= ClassFromDateTime) || (s.StartTime >= ClassFromDateTime && s.StartTime <= ClassToDateTime) || (s.StartTime <= ClassFromDateTime && s.EndTime >= ClassFromDateTime) || (s.StartTime <= ClassToDateTime && s.EndTime >= ClassToDateTime)) && (_classDetails == null || s.ClassId != _classDetails.ClassId)).ToList();
                                coflictClass.ForEach(s => { s.ConflictClassStartTime = multiClassItem.from_date_time; s.ConflictClassEndTime = multiClassItem.to_date_time; });
                                conflictClassList.AddRange(coflictClass);
                            }
                        }
                        ConflictClassList = conflictClassList;
                        _isRecurrentOrSingleDayMultiClass = "Recurrent";
                    }

                    else if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) && ClassList != null && ClassList.Count > 0)
                    {
                        conflictClassList = ClassList.Where(s => ((s.StartTime >= ClassToDateTime && s.EndTime <= ClassToDateTime) || (s.StartTime <= ClassToDateTime && s.StartTime >= ClassFromDateTime) || (s.StartTime >= ClassFromDateTime && s.StartTime <= ClassToDateTime) || (s.StartTime <= ClassFromDateTime && s.EndTime >= ClassFromDateTime) || (s.StartTime <= ClassToDateTime && s.EndTime >= ClassToDateTime)) && (_classDetails == null || s.ClassId != _classDetails.ClassId)).ToList();
                        conflictClassList.ForEach(s => { s.ConflictClassStartTime = Convert.ToDateTime(s.StartTime.ToString("yyyy-MM-dd") + " " + _newClassView.txt_from_time.Text); s.ConflictClassEndTime = Convert.ToDateTime(s.EndTime.ToString("yyyy-MM-dd") + " " + _newClassView.txt_to_time.Text); });
                        ConflictClassList = null;
                        ConflictClassList = conflictClassList;
                    }

                    if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.SingleDayMultipleClass.ToString()) && !_isFromReschedule)
                    {
                        _newClassView.txt_from_date.IsHitTestVisible = false;
                        if (conflictClassList == null)
                            conflictClassList = new List<Class>();
                        ClassList = Service.GetClassList(ClassFromDateTime);
                        if (_isFromConflictResolve)
                        {
                            if (!MultiClassList.Any(s => (s.from_date_time <= ClassFromDateTime && s.to_date_time >= ClassFromDateTime) || (s.from_date_time <= ClassToDateTime && s.to_date_time >= ClassToDateTime) || (s.from_date_time <= ClassFromDateTime && s.to_date_time >= ClassToDateTime) || (s.from_date_time >= ClassFromDateTime && s.from_date_time <= ClassToDateTime && s.to_date_time >= ClassFromDateTime && s.to_date_time <= ClassToDateTime)))
                            {
                                _multipleClassList.Add(new MultiClass() { from_date_time = ClassFromDateTime, to_date_time = ClassToDateTime });

                                MultiClassList = null;
                                MultiClassList = _multipleClassList;
                            }
                            else
                            {
                                Messenger.Default.Send("This time slot is already in use, Please select Different time slot", "Notification");
                                return false;
                            }
                        }

                        if (MultiClassList == null || MultiClassList.Count == 0)
                        {
                            Messenger.Default.Send("Please select atleast one time slot", "Notification");
                            return false;
                        }

                        foreach (MultiClass multiclassitem in MultiClassList)
                        {
                            ClassFromDateTime = multiclassitem.from_date_time;
                            ClassToDateTime = multiclassitem.to_date_time;
                            if (ClassList != null && ClassList.Count > 0)
                            {
                                List<Class> coflictClass = new List<Class>();

                                coflictClass = ClassList.Where(s => ((s.StartTime >= ClassToDateTime && s.EndTime <= ClassToDateTime) || (s.StartTime <= ClassToDateTime && s.StartTime >= ClassFromDateTime) || (s.StartTime >= ClassFromDateTime && s.StartTime <= ClassToDateTime) || (s.StartTime <= ClassFromDateTime && s.EndTime >= ClassFromDateTime) || (s.StartTime <= ClassToDateTime && s.EndTime >= ClassToDateTime)) && (_classDetails == null || s.ClassId != _classDetails.ClassId)).ToList();

                                List<Class> duplicateCoflictClass = NxgUtilities.GetDuplicateOfObject(coflictClass);

                                duplicateCoflictClass.ForEach(s => { s.ConflictClassStartTime = multiclassitem.from_date_time; s.ConflictClassEndTime = multiclassitem.to_date_time; });

                                conflictClassList.AddRange(duplicateCoflictClass);
                            }
                        }
                        ConflictClassList = null;
                        ConflictClassList = conflictClassList;
                        _isRecurrentOrSingleDayMultiClass = "SingleDayMulti";
                    }

                    ClosePopupAnimaton();
                    HideAllSteps();

                    _classFixedStartDateTime = ClassFromDateTime;
                    _classFixedEndDateTime = ClassToDateTime;
                    _classTotalDuration = Convert.ToInt32(ClassToDateTime.Subtract(ClassFromDateTime).TotalMinutes);
                    //_newClassView.tbk_total_duration.Text = Convert.ToString(_classTotalDuration);
                    _remainingDuration = _classTotalDuration - _completedDuration;
                    _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);
                    //_newClassView.slider_agenda.Maximum = _classTotalDuration;
                    _isFromConflictResolve = false;

                    _newClassView.wp_scheduled_slots.Visibility = Visibility.Collapsed;

                    if (ClassType != NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) && !_isFromReschedule)
                        _newClassView.wp_scheduled_slots.Visibility = Visibility.Visible;

                    if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                    {
                        ClassFromDateTime = _recurringClassDatesList[0].from_date_time;
                        RecurringFromDateTime = _recurringClassDatesList[_recurringClassDatesList.Count - 1].from_date_time;

                        _newClassView.tbk_confirmed_class.Text = _recurringClassDatesList.Count.ToString();
                    }
                    else if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                    {
                        ClassFromDateTime = MultiClassList[0].from_date_time;
                        ClassToDateTime = MultiClassList[MultiClassList.Count - 1].to_date_time;

                        if (MultiClassList != null)
                            _newClassView.tbk_confirmed_class.Text = MultiClassList.Count.ToString();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return false;
        }

        private void GoToStep1()
        {
            _newClassView.canv_newclass.Visibility = Visibility.Visible;
            _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
            _newClassView.path_1.Opacity = 0.8;

            _newClassView.wp_status.Visibility = Visibility.Collapsed;
            _newClassView.canv_nextstep.Visibility = Visibility.Visible;

            _currentEllipseIndex = 0;

            if (!_isFromConflictResolve)
            {
                _newClassView.canv_classtype.Visibility = _isFromReschedule ? Visibility.Collapsed : Visibility.Visible;

                _isFromReschedule = true;

                if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                {
                    _newClassView.canv_singleday_multi_class.Visibility = Visibility.Visible;
                }
                if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                {
                    _newClassView.canv_classtype.Visibility = Visibility.Visible;
                }
            }
        }

        private void DeleteAgendas(string obj)
        {
            try
            {
                if (_agendaList.Count > 0)
                {
                    _agendaList.Clear();
                    _completedDuration = 0;
                }
                if (_classDetails != null && _classDetails.AgendaList != null && _classDetails.AgendaList.Count > 0)
                {
                    _classDetails.AgendaList.ForEach(a => Service.InsertOrUpdateDataToDB(a, CrudActions.Delete, a.AgendaId));
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// close pop ups using animation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_close_popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                string elementName = (sender as FrameworkElement).Name;
                if (elementName == "canv_conflict_class")
                    _newClassView.canv_recursive_class_reschedule.Visibility = Visibility.Hidden;
                else if (elementName == "canv_month_close")
                    _newClassView.canv_month_selection.Visibility = Visibility.Collapsed;
                else if (elementName == "canv_total_class")
                    _newClassView.canv_recursive_class_reschedule.Visibility = Visibility.Hidden;
                else if (!new List<string> { "canv_class_type", "canv_month_selection", "canv_class_date", "canv_class_time", "canv_class_occurance", "canv_recurring_date" }.Contains(elementName))
                    ClosePopupAnimaton();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// open class types selection event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void txt_class_type_MouseDown(object sender, MouseButtonEventArgs args)
        {
            try
            {
                args.Handled = true;
                HideStepOnePopups();
                _newClassView.canv_class_type.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_class_type, 0.7, 1, 0.4);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void tbk_class_period_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                HideStepOnePopups();
                _newClassView.canv_recurrance_type.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_recurrance_type, 0.7, 1, 0.4);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Open date selection event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_from_date_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isFromClassDate = true;
                e.Handled = true;
                HideStepOnePopups();
                _newClassView.canv_class_date.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_class_date, 0.7, 1, 0.4);

                int day = ClassFromDateTime.Date.Day;
                lbDatesScrollIndex = day < _newClassView.lb_dates.Items.Count - 3 ? day + 2 : day - 1;
                _newClassView.lb_dates.ScrollIntoView(_newClassView.lb_dates.Items[lbDatesScrollIndex]);

                GetSelectedDateClass();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Date selection changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void lb_dates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_newClassView.lb_dates.SelectedIndex > -1)
                {
                    DateTime currentDateTime = ClassFromDateTime;
                    ClassFromDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, Convert.ToDateTime(_newClassView.lb_dates.SelectedItem).Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

                    DateTime toDateTime = ClassToDateTime;
                    ClassToDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, Convert.ToDateTime(_newClassView.lb_dates.SelectedItem).Day, toDateTime.Hour, toDateTime.Minute, toDateTime.Second);

                    GetSelectedDateClass();

                    _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Open month & year selection event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_month_year_popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                _newClassView.canv_month_selection.Visibility = Visibility.Visible;

                if (isFromClassDate)
                {
                    _newClassView.listbox_months.SelectedIndex = ClassFromDateTime.Month - 1;
                    _newClassView.listbox_years.SelectedIndex = YearsList.IndexOf(ClassFromDateTime.Year);
                }
                else
                {
                    _newClassView.listbox_months.SelectedIndex = RecurringFromDateTime.Month - 1;
                    _newClassView.listbox_years.SelectedIndex = YearsList.IndexOf(RecurringFromDateTime.Year);
                }

                if (_newClassView.listbox_months.SelectedIndex != -1)
                    _newClassView.listbox_months.ScrollIntoView(_newClassView.listbox_months.Items[_newClassView.listbox_months.SelectedIndex]);
                if (_newClassView.listbox_years.SelectedIndex != -1)
                    _newClassView.listbox_years.ScrollIntoView(_newClassView.listbox_years.Items[_newClassView.listbox_years.SelectedIndex]);
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
        public void listbox_months_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedIndex != -1)
                {
                    DateTime currentDateTime = isFromClassDate ? ClassFromDateTime : RecurringFromDateTime;

                    if (isFromClassDate)
                        ClassFromDateTime = new DateTime(currentDateTime.Year, Convert.ToDateTime(listBox.SelectedItem).Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
                    else
                        RecurringFromDateTime = new DateTime(currentDateTime.Year, Convert.ToDateTime(listBox.SelectedItem).Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

                    GetSelectedDateClass();

                    if (isFromClassDate)
                        _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
                    else
                        _newClassView.lb_dates_recurring.SelectedIndex = Convert.ToInt32(RecurringFromDateTime.ToString("dd")) - 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// year selection changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listbox_years_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedIndex != -1)
                {
                    DateTime currentDateTime = isFromClassDate ? ClassFromDateTime : RecurringFromDateTime;

                    if (isFromClassDate)
                        ClassFromDateTime = new DateTime(Convert.ToInt32(listBox.SelectedItem), currentDateTime.Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
                    else
                        RecurringFromDateTime = new DateTime(Convert.ToInt32(listBox.SelectedItem), currentDateTime.Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

                    GetSelectedDateClass();

                    if (isFromClassDate)
                        _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
                    else
                        _newClassView.lb_dates_recurring.SelectedIndex = Convert.ToInt32(RecurringFromDateTime.ToString("dd")) - 1;

                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// scroll dates listbox items with 7 items to previous
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_dates_prev_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (NxgScrollItems.itemScrollIndex == -1)
                    NxgScrollItems.itemScrollIndex = lbDatesScrollIndex;
                NxgScrollItems.ScrollPrevious(_newClassView.lb_dates, lbDatesScrollIndex, 7, _newClassView.canv_dates_prev, _newClassView.canv_dates_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// scroll dates listbox items with 7 items to next
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_dates_next_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (NxgScrollItems.itemScrollIndex == -1)
                    NxgScrollItems.itemScrollIndex = lbDatesScrollIndex;
                NxgScrollItems.ScrollNext(_newClassView.lb_dates, lbDatesScrollIndex, 7, _newClassView.canv_dates_prev, _newClassView.canv_dates_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to next class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_next_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollNext(_newClassView.listbox_all_class, _newClassView.canv_all_class_prev, _newClassView.canv_all_class_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to previous class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_prev_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollPrevious(_newClassView.listbox_all_class, _newClassView.canv_all_class_prev, _newClassView.canv_all_class_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to previous class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_time_prev_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollPrevious(_newClassView.listbox_all_class_time, _newClassView.canv_all_class_time_prev, _newClassView.canv_all_class_time_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to next class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_time_next_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollNext(_newClassView.listbox_all_class_time, _newClassView.canv_all_class_time_prev, _newClassView.canv_all_class_time_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Open time selection event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_time_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                HideStepOnePopups();

                _newClassView.canv_class_time.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_class_time, 0.7, 1, 0.4);

                GetSelectedDateClass();

                if (e.Source is TextBlock)
                {
                    if ((e.Source as TextBlock).Name == "txt_to_time")
                        GetEndTimeIndexes();
                    else
                        GetStartTimeIndexes();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Selected time slot binding event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_timeslot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if ((sender as Canvas).Name == "canv_endtime")
                    GetEndTimeIndexes();
                else
                    GetStartTimeIndexes();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Time slot selection changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void lb_time_slot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox.SelectedItem != null)
                {
                    string selectedItem = Convert.ToString(listBox.Tag);
                    DateTime currentFromDateTime = ClassFromDateTime;
                    DateTime currentToDateTime = ClassToDateTime;
                    if (selectedItem == "H" && _newClassView.path_start_time.Visibility == Visibility.Visible)
                    {
                        int selectedHour = Convert.ToInt32(listBox.SelectedItem);
                        ClassFromDateTime = new DateTime(currentFromDateTime.Year, currentFromDateTime.Month, currentFromDateTime.Day, (currentFromDateTime.ToString("tt") == "PM" && selectedHour != 12 ? selectedHour + 12 : selectedHour), currentFromDateTime.Minute, currentFromDateTime.Second);

                        if (RecurringFromDateTime != null)
                            RecurringFromDateTime = new DateTime(RecurringFromDateTime.Year, RecurringFromDateTime.Month, RecurringFromDateTime.Day, (RecurringFromDateTime.ToString("tt") == "PM" && selectedHour != 12 ? selectedHour + 12 : selectedHour), RecurringFromDateTime.Minute, RecurringFromDateTime.Second);

                        GetSessionOpacity(true);
                    }
                    else if (selectedItem == "H" && _newClassView.path_end_time.Visibility == Visibility.Visible)
                    {
                        int selectedHour = Convert.ToInt32(listBox.SelectedItem);
                        ClassToDateTime = new DateTime(currentToDateTime.Year, currentToDateTime.Month, currentToDateTime.Day, (currentFromDateTime.ToString("tt") == "PM" && selectedHour != 12 ? selectedHour + 12 : selectedHour), currentToDateTime.Minute, currentToDateTime.Second);

                        if (RecurringToDateTime != null)
                            RecurringToDateTime = new DateTime(RecurringToDateTime.Year, RecurringToDateTime.Month, RecurringToDateTime.Day, (RecurringToDateTime.ToString("tt") == "PM" && selectedHour != 12 ? selectedHour + 12 : selectedHour), RecurringToDateTime.Minute, RecurringToDateTime.Second);

                        GetSessionOpacity(false);
                    }
                    else if (selectedItem == "M" && _newClassView.path_start_time.Visibility == Visibility.Visible)
                    {
                        ClassFromDateTime = new DateTime(currentFromDateTime.Year, currentFromDateTime.Month, currentFromDateTime.Day, currentFromDateTime.Hour, Convert.ToInt32(listBox.SelectedItem), currentFromDateTime.Second);

                        if (RecurringFromDateTime != null)
                            RecurringFromDateTime = new DateTime(RecurringFromDateTime.Year, RecurringFromDateTime.Month, RecurringFromDateTime.Day, RecurringFromDateTime.Hour, Convert.ToInt32(listBox.SelectedItem), RecurringFromDateTime.Second);
                    }
                    else if (selectedItem == "M" && _newClassView.path_end_time.Visibility == Visibility.Visible)
                    {
                        ClassToDateTime = new DateTime(currentToDateTime.Year, currentToDateTime.Month, currentToDateTime.Day, currentToDateTime.Hour, Convert.ToInt32(listBox.SelectedItem), currentToDateTime.Second);

                        if (RecurringToDateTime != null)
                            RecurringToDateTime = new DateTime(RecurringToDateTime.Year, RecurringToDateTime.Month, RecurringToDateTime.Day, RecurringToDateTime.Hour, Convert.ToInt32(listBox.SelectedItem), RecurringToDateTime.Second);
                    }

                    _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Time slot session binding evengt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_session_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                DateTime currentFromDateTime = ClassFromDateTime;
                DateTime currentToDateTime = ClassToDateTime;
                if (_newClassView.path_start_time.Visibility == Visibility.Visible && (sender as Canvas).Tag.ToString() == "PM")
                {
                    ClassFromDateTime = new DateTime(currentFromDateTime.Year, currentFromDateTime.Month, currentFromDateTime.Day, currentFromDateTime.Hour < 12 ? (currentFromDateTime.Hour + 12) : currentFromDateTime.Hour, currentFromDateTime.Minute, currentFromDateTime.Second);
                }
                else if (_newClassView.path_start_time.Visibility == Visibility.Visible && (sender as Canvas).Tag.ToString() == "AM")
                {
                    ClassFromDateTime = new DateTime(currentFromDateTime.Year, currentFromDateTime.Month, currentFromDateTime.Day, currentFromDateTime.Hour >= 12 ? (currentFromDateTime.Hour - 12) : currentFromDateTime.Hour, currentFromDateTime.Minute, currentFromDateTime.Second);
                }
                else if (_newClassView.path_end_time.Visibility == Visibility.Visible && (sender as Canvas).Tag.ToString() == "PM")
                {
                    ClassToDateTime = new DateTime(currentToDateTime.Year, currentToDateTime.Month, currentToDateTime.Day, currentToDateTime.Hour < 12 ? (currentToDateTime.Hour + 12) : currentToDateTime.Hour, currentToDateTime.Minute, currentToDateTime.Second);
                }
                else if (_newClassView.path_end_time.Visibility == Visibility.Visible && (sender as Canvas).Tag.ToString() == "AM")
                {
                    ClassToDateTime = new DateTime(currentToDateTime.Year, currentToDateTime.Month, currentToDateTime.Day, currentToDateTime.Hour >= 12 ? (currentToDateTime.Hour - 12) : currentToDateTime.Hour, currentToDateTime.Minute, currentToDateTime.Second);
                }

                GetSessionOpacity(_newClassView.path_start_time.Visibility == Visibility.Visible);

                _newClassView.lb_dates.SelectedIndex = Convert.ToInt32(ClassFromDateTime.ToString("dd")) - 1;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// to select one time/ reccurance/ multiple class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_class_reccurence_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                HideStepOnePopups();
                _newClassView.canv_class_occurance.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_class_occurance, 0.7, 1, 0.4);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// class occurence listbox selection changed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void lb_class_occurence_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ClosePopupAnimaton();
                _newClassView.listbox_reccurance_frequency_type.SelectedIndex = -1;
                RecurringFromDateTime = ClassFromDateTime;
                if (Convert.ToString(_newClassView.lb_class_occurence.SelectedItem).ToLower() == "recurring class")
                    _newClassView.listbox_reccurance_frequency_type.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// class reccurance type selection changed (daily/weekly/monthly..)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listbox_reccurance_frequency_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_newClassView.listbox_reccurance_frequency_type.SelectedIndex > -1)
                {
                    //RecurringMeetingsList = null;
                    //RecurringConflictsMeetingsList = null;
                    FrequencyEndDate = null;
                    //selectedRecurranceMeetingFrequencyIndex = _newMeetingView.listbox_reccurance_frequency_type.SelectedIndex;

                    Frequency = _newClassView.txt_class_period.Text = _newClassView.listbox_reccurance_frequency_type.SelectedItem.ToString();

                    //switch ((RecurranceMeetingFrequencyType)Enum.Parse(typeof(RecurranceMeetingFrequencyType), Frequency))
                    //{
                    //    case RecurranceMeetingFrequencyType.Alternatedays:
                    //    case RecurranceMeetingFrequencyType.Daily:
                    //        RecurringFromDateTime = MeetingFromDateTime.AddDays(1);
                    //        break;
                    //    case RecurranceMeetingFrequencyType.Weekly:
                    //        RecurringFromDateTime = MeetingFromDateTime.AddDays(7);
                    //        break;
                    //    case RecurranceMeetingFrequencyType.Monthly:
                    //        RecurringFromDateTime = MeetingFromDateTime.AddMonths(1);
                    //        break;
                    //    case RecurranceMeetingFrequencyType.Yearly:
                    //        RecurringFromDateTime = MeetingFromDateTime.AddYears(1);
                    //        break;
                    //    default:
                    //        break;
                    //}

                    ClosePopupAnimaton();
                }
                else
                    Frequency = _newClassView.txt_class_period.Text = null;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private bool isFromClassDate = true;

        public void txt_to_date_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isFromClassDate = false;
                e.Handled = true;
                HideStepOnePopups();
                _newClassView.canv_recurring_date.Visibility = Visibility.Visible;

                Animation.Scale(_newClassView.canv_recurring_date, 0.7, 1, 0.4);

                int day = RecurringFromDateTime.Date.Day;

                _newClassView.lb_dates_recurring.SelectedIndex = Convert.ToInt32(RecurringFromDateTime.ToString("dd")) - 1;

                lbDatesScrollIndex = day < _newClassView.lb_dates_recurring.Items.Count - 3 ? day + 2 : day - 1;
                _newClassView.lb_dates_recurring.ScrollIntoView(_newClassView.lb_dates_recurring.Items[lbDatesScrollIndex]);

                GetSelectedDateClass();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// scroll dates listbox items with 7 items to previous
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_dates_prev_recurring_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (NxgScrollItems.itemScrollIndex == -1)
                    NxgScrollItems.itemScrollIndex = lbDatesScrollIndex;
                NxgScrollItems.ScrollPrevious(_newClassView.lb_dates_recurring, lbDatesScrollIndex, 7, _newClassView.canv_dates_prev_recurring, _newClassView.canv_dates_next_recurring);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// scroll dates listbox items with 7 items to next
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_dates_next_recurring_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                if (NxgScrollItems.itemScrollIndex == -1)
                    NxgScrollItems.itemScrollIndex = lbDatesScrollIndex;
                NxgScrollItems.ScrollNext(_newClassView.lb_dates_recurring, lbDatesScrollIndex, 7, _newClassView.canv_dates_prev_recurring, _newClassView.canv_dates_next_recurring);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to next class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_next_recurring_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollNext(_newClassView.listbox_all_class_recurring, _newClassView.canv_all_class_prev_recurring, _newClassView.canv_all_class_next_recurring);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Scroll to previous class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_all_class_prev_recurring_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollPrevious(_newClassView.listbox_all_class_recurring, _newClassView.canv_all_class_prev_recurring, _newClassView.canv_all_class_next_recurring);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void lb_dates_recurring_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_newClassView.lb_dates_recurring.SelectedIndex > -1)
                {
                    DateTime currentDateTime = RecurringFromDateTime;
                    RecurringFromDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, Convert.ToDateTime(_newClassView.lb_dates_recurring.SelectedItem).Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

                    DateTime toDateTime = RecurringToDateTime;
                    RecurringToDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, Convert.ToDateTime(_newClassView.lb_dates_recurring.SelectedItem).Day, toDateTime.Hour, toDateTime.Minute, toDateTime.Second);

                    GetSelectedDateClass();

                    _newClassView.lb_dates_recurring.SelectedIndex = Convert.ToInt32(RecurringFromDateTime.ToString("dd")) - 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Hide all step 1 popups with animation
        /// </summary>
        public void HideStepOnePopups()
        {
            NxgUtilities.CollapseElements(new List<FrameworkElement> { _newClassView.canv_class_type, _newClassView.canv_class_date, _newClassView.canv_class_time, _newClassView.canv_class_occurance, _newClassView.canv_recurrance_type, _newClassView.canv_month_selection, _newClassView.canv_recurring_date });
        }

        /// <summary>
        /// Get selected date class from db
        /// </summary>
        private void GetSelectedDateClass()
        {
            try
            {
                ClassList = isFromClassDate ? Service.GetClassList(ClassFromDateTime) : Service.GetClassList(RecurringFromDateTime);

                NxgUtilities.CollapseElements(new List<FrameworkElement> { _newClassView.canv_all_class_next, _newClassView.canv_all_class_prev, _newClassView.canv_all_class_time_next, _newClassView.canv_all_class_time_prev, _newClassView.canv_all_class_next_recurring, _newClassView.canv_all_class_prev_recurring });

                if (ClassList != null && ClassList.Count > 0)
                {
                    if (isFromClassDate)
                    {
                        int selectedIndex = ClassList.IndexOf(ClassList.FirstOrDefault(s => s.StartTime >= ClassFromDateTime));
                        selectedIndex = selectedIndex == -1 ? 0 : selectedIndex;

                        _newClassView.listbox_all_class.SelectedIndex = selectedIndex;
                        _newClassView.listbox_all_class.ScrollIntoView(_newClassView.listbox_all_class.SelectedItem);

                        _newClassView.listbox_all_class_time.SelectedIndex = selectedIndex;
                        _newClassView.listbox_all_class_time.ScrollIntoView(_newClassView.listbox_all_class_time.SelectedItem);

                        if (ClassList.Count - 1 > selectedIndex)
                            _newClassView.canv_all_class_next.Visibility = _newClassView.canv_all_class_time_next.Visibility = Visibility.Visible;

                        if (selectedIndex > 0)
                            _newClassView.canv_all_class_prev.Visibility = _newClassView.canv_all_class_time_prev.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        int selectedIndex = ClassList.IndexOf(ClassList.FirstOrDefault(s => s.StartTime >= RecurringFromDateTime));
                        selectedIndex = selectedIndex == -1 ? 0 : selectedIndex;

                        _newClassView.listbox_all_class_recurring.SelectedIndex = selectedIndex;
                        _newClassView.listbox_all_class.ScrollIntoView(_newClassView.listbox_all_class_recurring.SelectedItem);

                        if (ClassList.Count - 1 > selectedIndex)
                            _newClassView.canv_all_class_next_recurring.Visibility = Visibility.Visible;

                        if (selectedIndex > 0)
                            _newClassView.canv_all_class_prev_recurring.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Get index for time slots to bind in months & years for start time
        /// </summary>
        private void GetStartTimeIndexes()
        {
            try
            {
                _newClassView.path_start_time.Visibility = _newClassView.path_end_time.Visibility = Visibility.Collapsed;
                _newClassView.path_start_time.Visibility = Visibility.Visible;

                HoursSelectedIndex = HoursList.IndexOf(ClassFromDateTime.Hour > 12 ? ClassFromDateTime.Hour - 12 : ClassFromDateTime.Hour);
                MinutesSelectedIndex = MinutesList.IndexOf(ClassFromDateTime.Minute / 5 * 5);

                GetSessionOpacity(true);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Get index for time slots to bind in months & years for end time
        /// </summary>
        private void GetEndTimeIndexes()
        {
            try
            {
                _newClassView.path_start_time.Visibility = _newClassView.path_end_time.Visibility = Visibility.Collapsed;
                _newClassView.path_end_time.Visibility = Visibility.Visible;

                HoursSelectedIndex = HoursList.IndexOf(ClassToDateTime.Hour > 12 ? ClassToDateTime.Hour - 12 : ClassToDateTime.Hour);
                MinutesSelectedIndex = MinutesList.IndexOf(ClassToDateTime.Minute / 5 * 5);

                GetSessionOpacity(false);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Get opacity values to bind in session items
        /// </summary>
        /// <param name="isFromStart"></param>
        private void GetSessionOpacity(bool isFromStart)
        {
            try
            {
                if (isFromStart)
                {
                    StartSessionOpacity = ClassFromDateTime.ToString("tt") == "AM" ? 1 : 0.7;
                    EndSessionOpacity = ClassFromDateTime.ToString("tt") == "PM" ? 1 : 0.7;
                }
                else
                {
                    StartSessionOpacity = ClassToDateTime.ToString("tt") == "AM" ? 1 : 0.7;
                    EndSessionOpacity = ClassToDateTime.ToString("tt") == "PM" ? 1 : 0.7;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #region recurring class 

        private List<DateTime> GetRecurringClassDatesList()
        {
            try
            {
                RecurringFromDateTime = DateTime.Parse(RecurringFromDateTime.ToString("yyyy-MM-dd") + " " + ClassFromDateTime.ToString("hh:mm tt"));

                DateTime classStartDateTime = DateTime.Parse(ClassFromDateTime.ToString("yyyy-MM-dd") + " " + ClassFromDateTime.ToString("hh:mm tt")); // this is to equalate two date times format

                switch ((RecurranceClassFrequencyType)Enum.Parse(typeof(RecurranceClassFrequencyType), Frequency))
                {
                    case RecurranceClassFrequencyType.Alternatedays:
                        return CustomDatesFrequency.GetAlternateDates(classStartDateTime, RecurringFromDateTime);

                    case RecurranceClassFrequencyType.Daily:
                        return CustomDatesFrequency.GetDailyDates(classStartDateTime, RecurringFromDateTime);

                    case RecurranceClassFrequencyType.Weekly:
                        return CustomDatesFrequency.GetWeekDates(classStartDateTime, RecurringFromDateTime);

                    case RecurranceClassFrequencyType.Monthly:
                        return CustomDatesFrequency.GetMonthlyDates(classStartDateTime, RecurringFromDateTime);

                    case RecurranceClassFrequencyType.Yearly:
                        return CustomDatesFrequency.GetYearlyDates(classStartDateTime, RecurringFromDateTime);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        private List<MultiClass> _multipleClassList = new List<MultiClass>();

        public void canv_add_to_multi_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ClassFromDateTime.Date < DateTime.Today)
                {
                    Messenger.Default.Send("You can't create Class in the Past time..!", "Notification");
                }
                else if (ClassFromDateTime > ClassToDateTime)
                {
                    Messenger.Default.Send("Class 'StartTime' is More than 'EndTime'", "Notification");
                }
                else
                {
                    if (MultiClassList == null || _multipleClassList == null)
                    {
                        if (_multipleClassList == null)
                            _multipleClassList = new List<MultiClass>();
                    }
                    else if (MultiClassList != null && MultiClassList.Count > 0 && !MultiClassList.Any(s => s.from_date_time.ToString("dd:MM:yyyy") == ClassFromDateTime.ToString("dd:MM:yyyy")))
                    {
                        _multipleClassList = new List<MultiClass>();
                    }

                    if (!_multipleClassList.Any(s => (s.from_date_time <= ClassFromDateTime && s.to_date_time >= ClassFromDateTime) || (s.from_date_time <= ClassToDateTime && s.to_date_time >= ClassToDateTime) || (s.from_date_time <= ClassFromDateTime && s.to_date_time >= ClassToDateTime) || (s.from_date_time >= ClassFromDateTime && s.from_date_time <= ClassToDateTime && s.to_date_time >= ClassFromDateTime && s.to_date_time <= ClassToDateTime)))
                    {
                        _multipleClassList.Add(new MultiClass() { from_date_time = ClassFromDateTime, to_date_time = ClassToDateTime });

                        MultiClassList = null;
                        MultiClassList = _multipleClassList;
                    }
                    else
                    {
                        Messenger.Default.Send("This time slot is already in use, Please select Different time slot", "Notification");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_remove_multi_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if ((sender as Canvas).Tag != null)
                {
                    _multipleClassList.Remove((sender as Canvas).Tag as MultiClass);

                    MultiClassList = null;
                    MultiClassList = _multipleClassList;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion recurring class 

        #region Conflict Class

        public void canv_conflict_next_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollNext(_newClassView.totalClassList, _newClassView.canv_conflict_prev, _newClassView.canv_conflict_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_conflict_prev_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollPrevious(_newClassView.totalClassList, _newClassView.canv_conflict_prev, _newClassView.canv_conflict_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void txt_re_schedule_this_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _newClassView.canv_recursive_class_reschedule.Visibility = Visibility.Visible;
        }

        #endregion Conflict Class

        #endregion

        #region Step 2

        List<Employees> _resultContactList = null;
        List<Participants> _invitedParticipantsList = new List<Participants>();
        Canvas _canvOrganizer = null;
        int _organizerIndex = -1;
        bool isFromExisted = false;

        /// <summary>
        /// To validate Step 2
        /// </summary>
        /// <returns></returns>
        private bool ValidateStep2()
        {
            try
            {
                if (_invitedParticipantsList == null || _invitedParticipantsList.Count == 0)
                {
                    Messenger.Default.Send("Please invite at least one Class Room to go further", "Notification");
                    return false;
                }

                if (!_invitedParticipantsList.Any(s => s.IsOrganizer))
                {
                    Messenger.Default.Send("Please select some one as organizer", "Notification");
                    return false;
                }

                HideAllSteps();
                _newClassView.lb_confirmed_participants.ItemsSource = null;
                _newClassView.lb_confirmed_participants.ItemsSource = _invitedParticipantsList;

                _newClassView.lb_agenda_presenter.ItemsSource = null;
                _newClassView.lb_agenda_presenter.ItemsSource = _invitedParticipantsList;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return true;
        }

        /// <summary>
        /// to show step 2
        /// </summary>
        private void GoToStep2()
        {
            try
            {
                ClosePopupAnimaton();
                HideAllSteps();

                _newClassView.canv_inviteParticipants.Visibility = Visibility.Visible;
                _newClassView.txt_add_contact.Text = "";
                _newClassView.txt_from_date.IsHitTestVisible = true;
                _isFromConflictResolve = false;



                if (!isFromExisted || _isFromReschedule)
                    BindInvitedParticipants();
                _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
                _newClassView.path_2.Opacity = 0.8;
                _newClassView.wp_status.Visibility = Visibility.Visible;
                _newClassView.canv_nextstep.Visibility = Visibility.Visible;
                EnableHitTestVisible(_newClassView.canv_ellipse_2);
                _currentEllipseIndex = 1;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// filter method depends on given text
        /// </summary>
        /// <param name="searchText"></param>
        void SearchedContacts(string searchText)
        {
            try
            {
                _newClassView.lb_contacts.ItemsSource = null;
                if (searchText.Length > 0 && HomePageViewModel._contactsDbList != null && HomePageViewModel._contactsDbList.Count > 0 && _newClassView.lb_alphabets.SelectedIndex != 0 && searchText.ToLower() != "all")
                {
                    _resultContactList = HomePageViewModel._contactsDbList.Where(x => x.Name.ToLower().StartsWith(searchText.ToLower()) || x.Email.ToLower().StartsWith(searchText.ToLower())).ToList();

                    if (_resultContactList.Count > 0)
                    {
                        _newClassView.lb_contacts.ItemsSource = _resultContactList;
                    }
                }
                else if (HomePageViewModel._contactsDbList != null && HomePageViewModel._contactsDbList.Count > 0)
                {
                    _newClassView.lb_contacts.ItemsSource = HomePageViewModel._contactsDbList;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To add new contact
        /// </summary>
        void AddNewContact()
        {
            try
            {
                string newContactEmail = _newClassView.txt_add_contact.Text.Trim().ToLower();

                Participants addParticipant = null;

                if (string.IsNullOrWhiteSpace(newContactEmail))
                {
                    Messenger.Default.Send("Email should not be empty to invite the Class room", "Notification");
                }
                else if (!NxgUtilities.IsValidEmail(newContactEmail))
                {
                    Messenger.Default.Send("A valid email is required to invite the Class room", "Notification");
                }
                else
                {
                    if (_invitedParticipantsList.Any(s => s.Employee.Email.Trim().ToLower() == newContactEmail))
                    {
                        Messenger.Default.Send("This Class room is already added!", "Notification");
                        return;
                    }
                    else if (HomePageViewModel._contactsDbList.Any(s => s.Email.Trim().ToLower() == newContactEmail))
                    {
                        Employees participant = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == newContactEmail);
                        if (participant != null)
                            addParticipant = new Participants { EmployeeId = participant.EmployeeId, Employee = participant };
                    }
                    else
                    {
                        Employees emp = new Employees { Email = newContactEmail, FirstName = newContactEmail.Substring(0, newContactEmail.IndexOf("@")) };
                        int id = Service.InsertOrUpdateDataToDB(emp, CrudActions.Create);
                        if (id > -1)
                        {
                            emp.EmployeeId = id;
                            HomePageViewModel._contactsDbList.Add(emp);
                            _newClassView.lb_contacts.ItemsSource = null;
                            _newClassView.lb_contacts.ItemsSource = HomePageViewModel._contactsDbList;
                            addParticipant = new Participants { EmployeeId = emp.EmployeeId, Employee = emp };
                        }
                    }

                    if (addParticipant != null)
                    {
                        _invitedParticipantsList.Add(addParticipant);
                        _newClassView.lb_invited_participants.Items.Insert(0, addParticipant);
                        _newClassView.txt_add_contact.Text = "";

                        if (_invitedParticipantsList.Count == 1)
                            SelectDefaultOrganiser(0);
                    }
                    _newClassView.canv_drophere_icon.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Wheather participant optional or not 
        /// </summary>
        /// <param name="canv"></param>
        void Participant_optional(Canvas canv)
        {
            try
            {
                List<Participants> participantsList = _newClassView.lb_invited_participants.Items.Cast<Participants>().ToList();

                if (_canvOrganizer != null && canv.Name == "canv_organizer")
                {
                    if (participantsList != null && participantsList.FirstOrDefault(s => s.EmployeeId == _organizerIndex) != null)
                        participantsList.FirstOrDefault(s => s.EmployeeId == _organizerIndex).IsOrganizer = false;
                    (_canvOrganizer.Children[7] as Canvas).Opacity = 0.2;
                    _canvOrganizer = null;
                }

                Canvas canvParticipantOptionalSelection = canv.Parent as Canvas;
                string canvParentName = Convert.ToString(canvParticipantOptionalSelection.Tag);
                Participants emp = participantsList.FirstOrDefault(s => s.Employee != null && s.Employee.Email == canvParentName);
                if (emp != null)
                {
                    int participantIndex = participantsList.IndexOf(emp);
                    if (participantIndex > -1)
                    {
                        if (canv.Name == "canv_remove_contact")
                        {
                            if (!_agendaList.ToList().Any(s => s.EmployeeId == emp.ParticipantId))
                            {
                                _invitedParticipantsList.RemoveAt((_invitedParticipantsList.Count - 1) - participantIndex);

                                _newClassView.lb_invited_participants.Items.RemoveAt(participantIndex);
                                (canvParticipantOptionalSelection.Children[5] as Slider).Value = 0;

                                if (_newClassView.lb_invited_participants.Items.Count == 0)
                                    _newClassView.canv_drophere_icon.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                Messenger.Default.Send("Sorry can't delete due to Class room is presented in Class plan list.", "Notification");
                            }
                        }
                        else if (canv.Name == "canv_organizer")
                        {
                            _organizerIndex = emp.EmployeeId;
                            _canvOrganizer = canvParticipantOptionalSelection;

                            emp.IsOrganizer = true;
                            (_canvOrganizer.Children[7] as Canvas).Opacity = 1;
                            Recognizedemail = emp.Employee != null ? emp.Employee.Email : "";
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(canvParentName))
                            {
                                if (canvParticipantOptionalSelection != null)
                                {
                                    if (Convert.ToString(canv.Tag) == "no")
                                    {
                                        participantsList[participantIndex].IsOptional = true;
                                        (canvParticipantOptionalSelection.Children[4] as Canvas).Visibility = Visibility.Visible;
                                        (canvParticipantOptionalSelection.Children[3] as Canvas).Visibility = Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        participantsList[participantIndex].IsOptional = false;
                                        (canvParticipantOptionalSelection.Children[4] as Canvas).Visibility = Visibility.Collapsed;
                                        (canvParticipantOptionalSelection.Children[3] as Canvas).Visibility = Visibility.Visible;
                                    }
                                }
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

        private void SelectDefaultOrganiser(int index)
        {
            try
            {
                ContentPresenter content = TemplateModifier.GetContentPresenter(_newClassView.lb_invited_participants, index);
                if (content != null)
                {
                    Canvas canv_organizer = content.ContentTemplate.FindName("canv_organizer", content) as Canvas;
                    if (canv_organizer != null)
                    {
                        Participant_optional(canv_organizer);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// alphabet listbox selection changed event to filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void lb_alphabets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_newClassView.lb_alphabets.SelectedIndex != -1)
                {
                    SearchedContacts(_newClassView.lb_alphabets.SelectedItem.ToString());
                    _newClassView.txt_search_contact.Text = "";
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// search box keydown event to filter contacts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_search_contact_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_newClassView.txt_search_contact.Text) && _newClassView.lb_alphabets.SelectedItem != null && _newClassView.txt_search_contact.Text != _newClassView.lb_alphabets.SelectedItem.ToString())
                {
                    _newClassView.lb_alphabets.SelectedIndex = -1;
                }
                SearchedContacts(_newClassView.txt_search_contact.Text);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// to clear text in search box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_clear_contact_textbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _newClassView.lb_alphabets.SelectedIndex = 0;
        }

        /// <summary>
        /// Optional yes/No slider value changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void slider_yes_no_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                double value = (sender as Slider).Value;

                if (value == 0)
                {
                    SelectOptionalContact_MouseDown(((sender as Slider).Parent as Canvas).Children[4] as Canvas, null);
                }
                else if (value == 0.1)
                {
                    SelectOptionalContact_MouseDown(((sender as Slider).Parent as Canvas).Children[3] as Canvas, null);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Select Whether Optional or not by clicking on canvas.
        /// 
        /// Event from Contacts Model class
        /// </summary>
        /// <param name="canv"></param>
        /// 
        public void SelectOptionalContact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Participant_optional(sender as Canvas);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Mouse down event to + Icon to add contact
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_addcontact_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                AddNewContact();
                HomePageViewModel._contactsDbList = Service.GetModuleDataList<Employees>(null);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Event if Enter Key pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_add_contact_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    canv_addcontact_MouseDown(null, null);
                    return;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// open keyboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void txt_add_contact_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            NxgUtilities.StartTouchKeyboard(Constants.TouchKeyboard);
        }

        #endregion Step2

        #region Step 3

        private bool ValidateStep3()
        {
            try
            {
                ClassTitle = _newClassView.txt_class_title.Text.Trim();
                if (ClassTitle == null || ClassTitle.Length == 0)
                    Messenger.Default.Send("Need a title to the current Class for further reference", "Notification");
                else
                {
                    HideAllSteps();
                    return true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return false;
        }

        private void GoToStep3()
        {
            try
            {
                _newClassView.canv_classtitle.Visibility = Visibility.Visible;

                _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
                _newClassView.path_3.Opacity = 0.8;

                _newClassView.wp_status.Visibility = Visibility.Visible;
                _newClassView.canv_nextstep.Visibility = Visibility.Visible;

                grid_inputoptions_MouseDown(_newClassView.grid_keyboard, null);

                EnableHitTestVisible(_newClassView.canv_ellipse_3);

                _currentEllipseIndex = 2;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void ResetInputOptions()
        {
            _newClassView.img_ink_selected.Visibility = _newClassView.img_keyboard_selected.Visibility = _newClassView.img_voice_selected.Visibility = Visibility.Collapsed;
            _newClassView.shape_select_ink.Fill = _newClassView.shape_select_keyboard.Fill = _newClassView.shape_select_voice.Fill = Brushes.White;

            _newClassView.txt_class_title.Visibility = _newClassView.inkcanv_class_title.Visibility = _newClassView.canv_clear.Visibility = Visibility.Collapsed;
        }

        public void txt_class_title_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    canv_nextstep_MouseDown(null, null);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// input options to select hand or keyboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void grid_inputoptions_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ResetInputOptions();

                Grid senderGrid = sender as Grid;
                (senderGrid.Children[0] as Image).Visibility = Visibility.Visible;
                (senderGrid.Children[1] as Path).Fill = Brushes.Black;

                if (senderGrid.Name == "grid_keyboard")
                {
                    string recognizedText = string.Empty;

                    if (_newClassView.inkcanv_class_title.Strokes.Count > 0)
                        recognizedText = RecognizeStrokes.RecognizeText(_newClassView.inkcanv_class_title, null);

                    GotoKeyBoardClassTitle(recognizedText);

                    //_newMeetingView.inkcanv_meeting_title.Strokes.Clear();

                    //MeetingBoardViewModel.StartTouchKeyboard();
                    //_newMeetingView.txt_newmeeting_name.Focus();
                    //Keyboard.Focus(_newMeetingView.txt_newmeeting_name);
                    _newClassView.txt_class_title.CaretIndex = _newClassView.txt_class_title.Text.Length;
                }
                else if (senderGrid.Name == "grid_ink")
                {
                    _newClassView.txt_class_title.Visibility = Visibility.Collapsed;
                    _newClassView.inkcanv_class_title.Visibility = Visibility.Visible;
                    _newClassView.canv_clear.Visibility = Visibility.Visible;
                    //_newMeetingView.txt_newmeeting_name.Focus();
                    //Keyboard.Focus(_newMeetingView.txt_newmeeting_name);
                    //MeetingBoardViewModel.StopTouchKeyBoard();
                }

                else if (senderGrid.Name == "grid_voice")
                {
                    _newClassView.txt_class_title.Visibility = Visibility.Collapsed;
                    _newClassView.inkcanv_class_title.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void GotoKeyBoardClassTitle(string text)
        {
            ResetInputOptions();

            Grid senderGrid = _newClassView.grid_keyboard;
            (senderGrid.Children[0] as Image).Visibility = Visibility.Visible;
            (senderGrid.Children[1] as Path).Fill = Brushes.Black;

            _newClassView.txt_class_title.Visibility = Visibility.Visible;
            _newClassView.inkcanv_class_title.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(text))
            {
                _newClassView.txt_class_title.Text = text;
            }
        }

        /// <summary>
        /// To clear class title ink
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_clear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _newClassView.inkcanv_class_title.Strokes.Clear();
        }

        public void InvitedParticipant_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas selectedItem = sender as Canvas;
                if (selectedItem != null && isFromUpdate && !string.IsNullOrWhiteSpace(Convert.ToString(selectedItem.Tag)) && Convert.ToInt32(_invitedParticipantsList.FirstOrDefault(s => s.Employee.Email == Convert.ToString(selectedItem.Tag)).IsOptional) == 1)
                {
                    (selectedItem.Children[5] as Slider).Value = 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Step 3

        #region Step 4

        private bool isFromUpdate = false;
        private bool _isPasswordRequired = false;

        private void GoToStep4()
        {
            try
            {
                //if (_remainingDuration < 0)
                //{
                //    Messenger.Default.Send("Please select Class plan presenter.", "Notification");
                //    return;
                //}

                _newClassView.canv_class_confirmation.Visibility = Visibility.Visible;

                _newClassView.canv_nextstep.Visibility = Visibility.Collapsed;

                _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
                _newClassView.path_5.Opacity = 0.8;

                _newClassView.wp_status.Visibility = Visibility.Collapsed;
                _newClassView.canv_nextstep.Visibility = Visibility.Collapsed;

                EnableHitTestVisible(_newClassView.canv_ellipse_5);

                _currentEllipseIndex = 4;

                LoadConfirmedClassList();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        ///  refine to move to 1st step
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_refine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isFromExisted = true;
                slider_confirmation_ValueChanged(new Slider() { Value = 0 }, null);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// confirm option mouse down to enter user email address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_confirm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //if (_meetingDetails != null && NxgUtilities.IsValidEmail(_meetingDetails.OrganizerMailId))
                //{
                _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = false;
                App.ExecuteMethod(() =>
                {
                    isFromExisted = false;
                    ConflictClassList = conflictClassList = null;
                    ValidateEmail(Recognizedemail);
                    _recurringClassDatesList = null;
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _newClassView.txt_from_date.IsHitTestVisible = false;
                        _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = true;
                    });
                }, true);
                //}
                //else
                //{
                //    _newMeetingView.txt_email.Text = _organizerMailId;
                //    slider_confirmation_ValueChanged(new Slider() { Value = 2 }, null);
                //}
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// To Refine or Confirm Class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        public void slider_confirmation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                double value = (sender as Slider).Value;

                if (isFromUpdate && value == 2.0)
                {
                    _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = false;
                    App.ExecuteMethod(() =>
                    {
                        ValidateEmail(Recognizedemail);
                    }, true);
                    _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = true;
                    return;
                }

                if (value == 0.0 && _newClassView.canv_class_confirmation.Visibility == Visibility.Visible)
                {
                    HideAllSteps();

                    _newClassView.slider_confirmation.UpdateLayout();
                    _newClassView.slider_confirmation.Value = 1.0;

                    GoToStep1();

                    //if (!isFromUpdate)
                    //    _newMeetingView.canv_meetingtype.Visibility = Visibility.Visible;
                    //_newMeetingView.canv_meeting_period.Visibility = Visibility.Collapsed;
                    //_newMeetingView.multiple_meetings_Type.Visibility = Visibility.Collapsed;
                    //if (MeetingType == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingScheduleType.RecurringMeeting.ToString()))
                    //{
                    //    _newMeetingView.canv_meeting_period.Visibility = Visibility.Visible;
                    //}
                    //else if (MeetingType == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingScheduleType.SingleDayMultipleMeetings.ToString()))
                    //{
                    //    _newMeetingView.listbox_multiple_meetings_type.ItemsSource = null;
                    //    _newMeetingView.listbox_multiple_meetings_type.ItemsSource = RecurringMeetingsList;
                    //    _newMeetingView.multiple_meetings_Type.Visibility = Visibility.Visible;
                    //}
                }
                else if (value == 2.0 && _newClassView.canv_class_confirmation.Visibility == Visibility.Visible)
                {
                    _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = false;
                    App.ExecuteMethod(() =>
                    {
                        ValidateEmail(Recognizedemail);
                    }, true);
                    _newClassView.slider_confirmation.IsHitTestVisible = _newClassView.canv_confirm.IsHitTestVisible = true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void tbk_view_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _newClassView.canv_confirmed_class.Visibility = Visibility.Visible;

                _newClassView.grid_date_time.Visibility = Visibility.Collapsed;
                _newClassView.grid_class_type.Visibility = Visibility.Collapsed;
                _newClassView.canv_switch_refine_confirm.Visibility = Visibility.Collapsed;

                LoadConfirmedClassList();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void LoadConfirmedClassList()
        {
            try
            {
                if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.RecurringClass.ToString()) && _recurringClassDatesList != null && _recurringClassDatesList.Count > 0)
                {
                    _newClassView.lb_confirmed_class.ItemsSource = null;
                    _newClassView.lb_confirmed_class.ItemsSource = _recurringClassDatesList;
                }
                else if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.SingleDayMultipleClass.ToString()) && MultiClassList != null && MultiClassList.Count > 0)
                {
                    _newClassView.lb_confirmed_class.ItemsSource = null;
                    _newClassView.lb_confirmed_class.ItemsSource = MultiClassList;
                }

                _newClassView.canv_confirmed_class_next.Visibility = Visibility.Hidden;
                _newClassView.canv_confirmed_class_prev.Visibility = Visibility.Hidden;
                if (_newClassView.lb_confirmed_class != null && _newClassView.lb_confirmed_class.Items.Count > 1)
                {
                    _newClassView.lb_confirmed_class.SelectedIndex = 0;
                    _newClassView.lb_confirmed_class.ScrollIntoView(_newClassView.lb_confirmed_class.Items[0]);
                    _newClassView.canv_confirmed_class_next.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_confirmed_class_prev_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollPrevious(_newClassView.lb_confirmed_class, _newClassView.lb_confirmed_class.SelectedIndex, 1, _newClassView.canv_confirmed_class_prev, _newClassView.canv_confirmed_class_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_confirmed_class_next_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                NxgScrollItems.ScrollNext(_newClassView.lb_confirmed_class, _newClassView.lb_confirmed_class.SelectedIndex, 1, _newClassView.canv_confirmed_class_prev, _newClassView.canv_confirmed_class_next);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_close_confirmed_class_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _newClassView.canv_confirmed_class.Visibility = Visibility.Collapsed;

            _newClassView.grid_date_time.Visibility = Visibility.Visible;
            _newClassView.grid_class_type.Visibility = Visibility.Visible;
            _newClassView.canv_switch_refine_confirm.Visibility = Visibility.Visible;
        }

        public void password_req_yes_no_MouseDown(object sender, MouseButtonEventArgs touchEventArgs)
        {
            try
            {
                if (!_newClassView.password_req_no.IsVisible)
                {
                    _isPasswordRequired = false;
                }
                else
                {
                    _isPasswordRequired = true;
                }
                _newClassView.password_req_yes.Visibility = _newClassView.password_req_yes.IsVisible ? Visibility.Collapsed : Visibility.Visible;
                _newClassView.password_req_no.Visibility = _newClassView.password_req_no.IsVisible ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Email

        string Recognizedemail = "";

        /// to validate email address and store all values 
        /// </summary>
        /// <param name="emailid"></param>
        private void ValidateEmail(string emailid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailid) || emailid.Length == 0)
                {
                    //_newMeetingView.canv_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Messenger.Default.Send("A valid Email is required to invite send the information", "Notification");
                    });
                }
                else
                {
                    emailid = emailid.Trim().ToLower();
                    if (NxgUtilities.IsValidEmail(emailid))
                    {

                        int contactId = -1;
                        if (!HomePageViewModel._contactsDbList.Any(s => s.Email == emailid))
                        {
                            Employees participant = new Employees()
                            {
                                Email = emailid,
                                FirstName = emailid.Substring(0, emailid.IndexOf("@"))
                            };
                            contactId = Service.InsertOrUpdateDataToDB(participant, CrudActions.Create);
                        }
                        else
                        {
                            contactId = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == emailid).EmployeeId;
                        }

                        string passcode = !_isPasswordRequired ? "" : _classDetails != null ? _classDetails.Password : NxgUtilities.GetRandomPassword(6);

                        int selectedClassDbId = -1;
                        bool isEmailSent = false;

                        if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.OneTimeClass.ToString()) || _isFromReschedule)
                        {
                            isEmailSent = InsertClassData(ClassTitle, ClassFromDateTime, ClassToDateTime, ClassCategory, ClassType, passcode, emailid, out selectedClassDbId, RecurringClassId: _classDetails != null ? _classDetails.RecurringClassId : null);
                        }
                        else
                        {
                            string recurringClassId = Guid.NewGuid().ToString();
                            List<MultiClass> finalClassDatesList = null;

                            if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.RecurringClass.ToString()) && _recurringClassDatesList != null && _recurringClassDatesList.Count > 0)
                            {
                                finalClassDatesList = _recurringClassDatesList;
                            }
                            else if (ClassType == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassScheduleType.SingleDayMultipleClass.ToString()) && MultiClassList != null && MultiClassList.Count > 0)
                            {
                                finalClassDatesList = MultiClassList;
                            }

                            if (finalClassDatesList != null && finalClassDatesList.Count > 0)
                            {
                                foreach (var finalClassDate in finalClassDatesList)
                                {
                                    if (finalClassDatesList.IndexOf(finalClassDate) == 11)
                                    {
                                        Messenger.Default.Send("Class count exceeded 10, First 10 Classes will be created", "Notification");
                                        break;
                                    }

                                    bool isMeeetingCreated = InsertClassData(ClassTitle, finalClassDate.from_date_time, finalClassDate.to_date_time, ClassCategory, ClassType, passcode, emailid, out selectedClassDbId, Frequency, RecurringFromDateTime, RecurringToDateTime, recurringClassId);

                                    if (finalClassDatesList[0].from_date_time == finalClassDate.from_date_time && finalClassDatesList[0].to_date_time == finalClassDate.to_date_time)
                                        isEmailSent = isMeeetingCreated;
                                }
                            }
                        }

                        if (isEmailSent)
                        {
                            bool mailSent = GenerateEmail(ClassTitle, passcode, ClassFromDateTime.ToString("yyyy-MM-dd"), ClassFromDateTime.ToString("hh:mm tt"), ClassToDateTime.ToString("hh:mm tt"), Constants.LocationName, _invitedParticipantsList, _agendaList, emailid, selectedClassDbId, _classDetails != null ? "updated" : "created");
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (mailSent)
                                    Messenger.Default.Send("Your new Class added to the calendar. Refer the information in the email sent for further instructions", "Notification");
                                else
                                    Messenger.Default.Send("Your new Class added to the calendar but Email not sent.", "Notification");
                            });

                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                HideAllSteps();
                                _newClassView.canv_newclass.Visibility = Visibility.Visible;
                                _invitedParticipantsList.Clear();
                                isFromUpdate = false;

                                _isFromCarousel = true;
                                CarouselViewModel._selectedClassId = selectedClassDbId;
                                CarouselViewModel._selectedDate = ClassFromDateTime.ToString("yyyy-MM-dd");

                                Messenger.Default.Send("canv_show_calendar", "ShowContentControl");
                            });
                            Messenger.Default.Unregister(this);
                            _classDetails = null;
                        }
                        else
                        {
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Messenger.Default.Send("Oops..!, something went wrong. We will get it back working as soon as possible.", "Notification");
                            });
                        }
                    }
                    else
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Messenger.Default.Send("A valid email is required to invite send the information", "Notification");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private bool InsertClassData(string ClassTitle, DateTime ClassFromDateTime, DateTime ClassToDateTime, string ClassCategory, string ClassType, string passcode, string emailid, out int insertedId, string ClassFrequency = null, DateTime FrequencyStartTime = default(DateTime), DateTime FrequencyEndTime = default(DateTime), string RecurringClassId = null)
        {
            try
            {
                int classDbId = 0;

                Class oneTimeClassDetails = new Class
                {
                    ClassName = ClassTitle,
                    StartTime = ClassFromDateTime,
                    EndTime = ClassToDateTime,
                    ClassCategoryId = (int)(ClassCategoryType)Enum.Parse(typeof(ClassCategoryType), ClassCategory.Replace(" & ", "And").Replace(" ", "")),
                    ClassCategory = ClassCategory,
                    ClassTypeId = (int)(ClassScheduleType)Enum.Parse(typeof(ClassScheduleType), ClassType.Replace(" ", "")),
                    ClassType = ClassType,
                    Password = passcode,
                    OrganizerMailId = emailid,
                    UniqueClassId = "NXG" + NxgUtilities.GetRandomPassword(6)
                };

                if (ClassFrequency != null || !string.IsNullOrWhiteSpace(RecurringClassId))
                {
                    oneTimeClassDetails.ClassFrequency = Frequency;
                    oneTimeClassDetails.FrequencyStartTime = RecurringFromDateTime;
                    oneTimeClassDetails.FrequencyEndTime = RecurringToDateTime;
                    oneTimeClassDetails.RecurringClassId = RecurringClassId;
                }

                if (_classDetails != null)
                {
                    classDbId = oneTimeClassDetails.ClassId = _classDetails.ClassId;
                    Service.InsertOrUpdateDataToDB(oneTimeClassDetails, CrudActions.Update);
                }
                else
                {
                    classDbId = Service.InsertOrUpdateDataToDB(oneTimeClassDetails, CrudActions.Create);
                    oneTimeClassDetails.ClassId = classDbId;
                }

                string participants = string.Empty;
                _invitedParticipantsList.ForEach(s => participants += s.Employee.Email + ", ");
                if (!string.IsNullOrWhiteSpace(participants))
                {
                    participants = participants.Remove(participants.LastIndexOf(','));
                }

                Class details = Service.GetClassList(ClassFromDateTime, true).FirstOrDefault(s => s.ClassId == oneTimeClassDetails.ClassId);

                if (details != null && _invitedParticipantsList.Count > 0)
                {
                    if (_classDetails != null)
                    {
                        _classDetails.ParticipantList.ForEach(emp => Service.InsertOrUpdateDataToDB(emp, CrudActions.Delete, emp.ParticipantId));
                    }

                    _invitedParticipantsList.ForEach(s => { s.ClassId = classDbId; Service.InsertOrUpdateDataToDB(s, CrudActions.Create); });
                }

                if (_classDetails != null && _classDetails.AgendaList != null && _classDetails.AgendaList.Count > 0)
                {
                    _classDetails.AgendaList.ForEach(a => Service.InsertOrUpdateDataToDB(a, CrudActions.Delete, a.AgendaId));
                }

                if (details != null)
                {
                    for (int i = 0; i < _agendaList.Count; i++)
                    {
                        _agendaList[i].ClassId = details.ClassId;
                        _agendaList[i].StartTime = i == 0 ? ClassFromDateTime : _agendaList[i - 1].EndTime;
                        _agendaList[i].EndTime = _agendaList[i].StartTime.AddMinutes(Convert.ToInt32(_agendaList[i].AgendaDuration));

                        Service.InsertOrUpdateDataToDB(_agendaList[i], CrudActions.Create);
                    }
                }

                insertedId = classDbId;
                return true;
            }
            catch (Exception ex)
            {
                ex.InsertException();
            }
            insertedId = -1;
            return false;
        }

        #region Generate Email

        /// <summary>
        /// generate email
        /// </summary>
        /// <param name="classTitle"></param>
        /// <param name="passcode"></param>
        /// <param name="fromdate"></param>
        /// <param name="fromTime"></param>
        /// <param name="toTime"></param>
        /// <param name="location"></param>
        /// <param name="participants"></param>
        /// <param name="emailId"></param>
        public static bool GenerateEmail(string classTitle, string passcode, string fromdate, string fromTime, string toTime, string location, List<Participants> participants, List<Agendas> agendas, string emailId, int classId, string createdorupdated = "created")
        {
            bool mailStatus = false;
            try
            {
                DateTime dateTime = DateTime.ParseExact(fromdate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                string subject = classTitle + " on " + dateTime.Day.ToString() + " " + dateTime.ToString("MMM") + " " + fromTime;

                string classHeader = createdorupdated == "accessed" ? "Thanks for using Wall X." : "Your class " + createdorupdated + " successfully.";

                string passcodeInfo = (createdorupdated == "accessed" ? "Use this <b> Passcode </b> to revisit your class : <b>" : "Use this <b> Passcode </b> to access : <b>") + passcode;

                string body = classHeader + (string.IsNullOrWhiteSpace(passcode) ? "" : " " + passcodeInfo) + "</b><br/><br/>";

                //string body = meetingHeader + " " + passcodeInfo + "</b><br/><br/>" +

                //    "<b>" + meetingTitle + "</b>" + "<br/><br/>" +

                //            "<b>" + "When" + "</b>" + "<br/>" +
                //             dateTime.ToString("ddd") + " " + dateTime.ToString("MMM") + " " + dateTime.Day.ToString() + ", " + dateTime.Year.ToString() + " " + fromTime + "–" + toTime + " " + "IST" + "<br/><br/>" +

                //             "<b>" + "Where" + "</b>" + "<br/>" +
                //             location + "<br/><br/>";

                //if (participants != null && participants.Count > 0)
                //{
                //    body += "<b>" + "Who" + "</b>" + "<br/>";
                //    participants.ForEach(s => body += s.Employee.Name + " &lt;" + s.Employee.Email + "&gt;" + "<br/>");
                //}                

                //List <KeyValuePair<string, KeyValuePair<DateTime, DateTime>>> icalAgendaData = new List<KeyValuePair<string, KeyValuePair<DateTime, DateTime>>>();

                if (agendas != null && agendas.Count > 0)
                {
                    body += "<b>" + "Agenda" + "</b>" + "<br/>";

                    //agendas.ForEach(s => body += s.StartTime + " - " + s.EndTime + " " + s.AgendaName + " by " + " &lt;" + participants.FirstOrDefault(k => k.EmployeePKID == s.EmployeePKID).Employee.Email + "&gt;" + "<br/>"); // pending by sat

                    agendas.ForEach(s => body += s.StartTime.ToString("hh:mm tt") + " - " + s.EndTime.ToString("hh:mm tt") + " " + s.AgendaName + " by " + " &lt;" + s.EmployeeEmail + "&gt;" + "<br/>");

                    //foreach (Agendas agenda in agendas)
                    //{
                    //    body += agenda.StartTime + " - " + agenda.EndTime + " " + agenda.AgendaName + " by " + " &lt;" + agenda.EmployeeEmail + "&gt;" + "<br/>";

                    //    icalAgendaData.Add(new KeyValuePair<string, KeyValuePair<DateTime, DateTime>>(agenda.AgendaName, new KeyValuePair<DateTime, DateTime>(agenda.StartTime, agenda.EndTime)));
                    //}
                }

                DateTime fromdateTime = DateTime.ParseExact(fromdate + " " + fromTime, "yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);
                DateTime todateTime = DateTime.ParseExact(fromdate + " " + toTime, "yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);

                List<KeyValuePair<string, object>> icalViewData = new List<KeyValuePair<string, object>>();
                icalViewData.Add(new KeyValuePair<string, object>("StartTime", fromdateTime.ToUniversalTime()));
                icalViewData.Add(new KeyValuePair<string, object>("EndTime", todateTime.ToUniversalTime()));
                icalViewData.Add(new KeyValuePair<string, object>("Location", location));

                mailStatus = EMailer.SendEmailToClient(Constants.UserName, Constants.Password, (participants != null && participants.Count > 0) ? string.Join(",", participants.Where(s => s.Employee != null).Select(s => s.Employee.Email)) : emailId, subject, body, null, icalViewData, null);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return mailStatus;
        }

        #endregion Generate Email

        #endregion Email

        #region Next Step Button

        public void canv_nextstep_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_newClassView.canv_newclass.IsVisible)
                {
                    if (ValidateStep1())
                    {
                        if (ConflictClassList != null && ConflictClassList.Count > 0)
                        {
                            ClassRescheduleList = Enum.GetNames(typeof(ClassRescheduleTypes)).Select(s => NxgUtilities.GetStringUpperCharwithAddedSpace(s)).ToList();
                            _newClassView.totalClassList.SelectedIndex = 0;
                            _newClassView.canv_total_class.Visibility = Visibility.Visible;
                            _newClassView.canv_recursive_class_reschedule.Visibility = _newClassView.canv_conflict_next.Visibility = _newClassView.canv_conflict_prev.Visibility = Visibility.Hidden;

                            _newClassView.totalClassList.SelectedIndex = 0;
                            _newClassView.totalClassList.ScrollIntoView(_newClassView.totalClassList.Items[0]);

                            if (ConflictClassList.Count > 1)
                                _newClassView.canv_conflict_next.Visibility = Visibility.Visible;
                        }
                        else
                            GoToStep2();
                    }
                }
                else if (_newClassView.canv_total_class.IsVisible)
                {
                    //_newMeetingView.canv_singleday_multi_meetings.Visibility = _newMeetingView.canv_total_meetings.Visibility = Visibility.Collapsed;

                    if (ClassReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassRescheduleTypes.RescheduleThisClass.ToString()))
                    {
                        if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                        {
                            ClassFromDateTime = _recurringClassDatesList.FirstOrDefault(s => s.from_date_time.ToString("dd/MM/yyyy") == ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassStartTime.ToString("dd/MM/yyyy")).from_date_time;
                            ClassToDateTime = _recurringClassDatesList.FirstOrDefault(s => s.to_date_time.ToString("dd/MM/yyyy") == ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassEndTime.ToString("dd/MM/yyyy")).to_date_time;

                            RecurringFromDateTime = ClassFromDateTime;
                        }
                        else if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                        {
                            ClassFromDateTime = ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassStartTime;
                            ClassToDateTime = ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassEndTime;

                            _multipleClassList.Remove(_multipleClassList.FirstOrDefault(s => s.from_date_time == ClassFromDateTime && s.to_date_time == ClassToDateTime));

                            MultiClassList = null;
                            MultiClassList = _multipleClassList;

                            conflictClassList.Remove(conflictClassList.FirstOrDefault(s => s.ConflictClassStartTime == ClassFromDateTime && s.ConflictClassEndTime == ClassToDateTime));
                            _newClassView.canv_singleday_multi_class.Visibility = Visibility.Collapsed;
                        }

                        _isFromConflictResolve = true;
                        _newClassView.canv_total_class.Visibility = Visibility.Collapsed;
                        _newClassView.canv_newclass.Visibility = Visibility.Visible;
                        _newClassView.canv_classtype.Visibility = Visibility.Collapsed;
                    }
                    else if (ClassReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassRescheduleTypes.RescheduleAllTheClass.ToString()))
                    {
                        if (ConflictClassList.Count > 0)
                        {
                            _newClassView.canv_classtype.Visibility = Visibility.Collapsed;
                            _newClassView.canv_total_class.Visibility = Visibility.Collapsed;
                            _newClassView.canv_newclass.Visibility = Visibility.Visible;
                            if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                            {
                                ClassFromDateTime = _recurringClassDatesList.FirstOrDefault(s => s.from_date_time.ToString("dd/MM/yyyy") == ConflictClassList[0].ConflictClassStartTime.ToString("dd/MM/yyyy")).from_date_time; ;
                                ClassToDateTime = _recurringClassDatesList.FirstOrDefault(s => s.to_date_time.ToString("dd/MM/yyyy") == ConflictClassList[0].ConflictClassEndTime.ToString("dd/MM/yyyy")).to_date_time;
                                _newClassView.canv_singleday_multi_class.Visibility = Visibility.Collapsed;

                                RecurringFromDateTime = ConflictClassList[ConflictClassList.Count - 1].ConflictClassEndTime;
                                _isFromConflictResolve = true;
                            }
                            else if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                            {
                                ClassFromDateTime = ConflictClassList[0].ConflictClassStartTime;
                                ClassToDateTime = ConflictClassList[0].ConflictClassEndTime;
                                _newClassView.canv_singleday_multi_class.Visibility = Visibility.Visible;
                                _newClassView.canv_classtype.Visibility = Visibility.Visible;
                                conflictClassList = null;
                            }
                        }
                    }
                    else if (ClassReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassRescheduleTypes.CancelThisClass.ToString()))
                    {
                        if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                        {
                            if (_recurringClassDatesList.Count > 1)
                            {
                                _recurringClassDatesList.Remove(_recurringClassDatesList.FirstOrDefault(s => s.from_date_time.ToString("dd/MM/yyyy") == ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassStartTime.ToString("dd/MM/yyyy")));
                                conflictClassList.RemoveAt(_newClassView.totalClassList.SelectedIndex);
                            }
                            else
                                Messenger.Default.Send("Can't be deleted, because only one Class left", "Notification");
                        }
                        else if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                        {
                            if (MultiClassList.Count > 1)
                            {
                                _multipleClassList.Remove(_multipleClassList.FirstOrDefault(s => s.from_date_time == ConflictClassList[_newClassView.totalClassList.SelectedIndex].ConflictClassStartTime));

                                MultiClassList = null;
                                MultiClassList = _multipleClassList;

                                conflictClassList.RemoveAt(_newClassView.totalClassList.SelectedIndex);
                            }
                            else
                                Messenger.Default.Send("Can't be deleted, beacause only one Class left", "Notification");
                        }
                        else
                        {
                            if (conflictClassList.Count > 0)
                            {
                                if (ClassList != null && ClassList.Count <= 1)
                                {
                                    Messenger.Default.Send("Can't be deleted, beacause only one Class left", "Notification");
                                }
                                else
                                {
                                    conflictClassList.RemoveAt(_newClassView.totalClassList.SelectedIndex);
                                }
                            }
                        }
                        ConflictClassList = null;
                        ConflictClassList = conflictClassList;
                        if (conflictClassList.Count == 0)
                            GoToStep2();
                    }
                    else if (ClassReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassRescheduleTypes.CancelAllClass.ToString()))
                    {
                        if (_isRecurrentOrSingleDayMultiClass == "Recurrent")
                            foreach (var conflict in ConflictClassList)
                            {
                                if (_recurringClassDatesList.Count > 1)
                                    _recurringClassDatesList.Remove(_recurringClassDatesList.FirstOrDefault(s => s.from_date_time.ToString("dd/MM/yyyy") == conflict.ConflictClassStartTime.ToString("dd/MM/yyyy")));
                            }
                        else if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti")
                        {
                            foreach (var conflict in ConflictClassList)
                            {
                                if (MultiClassList.Count > 1)
                                {
                                    _multipleClassList.Remove(_multipleClassList.FirstOrDefault(s => s.from_date_time == conflict.ConflictClassStartTime));

                                    MultiClassList = null;
                                    MultiClassList = _multipleClassList;
                                }
                            }
                        }
                        else
                        {
                            foreach (var conflict in ConflictClassList)
                            {
                                if (ClassList.Count > 1)
                                    ClassList.Remove(ClassList.FirstOrDefault(s => s.StartTime == conflict.ConflictClassStartTime));
                            }
                        }
                        ConflictClassList = conflictClassList = null;
                        GoToStep2();
                    }
                    else if (ClassReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(ClassRescheduleTypes.SkipForNow.ToString()))
                    {
                        ConflictClassList = conflictClassList = null;
                        GoToStep2();
                    }

                    if (_isRecurrentOrSingleDayMultiClass == "Recurrent" && _recurringClassDatesList != null)
                    {
                        _newClassView.tbk_confirmed_class.Text = _recurringClassDatesList.Count.ToString();
                    }
                    if (_isRecurrentOrSingleDayMultiClass == "SingleDayMulti" && MultiClassList != null)
                    {
                        _newClassView.tbk_confirmed_class.Text = MultiClassList.Count.ToString();
                    }

                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.RescheduleAllTheMeetings.ToString()))
                    //{
                    //}
                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.ChangeLocation.ToString()))
                    //{
                    //}
                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.RequestOtherParty.ToString()))
                    //{
                    //}
                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.CancelThisMeeting.ToString()))
                    //{
                    //    KeyValuePair<string, string> notificationClear = new KeyValuePair<string, string>("cancel_this_meeting", "Are you sure to  cancel this meeting ?");
                    //    Messenger.Default.Send(notificationClear, "Result");
                    //}
                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.CancelAllMeetings.ToString()))
                    //{
                    //    KeyValuePair<string, string> notificationClear = new KeyValuePair<string, string>("cancel_all_meetings", "Are you sure to  cancel all meetings ?");
                    //    Messenger.Default.Send(notificationClear, "Result");
                    //}
                    //else if (MeetingReschedule.Trim() == NxgUtilities.GetStringUpperCharwithAddedSpace(MeetingRescheduleTypes.SkipForNow.ToString()))
                    //{
                    //    GoToStep2();
                    //}
                }
                else if (_newClassView.canv_inviteParticipants.IsVisible)
                {
                    if (ValidateStep2())
                        GoToStep3();
                }
                else if (_newClassView.canv_classtitle.IsVisible)
                {
                    if (ValidateStep3())
                    {
                        HideAllSteps();
                        GoToAgendaStep();
                    }
                }
                else if (_newClassView.canv_agenda.IsVisible)
                {
                    if (_newClassView.canv_add_agenda.IsVisible)
                    {
                        CreateAgenda();
                    }
                    else if (ValidateAgendaStep())
                    {
                        HideAllSteps();
                        GoToStep4();
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Next Step Button

        #region Ellipses Menu

        private int _currentEllipseIndex = 0, _nextEllipseIndex = 1;

        public void canv_ellipse_steps_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas canvEllipse = (sender as Canvas);
                _nextEllipseIndex = _newClassView.sp_ellipse_steps.Children.IndexOf(canvEllipse);
                isFromExisted = true;

                if (_currentEllipseIndex != _nextEllipseIndex)
                {
                    switch (_currentEllipseIndex)
                    {
                        case 0:
                            if (ValidateStep1())
                            {
                                if (ConflictClassList != null && ConflictClassList.Count > 0)
                                {
                                    ClassRescheduleList = Enum.GetNames(typeof(ClassRescheduleTypes)).Select(s => NxgUtilities.GetStringUpperCharwithAddedSpace(s)).ToList();

                                    _newClassView.totalClassList.SelectedIndex = 0;
                                    _newClassView.canv_total_class.Visibility = Visibility.Visible;
                                    _newClassView.canv_recursive_class_reschedule.Visibility = _newClassView.canv_conflict_next.Visibility = _newClassView.canv_conflict_prev.Visibility = Visibility.Hidden;

                                    if (ConflictClassList.Count > 1)
                                        _newClassView.canv_conflict_next.Visibility = Visibility.Visible;
                                }
                                else
                                    ShowNextScreen();
                            }
                            break;
                        case 1:
                            if (ValidateStep2())
                                ShowNextScreen();
                            break;
                        case 2:
                            if (ValidateStep3())
                                ShowNextScreen();
                            break;
                        case 3:
                            if (ValidateAgendaStep())
                            {
                                HideAllSteps();
                                ShowNextScreen();
                            }
                            break;
                        case 4:
                            HideAllSteps();
                            ShowNextScreen();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ShowNextScreen()
        {
            try
            {
                switch (_nextEllipseIndex)
                {
                    case 0:
                        GoToStep1();
                        break;
                    case 1:
                        GoToStep2();
                        break;
                    case 2:
                        GoToStep3();
                        break;
                    case 3:
                        GoToAgendaStep();
                        break;
                    case 4:
                        GoToStep4();
                        break;
                }
                _currentEllipseIndex = _nextEllipseIndex;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Ellipses Menu

        #region Properties

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

        #endregion Properties

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

        #region Properties

        private string _classCategory;
        public string ClassCategory
        {
            get { return _classCategory; }
            set
            {
                _classCategory = value;
                OnPropertyChanged("ClassCategory");
            }
        }

        private string _classType;
        public string ClassType
        {
            get { return _classType; }
            set
            {
                if (value != null)
                {
                    _classType = value;
                    OnPropertyChanged("ClassType");
                }
            }
        }

        private string _classReschedule;
        public string ClassReschedule
        {
            get { return _classReschedule; }
            set
            {
                _classReschedule = value;
                OnPropertyChanged("ClassReschedule");
            }
        }

        //private string _agendaTitle;
        //public string AgendaTitle
        //{
        //    get { return _agendaTitle; }
        //    set
        //    {
        //        _agendaTitle = value;
        //        OnPropertyChanged("AgendaTitle");
        //    }
        //}

        //private int _agendaMinutes;
        //public int AgendaMinutes
        //{
        //    get { return _agendaMinutes; }
        //    set
        //    {
        //        _agendaMinutes = value;
        //        OnPropertyChanged("AgendaMinutes");
        //    }
        //}

        //private string _agendaPresenter;
        //public string AgendaPresenter
        //{
        //    get { return _agendaPresenter; }
        //    set
        //    {
        //        _agendaPresenter = value;
        //        OnPropertyChanged("AgendaPresenter");
        //    }
        //}

        private DateTime _classFromDateTime;
        public DateTime ClassFromDateTime
        {
            get { return _classFromDateTime; }
            set
            {
                _classFromDateTime = value;
                OnPropertyChanged("ClassFromDateTime");
            }
        }

        private DateTime _classToDateTime;
        public DateTime ClassToDateTime
        {
            get { return _classToDateTime; }
            set
            {
                _classToDateTime = value;
                OnPropertyChanged("ClassToDateTime");
            }
        }

        private List<int> _hoursList;
        public List<int> HoursList
        {
            get { return _hoursList; }
            set
            {
                _hoursList = value;
                OnPropertyChanged("HoursList");
            }
        }

        private int _hoursSelectedIndex;
        public int HoursSelectedIndex
        {
            get { return _hoursSelectedIndex; }
            set
            {
                _hoursSelectedIndex = value;
                OnPropertyChanged("HoursSelectedIndex");
            }
        }

        private double _startSessionOpacity;
        public double StartSessionOpacity
        {
            get { return _startSessionOpacity; }
            set
            {
                _startSessionOpacity = value;
                OnPropertyChanged("StartSessionOpacity");
            }
        }

        private double _endSessionOpacity;
        public double EndSessionOpacity
        {
            get { return _endSessionOpacity; }
            set
            {
                _endSessionOpacity = value;
                OnPropertyChanged("EndSessionOpacity");
            }
        }

        private List<int> _minutesList;
        public List<int> MinutesList
        {
            get { return _minutesList; }
            set
            {
                _minutesList = value;
                OnPropertyChanged("MinutesList");
            }
        }

        private int _minutesSelectedIndex;
        public int MinutesSelectedIndex
        {
            get { return _minutesSelectedIndex; }
            set
            {
                _minutesSelectedIndex = value;
                OnPropertyChanged("MinutesSelectedIndex");
            }
        }

        private List<int> _yearsList;
        public List<int> YearsList
        {
            get { return _yearsList; }
            set
            {
                _yearsList = value;
                OnPropertyChanged("YearsList");
            }
        }

        private List<DateTime> _monthsList;
        public List<DateTime> MonthsList
        {
            get { return _monthsList; }
            set
            {
                _monthsList = value;
                OnPropertyChanged("MonthsList");
            }
        }

        private List<string> _classCategoryList;
        public List<string> ClassCategoryList
        {
            get { return _classCategoryList; }
            set
            {
                _classCategoryList = value;
                OnPropertyChanged("ClassCategoryList");
            }
        }

        private List<string> _classTypeList;
        public List<string> ClassTypeList
        {
            get { return _classTypeList; }
            set
            {
                _classTypeList = value;
                OnPropertyChanged("ClassTypeList");
            }
        }

        private List<string> _classRescheduleList;
        public List<string> ClassRescheduleList
        {
            get { return _classRescheduleList; }
            set
            {
                _classRescheduleList = value;
                OnPropertyChanged("ClassRescheduleList");
            }
        }

        private List<Class> _classList;
        public List<Class> ClassList
        {
            get { return _classList; }
            set
            {
                _classList = value;
                OnPropertyChanged("ClassList");
            }
        }

        private List<Class> _conflictClassList;
        public List<Class> ConflictClassList
        {
            get { return _conflictClassList; }
            set
            {
                _conflictClassList = value;
                OnPropertyChanged("ConflictClassList");
            }
        }

        private List<string> _alphabetsList;
        public List<string> AlphabetsList
        {
            get { return _alphabetsList; }
            set
            {
                _alphabetsList = value;
                OnPropertyChanged("AlphabetsList");
            }
        }

        public List<Employees> _contactsList = HomePageViewModel._contactsDbList;
        public List<Employees> ContactsList
        {
            get { return _contactsList; }
            set
            {
                _contactsList = value;
                OnPropertyChanged("ContactsList");
            }
        }

        private string _frequencyEndDate;
        public string FrequencyEndDate
        {
            get { return _frequencyEndDate; }
            set
            {
                _frequencyEndDate = value;
                OnPropertyChanged("FrequencyEndDate");
            }
        }

        private string _frequency;
        public string Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = value;
                OnPropertyChanged("Frequency");
            }
        }

        private List<string> _recurranceClassFrequencyTypeList;
        public List<string> RecurranceClassFrequencyTypeList
        {
            get { return _recurranceClassFrequencyTypeList; }
            set
            {
                _recurranceClassFrequencyTypeList = value;
                OnPropertyChanged("RecurranceClassFrequencyTypeList");
            }
        }

        private DateTime _recurringFromDateTime;
        public DateTime RecurringFromDateTime
        {
            get { return _recurringFromDateTime; }
            set
            {
                _recurringFromDateTime = value;
                OnPropertyChanged("RecurringFromDateTime");
            }
        }

        private DateTime _recurringToDateTime;
        public DateTime RecurringToDateTime
        {
            get { return _recurringToDateTime; }
            set
            {
                _recurringToDateTime = value;
                OnPropertyChanged("RecurringToDateTime");
            }
        }

        private List<MultiClass> _multiClassList;
        public List<MultiClass> MultiClassList
        {
            get { return _multiClassList; }
            set
            {
                _multiClassList = value;
                OnPropertyChanged("MultiClassList");
            }
        }

        #endregion

        #region Popup Animations

        /// <summary>
        /// Popup close animation for step 1
        /// </summary>
        private void ClosePopupAnimaton()
        {
            try
            {
                Animation.Scale(new List<FrameworkElement> { _newClassView.canv_class_type, _newClassView.canv_class_date, _newClassView.canv_recurring_date, _newClassView.canv_class_time, _newClassView.canv_class_occurance, _newClassView.canv_recurrance_type }, 1, 0, 0.3, 0, HideStepOnePopups);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion

        #region Agenda 

        private int _remainingDuration;
        private int _completedDuration;

        private void GoToAgendaStep()
        {
            try
            {
                HideAllSteps();

                _newClassView.sp_ellipse_steps.Children.OfType<Canvas>().ToList().ForEach(c => (c.Children[0] as Path).Opacity = 0.2);
                _newClassView.path_4.Opacity = 0.8;

                _newClassView.wp_status.Visibility = Visibility.Visible;
                _newClassView.canv_nextstep.Visibility = Visibility.Visible;

                EnableHitTestVisible(_newClassView.canv_ellipse_4);
                _currentEllipseIndex = 3;

                _newClassView.canv_agenda.Visibility = Visibility.Visible;

                if (_agendaList == null || _agendaList.Count == 0)
                {
                    //_newMeetingView.canv_schedule_agenda.Visibility = Visibility.Visible;
                    //_newMeetingView.stackpanel_Carosel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // _newMeetingView.canv_schedule_agenda.Visibility = Visibility.Collapsed;
                    //_newMeetingView.stackpanel_Carosel.Visibility = Visibility.Visible;

                    if (_newClassView.lb_agendas.Items.Count > 0 || (_agendaList != null && _agendaList.Count > 0))
                    {
                        _agendaList.ForEach(s => s.Participants = _invitedParticipantsList);

                        _newClassView.lb_agendas.Items.Clear();

                        _newClassView.lb_agendas.ItemsSource = null;
                        _newClassView.lb_agendas.ItemsSource = _agendaList;
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #region Methods

        private bool ValidateAgendaStep()
        {
            bool returnValue = _agendaList == null || _agendaList.Count == 0;
            try
            {
                if (!returnValue && _agendaList != null && _agendaList.Count > 0)
                {
                    returnValue = string.IsNullOrWhiteSpace(_agendaList.Last().AgendaName) && string.IsNullOrWhiteSpace(_agendaList.Last().EmployeeEmail) && _agendaList.Last().AgendaDuration <= 0;
                    if (!returnValue)
                        returnValue = AddAgendaToList(_agendaList.Last());
                    else if (returnValue)
                        _agendaList.Remove(_agendaList.Last());
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return returnValue;
        }

        /// <summary>
        /// create new Agenda
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CreateAgenda()
        {
            try
            {
                if (_newClassView.inkcanvas_agenda_title.IsVisible && _newClassView.inkcanvas_agenda_title.Strokes.Count > 0)
                {
                    _newClassView.txt_agenda_title.Text = RecognizeStrokes.RecognizeText(_newClassView.inkcanvas_agenda_title, null);
                }
                string agendaTitle = _newClassView.txt_agenda_title.Text.Trim();

                if (string.IsNullOrWhiteSpace(agendaTitle))
                {
                    Messenger.Default.Send("Please enter Class Plan title", "Notification");
                    return;
                }

                int agendaDuration = Convert.ToInt32(_newClassView.tbk_agenda_duration.Text.Trim());

                if (agendaDuration <= 0)
                {
                    Messenger.Default.Send("Please enter Class Plan duration", "Notification");
                    return;
                }

                Participants presenter = _newClassView.lb_agenda_presenter.SelectedItem as Participants;

                if (presenter == null)
                {
                    Messenger.Default.Send("Please select Class Plan presenter", "Notification");
                    return;
                }

                if (_agendaList == null)
                    _agendaList = new List<Agendas>();

                Agendas agenda = new Agendas { AgendaName = agendaTitle, AgendaDuration = agendaDuration, Presenter = presenter, EmployeeEmail = presenter.Employee.Email, EmployeeId = presenter.Employee.EmployeeId };

                if (_selectedAgendaIndex > -1)
                {
                    _agendaList.Remove(_selectedAgenda);
                    _agendaList.Insert(_selectedAgendaIndex, agenda);
                }
                else
                {
                    _agendaList.Add(agenda);
                }
                for (int i = 0; i < _agendaList.Count; i++)
                {
                    _agendaList[i].StartTime = i == 0 ? ClassFromDateTime : _agendaList[i - 1].EndTime;
                    _agendaList[i].EndTime = _agendaList[i].StartTime.AddMinutes(Convert.ToInt32(_agendaList[i].AgendaDuration));
                }

                _newClassView.lb_agendas.ItemsSource = null;
                _newClassView.lb_agendas.ItemsSource = _agendaList;

                //_completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum() + Convert.ToInt32(_newMeetingView.tbk_agenda_duration.Text);
                //_remainingDuration = _meetingTotalDuration - _completedDuration;
                //_newMeetingView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                _newClassView.txt_agenda_title.Text = "";
                _newClassView.tbk_agenda_duration.Text = "0";
                _newClassView.lb_agenda_presenter.SelectedIndex = -1;

                _newClassView.canv_agendas_list.Visibility = Visibility.Visible;
                _newClassView.canv_add_agenda.Visibility = Visibility.Collapsed;

                _newClassView.sp_durations.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as TextBlock).Foreground = Brushes.Black);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// add agenda to agendas list
        /// </summary>
        /// <param name="selectedAgenda"></param>
        /// <returns></returns>
        private bool AddAgendaToList(Agendas selectedAgenda)
        {
            bool returnValue = false;
            try
            {
                if (string.IsNullOrWhiteSpace(selectedAgenda.AgendaName))
                {
                    Messenger.Default.Send("Please enter Class Plan title.", "Notification");
                    return returnValue;
                }

                if (selectedAgenda.AgendaDuration <= 0)
                {
                    Messenger.Default.Send("Please select Class Plan duration.", "Notification");
                    return returnValue;
                }

                if (selectedAgenda.Presenter == null || string.IsNullOrWhiteSpace(selectedAgenda.Presenter.Employee.Email))
                {
                    Messenger.Default.Send("Please select Class Plan presenter.", "Notification");
                    return returnValue;
                }

                if (_remainingDuration < 0)
                {
                    Messenger.Default.Send("Class Plan duration exceeded Class duration", "Notification");
                    return returnValue;
                }

                _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum();
                _remainingDuration = _classTotalDuration - _completedDuration;
                _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                if (selectedAgenda.EmployeeId > 0)
                    return true;

                selectedAgenda.EmployeeEmail = selectedAgenda.Presenter.Employee.Email;
                selectedAgenda.EmployeeId = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == selectedAgenda.Presenter.Employee.Email).EmployeeId;
                returnValue = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return returnValue;
        }

        private void GotoKeyBoardAgendaTitle(string text)
        {
            Canvas senderCanvas = _newClassView.canv_agenda_keyboard;

            _newClassView.txt_agenda_title.Visibility = Visibility.Visible;
            _newClassView.inkcanvas_agenda_title.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(text))
            {
                _newClassView.txt_agenda_title.Text = text;
            }
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// add agenda click event if no items in agendas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_add_new_agenda_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _newClassView.txt_agenda_title.Text = "";
                _newClassView.inkcanvas_agenda_title.Strokes.Clear();
                canv_inputoptions_MouseDown(_newClassView.canv_agenda_keyboard, null);
                _selectedAgendaIndex = -1;
                _newClassView.canv_add_agenda.Visibility = Visibility.Visible;
                _newClassView.canv_agendas_list.Visibility = Visibility.Collapsed;

                _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum();
                _remainingDuration = _classTotalDuration - _completedDuration;
                _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                _newClassView.lb_agenda_presenter.ItemsSource = null;
                _newClassView.lb_agenda_presenter.ItemsSource = _invitedParticipantsList;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private int _selectedAgendaIndex = -1;
        private Agendas _selectedAgenda;
        /// <summary>
        /// to edit agenda item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_edit_agenda_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas canv = sender as Canvas;
                canv_inputoptions_MouseDown(_newClassView.canv_agenda_keyboard, null);
                if (canv != null)
                {
                    _selectedAgenda = canv.Tag as Agendas;
                    if (_selectedAgenda != null)
                    {
                        _selectedAgendaIndex = _agendaList.IndexOf(_selectedAgenda);

                        _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum() + Convert.ToInt32(_newClassView.tbk_agenda_duration.Text);
                        _remainingDuration = _classTotalDuration - _completedDuration;
                        _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                        _newClassView.txt_agenda_title.Text = _selectedAgenda.AgendaName;
                        _newClassView.tbk_agenda_duration.Text = _selectedAgenda.AgendaDuration.ToString();
                        _newClassView.lb_agenda_presenter.SelectedIndex = _invitedParticipantsList.IndexOf(_selectedAgenda.Presenter);

                        _newClassView.canv_agendas_list.Visibility = Visibility.Collapsed;
                        _newClassView.canv_add_agenda.Visibility = Visibility.Visible;

                        _newClassView.sp_durations.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as TextBlock).Foreground = Brushes.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Delete agenda
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        Agendas _agendaToBeDeleted;
        public void canv_Delete_Event_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                Canvas canvToBeDeleted = sender as Canvas;
                if (canvToBeDeleted != null)
                {
                    _agendaToBeDeleted = canvToBeDeleted.Tag as Agendas;
                    if (_agendaToBeDeleted != null)
                    {
                        Messenger.Default.Send(new KeyValuePair<string, string>("Delete Agendas Confirm", "Are you sure, Do you want to delete this Class Plan ?"), "Result");
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void DeleteAgendasConfirm(string obj)
        {
            try
            {
                _completedDuration = _completedDuration - _agendaToBeDeleted.AgendaDuration;
                _remainingDuration = _classTotalDuration - _completedDuration;
                _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);
                _agendaList.Remove(_agendaToBeDeleted);

                for (int i = 0; i < _agendaList.Count; i++)
                {
                    _agendaList[i].StartTime = i == 0 ? ClassFromDateTime : _agendaList[i - 1].EndTime;
                    _agendaList[i].EndTime = _agendaList[i].StartTime.AddMinutes(Convert.ToInt32(_agendaList[i].AgendaDuration));
                }

                _newClassView.lb_agendas.ItemsSource = null;
                _newClassView.lb_agendas.ItemsSource = _agendaList;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Adds duration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void sp_durations_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                int durationToBeAdded = Convert.ToInt32(Convert.ToString(((sender as Canvas).Children[0] as TextBlock).Text).Substring(1));

                if (_remainingDuration >= durationToBeAdded)
                {
                    _newClassView.sp_durations.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as TextBlock).Foreground = Brushes.Black);
                    ((sender as Canvas).Children[0] as TextBlock).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF3EE922"));

                    _newClassView.tbk_agenda_duration.Text = Convert.ToString(Convert.ToInt32(_newClassView.tbk_agenda_duration.Text) + durationToBeAdded);

                    if (_selectedAgendaIndex > -1)
                    {
                        _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum() - _selectedAgenda.AgendaDuration + Convert.ToInt32(_newClassView.tbk_agenda_duration.Text);
                    }
                    else
                    {
                        _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum() + Convert.ToInt32(_newClassView.tbk_agenda_duration.Text);
                    }
                    _remainingDuration = _classTotalDuration - _completedDuration;
                    _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);
                }
                else
                {
                    Messenger.Default.Send("Class Plan duration exceeded Class duration..!", "Notification");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear agenda title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_clear_agenda_title_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _newClassView.txt_agenda_title.Text = "";
                _newClassView.inkcanvas_agenda_title.Strokes.Clear();

            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear duration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_clear_agenda_duration_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (_selectedAgendaIndex > -1)
                {
                    if (Convert.ToInt32(_newClassView.tbk_agenda_duration.Text) != 0)
                    {
                        _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum() - _selectedAgenda.AgendaDuration;
                    }

                }
                else
                {
                    _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum();
                }

                _remainingDuration = _classTotalDuration - _completedDuration;
                _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                _newClassView.tbk_agenda_duration.Text = "0";
                _newClassView.sp_durations.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as TextBlock).Foreground = Brushes.Black);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// Clear add agenda screen without saving
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_back_to_agenda_list_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _newClassView.canv_agendas_list.Visibility = Visibility.Visible;
                _newClassView.canv_add_agenda.Visibility = Visibility.Collapsed;

                _completedDuration = _agendaList.Select(s => s.AgendaDuration).Sum();
                _remainingDuration = _classTotalDuration - _completedDuration;
                _newClassView.tbk_remaining_duration.Text = Convert.ToString(_remainingDuration);

                _newClassView.txt_agenda_title.Text = "";
                _newClassView.tbk_agenda_duration.Text = "0";
                _newClassView.lb_agenda_presenter.SelectedIndex = -1;
                _newClassView.sp_durations.Children.OfType<Canvas>().ToList().ForEach(s => (s.Children[0] as TextBlock).Foreground = Brushes.Black);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_inputoptions_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {

                Canvas senderCanvas = sender as Canvas;

                if (senderCanvas.Name == "canv_agenda_keyboard")
                {
                    string recognizedText = string.Empty;

                    if (_newClassView.inkcanvas_agenda_title.Strokes.Count > 0)
                        recognizedText = RecognizeStrokes.RecognizeText(_newClassView.inkcanvas_agenda_title, null);

                    GotoKeyBoardAgendaTitle(recognizedText);
                    _newClassView.canv_keyboard.Opacity = 1;
                    _newClassView.canv_ink.Opacity = 0.502;
                    _newClassView.txt_agenda_title.CaretIndex = _newClassView.txt_agenda_title.Text.Length;
                }
                else if (senderCanvas.Name == "canv_agenda_ink")
                {
                    _newClassView.txt_agenda_title.Visibility = Visibility.Collapsed;
                    _newClassView.inkcanvas_agenda_title.Visibility = Visibility.Visible;
                    _newClassView.canv_keyboard.Opacity = 0.502;
                    _newClassView.canv_ink.Opacity = 1;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }



        #endregion Events

        #region Properties

        private List<Agendas> _agendaList;
        public List<Agendas> AgendaList
        {
            get { return _agendaList; }
            set
            {
                _agendaList = value;
                OnPropertyChanged("AgendaList");
            }
        }

        #endregion Properties

        #endregion Agenda

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
            DragAndDrop.ActionUIElement_StylusDown(sender, e);
        }

        /// <summary>
        /// Stylus move event to move position of drag element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActionUIElement_StylusMove(object sender, StylusEventArgs e)
        {
            DragAndDrop.ActionUIElement_StylusMove(sender, e);
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
                Canvas canv = dragElement as Canvas;
                if (canv != null)
                {
                    string invitedMailId = (canv.Children[2] as TextBlock).Text;

                    if (_invitedParticipantsList.ToList().Any(s => s.Employee.Email.Trim() == invitedMailId.Trim()))
                    {
                        Messenger.Default.Send("This Class room is already added", "Notification");
                        return;
                    }

                    Employees item = HomePageViewModel._contactsDbList.FirstOrDefault(s => s.Email == invitedMailId);
                    Participants participant = new Participants { EmployeeId = item.EmployeeId, Employee = item };
                    _invitedParticipantsList.Add(participant);
                    _newClassView.lb_invited_participants.Items.Insert(0, participant);

                    if (_invitedParticipantsList.Count == 1)
                        SelectDefaultOrganiser(0);

                    if (_newClassView.lb_invited_participants.Items.Count > 0)
                        _newClassView.canv_drophere_icon.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Drag & Drop

    }
}
