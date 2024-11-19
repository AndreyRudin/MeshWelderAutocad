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
        private static string appName = "MyPluginXData";
        private static Document doc;
        private static Editor ed;
        private static Database db;
        [CommandMethod("Test")]
        public static void Test()
        {
            try
            {
                doc = Application.DocumentManager.MdiActiveDocument;
                ed = doc.Editor;
                db = doc.Database;
                using (DocumentLock docLock = doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        ObjectId planPolylineId = SelectOriginalPolyLine();
                        Point3d pointInPlanPolyline = SelectPoint("Выберите базовую точку для копирования: ");
                        ObjectId slabImageId = SelectOriginalImage();

                        Polyline planPolyline = tr.GetObject(planPolylineId, OpenMode.ForWrite) as Polyline;

                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        Point3d endPoint = SelectPoint("Укажите точку для вставки копии: ");
                        Vector3d displacement = endPoint - pointInPlanPolyline;

                        RasterImage slabImage = tr.GetObject(slabImageId, OpenMode.ForRead) as RasterImage;
                        RasterImage planImage = slabImage.Clone() as RasterImage;
                        planImage.TransformBy(Matrix3d.Displacement(-displacement));
                        btr.AppendEntity(planImage);
                        tr.AddNewlyCreatedDBObject(planImage, true);

                        Polyline slabPolyline = planPolyline.Clone() as Polyline;
                        slabPolyline.TransformBy(Matrix3d.Displacement(displacement));
                        btr.AppendEntity(slabPolyline);
                        tr.AddNewlyCreatedDBObject(slabPolyline, true);
                        AddXDataToEntity(slabPolyline, planPolyline.Id, planImage.Id, slabImage.Id);

                        ClippingRasterBoundary(planImage, planPolyline);
                        tr.Commit();
                    }
                }
                db.ObjectModified += OnObjectModified;
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
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
        private static void OnObjectModified(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Polyline polyline)
            {
                XDataJson xDataObj = null;
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
                                    // Преобразуем JSON строку в поток
                                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                                    {
                                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(XDataJson));
                                        xDataObj = (XDataJson)serializer.ReadObject(ms);
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    throw new System.Exception($"Ошибка при разборе JSON: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                if (xDataObj != null && !IsPointEquals(GetVertexes(polyline).First(), xDataObj.FirstVertex))
                {
                    double verticalOffset = GetVertexes(polyline).First().Y - xDataObj.FirstVertex.Y;
                    double horizontalOffset = GetVertexes(polyline).First().X - xDataObj.FirstVertex.X;
                    using (Transaction tr = e.DBObject.Database.TransactionManager.StartTransaction())
                    {
                        ObjectId planImageId = new ObjectId(new IntPtr(Convert.ToInt64(xDataObj.PlanImageId)));
                        RasterImage planImage = tr.GetObject(planImageId, OpenMode.ForWrite) as RasterImage;

                        ObjectId planPolylineId = new ObjectId(new IntPtr(Convert.ToInt64(xDataObj.PlanPolylineId)));
                        Polyline planPolyline = tr.GetObject(planPolylineId, OpenMode.ForWrite) as Polyline;

                        //Тут нужно переписать xData в полилинии на картинке, чтобы во второй и последующие разы смещение было правильным, сейчас первый раз норм отрабатывает, а вот второй и последующие косячит

                        ClippingRasterBoundary(planImage, planPolyline, verticalOffset, horizontalOffset);
                        planImage.TransformBy(Matrix3d.Displacement(-new Vector3d(-horizontalOffset, -verticalOffset, 0)));
                        tr.Commit();
                    }
                }
                else
                {
                    //Получить все полилинии
                    //если была отредактирована линия, которая у другой полилинии сохранена как связанная, то нужно удалить связанную полилинию и связанную картинку с плана
                }
            }
            else if (e.DBObject is RasterImage image)
            {
                //Получить все полилинии
                //если была удалена картинка слеба, которая была связанная у какой-то линии на слебе, то удалить все связанные объекты
                //если была была удалена картинка на плане, то удалить все связанные линии, но картинку слеба не трогать
            }
            //ВЕЗДЕ НЕ ЗАБЫТЬ ПОМЕНЯТЬ XDATA на новую, если стараю не актуальная
        }

        private static bool IsPointEquals(Point3d point1, Point point2)
        {
            return Math.Round(point1.X, 8) == Math.Round(point2.X, 8)
                && Math.Round(point1.Y, 8) == Math.Round(point2.Y, 8)
                && Math.Round(point1.Z, 8) == Math.Round(point2.Z, 8);

        }

        private static ObjectId SelectOriginalImage()
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
                throw new System.Exception("\nОтмена выбора точки.");
            }
            return ppr.Value;
        }

        private static ObjectId SelectOriginalPolyLine()
        {
            PromptEntityOptions peo = new PromptEntityOptions("\nВыберите полилинию: ");
            peo.SetRejectMessage("\nВыбранный объект не является полилинией.");
            peo.AddAllowedClass(typeof(Polyline), exactMatch: true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                throw new System.Exception("\nОтмена или некорректный выбор.");
            }
            return per.ObjectId;
        }

        private static void AddXDataToEntity(Polyline polyline,
            ObjectId planPolylineId,
            ObjectId planImageId,
            ObjectId slabImageId)
        {
            Database db = polyline.Database;

            RegAppTable regAppTable = (RegAppTable)db.RegAppTableId.GetObject(OpenMode.ForRead);
            if (!regAppTable.Has(appName))
            {
                regAppTable.UpgradeOpen();
                RegAppTableRecord regAppRecord = new RegAppTableRecord { Name = appName };
                regAppTable.Add(regAppRecord);
                db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(regAppRecord, true);
            }
            Point3d point = GetVertexes(polyline).First();
            var jsonXData = new XDataJson()
            {
                PlanImageId = planImageId.ToString().Replace("(", "").Replace(")", ""),
                PlanPolylineId = planPolylineId.ToString().Replace("(", "").Replace(")", ""),
                SlabImageId = slabImageId.ToString().Replace("(", "").Replace(")", ""),
                FirstVertex = new Point()
                {
                    X = point.X,
                    Y = point.Y,
                    Z = point.Z
                }
            };

            string json;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(XDataJson));
                serializer.WriteObject(ms, jsonXData);
                json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
            ResultBuffer rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, json)
            );

            polyline.XData = rb;
        }

        private static List<Point3d> GetVertexes(Polyline polyline)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }
            return points;
        }
    }
}
