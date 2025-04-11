using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.GraphicsSystem;
using MeshWelderAutocad.WPF;
using MeshWelderAutocad.Commands.Settings.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
namespace MeshWelderAutocad.Commands.Settings
{
    internal partial class ViewModel : BaseViewModel
    {
        private string _defaultSettingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Autodesk",
                         "Revit",
                         "Addins",
                         "SummaryScheduleSetting.cfg");
        public Model Model { get; set; }
        public View View { get; set; }

        private ObservableCollection<Reserve> _reserves = new ObservableCollection<Reserve>();
        public ObservableCollection<Reserve> Reserves
        {
            get => _reserves;
            set
            {
                if (_reserves != value)
                {
                    _reserves = value;
                    OnPropertyChanged(nameof(Reserves));
                }
            }
        }

        public ViewModel()
        {
            Model = new Model();
            AddRowReserveCommand = new LambdaCommand(AddRowReserve(),
                _ => true);
            DeleteRowReserveCommand = new LambdaCommand(DeleteRowReserve(),
                _ => Reserves.Count > 1);
            CancelCommand = new LambdaCommand(Cancel(),
                _ => true);
            SaveCommand = new LambdaCommand(Save(),
                _ => Reserves?.Count > 0);
            View = new View() { DataContext = this };
        }
        private void SaveSettings()
        {
            SettingStorage settings = new SettingStorage()
            {
                Presets = Presets.ToList(),
                Reserves = Reserves.ToList(),
            };

            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            string jsonString = JsonConvert.SerializeObject(settings, jsonSettings);
            File.WriteAllText(_defaultSettingPath, jsonString);
        }
    }
}
