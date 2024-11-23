﻿using Autodesk.AutoCAD.ApplicationServices;
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
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace MeshWelderAutocad.Commands.Test
{
    internal class Command
    {
        private static string appName = "SlabMaster";
        private static List<ObjectId> _polylinesNeedToUpdateXData = new List<ObjectId>();
        private static Document doc;
        private static Editor ed;
        [CommandMethod("Test")]
        public static void Test()
        {
            //Проверить как сейчас работает если поменять количество вершин или повернуть полигон на плане и на слебе?
            //ругается при выборе прямоугольников
            //подписка пропадает при закрытии автокада, нужно подписываться заново при открытии повторном
            //Проблемы при слежение за всеми объектами сразу, транзакции одновременно открываются что ли? по идее нужно бы подписку хотя бы за линией плана сделать, а в идеале за картинками бы тоже следить
            try
            {
                doc = Application.DocumentManager.MdiActiveDocument;
                ed = doc.Editor;
                Database db = doc.Database;
                ObjectId slabPolylineId;
                ObjectId planPolylineId;
                ObjectId slabImageId;
                ObjectId planImageId;
                using (DocumentLock docLock = doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        Polyline planPolyline = SelectPlanPolyLine();
                        planPolylineId = planPolyline.Id;

                        Point3d pointInPlanPolyline = SelectPoint("Выберите базовую точку для копирования: ");
                        slabImageId = SelectSlabImage();
                        Point3d endPoint = SelectPoint("Укажите точку для вставки копии: ");

                        BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Vector3d displacement = endPoint - pointInPlanPolyline;

                        RasterImage slabImage = tr.GetObject(slabImageId, OpenMode.ForRead) as RasterImage;
                        RasterImage planImage = CreatePlanImage(slabImage, displacement, tr, planPolyline, btr);
                        planImageId = planImage.Id;
                        Polyline slabPolyline = CreateSlabPolyline(planPolyline, displacement, planImage, slabImageId, tr, btr, db);
                        slabPolylineId = slabPolyline.Id;

                        AddXDataToPlanPolyline(slabPolyline, planPolyline, planImage.Id, slabImageId, db);
                        tr.Commit();
                    }
                    AddModifiedHandlers(slabPolylineId, planPolylineId, slabImageId, planImageId);
                    Application.DocumentManager.MdiActiveDocument.CommandEnded += OnCommandEnded;
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
            }
        }

        private static void OnCommandEnded(object sender, CommandEventArgs e)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            List<ObjectId> slabPolylineIds = new List<ObjectId>();
            if (_polylinesNeedToUpdateXData.Count > 0)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var objId in _polylinesNeedToUpdateXData)
                    {
                        Polyline slabPolyline = tr.GetObject(objId, OpenMode.ForWrite) as Polyline;
                        RemoveExistXData(slabPolyline);
                        List<SetLinkedElement> sets = GetSetsLinkedElement(db);
                        PolylineType polylineType = GetTypePolyline(slabPolyline, sets);
                        SetLinkedElement matchSet = GetMatchSet(slabPolyline, polylineType, sets);
                        Polyline planPolyline = GetPolylineById(db, matchSet.PlanPolylineId);
                        AddXDataToSlabPolyline(slabPolyline, planPolyline, matchSet.PlanImageId, matchSet.SlabImageId, db);
                        slabPolylineIds.Add(slabPolyline.Id);
                    }
                    tr.Commit();
                }
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    foreach (var slabPolylineId in slabPolylineIds)
                    {
                        DBObject slabPolyline = tr.GetObject(slabPolylineId, OpenMode.ForWrite);
                        slabPolyline.Modified += OnElementModified;
                    }
                    tr.Commit();
                }

                _polylinesNeedToUpdateXData.Clear();
            }
        }

        private static void AddModifiedHandlers(ObjectId slabPolylineId, ObjectId planPolylineId, ObjectId slabImageId, ObjectId planImageId)
        {
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                DBObject slabPolyline = GetDBObjectById(slabPolylineId, tr);
                slabPolyline.Modified += OnElementModified;
                //DBObject planPolyline = GetDBObjectById(planPolylineId, tr);
                //planPolyline.Modified += OnElementModified;
                //DBObject slabImage = GetDBObjectById(slabImageId, tr);
                //slabImage.Modified += OnElementModified;
                //DBObject planImage = GetDBObjectById(planImageId, tr);
                //planImage.Modified += OnElementModified;
                tr.Commit();
            }
        }

        private static Polyline CreateSlabPolyline(Polyline planPolyline, Vector3d displacement,
            RasterImage planImage, ObjectId slabImageId, Transaction tr, BlockTableRecord btr, Database db)
        {
            Polyline slabPolyline = planPolyline.Clone() as Polyline;
            slabPolyline.TransformBy(Matrix3d.Displacement(displacement));
            btr.AppendEntity(slabPolyline);
            tr.AddNewlyCreatedDBObject(slabPolyline, true);
            AddXDataToSlabPolyline(slabPolyline, planPolyline, planImage.Id, slabImageId, db);
            return slabPolyline;
        }

        private static RasterImage CreatePlanImage(RasterImage slabImage, Vector3d displacement,
            Transaction tr, Polyline planPolyline, BlockTableRecord btr)
        {
            RasterImage planImage = slabImage.Clone() as RasterImage;
            planImage.TransformBy(Matrix3d.Displacement(-displacement));
            btr.AppendEntity(planImage);
            tr.AddNewlyCreatedDBObject(planImage, true);
            ClippingRasterBoundary(planImage, planPolyline);
            return planImage;
        }
        /// <summary>
        /// Сюда попадают только полилинии плана или полилинии на слебе, других не будет
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnElementModified(object sender, EventArgs e)
        {
            if (sender is Polyline || sender is RasterImage)
            {
                Database db = (sender as DBObject).Database;
                List<SetLinkedElement> sets = GetSetsLinkedElement(db);
                RemoveObjectsInNotFullSets(sets, db);
                sets = FilterSetWithAllExistObjects(sets);
                if (sender is Polyline polyline)
                {
                    PolylineType polylineType = GetTypePolyline(polyline, sets);
                    SetLinkedElement matchSet = GetMatchSet(polyline, polylineType, sets);
                    if (matchSet == null) // Если изменение было удаление полилинии на слебе или на плане, то ничего делать не требуется
                        return;

                    if (polylineType == PolylineType.SlabPolyline)
                    {
                        UpdateClipBoundary(polyline, db, matchSet);
                        _polylinesNeedToUpdateXData.Add(polyline.Id);
                        polyline.Modified -= OnElementModified;
                        //Если была изменена не только позиция полилинии, то нужно поменять и позицию полилинии на плане
                    }
                    else if (polylineType == PolylineType.PlanPolyline)
                    {
                        //содержимое картинки должно остатся таким же но переместить на новое место за смещенной полилинией плана
                        //в новом функционале также должен измениться контур, если мы меняем количество вершин или поворачиваем контур
                        //а также должен пересроится контур 
                    }
                    //UpdateXDataPolyline(polyline, db, sets); 
                }
                else if (sender is RasterImage image)
                {
                    //Ну вообще как будто реально не очень желательно слебы или картинки на плане перемещать куда-то, чтобы не следить за их координатами
                    //Я хз, надо ли тут что-то делать, наверное нет, вообще как будто бы выдавать окошко,
                    //что верните картинку обратно или самому вернуть ее где она была или все-таки лучше хранить просто положение картинки в xdata
                }
            }

        }

        private static void RemoveExistXData(Polyline polyline)
        {
            ResultBuffer existingXData = polyline.XData;
            if (existingXData != null)
            {
                TypedValue[] xdataValues = existingXData.AsArray();
                if (xdataValues.Length > 0 && xdataValues[0].TypeCode == (int)DxfCode.ExtendedDataRegAppName
                    && xdataValues[0].Value.ToString() == appName)
                {
                    polyline.XData = null; // Удаляем XData
                }
            }
        }

        private static Polyline GetPolylineById(Database db, ObjectId planPolylineId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline polyline = tr.GetObject(planPolylineId, OpenMode.ForRead) as Polyline;
                tr.Commit();
                return polyline;
            }
        }

        private static void UpdateClipBoundary(Polyline polyline, Database db, SetLinkedElement matchSet)
        {
            double verticalOffset = GetVerticalOffset(db, polyline);
            double horizontalOffset = GetHorizontalOffset(db, polyline);
            using (Transaction tr = db.TransactionManager.StartTransaction())
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

        private static double GetVerticalOffset(Database db, Polyline polyline)
        {
            PolylineXData xData = GetXDataForPolyline(db, polyline);
            Vertex currentFirstVertex = GetVertexes(polyline).First();
            Vertex oldFirstVertex = xData.SlabPolylineVertexes.First();
            return currentFirstVertex.Y - oldFirstVertex.Y;
        }
        private static double GetHorizontalOffset(Database db, Polyline polyline)
        {
            PolylineXData xData = GetXDataForPolyline(db, polyline);
            Vertex currentFirstVertex = GetVertexes(polyline).First();
            Vertex oldFirstVertex = xData.SlabPolylineVertexes.First();
            return currentFirstVertex.X - oldFirstVertex.X;
        }

        private static List<SetLinkedElement> FilterSetWithAllExistObjects(List<SetLinkedElement> sets)
        {
            return sets
                .Where(set => !set.PlanPolylineId.IsErased
                    && !set.SlabPolylineId.IsErased
                    && !set.PlanImageId.IsErased
                    && !set.SlabImageId.IsErased)
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

        /// <summary>
        /// Переместить картинку на плане нужно,только если количество вершин осталось таким же у полилинии плана и полилинии слеба 
        /// оличество вершин осталось таким же и все вершины получили одинаковое смещение по сравнению с предыдущим положением
        /// </summary>
        /// <param name="slabPolyline"></param>
        /// <param name="sets"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        //private static bool IsPossibleToChangeClipPosition(Polyline slabPolyline, SetLinkedElement set)
        //{
        //    if ()
        //    {

        //    }
        //}

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

        //private static void UpdateXDataPolyline(Polyline polyline, Database db, PolylineType polylineType, SetLinkedElement set)
        //{
        //    if (polylineType == PolylineType.PlanPolyline)
        //    {
        //        AddXDataToPlanPolyline(polyline,)
        //    }
        //    else if (polylineType == PolylineType.SlabPolyline)
        //    {
        //        AddXDataToSlabPolyline(polyline,)
        //    }
        //}

        private static List<SetLinkedElement> GetSetsLinkedElement(Database db)
        {
            Database db1 = HostApplicationServices.WorkingDatabase;
            List<SetLinkedElement> setsLinkedElement = new List<SetLinkedElement>();
            List<PolylineXData> xDatas = GetXDataFromAllPolylines(db1);

            for (int i = 0; i < xDatas.Count; i++)
            {
                ObjectId planPolylineId = GetObjectIdByLong(xDatas[i].PlanPolylineId);
                if (setsLinkedElement.FirstOrDefault(s => s.PlanPolylineId.Equals(planPolylineId)) == null)
                {
                    ObjectId slabPolylineId = GetObjectIdByLong(xDatas[i].SlabPolylineId);
                    ObjectId slabImageId = GetObjectIdByLong(xDatas[i].SlabImageId);
                    ObjectId planImageId = GetObjectIdByLong(xDatas[i].PlanImageId);
                    setsLinkedElement.Add(new SetLinkedElement(planImageId, slabImageId, planPolylineId, slabPolylineId));
                }
            }
            return setsLinkedElement;
        }

        /// <summary>
        /// Проверяет существуют все ли объекты, связанные с существующей полилинией, чтобы было что перемещать вообще
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        //private static bool IsLinkElementIsExist(Polyline polyline, List<SetLinkedElement> linkElements)
        //{
        //    SetLinkedElement match = linkElements.FirstOrDefault();
        //    if (match == null)
        //    {
        //        //по идее такого быть не может потому что мы подписываем на событие изменение только линии с xData
        //        return false;
        //    }
        //}

        private static ObjectId GetObjectIdByLong(long number)
        {
            return new ObjectId(new IntPtr(number));
        }

        //    //if (e.DBObject is Polyline polyline)
        //    //{
        //    //    PolylineXData currentSlabPolylineXData = GetXData(polyline);
        //    //    List<Vertex> polylineVertexes = GetVertexes(polyline);
        //    //    if (currentSlabPolylineXData != null)
        //    //    {
        //    //        //INFO была измене
        //    //        return;
        //    //    }

        //        //&& IsSlabLineChangePosition(modifiedPolylineVertexes, currentSlabPolylineXData.SlabPolylineVertexes))
        //        //{
        //        //    if ()
        //        //    {
        //        //        MessageBox.Show("У полилинии были изменены количество вершин. " +
        //        //            "Для правильной работы плагина требуется вернуть измененную полилинию к исходному состоянию.");
        //        //    }
        //        //    if ()
        //        //    {
        //        //        MessageBox.Show("У полилинии слэба были изменены количество вершин." +
        //        //            " Для правильной работы плагина требуется вернуть измененную полилинию к исходному состоянию.");
        //        //    }

        //        //    double verticalOffset = GetVertexes(slabLine).First().Y - currentSlabPolylineXData.SlabPolylineVertexes.Y;
        //        //    double horizontalOffset = GetVertexes(slabLine).First().X - currentSlabPolylineXData.SlabPolylineVertexes.X;
        //        //    using (Transaction tr = e.DBObject.Database.TransactionManager.StartTransaction())
        //        //    {
        //        //        ObjectId planImageId = new ObjectId(new IntPtr(Convert.ToInt64(currentSlabPolylineXData.PlanImageId)));
        //        //        RasterImage planImage = tr.GetObject(planImageId, OpenMode.ForWrite) as RasterImage;

        //        //        ObjectId planPolylineId = new ObjectId(new IntPtr(Convert.ToInt64(currentSlabPolylineXData.PlanPolylineId)));
        //        //        Polyline planPolyline = tr.GetObject(planPolylineId, OpenMode.ForWrite) as Polyline;

        //        //        UpdateXDataToSlabPolyline(currentSlabPolylineXData);

        //        //        ClippingRasterBoundary(planImage, planPolyline, verticalOffset, horizontalOffset);
        //        //        planImage.TransformBy(Matrix3d.Displacement(-new Vector3d(-horizontalOffset, -verticalOffset, 0)));
        //        //        tr.Commit();
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    //Получить все полилинии
        //        //    //если была отредактирована линия, которая у другой полилинии сохранена как связанная, то нужно удалить связанную полилинию и связанную картинку с плана
        //        //}
        //    //}
        //    //else if (e.DBObject is RasterImage image)
        //    //{
        //    //    //Получить все полилинии
        //    //    //если была удалена картинка слеба, которая была связанная у какой-то линии на слебе, то удалить все связанные объекты
        //    //    //если была была удалена картинка на плане, то удалить все связанные линии, но картинку слеба не трогать
        //    //}
        //    //ВЕЗДЕ НЕ ЗАБЫТЬ ПОМЕНЯТЬ XDATA на новую, если стараю не актуальная
        //}

        private static void RemoveObjectsInNotFullSets(List<SetLinkedElement> sets, Database db)
        {
            foreach (SetLinkedElement set in sets)
            {
                if (set.SlabPolylineId.IsErased
                    || set.PlanPolylineId.IsErased
                    || set.PlanImageId.IsErased
                    || set.SlabImageId.IsErased)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        //TODO На картинку самого слеба подписку надо удалять и как?
                        if (!set.PlanImageId.IsErased)
                        {
                            DBObject planImage = GetDBObjectById(set.PlanImageId, tr);
                            planImage.Modified -= OnElementModified;
                            planImage.Erase();
                        }
                        if (!set.PlanPolylineId.IsErased)
                        {
                            DBObject planPolyline = GetDBObjectById(set.PlanPolylineId, tr);
                            planPolyline.Modified -= OnElementModified;
                            planPolyline.Erase();
                        }
                        if (!set.SlabPolylineId.IsErased)
                        {
                            DBObject slabPolyline = GetDBObjectById(set.PlanPolylineId, tr);
                            slabPolyline.Modified -= OnElementModified;
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

        private static List<PolylineXData> GetXDataFromAllPolylines(Database db)
        {
            List<PolylineXData> polylineXDatas = new List<PolylineXData>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in btr)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                    if (obj is Polyline polyline)
                    {
                        ResultBuffer xData = polyline.GetXDataForApplication(appName);
                        if (xData != null)
                        {
                            polylineXDatas.Add(ParseXDataJSON(xData));
                        }
                    }
                }
                tr.Commit();
            }
            return polylineXDatas;
        }
        private static PolylineXData GetXDataForPolyline(Database db, Polyline polyline)
        {
            PolylineXData polylineXData;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ResultBuffer xData = polyline.GetXDataForApplication(appName);
                polylineXData = ParseXDataJSON(xData);
                tr.Commit();
            }
            return polylineXData;
        }

        private static PolylineXData ParseXDataJSON(ResultBuffer xData)
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
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PolylineXData));
                                return (PolylineXData)serializer.ReadObject(ms);
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

        private static bool IsSlabLineChangePosition(List<Vertex> points1, List<Vertex> points2)
        {
            if (points1.Count != points2.Count)
            {
                return false;
            }

            for (int i = 0; i < points1.Count; i++)
            {
                if (!ArePointsEqual(points1[i], points2[i]))
                {
                    return false;
                }
            }
            return true;
        }
        private static bool ArePointsEqual(Vertex p1, Vertex p2)
        {
            return Math.Round(p1.X, 8) == Math.Round(p2.X, 8) &&
                   Math.Round(p1.Y, 8) == Math.Round(p2.Y, 8) &&
                   Math.Round(p1.Z, 8) == Math.Round(p2.Z, 8);
        }

        private static ObjectId SelectSlabImage()
        {
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
            PromptPointResult ppr = ed.GetPoint(message);
            if (ppr.Status != PromptStatus.OK)
            {
                throw new System.Exception("\nПользователь отменил выбор");
            }
            return ppr.Value;
        }

        private static Polyline SelectPlanPolyLine()
        {
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
                if (pl != null && IsPolylineClosed(pl))
                    return pl;
                else
                    throw new System.Exception("Выбранная полилиния не является замкнутой. " +
                        "Требуется выбрать замкнутую линию.");
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

        private static void AddXDataToSlabPolyline(Polyline slabPolyline,
            Polyline planPolyline,
            ObjectId planImageId,
            ObjectId slabImageId,
            Database db)
        {
            ResultBuffer rb = CreateXData(slabPolyline, planPolyline, planImageId, slabImageId, db);
            slabPolyline.XData = rb;
        }
        private static void AddXDataToPlanPolyline(Polyline slabPolyline,
            Polyline planPolyline,
            ObjectId planImageId,
            ObjectId slabImageId,
            Database db)
        {
            using (Transaction tr = planPolyline.Database.TransactionManager.StartTransaction())
            {
                Polyline planPolylineForWrite = tr.GetObject(planPolyline.ObjectId, OpenMode.ForWrite) as Polyline;
                ResultBuffer rb = CreateXData(slabPolyline, planPolyline, planImageId, slabImageId, db);
                planPolyline.XData = rb;
                tr.Commit();
            }
        }

        private static ResultBuffer CreateXData(Polyline slabPolyline, Polyline planPolyline,
            ObjectId planImageId, ObjectId slabImageId, Database db)
        {
            Database db1 = HostApplicationServices.WorkingDatabase;
            var test1 = db1.RegAppTableId;
            RegAppTable regAppTable = (RegAppTable)test1.GetObject(OpenMode.ForRead);
            if (!regAppTable.Has(appName))
            {
                regAppTable.UpgradeOpen();
                RegAppTableRecord regAppRecord = new RegAppTableRecord { Name = appName };
                regAppTable.Add(regAppRecord);
                db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(regAppRecord, true);
            }
            var jsonXData = new PolylineXData()
            {
                PlanImageId = ParseObjectIdToLong(planImageId),
                PlanPolylineId = ParseObjectIdToLong(planPolyline.Id),
                SlabImageId = ParseObjectIdToLong(slabImageId),
                SlabPolylineId = ParseObjectIdToLong(slabPolyline.Id),
                SlabPolylineVertexes = GetVertexes(slabPolyline),
                PlanPolylineVertexes = GetVertexes(planPolyline),
            };

            string json;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PolylineXData));
                serializer.WriteObject(ms, jsonXData);
                json = Encoding.UTF8.GetString(ms.ToArray());
            }
            ResultBuffer rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, json)
            );
            return rb;
        }

        private static long ParseObjectIdToLong(ObjectId objectId)
        {
            return Convert.ToInt64(objectId.ToString().Replace("(", "").Replace(")", ""));
        }

        private static List<Vertex> GetVertexes(Polyline polyline)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }
            return points.Select(pt => new Vertex(pt.X, pt.Y, pt.Z)).ToList();
        }
    }
}
