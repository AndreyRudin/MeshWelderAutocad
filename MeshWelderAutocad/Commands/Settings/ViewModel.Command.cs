using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace MeshWelderAutocad.Commands.Settings
{
    internal partial class ViewModel
    {
        public ICommand AddRowDiameterColorCommand { get; }
        public ICommand DeleteRowDiameterColorCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveCommand { get; }

        private Action<object> AddRowDiameterColor()
        {
            return _ =>
            {
                if (SelectedRebarDiameterColors == null)
                {
                    RebarDiameterColors.Add(new RebarDiameterColor());
                }
                else
                {
                    int indexDiameterColors = RebarDiameterColors.IndexOf(SelectedRebarDiameterColors);
                    RebarDiameterColors.Insert(indexDiameterColors + 1, new RebarDiameterColor());
                }
            };
        }

        private Action<object> DeleteRowDiameterColor()
        {
            return (object parameter) =>
            {
                if (parameter is RebarDiameterColor rebarDiameterColor)
                    RebarDiameterColors.Remove(rebarDiameterColor);
            };
        }

        private Action<object> Cancel()
        {
            return _ =>
            {
                View.Close();
            };
        }
        private bool CanSave(object parameter)
        {
            bool isValidColor = RebarDiameterColors
                .All(r => r.Color.Red <= 255 &&
                     r.Color.Green <= 255 &&
                     r.Color.Blue <= 255);
            bool areDiametersUnique = RebarDiameterColors.Select(r => r.Diameter).Distinct().Count() == RebarDiameterColors.Count;

            return isValidColor && areDiametersUnique;
        }

        private Action<object> SaveSettings()
        {
            return _ =>
            {
                SettingStorage settings = new SettingStorage()
                {
                    RebarDiameterColors = this.RebarDiameterColors.ToList()
                };

                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };

                string jsonString = JsonConvert.SerializeObject(settings, jsonSettings);
                File.WriteAllText(_defaultSettingPath, jsonString);
                View.Close();
            };
        }
    }
}
