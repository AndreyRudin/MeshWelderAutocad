//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.Runtime;
//using MeshWelderAutocad.Utils;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Windows.Forms;
//using Exception = System.Exception;

//namespace MeshWelderAutocad.Commands.LaserMerge
//{
//    /// <summary>
//    /// Объединение чертежей обычного лазера (НС и ВС панели) и лазера ЭУИ:
//    /// база — DXF из папки обычного лазера, в неё добавляются объекты из DXF лазера ЭУИ со всех слоёв, кроме "0" и "Опалубка".
//    /// Габариты контура панели (слой "Опалубка") должны совпадать.
//    /// </summary>
//    internal class Command
//    {
//        private const string LayerFormwork = "Опалубка";
//        private const string LayerZero = "0";
//        /// <summary> Полное совпадение габаритов (без допуска). </summary>
//        private const double ExtentsTolerance = 0.0;

//        [CommandMethod("MergeLaserWithEOM")]
//        public static void MergeLaserWithEOM()
//        {
//            try
//            {
//                string laserFolder = SelectFolder("Укажите папку с чертежами DXF обычного лазера (НС и ВС панели):");
//                if (string.IsNullOrEmpty(laserFolder)) return;

//                string eomFolder = SelectFolder("Укажите папку с чертежами DXF лазера ЭУИ:");
//                if (string.IsNullOrEmpty(eomFolder)) return;

//                string outputFolder = SelectFolder("Укажите папку для сохранения объединённых DXF:");
//                if (string.IsNullOrEmpty(outputFolder)) return;

//                List<string> laserDxfPaths = GetDxfPathsRecursive(laserFolder);
//                if (laserDxfPaths.Count == 0)
//                {
//                    MessageBox.Show("В папке обычного лазера не найдено файлов DXF.", "Ошибка");
//                    return;
//                }

//                List<string> eomDxfPaths = GetDxfPathsRecursive(eomFolder);

//                int processed = 0;
//                int skippedNoPair = 0;
//                int skippedMismatch = 0;
//                List<string> errors = new List<string>();
//                List<string> laserFilesWithoutPair = new List<string>();
//                HashSet<string> usedEomRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//                foreach (string laserRelativePath in laserDxfPaths)
//                {
//                    string laserPath = Path.Combine(laserFolder, laserRelativePath);
//                    string laserFileName = Path.GetFileName(laserRelativePath);

//                    string eomRelativePath = FindMatchingEomFile(laserFileName, eomDxfPaths);
//                    if (eomRelativePath == null)
//                    {
//                        skippedNoPair++;
//                        laserFilesWithoutPair.Add(laserRelativePath);
//                        continue;
//                    }

//                    usedEomRelativePaths.Add(eomRelativePath);
//                    string eomPath = Path.Combine(eomFolder, eomRelativePath);
//                    string outputPath = Path.Combine(outputFolder, laserRelativePath);
//                    string outputDirectory = Path.GetDirectoryName(outputPath);
//                    if (!string.IsNullOrEmpty(outputDirectory))
//                        Directory.CreateDirectory(outputDirectory);

//                    bool mergeSuccess = TryMergePair(laserPath, eomPath, outputPath, out string errorMessage);
//                    if (mergeSuccess)
//                        processed++;
//                    else if (errorMessage != null)
//                    {
//                        if (errorMessage.Contains("габарит"))
//                            skippedMismatch++;
//                        errors.Add($"Файл лазера: {laserRelativePath}, файл ЭУИ: {eomRelativePath}. {errorMessage}");
//                    }
//                }

//                List<string> eomFilesWithoutPair = eomDxfPaths.Where(path => !usedEomRelativePaths.Contains(path)).ToList();

//                string reportPath = Path.Combine(outputFolder, "Laser_EOM_Merge_Report.txt");
//                WriteReportFile(reportPath, laserFilesWithoutPair, eomFilesWithoutPair, processed, skippedNoPair, skippedMismatch, errors);

//                string message;
//                if (errors.Count > 0)
//                    message = "Обнаружены ошибки. Подробности смотрите в отчёте.\n\nОтчёт: " + reportPath;
//                else
//                    message = "Всё успешно.\n\nОтчёт сохранён: " + reportPath;
//                MessageBox.Show(message, "Объединение лазер + ЭУИ");
//            }
//            catch (CustomException exception)
//            {
//                MessageBox.Show(exception.Message, "Ошибка");
//            }
//            catch (Exception exception)
//            {
//                MessageBox.Show(exception.Message + "\n" + exception.StackTrace, "Системная ошибка");
//            }
//        }

