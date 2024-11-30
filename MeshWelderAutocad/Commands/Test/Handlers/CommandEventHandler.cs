using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test.Handlers
{
    public class CommandEventHandler
    {
        private static Dictionary<Document, bool> _documentSubscriptions = new Dictionary<Document, bool>();
        public static void SubscribeToCommandEndedEvent()
        {
            // Получаем активный документ
            Document activeDoc = Application.DocumentManager.MdiActiveDocument;

            // Подписываемся на событие CommandEnded
            activeDoc.CommandEnded += Command.OnCommandEnded;

            //TODO Подписываемся на событие закрытия документа, чтобы отписаться от CommandEnded
            //activeDoc.BeginDocumentClose += OnDocumentClosed;
        }
        //public static void Initialize()
        //{
        //    // Подписываемся на событие изменения активного документа
        //    Application.DocumentManager.MdiActiveDocumentChanged += OnMdiActiveDocumentChanged;
        //}
        //private static void OnMdiActiveDocumentChanged(object sender, DocumentCollectionEventArgs e)
        //{
        //    // Получаем новый активный документ
        //    Document newActiveDoc = Application.DocumentManager.MdiActiveDocument;

        //    if (newActiveDoc != null)
        //    {
        //        // Проверяем, подписаны ли мы уже на событие CommandEnded для этого документа
        //        if (!_documentSubscriptions.ContainsKey(newActiveDoc) || !_documentSubscriptions[newActiveDoc])
        //        {
        //            // Подписываемся на событие CommandEnded для нового документа
        //            newActiveDoc.CommandEnded += OnCommandEnded;

        //            // Отмечаем, что подписка активирована для этого документа
        //            _documentSubscriptions[newActiveDoc] = true;

        //            // Подписываемся на событие закрытия документа, чтобы отписаться от CommandEnded
        //            newActiveDoc.DocumentClosed += OnDocumentClosed;
        //        }
        //    }
        //}
        //private static void OnDocumentClosed(object sender, DocumentCollectionEventArgs e)
        //{
        //    Document closedDoc = e.Document;

        //    // Отписываемся от события CommandEnded при закрытии документа
        //    closedDoc.CommandEnded -= OnCommandEnded;

        //    // Убираем запись о подписке на документ
        //    if (_documentSubscriptions.ContainsKey(closedDoc))
        //    {
        //        _documentSubscriptions[closedDoc] = false;
        //    }
        //}
    }
}
