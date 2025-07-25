﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Commands.Settings;
using MeshWelderAutocad.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = MeshWelderAutocad.Commands.Settings.Color;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;

namespace MeshWelderAutocad.Commands.MeshWelder
{
    internal class Command
    {
        private static readonly string _defaultSettingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 "Autodesk",
                 "Revit",
                 "Addins",
                 "MeshWelderDiameters.cfg");
        public static SettingStorage Settings { get; set; }
        public static List<double> MissingDiameter { get; set; }
        [CommandMethod("CreateMesh")]
        public static void CreateMesh()
        {
            //внедрить отправку данных о запуск - файл отправлять на почту например или просто
            //на какой-то хостинг, где я буду в БД его записывать, время запуска, имя модели, размер модели

            //Вызов команды из вкладки доступен даже если нету открытого чертежа
            try
            {
                MissingDiameter = new List<double>();

                Settings = ReadSettings();

                var openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;

                var result = openFileDialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                    return;


                string jsonFilePath = openFileDialog.FileName;
                string jsonContent = File.ReadAllText(jsonFilePath);

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<Mesh>));
                List<Mesh> meshs;
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
                {
                    object objResponse = jsonSerializer.ReadObject(stream);
                    meshs = objResponse as List<Mesh>;
                }

                if (meshs == null)
                {
                    MessageBox.Show("Некорректный JSON. Требуется выбрать корректный файл");
                    return;
                }

                string jsonDirectory = Path.GetDirectoryName(jsonFilePath);
                string timeStamp = DateTime.Now.ToString("dd.MM.yy__HH-mm-ss");
                string generalDwgDirectory = Path.Combine(jsonDirectory, $"{meshs[0].RevitModelName}_DWG-{timeStamp}");
                Directory.CreateDirectory(generalDwgDirectory);

                string templateDirectoryPath = HostApplicationServices.Current.GetEnvironmentVariable("TemplatePath");
                string templatePath = Path.Combine(templateDirectoryPath, "acad.dwt");

                foreach (var mesh in meshs)
                {
                    Document newDoc = acadApp.DocumentManager.Add(templatePath);
                    Database db = newDoc.Database;

                    var directoryDwgForPanel = Path.Combine(generalDwgDirectory, $"{mesh.PanelName}-{mesh.PanelCode}");
                    var path = Path.Combine(directoryDwgForPanel, $"{mesh.DwgName}.dxf");

                    using (DocumentLock docLock = newDoc.LockDocument())
                    {
                        CreateLayer(db, "MESH");
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

                            foreach (var rebar in mesh.Rebars)
                            {
                                Line line = new Line(
                                    new Point3d(rebar.StartPoint.X, rebar.StartPoint.Y, 0),
                                    new Point3d(rebar.EndPoint.X, rebar.EndPoint.Y, 0));
                                line.Color = GetColor(rebar.Diameter);
                                ObjectId layerId = layerTable["MESH"];
                                line.LayerId = layerId;
                                modelSpace.AppendEntity(line);
                                tr.AddNewlyCreatedDBObject(line, true);
                            }

                            ObjectId layerIdActive = layerTable["MESH"];
                            db.Clayer = layerIdActive;

                            tr.Commit();
                        }

                        if (!Directory.Exists(directoryDwgForPanel))
                            Directory.CreateDirectory(directoryDwgForPanel);

                        newDoc.Database.DxfOut(path, 12, DwgVersion.AC1024);
                    }
                    newDoc.CloseAndDiscard();
                }
                File.Delete(jsonFilePath);
                if (MissingDiameter.Count != 0)
                {
                    var validDiameters = MissingDiameter.Distinct().OrderBy(d => d).ToList();
                    string missingDiameter = string.Join(", ", validDiameters);
                    MessageBox.Show($"Данные диаметры: {missingDiameter} не найдены в файле настроек, для них принят цвет по умолчанию", "Warning");
                }
            }
            catch (CustomException e)
            {
                MessageBox.Show(e.ToString(), "Ошибка");
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace, "Системная ошибка");
            }
        }

        private static SettingStorage ReadSettings()
        {
            if (!File.Exists(_defaultSettingPath))
                throw new CustomException("Конфигурационный файл не найден или не создан, создайте его через кнопку настройки");
            try
            {
                string jsonString = File.ReadAllText(_defaultSettingPath);
                SettingStorage settings = JsonConvert.DeserializeObject<SettingStorage>(jsonString);
                return settings;
            }
            catch
            {
                throw new CustomException("Файл настроек не удалось прочитать. Удалите файл и создайте его снова через кнопку настройки");
            }
        }

        public static Autodesk.AutoCAD.Colors.Color GetColor(double diameter)
        {
            RebarDiameterColor rebarDiameterColor = Settings.RebarDiameterColors.FirstOrDefault(rbc => rbc.Diameter == diameter);
            if (rebarDiameterColor == null)
            {
                MissingDiameter.Add(diameter);
                return Autodesk.AutoCAD.Colors.Color.FromRgb(128, 128, 128);
            }
            else
            {
                Color color = rebarDiameterColor.Color;
                return Autodesk.AutoCAD.Colors.Color.FromRgb(color.Red, color.Green, color.Blue);
            }
        }
        
        public static void CreateLayer(Database db, string name)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                using (LayerTableRecord layer = new LayerTableRecord())
                {
                    layer.Name = name;
                    layerTable.UpgradeOpen();
                    ObjectId layerId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                tr.Commit();
                tr.Dispose();
            }
        }
    }
}
