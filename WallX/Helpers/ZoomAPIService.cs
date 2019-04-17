using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NextGen.Controls;

namespace WallX.Helpers
{
    #region Info urls

    //working with postman about zoom
    //https://developer.zoom.us/blog/using-zoom-apis-version-2-with-postman/

    //api documentation
    //https://zoom.github.io/api/#create-a-meeting

    //https://support.zoom.us/hc/en-us/articles/201363033-REST-User-API?mobile_site=true
    //https://zoom.github.io/api/#the-user-object
    //https://developer.zoom.us/playground/#/Meetings/meetingCreate
    //https://developer.zoom.us/playground-v1/
    //https://zoom.github.io/api-v1/#list-users

    #endregion

    #region Models

    public class ZoomUserMini
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public int type { get; set; }
        public long pmi { get; set; }
        public string timezone { get; set; }
        public int verified { get; set; }
        public string dept { get; set; }

        private DateTime _created_at;
        public DateTime created_at
        {
            get { return _created_at; }
            set
            {
                if (value != null)
                    _created_at = value.ToLocalTime();
            }
        }

        private DateTime _last_login_time;
        public DateTime last_login_time
        {
            get { return _last_login_time; }
            set
            {
                if (value != null)
                    _last_login_time = value.ToLocalTime();
            }
        }
        public string last_client_version { get; set; }
    }

    public class ZoomUser : ZoomUserMini
    {
        public bool disable_chat { get; set; }
        public bool enable_e2e_encryption { get; set; }
        public bool enable_silent_mode { get; set; }
        public bool disable_group_hd { get; set; }
        public bool disable_recording { get; set; }
        public bool enable_cmr { get; set; }
        public bool enable_auto_recording { get; set; }
        public bool enable_cloud_auto_recording { get; set; }
        public int meeting_capacity { get; set; }
        public bool enable_webinar { get; set; }
        public int webinar_capacity { get; set; }
        public bool enable_large { get; set; }
        public int large_capacity { get; set; }
        public bool disable_feedback { get; set; }
        public bool disable_jbh_reminder { get; set; }
        public bool enable_breakout_room { get; set; }
        public string token { get; set; }
        public string zpk { get; set; }
    }

    public class ZoomMeeting
    {
        public string uuid { get; set; }
        public uint id { get; set; }
        public string host_id { get; set; }
        public string topic { get; set; }
        public int type { get; set; }

        private DateTime _start_time;
        public DateTime start_time
        {
            get { return _start_time; }
            set
            {
                if (value != null)
                    _start_time = value.ToLocalTime();
            }
        }
        public int duration { get; set; }
        public string timezone { get; set; }

        private DateTime _created_at;
        public DateTime created_at
        {
            get { return _created_at; }
            set
            {
                if (value != null)
                    _created_at = value.ToLocalTime();
            }
        }
        public string start_url { get; set; }
        public string join_url { get; set; }
        public string password { get; set; }
        public string h323_password { get; set; }
        public string agenda { get; set; }
        public ZoomMeetingSettings Settings { get; set; }
        public string status { get; set; }
    }

    public class ZoomMeetingSettings
    {
        public bool host_video { get; set; }
        public bool participant_video { get; set; }
        public bool panelists_video { get; set; }
        public bool practice_session { get; set; }
        public bool hd_video { get; set; }
        public bool cn_meeting { get; set; }
        public bool in_meeting { get; set; }
        public bool join_before_host { get; set; }
        public bool mute_upon_entry { get; set; }
        public bool watermark { get; set; }
        public bool use_pmi { get; set; }
        public int approval_type { get; set; }
        public int registration_type { get; set; }
        public string audio { get; set; }
        public string auto_recording { get; set; }
        public bool enforce_login { get; set; }
        public string enforce_login_domains { get; set; }
        public string alternative_hosts { get; set; }
        public bool close_registration { get; set; }
        public bool show_share_button { get; set; }
        public bool allow_multiple_devices { get; set; }
    }

    public class ZoomPanalist
    {
        public string id { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string join_url { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string registrant_id { get; set; }
        public string topic { get; set; }

        private DateTime _start_time;
        public DateTime start_time
        {
            get { return _start_time; }
            set
            {
                if (value != null)
                    _start_time = value.ToLocalTime();
            }
        }
    }

    public class SignInCredential
    {
        public SignInCredential()
        {
            email = Constants.ZoomUserId;
            password = Constants.ZoomUserPwd;
        }

        public string email { get; set; }
        public string password { get; set; }
    }

    public class ApiCredential
    {
        public ApiCredential()
        {
            api_key = Constants.ZoomAPI;
            api_secret = Constants.ZoomSecret;
            data_type = "json";
            page_size = 30;
            page_number = 1;
        }

        public string api_key { get; set; }
        public string api_secret { get; set; }
        public string data_type { get; set; }
        public int page_size { get; set; }
        public int page_number { get; set; }
    }

    #endregion

    public class ZoomAPIService
    {
        #region Variables

        private static string baseUrl = "https://api.zoom.us/v2/";

        #endregion

        #region Sample Calls

        //List<Model.ZoomUserMini> zoomUsersList = Model.ZoomAPIService.getUsers();
        //Model.ZoomUserMini zoomUser = Model.ZoomAPIService.addUser("appalanaidu.p@thresholdsoft.com", "naidu", "p a", "the@1234");
        //List<Model.ZoomMeeting> zoomMeetingsList = Model.ZoomAPIService.getMeetings("bharath.b@nextgenmultitouch.com");
        //Model.ZoomMeeting zoomMeeting = Model.ZoomAPIService.addMeeting("bharath.b@nextgenmultitouch.com", "Test Meet Check", DateTime.Now, 60);
        //Model.ZoomMeeting zoomMeeting = Model.ZoomAPIService.getMeeting(3890684225);
        //Model.ZoomAPIService.getRegistrants(zoomMeeting.id);
        //List<Model.ZoomMeeting> zoomWebinarsList = Model.ZoomAPIService.getWebinars("bharath.b@nextgenmultitouch.com");
        //Model.ZoomMeeting zoomWebinar = Model.ZoomAPIService.addWebinar("bharath.b@nextgenmultitouch.com", "Test Webinar Check", DateTime.Now, 60);
        //List<Model.ZoomPanalist> zoomPanalistsList = Model.ZoomAPIService.getPanalists(803912256);
        //Model.ZoomPanalist zoomPanalist = Model.ZoomAPIService.addPanalist(803912256, "Sathish", "satish.c@threshold.com");
        //List<Model.ZoomPanalist> zoomAttendeesList = Model.ZoomAPIService.getAttendees(803912256);
        //Model.ZoomPanalist zoomAttendee = Model.ZoomAPIService.addAttendee(803912256, "Nagaraju", "R V", "nagaraju.rv@thresholdsoft.com");
        //List<Model.ZoomPanalist> zoomAttendeesList = Model.ZoomAPIService.getAttendees(803912256);
        //List<Model.ZoomPanalist> zoomPanalistsList = Model.ZoomAPIService.getPanalists(803912256);

        #endregion

        #region Methods

        //get users list of zoom account
        public static List<ZoomUserMini> getUsers()
        {
            try
            {
                string response = RequestZoomAPIData("Get", "users?status=active&page_size=30&page_number=1");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("users"))
                    return JsonConvert.DeserializeObject<List<ZoomUserMini>>(data["users"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //add user for zoom account
        public static ZoomUserMini addUser(string email, string fname, string lname, string password)
        {
            try
            {
                ZoomUserMini user = new ZoomUserMini { email = email, type = 1, first_name = fname, last_name = lname, password = password };
                string jsonValue = "{\"action\":\"create\", \"user_info\":" + JsonConvert.SerializeObject(user) + "}";
                string response = RequestZoomAPIData("Post", "users", jsonValue);
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomUserMini>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get meetings list of zoom user
        public static List<ZoomMeeting> getMeetings(string email)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "users/" + email + "/meetings");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("meetings"))
                    return JsonConvert.DeserializeObject<List<ZoomMeeting>>(data["meetings"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //add meeting for zoom user
        public static ZoomMeeting addMeeting(string email, string topic, DateTime startTime, int duration, string password = null, string agenda = null)
        {
            try
            {
                ZoomMeetingSettings settings = new ZoomMeetingSettings { host_video = true, participant_video = true, audio = "both", approval_type = 1 };

                ZoomMeeting meeting = new ZoomMeeting { topic = topic, type = 2, start_time = startTime, duration = duration, timezone = "Asia/Culcutta", password = password, agenda = agenda, Settings = settings };

                string response = RequestZoomAPIData("Post", "users/" + email + "/meetings", JsonConvert.SerializeObject(meeting));
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomMeeting>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get meeting for zoom user
        public static ZoomMeeting getMeeting(uint meetingId)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "meetings/" + meetingId);
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomMeeting>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get registrants list for zoom meeting
        public static ZoomMeeting getRegistrants(int meetingId)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "meetings/" + meetingId + "/registrants");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                //if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("meetings"))
                //return JsonConvert.DeserializeObject<List<ZoomMeeting>>(data["meetings"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get webinars list of zoom user
        public static List<ZoomMeeting> getWebinars(string email)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "users/" + email + "/webinars");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("webinars"))
                    return JsonConvert.DeserializeObject<List<ZoomMeeting>>(data["webinars"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //add webinar for zoom user
        public static ZoomMeeting addWebinar(string email, string topic, DateTime startTime, int duration, string password = null, string agenda = null)
        {
            try
            {
                ZoomMeetingSettings settings = new ZoomMeetingSettings { host_video = true, panelists_video = true, audio = "both", approval_type = 1 };

                ZoomMeeting meeting = new ZoomMeeting { topic = topic, type = 2, start_time = startTime, duration = duration, timezone = "Asia/Culcutta", password = password, agenda = agenda, Settings = settings };

                string response = RequestZoomAPIData("Post", "users/" + email + "/webinars", JsonConvert.SerializeObject(meeting));
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomMeeting>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get panalists list of zoom webinar
        public static List<ZoomPanalist> getPanalists(int webinarId)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "webinars/" + webinarId + "/panelists");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("panelists"))
                    return JsonConvert.DeserializeObject<List<ZoomPanalist>>(data["panelists"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //add panalist for zoom webinar
        public static ZoomPanalist addPanalist(int webinarId, string name, string email)
        {
            try
            {
                ZoomPanalist panalist = new ZoomPanalist { name = name, email = email };
                //string jsonValue = "{\"panelists\":[{\"name\":\"" + name + "\", \"email\":\"" + email + "\"}]}";
                string jsonValue = "{\"panelists\":" + JsonConvert.SerializeObject(new List<ZoomPanalist> { panalist }) + "}";
                string response = RequestZoomAPIData("Post", "webinars/" + webinarId + "/panelists", jsonValue);
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomPanalist>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //get attendees list of zoom webinar
        public static List<ZoomPanalist> getAttendees(int webinarId)
        {
            try
            {
                string response = RequestZoomAPIData("Get", "webinars/" + webinarId + "/registrants");
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (data != null && data.ContainsKey("total_records") && Convert.ToInt32(data["total_records"].ToString()) > 0 && data.ContainsKey("registrants"))
                    return JsonConvert.DeserializeObject<List<ZoomPanalist>>(data["registrants"].ToString());
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //add attendee for zoom webinar
        public static ZoomPanalist addAttendee(int webinarId, string fname, string lname, string email)
        {
            try
            {
                ZoomPanalist panalist = new ZoomPanalist { first_name = fname, last_name = lname, email = email };
                string response = RequestZoomAPIData("Post", "webinars/" + webinarId + "/panelists", JsonConvert.SerializeObject(panalist));
                if (!string.IsNullOrWhiteSpace(response))
                    return JsonConvert.DeserializeObject<ZoomPanalist>(response);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        //Post data to zoom api
        private static string RequestZoomAPIData(string method, string url, string jsonData = null, List<KeyValuePair<string, string>> formData = null)
        {
            try
            {
                List<KeyValuePair<string, string>> apiValues = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiI2QzV4a0tTb1FwaVpaRXAwdVJDcjhBIiwiZXhwIjoxNDk2MDkxOTY0MDAwfQ.pSANMbXVmB031aavyugcYitPsx36-sLbsR5hMFSt01I") };

                return NextGen.Controls.EMailer.HttpGetOrPost(method, baseUrl + url, jsonData, formData, headerData: apiValues);
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return null;
        }

        #endregion

        #region Old Calls

        public static ZoomMeeting GetMeetingInfo(string meetingId, string Host_id)
        {
            string apiCredential = JsonConvert.SerializeObject(new ApiCredential());
            List<KeyValuePair<string, string>> apiValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(apiCredential).ToList();
            apiValues.Add(new KeyValuePair<string, string>("host_id", Host_id));
            apiValues.Add(new KeyValuePair<string, string>("id", meetingId));
            string apiResponse = EMailer.HttpGetOrPost("Post", Constants.key_baseUrlZoomRestServices + Constants.key_get_meeting_info, null, apiValues);
            return JsonConvert.DeserializeObject<ZoomMeeting>(apiResponse);
        }

        #endregion
    }
}
