using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using NextGen.Controls.SQLite;
using NextGen.Controls.SQLite.Attributes;
using System.Windows.Controls;
using Newtonsoft.Json;
using NextGen.Controls;

namespace WallX.Services
{
    public class Employees
    {
        [PrimaryKey, AutoIncrement, Column("pk_id"), JsonIgnore]
        public int EmployeeId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        [NotNull, Column("first_name", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("First Name")]
        public string FirstName { get; set; }

        [Column("middle_name", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Middle Name")]
        public string MiddleName { get; set; }

        [Column("last_name", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Last Name")]
        public string LastName { get; set; }

        [NotNull, Column("email", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Email")]
        public string Email { get; set; }

        [Column("phone", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Phone")]
        public string Phone { get; set; }

        private string _image;
        [Column("image", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Image")]
        public string Image { get { return Assets.GetResourcePath(!string.IsNullOrWhiteSpace(_image) ? _image : FirstName, true); } set { _image = value; } }

        //private string _name;
        [Ignore, JsonIgnore]
        public string Name { get { return FirstName + " " + MiddleName + " " + LastName; }}

        [Ignore, JsonIgnore]
        public bool IsSelected { get; set; }
    }

    public class Participants
    {
        [PrimaryKey, AutoIncrement, Column("pk_id"), JsonIgnore]
        public int ParticipantId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        [NotNull, Column("is_optional", Default = 0, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Is Optional")]
        public bool IsOptional { get; set; }

        [NotNull, Column("is_attended", Default = 0, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Is Attended")]
        public bool IsAttended { get; set; }

        [NotNull, Column("is_organizer", Default = 0, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Is Organizer")]
        public bool IsOrganizer { get; set; }

        [NotNull, Column("employee_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int EmployeeId { get { return Convert.ToInt32(EmployeePKID); } set { EmployeePKID = value.ToString(); } }

        [Ignore, JsonProperty("Employee pk_id")]
        public string EmployeePKID { get; set; }

        [NotNull, Column("class_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassId { get { return Convert.ToInt32(ClassPKID); } set { ClassPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class pk_id")]
        public string ClassPKID { get; set; }

        [Ignore, JsonIgnore]
        public Employees Employee { get; set; }
    }

    [Serializable]
    public class DateTimeFormat
    {
        [Ignore, JsonIgnore]
        public DateTime StartTime { get; set; }

        [Ignore, JsonProperty("Start Time")]
        public DateTime? StartTimeServer { get { return StartTime; } set { StartTime = value ?? default(DateTime); } }

        [NotNull, Column("start_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string StartTimeInfo
        {
            get { return Assets.GetUniversalTime(StartTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    StartTime = Assets.GetLocalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime EndTime { get; set; }

        [Ignore, JsonProperty("End Time")]
        public DateTime? EndTimeServer { get { return EndTime; } set { EndTime = value ?? default(DateTime); } }

        [NotNull, Column("end_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string EndTimeInfo
        {
            get { return Assets.GetUniversalTime(EndTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    EndTime = Assets.GetLocalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime ActualStartTime { get; set; }

        [Ignore, JsonProperty("Actual Start Time")]
        public DateTime? ActualStartTimeServer { get { return ActualStartTime; } set { ActualStartTime = value ?? default(DateTime); } }

        [NotNull, Column("actual_start_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string ActualStartTimeInfo
        {
            get { return Assets.GetUniversalTime(ActualStartTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    ActualStartTime = Assets.GetLocalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime ActualEndTime { get; set; }

        [Ignore, JsonProperty("Actual End Time")]
        public DateTime? ActualEndTimeServer { get { return ActualEndTime; } set { ActualEndTime = value ?? default(DateTime); } }

        [NotNull, Column("actual_end_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string ActualEndTimeInfo
        {
            get { return Assets.GetUniversalTime(ActualEndTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    ActualEndTime = Assets.GetLocalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime FrequencyStartTime { get; set; }

        [Ignore, JsonProperty("Frequency Start Time")]
        public DateTime? FrequencyStartTimeServer { get { return FrequencyStartTime; } set { FrequencyStartTime = value ?? default(DateTime); } }

        [NotNull, Column("frequency_start_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string FrequencyStartTimeInfo
        {
            get { return Assets.GetUniversalTime(FrequencyStartTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    FrequencyStartTime = Assets.GetLocalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime FrequencyEndTime { get; set; }

        [Ignore, JsonProperty("Frequency End Time")]
        public DateTime? FrequencyEndTimeServer { get { return FrequencyEndTime; } set { FrequencyEndTime = value ?? default(DateTime); } }

        [NotNull, Column("frequency_end_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string FrequencyEndTimeInfo
        {
            get { return Assets.GetLocalTime(FrequencyEndTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    FrequencyEndTime = Assets.GetUniversalTime(Convert.ToDateTime(value));
            }
        }

        [Ignore, JsonIgnore]
        public DateTime PreviousClassEndTime { get; set; }

        [Ignore, JsonIgnore]
        public DateTime NextClassStartTime { get; set; }

        [Ignore, JsonIgnore]
        public string Duration { get { return EndTime.Subtract(StartTime).ToString(); } }

        [Ignore, JsonIgnore]
        public string ActualDuration { get { return ActualEndTime != default(DateTime) ? ActualEndTime.Subtract(ActualStartTime).ToString(@"hh\:mm\:ss") : "00:00:00"; } }
    }

    [Serializable]
    public class Class : DateTimeFormat
    {
        [PrimaryKey, AutoIncrement, Column("pk_id"), JsonIgnore]
        public int ClassId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        [NotNull, Column("class_name", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Name")]
        public string ClassName { get; set; }

        [NotNull, Column("class_category_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassCategoryId { get { return Convert.ToInt32(ClassCategoryPKID); } set { ClassCategoryPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class Category pk_id")]
        public string ClassCategoryPKID { get; set; }

        [NotNull, Column("class_category", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Class Category")]
        public string ClassCategory { get; set; }

        [NotNull, Column("class_type_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassTypeId { get { return Convert.ToInt32(ClassTypePKID); } set { ClassTypePKID = value.ToString(); } }

        [Ignore, JsonProperty("Class Type pk_id")]
        public string ClassTypePKID { get; set; }

        [NotNull, Column("class_type", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Class Type")]
        public string ClassType { get; set; }

        [Column("organizer_mail_id", PropertyAction = PropertyAction.CreateUpdate)]
        public string OrganizerMailId { get; set; }

        [Column("password", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Password")]
        public string Password { get; set; }

        [Column("recurring_class_id", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Recurring Class Id")]
        public string RecurringClassId { get; set; }

        [NotNull, Column("class_frequency_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassFrequencyId { get { return Convert.ToInt32(ClassFrequencyPKID); } set { ClassFrequencyPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class Frequency pk_id")]
        public string ClassFrequencyPKID { get; set; }

        [Column("class_frequency", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Class Frequency")]
        public string ClassFrequency { get; set; }

        [NotNull, Column("unique_class_id", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Unique Class Id")]
        public string UniqueClassId { get; set; }

        [Ignore, JsonIgnore]
        public bool IsFromAdhoc { get; set; }

        [Ignore, JsonIgnore]
        public DateTime ConflictClassStartTime { get; set; }

        [Ignore, JsonIgnore]
        public DateTime ConflictClassEndTime { get; set; }

        [Ignore, JsonIgnore]
        public List<Participants> ParticipantList { get; set; }

        [Ignore, JsonIgnore]
        public List<Agendas> AgendaList { get; set; }

        [Ignore, JsonIgnore]
        public List<LibraryThumbs> LibraryThumbList { get; set; }

        [Ignore, JsonIgnore]
        public List<BoardAnnotations> BoardAnnotationList { get; set; }

        //Zoom
        [Column("zoom_id", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Zoom Id")]
        public string ZoomStartUri { get; set; }

        [Column("zoom_host_id", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Zoom Host Id")]
        public string ZoomHostId { get; set; }

        [Column("is_zoom_started", 0, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Is Zoom Started")]
        public int IsZoomStarted { get; set; }

        [Ignore, JsonIgnore]
        public string ZoomJoinUri { get; set; }
    }

    public class Agendas : DateTimeFormat
    {
        [PrimaryKey, AutoIncrement, Column("pk_id"), JsonIgnore]
        public int AgendaId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }
        
        [NotNull, Column("agenda_name", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Name")]
        public string AgendaName { get; set; }

        [NotNull, Column("class_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassId { get { return Convert.ToInt32(ClassPKID); } set { ClassPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class pk_id")]
        public string ClassPKID { get; set; }

        [Ignore, JsonProperty("Class")]
        public string Class { get; set; }

        [NotNull, Column("employee_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int EmployeeId { get { return Convert.ToInt32(EmployeePKID); } set { EmployeePKID = value.ToString(); } }

        [Ignore, JsonProperty("Employee pk_id")]
        public string EmployeePKID { get; set; }

        [Ignore, JsonProperty("Employee")]
        public string Employee { get; set; }

        [NotNull, Column("employee_email", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public string EmployeeEmail { get; set; }

        [Ignore, JsonIgnore]
        public string EmployeeImage { get { return Assets.GetResourcePath(EmployeeEmail, true); } }

        [Ignore, JsonIgnore]
        internal bool IsSelected { get; set; }

        [Ignore, JsonIgnore]
        public string IsRunning { get; set; }

        [Ignore, JsonIgnore]
        public string IsLast { get; set; }

        [Ignore, JsonIgnore]
        internal int Minutes { get { return !string.IsNullOrWhiteSpace(Duration) ? Convert.ToInt32(TimeSpan.Parse(Duration).TotalMinutes) : 0; } }

        [Ignore, JsonIgnore]
        public List<Participants> Participants { get; set; }

        [Ignore, JsonIgnore]
        public Participants Presenter { get; set; }

        [Ignore, JsonIgnore]
        public int AgendaDuration { get; set; }

        [Ignore, JsonIgnore]
        public string EmployeeName { get; set; }
    }

    [Serializable, Table("board_annotation")]
    public class BoardAnnotations
    {
        [PrimaryKey, AutoIncrement, Column("pk_id"), JsonIgnore]
        public int AnnotationId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        [NotNull, Column("page_index", Default = 0, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Page Index")]
        public int PageIndex { get; set; }

        [NotNull, Column("ink_strokes", StringLength = 300, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Ink Strokes")]
        public string InkStrokes { get; set; }

        [NotNull, Column("total_ink_strokes", StringLength = 300, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Total Ink Strokes")]
        public string TotalInkStrokes { get; set; }

        [NotNull, Column("manipulation", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Manipulation")]
        public string Manipulation { get; set; }

        [NotNull, Column("class_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassId { get { return Convert.ToInt32(ClassPKID); } set { ClassPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class pk_id")]
        public string ClassPKID { get; set; }

        [Ignore, JsonProperty("Class")]
        public string Class { get; set; }

        [Ignore, JsonProperty("Connected Devices")]
        public string ConnectedDevices { get; set; }

        [Ignore, JsonProperty("Devices Modes")]
        public string DevicesModes { get; set; }

        [Ignore, JsonIgnore]
        public StrokeCollection StrokesList { get { return !string.IsNullOrEmpty(InkStrokes) && !InkStrokes.Contains("Margin=") ? ((XamlReader.Parse(InkStrokes) as InkCanvas).Strokes) : new StrokeCollection(); } }

        [Ignore, JsonIgnore]
        public string CanvasName { get { return "Canvas " + (PageIndex + 1).ToString("00"); } }

        [Ignore, JsonIgnore]
        public string Tag { get; set; }

        [Ignore, JsonIgnore]
        public int Index { get; set; }
    }

    [Table("library_thumb")]
    public class LibraryThumbs
    {
        [PrimaryKey, AutoIncrement, Column("library_thumb_id"), JsonIgnore]
        public int LibraryThumbId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        private string _attachment;
        [Column("attachment", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Attachment")]
        public string Attachment
        {
            get { return Assets.GetAttachmentFile(_attachment); }
            set
            {
                _attachment = value;
                AttachedFile attachmentFile = null;
                Assets.SetAttachmentFile(this, _attachment, ref attachmentFile);
                AttachmentFile = attachmentFile;
            }
        }

        private string _textInfo;
        [Column("textinfo", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Text Info")]
        public string TextInfo
        {
            get { return _textInfo; }
            set
            {
                _textInfo = value;
                if (AttachmentType == "Sticky" || (value != null && value.Contains("Sticky")))
                {
                    AttachedFile attachmentFile = null;
                    Assets.SetAttachmentFile(this, _textInfo, ref attachmentFile);
                    AttachmentFile = attachmentFile;
                }
            }
        }

        [Column("stroke_data", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Stroke Data")]
        public string StrokeData { get; set; }

        [Column("url", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Url")]
        public string Url { get; set; }

        [NotNull, Column("attachment_type_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int AttachmentTypeId { get { return Convert.ToInt32(AttachmentTypePKID); } set { AttachmentTypePKID = value.ToString(); } }

        [Ignore, JsonProperty("Attachment Type pk_id")]
        public string AttachmentTypePKID { get; set; }

        [NotNull, Column("attachment_type", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Attachment Type")]
        public string AttachmentType { get; set; }

        [NotNull, Column("participant_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ParticipantId { get { return Convert.ToInt32(ParticipantPKID); } set { ParticipantPKID = value.ToString(); } }

        [Ignore, JsonProperty("Participant pk_id")]
        public string ParticipantPKID { get; set; }

        [Ignore, JsonProperty("Participant")]
        public string Participant { get; set; }

        [NotNull, Column("agenda_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int AgendaId { get { return Convert.ToInt32(AgendaPKID); } set { AgendaPKID = value.ToString(); } }

        [Ignore, JsonProperty("Agenda pk_id")]
        public string AgendaPKID { get; set; }

        [Ignore, JsonProperty("Agenda")]
        public string Agenda { get; set; }

        [NotNull, Column("class_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassId { get { return Convert.ToInt32(ClassPKID); } set { ClassPKID = value.ToString(); } }

        [Ignore, JsonProperty("Class pk_id")]
        public string ClassPKID { get; set; }

        [Ignore, JsonProperty("Class")]
        public string Class { get; set; }

        [Ignore, JsonProperty("Created Time")]
        public DateTime CreatedDateTime { get; set; }

        [NotNull, Column("created_date_time", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate)]
        public string CreatedDateTimeInfo
        {
            get { return Assets.GetLocalTime(CreatedDateTime).ToString(); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    CreatedDateTime = Assets.GetUniversalTime(Convert.ToDateTime(value));
            }
        }

        [Column("assigned_employees", Default = "'01-01-0001 00:00:00'", PropertyAction = PropertyAction.CreateUpdate)]
        public string AssignedEmployeePKIDs { get; set; }

        [Ignore, JsonIgnore]
        public string ParticipantImage { get { return Assets.GetResourcePath(Participant, true); } }

        [Ignore, JsonIgnore]
        public AttachedFile AttachmentFile { get; set; }

        [Ignore, JsonIgnore]
        public string AttachmentName { get { return AttachmentFile != null ? AttachmentFile.Name : null; } }

        [Ignore, JsonIgnore]
        public string AttachmentUid { get { return AttachmentFile != null ? AttachmentFile.UUID : null; } }

        [Ignore, JsonIgnore]
        public string AttachmentLocalPath { get { return Assets.GetResourcePath(AttachmentUid); } }

        [Ignore, JsonIgnore]
        public BitmapImage ThumbnailImage
        {
            get
            {
                if (new List<string> { "capture", "screen_record", "excel", "word", "power_point" }.Contains(AttachmentType.ToLower()) || (AttachmentType.ToLower().StartsWith("media") || AttachmentType.ToLower().StartsWith("file")))
                    return Assets.GetBitmapImage(this, AttachmentUid);
                else
                    return null;
            }
        }

        [Ignore, JsonIgnore]
        public StrokeCollection Strokes { get { return !string.IsNullOrWhiteSpace(StrokeData) && StrokeData.Contains("StrokeCollection") ? XamlReader.Parse(StrokeData) as StrokeCollection : null; } }
    }

    [Table("image_annotation")]
    public class ImageAnnotations
    {
        [PrimaryKey, AutoIncrement, Column("annotation_id"), JsonIgnore]
        public int AnnotationId { get { return Convert.ToInt32(PKID); } set { PKID = value.ToString(); } }

        [Ignore, JsonProperty("pk_id")]
        public string PKID { get; set; }

        [Column("ink_strokes", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Ink Strokes")]
        public string InkStrokes { get; set; }

        [NotNull, Column("total_ink_strokes", StringLength = 300, PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Total Ink Strokes")]
        public string TotalInkStrokes { get; set; }

        [NotNull, Column("manipulation", PropertyAction = PropertyAction.CreateUpdate), JsonProperty("Manipulation")]
        public string Manipulation { get; set; }

        [NotNull, Column("board_annotation_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int BoardAnnotationId { get { return Convert.ToInt32(BoardAnnotationPKID); } set { BoardAnnotationPKID = value.ToString(); } }

        [Ignore, JsonProperty("Board Annotation pk_id")]
        public string BoardAnnotationPKID { get; set; }

        [Ignore, JsonProperty("Board Annotation")]
        public string BoardAnnotation { get; set; }

        [NotNull, Column("library_thumb_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int LibraryThumbId { get { return Convert.ToInt32(LibraryThumbPKID); } set { LibraryThumbPKID = value.ToString(); } }

        [Ignore, JsonProperty("Library Thumb pk_id")]
        public string LibraryThumbPKID { get; set; }

        [Ignore, JsonProperty("Library Thumb")]
        public string LibraryThumb { get; set; }

        [NotNull, Column("class_id", PropertyAction = PropertyAction.CreateUpdate), JsonIgnore]
        public int ClassId { get; set; }
    }

    public class AttachedFile
    {
        public string UUID { get; set; }

        public string Name { get; set; }
    }

    public class MultiClass
    {
        public DateTime from_date_time { get; set; }

        public DateTime to_date_time { get; set; }
    }

    public class RelationObjectsData
    {
        [JsonProperty("Participant Users")]
        public RelObjectDataList<Participants> ParticipantData { get; set; }

        [JsonProperty("Agendas")]
        public RelObjectDataList<Agendas> AgendaData { get; set; }

        [JsonProperty("Library Thumbs")]
        public RelObjectDataList<LibraryThumbs> LibraryThumbData { get; set; }

        [JsonProperty("Board Annotations")]
        public RelObjectDataList<BoardAnnotations> BoardAnnotationData { get; set; }
    }

    public class RelObjectDataList<T>
    {
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("rows")]
        public List<T> Rows { get; set; }
    }
}
