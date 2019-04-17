using WallX.Helpers;
using NextGen.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace WallX.Services
{
    internal class Assets
    {
        public static object MethodInvokation(object methodObject, string methodName, object[] methodParams)
        {
            object resObject = null;
            try
            {
                if (methodObject != null)
                {
                    MethodInfo method = methodObject.GetType().GetMethod(methodName);
                    if (method != null)
                        resObject = method.Invoke(methodObject, methodParams);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
            return resObject;
        }

        public static BitmapImage GetBitmapImage(object typeObject, string attachment_Uid)
        {
            if (string.IsNullOrWhiteSpace(attachment_Uid))
                return null;

            List<string> filesList = Directory.GetFiles(Constants.AttachmentResources).ToList();

            if (!attachment_Uid.ToLower().StartsWith("file") || typeObject is ImageAnnotations)
                attachment_Uid = filesList.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(attachment_Uid)));

            string filePath = Constants.AttachmentResourceThumbs + Path.GetFileNameWithoutExtension(attachment_Uid) + ".png"; // pending by sat

            if (!(typeObject is ImageAnnotations) && !File.Exists(filePath))
                GenerateThumb.GenerateThumbnail(filePath, Constants.AttachmentResourceThumbs, ".png");

            return NxgUtilities.GetBitmapImageFromFile(!(typeObject is ImageAnnotations) ? filePath : attachment_Uid, true);
        }

        public static string GetResourcePath(string value, bool isForEmployee = false)
        {
            if (!Directory.Exists(Constants.AttachmentResources))
                Directory.CreateDirectory(Constants.AttachmentResources);

            string filePath = !string.IsNullOrWhiteSpace(value) ? Directory.GetFiles(Constants.AttachmentResources).FirstOrDefault(s => Path.GetFileNameWithoutExtension(s).EndsWith(Path.GetFileNameWithoutExtension(value))) : null;

            return File.Exists(filePath) ? filePath : isForEmployee && !string.IsNullOrWhiteSpace(value) && value.Any(s => char.IsLetter(s)) ? Constants.InternalEmailIconsPath + Path.GetFileNameWithoutExtension(value)[0].ToString().ToUpper() + ".png" : Constants.InternalEmailIconsPath + "A.png";
        }

        public static string GetAttachmentFile(string attachment)
        {
            string returnValue = null;
            if (!string.IsNullOrWhiteSpace(attachment))
            {
                string[] values = attachment.Split(new string[] { "__@__" }, StringSplitOptions.RemoveEmptyEntries);
                returnValue = values != null && values.Count() == 2 ? "[{\"Name\":\"" + values[0] + "\",\"UUID\":\"" + values[1] + "\"}]" : null;
            }
            return returnValue;
        }

        public static void SetAttachmentFile(object typeObject, string attachment, ref AttachedFile AttachmentFile)
        {
            if (!string.IsNullOrWhiteSpace(attachment))
            {
                attachment = attachment.Replace("[{\"Name\":\"", "").Replace("\",\"UUID\":\"", "__@__").Replace("\"}]", "");

                string[] values = attachment.Split(new string[] { "__@__" }, StringSplitOptions.RemoveEmptyEntries);
                if (values != null && values.Count() == 2)
                    AttachmentFile = new AttachedFile { Name = values[0], UUID = values[1] };

                string AttachmentLocalPath = GetResourcePath(AttachmentFile.UUID);
            }
        }

        public static DateTime GetLocalTime(DateTime dateTime)
        {
            return dateTime != null && dateTime != default(DateTime) ? dateTime.ToLocalTime() : dateTime;
        }

        public static DateTime GetUniversalTime(DateTime dateTime)
        {
            return dateTime != null && dateTime != default(DateTime) ? dateTime.ToUniversalTime() : dateTime;
        }
    }

    public enum CrudActions
    {
        Create,
        Update,
        Delete
    }
}
