using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Exception = System.Exception;

namespace MeshWelderAutocad.Commands.LaserMerge
{
    /// <summary>
    /// Объединение чертежей конструктивного лазера (НС и ВС панели) и лазера электрики (ЭУИ):
    /// база — DXF из папки конструктивного лазера, в неё добавляются объекты из DXF лазера электрики со всех слоёв, кроме "0" и "Опалубка".
    /// Габариты контура панели (слой "Опалубка") должны совпадать.
    /// </summary>
    internal class Command
    {
        private const string LayerFormwork = "Опалубка";
        private const string LayerZero = "0";
        private const double ExtentsTolerance = 0.0;

        private const string DefaultStructuralLaserFolder = @"C:\Users\Acer\Downloads\Vova\лазер обычный баги\3НСц-16_DWG-14.03.26__04-06-10";
        private const string DefaultElectricalLaserFolder = @"C:\Users\Acer\Downloads\Vova\лазер обычный баги\электро";
        private const string DefaultOutputFolder = @"C:\Users\Acer\Downloads\Vova\лазер обычный баги\out";


        [CommandMethod("MergeLaserWithEOM")]
        public static void MergeLaserWithEOM()
        {
            try
            {
                //string structuralLaserFolder = DefaultStructuralLaserFolder;
                //string electricalLaserFolder = DefaultElectricalLaserFolder;
                //string outputFolder = DefaultOutputFolder;

                string structuralLaserFolder = SelectFolder("Укажите папку с чертежами DXF конструктивного лазера (НС и ВС панели):");
                if (string.IsNullOrEmpty(structuralLaserFolder)) 
                    return;

                string electricalLaserFolder = SelectFolder("Укажите папку с чертежами DXF лазера электрики (ЭУИ):");
                if (string.IsNullOrEmpty(electricalLaserFolder)) 
                    return;

                string outputFolder = SelectFolder("Укажите папку для сохранения объединённых DXF:");
                if (string.IsNullOrEmpty(outputFolder)) 
                    return;

                string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
                string outputRoot = Path.Combine(outputFolder, $"LaserMerge_DWG-{timeStamp}");
                Directory.CreateDirectory(outputRoot);

                List<string> structuralLaserDxfPaths = GetDxfPathsRecursive(structuralLaserFolder);
                if (structuralLaserDxfPaths.Count == 0)
                {
                    MessageBox.Show("В папке конструктивного лазера не найдено файлов DXF.", "Ошибка");
                    return;
                }

                List<string> electricalLaserDxfPaths = GetDxfPathsRecursive(electricalLaserFolder);
                if (electricalLaserDxfPaths.Count == 0)
                {
                    MessageBox.Show("В папке лазера электрики не найдено файлов DXF.", "Ошибка");
                    return;
                }

                int processed = 0;
                int skippedNoPair = 0;
                int skippedExtentMismatch = 0;
                List<string> errors = new List<string>();
                List<string> structuralLaserFilesWithoutPair = new List<string>();
                List<string> electricalLaserFilesWithoutPair = new List<string>();
                HashSet<string> usedElectricalLaserRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string structuralLaserRelativePath in structuralLaserDxfPaths)
                {
                    string structuralLaserFileName = Path.GetFileName(structuralLaserRelativePath);
                    string structuralLaserNameNoExt = Path.GetFileNameWithoutExtension(structuralLaserFileName);

                    List<string> matchingElectricalPaths = FindMatchingElectricalLaserFiles(structuralLaserFileName, electricalLaserDxfPaths);
                    if (matchingElectricalPaths.Count == 0)
                    {
                        skippedNoPair++;
                        structuralLaserFilesWithoutPair.Add(structuralLaserRelativePath);
                        continue;
                    }

                    string structuralLaserPath = Path.Combine(structuralLaserFolder, structuralLaserRelativePath);

                    foreach (string electricalLaserRelativePath in matchingElectricalPaths)
                    {
                        usedElectricalLaserRelativePaths.Add(electricalLaserRelativePath);
                        string electricalLaserFileName = Path.GetFileName(electricalLaserRelativePath);
                        string electricalLaserNameNoExt = Path.GetFileNameWithoutExtension(electricalLaserFileName);
                        string outputFileName = $"{structuralLaserNameNoExt}_{electricalLaserNameNoExt}.dxf";
                        string outputPath = Path.Combine(outputRoot, outputFileName);

                        string electricalLaserPath = Path.Combine(electricalLaserFolder, electricalLaserRelativePath);
                        bool mergeSuccess = TryMergeStructuralWithElectricalLaser(structuralLaserPath, electricalLaserPath, outputPath, out string errorMessage);
                        if (mergeSuccess)
                            processed++;
                        else if (errorMessage != null)
                        {
                            if (errorMessage.Contains("габарит"))
                                skippedExtentMismatch++;
                            errors.Add($"Файл конструктивного лазера: {structuralLaserRelativePath}, файл лазера электрики: {electricalLaserRelativePath}. {errorMessage}");
                        }
                    }
                }

                electricalLaserFilesWithoutPair.AddRange(electricalLaserDxfPaths.Where(p => !usedElectricalLaserRelativePaths.Contains(p)));

                string reportPath = Path.Combine(outputRoot, "Laser_EOM_Merge_Report.txt");
                WriteReportFile(reportPath, structuralLaserFilesWithoutPair, electricalLaserFilesWithoutPair, processed, skippedNoPair, skippedExtentMismatch, errors);

                string message;
                if (errors.Count > 0)
                    message = "Обнаружены ошибки. Подробности смотрите в отчёте.\n\nОтчёт: " + reportPath;
                else
                    message = "Всё успешно.\n\nОтчёт сохранён: " + reportPath;
                MessageBox.Show(message, "Объединение конструктивный лазер + лазер электрики");
            }
            catch (CustomException exception)
            {
                MessageBox.Show(exception.Message, "Ошибка");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + "\n" + exception.StackTrace, "Системная ошибка");
            }
        }
        /// <summary>
        /// КЖС: префикс — до первого пробела, суффикс — после последнего дефиса.
        /// Пример: "3НС 539.229.37-16" → prefix="3НС", suffix="16".
        /// </summary>
        private static void ParseStructuralFileName(string fileNameNoExt, out string panelType, out string panelCode)
        {
            panelType = "";
            panelCode = "";
            if (string.IsNullOrWhiteSpace(fileNameNoExt)) 
                return;
            int spaceIdx = fileNameNoExt.IndexOf(' ');
            panelType = spaceIdx >= 0 ? fileNameNoExt.Substring(0, spaceIdx).Trim() : fileNameNoExt.Trim();
            int lastDash = fileNameNoExt.LastIndexOf('-');
            panelCode = lastDash >= 0 && lastDash < fileNameNoExt.Length - 1 ? fileNameNoExt.Substring(lastDash + 1).Trim() : "";
        }

        /// <summary>
        /// ЭОМ: часть до первого дефиса и между первым и вторым дефисом.
        /// Пример: "3НС-16-Э2" → part1="3НС", part2="16".
        /// </summary>
        private static void ParseElectricalFileName(string fileNameNoExt, out string panelType, out string code)
        {
            panelType = "";
            code = "";
            if (string.IsNullOrWhiteSpace(fileNameNoExt)) return;
            string[] parts = fileNameNoExt.Split('-');
            if (parts.Length >= 1) panelType = parts[0].Trim();
            if (parts.Length >= 2) code = parts[1].Trim();
        }

        /// <summary>
        /// Совпадение: префикс КЖС = part1 ЭОМ и суффикс КЖС = part2 ЭОМ (без учёта регистра).
        /// </summary>
        private static bool IsMatchingStructuralAndElectricalPair(string structuralLaserFileName, string electricalLaserFileName)
        {
            string structuralName = Path.GetFileNameWithoutExtension(structuralLaserFileName);
            string electricalName = Path.GetFileNameWithoutExtension(electricalLaserFileName);
            ParseStructuralFileName(structuralName, out string panelTypeStructural, out string codeStructural);
            ParseElectricalFileName(electricalName, out string panelTypeElectrical, out string codeElectrical);
            return string.Equals(panelTypeStructural, panelTypeElectrical, StringComparison.OrdinalIgnoreCase)
                && string.Equals(codeStructural, codeElectrical, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteReportFile(
            string reportPath,
            List<string> structuralLaserFilesWithoutPair,
            List<string> electricalLaserFilesWithoutPair,
            int processedCount,
            int skippedNoPairCount,
            int skippedExtentMismatchCount,
            List<string> errors)
        {
            using (StreamWriter writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("Отчёт объединения чертежей конструктивного лазера и лазера электрики (ЭУИ)");
                writer.WriteLine("Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                writer.WriteLine();
                writer.WriteLine("Итог: обработано {0}, пропущено (нет пары) {1}, пропущено (несовпадение габаритов) {2}.", processedCount, skippedNoPairCount, skippedExtentMismatchCount);
                writer.WriteLine();
                writer.WriteLine("--- Файлы конструктивного лазера без пары (не найдено совпадение в лазере электрики) ---");
                if (structuralLaserFilesWithoutPair.Count == 0)
                    writer.WriteLine("(нет)");
                else
                    foreach (string path in structuralLaserFilesWithoutPair)
                        writer.WriteLine(path);
                writer.WriteLine();
                writer.WriteLine("--- Файлы лазера электрики без соответствий ---");
                if (electricalLaserFilesWithoutPair.Count == 0)
                    writer.WriteLine("(нет)");
                else
                    foreach (string path in electricalLaserFilesWithoutPair)
                        writer.WriteLine(path);
                if (errors.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("--- Ошибки при обработке ---");
                    foreach (string error in errors)
                        writer.WriteLine(error);
                }
            }
        }

        private static string SelectFolder(string title)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = title;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        /// <summary>
        /// Ищет все файлы лазера электрики, подходящие под данный файл КЖС (сопоставление по префиксу/суффиксу).
        /// </summary>
        private static List<string> FindMatchingElectricalLaserFiles(string structuralLaserFileName, List<string> electricalLaserRelativePaths)
        {
            var result = new List<string>();
            foreach (string electricalLaserRelativePath in electricalLaserRelativePaths)
            {
                string electricalLaserFileName = Path.GetFileName(electricalLaserRelativePath);
                if (IsMatchingStructuralAndElectricalPair(structuralLaserFileName, electricalLaserFileName))
                    result.Add(electricalLaserRelativePath);
            }
            return result;
        }

        private static List<string> GetDxfPathsRecursive(string root)
        {
            List<string> relativePaths = new List<string>();
            foreach (string path in Directory.GetFiles(root, "*.dxf", SearchOption.AllDirectories))
            {
                string relativePath = path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                relativePaths.Add(relativePath);
            }
            return relativePaths;
        }

        private static bool TryMergeStructuralWithElectricalLaser(string structuralLaserPath, string electricalLaserPath, string outputPath, out string error)
        {
            error = null;
            try
            {
                using (Database structuralLaserDatabase = new Database(false, true))
                using (Database electricalLaserDatabase = new Database(false, true))
                {
                    ReadDxf(structuralLaserDatabase, structuralLaserPath);
                    ReadDxf(electricalLaserDatabase, electricalLaserPath);

                    if (!GetContourExtents(structuralLaserDatabase, out double structuralLaserMinX, out double structuralLaserMaxX, out double structuralLaserMinY, out double structuralLaserMaxY))
                    {
                        error = "В чертеже конструктивного лазера не найден контур (слой «Опалубка»).";
                        return false;
                    }
                    if (!GetContourExtents(electricalLaserDatabase, out double electricalLaserMinX, out double electricalLaserMaxX, out double electricalLaserMinY, out double electricalLaserMaxY))
                    {
                        error = "В чертеже лазера электрики не найден контур (слой «Опалубка»).";
                        return false;
                    }

                    if (Math.Abs(structuralLaserMinX - electricalLaserMinX) > ExtentsTolerance || Math.Abs(structuralLaserMaxX - electricalLaserMaxX) > ExtentsTolerance ||
                        Math.Abs(structuralLaserMinY - electricalLaserMinY) > ExtentsTolerance || Math.Abs(structuralLaserMaxY - electricalLaserMaxY) > ExtentsTolerance)
                    {
                        error = "Несовпадение габаритов контура панели.";
                        return false;
                    }

                    ObjectIdCollection entityIdsToCopy = GetEntityIdsOnLayersOtherThan(electricalLaserDatabase, LayerZero, LayerFormwork);
                    if (entityIdsToCopy.Count == 0)
                    {
                        error = "В чертеже лазера электрики нет объектов на слоях, отличных от 0 и Опалубка.";
                        return false;
                    }

                    ObjectId modelSpaceId;
                    using (Transaction transaction = structuralLaserDatabase.TransactionManager.StartTransaction())
                    {
                        BlockTable blockTable = transaction.GetObject(structuralLaserDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                        modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                        transaction.Commit();
                    }
                    IdMapping idMapping = new IdMapping();
                    electricalLaserDatabase.WblockCloneObjects(entityIdsToCopy, modelSpaceId, idMapping, DuplicateRecordCloning.Replace, false);

                    structuralLaserDatabase.DxfOut(outputPath, 12, DwgVersion.AC1024);
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message + exception.StackTrace;
                return false;
            }
        }
        private static void ReadDxf(Database database, string path)
        {
            database.DxfIn(path, null);
        }

        /// <summary>
        /// Получает габариты контура панели по объектам на слое "Опалубка".
        /// </summary>
        private static bool GetContourExtents(Database database, out double minX, out double maxX, out double minY, out double maxY)
        {
            minX = maxX = minY = maxY = 0;
            bool found = false;
            Extents3d combinedExtents = default;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;

                foreach (ObjectId objectId in modelSpace)
                {
                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;

                    string layerName = GetLayerName(transaction, layerTable, entity.LayerId);
                    if (string.IsNullOrEmpty(layerName) || !string.Equals(layerName, LayerFormwork, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        if (!found)
                        {
                            combinedExtents = entity.GeometricExtents;
                            found = true;
                        }
                        else
                        {
                            combinedExtents.AddExtents(entity.GeometricExtents);
                        }
                    }
                    catch
                    {
                        // У части примитивов GeometricExtents может не поддерживаться
                    }
                }
                transaction.Commit();
            }

            if (!found) 
                return false;

            minX = combinedExtents.MinPoint.X;
            maxX = combinedExtents.MaxPoint.X;
            minY = combinedExtents.MinPoint.Y;
            maxY = combinedExtents.MaxPoint.Y;
            return true;
        }

        private static string GetLayerName(Transaction transaction, LayerTable layerTable, ObjectId layerId)
        {
            if (layerId.IsNull) 
                return null;
            if (!layerTable.Has(layerId)) 
                return null;
            LayerTableRecord layerTableRecord = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
            return layerTableRecord?.Name;
        }

        /// <summary>
        /// Возвращает идентификаторы объектов из модели, лежащих на слоях, отличных от указанных.
        /// </summary>
        private static ObjectIdCollection GetEntityIdsOnLayersOtherThan(Database database, params string[] excludedLayerNames)
        {
            HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in excludedLayerNames)
                excluded.Add(name ?? string.Empty);

            ObjectIdCollection entityIds = new ObjectIdCollection();
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;

                foreach (ObjectId objectId in modelSpace)
                {
                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                    if (entity == null) 
                        continue;
                    string entityLayerName = GetLayerName(transaction, layerTable, entity.LayerId);
                    if (string.IsNullOrEmpty(entityLayerName) || excluded.Contains(entityLayerName))
                        continue;
                    entityIds.Add(objectId);
                }
                transaction.Commit();
            }
            return entityIds;
        }
    }
}
