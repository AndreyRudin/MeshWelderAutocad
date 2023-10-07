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
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;

namespace MeshWelderAutocad
{
    public class Mesh
    {
        public string DwgName { get; set; }
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
            //внедрить отправку данных о запуск
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
                MessageBox.Show("Некорректный JSON. Обратитесь к разработчику");
                return;
            }

            string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
            string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

            foreach (var mesh in meshs)
            {
                Document newDoc = Application.DocumentManager.Add(templatePath);
                Database db = newDoc.Database;

                var path = Path.Combine(dwgDirectory, $"{mesh.DwgName}.dwg");

                using (DocumentLock docLock = newDoc.LockDocument())
                {
                    CreateLayer(db, "MESH");
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                        LayerTableRecord layerNull = (LayerTableRecord)tr.GetObject(layerTable["0"], OpenMode.ForWrite);
                        layerNull.IsOff = true;
                        layerNull.IsLocked = true;
                        layerNull.IsFrozen = true;


                        foreach (var rebar in mesh.Rebars)
                        {
                            Line line = new Line(new Point3d(rebar.StartX, rebar.StartY, 0), new Point3d(rebar.EndX, rebar.EndY, 0));
                            line.Color = GetColor(rebar.Diameter);
                            ObjectId layerId = layerTable["MESH"];
                            line.LayerId = layerId;

                            modelSpace.AppendEntity(line);
                            tr.AddNewlyCreatedDBObject(line, true);
                        }
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
