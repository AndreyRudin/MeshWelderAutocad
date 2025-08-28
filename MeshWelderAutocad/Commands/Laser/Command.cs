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
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MeshWelderAutocad.Commands.Laser
{
    internal class Command
    {
        private static Dtos.Panel _panel;
        private static Transaction _activeTransaction;
        private static LayerTable _layerTable;
        private static BlockTableRecord _modelSpace;
        private static Database _db;

        private static double _widthDetail5 = 300.0;
        private static double _heightDetail5 = 240.0;
        private static double _widthDetail6 = 220.0;
        private static double _heightDetail7 = 240.0;
        private static double _heightDetail8 = 240.0;
        private static double _widthDetail9 = 300.0;
        private static double _widthDetail11 = 300.0;
        private static double _widthPockets = 150.0;
        private static double _heightPockets = 165.0;


        [CommandMethod("CreateDrawingsForLaser")]
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
                            CreateOpenings("Опалубка");
                            CreateLoops($"Петли");
                            for (int i = 0; i < _panel.ConnectionsGroups.Count; i++)
                                CreateConnections(_panel.ConnectionsGroups[i], $"Связи {i + 1}");
                            CreateAnchors($"Анкера");
                            CreatePockets($"Карманы");
                            if (_panel.EmbeddedParts5.Count != 0)
                                CreateEmbeddedDetail5($"ЗД 1.5");
                            if (_panel.EmbeddedParts6.Count != 0)
                                CreateEmbeddedDetail6($"ЗД 1.6", _panel.EmbeddedParts6, _widthDetail6);
                            if (_panel.EmbeddedParts7.Count != 0)
                                CreateEmbeddedDetail7($"ЗД 1.7");
                            if (_panel.EmbeddedParts8.Count != 0)
                                CreateEmbeddedDetail8($"ЗД 1.8");
                            if (_panel.EmbeddedParts9.Count != 0)
                                CreateEmbeddedDetail9($"ЗД 1.9", _panel.EmbeddedParts9, _widthDetail9);
                            if (_panel.EmbeddedParts11.Count != 0)
                                CreateEmbeddedDetail11($"ЗД 1.11");
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

        private static void CreateLoops(string layerName)
        {
            if (_panel.Loops.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var loop in _panel.Loops)
                {
                    CreateLine(loop.X, _panel.Formwork.MaxYPanel - 600, loop.X, _panel.Formwork.MaxYPanel + 200, layerId);
                }
            }
        }

        private static void CreateConnections(List<Connection> connections, string layerName)
        {
            if (connections.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var connection in connections)
                {
                    CreateConnectionLines(connection, layerId);
                }
            }
        }

        private static void CreateConnectionLines(Connection connection, ObjectId layerId)
        {
            if (connection.IsDiagonal)
            {
                CreateLine(connection.X - 50 / Math.Sqrt(2), connection.Y - 50 / Math.Sqrt(2), connection.X + 50 / Math.Sqrt(2), connection.Y + 50 / Math.Sqrt(2), layerId);
                CreateLine(connection.X - 50 / Math.Sqrt(2), connection.Y + 50 / Math.Sqrt(2), connection.X + 50 / Math.Sqrt(2), connection.Y - 50 / Math.Sqrt(2), layerId);
                if (connection.Angle == 0)
                {
                    CreateLine(connection.X, connection.Y, connection.X, connection.Y + 100, layerId);
                }
                else if (connection.Angle == 90)
                {
                    CreateLine(connection.X, connection.Y, connection.X + 100, connection.Y, layerId);
                }
                else if (connection.Angle == 180)
                {
                    CreateLine(connection.X, connection.Y, connection.X, connection.Y - 100, layerId);
                }
                else if (connection.Angle == 270)
                {
                    CreateLine(connection.X, connection.Y, connection.X - 100, connection.Y, layerId);
                }
            }
            else
            {
                CreateLine(connection.X - 50, connection.Y, connection.X + 50, connection.Y, layerId);
                CreateLine(connection.X, connection.Y - 50, connection.X, connection.Y + 50, layerId);
            }
        }

        private static void CreateAnchors(string layerName)
        {
            if (_panel.Anchors.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var anchor in _panel.Anchors)
                {
                    CreateLine(anchor.X, _panel.Formwork.MaxYPanel - 300, anchor.X, _panel.Formwork.MaxYPanel + 300, layerId);
                }
            }
        }

        private static void CreatePockets(string layerName)
        {
            if (_panel.Pockets.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var pocket in _panel.Pockets)
                {
                    double minX = pocket.X - _widthPockets / 2.0;
                    double maxX = pocket.X + _widthPockets / 2.0;
                    double minY = _panel.Formwork.MinYPanel;
                    double maxY = _panel.Formwork.MinYPanel + _heightPockets;

                    CreateLine(minX, minY, minX, maxY, layerId);
                    CreateLine(minX, maxY, maxX, maxY, layerId);
                    CreateLine(maxX, maxY, maxX, minY, layerId);
                    CreateLine(maxX, minY, minX, minY, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail7(string layerName) 
        {
            if (_panel.EmbeddedParts7.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var detail7 in _panel.EmbeddedParts7)
                {
                    double minY = detail7.Y - _heightDetail7 / 2.0;
                    double maxY = detail7.Y + _heightDetail7 / 2.0;

                    CreateLine(detail7.MinXAcad, minY, detail7.MinXAcad, maxY, layerId);
                    CreateLine(detail7.MinXAcad, maxY, detail7.MaxXAcad, maxY, layerId);
                    CreateLine(detail7.MaxXAcad, maxY, detail7.MaxXAcad, minY, layerId);
                    CreateLine(detail7.MaxXAcad, minY, detail7.MinXAcad, minY, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail8(string layerName)
        {
            if (_panel.EmbeddedParts8.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var detail8 in _panel.EmbeddedParts8)
                {
                    double minY = detail8.Y - _heightDetail8 / 2.0;
                    double maxY = detail8.Y + _heightDetail8 / 2.0;

                    CreateLine(detail8.MinXAcad, minY, detail8.MinXAcad, maxY, layerId);
                    CreateLine(detail8.MinXAcad, maxY, detail8.MaxXAcad, maxY, layerId);
                    CreateLine(detail8.MaxXAcad, maxY, detail8.MaxXAcad, minY, layerId);
                    CreateLine(detail8.MaxXAcad, minY, detail8.MinXAcad, minY, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail6(string layerName, List<EmbeddedPart> parts, double widthDetaill)
        {
            CreateLayer(_db, layerName);
            ObjectId layerId = _layerTable[layerName];
            foreach (var detail6 in _panel.EmbeddedParts6)
            {
                double maxY = detail6.Y + 30.0; // _panel.Formwork.MaxYPanel - 240.0
                double minY = detail6.Y - 30.0;
                double minX = detail6.X - _widthDetail6 / 2.0;
                double maxX = detail6.X + _widthDetail6 / 2.0;
                CreateLine(minX, minY, minX, maxY, layerId);
                CreateLine(maxX, minY, maxX, maxY, layerId);
            }
        }
        private static void CreateEmbeddedDetail11(string layerName)
        {
            CreateLayer(_db, layerName);
            ObjectId layerId = _layerTable[layerName];
            foreach (var detail11 in _panel.EmbeddedParts11)
            {
                double maxY = _panel.Formwork.MaxYPanel + 30.0;
                double minY = _panel.Formwork.MaxYPanel - 30.0;
                double minX = detail11.X - _widthDetail11 / 2.0;
                double maxX = detail11.X + _widthDetail11 / 2.0;
                CreateLine(minX, minY, minX, maxY, layerId);
                CreateLine(maxX, minY, maxX, maxY, layerId);
            }
        }
        private static void CreateEmbeddedDetail9(string layerName, List<EmbeddedPart> parts, double widthDetaill)
        {
            CreateLayer(_db, layerName);
            ObjectId layerId = _layerTable[layerName];
            foreach (var detail9 in parts)
            {
                double maxY = _panel.Formwork.MaxYPanel + 30.0;
                double minY = _panel.Formwork.MaxYPanel - 30.0;
                double minX = detail9.X - widthDetaill / 2.0;
                double maxX = detail9.X + widthDetaill / 2.0;
                CreateLine(minX, minY, minX, maxY, layerId);
                CreateLine(maxX, minY, maxX, maxY, layerId);
            }
        }

        private static void CreateEmbeddedDetail5(string layerName)
        {
            if (_panel.EmbeddedParts5.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var detail5 in _panel.EmbeddedParts5)
                {
                    double width = detail5.IsVertical ? _heightDetail5 : _widthDetail5;
                    double height = detail5.IsVertical ? _widthDetail5 : _heightDetail5;
                    double maxY = detail5.Y + height / 2.0;
                    double minY = detail5.Y - height / 2.0;
                    double minX = detail5.X - width / 2.0;
                    double maxX = detail5.X + width / 2.0;
                    CreateLine(minX, minY, minX, maxY, layerId);
                    CreateLine(minX, maxY, maxX, maxY, layerId);
                    CreateLine(maxX, maxY, maxX, minY, layerId);
                    CreateLine(maxX, minY, minX, minY, layerId);
                }
            }
        }
        private static void CreateOpenings(string layerName)
        {
            ObjectId layerId = _layerTable[layerName];
            foreach (var opening in _panel.Formwork.Openings)
            {
                foreach (var curve in opening.Curves)
                {
                    if (curve is Dtos.Arc arc)
                    {
                        CreateArc(
                            arc.StartPoint.X, arc.StartPoint.Z,
                            arc.EndPoint.X, arc.EndPoint.Z,
                            arc.Center.X, arc.Center.Z,
                            layerId);
                    }
                    else if (curve is Dtos.Circle circle)
                    {
                        CreateCircle(circle.Center.X, circle.Center.Z, circle.Radius, layerId);
                    }
                    else if (curve is Dtos.Line line)
                    {
                        Point2D p1 = line.Start;
                        Point2D p2 = line.End;

                        CreateLine(p1.X, p1.Z, p2.X, p2.Z, layerId);
                    }
                }
            }
        }
        private static void CreateArc(double startX, double startZ, double endX, double endZ, double centerX, double centerZ, ObjectId layerId)
        {

            Point3d center = new Point3d(centerX, 0, centerZ);
            Point3d start = new Point3d(startX, 0, startZ);
            Point3d end = new Point3d(endX, 0, endZ);

            // Вектор от центра до начала и до конца
            Vector3d vStart = start - center;
            Vector3d vEnd = end - center;

            double radius = vStart.Length;
            double startAngle = Vector3d.XAxis.GetAngleTo(vStart, Vector3d.YAxis);
            double endAngle = Vector3d.XAxis.GetAngleTo(vEnd, Vector3d.YAxis);

            Autodesk.AutoCAD.DatabaseServices.Arc arc = new Autodesk.AutoCAD.DatabaseServices.Arc(center, radius, startAngle, endAngle)
            {
                LayerId = layerId
            };

            _modelSpace.AppendEntity(arc);
            _activeTransaction.AddNewlyCreatedDBObject(arc, true);

        }
        private static void CreateCircle(double centerX, double centerZ, double radius, ObjectId layerId)
        {
            Point3d center = new Point3d(centerX, centerZ, 0);
            Autodesk.AutoCAD.DatabaseServices.Circle circle = new Autodesk.AutoCAD.DatabaseServices.Circle(center, Vector3d.ZAxis, radius)
            {
                LayerId = layerId
            };

            _modelSpace.AppendEntity(circle);
            _activeTransaction.AddNewlyCreatedDBObject(circle, true);
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
        //private static Data GetData(string jsonFilePath)
        //{
        //    string jsonContent = File.ReadAllText(jsonFilePath);

        //    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Data));
        //    Data data;
        //    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
        //    {
        //        object objResponse = jsonSerializer.ReadObject(stream);
        //        data = objResponse as Data;
        //    }

        //    if (data == null)
        //        throw new CustomException("Некорректный JSON. Требуется выбрать корректный файл");

        //    return data;
        //}
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
