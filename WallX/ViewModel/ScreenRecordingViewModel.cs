using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using WallX.Services;
using WallX.Helpers;
using WallX.Views;
using Microsoft.Expression.Encoder.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;
using NextGen.Controls;

namespace WallX.ViewModel
{
    public class ScreenRecordingViewModel : INotifyPropertyChanged
    {
        public ScreenRecordingViewModel(ScreenRecordingView screenRecordingView, Class currentClass)
        {
            _screenRecordingView = screenRecordingView;
            _currentClass = currentClass;
            //HomePageViewModel._splashScreen.Show(true, true);                
            Messenger.Default.Register<string>(this, "screen_recording", ScreenRecording);
        }

        #region variables

        ScreenRecordingView _screenRecordingView = null;
        string recordedVideoName = string.Empty;
        string recordedVideoFilePath = string.Empty;
        Class _currentClass = null;

        #endregion

        #region pageLoad

        void ScreenRecording(string param)
        {
            StartRecording("");
        }



        #endregion

        #region Recording

        bool recStatus = true;
        bool isRecordingPlay = false;
        Microsoft.Expression.Encoder.ScreenCapture.ScreenCaptureJob _job = null;
        private string _savename;
        string recordingActualFilePath = string.Empty;
        DispatcherTimer dt = null;
        Stopwatch sw = null;
        string currentTime = string.Empty;

