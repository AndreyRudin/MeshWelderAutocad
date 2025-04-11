using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class Command
    {
        public static SettingStorage Settings { get; set; }
        [CommandMethod("ChangeSettingsDNS")]
        public static void ChangeSettingsDNS()
        {
            //считать файл json с настройками
            Settings = ReadSettings();
            //открыть окно с биндингом на эти настройки
            // или .Show()
            //заменить файл с настройками на новые через JSON из свойства Settings
        }

        private static SettingStorage ReadSettings()
        {
            //если файл есть, то принять его, если нет, то создать дефолтные настройки
            return new SettingStorage();
        }
    }
}
