using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;

namespace MeshWelderAutocad
{
    public class Command : IExtensionApplication
    {
        [CommandMethod("TestCom")]
        public void TestCom()
        {
            //внедрить отправку данных о запуск - файл отправлять на почту например или просто
            //на какой-то хостинг, где я буду в БД его записывать, время запуска, имя модели, размер модели

            //Панель создать Ribbon
            //Вызов команды из вкладки доступен даже если нету открытого чертежа

            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            var result = openFileDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            string jsonFilePath = openFileDialog.FileName;
            string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
            string jsonFileName = Path.GetFileNameWithoutExtension(jsonFilePath);
            string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
            string dwgDirectory = Path.Combine(jsonDirectory, $"DWG-{timeStamp}_JSON-{jsonFileName}");
            Directory.CreateDirectory(dwgDirectory);

            string jsonContent = File.ReadAllText(jsonFilePath);
            List<Mesh> meshs = JsonConvert.DeserializeObject<List<Mesh>>(jsonContent);
            if (meshs == null)
            {
                MessageBox.Show("Некорректный JSON. Требуется выбрать корректный файл");
                return;
            }

            string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
            string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

            foreach (var mesh in meshs)
            {
                Document newDoc = acadApp.DocumentManager.Add(templatePath);
                Database db = newDoc.Database;

                var path = Path.Combine(dwgDirectory, $"{mesh.DwgName}.dwg");

                using (DocumentLock docLock = newDoc.LockDocument())
                {
                    CreateLayer(db, "MESH");
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

                        foreach (var rebar in mesh.Rebars)
                        {
                            Line line = new Line(
                                new Point3d(rebar.StartPoint.X, rebar.StartPoint.Y, 0), 
                                new Point3d(rebar.EndPoint.X, rebar.EndPoint.Y, 0));
                            line.Color = GetColor(rebar.Diameter);
                            ObjectId layerId = layerTable["MESH"];
                            line.LayerId = layerId;
                            modelSpace.AppendEntity(line);
                            tr.AddNewlyCreatedDBObject(line, true);
                        }

                        LayerTableRecord layerNull = (LayerTableRecord)tr.GetObject(layerTable["0"], OpenMode.ForWrite);
                        layerNull.IsOff = true;
                        layerNull.IsLocked = true;
                        layerNull.IsFrozen = true;
                        tr.Commit();
                    }
                    newDoc.Database.SaveAs(path, true, DwgVersion.Current, null);
                }
                newDoc.CloseAndSave(path);
            }
        }

        private Color GetColor(double diameter)
        {
            switch (diameter)
            {
                case 6.0:
                    return Color.FromRgb(255, 0, 0);
                case 8.0:
                    return Color.FromRgb(255, 255, 0);
                case 10.0:
                    return Color.FromRgb(0, 255, 0);
                case 12.0:
                    return Color.FromRgb(0, 255, 255);
                default:
                    return Color.FromRgb(128, 128, 128);
            }
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
        public void Initialize()
        {

        }

        public void Terminate()
        {

        }
    }
}
