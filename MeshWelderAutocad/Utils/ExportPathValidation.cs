using System;
using System.Collections.Generic;
using System.IO;

namespace MeshWelderAutocad.Utils
{
    /// <summary>
    /// Ограничения длины путей Windows (как в AllApp.Utils.ExportPathValidation для Revit).
    /// </summary>
    public static class ExportPathValidation
    {
        public const int MaxFullPathLength = 248;
        public const int MaxFileNameLength = 255;

        public static bool TryValidateDxfOutputFilePath(string fullPath, out string errorMessage)
        {
            return TryValidateOutputFilePath(
                fullPath,
                pathTooLong: p => $"Полный путь к итоговому файлу DXF слишком длинный ({p.Length} символов; рекомендуется не более {MaxFullPathLength}). Выберите папку с более коротким путём или сократите имена сборок/панелей и повторите запуск.",
                nameTooLong: n => $"Имя итогового файла DXF слишком длинное ({n.Length} символов; допустимо не более {MaxFileNameLength}). Выберите папку с более коротким путём или сократите имя файла и повторите запуск.",
                out errorMessage);
        }

        public static bool TryValidateTextReportOutputFilePath(string fullPath, out string errorMessage)
        {
            return TryValidateOutputFilePath(
                fullPath,
                pathTooLong: p => $"Полный путь к файлу отчёта слишком длинный ({p.Length} символов; рекомендуется не более {MaxFullPathLength}). Укажите папку вывода с более коротким путём и повторите запуск.",
                nameTooLong: n => $"Имя файла отчёта слишком длинное ({n.Length} символов; допустимо не более {MaxFileNameLength}). Укажите другую папку или сократите имя и повторите запуск.",
                out errorMessage);
        }

        public static bool TryValidateDxfOutputPaths(IEnumerable<string> fullPaths, out string errorMessage)
        {
            foreach (string p in fullPaths)
            {
                if (string.IsNullOrWhiteSpace(p))
                    continue;
                if (!TryValidateDxfOutputFilePath(p, out errorMessage))
                    return false;
            }
            errorMessage = null;
            return true;
        }

        private static bool TryValidateOutputFilePath(
            string fullPath,
            Func<string, string> pathTooLong,
            Func<string, string> nameTooLong,
            out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                errorMessage = "Не указан путь к файлу.";
                return false;
            }
            string normalized;
            try
            {
                normalized = Path.GetFullPath(fullPath);
            }
            catch
            {
                errorMessage = "Указан некорректный путь к файлу.";
                return false;
            }
            if (normalized.Length > MaxFullPathLength)
            {
                errorMessage = pathTooLong(normalized);
                return false;
            }
            string name = Path.GetFileName(normalized);
            if (name.Length > MaxFileNameLength)
            {
                errorMessage = nameTooLong(name);
                return false;
            }
            return true;
        }
    }
}
