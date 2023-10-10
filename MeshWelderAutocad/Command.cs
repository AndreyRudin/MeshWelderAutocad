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
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using static Autodesk.AutoCAD.LayerManager.LayerFilter;
using static System.Net.Mime.MediaTypeNames;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;

namespace MeshWelderAutocad
{
    public class Mesh
    {
        public string DwgName { get; set; }
        public string View { get; set; }
        public string Rigth { get; set; }
        public string Normal { get; set; }
        public string Up { get; set; }
        public List<MyRebar> Rebars { get; set; } = new List<MyRebar>();
    }
    public class MyRebar
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Diameter { get; set; }
    }
    public class Command : IExtensionApplication
    {
        [CommandMethod("TestCom")]
        public void TestCom()
        {
            //Панель создать Ribbon
            //внедрить отправку данных о запуск - файл отправлять на почту например или просто на какой-то хостинг, где я буду в БД его записывать, время запуска, имя модели, размер модели
            //+?проверить что нет дублирования в каких панелях?
            //Написать проверку, возможно ли вообще десериализровать json таким образом, если нет, то сообщение и остановка плагина
            //Вызов команды доступен даже если нету открытого чертежа

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
            List<Mesh> meshs;
            try
            {
                meshs = JsonConvert.DeserializeObject<List<Mesh>>(jsonContent);
            }
            catch
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

                        //int heigth = -200;
                        //int x = -200;
                        //var text = new MText();
                        //text.Contents = "View: " + mesh.View;
                        //text.Location = new Point3d(x, heigth, 0);
                        //text.LayerId = layerTable["MESH"];
                        //text.TextHeight = 150;
                        //modelSpace.AppendEntity(text);
                        //tr.AddNewlyCreatedDBObject(text, true);
                        //heigth -= 200;

                        //var text2 = new MText();
                        //text2.Contents = "Rigth: " + mesh.Rigth;
                        //text2.Location = new Point3d(x, heigth, 0);
                        //text2.LayerId = layerTable["MESH"];
                        //text2.TextHeight = 150;
                        //modelSpace.AppendEntity(text2);
                        //tr.AddNewlyCreatedDBObject(text2, true);
                        //heigth -= 200;

                        //var text3 = new MText();
                        //text3.Contents = "Up: " + mesh.Up;
                        //text3.Location = new Point3d(x, heigth, 0);
                        //text3.LayerId = layerTable["MESH"];
                        //text3.TextHeight = 150;
                        //modelSpace.AppendEntity(text3);
                        //tr.AddNewlyCreatedDBObject(text3, true);
                        //heigth -= 200;

                        //var text5 = new MText();
                        //text5.Contents = "Normal: " + mesh.Normal;
                        //text5.Location = new Point3d(x, heigth, 0);
                        //text5.LayerId = layerTable["MESH"];
                        //text5.TextHeight = 150;
                        //modelSpace.AppendEntity(text5);
                        //tr.AddNewlyCreatedDBObject(text5, true);
                        //heigth -= 200;

                        //var text6 = new MText();
                        //text6.Contents = "Name view: " + mesh.DwgName;
                        //text6.Location = new Point3d(x, heigth, 0);
                        //text6.LayerId = layerTable["MESH"];
                        //text6.TextHeight = 150;
                        //modelSpace.AppendEntity(text6);
                        //tr.AddNewlyCreatedDBObject(text6, true);
                        //heigth -= 200;

                        foreach (var rebar in mesh.Rebars)
                        {
                            Line line = new Line(new Point3d(rebar.StartX, rebar.StartY, 0), new Point3d(rebar.EndX, rebar.EndY, 0));
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
