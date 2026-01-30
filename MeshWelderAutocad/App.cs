using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using MeshWelderAutocad.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonButton = Autodesk.Windows.RibbonButton;
using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MeshWelderAutocad
{
    public class App : IExtensionApplication
    {
        public const string RibbonTitle = "DNS_DSK";
        public const string RibbonId = "DNSPluginsId";

        [CommandMethod("InitMeshWelder", CommandFlags.Transparent)]
        public void Init()
        {
            CreateRibbon();
        }
        private void CreateRibbon()
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
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
            rtab.Panels.Add(AddPanelEVG());
            rtab.Panels.Add(AddPanelLaser());
            rtab.Panels.Add(AddInfoPanel());
        }
        private static RibbonPanel AddPanelEVG()
        {
            var rps = new RibbonPanelSource();
            rps.Title = "    EVG    ";
            RibbonPanel rp = new RibbonPanel();
            rp.Source = rps;

            var addinAssembly = typeof(App).Assembly;

            RibbonButton btnSettings = new RibbonButton
            {
                Orientation = Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Text = "Настройки\nEVG",
                ShowText = true,
                ToolTip = "подсказка пока не создана, обратитесь к BIM менеджеру",
                LargeImage = GetImageSourceByBitMapFromResource(Resource.LogoSettings),
                CommandHandler = new RelayCommand((_) => Commands.Settings.Command.ChangeSettingsDNS(), (_) => true)
            };

            RibbonButton btnMeshWelder = new RibbonButton
            {
                Orientation = Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Text = "Сетки\nдля EVG",
                ShowText = true,
                ToolTip = "Плагин необходим для приведения DWG-файла с чертежом арматурной сетки, полученного путем экспорта из Revit или выполненного в AutoCAD, в соответствие с требованиями импорта в специализированное ПО \"Meshbuilder\".\r\n\r\nРабота выполняется в двух вариантах:" +
                    "\n   - Исходные данные приходят из Revit;" +
                    "\n   - Исходные данные приходят из AutoCAD." +
                    "\n\n1. Если армирование выполнялось в Revit, необходимо выполнить экспорт вида в DWG. Для этого на вкладке \"DNS_Plugins\" на панели \"MeshWelder\" выбрать кнопку \"MeshExport\". В открывшемся меню выбрать виды, которые необходимо выгрузить в DWG и указать путь сохранения файлов. Выгрузка происходит в формате \"1 вид = 1 файл DWG\". Экспорт в DWG выполняется на создаваемый слой \"MESH\" (без кавычек). Экспортируемые элементы переносятся в DWG категорией \"отрезок\". В зависимости от диаметра арматурных стержней в Revit (параметр \"ADSK_Наименование\"), линиям присваиваются цвета. Затем у сетки определяется габарит, нижний левый угол которого переносится в абсолютные координаты 0.0. Если в файле есть слои, кроме \"MESH\", то они удаляются, а неудаляемые - скрываются, замораживаются и блокируются." +
                    "\n\n2. Если армирование выполнялось в AutoCAD, то на этапе разработки КЖ.И стержни должны выполняться категорией \"полилинии\", иметь замкнутый контур, углы между отрезками контура 90°, строиться на слое \"MESH\" (без кавычек), а также иметь цвета, соответствующие их диаметру. Находясь в окне AutoCAD, указать папку, из которой будут обрабатываться файлы формата DWG, а также папку, куда будут сохранены результаты преобразования. Обработка - пакетная. В каждом файле проводится проверка полилиний. Если элементы категории \"полилиния\" имеют замкнутый контур, то внутри него строится продольная средняя линия категорией \"отрезок\", а полилиния удаляется. Если элементы категории \"полилиния\" не имеют замкнутого контура, то они преобразуются в элементы категории \"отрезок\". В каждом файле у сетки определяется габарит, нижний левый угол которого переносится в абсолютные координаты 0.0. Если в файле есть слои, кроме \"MESH\", то они удаляются, а неудаляемые - скрываются, замораживаются и блокируются.",
                Image = GetImageSourceByBitMapFromResource(Resource.EVG_16x16),
                LargeImage = GetImageSourceByBitMapFromResource(Resource.EVG_32x32),
                CommandHandler = new RelayCommand((_) => Commands.MeshWelder.Command.CreateMesh(), (_) => true)
            };
            
            rps.Items.Add(btnSettings);
            rps.Items.Add(btnMeshWelder);

            return rp;
        }
        private static RibbonPanel AddPanelLaser()
        {
            var rps = new RibbonPanelSource();
            rps.Title = "ЛАЗЕРНЫЙ ПРОЕКТОР";
            RibbonPanel rp = new RibbonPanel();
            rp.Source = rps;

            RibbonButton btnLaser = new RibbonButton
            {
                Orientation = Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Text = "Разметка\nдля проектора",
                ShowText = true,
                ToolTip = "подсказка пока не создана, обратитесь к BIM менеджеру",
                Image = GetImageSourceByBitMapFromResource(Resource.Laser_16x16),
                LargeImage = GetImageSourceByBitMapFromResource(Resource.Laser_32x32),
                CommandHandler = new RelayCommand((_) => Commands.Laser.Command.CreateDrawingsForLaser(), (_) => true)
            };

            rps.Items.Add(btnLaser);

            RibbonButton btnLaserEOM = new RibbonButton
            {
                Orientation = Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Text = "Электроразметка\nдля проектора",
                ShowText = true,
                ToolTip = "подсказка пока не создана, обратитесь к BIM менеджеру",
                Image = GetImageSourceByBitMapFromResource(Resource.Laser_16x16),
                LargeImage = GetImageSourceByBitMapFromResource(Resource.Laser_32x32),
                CommandHandler = new RelayCommand((_) => Commands.LaserEOM.Command.CreateDrawingsForLaserEOM(), (_) => true)
            };

            rps.Items.Add(btnLaserEOM);

            return rp;
        }
        private static RibbonPanel AddInfoPanel()
        {
            string versionDate = "v26.01.31";
            var rps = new RibbonPanelSource();
            rps.Title = "    INFO    ";
            RibbonPanel rp = new RibbonPanel();
            rp.Source = rps;

            RibbonButton btnDevelopers = new RibbonButton
            {
                Orientation = Orientation.Vertical,
                AllowInStatusBar = true,
                Size = RibbonItemSize.Large,
                Text = versionDate,
                ShowText = true,
                ToolTip = "Команда разработки и обратная связь",
                LargeImage = GetImageSourceByBitMapFromResource(Resource.dev32),
                CommandHandler = new RelayCommand((_) => Commands.Developers.Command.ShowDevelopers(), (_) => true)
            };

            rps.Items.Add(btnDevelopers);

            return rp;
        }
        private static ImageSource GetImageSourceByBitMapFromResource(Bitmap source)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                source.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }

        public void Initialize()
        {
            
        }

        public void Terminate()
        {

        }
    }
}
