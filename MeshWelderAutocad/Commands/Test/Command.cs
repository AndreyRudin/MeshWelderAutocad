using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MessageBox = System.Windows.MessageBox;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

//не двигать руками картинки слеба
//!ПРОВЕРИТЬ иожно двигать полинию линию плана вместе с картинкой на плане можно
//не копировать использующиеся в плагине полилинии плана, слеба или картинки плана или слеба
//не менять количество вершин и положение вершин по отдельности в полилниях плана или полилиниях слеба
//не поворачивать никакие объекты уже связанные между собой
//не менять количество вершин в обрезке картинки плана

//нельзя копировать полилинии и картинки с xData, следить за таким и откатывать назад действие
//нельзя менять количество вершин в полилиниях, следить за таким и откатывать назад действие
//учесть что одна картинка слеба может ссылаться на несколько полилиний на плане

namespace MeshWelderAutocad.Commands.Test
{
    internal partial class Command
    {
        public static string _appName = "SlabMaster";
        private static List<ObjectId> _slabPolylinesNeedToAddModifiedHandle = new List<ObjectId>();
        private static Guid _planImageGuid;
        private static Guid _planPolylineGuid;
        private static Guid _slabImageGuid;
        private static Guid _slabPolylineGuid;
        private static List<Vertex> _slabPolylineVertexes;
        private static Vertex _slabPolylineCentroid;
        [CommandMethod("Test")]
        public static void Test()
        {
            //Проверить как сейчас работает если поменять количество вершин или повернуть полигон на плане и на слебе?
            //ругается при выборе прямоугольников
            //подписка пропадает при закрытии автокада, нужно подписываться заново при открытии повторном
            //Проблемы при слежение за всеми объектами сразу, транзакции одновременно открываются что ли? по идее нужно бы подписку хотя бы за линией плана сделать, а в идеале за картинками бы тоже следить
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                ObjectId slabPolylineId;
                ObjectId planPolylineId;
                ObjectId slabImageId;
                ObjectId planImageId;
                _planImageGuid = Guid.NewGuid();
                _planPolylineGuid = Guid.NewGuid();
                _slabPolylineGuid = Guid.NewGuid();
                using (DocumentLock docLock = doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        Polyline planPolyline = SelectPlanPolyLine(); // выбирать можно только полилинию без xData
                        planPolylineId = planPolyline.Id;

                        Point3d pointInPlanPolyline = SelectPoint("Выберите базовую точку для копирования: ");
                        slabImageId = SelectSlabImage();
                        _slabImageGuid = GetOrCreateSlabImageGuid(slabImageId);

                        Point3d endPoint = SelectPoint("Укажите точку для вставки копии: ");

                        BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Vector3d displacement = endPoint - pointInPlanPolyline;

                        RasterImage slabImage = tr.GetObject(slabImageId, OpenMode.ForRead) as RasterImage;
                        RasterImage planImage = CreatePlanImage(slabImage, displacement, planPolyline, btr);
                        planImageId = planImage.Id;
                        Polyline slabPolyline = CreateSlabPolyline(planPolyline, displacement, btr);
                        _slabPolylineCentroid = CalculateCentroid(slabPolyline);
                        slabPolylineId = slabPolyline.Id;
                        tr.Commit();
                    }
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        var slabPolyline = tr.GetObject(slabPolylineId, OpenMode.ForWrite) as Polyline;
                        var planPolyline = tr.GetObject(planPolylineId, OpenMode.ForWrite) as Polyline;
                        var slabImage = tr.GetObject(slabImageId, OpenMode.ForWrite) as RasterImage;
                        var planImage = tr.GetObject(planImageId, OpenMode.ForWrite) as RasterImage;
                        AddXDataToSlabPolyline(slabPolyline);
                        AddXDataToPlanPolyline(planPolyline);
                        AddXDataToImage(slabImage, _slabImageGuid);
                        AddXDataToImage(planImage, _planImageGuid);
                        tr.Commit();
                    }
                    AddModifiedHandlers(slabPolylineId);
                }
            }
            catch (System.Exception exception)
            {
                MessageBox.Show(exception.Message + exception.StackTrace);
            }
        }
        /// <summary>
        /// Получить guid картинки слеба, если он есть, если нет, то создать новый, потому что одна картинка слэба может использоваться в нескольких связах с разными полилиниями
        /// </summary>
        /// <param name="slabImageId"></param>
        /// <returns></returns>
        private static Guid GetOrCreateSlabImageGuid(ObjectId slabImageId)
        {
            Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.TopTransaction;
            DBObject slabImage = tr.GetObject(slabImageId, OpenMode.ForRead);
            ResultBuffer xData = slabImage.GetXDataForApplication(_appName);
            if (xData != null)
            {
                BaseXData xDataParse = ParseShortXDataJSON(xData);
                return xDataParse.Guid;
            }
            else
            {
                return Guid.NewGuid();
            }
        }

        /// <summary>
        /// Сюда попадают только полилинии плана или полилинии на слебе, других не будет
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnSlabPolylineModified(object sender, EventArgs e)
        {
            try
            {
                if (sender is Polyline slabPolyline)
                {
                    var xDatas = GetXDatas();
                    List<SetLinkedElement> sets = FilterSetWithAllExistObjects(GetSetsLinkedElement(xDatas));

                    PolylineType polylineType = PolylineType.SlabPolyline;
                    SetLinkedElement matchSet = GetMatchSet(slabPolyline, polylineType, sets);
                    if (matchSet == null) //INFO Если изменение было удаление полилинии на слебе, то ничего делать не требуется
                        return;

                    if (polylineType == PolylineType.SlabPolyline)
                    {
                        UpdateClipBoundary(slabPolyline, matchSet);
                        _slabPolylinesNeedToAddModifiedHandle.Add(slabPolyline.Id);
                        slabPolyline.Modified -= OnSlabPolylineModified; //INFO это нужно, чтобы после изменения Xdata не было бесконечного цикла модификации полилинии
                        //Если была изменена не только позиция полилинии, то нужно поменять и позицию полилинии на плане
                    }

                    //else if (polylineType == PolylineType.PlanPolyline)
                    //{
                    //    //содержимое картинки должно остатся таким же но переместить на новое место за смещенной полилинией плана
                    //    //в новом функционале также должен измениться контур, если мы меняем количество вершин или поворачиваем контур
                    //    //а также должен пересроится контур 
                    //}
                    //else if (sender is RasterImage image)
                    //{
                    //    //Ну вообще как будто реально не очень желательно слебы или картинки на плане перемещать куда-то, чтобы не следить за их координатами
                    //    //Я хз, надо ли тут что-то делать, наверное нет, вообще как будто бы выдавать окошко,
                    //    //что верните картинку обратно или самому вернуть ее где она была или все-таки лучше хранить просто положение картинки в xdata
                    //}
                }
            }
            catch (System.Exception exception)
            {
                MessageBox.Show(exception.Message + exception.StackTrace);
            }
        }
        public static void OnCommandEnded(object sender, CommandEventArgs e)
        {
            try
            {
                if (e.GlobalCommandName == "ERASE")
                {
                    List<SlabPolylineXData> slabPolylinesXDatas = GetXDatas();
                    RemoveObjectsInNotFullSets(GetSetsLinkedElement(slabPolylinesXDatas));
                }

                //и если линия была скопирована с xData то уведомить пользователя хотя бы

                if (_slabPolylinesNeedToAddModifiedHandle.Count > 0)
                {
                    List<ObjectId> slabPolylineIds = new List<ObjectId>();
                    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId slabPolylineObjId in _slabPolylinesNeedToAddModifiedHandle)
                        {
                            Polyline slabPolyline = tr.GetObject(slabPolylineObjId, OpenMode.ForWrite) as Polyline;
                            SlabPolylineXData xDataOld = ParseXDataJSON(slabPolyline.GetXDataForApplication(_appName));

                            _planImageGuid = xDataOld.PlanImageGuid;
                            _planPolylineGuid = xDataOld.PlanPolylineGuid;
                            _slabPolylineCentroid = CalculateCentroid(slabPolyline);
                            _slabImageGuid = xDataOld.SlabImageGuid;

                            slabPolyline.XData = null;
                            UpdateXData(slabPolyline, xDataOld.Guid);
                            slabPolylineIds.Add(slabPolyline.Id);
                        }
                        tr.Commit();
                    }

                    foreach (var slabPolylineId in slabPolylineIds)
                        AddModifiedHandlers(slabPolylineId);

                    _slabPolylinesNeedToAddModifiedHandle.Clear();
                }
            }
            catch (System.Exception exception)
            {
                MessageBox.Show(exception.Message + exception.StackTrace);
            }
        }

        private static void UpdateXData(Polyline slabPolyline, Guid slabPolylineGuid)
        {
            ResultBuffer rb = CreateXData(slabPolylineGuid);
            slabPolyline.XData = rb;
        }

        private static void AddXDataToImage(RasterImage image, Guid guidImage)
        {
            using (Transaction tr = image.Database.TransactionManager.StartTransaction())
            {
                DBObject slabImage1 = tr.GetObject(image.ObjectId, OpenMode.ForWrite);
                ResultBuffer rb = CreateShortXData(guidImage);
                slabImage1.XData = rb;
                tr.Commit();
            }
        }
        private static void AddModifiedHandlers(ObjectId slabPolylineId)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                DBObject slabPolyline = tr.GetObject(slabPolylineId, OpenMode.ForWrite);
                slabPolyline.Modified += OnSlabPolylineModified;
                tr.Commit();
            }
        }

        private static Polyline CreateSlabPolyline(Polyline planPolyline, 
            Vector3d displacement,BlockTableRecord btr)
        {
            Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.TopTransaction;
            Polyline slabPolyline = planPolyline.Clone() as Polyline;
            slabPolyline.TransformBy(Matrix3d.Displacement(displacement));
            btr.AppendEntity(slabPolyline);
            tr.AddNewlyCreatedDBObject(slabPolyline, true);
            return slabPolyline;
        }

        private static RasterImage CreatePlanImage(RasterImage slabImage, Vector3d displacement,
            Polyline planPolyline, BlockTableRecord btr)
        {
            Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.TopTransaction;
            RasterImage planImage = slabImage.Clone() as RasterImage;
            planImage.TransformBy(Matrix3d.Displacement(-displacement));
            btr.AppendEntity(planImage);
            tr.AddNewlyCreatedDBObject(planImage, true);
            ClippingRasterBoundary(planImage, planPolyline);
            return planImage;
        }
        private static void UpdateClipBoundary(Polyline slabPolyline, SetLinkedElement matchSet)
        {
            Vertex currentCentroid = CalculateCentroid(slabPolyline);
            Vertex oldCentroid = GetXDataForPolyline(slabPolyline).SlabPolylineCentroid;
            double verticalOffset = currentCentroid.Y - oldCentroid.Y;
            double horizontalOffset = currentCentroid.X - oldCentroid.X;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                RasterImage planImage = tr.GetObject(matchSet.PlanImageId, OpenMode.ForWrite) as RasterImage;
                Polyline planPolyline = tr.GetObject(matchSet.PlanPolylineId, OpenMode.ForWrite) as Polyline;

                planImage.TransformBy(Matrix3d.Displacement(-new Vector3d(horizontalOffset, verticalOffset, 0)));
                ClippingRasterBoundary(planImage, planPolyline, 0, 0);
                tr.Commit();
            }
        }
        private static void ClippingRasterBoundary(RasterImage image, Polyline polyline, double verticalOffset = 0, double horizontalOffset = 0)
        {
            Point2dCollection clipBoundary = new Point2dCollection();
            Matrix3d transformMatrix = image.PixelToModelTransform.Inverse();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point3d = new Point3d(polyline.GetPoint3dAt(i).X + horizontalOffset, polyline.GetPoint3dAt(i).Y + verticalOffset, polyline.GetPoint3dAt(i).Z);
                Point2d point = point3d.TransformBy(transformMatrix).Convert2d(new Plane());
                clipBoundary.Add(point);
            }

            clipBoundary.Add(clipBoundary[0]);

            image.SetClipBoundary(ClipBoundaryType.Poly, clipBoundary);
            image.IsClipped = true;
        }
        private static Vertex CalculateCentroid(Polyline polyline)
        {
            double sumX = 0;
            double sumY = 0;

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point2d vertex = polyline.GetPoint2dAt(i);
                sumX += vertex.X;
                sumY += vertex.Y;
            }

            double centerX = sumX / polyline.NumberOfVertices;
            double centerY = sumY / polyline.NumberOfVertices;

            return new Vertex(centerX, centerY);
        }
        //private static double GetVerticalOffset(Polyline polyline)
        //{
        //    Vertex currentFirstVertex = GetVertexes(polyline).First();
        //    Vertex oldFirstVertex = xData.SlabPolylineVertexes.First();
        //    return currentFirstVertex.Y - oldFirstVertex.Y;
        //}
        //private static double GetHorizontalOffset(Polyline polyline)
        //{
        //    Vertex currentFirstVertex = GetVertexes(polyline).First();
        //    Vertex oldFirstVertex = xData.SlabPolylineVertexes.First();
        //    return currentFirstVertex.X - oldFirstVertex.X;
        //}

        private static List<SetLinkedElement> FilterSetWithAllExistObjects(List<SetLinkedElement> sets)
        {
            return sets
                .Where(set => !set.PlanPolylineId.IsErased && !set.PlanPolylineId.IsNull
                    && !set.SlabPolylineId.IsErased && !set.SlabPolylineId.IsNull
                    && !set.PlanImageId.IsErased && !set.PlanImageId.IsNull
                    && !set.SlabImageId.IsErased && !set.SlabImageId.IsNull)
                .ToList();
        }

        private static SetLinkedElement GetMatchSet(Polyline polyline, PolylineType polylineType, List<SetLinkedElement> sets)
        {
            if (polylineType == PolylineType.PlanPolyline)
            {
                return sets.FirstOrDefault(set => set.PlanPolylineId.Equals(polyline.Id));
            }
            else //Slab
            {
                return sets.FirstOrDefault(set => set.SlabPolylineId.Equals(polyline.Id));
            }
        }
        private static PolylineType GetTypePolyline(Polyline polyline, List<SetLinkedElement> sets)
        {
            if (sets.FirstOrDefault(s => s.PlanPolylineId.Equals(polyline.Id)) != null)
            {
                return PolylineType.PlanPolyline;
            }
            else
            {
                return PolylineType.SlabPolyline;
            }
        }
        private static List<SetLinkedElement> GetSetsLinkedElement(List<SlabPolylineXData> xDatas)
        {
            List<SetLinkedElement> setsLinkedElement = new List<SetLinkedElement>();
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < xDatas.Count; i++)
                {
                    ObjectId planPolylineId = GetObjectIdByGuid(xDatas[i].PlanPolylineGuid);
                    ObjectId slabPolylineId = GetObjectIdByGuid(xDatas[i].Guid);
                    ObjectId slabImageId = GetObjectIdByGuid(xDatas[i].SlabImageGuid);
                    ObjectId planImageId = GetObjectIdByGuid(xDatas[i].PlanImageGuid);
                    setsLinkedElement.Add(new SetLinkedElement(planImageId, slabImageId, planPolylineId, slabPolylineId));
                }
                tr.Commit();
            }
            return setsLinkedElement;
        }

        private static ObjectId GetObjectIdByGuid(Guid guid)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Transaction tr = db.TransactionManager.TopTransaction;
            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

            foreach (ObjectId objId in btr)
            {
                DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                if (obj is Polyline || obj is RasterImage)
                {
                    ResultBuffer xData = obj.GetXDataForApplication(_appName);
                    if (xData != null)
                    {
                        SlabPolylineXData xDataParse = ParseXDataJSON(xData);
                        if (xDataParse.Guid == guid)
                        {
                            return obj.Id;
                        }
                    }
                }
            }
            return ObjectId.Null; //может возникнуть когда мы открываем чертеж заново и линия была удалена
        }

        private static void RemoveObjectsInNotFullSets(List<SetLinkedElement> sets)
        {
            foreach (SetLinkedElement set in sets)
            {
                if (set.SlabPolylineId.IsErased || set.SlabPolylineId.IsNull
                    || set.PlanPolylineId.IsErased || set.PlanPolylineId.IsNull
                    || set.PlanImageId.IsErased || set.PlanImageId.IsNull
                    || set.SlabImageId.IsErased || set.SlabImageId.IsNull)
                {
                    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                    {
                        //TODO На картинку самого слеба подписку надо удалять и как?
                        if (!set.PlanImageId.IsErased && !set.PlanImageId.IsNull)
                        {
                            DBObject planImage = GetDBObjectById(set.PlanImageId, tr);
                            planImage.Modified -= OnSlabPolylineModified;
                            planImage.Erase();
                        }
                        if (!set.PlanPolylineId.IsErased && !set.PlanPolylineId.IsNull)
                        {
                            DBObject planPolyline = GetDBObjectById(set.PlanPolylineId, tr);
                            planPolyline.Modified -= OnSlabPolylineModified;
                            planPolyline.Erase();
                        }
                        if (!set.SlabPolylineId.IsErased && !set.SlabPolylineId.IsNull)
                        {
                            DBObject slabPolyline = GetDBObjectById(set.PlanPolylineId, tr);
                            slabPolyline.Modified -= OnSlabPolylineModified;
                            slabPolyline.Erase();
                        }
                        tr.Commit();
                    }
                }
            }
        }

        private static DBObject GetDBObjectById(ObjectId objId, Transaction tr)
        {
            return tr.GetObject(objId, OpenMode.ForWrite, false);
        }

        private static List<RasterImage> GetRasterImagesByIds(IEnumerable<long> enumerable, Database db)
        {
            List<RasterImage> rasterImages = new List<RasterImage>();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (long idValue in enumerable)
                {
                    ObjectId objId = new ObjectId(new IntPtr(idValue));

                    if (objId.IsValid && !objId.IsNull)
                    {
                        DBObject dbObject = trans.GetObject(objId, OpenMode.ForRead, false);
                        if (dbObject is RasterImage rasterImage)
                        {
                            rasterImages.Add(rasterImage);
                        }
                        else
                        {
                            rasterImages.Add(null);
                        }
                    }
                    else
                    {
                        rasterImages.Add(null);
                    }
                }
                trans.Commit();
            }

            return rasterImages;
        }

        private static List<Polyline> GetPolylinesByIds(IEnumerable<long> ids, Database db)
        {
            List<Polyline> polylines = new List<Polyline>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (long idValue in ids)
                {
                    ObjectId id = new ObjectId(new IntPtr(idValue));

                    if (id.IsValid && !id.IsErased)
                    {
                        DBObject obj = trans.GetObject(id, OpenMode.ForRead, false);

                        if (obj is Polyline polyline)
                        {
                            polylines.Add(polyline);
                        }
                        else
                        {
                            polylines.Add(null);
                        }
                    }
                    else
                    {
                        polylines.Add(null);
                    }
                }
                trans.Commit();
            }

            return polylines;
        }
        private static List<SlabPolylineXData> GetXDatas()
        {
            List<SlabPolylineXData> xDatas = new List<SlabPolylineXData>();

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in btr)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                    if (obj is Polyline polyline)
                    {
                        ResultBuffer xData = polyline.GetXDataForApplication(_appName);
                        if (xData != null)
                        {
                            SlabPolylineXData xDataParse = ParseXDataJSON(xData);
                            if (xDataParse.PlanPolylineGuid != Guid.Empty) //INFO чтобы от полилиний плана инфа не попадала
                            {
                                xDatas.Add(xDataParse);
                            }
                        }
                    }
                }
                tr.Commit();
            }
            return xDatas;
        }

        private static SlabPolylineXData GetXDataForPolyline(Polyline polyline)
        {
            SlabPolylineXData polylineXData;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                ResultBuffer xData = polyline.GetXDataForApplication(_appName);
                polylineXData = ParseXDataJSON(xData);
                tr.Commit();
            }
            return polylineXData;
        }

        public static SlabPolylineXData ParseXDataJSON(ResultBuffer xData)
        {
            foreach (TypedValue value in xData)
            {
                if (value.TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                {
                    string jsonString = value.Value as string;
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                            {
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SlabPolylineXData));
                                return serializer.ReadObject(ms) as SlabPolylineXData;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            throw new System.Exception($"Ошибка при парсинге JSON. Обратитесь к разработчику. {ex.Message} {ex.StackTrace}");
                        }
                    }
                }
            }
            return null;
        }
        public static BaseXData ParseShortXDataJSON(ResultBuffer xData) 
        {
            foreach (TypedValue value in xData)
            {
                if (value.TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                {
                    string jsonString = value.Value as string;
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                            {
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BaseXData));
                                return serializer.ReadObject(ms) as BaseXData;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            throw new System.Exception($"Ошибка при парсинге JSON. Обратитесь к разработчику. {ex.Message} {ex.StackTrace}");
                        }
                    }
                }
            }
            return null;
        }
        //private static bool IsSlabLineChangePosition(List<Vertex> points1, List<Vertex> points2)
        //{
        //    if (points1.Count != points2.Count)
        //    {
        //        return false;
        //    }

        //    for (int i = 0; i < points1.Count; i++)
        //    {
        //        if (!ArePointsEqual(points1[i], points2[i]))
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}
        private static bool ArePointsEqual(Vertex p1, Vertex p2)
        {
            return Math.Round(p1.X, 8) == Math.Round(p2.X, 8) &&
                   Math.Round(p1.Y, 8) == Math.Round(p2.Y, 8);
        }

        private static ObjectId SelectSlabImage()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptEntityOptions peoImg = new PromptEntityOptions("\nВыберите изображение под полилинией: ");
            peoImg.SetRejectMessage("\nВыбранный объект не является изображением.");
            peoImg.AddAllowedClass(typeof(RasterImage), exactMatch: true);

            PromptEntityResult perImg = ed.GetEntity(peoImg);
            if (perImg.Status != PromptStatus.OK)
            {
                throw new System.Exception("\nОтмена или некорректный выбор.");
            }

            return perImg.ObjectId;
        }

        private static Point3d SelectPoint(string message)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptPointResult ppr = ed.GetPoint(message);
            if (ppr.Status != PromptStatus.OK)
            {
                throw new System.Exception("\nПользователь отменил выбор");
            }
            return ppr.Value;
        }

        private static Polyline SelectPlanPolyLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityOptions peo = new PromptEntityOptions("\nВыберите полилинию: ");
            peo.SetRejectMessage("\nВыбранный объект не является полилинией.");
            peo.AddAllowedClass(typeof(Polyline), exactMatch: true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                throw new System.Exception("Пользователь отменил выбор");
            }
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Polyline;

                if (!IsPolylineClosed(pl))
                    throw new System.Exception("Выбранная полилиния не является замкнутой. Требуется выбрать замкнутую линию.");

                ResultBuffer xData = pl.GetXDataForApplication(_appName);
                if (xData != null)
                    throw new System.Exception("Выбранная полилиния уже связанна с полинией на слэбе. Выберите другую полилинию.");

                return pl;
            }
        }
        private static bool IsPolylineClosed(Polyline polyline)
        {
            if (polyline == null || polyline.NumberOfVertices < 2)
                return false;

            Point2d firstPoint = polyline.GetPoint2dAt(0);
            Point2d lastPoint = polyline.GetPoint2dAt(polyline.NumberOfVertices - 1);

            return firstPoint.IsEqualTo(lastPoint, Tolerance.Global);
        }

        private static void AddXDataToSlabPolyline(Polyline slabPolyline)
        {
            ResultBuffer rb = CreateXData(_slabPolylineGuid);
            slabPolyline.XData = rb;
        }
        private static void AddXDataToPlanPolyline(Polyline planPolyline)
        {
            using (Transaction tr = planPolyline.Database.TransactionManager.StartTransaction())
            {
                Polyline planPolylineForWrite = tr.GetObject(planPolyline.ObjectId, OpenMode.ForWrite) as Polyline;
                ResultBuffer rb = CreateShortXData(_planPolylineGuid);
                planPolyline.XData = rb;
                tr.Commit();
            }
        }

        private static ResultBuffer CreateXData(Guid currentEntityGuid)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            RegAppTable regAppTable = (RegAppTable)db.RegAppTableId.GetObject(OpenMode.ForRead);
            if (!regAppTable.Has(_appName))
            {
                regAppTable.UpgradeOpen();
                RegAppTableRecord regAppRecord = new RegAppTableRecord { Name = _appName };
                regAppTable.Add(regAppRecord);
                db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(regAppRecord, true);
            }
            var jsonXData = new SlabPolylineXData()
            {
                Guid = currentEntityGuid,
                PlanImageGuid = _planImageGuid,
                PlanPolylineGuid = _planPolylineGuid,
                SlabImageGuid = _slabImageGuid,
                SlabPolylineCentroid = _slabPolylineCentroid
            };

            string json;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SlabPolylineXData));
                serializer.WriteObject(ms, jsonXData);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }
            ResultBuffer rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, _appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, json)
            );
            return rb;
        }
        private static ResultBuffer CreateShortXData(Guid currentEntityGuid)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            RegAppTable regAppTable = (RegAppTable)db.RegAppTableId.GetObject(OpenMode.ForRead);
            if (!regAppTable.Has(_appName))
            {
                regAppTable.UpgradeOpen();
                RegAppTableRecord regAppRecord = new RegAppTableRecord { Name = _appName };
                regAppTable.Add(regAppRecord);
                db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(regAppRecord, true);
            }
            string json;
            var shortXData = new BaseXData() { Guid = currentEntityGuid };
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BaseXData));
                serializer.WriteObject(ms, shortXData);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }
            ResultBuffer rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, _appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, json)
            );
            return rb;
        }

        //private static List<Vertex> GetVertexes(Polyline polyline)
        //{
        //    List<Point3d> points = new List<Point3d>();
        //    for (int i = 0; i < polyline.NumberOfVertices; i++)
        //    {
        //        points.Add(polyline.GetPoint3dAt(i));
        //    }
        //    return points.Select(pt => new Vertex(pt.X, pt.Y)).ToList();
        //}

        internal static List<ObjectId> GetSlabPolylineIds()
        {
            List<ObjectId> slabPolylineIds = new List<ObjectId>();

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in btr)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                    if (obj is Polyline polyline)
                    {
                        ResultBuffer xData = polyline.GetXDataForApplication(_appName);
                        if (xData != null)
                        {
                            SlabPolylineXData xDataParse = ParseXDataJSON(xData);
                            if (xDataParse.PlanPolylineGuid != Guid.Empty) //INFO чтобы от полилиний плана инфа не попадала
                            {
                                slabPolylineIds.Add(objId);
                            }
                        }
                    }
                }
                tr.Commit();
            }
            return slabPolylineIds;
        }
    }
}
