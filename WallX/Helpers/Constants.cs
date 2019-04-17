using WallX.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace WallX.Helpers
{
    public class Constants
    {
        public static Dictionary<string, string> PropertiesList = null;

        static Constants()
        {
            try
            {
                SettingsFilePath = Assembly.GetExecutingAssembly().GetName().Name + ".xml";

                XmlDocument xmldoc = new XmlDocument();
                FileStream fs = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);
                XmlNodeList xmlnode = xmldoc.GetElementsByTagName("Config");
                if (xmlnode != null && xmlnode.Count >= 1)
                {
                    for (int i = 0; i <= xmlnode[0].ChildNodes.Count - 1; i++)
                    {
                        switch (xmlnode[0].ChildNodes.Item(i).Name)
                        {
                            case "Resources":
                                string text = xmlnode[0].ChildNodes.Item(i).InnerText;
                                ProjectResources = (string.IsNullOrWhiteSpace(text) || text.ToLower() == "resources" ? (CurrentDirectoryPath + text) : text) + "/";
                                AttachmentResources = ProjectResources + @"\Attachments\";
                                AttachmentResourceThumbs = AttachmentResources + @"\Thumbs\";
                                break;
                            case "LocationName":
                                LocationName = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "CityName":
                                CityName = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "CountryName":
                                CountryName = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "AutoSaveTimeFrequency":
                                AutoSaveTimeFrequency = Convert.ToDouble(xmlnode[0].ChildNodes.Item(i).InnerText);
                                break;
                            case "DayStartTime":
                                DayStartTime = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "DayEndTime":
                                DayEndTime = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "DefaultBoardBG":
                                DefaultBoardBG = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "AppVersion":
                                AppVersion = Convert.ToDouble(xmlnode[0].ChildNodes.Item(i).InnerText);
                                break;
                            case "AppLocation":
                                AppLocation = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ExtraFeatures":
                                ExtraFeatures = Convert.ToBoolean(xmlnode[0].ChildNodes.Item(i).InnerText);
                                break;
                            case "ZoomUserId":
                                ZoomUserId = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ZoomUserPwd":
                                ZoomUserPwd = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ZoomStartType":
                                ZoomStartType = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ZoomAPI":
                                ZoomAPI = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ZoomSecret":
                                ZoomSecret = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                            case "ZoomToken":
                                ZoomToken = xmlnode[0].ChildNodes.Item(i).InnerText;
                                break;
                        }
                    }
                }

                string assemblyPath = Environment.SystemDirectory + "/ncryptnc.dll";
                if (File.Exists(assemblyPath))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    if (assembly != null)
                    {
                        Type constantsType = assembly.GetType("NextGen.Controls.Constants");
                        if (constantsType != null)
                        {
                            object constants = Activator.CreateInstance(constantsType);
                            PropertiesList = new Dictionary<string, string>();
                            constantsType.GetProperties().ToList().ForEach(s => PropertiesList.Add(s.Name, Convert.ToString(s.GetValue(constants))));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        

        public static string UserName { get { return PropertiesList[ConstKey.EmailId.ToString()]; } }

        public static string Password { get { return PropertiesList[ConstKey.EmailPWD.ToString()]; } }

        public static string MwBaseUrl { get { return PropertiesList[ConstKey.WallXBaseUrl.ToString()]; } }

        public static string MwUserName { get { return PropertiesList[ConstKey.WallXUserName.ToString()]; } }

        public static string MwPassword { get { return PropertiesList[ConstKey.WallXPassword.ToString()]; } }

        #region Rethink DB Details

        public static string RethinkServer { get { return PropertiesList[ConstKey.RtServer.ToString()]; } }

        public static string RethinkDatabase { get { return PropertiesList[ConstKey.RtWallX.ToString()]; } }

        public static string RethinkAnnotations { get { return PropertiesList[ConstKey.RtBoardAnnotation.ToString()]; } }

        public static string RethinkResources { get { return PropertiesList[ConstKey.RtLibraryThumb.ToString()]; } }

        #endregion



        public static string CurrentDirectoryPath = Directory.GetCurrentDirectory() + "/";

        public static string AttachmentResources = ProjectResources + @"Attachments\";

        public static string AttachmentResourceThumbs = AttachmentResources + @"Thumbs\";

        public static string SampleContactsExcel = CurrentDirectoryPath + "File_62_347980e7-686a-4073-9bfe-356845fdde18.xls";



        private static string InternalDirectoryPath = "pack://application:,,,/WallX.Resources;component/";

        public static string InternalResourcesPath = InternalDirectoryPath + "Resources/";

        public static string InternalEmailIconsPath = InternalDirectoryPath + "Email Icons/";

        public static string InternalExtensionIconsPath = InternalDirectoryPath + "Extension Icons/";

        public static string InternalLogoPath = InternalResourcesPath + "Logo.png";

        public static string MediaNotFoundPath = InternalDirectoryPath + "no-preview.png";



        public static string TouchKeyboard { get { return @"C:\Program Files\Common Files\Microsoft Shared\ink\TabTip.exe"; } }

        public static string SettingsFilePath { get; set; }

        public static string ProjectResources { get; set; }

        public static string LocationName { get; set; }

        public static string CityName { get; set; }

        public static string CountryName { get; set; }

        public static double AutoSaveTimeFrequency { get; set; }

        public static string DayStartTime { get; set; }

        public static string DayEndTime { get; set; }

        public static string DefaultBoardBG { get; set; }

        public static double AppVersion { get; set; }

        public static string AppLocation { get; set; }

        public static bool ExtraFeatures { get; set; }


        #region Zoom Call

        public static string ZoomUserId { get; set; }

        public static string ZoomUserPwd { get; set; }

        public static string ZoomStartType { get; set; }

        public static string ZoomAPI { get; set; }

        public static string ZoomSecret { get; set; }

        public static string ZoomToken { get; set; }



        public static string key_baseUrlZoomRestServices = "https://api.zoom.us/v2/";

        public static string key_createMeeting = "v1/meeting/create";

        public static string key_zoomSignIn = "signin";

        public static string key_getMeetings = "v1/meeting/list";

        public static string key_getLiveMeetings = "v1/meeting/live";

        public static string key_get_meeting_info = "v1/meeting/get";

        public static string key_get_users_list = "v1/user/list";

        #endregion Zoom Call

    }
}