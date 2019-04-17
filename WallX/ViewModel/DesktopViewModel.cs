using WallX.Services;
using WallX.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Animation;

using Task = System.Threading.Tasks.Task;
using WallX.Helpers;

namespace WallX.ViewModel
{
    public partial class DesktopViewModel : INotifyPropertyChanged
    {
        #region variables

        private MainWindow _mainWindow;
        private DesktopView _desktopView = null;
        private bool isInkMode = true;
        private List<string> libraryImages = null;
        private bool isFullScreenSnap = false;

        #endregion

        #region Properties

        private string _notificationKey;
        public string NotificationKey
        {
            get { return this._notificationKey; }
            set
            {
                this._notificationKey = value;
                OnPropertyChanged("NotificationKey");
            }
        }

        private string _notificationValue;
        public string NotificationValue
        {
            get { return this._notificationValue; }
            set
            {
                this._notificationValue = value;
                OnPropertyChanged("NotificationValue");
            }
        }

        private string _notificationValueHeader;
        public string NotificationValueHeader
        {
            get { return this._notificationValueHeader; }
            set
            {
                this._notificationValueHeader = value;
                OnPropertyChanged("NotificationValueHeader");
            }
        }

        private string _resultKey;
        public string ResultKey
        {
            get { return this._resultKey; }
            set
            {
                this._resultKey = value;
                OnPropertyChanged("ResultKey");
            }
        }

