using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using NextGen.Controls.SQLite;
using WallX.Helpers;
using NextGen.Controls;

namespace WallX.Services
{
    public class Service
    {
        public static bool _isServerConnected = false;
        private static NextGen.Controls.Services.Service _apiService = null;

        private static SQLiteConnection _sqLiteCon = null;

        static Service()
        {
            try
            {
                _sqLiteCon = new SQLiteConnection(System.Reflection.Assembly.GetEntryAssembly().GetName().Name, Directory.GetCurrentDirectory(), "the@123");

                _sqLiteCon.CreateTable<Employees>();
                _sqLiteCon.CreateTable<Participants>();
                _sqLiteCon.CreateTable<Class>();
                _sqLiteCon.CreateTable<Agendas>();
                _sqLiteCon.CreateTable<LibraryThumbs>();
                _sqLiteCon.CreateTable<BoardAnnotations>();
                _sqLiteCon.CreateTable<ImageAnnotations>();

                if (Constants.AppLocation.ToLower() == "server" && Utilities.IsInternetAvailable() && ConnectToServer())
                {
                    ConnectToServer();
                    _isServerConnected = true;

                    //if (_isServerConnected)
                    //{
                    //    //string metaData = _apiService.GetMetaData(typeof(StorageObjects), ModuleName.LibraryThumbs.ToString(), true);
                    //    //List<Meetings> moduleData = _apiService.GetModuleDataList<Meetings>(typeof(StorageObjects));
                    //}
                }
            }
            catch (Exception ex) { App.InsertException(ex); }
        }

        public static bool ConnectToServer()
        {
            _apiService = new NextGen.Controls.Services.Service(Constants.MwBaseUrl);
            return _apiService.LoginUser(Constants.MwUserName, Constants.MwPassword);
        }

