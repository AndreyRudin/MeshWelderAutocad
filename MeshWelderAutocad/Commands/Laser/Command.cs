using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Commands.Laser.Dtos;
using MeshWelderAutocad.Utils;
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
                            CreateFormwork("1. Опалубка");
                            CreateOpenings("1. Опалубка");
                            CreateConnections(_panel.Connection1, "3. Связи 1");
                            CreateConnections(_panel.Connection2, "4. Связи 2");
                            CreateEmbeddedDetail5("9. ЗД 1.5");
                            CreateEmbeddedDetail6And9();
                            CreateEmbeddedDetail7("8. ЗД 1.7");
                            CreatePockets("7. Карманы");
                            CreateLoops("2. Петли");
                            CreateAnchors("5. Анкера");
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
                CreateLine(connection.X - 50 / Math.Sqrt(2), connection.Y -50 / Math.Sqrt(2), connection.X + 50 / Math.Sqrt(2), connection.Y + 50 / Math.Sqrt(2), layerId);
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
            double width = 165.0;
            double height = 150.0;
            if (_panel.Pockets.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var pocket in _panel.Pockets)
                {
                    double minX = pocket.X - width / 2.0;
                    double maxX = pocket.X + width / 2.0;
                    double minY = _panel.Formwork.MinYPanel;
                    double maxY = _panel.Formwork.MinYPanel + height;

                    CreateLine(minX, minY, minX, maxY, layerId);
                    CreateLine(minX, maxY, maxX, maxY, layerId);
                    CreateLine(maxX, maxY, maxX, minY, layerId);
                    CreateLine(maxX, minY, minX, minY, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail7(string layerName)
        {
            double width = 210.0;
            double height = 240.0;
            if (_panel.EmbeddedParts7.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var detail7 in _panel.EmbeddedParts7)
                {
                    double minX = detail7.X - width / 2.0;
                    double maxX = detail7.X + width / 2.0;
                    double minY = detail7.Y - height / 2.0;
                    double maxY = detail7.Y + height / 2.0;

                    CreateLine(minX, minY, minX, maxY, layerId);
                    CreateLine(minX, maxY, maxX, maxY, layerId);
                    CreateLine(maxX, maxY, maxX, minY, layerId);
                    CreateLine(maxX, minY, minX, minY, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail6And9()
        {
            double width = 220.0;
            if (_panel.EmbeddedParts6.Count != 0)
            {
                CreateLayer(_db, "6. ЗД 1.6");
                ObjectId layerId = _layerTable["6. ЗД 1.6"];
                foreach (var detail6 in _panel.EmbeddedParts6)
                {
                    double maxY = _panel.Formwork.MaxYPanel - 240.0 + 30.0;
                    double minY = _panel.Formwork.MaxYPanel - 240.0 - 30.0;
                    double minX = detail6.X - width / 2.0;
                    double maxX = detail6.X + width / 2.0;
                    CreateLine(minX, minY, minX, maxY, layerId);
                    CreateLine(maxX, minY, maxX, maxY, layerId);
                    //CreateLine(minX , minY + 30, maxX, maxY - 30, layerId);
                }
            }
            else if (_panel.EmbeddedParts9.Count != 0)
            {
                CreateLayer(_db, "6. ЗД 1.9");
                ObjectId layerId = _layerTable["6. ЗД 1.9"];
                foreach (var detail6 in _panel.EmbeddedParts6)
                {
                    double maxY = _panel.Formwork.MaxYPanel - 240.0 + 30.0;
                    double minY = _panel.Formwork.MaxYPanel - 240.0 - 30.0;
                    double minX = detail6.X - width / 2.0;
                    double maxX = detail6.X + width / 2.0;
                    CreateLine(minX, minY, minX, maxY, layerId);
                    //CreateLine(minX, minY + 30, maxX, maxY - 30, layerId);
                }
            }
        }

        private static void CreateEmbeddedDetail5(string layerName)
        {
            double width = 360.0;
            double height = 240.0;
            if (_panel.EmbeddedParts5.Count != 0)
            {
                CreateLayer(_db, layerName);
                ObjectId layerId = _layerTable[layerName];
                foreach (var detail5 in _panel.EmbeddedParts5)
                {

                }
            }
        }
        private static void CreateOpenings(string layerName)
        {
            //TODO у нижней линии дубляж с опалубкой может быть получается
            ObjectId layerId = _layerTable[layerName];
            foreach (var opening in _panel.Formwork.Openings)
            {
                CreateLine(opening.MinX, opening.MinY,
                           opening.MinX, opening.MaxY, layerId);
                CreateLine(opening.MinX, opening.MaxY,
                           opening.MaxX, opening.MaxY, layerId);
                CreateLine(opening.MaxX, opening.MaxY,
                           opening.MaxX, opening.MinY, layerId);
                CreateLine(opening.MaxX, opening.MinY,
                           opening.MinX, opening.MinY, layerId);
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
            Line line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
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

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Data));
            Data data;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
            {
                object objResponse = jsonSerializer.ReadObject(stream);
                data = objResponse as Data;
            }

            if (data == null)
                throw new CustomException("Некорректный JSON. Требуется выбрать корректный файл");

            return data;
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
