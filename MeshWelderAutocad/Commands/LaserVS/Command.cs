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

namespace MeshWelderAutocad.Commands.LaserVS
{
    internal class Command
    {
        private const string LayerFormwork = "Опалубка";
        private const string LayerCutouts = "Вырезы";
        private const string LayerAnchors = "Анкеры";
        private const string LayerLoops = "Петли";
        private const string LayerPockets = "Карманы";

        private static readonly short[] EmbeddedDetailColorCycle =
        {
            30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200
        };

        private static PanelVsDto _panel;
        private static Transaction _activeTransaction;
        private static LayerTable _layerTable;
        private static BlockTableRecord _modelSpace;
        private static Database _db;

        [CommandMethod("CreateDrawingsForLaserVS")]
        public static void CreateDrawingsForLaserVS()
        {
            try
            {
                string jsonFilePath = SelectJSON();
                if (string.IsNullOrWhiteSpace(jsonFilePath))
                    return;

                DataVsDto data = GetData(jsonFilePath);
                string generalDwgDirectory = GetOutputDrawingsDirectoryPath(jsonFilePath, "LaserVS");
                var plannedDxfPaths = new List<string>();
                foreach (PanelVsDto panel in data.Panels)
                {
                    string panelName = GetSafeFileName(string.IsNullOrWhiteSpace(panel.AssemblyName) ? "PanelVS" : panel.AssemblyName);
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

                foreach (PanelVsDto panel in data.Panels)
                {
                    _panel = panel;
                    Document newDoc = acadApp.DocumentManager.Add(templatePath);
                    _db = newDoc.Database;
                    string panelName = GetSafeFileName(string.IsNullOrWhiteSpace(panel.AssemblyName) ? "PanelVS" : panel.AssemblyName);
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

                            CreateFormworkAndLargeOpenings();
                            CreateSmallOpenings();
                            CreateAnchors();
                            CreateLoops();
                            CreatePockets();
                            CreateEmbeddedDetailsByType();

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

        private static void CreateFormworkAndLargeOpenings()
        {
            ObjectId layerId = GetOrCreateLayerId(LayerFormwork, 3);
            CreateLines(_panel.Boundaries, layerId);
            CreateCurves(_panel.LargeOpeningsLines, layerId);
        }

        private static void CreateSmallOpenings()
        {
            ObjectId layerId = GetOrCreateLayerId(LayerCutouts, 1);
            CreateCurves(_panel.SmallOpeningsLines, layerId);
        }

        private static void CreateAnchors()
        {
            if (_panel.Anchors == null || _panel.Anchors.Count == 0)
                return;
            ObjectId layerId = GetOrCreateLayerId(LayerAnchors, 5);
            CreateLines(_panel.Anchors, layerId);
        }

        private static void CreateLoops()
        {
            if (_panel.Loops == null || _panel.Loops.Count == 0)
                return;
            ObjectId layerId = GetOrCreateLayerId(LayerLoops, 6);
            CreateLines(_panel.Loops, layerId);
        }

        private static void CreatePockets()
        {
            if (_panel.Pockets == null || _panel.Pockets.Count == 0)
                return;
            ObjectId layerId = GetOrCreateLayerId(LayerPockets, 4);
            CreateLines(_panel.Pockets, layerId);
        }

        private static void CreateEmbeddedDetailsByType()
        {
            if (_panel.EmbeddedDetails == null || _panel.EmbeddedDetails.Count == 0)
                return;
            int colorIx = 0;
            foreach (KeyValuePair<string, List<Line2Dto>> entry in _panel.EmbeddedDetails)
            {
                string layerName = string.IsNullOrWhiteSpace(entry.Key) ? "ЗД" : entry.Key.Trim();
                if (entry.Value == null || entry.Value.Count == 0)
                    continue;
                short color = EmbeddedDetailColorCycle[colorIx % EmbeddedDetailColorCycle.Length];
                colorIx++;
                ObjectId layerId = GetOrCreateLayerId(layerName, color);
                CreateLines(entry.Value, layerId);
            }
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

        private static void CreateCurves(IEnumerable<CurveDto> curves, ObjectId layerId)
        {
            if (curves == null)
                return;
            foreach (CurveDto c in curves)
            {
                if (c == null)
                    continue;
                switch (c.Kind)
                {
                    case CurveDtoKind.Line:
                        if (c.Start != null && c.End != null)
                            CreateLine(c.Start.X, c.Start.Y, c.End.X, c.End.Y, layerId);
                        break;
                    case CurveDtoKind.Arc:
                        if (c.Start != null && c.End != null && c.PointOnArc != null)
                            TryCreateArcThreePoints(c.Start, c.PointOnArc, c.End, layerId);
                        break;
                }
            }
        }

        private static void TryCreateArcThreePoints(Point2Dto a, Point2Dto mid, Point2Dto b, ObjectId layerId)
        {
            Point3d p1 = new Point3d(a.X, a.Y, 0);
            Point3d p2 = new Point3d(mid.X, mid.Y, 0);
            Point3d p3 = new Point3d(b.X, b.Y, 0);
            try
            {
                using (CircularArc3d geo = new CircularArc3d(p1, p2, p3))
                {
                    Point3d center = geo.Center;
                    double radius = geo.Radius;
                    if (radius < 1e-9)
                        return;
                    Vector3d normal = geo.Normal;
                    if (Math.Abs(Math.Abs(normal.Z) - 1.0) > 1e-6)
                    {
                        CreateLine(a.X, a.Y, b.X, b.Y, layerId);
                        return;
                    }
                    Vector3d vStart = p1 - center;
                    Vector3d vEnd = p3 - center;
                    Vector3d refAxis = Vector3d.XAxis;
                    double startAngle = refAxis.GetAngleTo(vStart, normal);
                    double endAngle = refAxis.GetAngleTo(vEnd, normal);
                    if (endAngle < startAngle)
                        endAngle += Math.PI * 2.0;
                    var arc = new Autodesk.AutoCAD.DatabaseServices.Arc(center, normal, radius, startAngle, endAngle)
                    {
                        LayerId = layerId
                    };
                    _modelSpace.AppendEntity(arc);
                    _activeTransaction.AddNewlyCreatedDBObject(arc, true);
                }
            }
            catch
            {
                CreateLine(a.X, a.Y, b.X, b.Y, layerId);
            }
        }

        private static ObjectId GetOrCreateLayerId(string layerName, short colorIndex)
        {
            if (_layerTable.Has(layerName))
                return _layerTable[layerName];

            LayerTableRecord layer = new LayerTableRecord();
            layer.Name = layerName;
            layer.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
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

        private static DataVsDto GetData(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            try
            {
                DataVsDto data = JsonConvert.DeserializeObject<DataVsDto>(jsonContent);
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