//        private static void WriteReportFile(
//            string reportPath,
//            List<string> laserFilesWithoutPair,
//            List<string> eomFilesWithoutPair,
//            int processedCount,
//            int skippedNoPairCount,
//            int skippedMismatchCount,
//            List<string> errors)
//        {
//            using (StreamWriter writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8))
//            {
//                writer.WriteLine("Отчёт объединения чертежей лазера и лазера ЭУИ");
//                writer.WriteLine("Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
//                writer.WriteLine();
//                writer.WriteLine("Итог: обработано {0}, пропущено (нет пары) {1}, пропущено (несовпадение габаритов) {2}.", processedCount, skippedNoPairCount, skippedMismatchCount);
//                writer.WriteLine();
//                writer.WriteLine("--- Файлы обычного лазера без пары (не найдено совпадение в ЭУИ) ---");
//                if (laserFilesWithoutPair.Count == 0)
//                    writer.WriteLine("(нет)");
//                else
//                    foreach (string path in laserFilesWithoutPair)
//                        writer.WriteLine(path);
//                writer.WriteLine();
//                writer.WriteLine("--- Файлы лазера ЭУИ без пары (не использованы) ---");
//                if (eomFilesWithoutPair.Count == 0)
//                    writer.WriteLine("(нет)");
//                else
//                    foreach (string path in eomFilesWithoutPair)
//                        writer.WriteLine(path);
//                if (errors.Count > 0)
//                {
//                    writer.WriteLine();
//                    writer.WriteLine("--- Ошибки при обработке ---");
//                    foreach (string error in errors)
//                        writer.WriteLine(error);
//                }
//            }
//        }

//        private static string SelectFolder(string title)
//        {
//            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
//            {
//                dialog.Description = title;
//                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
//            }
//        }

//        /// <summary>
//        /// Определяет, соответствуют ли друг другу файл обычного лазера и файл лазера ЭУИ.
//        /// </summary>
//        /// <param name="laserFileName">Имя файла из папки обычного лазера.</param>
//        /// <param name="eomFileName">Имя файла из папки лазера ЭУИ.</param>
//        /// <returns>true, если файлы образуют пару для объединения.</returns>
//        private static bool IsMatchingPair(string laserFileName, string eomFileName)
//        {
//            // TODO: реализация сопоставления имён файлов
//            return false;
//        }

//        /// <summary>
//        /// Ищет в списке путей ЭУИ файл, сопоставленный заданному имени файла лазера.
//        /// </summary>
//        private static string FindMatchingEomFile(string laserFileName, List<string> eomRelativePaths)
//        {
//            foreach (string eomRelativePath in eomRelativePaths)
//            {
//                string eomFileName = Path.GetFileName(eomRelativePath);
//                if (IsMatchingPair(laserFileName, eomFileName))
//                    return eomRelativePath;
//            }
//            return null;
//        }

//        private static List<string> GetDxfPathsRecursive(string root)
//        {
//            List<string> relativePaths = new List<string>();
//            foreach (string path in Directory.GetFiles(root, "*.dxf", SearchOption.AllDirectories))
//            {
//                string relativePath = path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
//                relativePaths.Add(relativePath);
//            }
//            return relativePaths;
//        }

//        private static bool TryMergePair(string laserPath, string eomPath, string outputPath, out string error)
//        {
//            error = null;
//            try
//            {
//                using (Database laserDatabase = new Database(false, true))
//                using (Database eomDatabase = new Database(false, true))
//                {
//                    ReadDxfOrDwg(laserDatabase, laserPath);
//                    ReadDxfOrDwg(eomDatabase, eomPath);

//                    laserDatabase.UpdateExt(true);
//                    eomDatabase.UpdateExt(true);

//                    if (!GetContourExtents(laserDatabase, out double laserMinX, out double laserMaxX, out double laserMinY, out double laserMaxY))
//                    {
//                        error = "В чертеже лазера не найден контур (слой «Опалубка»).";
//                        return false;
//                    }
//                    if (!GetContourExtents(eomDatabase, out double eomMinX, out double eomMaxX, out double eomMinY, out double eomMaxY))
//                    {
//                        error = "В чертеже ЭУИ не найден контур (слой «Опалубка»).";
//                        return false;
//                    }

