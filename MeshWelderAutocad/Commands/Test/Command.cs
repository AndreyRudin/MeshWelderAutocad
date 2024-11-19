using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace MeshWelderAutocad.Commands.Test
{
    internal class Command
    {
        private static string appName = "SlabMaster";
        private static bool isObjectModifiedSubscribed = false; //можно сохранить это во внешний конфиг на самый крайний случай, потому что 
        private static Document doc;
        private static Editor ed;
        [CommandMethod("Test")]
        public static void Test()
        {
            try
            {
                doc = Application.DocumentManager.MdiActiveDocument;
                ed = doc.Editor;
                ObjectId slabPolylineId;
                using (DocumentLock docLock = doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        Polyline planPolyline = SelectPlanPolyLine();
                        //planPolyline.Modified += OnPolylineModified;

                        Point3d pointInPlanPolyline = SelectPoint("Выберите базовую точку для копирования: ");
                        ObjectId slabImageId = SelectSlabImage();
                        Point3d endPoint = SelectPoint("Укажите точку для вставки копии: ");

                        BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Vector3d displacement = endPoint - pointInPlanPolyline;

                        RasterImage slabImage = tr.GetObject(slabImageId, OpenMode.ForRead) as RasterImage;
                        RasterImage planImage = CreatePlanImage(slabImage, displacement, tr, planPolyline, btr);
                        Polyline slabPolyline = CreateSlabPolyline(planPolyline, displacement, planImage, slabImageId, tr, btr);
                        slabPolylineId = slabPolyline.Id;

                        AddXDataToPlanPolyline(slabPolyline, planPolyline, planImage.Id, slabImageId);
                        tr.Commit();
                    }
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        Polyline polyline = tr.GetObject(slabPolylineId, OpenMode.ForWrite) as Polyline;
                        polyline.Modified += OnPolylineModified;
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
            }
        }
        private static Polyline CreateSlabPolyline(Polyline planPolyline, Vector3d displacement, 
            RasterImage planImage, ObjectId slabImageId, Transaction tr, BlockTableRecord btr)
        {
            Polyline slabPolyline = planPolyline.Clone() as Polyline;
            slabPolyline.TransformBy(Matrix3d.Displacement(displacement));
            btr.AppendEntity(slabPolyline);
            tr.AddNewlyCreatedDBObject(slabPolyline, true);
            AddXDataToSlabPolyline(slabPolyline, planPolyline, planImage.Id, slabImageId);
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
        private static void OnPolylineModified(object sender, EventArgs e)
        {
            if (sender is Polyline polyline)
            {
                Database db = polyline.Database;
                List<Polyline> polylinesWithXData = GetPolylinesWithData(db, appName);
                List<PolylineXData> xDatas = polylinesWithXData.Select(pl => GetXData(pl)).ToList();
                List<Polyline> planPolylines = GetPolylinesByIds(xDatas.Select(data => data.PlanPolylineId).Distinct(), db);
                List<Polyline> slabPolylines = GetPolylinesByIds(xDatas.Select(data => data.SlabPolylineId).Distinct(), db);
                List<RasterImage> slabImages = GetRasterImagesByIds(xDatas.Select(data => data.SlabPolylineId).Distinct(), db);
                List<RasterImage> planImages = GetRasterImagesByIds(xDatas.Select(data => data.PlanPolylineId).Distinct(), db);
            }
            else
            {

            }
        }
        //private static void OnЩиоусеModified(object sender, EventArgs e)
        //{
        //    var modifiedProperties = .GetModifiedProperties();
        //    if (modifiedProperties != null && modifiedProperties.Count > 0)
        //    {
        //        ed.WriteMessage($"\nObject {e.DBObject.Id} реально изменен.");
        //    }
        //    else
        //    {
        //        ed.WriteMessage($"\nObject {e.DBObject.Id} модифицировался без изменений данных.");
        //    }

       
            
        //    List<LinkElement> linkElements = new List<LinkElement>();
        //    for (int i = 0; i < planPolylines.Count; i++)
        //    {
        //        linkElements.Add(new LinkElement(planImages[i], slabImages[i], 
        //            planPolylines[i], slabPolylines[i]));
        //    }
        //    //RemoveUnusedObjects(linkElements, db);

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

        private static void RemoveUnusedObjects(List<LinkElement> linkElements, Database db)
        {
            foreach (LinkElement linkElement in linkElements)
            {
                if (linkElement.SlabPolyline == null 
                    || linkElement.PlanPolyline == null 
                    || linkElement.PlanImage == null
                    || linkElement.SlabPolyline == null)
                {
                    if (linkElement.PlanImage != null && !linkElement.PlanImage.IsErased)
                    {
                        linkElement.PlanImage.UpgradeOpen();
                        linkElement.PlanImage.Erase();      
                    }

                    if (linkElement.SlabImage != null && !linkElement.SlabImage.IsErased)
                    {
                        linkElement.SlabImage.UpgradeOpen();
                        linkElement.SlabImage.Erase();
                    }

                    if (linkElement.PlanPolyline != null && !linkElement.PlanPolyline.IsErased)
                    {
                        linkElement.PlanPolyline.UpgradeOpen();
                        linkElement.PlanPolyline.Erase();
                    }

                    if (linkElement.SlabPolyline != null && !linkElement.SlabPolyline.IsErased)
                    {
                        linkElement.SlabPolyline.UpgradeOpen();
                        linkElement.SlabPolyline.Erase();
                    }
                }
            }
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

        private static List<Polyline> GetPolylinesWithData(Database db, string appName)
        {
            List<Polyline> polylinesWithXData = new List<Polyline>();

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
                            polylinesWithXData.Add(polyline);
                        }
                    }
                }

                tr.Commit();
            }
            return polylinesWithXData;
        }

        private static void UpdateXDataToSlabPolyline(PolylineXData currentSlabPolylineXData)
        {
            throw new NotImplementedException();
        }

        private static PolylineXData GetXData(Polyline polyline)
        {
            ResultBuffer xData = polyline.GetXDataForApplication(appName);
            if (xData != null)
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
                                throw new System.Exception($"Ошибка при парсинге JSON. Обратитесь к разработчику.");
                            }
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
            ObjectId slabImageId)
        {
            ResultBuffer rb = CreateXData(slabPolyline, planPolyline, planImageId, slabImageId);
            slabPolyline.XData = rb;
        }
        private static void AddXDataToPlanPolyline(Polyline slabPolyline, 
            Polyline planPolyline, 
            ObjectId planImageId, 
            ObjectId slabImageId)
        {
            using (Transaction tr = planPolyline.Database.TransactionManager.StartTransaction())
            {
                Polyline planPolylineForWrite = tr.GetObject(planPolyline.ObjectId, OpenMode.ForWrite) as Polyline;
                if (planPolylineForWrite != null)
                {
                    ResultBuffer rb = CreateXData(slabPolyline, planPolyline, planImageId, slabImageId);
                    planPolyline.XData = rb;
                }
                tr.Commit();
            }
        }

        private static ResultBuffer CreateXData(Polyline slabPolyline, Polyline planPolyline, 
            ObjectId planImageId, ObjectId slabImageId)
        {
            RegAppTable regAppTable = (RegAppTable)doc.Database.RegAppTableId.GetObject(OpenMode.ForRead);
            if (!regAppTable.Has(appName))
            {
                regAppTable.UpgradeOpen();
                RegAppTableRecord regAppRecord = new RegAppTableRecord { Name = appName };
                regAppTable.Add(regAppRecord);
                doc.Database.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(regAppRecord, true);
            }
            var jsonXData = new PolylineXData()
            {
                PlanImageId = ParseObjectIdToString(planImageId),
                PlanPolylineId = ParseObjectIdToString(planPolyline.Id),
                SlabImageId = ParseObjectIdToString(slabImageId),
                SlabPolylineId = ParseObjectIdToString(slabPolyline.Id),
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

        private static long ParseObjectIdToString(ObjectId objectId)
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
