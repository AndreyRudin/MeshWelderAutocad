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
    //TODO зеркальность проверить!
    internal class Command
    {
        private static Dtos.Panel _panel;
        private static Transaction _activeTransaction;
        private static LayerTable _layerTable;
        private static BlockTableRecord _modelSpace;
        [CommandMethod("CreateDrawingsForLaser")]
        public static void CreateDrawingsForLaser()
        {
            try
            {
                string jsonFilePath = SelectJSON();
                Data data = GetData(jsonFilePath);
                string generalDwgDirectory = CreateDirectoryForDrawings(jsonFilePath, data.RevitFileName);

                string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
                string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

                foreach (var panel in data.Panels)
                {
                    _panel = panel;
                    Document newDoc = acadApp.DocumentManager.Add(templatePath);
                    Database db = newDoc.Database;

                    var path = Path.Combine(generalDwgDirectory, $"{panel.Name}.dxf");

                    using (DocumentLock docLock = newDoc.LockDocument())
                    {
                        CreateLayers(db, panel);
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            _activeTransaction = tr;
                            BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            _modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            _layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                            CreateFormwork();
                            CreateOpenings();
                            CreateConnections(_panel.Connection1, "3. Связи 1");
                            CreateConnections(_panel.Connection2, "4. Связи 2");
                            //CreateEmbeddedDetail5();
                            //CreateEmbeddedDetail6And9();
                            //CreateEmbeddedDetail7();
                            //CreatePockets();
                            //CreateAnchors();
                            tr.Commit();
                        }
                        newDoc.Database.DxfOut(path, 12, DwgVersion.AC1024);
                    }
                    //newDoc.CloseAndDiscard();
                }
                //File.Delete(jsonFilePath);
            }
            catch(CustomException e)
            {
                MessageBox.Show(e.Message, "Ошибка");
            }
            catch(System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace, "Системная ошибка");
            }
        }

        private static void CreateConnections(List<Connection> connections, string layerName)
        {
            if (connections.Count != 0)
            {
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
                //TODO диагональные связи обработать
                CreateLine(connection.X - 50, connection.Y, connection.X + 50, connection.Y, layerId);
                CreateLine(connection.X, connection.Y - 50, connection.X, connection.Y + 50, layerId);
            }
            else
            {
                CreateLine(connection.X - 50, connection.Y, connection.X + 50, connection.Y, layerId);
                CreateLine(connection.X, connection.Y - 50, connection.X, connection.Y + 50, layerId);
            }
        }

        private static void CreateAnchors()
        {
            throw new NotImplementedException();
        }

        private static void CreatePockets()
        {
            throw new NotImplementedException();
        }

        private static void CreateEmbeddedDetail7()
        {
            throw new NotImplementedException();
        }

        private static void CreateEmbeddedDetail6And9()
        {
            throw new NotImplementedException();
        }

        private static void CreateEmbeddedDetail5()
        {
            throw new NotImplementedException();
        }
        private static void CreateOpenings()
        {
            //TODO у нижней линии дубляж с опалубкой может быть получается
            ObjectId layerId = _layerTable["1. Опалубка"];
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

        private static void CreateFormwork()
        {
            ObjectId layerId = _layerTable["1. Опалубка"];
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

        private static void CreateLayers(Database db, Dtos.Panel panel)
        {
            CreateLayer(db, "1. Опалубка");
            CreateLayer(db, "2. Петли");
            CreateLayer(db, "3. Связи 1");
            if (panel.Connection2.Count != 0) CreateLayer(db, "4. Связи 2");
            CreateLayer(db, "5. Анкера");
            CreateLayer(db, "6. ЗД 1.6 (ЗД 1.9)");
            CreateLayer(db, "7. Карманы");
            CreateLayer(db, "8. ЗД 1.7");
            CreateLayer(db, "9. ЗД 1.5");
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
                throw new CustomException("Файл не был выбран");

            return openFileDialog.FileName;
        }
    }
}
