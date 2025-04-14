using Autodesk.AutoCAD.Colors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class SettingStorage
    {
        //Зачем здесь создаётся new List<RebarDiameterColor>() и в конструкторе повторяется
        public List<RebarDiameterColor> RebarDiameterColors { get; set; }
        public SettingStorage()
        {
            if (IsDiameterValid)
            {
                RebarDiameterColors = new List<RebarDiameterColor>()
                {
                    new RebarDiameterColor(6, 255, 0, 0),
                    new RebarDiameterColor(8, 255, 255, 0),
                    new RebarDiameterColor(10, 0, 128, 0),
                    new RebarDiameterColor(12, 0, 255, 255),
                    new RebarDiameterColor(28, 0, 255, 0)
                };
            }
            //перенести сюда дефолтные цвета ,если файла с настройками пока что нет
            
        }
        public bool IsDiameterValid(double diameter)
        {
            var validDiameters = new HashSet<double> { 6, 8, 10, 12, 28 };

            if (!validDiameters.Contains(diameter))
            {
                MessageBox.Show($"Обнаружен неизвестный диаметр: {diameter}. Принят цвет по умолчанию RGB(128,128,128)",
                             "Внимание",
                             MessageBoxButton.OK,
                             MessageBoxImage.Warning);

                return false;
            }
            return true;
        }
        //        return Color.FromRgb(, 0, 0);
        //    case 8.0:
        //        return Color.FromRgb(255, 255, 0);
        //    case 10.0:
        //        return Color.FromRgb(0, 128, 0);
        //    case 12.0:
        //        return Color.FromRgb(0, 255, 255);
        //    case 28.0:
        //        return Color.FromRgb(0, 255, 0);
        //    default:
        //        MessageBox.Show($"Обнаружен неизвестный диаметр: {diameter}. Принят цвет по умолчанию RGB(128,128,128)");
        //        return Color.FromRgb(128, 128, 128);
        //}

    }
}
