using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;
using RibbonButton = Autodesk.Windows.RibbonButton;
using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;

namespace MeshWelderAutocad
{
    public class App : IExtensionApplication
    {
        public const string RibbonTitle = "DNS_Plugins";
        public const string RibbonId = "DNSPluginsId";

        [CommandMethod("CreateMesh")]
        public static void CreateMesh()
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
            string jsonContent = File.ReadAllText(jsonFilePath);

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<Mesh>));
            List<Mesh> meshs;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
            {
                object objResponse = jsonSerializer.ReadObject(stream);
                meshs = objResponse as List<Mesh>;
            }

            if (meshs == null)
            {
                MessageBox.Show("Некорректный JSON. Требуется выбрать корректный файл");
                return;
            }

            string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
            string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
            string generalDwgDirectory = Path.Combine(jsonDirectory, $"{meshs[0].RevitModelName}_DWG-{timeStamp}");
            Directory.CreateDirectory(generalDwgDirectory);

            string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
            string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

            foreach (var mesh in meshs)
            {
                Document newDoc = acadApp.DocumentManager.Add(templatePath);
                Database db = newDoc.Database;

                var directoryDwgForPanel = Path.Combine(generalDwgDirectory, $"{mesh.PanelName}-{mesh.PanelCode}");
                var path = Path.Combine(directoryDwgForPanel, $"{mesh.DwgName}.dxf");

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

                        ObjectId layerIdActive = layerTable["MESH"];
                        db.Clayer = layerIdActive;

                        tr.Commit();
                    }

                    if (!Directory.Exists(directoryDwgForPanel))
                        Directory.CreateDirectory(directoryDwgForPanel);

                    newDoc.Database.DxfOut(path, 12, DwgVersion.Current);
                }
                newDoc.CloseAndDiscard();
            }
            File.Delete(jsonFilePath);
        }

        public static Color GetColor(double diameter)
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
            CreateRibbon();


            //MessageBox.Show("dfds");

            //RibbonControl rc = ComponentManager.Ribbon;
            //var rt = new RibbonTab();
            //rt.Title = "DNS_Plugins";
            //rt.Id = "ID_MESHWELDER";
            //rc.Tabs.Add(rt);

            //RibbonPanelSource rps = new RibbonPanelSource();
            //rps.Title = "MeshWelder";
            //RibbonPanel rp = new RibbonPanel();
            //rp.Source = rps;
            //rt.Panels.Add(rp);

            //var rb = new RibbonButton();
            //rb.Name = "Подготовка чертежей сеток\nдля сеткосварочной машины";
            //rb.ShowText = true;
            //rb.CommandHandler = new RelayCommand((_) => CreateMesh(), (_) => true);
            //rb.Text = "Test Buttonasdad";
            //rps.Items.Add(rb);
        }
        private void CreateRibbon()
        {
            Autodesk.Windows.RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon != null)
            {
                RibbonTab rtab = ribbon.FindTab(RibbonId);
                if (rtab != null)
                {
                    ribbon.Tabs.Remove(rtab);
                }
                
                rtab = new RibbonTab();
                rtab.Title = RibbonTitle;
                rtab.Id = RibbonId;
                ribbon.Tabs.Add(rtab);
                AddContentToTab(rtab);
                rtab.IsActive = true;
            }
        }
        private void AddContentToTab(RibbonTab rtab)
        {
            rtab.Panels.Add(AddPanelOne());
        }
        private static RibbonPanel AddPanelOne()
        {
            var rps = new RibbonPanelSource();
            rps.Title = "MeshWelder";
            RibbonPanel rp = new RibbonPanel();
            rp.Source = rps;
            RibbonButton rci = new RibbonButton();
            rci.Name = "Подготовка чертежей сеток\nдля сеткосварочной машины";
            rps.DialogLauncher = rci;

            var addinAssembly = typeof(App).Assembly;
            RibbonButton btnPythonShell = new RibbonButton
            {
                Orientation = (System.Windows.Controls.Orientation)Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Name = "Addin Manager Manual",
                ShowText = true,
                Text = "Addin Manager \n Manual",
                Description = "Start Write Addin Manager Manual",
                CommandHandler = new RelayCommand((_) => CreateMesh(), (_) => true)
            };
            rps.Items.Add(btnPythonShell);
            return rp;
        }
        //public static ImageSource GetEmbeddedPng(System.Reflection.Assembly app, string imageName)
        //{
        //    var file = app.GetManifestResourceStream(imageName);
        //    var source = PngBitmapDecoder.Create(file, BitmapCreateOptions.None, BitmapCacheOption.None);
        //    return source.Frames[0];
        //}

        public void Terminate()
        {

        }
    }
}
