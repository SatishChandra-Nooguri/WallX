using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NextGen.Controls
{
    public static class NxgUtilities
    {
        /// <summary>
        /// creates directory if not exists 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static void CreateDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        /// <summary>
        /// get bitmapimage from bytes
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static BitmapImage GetBitmapImageFromFile(string file, bool checkForExist = true)
        {
            BitmapImage bitmapImage = null;
            if (file != null)
            {
                file = file.Replace("file:///", "");
                if (checkForExist && File.Exists(file) && IsFileReadyToOpen(file) && new FileInfo(file).Length > 0)
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(file);
                    //bitmapImage.DecodePixelWidth = 1920;
                    bitmapImage.EndInit();

                    //bitmapImage.BeginInit();
                    //bitmapImage.UriSource = new Uri(file);
                    ////bitmapImage.DecodePixelWidth = 1920; //based on your wish
                    //bitmapImage.EndInit();
                }
                else if (File.Exists(file))
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(file);
                    bitmapImage.EndInit();
                }
            }
            return bitmapImage;
        }

        /// <summary>
        /// Checking file weather it is available to open or not
        /// </summary>
        /// <param name="fileName">fileName</param>
        /// <returns></returns>
        public static bool IsFileReadyToOpen(string fileName)
        {
            FileStream stream = null;
            try
            {
                stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                stream.Close();
                stream.Dispose();
            }
            catch (Exception ex) { ex.InsertException(); }
            return stream != null;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filePath"></param>
        public static bool DeleteFile(string filePath)
        {
            bool isDeleted = false;
            if (File.Exists(filePath) && IsFileReadyToOpen(filePath))
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
                isDeleted = true;
            }
            return isDeleted;
        }

        /// <summary>
        /// Check internet is available or not
        /// </summary>
        /// <returns></returns>
        public static bool IsInternetAvailable()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    Ping myPing = new Ping();
                    string host = "google.com";
                    byte[] buffer = new byte[32];
                    int timeout = 1000;
                    PingOptions pingOptions = new PingOptions();
                    PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                    return (reply.Status == IPStatus.Success);
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Add space to camelcase string 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetStringUpperCharwithAddedSpace(string value)
        {
            return Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        }

        private static Random _randomNumber = new Random();

        /// <summary>
        /// get random position
        /// </summary>
        /// <returns></returns>
        public static double GetRandomPosition(int startValue, int rangeValue)
        {
            return _randomNumber.Next(startValue, startValue + rangeValue);
        }

        /// <summary>
        /// to generate random passwords
        /// </summary>
        /// <param name="passwordLength"></param>
        /// <returns></returns>
        public static string GetRandomPassword(int passwordLength)
        {
            //string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?*";
            string allowedChars = "0123456789";
            char[] chars = new char[passwordLength];
            Random rd = new Random();
            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }
            return new string(chars);
        }

        /// <summary>
        /// Check image extension valid or not
        /// </summary>
        /// <param name="extension">Extension</param>
        /// <returns></returns>
        public static bool IsValidImageExtension(string extension)
        {
            extension = extension.ToLower().Trim();
            return new List<string> { ".jpg", ".JPG", ".jpeg", ".JPEG", ".png", ".PNG" }.Contains(extension);
        }

        /// <summary>
        /// Check video extension valid or not
        /// </summary>
        /// <param name="extension">Extension</param>
        /// <returns></returns>
        public static bool IsValidVideoExtension(string extension)
        {
            extension = extension.ToLower().Trim();
            return new List<string> { ".mp4", ".MP4", ".wmv", ".WMV" }.Contains(extension);
        }

        /// <summary>
        /// Check pdf extension valid or not
        /// </summary>
        /// <param name="extension">Extension</param>
        /// <returns></returns>
        public static bool IsValidPdfExtension(string extension)
        {
            extension = extension.ToLower().Trim();
            return new List<string> { ".pdf", ".PDF" }.Contains(extension);
        }

        /// <summary>
        /// To Check whether valid email address or not..!
        /// </summary>
        /// <param name="inputEmail"></param>
        /// <returns></returns>
        public static bool IsValidEmail(string inputEmail)
        {
            string strRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";

            Regex re = new Regex(strRegex);
            return re.IsMatch(inputEmail);
        }

        /// <summary>
        /// Check is valid text to allow only digits
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsDigitAllowedText(string text)
        {
            Regex regex = new Regex("[0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        /// <summary>
        /// Get time in 12 hours format from datetime
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetTimeFromDate(DateTime datetime)
        {
            return datetime.ToString("hh:mm tt");
        }

        /// <summary>
        /// Get Extensions for date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetDateExtension(string date)
        {
            int passedValue = int.Parse(date);
            int remainder = passedValue % 10;
            return date + (new List<int> { 11, 12, 13 }.Contains(passedValue) || !(new List<int> { 1, 2, 3 }.Contains(remainder)) ? "th" : remainder == 1 ? "st" : remainder == 2 ? "nd" : "rd");
        }

        /// <summary>
        /// Get date and time as list of strings format
        /// </summary>
        /// <returns></returns>
        public static List<string> GetDateTimeasStringsList(DateTime dateItem)
        {
            List<string> date = new List<string>();
            date.Add(dateItem.ToString("dd"));
            date.Add(dateItem.ToString("MM"));
            date.Add(dateItem.ToString("yyyy"));
            date.Add(dateItem.ToString("hh"));
            date.Add(dateItem.ToString("mm"));
            date.Add(dateItem.ToString("ss"));
            date.Add(dateItem.ToString("tt"));
            return date;
        }

        /// <summary>
        /// Check element is in visible or not
        /// </summary>
        /// <param name="element"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        public static List<string> GetImageListFromDLL(string assemblyName)
        {
            Assembly asm = Assembly.LoadFrom(assemblyName + (assemblyName.Contains(".dll") ? "" : ".dll"));
            System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            string resourceName = asm.GetName().Name + ".g";
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(resourceName, asm);
            System.Resources.ResourceSet resourceSet = rm.GetResourceSet(culture, true, true);
            List<string> resources = new List<string>();
            foreach (System.Collections.DictionaryEntry resource in resourceSet)
            {
                resources.Add((string)resource.Key);
            }
            rm.ReleaseAllResources();
            return resources;
        }

        public static List<string> GetImageListFromDLLFolder(string assemblyName, string folderName)
        {
            List<string> resources = GetImageListFromDLL(assemblyName);
            return resources.Where(s => s.StartsWith(folderName.ToLower())).ToList();
        }

        public static T GetDuplicateOfObject<T>(T selectedItem)
        {
            T createdProduct = default(T);
            try
            {
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(stream, selectedItem);
                    stream.Position = 0;
                    createdProduct = (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex) { ex.InsertException(); }
            return createdProduct;
        }

        public static void GetResourceFromDll(string assemblyPath, string resourcePath, string resourceName)
        {
            try
            {
                if (!File.Exists(Directory.GetCurrentDirectory() + "/" + resourceName))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);

                    using (Stream stream = assembly.GetManifestResourceStream(resourcePath + "." + resourceName))
                    {
                        if (stream != null)
                        {
                            FileStream fileStream = new FileStream(resourceName, FileMode.CreateNew);
                            for (int i = 0; i < stream.Length; i++)
                                fileStream.WriteByte((byte)stream.ReadByte());
                            fileStream.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// get time with milliseconds in number format
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTimeMilliSeconds()
        {
            return DateTime.Now.ToString("yyyyMMddhhmmssms");
        }

        /// <summary>
        /// get time in number format
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("ddhhmmssms");
        }

        /// <summary>
        /// Get bitmap from bytes
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        public static bool GetBitmapFromBytes(byte[] bytesData, string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                MemoryStream stream = new MemoryStream(bytesData);
                if (stream != null && stream.Length > 0)
                {
                    Bitmap bm = null;
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        bm = new Bitmap(stream);
                        bm.Save(mStream, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    System.Drawing.Image image = bm;
                    image.Save(filePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    return true;
                }
            }
            return false;
        }

        #region Visibility & Hidden

        /// <summary>
        /// visible multiple elements
        /// </summary>
        /// <param name="elements"></param>
        public static void VisibleElements(List<FrameworkElement> elements)
        {
            if (elements != null)
                elements.ForEach(s => s.Visibility = Visibility.Visible);
        }

        /// <summary>
        /// Collapse multiple elements
        /// </summary>
        /// <param name="elements"></param>
        public static void CollapseElements(List<FrameworkElement> elements)
        {
            if (elements != null)
                elements.ForEach(s => s.Visibility = Visibility.Collapsed);
        }

        #endregion

        #region keyboard and process start & stop

        /// <summary>
        /// start onscreen keyboard
        /// </summary>
        public static void StartTouchKeyboard(string keyboardPath)
        {
            if (!string.IsNullOrWhiteSpace(keyboardPath) && File.Exists(keyboardPath))
            {
                Process.Start(keyboardPath);
            }
        }

        /// <summary>
        /// stop onscreen keyboard
        /// </summary>
        public static void StopTouchKeyboard(string keyboardPath, bool isRequriedFind = false)
        {
            if (!string.IsNullOrWhiteSpace(keyboardPath) && (File.Exists(keyboardPath) || !isRequriedFind))
            {
                StopProcess(keyboardPath);
            }
        }

        /// <summary>
        /// start any process by its name
        /// </summary>
        /// <param name="procName"></param>
        /// <returns></returns>
        public static bool StartProcess(string procName, string param = "")
        {
            bool isProcessStarted = false;
            if (File.Exists(procName))
            {
                if (param == "")
                {
                    Process.Start(procName);
                }
                else
                {
                    Process.Start(procName, "\"" + param + "\"");
                }
                isProcessStarted = true;
            }
            return isProcessStarted;
        }

        /// <summary>
        /// stop any process by its name
        /// </summary>
        /// <param name="procName"></param>
        /// <returns></returns>
        public static bool StopProcess(string procName)
        {
            bool isProcessStopped = false;
            try
            {
                Process[] workers = Process.GetProcessesByName(procName);
                if (workers.Length > 0)
                {
                    foreach (Process worker in workers)
                    {
                        worker.Kill();
                        worker.WaitForExit();
                        worker.Dispose();
                    }
                }
                isProcessStopped = true;
            }
            catch (Exception ex) { ex.InsertException(); }
            return isProcessStopped;
        }

        #endregion keyboard and process start & stop

        #region crypt

        private static object _ncrypt = null;

        static NxgUtilities()
        {
            try
            {
                string assemblyPath = Environment.SystemDirectory + "/ncryptnc.dll";
                if (File.Exists(assemblyPath))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    if (assembly != null)
                    {
                        Type objectType = assembly.GetType("NextGen.Controls.Cryptography");
                        if (objectType != null)
                        {
                            _ncrypt = Activator.CreateInstance(objectType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.InsertException();
            }
        }

        /// <summary>
        /// Get pkid int value from string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int SetPKID(string value)
        {
            MethodInfo mi = GetMethodFromObject(_ncrypt, "DecryptUsingRij");
            return !string.IsNullOrWhiteSpace(value) && mi != null ? Convert.ToInt32(value.Any(s => char.IsLetter(s)) ? mi?.Invoke(_ncrypt, new object[] { value }) : value) : 0;
        }

        /// <summary>
        /// Get pkid string from int value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetPKID(int value)
        {
            MethodInfo mi = GetMethodFromObject(_ncrypt, "EncryptUsingRij");
            return Convert.ToString(mi?.Invoke(_ncrypt, new object[] { value.ToString() }));
        }

        /// <summary>
        /// get method from object using method name
        /// </summary>
        /// <param name="objectInstance"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodFromObject(object objectInstance, string methodName)
        {
            if (objectInstance == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            return objectInstance.GetType().GetMethod(methodName);
        }

        #endregion

    }
}
