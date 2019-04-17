using System;
using System.Windows.Threading;

namespace NextGen.Controls
{
    public class NxgDynamicGestures
    {
        #region Longtap

        private static DispatcherTimer _longTapTimer = new DispatcherTimer();
        private static Action<object> _methodInfo = null;
        private static object _objectInfo = null;

        public static void StartLongTapTimer(Action<object> methodInfo, object objectInfo)
        {
            try
            {
                if (methodInfo != null)
                {
                    _longTapTimer.Tick += longtapTimer_Tick;
                    _longTapTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                    _longTapTimer.Tag = 0;
                    _methodInfo = methodInfo;
                    _objectInfo = objectInfo;
                    _longTapTimer.Start();
                }
            }
            catch (Exception ex) { ex.InsertException(); }
        }

        private static void longtapTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_longTapTimer != null)
                {
                    _longTapTimer.Tag = Convert.ToInt32(_longTapTimer.Tag) + 1;
                    if (Convert.ToInt32(_longTapTimer.Tag) > 3)
                    {
                        StopLongTapTimer();
                        _methodInfo.Invoke(_objectInfo);
                    }
                }
            }
            catch (Exception ex) { ex.InsertException(); }
        }

        public static void StopLongTapTimer()
        {
            try
            {
                if (_longTapTimer != null)
                {
                    _longTapTimer.Stop();
                    _longTapTimer.Tick -= longtapTimer_Tick;
                }
            }
            catch (Exception ex) { ex.InsertException(); }
        }

        #endregion
    }
}
