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
        public List<RebarDiameterColor> RebarDiameterColors { get; set; } = new List<RebarDiameterColor>();
        public SettingStorage()
        {
            //перенести сюда дефолтные цвета ,если файла с настройками пока что нет
            RebarDiameterColors = new List<RebarDiameterColor>()
            {
                new RebarDiameterColor(6, 255, 0,0 ),
                new RebarDiameterColor(8, 255, 255,0 ),
                new RebarDiameterColor(6, 255, 0,0 ),
                new RebarDiameterColor(6, 255, 0,0 ),

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
            };
        }
    }
}
