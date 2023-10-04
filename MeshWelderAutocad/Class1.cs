using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MeshWelderAutocad
{
    public class Mesh
    {
        public string Name { get; set; }
        public List<MyRebar> Rebars { get; set; } = new List<MyRebar>();
        public Mesh(string name)
        {
            Name = name;
        }
    }
    public class MyRebar
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public int Diameter { get; set; }
    }
    public class Command : IExtensionApplication
    {
        [CommandMethod("TestCom")]
        public void TestCom()
        {
            //var paths = new List<string>() { @"C:\Users\Acer\Desktop\dwg\Отметка сверху 216909055.dwg", @"C:\Users\Acer\Desktop\dwg\Отметка сверху 216909587.dwg" };
            //foreach (var filePath in paths)
            //{
            //Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(filePath, false);
            Document acDoc = acadApp.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            var editor = acDoc.Editor;
            using (DocumentLock docLock = acDoc.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    string jsonFilePath = @"C:\Users\Acer\Desktop\test.json";
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var meshs = JsonConvert.DeserializeObject<List<Mesh>>(jsonContent);
                    foreach (var mesh in meshs)
                    {
                        foreach (var rebar in mesh.Rebars)
                        {
                            Polyline polyline = new Polyline();
                            polyline.AddVertexAt(0, new Point2d(rebar.StartX, rebar.StartY), 0, 0.15, 0.15);
                            polyline.AddVertexAt(1, new Point2d(rebar.EndX, rebar.EndY), 0, 0.15, 0.15);
                            modelSpace.AppendEntity(polyline);
                            tr.AddNewlyCreatedDBObject(polyline, true);
                        }
                    }
   
                    //BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    //BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    //foreach (ObjectId blockRefId in modelSpace)
                    //{
                    //    editor.WriteMessage(blockRefId.ToString());
                    //    Entity entity = tr.GetObject(blockRefId, OpenMode.ForRead) as Entity;

                    //    if (entity is BlockReference blockReference)
                    //    {
                    //        Matrix3d transformMatrix = blockReference.BlockTransform;
                    //        // Открываем определение блока
                    //        BlockTableRecord blockDef = tr.GetObject(blockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    //        // Перебираем все объекты в определении блока и ищем отрезки
                    //        var points = new List<Point3d>();
                    //        foreach (ObjectId entityId in blockDef)
                    //        {
                    //            Entity blockEntity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                    //            if (blockEntity is Line line)
                    //            {
                    //                points.Add(line.StartPoint);
                    //                points.Add(line.EndPoint);
                    //            }
                    //        }
                    //        if (points.Count != 0)
                    //        {
                    //            double maxY = points.Select(p => p.Y).Max();
                    //            double maxX = points.Select(p => p.X).Max();
                    //            double minY = points.Select(p => p.Y).Min();
                    //            double minX = points.Select(p => p.X).Min();
                    //            bool isVerticalLine = maxY - minY > maxX - minX ? true : false;

                    //            Polyline polyline = new Polyline();
                    //            Point3d startPoint;
                    //            Point3d endPoint;
                    //            if (isVerticalLine)
                    //            {
                    //                startPoint = new Point3d(minX + (maxX - minX) / 2, minY, 0).TransformBy(transformMatrix);
                    //                endPoint = new Point3d(minX + (maxX - minX) / 2, maxY, 0).TransformBy(transformMatrix);
                    //            }
                    //            else
                    //            {
                    //                startPoint = new Point3d(minX, minY + (maxY - minY) / 2, 0).TransformBy(transformMatrix);
                    //                endPoint = new Point3d(maxX, minY + (maxY - minY) / 2, 0).TransformBy(transformMatrix);
                    //            }
                    //            polyline.AddVertexAt(0, new Point2d(startPoint.X, startPoint.Y), 0, 0.15, 0.15);
                    //            polyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0, 0.15, 0.15);
                    //            modelSpace.AppendEntity(polyline);
                    //            tr.AddNewlyCreatedDBObject(polyline, true);
                    //        }
                    //    }
                    //}
                    tr.Commit();
                }
                //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                //acDoc.Database.SaveAs($@"C:\Users\Acer\Desktop\dwg\{fileNameWithoutExtension}_out.dwg", DwgVersion.Current);
            }
            //    acDoc.CloseAndDiscard();
            //}
        }
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
           
        }
    }
}
