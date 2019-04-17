using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WallX.Helpers;
using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using System.IO;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Ink;
using GalaSoft.MvvmLight.Messaging;
using NextGen.Controls;

namespace WallX.Helpers
{
    #region Models

    public class RtBoardAnnotation
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        public string MeetingId { get; set; }
        public int PageIndex { get; set; }
        public string Manipulation { get; set; }
        public string UserId { get; set; }
        public string Strokes { get; set; }
        public string SenderId { get; set; }
        public string SenderType { get; set; }

    }

    public class RtLibraryThumb
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        public string MeetingId { get; set; }
        public int PageIndex { get; set; }
        public string UserId { get; set; }
        public string BinaryData { get; set; }
        public string FileId { get; set; }
        public string FileType { get; set; }
    }

    #endregion

    public class RethinkService
    {
        private static RethinkDB _rethinkDb = RethinkDB.R;
        private string _rethinkServer = Constants.RethinkServer;
        private string _rethinkDatabase = Constants.RethinkDatabase;
        private string _rethinkAnnotations = Constants.RethinkAnnotations;
        private string _rethinkResources = Constants.RethinkResources;
        public static string _macAddress = SystemDetails.GetMacAddress();

        private Connection _rethinkCon = null;
        private InkCanvas _boardInkCanvas = null;
        private InkCanvas _guestInkCanvas = null;
        private string _meetingId = string.Empty;

        public RethinkService(string meetingId, InkCanvas inkCanv, InkCanvas guestInkCanv)
        {
            try
            {
                _meetingId = meetingId;
                _boardInkCanvas = inkCanv;
                _guestInkCanvas = guestInkCanv;
                _rethinkCon = _rethinkDb.Connection().Hostname(_rethinkServer).Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
                if (_rethinkCon != null && _rethinkCon.Open)
                {
                    CreateDatabase(_rethinkDatabase);
                    //GetDefaultBoardData();
                    GetDataChanges();
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (_rethinkCon != null && _rethinkCon.Open)
                {
                    _rethinkCon.Close(true);
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        protected void CreateDatabase(string dbName)
        {
            try
            {
                var exists = _rethinkDb.DbList().Contains(db => db == dbName).Run(_rethinkCon);
                if (!exists)
                {
                    _rethinkDb.DbCreate(dbName).Run(_rethinkCon);
                    _rethinkDb.Db(dbName).Wait_().Run(_rethinkCon);

                    foreach (string tname in new List<string> { "RtBoardAnnotation", "RtLibraryThumb" })
                    {
                        _rethinkDb.Db(dbName).TableCreate(tname).Run(_rethinkCon);
                        _rethinkDb.Db(dbName).Table(tname).Wait_().Run(_rethinkCon);

                        _rethinkDb.Db(dbName).Table(tname).IndexCreate("MeetingId").Run(_rethinkCon);
                        _rethinkDb.Db(dbName).Table(tname).IndexWait("MeetingId").Run(_rethinkCon);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void GetDefaultBoardData()
        {
            try
            {
                if (_rethinkCon != null && _rethinkCon.Open)
                {
                    var strokesData = _rethinkDb.Db(_rethinkDatabase).Table(_rethinkAnnotations).Filter(k => k.G("MeetingId").Eq(_meetingId)).Run(_rethinkCon);
                    var resourcesData = _rethinkDb.Db(_rethinkDatabase).Table(_rethinkResources).Filter(k => k.G("MeetingId").Eq(_meetingId)).Run(_rethinkCon);

                    NxgUtilities.CreateDirectory(Constants.AttachmentResources + _meetingId);
                    foreach (var data in resourcesData.BufferedItems)
                    {
                        RtLibraryThumb resource = JsonConvert.DeserializeObject<RtLibraryThumb>(Convert.ToString(data));
                        string filePath = Constants.AttachmentResources + _meetingId + "/" + resource.FileId + ".png";
                        if (!File.Exists(filePath))
                            NxgUtilities.GetBitmapFromBytes(Convert.FromBase64String(resource.BinaryData), filePath);
                    }

                    if (strokesData.BufferedItems.Count > 0)
                    {
                        foreach (var item in strokesData.BufferedItems)
                        {
                            RtBoardAnnotation annotations = JsonConvert.DeserializeObject<RtBoardAnnotation>(Convert.ToString(item));
                            if (annotations != null && !string.IsNullOrWhiteSpace(annotations.Strokes) && annotations.MeetingId == _meetingId)
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        StrokeCollection strokes = XamlReader.Parse(annotations.Strokes) as StrokeCollection;
                                        _guestInkCanvas.Strokes = new StrokeCollection(strokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) != _macAddress).ToList());
                                        _boardInkCanvas.Strokes = new StrokeCollection(strokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == _macAddress).ToList());
                                    }
                                    catch (Exception)
                                    {
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public void AddorUpdateStrokeDataintoDB(string senderType, string strokes, string manipulation = null, int annoId = -1, int pageId = 0)
        {
            try
            {
                if (_rethinkCon != null && _rethinkCon.Open)
                {
                    RtBoardAnnotation annotations = new RtBoardAnnotation { MeetingId = _meetingId, UserId = _macAddress, SenderType = senderType, PageIndex = pageId };
                    annotations.SenderId = annoId.ToString();
                    annotations.Strokes = strokes;
                    annotations.Manipulation = manipulation;

                    Cursor<RtBoardAnnotation> allRows = _rethinkDb.Db(_rethinkDatabase).Table(_rethinkAnnotations)
                        .GetAll(annotations.MeetingId)[new { index = nameof(annotations.MeetingId) }]
                        .Run<RtBoardAnnotation>(_rethinkCon);
                    List<RtBoardAnnotation> annotationsList = allRows.ToList();

                    if (annotationsList != null && annotationsList.Count > 0 && annotationsList.Any(s => s.SenderId == Convert.ToString(annoId)))
                    {
                        string annoData = annoId == -1 ? annotationsList.First().Id : annotationsList.FirstOrDefault(s => s.SenderId == Convert.ToString(annoId)).Id;
                        _rethinkDb.Db(_rethinkDatabase).Table(_rethinkAnnotations).Get(annoData).Update(annotations).RunResult(_rethinkCon);
                    }
                    else
                    {
                        var result = _rethinkDb.Db(_rethinkDatabase).Table(_rethinkAnnotations).Insert(annotations).RunResult(_rethinkCon);
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }

        public async void GetDataChanges()
        {
            try
            {
                var changeCursor = await _rethinkDb.Db(_rethinkDatabase).Table(_rethinkAnnotations).Filter(row => row.G("MeetingId").Eq(_meetingId)).Changes().RunChangesAsync<object>(_rethinkCon);

                while (await changeCursor.MoveNextAsync())
                {
                    if (_rethinkCon != null && _rethinkCon.Open)
                    {
                        RtBoardAnnotation strokesData = JsonConvert.DeserializeObject<RtBoardAnnotation>(Convert.ToString(changeCursor.Current.NewValue));
                        if (strokesData.MeetingId == _meetingId && strokesData.UserId != null && Convert.ToString(strokesData.UserId) != Convert.ToString(_macAddress))
                        {
                            if (strokesData.SenderType == "InkCanvas")
                            {
                                StrokeCollection guestInk = XamlReader.Parse(strokesData.Strokes) as StrokeCollection;
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    _guestInkCanvas.Strokes.Remove(new StrokeCollection(_guestInkCanvas.Strokes.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == strokesData.UserId).ToList()));
                                    _guestInkCanvas.Strokes.Add(new StrokeCollection(guestInk.Where(s => Convert.ToString(s.GetPropertyData(s.GetPropertyDataIds()[0])) == strokesData.UserId).ToList()));
                                });
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "InkCanvasManipulated")
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    (_guestInkCanvas.Parent as Canvas).RenderTransform = XamlReader.Parse(strokesData.Manipulation) as System.Windows.Media.MatrixTransform;
                                });
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "InkCanvasAdded")
                            {
                                Messenger.Default.Send(strokesData.Strokes, "Add Canvas");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "InkCanvasDeleted")
                            {
                                Messenger.Default.Send("DeletePageFromOthers", "Delete Canvas");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "InkCanvasSelectionChanged")
                            {
                                Messenger.Default.Send(strokesData.PageIndex, "ChangePage");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "ChildAdded")
                            {
                                Messenger.Default.Send(new KeyValuePair<string, string>(strokesData.Manipulation, strokesData.SenderId), "AddColBoardItem");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "ChildUpdated")
                            {
                                StrokeCollection guestInkCanvas = XamlReader.Parse(strokesData.Strokes) as StrokeCollection;
                                Messenger.Default.Send(new KeyValuePair<object, KeyValuePair<string, string>>(guestInkCanvas, new KeyValuePair<string, string>(strokesData.SenderId, strokesData.UserId)), "UpdateColBoardItem");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "ChildManipulated")
                            {
                                Messenger.Default.Send(new KeyValuePair<object, KeyValuePair<string, string>>(strokesData.Manipulation, new KeyValuePair<string, string>(strokesData.SenderId, strokesData.UserId)), "UpdateColBoardItem");
                            }
                            //else if (Convert.ToString(strokesData.SenderType) == "canv_Undo")
                            //{
                            //    Messenger.Default.Send("canv_Undo", "UndoRedo");
                            //}
                            //else if (Convert.ToString(strokesData.SenderType) == "canv_Redo")
                            //{
                            //    Messenger.Default.Send("canv_Redo", "UndoRedo");
                            //}
                            else if (Convert.ToString(strokesData.SenderType) == "ClearBoard")
                            {
                                Messenger.Default.Send(strokesData.Strokes, "Clear Board");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "DeleteBoardChild")
                            {
                                Messenger.Default.Send(strokesData.Strokes, "RemoveBoardItem");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "BackgroundChanged")
                            {
                                Messenger.Default.Send(strokesData.Strokes, "ChangeBoardBackground");
                            }
                            else if (Convert.ToString(strokesData.SenderType) == "DeleteBoardChild")
                            {
                                Messenger.Default.Send(strokesData.Strokes, "RemoveBoardItem");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.InsertException(ex);
            }
        }
    }
}
