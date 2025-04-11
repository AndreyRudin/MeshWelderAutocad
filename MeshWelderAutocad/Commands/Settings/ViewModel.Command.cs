using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private Action<object> AddRowReserve()
        {
            return _ =>
            {
                if (SelectedReserve == null)
                {
                    Reserves.Add(new Reserve());
                }
                else
                {
                    int indexSelectedReserve = Reserves.IndexOf(SelectedReserve);
                    Reserves.Insert(indexSelectedReserve + 1, new Reserve());
                }
            };
        }

        private Action<object> DeleteRowReserve()
        {
            return (object parameter) =>
            {
                if (parameter is Reserve reserve)
                    Reserves.Remove(reserve);
            };
        }

        private Action<object> Cancel()
        {
            return _ =>
            {
                View.Close();
            };
        }

        private Action<object> Save()
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
            };
        }
    }
}
