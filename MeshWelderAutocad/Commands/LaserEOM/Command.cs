using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Commands.Laser.Dtos;
using MeshWelderAutocad.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MeshWelderAutocad.Commands.LaserEOM
{
    internal class Command
    {
        private static Panel _panel;
        private static Transaction _activeTransaction;
        private static LayerTable _layerTable;
        private static BlockTableRecord _modelSpace;
        private static Database _db;
        [CommandMethod("CreateDrawingsForLaserEOM")]
        public static void CreateDrawingsForLaser()
        {
            try
            {
                string jsonFilePath = SelectJSON();
                if (string.IsNullOrWhiteSpace(jsonFilePath))
                {
                    return;
                }
                Data data = GetData(jsonFilePath);
                string generalDwgDirectory = CreateDirectoryForDrawings(jsonFilePath, data.RevitFileName);

                string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
                string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

                foreach (var panel in data.Panels)
                {
                    _panel = panel;
                    Document newDoc = acadApp.DocumentManager.Add(templatePath);
                    _db = newDoc.Database;

                    var path = Path.Combine(generalDwgDirectory, $"{panel.Name}.dxf");

                    using (DocumentLock docLock = newDoc.LockDocument())
                    {
                        using (Transaction tr = _db.TransactionManager.StartTransaction())
                        {
                            _activeTransaction = tr;
                            BlockTable blockTable = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            _modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            _layerTable = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForWrite);
                            CreateFormwork("Опалубка");
                            CreateLayer(_db, "Электрика");
                            CreateBoxes("Электрика", _panel.Boxes);
                            CreatePipes("Электрика", _panel.Pipes);
                            tr.Commit();
                        }
                        newDoc.Database.DxfOut(path, 12, DwgVersion.AC1024);
                    }
                    newDoc.CloseAndDiscard();
                }
                File.Delete(jsonFilePath);
            }
            catch (CustomException e)
            {
                MessageBox.Show(e.Message, "Ошибка");
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace, "Системная ошибка");
            }
        }

        private static void CreatePipes(string layerName, List<Pipe> pipes)
        {
            ObjectId layerId = _layerTable[layerName];
            for (int i = 0; i < pipes.Count; i++)
            {
                Pipe currentPipe = pipes[i];
                CreateLine(currentPipe.StartX, currentPipe.StartY, currentPipe.EndX, currentPipe.EndY, layerId);
                if (i < pipes.Count - 1)
                {
                    Pipe nextPipe = pipes[i + 1];
                    CreateArcFromTwoPoints(currentPipe.EndX, currentPipe.EndY, nextPipe.StartX, nextPipe.StartY, 58, layerId);
                }
            }
        }
        private static void CreateArcFromTwoPoints(double x1, double y1, double x2, double y2, double radius, ObjectId layerId)
        {
            Point3d startPoint = new Point3d(x1, y1, 0);
            Point3d endPoint = new Point3d(x2, y2, 0);

            Vector3d chordVector = endPoint - startPoint;
            double chordLength = chordVector.Length;

            Point3d midPoint = new Point3d(
                (startPoint.X + endPoint.X) / 2.0,
                (startPoint.Y + endPoint.Y) / 2.0,
                0);

            double distanceToCenter = Math.Sqrt(radius * radius - (chordLength * chordLength) / 4.0);

            Vector3d perpendicular = new Vector3d(-chordVector.Y, chordVector.X, 0);
            perpendicular = perpendicular.GetNormal();

            Point3d center = midPoint + perpendicular * distanceToCenter;

            Vector3d startVector = startPoint - center;
            Vector3d endVector = endPoint - center;

            double startAngle = Math.Atan2(startVector.Y, startVector.X);
            double endAngle = Math.Atan2(endVector.Y, endVector.X);

            Autodesk.AutoCAD.DatabaseServices.Arc arc =
                new Autodesk.AutoCAD.DatabaseServices.Arc(
                    center,          
                    Vector3d.ZAxis,  
                    radius,          
                    startAngle,      
                    endAngle         
                );

            arc.LayerId = layerId;

            _modelSpace.AppendEntity(arc);
            _activeTransaction.AddNewlyCreatedDBObject(arc, true);
        }
        private static void CreateBoxes(string layerName, List<Box> boxes)
        {
            ObjectId layerId = _layerTable[layerName];
            foreach (var box in boxes)
            {
                CreateCircle(box.CenterX, box.CenterY, 70, layerId);
            }
        }
        private static void CreateCircle(double centerX, double centerY, double radius, ObjectId layerId)
        {
            Point3d center = new Point3d(centerX, centerY, 0);
            Vector3d normal = Vector3d.ZAxis;

            Autodesk.AutoCAD.DatabaseServices.Circle circle =
                new Autodesk.AutoCAD.DatabaseServices.Circle(center, normal, radius);

            circle.LayerId = layerId;

            _modelSpace.AppendEntity(circle);
            _activeTransaction.AddNewlyCreatedDBObject(circle, true);
        }

        public static void CreateLayer(Database db, string name)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                using (LayerTableRecord layer = new LayerTableRecord())
                {
                    layer.Name = name;
                    layerTable.UpgradeOpen();
                    ObjectId layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                tr.Commit();
                tr.Dispose();
            }
        }
        private static void CreateFormwork(string layerName)
        {
            CreateLayer(_db, layerName);
            ObjectId layerId = _layerTable[layerName];
            CreateLine(_panel.Formwork.MinXPanel, _panel.Formwork.MinYPanel,
                       _panel.Formwork.MinXPanel, _panel.Formwork.MaxYPanel, layerId);
            CreateLine(_panel.Formwork.MinXPanel, _panel.Formwork.MaxYPanel,
                       _panel.Formwork.MaxXPanel, _panel.Formwork.MaxYPanel, layerId);
            CreateLine(_panel.Formwork.MaxXPanel, _panel.Formwork.MaxYPanel,
                       _panel.Formwork.MaxXPanel, _panel.Formwork.MinYPanel, layerId);
            CreateLine(_panel.Formwork.MaxXPanel, _panel.Formwork.MinYPanel,
                       _panel.Formwork.MinXPanel, _panel.Formwork.MinYPanel, layerId);
        }
        private static void CreateLine(double x1, double y1, double x2, double y2, ObjectId layerId)
        {
            Autodesk.AutoCAD.DatabaseServices.Line line = new Autodesk.AutoCAD.DatabaseServices.Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            line.LayerId = layerId;
            _modelSpace.AppendEntity(line);
            _activeTransaction.AddNewlyCreatedDBObject(line, true);
        }
        private static string CreateDirectoryForDrawings(string jsonFilePath, string revitFileName)
        {
            string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
            string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
            string generalDwgDirectory = Path.Combine(jsonDirectory, $"{revitFileName}_DWG-{timeStamp}");
            Directory.CreateDirectory(generalDwgDirectory);
            return generalDwgDirectory;
        }
        private static Data GetData(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);

            try
            {
                var data = JsonConvert.DeserializeObject<Data>(jsonContent);

                if (data == null)
                    throw new CustomException("Некорректный JSON. Требуется выбрать корректный файл");

                return data;
            }
            catch (JsonException ex)
            {
                throw new CustomException("Ошибка при чтении JSON: " + ex.Message);
            }
        }
        private static string SelectJSON()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            var result = openFileDialog.ShowDialog();

            if (result != DialogResult.OK)
                return string.Empty;

            return openFileDialog.FileName;
        }
    }
}