        private string _resultValue;
        public string ResultValue
        {
            get { return this._resultValue; }
            set
            {
                this._resultValue = value;
                OnPropertyChanged("ResultValue");
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
        #endregion Property Change event

        #region pageLoad

        void PageLoad()
        {
            try
            {
                if (_desktopView != null)
                {
                    libraryImages = new List<string>();
                    SetDefaultOptions();
                    //GoToInkMode();
                }
                else
                {
                    MessageBox.Show("Something went wrong ..!");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public DesktopViewModel(DesktopView desktopView, MainWindow parentWindow, string filePath)
        {
            try
            {
                _desktopView = desktopView;
                _mainWindow = parentWindow;
                PageLoad();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    isInkMode = false;
                    DeskTopToggle_MouseDown(null, null);
                }
                else
                {
                    OpenExternalApp(filePath);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void OpenExternalApp(string filePath)
        {
            try
            {
                isInkMode = true;
                DeskTopToggle_MouseDown(null, null);
                if (filePath.StartsWith("browser"))
                {
                    Process.Start(filePath.Remove(0, 7));
                }
                else
                {
                    if (NextGen.Controls.NxgUtilities.IsValidVideoExtension(System.IO.Path.GetExtension(filePath)))
                        Process.Start("vlc", "\"" + filePath + "\"");
                    else
                        Process.Start("\"" + filePath + "\"");
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion page load

        #region Events

        private void SaveCapture_click(object sender, RoutedEventArgs e)
        {
            try
            {

                App.ExecuteMethod(() => FullScreenShot(rectCropWidth, rectCropHeight, startDrag.X, startDrag.Y, false, false));
                ResetCropScreenShotVariables();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void DeskTopToggle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e != null)
                    e.Handled = true;

                Console.WriteLine("========================" + sender);
                if (isInkMode == false)
                {
                    Console.WriteLine("==========================InkMode");
                    _desktopView.canv_bottom_options.Visibility = Visibility.Visible;
                    _desktopView.canv_green_mode.Visibility = Visibility.Visible;
                    _desktopView.canv_red_mode.Visibility = Visibility.Hidden;
                    GoToInkMode();
                    isInkMode = true;
                }
                else
                {
                    Console.WriteLine("==========================Desktop Mode");
                    _desktopView.canv_bottom_options.Visibility = Visibility.Collapsed;
                    _desktopView.canv_green_mode.Visibility = Visibility.Hidden;
                    _desktopView.canv_red_mode.Visibility = Visibility.Visible;
                    GoToNormalMode();
                    isInkMode = false;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void canv_exit_desktop_mode_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                GoToBoardView(false);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void Capture_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isInkMode)
                {
                    DisableToolBoxSelectors();
                    _desktopView.path_pen_screen_shot_bg.Visibility = Visibility.Visible;
                    _desktopView.path_pen_screen_shot.Opacity = 100;
                    _desktopView.path_pen_screen_shot.Fill = Brushes.Black;
                    _desktopView.path_pen_screen_shot.Stroke = Brushes.Black;
                    _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.None;
                    DesktopScreenShotCrop();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void img_add_to_board_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
                if (isFullScreenSnap)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(1000)).ContinueWith((t) =>
                    {
                        screenShotImagePath = FullScreenShot(rectCropWidth, rectCropHeight, startDrag.X, startDrag.Y, false, false);
                        if (screenShotImagePath != null)
                        {
                        }
                        isFullScreenSnap = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else if (!isFullScreenSnap)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(1000)).ContinueWith((t) =>
                    {
                        screenShotImagePath = FullScreenShot(rectCropWidth, rectCropHeight, startDrag.X, startDrag.Y, false, false);
                        ResetCropScreenShotVariables();
                        if (screenShotImagePath != null)
                        {
                            GoToBoardView(true);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    isFullScreenSnap = false;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void AddImageToLibrary_MouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseAddToLib();
                CancelImageCropping();
                screenShotImagePath = null;
                if (isFullScreenSnap)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(1000)).ContinueWith((t) =>
                    {
                        screenShotImagePath = FullScreenShot(_desktopView.canv_inkcanvas_desktop.Width, _desktopView.canv_inkcanvas_desktop.Height, 0, 0, true, true);
                        if (screenShotImagePath != null)
                        {
                            AddItemtoLibrary(screenShotImagePath);
                        }
                        isFullScreenSnap = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else if (!isFullScreenSnap)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(1000)).ContinueWith((t) =>
                    {
                        screenShotImagePath = FullScreenShot(_desktopView.rectangle_screenshot.Width, _desktopView.rectangle_screenshot.Height, startDrag.X, startDrag.Y, false, true);
                        if (screenShotImagePath != null)
                        {
                            AddItemtoLibrary(screenShotImagePath);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                isFullScreenSnap = false;
                _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Events

        #region Methods

        void AddItemtoLibrary(string libraryItemPath)
        {
            try
            {
                libraryImages.Add(libraryItemPath);
                ShowNotificationMessageBox(new KeyValuePair<string, string>(null, "Item added to the library"));
                _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
                _desktopView.rectangle_screenshot.Visibility = Visibility.Hidden;
                GoToInkMode();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void GoToNormalMode()
        {
            try
            {
                _desktopView.Topmost = true;
                _desktopView.inkcanvas_desktop.Visibility = Visibility.Hidden;
                _desktopView.canv_inkcanvas_desktop.Background = Brushes.Transparent;
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.None;
                _desktopView.canv_bottom_options.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void GoToInkMode(string param = "")
        {
            try
            {
                ResetScreenCaptureEvents();
                _desktopView.canv_inkcanvas_desktop.Cursor = Cursors.Arrow;
                _desktopView.inkcanvas_desktop.Visibility = Visibility.Visible;
                _desktopView.canv_inkcanvas_desktop.Background = Brushes.Transparent;
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.Ink;
                PaintMenu("canv_pen");
                InkColors(_desktopView.path_color_red);
                SetInkStrokes(_desktopView.path_size_1);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        void GoToBoardView(bool goToBoardViewWithImages = false)
        {
            try
            {
                GotoBoardWindow(libraryImages, !goToBoardViewWithImages ? null : screenShotImagePath, 0, 0);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void GotoBoardWindow(List<string> libraryImages, string cropedImagePath, double cropedImgWidth, double cropedImgHeight)
        {
            try
            {
                App.ExecuteMethod(() => _mainWindow.DataFromDeskTopWindow(libraryImages, cropedImagePath, cropedImgWidth, cropedImgHeight));
                _mainWindow.Show();
                _desktopView.Close();
                _mainWindow.Topmost = false;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Methods

        #region capture Rectangle Area

        #region Events

        /// <summary>
        /// main canvas mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                screenShotImagePath = null;
                _desktopView.rectangle_screenshot.Visibility = Visibility.Visible;
                _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
                rectCropWidth = -1;
                rectCropHeight = -1;
                captureDragStatus = true;
                //Set the start point
                startDrag = e.GetPosition(_desktopView.canv_inkcanvas_desktop);
                //Capture the mouse
                if (!_desktopView.canv_inkcanvas_desktop.IsMouseCaptured)
                {
                    _desktopView.canv_inkcanvas_desktop.CaptureMouse();
                    _desktopView.canv_inkcanvas_desktop.Cursor = Cursors.Cross;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// main canvas mouse Move event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_main_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_desktopView.canv_inkcanvas_desktop.IsMouseCaptured)
                {
                    if (captureDragStatus)
                    {
                        screenShotImagePath = null;
                        System.Windows.Point currentPoint = e.GetPosition(_desktopView.canv_inkcanvas_desktop);
                        _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;

                        if (_desktopView.rectangle_screenshot.Visibility == Visibility.Hidden)
                            _desktopView.rectangle_screenshot.Visibility = Visibility.Visible;
                        App.ExecuteMethod(() =>
                        {
                            //Calculate the top left corner of the rectangle regardless of drag direction
                            double x = startDrag.X < currentPoint.X ? startDrag.X : currentPoint.X;
                            double y = startDrag.Y < currentPoint.Y ? startDrag.Y : currentPoint.Y;

                            //Move the rectangle to proper place
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _desktopView.rectangle_screenshot.RenderTransform = new TranslateTransform(x, y);
                                _desktopView.rectangle_screenshot.Width = Math.Abs(e.GetPosition(_desktopView.canv_inkcanvas_desktop).X - startDrag.X);
                                _desktopView.rectangle_screenshot.Height = Math.Abs(e.GetPosition(_desktopView.canv_inkcanvas_desktop).Y - startDrag.Y);
                                rectCropWidth = Math.Abs(e.GetPosition(_desktopView.canv_inkcanvas_desktop).X - startDrag.X);
                                rectCropHeight = Math.Abs(e.GetPosition(_desktopView.canv_inkcanvas_desktop).Y - startDrag.Y);
                            });
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
        /// main canvasd mouse up event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void canv_main_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Console.WriteLine("rectCropWidth --" + rectCropWidth);
                Console.WriteLine("rectCropHeight -- " + rectCropHeight);
                screenShotImagePath = null;
                //Set the start point
                endDrag = e.GetPosition(_desktopView.canv_inkcanvas_desktop);
                //Release the mouse
                if (_desktopView.canv_inkcanvas_desktop.IsMouseCaptured)
                    _desktopView.canv_inkcanvas_desktop.ReleaseMouseCapture();
                _desktopView.canv_inkcanvas_desktop.Cursor = Cursors.Arrow;
                if (captureDragStatus)
                {
                    captureDragStatus = false;
                    isFullScreenSnap = false;
                    if (rectCropWidth < 10 && rectCropHeight < 10)
                    {
                        rectCropWidth = -1;
                        rectCropHeight = -1;
                    }
                    else
                    {
                        _desktopView.canv_addtto_lib_board.Visibility = Visibility.Visible;
                    }
                }
                captureDragStatus = false;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        /// <summary>
        /// cancel mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void img_cancel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                PaintMenu("canv_pen");
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Events

        #region Methods

        private void CancelImageCropping()
        {
            try
            {
                if (screenShotImagePath != null && screenShotImagePath.Length > 0)
                {
                    //SingleFileDelete(screenShotImagePath);
                    screenShotImagePath = null;
                }
                CloseAddToLib();
                ResetCropScreenShotVariables();
                ResetScreenCaptureEvents();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }

        }

        private void CloseAddToLib()
        {
            _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
            _desktopView.rectangle_screenshot.Visibility = Visibility.Hidden;
        }

        #endregion Methods

        #endregion capture Rectangle Area

        #region desktop screenshot

        #region Variables

        private double rectCropWidth = -1;
        private double rectCropHeight = -1;
        private System.Windows.Point startDrag;
        private System.Windows.Point endDrag;
        private bool captureDragStatus = false;
        private string screenShotImagePath = null;
        private string imageName = string.Empty;
        private int rectWidth = 0;
        private int rectHeight = 0;
        private int rectLeft = 0;
        private int rectTop = 0;

        #endregion Variables

        #region Methods

        private void DesktopScreenShotCrop()
        {
            try
            {
                rectCropWidth = -1;
                rectCropHeight = -1;
                _desktopView.canv_inkcanvas_desktop.MouseDown += canv_main_MouseDown;
                _desktopView.canv_inkcanvas_desktop.MouseMove += canv_main_MouseMove;
                _desktopView.canv_inkcanvas_desktop.MouseUp += canv_main_MouseUp;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ResetScreenCaptureEvents()
        {
            try
            {
                _desktopView.canv_inkcanvas_desktop.MouseDown -= canv_main_MouseDown;
                _desktopView.canv_inkcanvas_desktop.MouseMove -= canv_main_MouseMove;
                _desktopView.canv_inkcanvas_desktop.MouseUp -= canv_main_MouseUp;
                captureDragStatus = false;
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.Ink;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ResetCropScreenShotVariables()
        {
            try
            {
                _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
                rectCropWidth = -1;
                rectCropHeight = -1;
                captureDragStatus = false;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private string FullScreenShot(double eleWidth, double eleHeight, double eleLeft, double eleRight, bool fullScreen, bool fromLibrary)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _desktopView.canv_addtto_lib_board.Visibility = Visibility.Hidden;
                });
                Console.WriteLine("FullScreenShot");
                rectWidth = 0;
                rectHeight = 0;
                rectLeft = 0;
                rectTop = 0;

                if (fullScreen)
                {
                    rectWidth = Convert.ToInt32(eleWidth);
                    rectHeight = Convert.ToInt32(eleHeight);
                    rectLeft = Convert.ToInt32(eleLeft);
                    rectTop = Convert.ToInt32(eleRight);
                }
                else
                {
                    if (eleRight > endDrag.Y)
                        eleRight = endDrag.Y;

                    if (eleLeft > endDrag.X)
                        eleLeft = endDrag.X;

                    rectWidth = Convert.ToInt32(eleWidth - 6);
                    rectHeight = Convert.ToInt32(eleHeight - 6);
                    rectLeft = Convert.ToInt32(eleLeft + 3);
                    rectTop = Convert.ToInt32(eleRight + 3);
                }

                System.Drawing.Bitmap bitmap = null;
                string childName = Constants.AttachmentResources + "capture_" + DateTime.Now.ToString("hhmmssfff") + "_png" + ".png";
                if (rectWidth > 1 || rectHeight > 1)
                {
                    using (bitmap = new System.Drawing.Bitmap(rectWidth, rectHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (System.Drawing.Graphics grx = System.Drawing.Graphics.FromImage(bitmap))
                        {
                            grx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            grx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            grx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            grx.CopyFromScreen(rectLeft, rectTop, 0, 0, bitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                            grx.Dispose();
                        }

                        bitmap.Save(childName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    bitmap.Dispose();
                    return childName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
                return null;
            }
        }

        #endregion Methods

        #endregion desktop screenshot

        #region InkToolBox

        #region Variables

        private Canvas stroke_size_path_default = null;
        private System.Windows.Shapes.Path color_default_path = null;
        private Canvas eraser_default = null;
        private string selectedInkTool = string.Empty;
        private double eraserSize = 10;
        private Canvas selectedInkSizeCanvas = null;
        private double inkSize = 0;
        private Canvas selectedInkStrokesCanv = null;
        private System.Windows.Shapes.Path selectedInkPath = null;
        private System.Windows.Shapes.Path selectedInkColorPath = null;

        private enum MenuInkToolbox
        {
            canv_pen,
            canv_eraser,
            canv_highlighter,
            canv_clear,
            path_color_white,
            path_color_red,
            path_color_green,
            path_color_blue,
            canv_selection,

            path_size_1,
            path_size_2,
            path_size_3,
            path_size_4,

            path_eraser_1,
            path_eraser_2,
            path_eraser_3,
            path_eraser_4,
            canv_screen_shot,
        }

        #endregion Variables

        #region Events

        public void menu_ink_toolbox(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas canv_sender = sender as Canvas;
                PaintMenu(canv_sender.Name.ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void ClearInkBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ShowResultMessageBox(new KeyValuePair<string, string>("Clear", "This will clear all the strokes on the canvas. Do you want to proceed?"));
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void ink_colors_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                InkColors(sender as System.Windows.Shapes.Path);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void ink_stroke_sizes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SetInkStrokes(sender as Canvas);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void eraser_sizes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SetEraserSize(sender as Canvas);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Events

        #region Methods

        private void SetDefaultOptions()
        {
            try
            {
                color_default_path = _desktopView.path_color_red;
                stroke_size_path_default = _desktopView.path_size_1;
                eraser_default = _desktopView.path_eraser_1;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void PaintMenu(string senderName)
        {
            try
            {
                selectedInkTool = senderName;
                DisableToolBoxSelectors();
                CancelImageCropping();
                if (MenuInkToolbox.canv_pen.ToString() == senderName)
                {

                    _desktopView.canv_pen_options.Visibility = Visibility.Visible;
                    _desktopView.canv_pen_colors.Visibility = Visibility.Visible;
                    _desktopView.canv_pen_strokes_options.Visibility = Visibility.Visible;
                    _desktopView.canv_pen_strokes_options.Visibility = Visibility.Visible;
                    _desktopView.path_pen_bg.Visibility = Visibility.Visible;
                    _desktopView.path_pen.Opacity = 100;
                    _desktopView.path_pen.Fill = Brushes.Black;
                    _desktopView.path_pen.Stroke = Brushes.Black;

                    SetInkStrokes(stroke_size_path_default);
                    InkColors(color_default_path);
                    SetInkMode(InkCanvasEditingMode.Ink);
                }
                else if (MenuInkToolbox.canv_eraser.ToString() == senderName)
                {
                    _desktopView.canv_pen_options.Visibility = Visibility.Visible;
                    _desktopView.path_eraser_bg.Visibility = Visibility.Visible;
                    _desktopView.path_eraser.Opacity = 100;
                    _desktopView.path_eraser.Fill = Brushes.Black;
                    _desktopView.path_eraser.Stroke = Brushes.Black;

                    if (eraser_default == null)
                        SetEraserSize(_desktopView.path_eraser_1);
                    else
                        SetEraserSize(eraser_default);
                    SetInkMode(InkCanvasEditingMode.EraseByPoint);
                }
                else if (MenuInkToolbox.canv_highlighter.ToString() == senderName)
                {
                    _desktopView.canv_pen_options.Visibility = Visibility.Visible;
                    _desktopView.canv_pen_colors.Visibility = Visibility.Hidden;
                    _desktopView.canv_pen_strokes_options.Visibility = Visibility.Visible;
                    _desktopView.path_highlighter_bg.Visibility = Visibility.Visible;
                    _desktopView.path_highlighter.Opacity = 100;
                    _desktopView.path_highlighter.Fill = Brushes.Black;
                    _desktopView.path_highlighter.Stroke = Brushes.Black;

                    HighlighterSettings(InkCanvasEditingMode.Ink);
                }
                else if (MenuInkToolbox.canv_selection.ToString() == senderName)
                {
                    _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.Select;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetEraserSize(Canvas pathSender)
        {
            try
            {
                eraser_default = pathSender;
                _desktopView.canv_pen_options.Visibility = Visibility.Hidden;
                _desktopView.canv_pen_strokes_options.Visibility = Visibility.Hidden;
                _desktopView.canv_eraser_options.Visibility = Visibility.Visible;
                DisableEraserSelectors();
                if (MenuInkToolbox.path_eraser_1.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_eraser_1_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_eraser_2.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_eraser_2_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_eraser_3.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_eraser_3_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_eraser_4.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_eraser_4_bg.Visibility = Visibility.Visible;
                }
                double size = Convert.ToDouble(pathSender.Tag.ToString());
                SetEraserSizeInk(size * 3);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetEraserSizeInk(double size)
        {
            try
            {
                eraserSize = size;
                _desktopView.inkcanvas_desktop.EraserShape = new EllipseStylusShape(size, size);
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.None;
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetInkMode(InkCanvasEditingMode ink_mode)
        {
            try
            {
                eraserSize = eraserSize + 1;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.IsHighlighter = false;
                _desktopView.inkcanvas_desktop.IsManipulationEnabled = false;
                _desktopView.inkcanvas_desktop.EraserShape = new EllipseStylusShape(eraserSize, eraserSize);
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.None;
                _desktopView.inkcanvas_desktop.EditingMode = ink_mode;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void HighlighterSettings(InkCanvasEditingMode ink_mode)
        {
            try
            {
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.Ink;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.FitToCurve = true;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.IsHighlighter = true;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void DisableToolBoxSelectors()
        {
            try
            {
                ShowHidePenEraserOptions(Visibility.Hidden);

                _desktopView.path_pen_bg.Visibility = Visibility.Hidden;
                _desktopView.path_pen.Opacity = 0.502;
                _desktopView.path_pen.Stroke = Brushes.White;
                _desktopView.path_pen.Fill = Brushes.Transparent;

                _desktopView.path_eraser_bg.Visibility = Visibility.Hidden;
                _desktopView.path_eraser.Opacity = 0.502;
                _desktopView.path_eraser.Stroke = Brushes.White;
                _desktopView.path_eraser.Fill = Brushes.Transparent;

                _desktopView.path_highlighter_bg.Visibility = Visibility.Hidden;
                _desktopView.path_highlighter.Opacity = 0.502;
                _desktopView.path_highlighter.Stroke = Brushes.White;
                _desktopView.path_highlighter.Fill = Brushes.Transparent;

                _desktopView.path_pen_screen_shot_bg.Visibility = Visibility.Hidden;
                _desktopView.path_pen_screen_shot.Opacity = 0.502;
                _desktopView.path_pen_screen_shot.Stroke = Brushes.White;
                _desktopView.path_pen_screen_shot.Fill = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ShowHidePenEraserOptions(Visibility vis)
        {
            _desktopView.canv_pen_options.Visibility = vis;
            _desktopView.canv_pen_strokes_options.Visibility = vis;
            _desktopView.canv_eraser_options.Visibility = vis;
        }

        private void SetInkStrokes(Canvas canvSender)
        {
            try
            {
                if (canvSender != null)
                {
                    selectedInkStrokesCanv = canvSender;
                    stroke_size_path_default = canvSender;
                    _desktopView.canv_pen_options.Visibility = Visibility.Visible;
                    _desktopView.canv_pen_strokes_options.Visibility = Visibility.Visible;
                    _desktopView.canv_eraser_options.Visibility = Visibility.Hidden;
                    DisableSizeSelectors();

                    if (MenuInkToolbox.path_size_1.ToString() == canvSender.Name.ToString())
                    {
                        _desktopView.path_size_1_bg.Visibility = Visibility.Visible;
                    }
                    else if (MenuInkToolbox.path_size_2.ToString() == canvSender.Name.ToString())
                    {
                        _desktopView.path_size_2_bg.Visibility = Visibility.Visible;
                    }
                    else if (MenuInkToolbox.path_size_3.ToString() == canvSender.Name.ToString())
                    {
                        _desktopView.path_size_3_bg.Visibility = Visibility.Visible;
                    }
                    else if (MenuInkToolbox.path_size_4.ToString() == canvSender.Name.ToString())
                    {
                        _desktopView.path_size_4_bg.Visibility = Visibility.Visible;
                    }
                    SetInkStrokeSizeInk(canvSender);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetInkStrokeSizeInk(Canvas canvSender)
        {
            try
            {
                selectedInkSizeCanvas = canvSender;
                double size = Convert.ToDouble(canvSender.Tag.ToString());
                setInkStrokeSizeFromParam(size);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void setInkStrokeSizeFromParam(double size)
        {
            try
            {
                inkSize = size;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.Width = size;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.Height = size;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void InkColors(System.Windows.Shapes.Path pathSender)
        {
            try
            {
                selectedInkPath = pathSender;
                color_default_path = pathSender;
                DisableMenuSelectors();
                if (MenuInkToolbox.path_color_white.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_color_white_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_color_red.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_color_red_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_color_green.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_color_green_bg.Visibility = Visibility.Visible;
                }
                else if (MenuInkToolbox.path_color_blue.ToString() == pathSender.Name.ToString())
                {
                    _desktopView.path_color_blue_bg.Visibility = Visibility.Visible;
                }
                SetInkColor(pathSender);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetInkColor(System.Windows.Shapes.Path pathSender)
        {
            try
            {
                selectedInkColorPath = pathSender;
                Color ink_color = (pathSender.Fill as SolidColorBrush).Color;
                SetInkColorfromParam(ink_color);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SetInkColorfromParam(Color ink_color)
        {
            try
            {
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.FitToCurve = true;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.IsHighlighter = false;
                _desktopView.inkcanvas_desktop.DefaultDrawingAttributes.Color = ink_color;
                _desktopView.inkcanvas_desktop.EditingMode = InkCanvasEditingMode.Ink;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void DisableMenuSelectors()
        {
            _desktopView.path_color_white_bg.Visibility = Visibility.Hidden;
            _desktopView.path_color_red_bg.Visibility = Visibility.Hidden;
            _desktopView.path_color_green_bg.Visibility = Visibility.Hidden;
            _desktopView.path_color_blue_bg.Visibility = Visibility.Hidden;
        }

        private void DisableSizeSelectors()
        {
            _desktopView.path_size_1_bg.Visibility = _desktopView.path_size_2_bg.Visibility = _desktopView.path_size_3_bg.Visibility = _desktopView.path_size_4_bg.Visibility = Visibility.Hidden;
        }

        private void DisableEraserSelectors()
        {
            _desktopView.path_eraser_1_bg.Visibility = _desktopView.path_eraser_2_bg.Visibility = _desktopView.path_eraser_3_bg.Visibility = _desktopView.path_eraser_4_bg.Visibility = Visibility.Hidden;
        }

        #endregion Methods

        #endregion

        #region Nofifications

        #region Variables

        private Storyboard notificationMessageBoxAnimation = null;
        private Storyboard sbMessageBoxAnimation = null;

        #endregion Variables

        #region Events

        public void Notification_ConfirmMessage_Tapped(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ResetResultMessageBox();
                (sender as Canvas).Children[0].Visibility = Visibility.Visible;
                ((sender as Canvas).Children[2] as TextBlock).Foreground = new SolidColorBrush(Colors.White);

                if ((sender as Canvas).Name == "canv_YesButton")
                {
                    string messageTitle = ((((sender as Canvas).Parent as Canvas).Parent as Canvas).Children[0] as TextBlock).Text;
                    YesResponseToResultMessageBox(messageTitle);
                }
                else if ((sender as Canvas).Name == "canv_NoButton")
                {
                    NoResponseToResultMessageBox("");
                }
                MessageBoxCloseAnimation();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void ResultNotification_Close_mouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ResetResultMessageBox();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void Notification_Close_mouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _desktopView.canv_Notification_MessageBox.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void notificationMessageBoxAnimation_Completed(object sender, object e)
        {
            try
            {
                if (notificationMessageBoxAnimation != null)
                {
                    notificationMessageBoxAnimation.Stop();
                    notificationMessageBoxAnimation = null;
                    _desktopView.canv_Notification_MessageBox.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void SbMessageBoxAnimation_Completed(object sender, object e)
        {
            try
            {
                if (sbMessageBoxAnimation != null)
                {
                    sbMessageBoxAnimation.Stop();
                    sbMessageBoxAnimation = null;
                    ResetResultMessageBox();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        #endregion Events

        #region Methods

        private void ShowNotificationMessageBox(KeyValuePair<string, string> notification)
        {
            try
            {
                _desktopView.canv_Alert_Title.Visibility = Visibility.Hidden;
                _desktopView.txt_Alert_Info_Only.Visibility = Visibility.Hidden;
                if (!string.IsNullOrWhiteSpace(notification.Key))
                {
                    _desktopView.canv_Alert_Title.Visibility = Visibility.Visible;
                    NotificationKey = notification.Key.ToString();
                    NotificationValue = notification.Value.ToString();
                }
                else
                {
                    _desktopView.txt_Alert_Info_Only.Visibility = Visibility.Visible;
                    NotificationValueHeader = notification.Value.ToString();
                }
                NotificationMessageBoxAnimation();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ShowResultMessageBox(KeyValuePair<string, string> result)
        {
            try
            {
                ResultKey = result.Key.ToString();
                ResultValue = result.Value.ToString();
                ResultMessageBoxAnimation();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void NotificationMessageBoxAnimation()
        {
            try
            {
                _desktopView.canv_Notification_MessageBox.Visibility = Visibility.Visible;
                notificationMessageBoxAnimation = _desktopView.Resources["sb_NotificationMessageBoxAnimation"] as Storyboard;
                notificationMessageBoxAnimation.Completed += notificationMessageBoxAnimation_Completed;
                if (notificationMessageBoxAnimation != null)
                    notificationMessageBoxAnimation.Begin();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ResultMessageBoxAnimation()
        {
            try
            {
                _desktopView.canv_Result_MessageBox.Visibility = Visibility.Visible;
                sbMessageBoxAnimation = _desktopView.Resources["sb_ResultMessageBoxAnimation"] as Storyboard;
                if (sbMessageBoxAnimation != null)
                    sbMessageBoxAnimation.Begin();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void MessageBoxCloseAnimation()
        {
            try
            {
                _desktopView.canv_Result_MessageBox.Visibility = Visibility.Visible;
                sbMessageBoxAnimation = _desktopView.Resources["sb_ResultMessageBoxCloseAnimation"] as Storyboard;
                sbMessageBoxAnimation.Completed += SbMessageBoxAnimation_Completed;
                if (sbMessageBoxAnimation != null)
                    sbMessageBoxAnimation.Begin();
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void ResetResultMessageBox()
        {
            try
            {
                _desktopView.canv_YesButton.Children[0].Visibility = Visibility.Hidden;
                _desktopView.canv_NoButton.Children[0].Visibility = Visibility.Hidden;
                (_desktopView.canv_YesButton.Children[2] as TextBlock).Foreground = new SolidColorBrush(Colors.Black);
                (_desktopView.canv_NoButton.Children[2] as TextBlock).Foreground = new SolidColorBrush(Colors.Black);
                _desktopView.canv_Result_MessageBox.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void YesResponseToResultMessageBox(string tilte)
        {
            try
            {
                switch (tilte)
                {
                    case "Clear":
                        _desktopView.inkcanvas_desktop.Strokes.Clear();
                        break;
                    case "close":
                        GoToBoardView();
                        break;
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        private void NoResponseToResultMessageBox(string tilte)
        {

        }

        #endregion Methods

        #endregion Nofifications

        #region Gestures

        public void Tap_4_TapMethod(UIElement ele, TouchEventArgs e)
        {
            e.Handled = true;
            GoToBoardView(false);
        }

        #endregion
    }
}
