using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.GraphicsSystem;
using MeshWelderAutocad.WPF;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using System.ServiceModel.Channels;
namespace MeshWelderAutocad.Commands.Settings
{
    internal partial class ViewModel : BaseViewModel
    {
        private string _defaultSettingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Autodesk",
                         "Revit",
                         "Addins",
                         "MesgWelderDiameters.cfg");
        public View View { get; set; }

        private ObservableCollection<RebarDiameterColor> _rebarDiameterColors = new ObservableCollection<RebarDiameterColor>();
        public ObservableCollection<RebarDiameterColor> RebarDiameterColors
        {
            get => _rebarDiameterColors;
            set
            {
                _rebarDiameterColors = value;
                OnPropertyChanged(nameof(RebarDiameterColors));
            }
        }
        private RebarDiameterColor _selectedRebarDiameterColors;
        public RebarDiameterColor SelectedRebarDiameterColors
        {
            get => _selectedRebarDiameterColors;
            set
            {
                _selectedRebarDiameterColors = value;
                OnPropertyChanged(nameof(SelectedRebarDiameterColors));
            }
        }

        public ViewModel()
        {
            RebarDiameterColors = new ObservableCollection<RebarDiameterColor>(ReadSettings().RebarDiameterColors);

            AddRowDiameterColorCommand = new LambdaCommand(AddRowReserve(),
                _ => true);
            DeleteRowDiameterColorCommand = new LambdaCommand(DeleteRowReserve(),
                _ => RebarDiameterColors.Count > 1);
            CancelCommand = new LambdaCommand(Cancel(),
                _ => true);
            SaveCommand = new LambdaCommand(Save(),
                _ => true);

            View = new View() { DataContext = this };
        }
        private SettingStorage ReadSettings()
        {
            if (!File.Exists(_defaultSettingPath))
                return new SettingStorage();

            try
            {
                string jsonString = File.ReadAllText(_defaultSettingPath);
                SettingStorage settings = JsonConvert.DeserializeObject<SettingStorage>(jsonString);
                return settings;
            }
            catch (Exception ex)
            {
                //чтение настроек произошло с ошибкой, приняты настройки по умолчанию
                MessageBox
            }
            return new SettingStorage();
        }
    }
}
