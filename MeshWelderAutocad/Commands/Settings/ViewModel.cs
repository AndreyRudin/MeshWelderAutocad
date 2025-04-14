using System;
using MeshWelderAutocad.WPF;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using System.Windows;
namespace MeshWelderAutocad.Commands.Settings
{
    internal partial class ViewModel : BaseViewModel
    {
        private string _defaultSettingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Autodesk",
                         "Revit",
                         "Addins",
                         "MeshWelderDiameters.cfg");
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

            AddRowDiameterColorCommand = new LambdaCommand(AddRowDiameterColor(),
                _ => true);
            DeleteRowDiameterColorCommand = new LambdaCommand(DeleteRowDiameterColor(),
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
                MessageBox.Show(ex.Message + ex.StackTrace, "Чтение настроек произошло с ошибкой, приняты настройки по умолчанию");
            }
            return new SettingStorage();
        }
    }
}
