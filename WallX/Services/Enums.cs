
namespace WallX.Services
{
    public enum ClassCategoryType
    {
        Mathematics = 33,
        Physics = 34,
        Social = 35,
        Chemistry = 36,
        Others = 37
    }

    public enum ClassScheduleType
    {
        OneTimeClass = 4,
        RecurringClass = 7,
        SingleDayMultipleClass = 5
    }

    public enum RecurranceClassFrequencyType
    {
        Alternatedays = 31,
        Daily = 27,
        Weekly = 28,
        Monthly = 29,
        Yearly = 30
    }

    public enum ClassRescheduleTypes
    {
        RescheduleThisClass,
        RescheduleAllTheClass,
        CancelThisClass,
        CancelAllClass,
        SkipForNow
    }

    public enum AttachmentType
    {
        Media_Image = 38,
        Media_Video = 39,
        Media_Pdf = 40,
        Capture = 41,
        Screen_Record = 43,
        Sticky = 44,
        Note = 45,
        Decision = 46,
        Excel = 50,
        Word = 51,
        Power_Point = 52,
        Task = 53, 
        Audio = 42
    }

    public enum YesOrNoPickList
    {
        Yes = 15,
        No = 16
    }

    public enum NxgInputType
    {
        Keyboard,
        Hand
    }

    public enum ConstKey
    {
        EmailId,
        EmailPWD,
        WallXBaseUrl,
        WallXUserName,
        WallXPassword,
        RtServer,
        RtWallX,
        RtBoardAnnotation,
        RtLibraryThumb
    }

    public enum ModuleName
    {
        Employees,
        Participants,
        ParticipantUsers,
        Class,
        Agendas,
        LibraryThumbs,
        BoardAnnotations,
        ImageAnnotations
    }
}