//                    if (Math.Abs(laserMinX - eomMinX) > ExtentsTolerance || Math.Abs(laserMaxX - eomMaxX) > ExtentsTolerance ||
//                        Math.Abs(laserMinY - eomMinY) > ExtentsTolerance || Math.Abs(laserMaxY - eomMaxY) > ExtentsTolerance)
//                    {
//                        error = "Несовпадение габаритов контура панели.";
//                        return false;
//                    }

//                    ObjectIdCollection entityIdsToCopy = GetEntityIdsOnLayersOtherThan(eomDatabase, LayerZero, LayerFormwork);
//                    if (entityIdsToCopy.Count == 0)
//                    {
//                        error = "В чертеже ЭУИ нет объектов на слоях, отличных от 0 и Опалубка.";
//                        return false;
//                    }

//                    ObjectId modelSpaceId;
//                    using (Transaction transaction = laserDatabase.TransactionManager.StartTransaction())
//                    {
//                        BlockTable blockTable = transaction.GetObject(laserDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
//                        modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
//                        transaction.Commit();
//                    }
//                    IdMapping idMapping = new IdMapping();
//                    eomDatabase.WblockCloneObjects(entityIdsToCopy, modelSpaceId, idMapping, DuplicateRecordCloning.Replace, false);

//                    laserDatabase.DxfOut(outputPath, 12, DwgVersion.AC1024);
//                    return true;
//                }
//            }
//            catch (System.Exception exception)
//            {
//                error = exception.Message;
//                return false;
//            }
//        }

//        private static void ReadDxfOrDwg(Database database, string path)
//        {
//            // Чтение DWG; для DXF в части версий AutoCAD может потребоваться предварительное сохранение в DWG
//            database.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, "");
//        }

//        /// <summary>
//        /// Получает габариты контура панели по объектам на слое "Опалубка".
//        /// </summary>
//        private static bool GetContourExtents(Database database, out double minX, out double maxX, out double minY, out double maxY)
//        {
//            minX = maxX = minY = maxY = 0;
//            bool found = false;
//            Extents3d combinedExtents = default;

//            using (Transaction transaction = database.TransactionManager.StartTransaction())
//            {
//                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
//                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
//                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;

//                foreach (ObjectId objectId in modelSpace)
//                {
//                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
//                    if (entity == null) continue;

//                    string layerName = GetLayerName(transaction, layerTable, entity.LayerId);
//                    if (string.IsNullOrEmpty(layerName) || !string.Equals(layerName, LayerFormwork, StringComparison.OrdinalIgnoreCase))
//                        continue;

//                    try
//                    {
//                        if (!found)
//                        {
//                            combinedExtents = entity.GeometricExtents;
//                            found = true;
//                        }
//                        else
//                        {
//                            combinedExtents.AddExtents(entity.GeometricExtents);
//                        }
//                    }
//                    catch
//                    {
//                        // У части примитивов GeometricExtents может не поддерживаться
//                    }
//                }
//                transaction.Commit();
//            }

//            if (!found) return false;

//            minX = combinedExtents.MinPoint.X;
//            maxX = combinedExtents.MaxPoint.X;
//            minY = combinedExtents.MinPoint.Y;
//            maxY = combinedExtents.MaxPoint.Y;
//            return true;
//        }

//        private static string GetLayerName(Transaction transaction, LayerTable layerTable, ObjectId layerId)
//        {
//            if (layerId.IsNull) return null;
//            if (!layerTable.Has(layerId)) return null;
//            LayerTableRecord layerTableRecord = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
//            return layerTableRecord?.Name;
//        }

//        /// <summary>
//        /// Возвращает идентификаторы объектов из модели, лежащих на слоях, отличных от указанных.
//        /// </summary>
//        private static ObjectIdCollection GetEntityIdsOnLayersOtherThan(Database database, params string[] excludedLayerNames)
//        {
//            HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            foreach (string name in excludedLayerNames)
//                excluded.Add(name ?? string.Empty);

//            ObjectIdCollection entityIds = new ObjectIdCollection();
//            using (Transaction transaction = database.TransactionManager.StartTransaction())
//            {
//                BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
//                BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
//                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;

//                foreach (ObjectId objectId in modelSpace)
//                {
//                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
//                    if (entity == null) continue;
//                    string entityLayerName = GetLayerName(transaction, layerTable, entity.LayerId);
//                    if (string.IsNullOrEmpty(entityLayerName) || excluded.Contains(entityLayerName))
//                        continue;
//                    entityIds.Add(objectId);
//                }
//                transaction.Commit();
//            }
//            return entityIds;
//        }
//    }
//}
