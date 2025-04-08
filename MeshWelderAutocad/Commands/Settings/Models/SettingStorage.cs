using Autodesk.AutoCAD.Colors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class SettingStorage
    {
        public ObservableCollection<RebarDiameterColor> RebarDiameterColors { get; set; } = new ObservableCollection<RebarDiameterColor>();
        public SettingStorage()
        {
            //перенести сюда дефолтные цвета ,если файла с настройками пока что нет
            RebarDiameterColors = new ObservableCollection<RebarDiameterColor>()
            {

            };
        }
    }
}
