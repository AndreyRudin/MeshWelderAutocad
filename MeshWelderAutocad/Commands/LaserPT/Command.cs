using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcLine = Autodesk.AutoCAD.DatabaseServices.Line;

namespace MeshWelderAutocad.Commands.LaserPT
{
    internal class Command
    {
        private const string FormworkLayerName = "Опалубка";
        private const string LoopsLayerName = "Петли";
        private const string PocketsLayerName = "Карманы";
        private const string OpeningsLayerName = "Вырезы";
        private const double LoopCrossLineLength = 300.0;

        private static PanelPtDto _panel;
        private static Transaction _activeTransaction;
        private static LayerTable _layerTable;
        private static BlockTableRecord _modelSpace;
        private static Database _db;

        [CommandMethod("CreateDrawingsForLaserPT")]
        public static void CreateDrawingsForLaserPT()
        {
            try
            {
                string jsonFilePath = SelectJSON();
                if (string.IsNullOrWhiteSpace(jsonFilePath))
                    return;

                DataPtDto data = GetData(jsonFilePath);
                string generalDwgDirectory = GetOutputDrawingsDirectoryPath(jsonFilePath, "LaserPT");
                var plannedDxfPaths = new List<string>();
                foreach (PanelPtDto panel in data.Panels)
                {
                    string panelName = GetSafeFileName(string.IsNullOrWhiteSpace(panel.AssemblyName) ? "PanelPT" : panel.AssemblyName);
                    plannedDxfPaths.Add(Path.Combine(generalDwgDirectory, $"{panelName}.dxf"));
                }
                if (!ExportPathValidation.TryValidateDxfOutputPaths(plannedDxfPaths, out string pathLengthError))
                {
                    MessageBox.Show(pathLengthError, "Ошибка");
                    return;
                }
                Directory.CreateDirectory(generalDwgDirectory);
                string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
                string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

                foreach (PanelPtDto panel in data.Panels)
                {
                    _panel = panel;
                    Document newDoc = acadApp.DocumentManager.Add(templatePath);
                    _db = newDoc.Database;
                    string panelName = GetSafeFileName(string.IsNullOrWhiteSpace(panel.AssemblyName) ? "PanelPT" : panel.AssemblyName);
                    string path = Path.Combine(generalDwgDirectory, $"{panelName}.dxf");

                    using (DocumentLock docLock = newDoc.LockDocument())
                    {
                        _db.Insunits = UnitsValue.Millimeters;
                        using (Transaction tr = _db.TransactionManager.StartTransaction())
                        {
                            _activeTransaction = tr;
                            BlockTable blockTable = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            _modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            _layerTable = tr.GetObject(_db.LayerTableId, OpenMode.ForWrite) as LayerTable;

                            CreateBoundaries();
                            CreateLoops();
                            CreatePockets();
                            CreateOpenings();
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

        private static void CreateBoundaries()
        {
            ObjectId layerId = GetOrCreateLayerId(FormworkLayerName, 3);
            CreateLines(_panel.Boundaries, layerId);
        }

        private static void CreateLoops()
        {
            ObjectId layerId = GetOrCreateLayerId(LoopsLayerName, 6);
            double halfLength = LoopCrossLineLength / 2.0;
            foreach (Point2Dto loop in _panel.Loops)
            {
                CreateLine(loop.X - halfLength, loop.Y, loop.X + halfLength, loop.Y, layerId);
                CreateLine(loop.X, loop.Y - halfLength, loop.X, loop.Y + halfLength, layerId);
            }
        }

        private static void CreatePockets()
        {
            ObjectId layerId = GetOrCreateLayerId(PocketsLayerName, 4);
            foreach (List<Line2Dto> pocket in _panel.Pockets)
                CreateLines(pocket, layerId);
        }

        private static void CreateOpenings()
        {
            ObjectId layerId = GetOrCreateLayerId(OpeningsLayerName, 1);
            foreach (List<Line2Dto> opening in _panel.OpeningsLines)
                CreateLines(opening, layerId);
        }

        private static void CreateLines(IEnumerable<Line2Dto> lines, ObjectId layerId)
        {
            if (lines == null)
                return;

            foreach (Line2Dto line in lines)
            {
                if (line?.Start == null || line.End == null)
                    continue;
                CreateLine(line.Start.X, line.Start.Y, line.End.X, line.End.Y, layerId);
            }
        }

        private static ObjectId GetOrCreateLayerId(string layerName, short colorIndex)
        {
            if (_layerTable.Has(layerName))
                return _layerTable[layerName];

            LayerTableRecord layer = new LayerTableRecord();
            layer.Name = layerName;
            layer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
            _layerTable.UpgradeOpen();
            ObjectId layerId = _layerTable.Add(layer);
            _activeTransaction.AddNewlyCreatedDBObject(layer, true);
            return layerId;
        }

        private static void CreateLine(double x1, double y1, double x2, double y2, ObjectId layerId)
        {
            AcLine line = new AcLine(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            line.LayerId = layerId;
            _modelSpace.AppendEntity(line);
            _activeTransaction.AddNewlyCreatedDBObject(line, true);
        }

        private static string GetOutputDrawingsDirectoryPath(string jsonFilePath, string drawingsName)
        {
            string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
            string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
            return Path.Combine(jsonDirectory, $"{drawingsName}_DWG-{timeStamp}");
        }

        private static string GetSafeFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');
            return fileName;
        }

        private static DataPtDto GetData(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            try
            {
                DataPtDto data = JsonConvert.DeserializeObject<DataPtDto>(jsonContent);
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return string.Empty;
            return openFileDialog.FileName;
        }
    }
}