        /// <summary>
        /// Get all class list for required categories data to display in carousel calendar from db
        /// </summary>
        /// <returns></returns>
        public static List<Class> GetClassList(DateTime dateTime, bool isRelationRequired = false)
        {
            List<Class> classList = null;

            try
            {
                if (!_isServerConnected)
                {
                    classList = _sqLiteCon.Table<Class>().ToList().Where(s => s.StartTime.ToString("yyyy-MM-dd") == dateTime.ToString("yyyy-MM-dd")).OrderBy(s => s.StartTime).ToList();

                    if (isRelationRequired && classList != null && classList.Count > 0)
                    {
                        List<Participants> ParticipantList = _sqLiteCon.Table<Participants>().ToList();
                        List<Agendas> AgendaList = _sqLiteCon.Table<Agendas>().ToList();
                        List<BoardAnnotations> BoardAnnotationList = _sqLiteCon.Table<BoardAnnotations>().ToList();
                        List<LibraryThumbs> LibraryThumbList = _sqLiteCon.Table<LibraryThumbs>().ToList();

                        foreach (Class item in classList)
                        {
                            item.ParticipantList = ParticipantList.Where(s => s.ClassId == item.ClassId).ToList();
                            item.AgendaList = AgendaList.Where(s => s.ClassId == item.ClassId).ToList();
                            item.BoardAnnotationList = BoardAnnotationList.Where(s => s.ClassId == item.ClassId).ToList();
                            item.LibraryThumbList = LibraryThumbList.Where(s => s.ClassId == item.ClassId).ToList();
                        }
                    }
                }
                else
                {
                    classList = _apiService.GetModuleDataList<Class>(typeof(StorageObjects));
                    if (classList != null)
                    {
                        classList = classList.Where(s => s.StartTime.ToLocalTime().Date == dateTime.Date).OrderBy(s => s.StartTime).ToList();

                        if (isRelationRequired)
                        {
                            classList.ForEach(s => s = GetClassData(s));
                        }
                    }
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return classList;
        }

        /// <summary>
        /// return class based on unique id
        /// </summary>
        /// <param name="classUid"></param>
        /// <returns></returns>
        public static List<Class> GetRecurringClassById(string recurringClassId)
        {
            List<Class> classesList = null;
            try
            {
                if (!_isServerConnected && !string.IsNullOrWhiteSpace(recurringClassId))
                    classesList = _sqLiteCon.Table<Class>().ToList().Where(s => s.RecurringClassId == recurringClassId).ToList();
                else
                {
                    classesList = _apiService.GetModuleDataList<Class>(typeof(StorageObjects));
                    if (classesList != null)
                    {
                        classesList = classesList.Where(s => s.RecurringClassId == recurringClassId).ToList();
                    }
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return classesList;
        }

        /// <summary>
        /// Get class data from db using class overview
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        public static Class GetClassData(Class classInfo)
        {
            Class classData = classInfo;

            try
            {
                if (!_isServerConnected)
                {
                    classData.ParticipantList = _sqLiteCon.Table<Participants>().Where(s => s.ClassId == classInfo.ClassId).ToList();
                    classData.AgendaList = _sqLiteCon.Table<Agendas>().Where(s => s.ClassId == classInfo.ClassId).ToList();
                    classData.LibraryThumbList = _sqLiteCon.Table<LibraryThumbs>().Where(s => s.ClassId == classInfo.ClassId).ToList();
                    classData.BoardAnnotationList = _sqLiteCon.Table<BoardAnnotations>().Where(s => s.ClassId == classInfo.ClassId).ToList();
                }
                else
                {
                    RelationObjectsData relationObjectsData = _apiService.GetRelationDataList<RelationObjectsData>(classInfo.ClassId, ModuleName.Class.ToString(), new List<string> { ModuleName.ParticipantUsers.ToString(), ModuleName.Agendas.ToString(), ModuleName.LibraryThumbs.ToString(), ModuleName.BoardAnnotations.ToString() });

                    if (relationObjectsData != null)
                    {
                        classData.ParticipantList = relationObjectsData.ParticipantData.Rows;
                        classData.AgendaList = relationObjectsData.AgendaData.Rows;
                        classData.LibraryThumbList = relationObjectsData.LibraryThumbData.Rows;
                        classData.BoardAnnotationList = relationObjectsData.BoardAnnotationData.Rows;
                    }
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return classData;
        }

        /// <summary>
        /// Insert module data into db
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="insertedId"></param>
        /// <param name="moduleFieldsData"></param>
        /// <param name="classDetails"></param>
        /// <returns></returns>
        public static int InsertOrUpdateDataToDB<T>(T objectData, CrudActions action, int objectId = -1)
        {
            int InsertOrUpdatedId = -1;

            try
            {
                if (!_isServerConnected)
                {
                    switch (action)
                    {
                        case CrudActions.Create:
                            InsertOrUpdatedId = _sqLiteCon.Insert(objectData);
                            break;
                        case CrudActions.Update:
                            InsertOrUpdatedId = _sqLiteCon.Update(objectData);
                            break;
                        case CrudActions.Delete:
                            Class classObjectInfo = objectData as Class;
                            if (classObjectInfo != null)
                            {
                                List<string> filesList = _sqLiteCon.Table<LibraryThumbs>().Where(s => s.ClassId == classObjectInfo.ClassId).Select(s => s.AttachmentLocalPath).ToList();
                                filesList.Where(s => !s.Contains("pack://application:,,,/WallX.Resources")).ToList().ForEach(s => { File.Delete(s); File.Delete(Constants.AttachmentResourceThumbs + Path.GetFileNameWithoutExtension(s) + ".png"); });

                                _sqLiteCon.ExecuteScalar<int>("Delete from participants where class_id = " + classObjectInfo.ClassId);
                                _sqLiteCon.ExecuteScalar<int>("Delete from agendas where class_id = " + classObjectInfo.ClassId);
                                _sqLiteCon.ExecuteScalar<int>("Delete from board_annotation where class_id = " + classObjectInfo.ClassId);
                                _sqLiteCon.ExecuteScalar<int>("Delete from library_thumb where class_id = " + classObjectInfo.ClassId);
                                _sqLiteCon.ExecuteScalar<int>("Delete from image_annotation where class_id = " + classObjectInfo.ClassId);
                            }
                            InsertOrUpdatedId = _sqLiteCon.Delete(objectData);
                            break;
                    }

                    //if (InsertOrUpdatedId == 1 && action != CrudActions.Delete && action != CrudActions.Update)
                    //    InsertOrUpdatedId = _sqLiteCon.ExecuteScalar<int>("SELECT last_insert_rowid()");
                }
                else
                {
                    switch (action)
                    {
                        case CrudActions.Create:
                            InsertOrUpdatedId = _apiService.InsertOrUpdateDataToDB(typeof(StorageObjects), objectData, NextGen.Controls.Services.CrudAction.Create);
                            break;
                        case CrudActions.Update:
                            InsertOrUpdatedId = _apiService.InsertOrUpdateDataToDB(typeof(StorageObjects), objectData, NextGen.Controls.Services.CrudAction.Update);
                            break;
                        case CrudActions.Delete:
                            InsertOrUpdatedId = _apiService.DeleteModuleItem(objectData.GetType().Name, objectId) ? 1 : 0;
                            break;
                    }
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return InsertOrUpdatedId;
        }

        /// <summary>
        /// Get relation data list data from db using class id
        /// First get relation data of a class object
        /// Second get meta data of relative table
        /// Third join relation data & metadata, then post to server
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        public static List<T> GetModuleDataList<T>(Class classData, int subRelationId = -1)
        {
            List<T> objectsList = null;
            try
            {
                ModuleName moduleName = (ModuleName)Enum.Parse(typeof(ModuleName), typeof(T).Name);
                if (!_isServerConnected)
                {
                    switch (moduleName)
                    {
                        case ModuleName.Employees:
                            objectsList = (List<T>)Convert.ChangeType(_sqLiteCon.Table<Employees>().ToList(), typeof(List<T>));
                            break;
                        case ModuleName.Participants:
                            break;
                        case ModuleName.Class:
                            break;
                        case ModuleName.Agendas:
                            break;
                        case ModuleName.LibraryThumbs:
                            objectsList = (List<T>)Convert.ChangeType(_sqLiteCon.Table<LibraryThumbs>().ToList(), typeof(List<T>));
                            if (classData != null)
                                objectsList = objectsList.Where(s => (s as LibraryThumbs).ClassId == classData.ClassId).ToList();
                            break;
                        case ModuleName.BoardAnnotations:
                            objectsList = (List<T>)Convert.ChangeType(_sqLiteCon.Table<BoardAnnotations>().ToList(), typeof(List<T>));
                            if (classData != null)
                                objectsList = objectsList.Where(s => (s as BoardAnnotations).ClassId == classData.ClassId).ToList();
                            break;
                        case ModuleName.ImageAnnotations:
                            objectsList = (List<T>)Convert.ChangeType(_sqLiteCon.Table<ImageAnnotations>().ToList(), typeof(List<T>));
                            if (classData != null && subRelationId > 0)
                                objectsList = objectsList.Where(s => (s as ImageAnnotations).ClassId == classData.ClassId && (s as ImageAnnotations).BoardAnnotationId == subRelationId).ToList();
                            else if (classData != null)
                                objectsList = objectsList.Where(s => (s as ImageAnnotations).ClassId == classData.ClassId).ToList();
                            break;
                    }
                }
                else
                {
                    switch (moduleName)
                    {
                        case ModuleName.Employees:
                            objectsList = (List<T>)Convert.ChangeType(_apiService.GetModuleDataList<Employees>(typeof(StorageObjects)), typeof(List<T>));
                            break;
                        case ModuleName.Participants:
                            break;
                        case ModuleName.Class:
                            break;
                        case ModuleName.Agendas:
                            break;
                        case ModuleName.LibraryThumbs:
                            objectsList = (List<T>)Convert.ChangeType(_apiService.GetModuleDataList<LibraryThumbs>(typeof(StorageObjects)), typeof(List<T>));
                            if (classData != null)
                                objectsList = objectsList.Where(s => (s as LibraryThumbs).ClassId == classData.ClassId).ToList();
                            break;
                        case ModuleName.BoardAnnotations:
                            objectsList = (List<T>)Convert.ChangeType(_apiService.GetModuleDataList<BoardAnnotations>(typeof(StorageObjects)), typeof(List<T>));
                            if (classData != null)
                                objectsList = objectsList.Where(s => (s as BoardAnnotations).ClassId == classData.ClassId).ToList();
                            break;
                        case ModuleName.ImageAnnotations:
                            objectsList = (List<T>)Convert.ChangeType(_apiService.GetModuleDataList<ImageAnnotations>(typeof(StorageObjects)), typeof(List<T>));
                            if (classData != null && subRelationId > 0)
                                objectsList = objectsList.Where(s => (s as ImageAnnotations).BoardAnnotationId == subRelationId).ToList();
                            break;
                    }
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return objectsList;
        }

        /// <summary>
        /// Upload file to server
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        public static string UploadFile(string file, bool isFromPdfGeneration = false)
        {
            try
            {
                if (!_isServerConnected)
                {
                    List<AttachedFile> dataInfo = null;

                    if (!isFromPdfGeneration)
                    {
                        dataInfo = new List<AttachedFile>();
                        dataInfo.Add(new AttachedFile { UUID = Guid.NewGuid().ToString(), Name = Path.GetFileName(file) });
                    }
                    return dataInfo != null && dataInfo.Count > 0 ? dataInfo.FirstOrDefault().Name + "__@__" + dataInfo.FirstOrDefault().UUID : null;
                }
                else
                {
                    return _apiService.UploadFile(new List<string> { file }, "Library Thumbs");
                }
            }
            catch (Exception ex) { App.InsertException(ex); }

            return string.Empty;
        }
    }
}