        bool StopScreenRecording()
        {
            try
            {
                //HomePageViewModel._splashScreen.Show(true, true);
                if (_job != null)
                {
                    StopRecording();
                    sw.Reset();
                    Clocktxtblock = "00:00:00";
                    dt.Tick -= new EventHandler(dt_Tick);
                    sw = null;
                    dt = null;
                    isRecordingPlay = false;
                    Pause_recording_visibiity = Visibility.Visible;
                    Start_recording_visibiity = Visibility.Hidden;
                    return true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
                return true;
            }
            return false;
        }

        void PauseResumeRecording()
        {
            if (recStatus == false)
            {
                try
                {
                    if (isRecordingPlay == false)
                    {
                        if (_job != null)
                        {
                            _job.Pause();
                            Text_recording_status = "Play";
                            Pause_recording_visibiity = Visibility.Hidden;
                            Start_recording_visibiity = Visibility.Visible;
                            Text_recording_status = "Stop";
                            if (sw.IsRunning)
                            {
                                sw.Stop();
                            }
                            isRecordingPlay = true;
                        }

                    }
                    else if (isRecordingPlay == true)
                    {
                        if (_job != null)
                        {
                            _job.Resume();
                            Text_recording_status = "Stop";
                            Pause_recording_visibiity = Visibility.Visible;
                            Start_recording_visibiity = Visibility.Hidden;
                        }
                        sw.Start();
                        dt.Start();
                        isRecordingPlay = false;
                    }
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            }
        }

        private int RoundOff(int round, double roundOffTo)
        {
            return ((int)Math.Round(round / roundOffTo)) * (int)roundOffTo;
        }

        private List<string> _fileNames = null;
        void dt_Tick(object sender, EventArgs e)
        {
            if (sw != null && sw.IsRunning)
            {
                TimeSpan ts = sw.Elapsed;
                currentTime = string.Format("{0:00}:{1:00}:{2:00}",
                ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Clocktxtblock = currentTime;

                if (ts.Minutes > (8 * (_fileNames == null ? 1 : _fileNames.Count)) && _job != null)
                {
                    if (_fileNames == null)
                    {
                        _fileNames = new List<string>();
                        _fileNames.Add(Path.GetFileName(recordedVideoFilePath));
                    }

                    _job.Stop();

                    string fileName = "File" + _fileNames.Count + "_" + Path.GetFileName(recordedVideoFilePath);
                    _fileNames.Add(fileName);
                    _job.OutputScreenCaptureFileName = Path.GetDirectoryName(recordedVideoFilePath) + "\\" + fileName;
                    _job.Start();
                }
            }
        }

        public void StartRecording(string startRecording)
        {
            if (recStatus)
            {
                try
                {
                    if (_job == null)
                    {
                        _job = new Microsoft.Expression.Encoder.ScreenCapture.ScreenCaptureJob();
                        string tempPath = Constants.AttachmentResources;

                        _savename = tempPath;
                        _job.OutputPath = _savename;

                        recordedVideoName = "rec_" + NxgUtilities.GetCurrentTime() + ".wmv";

                        recordedVideoFilePath = tempPath + recordedVideoName;

                        recordingActualFilePath = _job.OutputScreenCaptureFileName = recordedVideoFilePath;
                        var audioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);
                        for (var deviceCount = 1; deviceCount <= audioDevices.Count; deviceCount++)
                        {
                            var id = deviceCount - 1;
                            _job.AddAudioDeviceSource(audioDevices.ElementAt(id));
                        }

                        _job.ScreenCaptureVideoProfile = new Microsoft.Expression.Encoder.Profiles.ScreenCaptureVideoProfile();
                        _job.ScreenCaptureVideoProfile.AutoFit = true;
                        dt = new DispatcherTimer();
                        sw = new Stopwatch();
                        dt.Tick += new EventHandler(dt_Tick);
                        sw.Start();
                        dt.Start();
                        dt.Interval = new TimeSpan(0, 0, 0, 0, 1);

                        _job.ScreenCaptureVideoProfile.Quality = 50;
                        _job.ScreenCaptureVideoProfile.FrameRate = 24;
                        _job.Start();

                        Canv_stop_recording_visibility = Visibility.Visible;
                        Pause_recording_visibiity = Visibility.Visible;
                        Start_recording_visibiity = Visibility.Hidden;
                    }
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                    recStatus = false;
                }
                recStatus = false;
            }
            else
            {
            }
        }

        void StopRecording()
        {
            if (recStatus == false)
            {
                try
                {
                    recStatus = true;

                    if (_job != null)
                    {
                        _job.Stop();
                        _job.Dispose();
                        _job = null;
                    }
                    Canv_stop_recording_visibility = Visibility.Hidden;
                    recStatus = true;
                }
                catch (Exception ex)
                {
                    App.InsertException(ex);
                }
            }
        }

        #endregion

        #region events

        public void canv_recording_pause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PauseResumeRecording();
            Messenger.Default.Send(-1, "StrokeSelectionbySize");
        }

        public async void canv_recording_stop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (StopScreenRecording())
                {
                    if (_fileNames != null && _fileNames.Count > 0)
                    {
                        string fileName = Path.GetFileName(recordedVideoFilePath);
                        string fileDirPath = Path.GetDirectoryName(recordedVideoFilePath) + "/";

                        string tempFileName = Path.GetTempPath() + fileName;
                        string filePath = fileDirPath + "Splitted_Videos.txt";
                        File.WriteAllText(filePath, string.Join(Environment.NewLine, from file in _fileNames select "file '" + file + "'"));

                        FfmpegRecorder ffmpegRecorder = new FfmpegRecorder(Constants.CurrentDirectoryPath);
                        ffmpegRecorder.Merge(filePath, tempFileName);

                        if (File.Exists(tempFileName))
                        {
                            _fileNames.ForEach(file => File.Delete(fileDirPath + file));
                            File.Delete(filePath);
                            File.Move(tempFileName, recordedVideoFilePath);
                        }
                    }

                    if (recordedVideoFilePath.ToString().Trim().Length > 0)
                    {
                        //App.ExecuteMethod(new Action<string>(ScreenRecordToLibrary), true, recordedVideoFilePath);
                        await Task.Run(() => ScreenRecordToLibrary(recordedVideoFilePath));
                    }
                    Messenger.Default.Send(-1, "StrokeSelectionbySize");
                    Messenger.Default.Unregister(this);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void ScreenRecordToLibrary(string videoName)
        {
            _screenRecordingView.Dispatcher.Invoke(() =>
            {
                if (File.Exists(recordedVideoFilePath))
                {
                    try
                    {
                        string response = Service.UploadFile(recordedVideoFilePath);
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            LibraryThumbs libraryItem = new LibraryThumbs { AttachmentTypeId = (int)AttachmentType.Screen_Record, AttachmentType = AttachmentType.Screen_Record.ToString(), Attachment = response, ClassId = BoardViewModel._currentClass.ClassId };
                            int pk_id = Service.InsertOrUpdateDataToDB(libraryItem, CrudActions.Create);

                            if (pk_id > 0)
                            {
                                string localPath = Constants.AttachmentResources + "File_" + pk_id + "_" + libraryItem.AttachmentUid + Path.GetExtension(recordedVideoFilePath);
                                if (File.Exists(recordedVideoFilePath))
                                    File.Move(recordedVideoFilePath, localPath);
                                NextGen.Controls.GenerateThumb.GenerateThumbnail(localPath, Constants.AttachmentResourceThumbs, ".png");
                                //Messenger.Default.Send("Successfully added to Screen Recordings.", "Notification");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.InsertException(ex);
                    }
                }
                else
                {
                    Messenger.Default.Send("Oops...! Something went wrong. We will get it back working as soon as possible", "Notification");
                }
                Messenger.Default.Send("", "close_screen_recording");
            });
        }

        #endregion //events

        #region Drag

        Point anchorPoint;
        Point currentPoint;
        bool isInDrag = false;

        private TranslateTransform transform = new TranslateTransform();

        public void root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                anchorPoint = e.GetPosition(null);
                element.CaptureMouse();
                isInDrag = true;
                e.Handled = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void root_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (isInDrag)
                {
                    var element = sender as FrameworkElement;
                    currentPoint = e.GetPosition(null);

                    transform.X += currentPoint.X - anchorPoint.X;
                    transform.Y += (currentPoint.Y - anchorPoint.Y);

                    _screenRecordingView.RenderTransform = transform;
                    anchorPoint = currentPoint;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (isInDrag)
                {
                    var element = sender as FrameworkElement;
                    element.ReleaseMouseCapture();
                    isInDrag = false;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Drag

        #region properties

        private string text_recording_status;
        public string Text_recording_status
        {
            get { return text_recording_status; }
            set
            {
                text_recording_status = value;
                OnPropertyChanged("Text_recording_status");
            }
        }

        private string _clocktxtblock;
        public string Clocktxtblock
        {
            get { return _clocktxtblock; }
            set
            {
                _clocktxtblock = value;
                OnPropertyChanged("Clocktxtblock");
            }
        }

        private Visibility _pause_recording_visibiity;
        public Visibility Pause_recording_visibiity
        {
            get { return _pause_recording_visibiity; }
            set
            {
                _pause_recording_visibiity = value;
                OnPropertyChanged("Pause_recording_visibiity");
            }
        }

        private Visibility _start_recording_visibiity;
        public Visibility Start_recording_visibiity
        {
            get { return _start_recording_visibiity; }
            set
            {
                _start_recording_visibiity = value;
                OnPropertyChanged("Start_recording_visibiity");
            }
        }

        private Visibility _canv_stop_recording_visibility;
        public Visibility Canv_stop_recording_visibility
        {
            get { return _canv_stop_recording_visibility; }
            set
            {
                _canv_stop_recording_visibility = value;
                OnPropertyChanged("Canv_stop_recording_visibility");
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
    }
}
