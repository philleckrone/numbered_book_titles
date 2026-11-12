using System;

namespace NumberedBookTitles
{
    /// <summary>
    /// The window's text in English, Spanish or French, picked from Paratext's interface language.
    /// Keep this file ASCII: run tools\asciify_cs.py after typing accented letters.
    /// </summary>
    public sealed class Strings
    {
        /// <summary>The tool's version: shown in the window title and reported to Paratext.</summary>
        public const string ToolVersion = "1.1";

        public string MenuText;
        public string Title;
        public string Description;
        public string ProjectLabel;
        public string Intro;
        public string Scanning;
        public string Summary;
        public string NoNumberedBooks;
        public string NoProblems;
        public string BookNamesMissing;
        public string TabIssues;
        public string TabAll;
        public string ColFix;
        public string ColBook;
        public string ColField;
        public string ColLine;
        public string ColNow;
        public string ColStyle;
        public string ColUsual;
        public string ColChangeTo;
        public string ColPermission;
        public string CannotEdit;
        public string UsualCount;
        public string NoUsual;
        public string NoUsualShort;
        public string UsualRow;
        public string Missing;
        public string BookNamesFix;
        public string NoSuggestion;
        public string AllHint;
        public string GoToButton;
        public string SelectAll;
        public string ClearAll;
        public string FixButton;
        public string Rescan;
        public string Close;
        public string Counts;
        public string NothingSelected;
        public string ConfirmTitle;
        public string Confirm;
        public string HistoryMessage;
        public string ResultsLabel;
        public string ResFixed;
        public string ResTotal;
        public string ResNothing;
        public string ResNoPermission;
        public string ResBusy;
        public string ResFailed;
        public string ResChanged;
        public string ResBackups;
        public string NotSaved;
        public string SomeNotEditable;
        public string OpenProjectFirst;
        public string Error;
        public string HistoryNotRecorded;
        public string HistoryNoFolder;
        public string HistoryStopped;

        /// <summary>One name per TitleField.</summary>
        public string[] FieldNames;
        /// <summary>One name per NumberStyle.</summary>
        public string[] StyleNames;

        public static Strings For(string locale)
        {
            string lang = string.IsNullOrEmpty(locale) ? "" : locale.ToLowerInvariant();
            if (lang.StartsWith("es", StringComparison.Ordinal)) return Spanish();
            if (lang.StartsWith("fr", StringComparison.Ordinal)) return French();
            return English();
        }

        public string FieldName(TitleField f)
        {
            return FieldNames[(int)f];
        }

        public string StyleName(NumberStyle s)
        {
            return StyleNames[(int)s];
        }

        public static Strings English()
        {
            var s = new Strings();
            s.MenuText = "Numbered Book Titles...";
            s.Title = "Numbered Book Titles";
            s.Description = "Checks that the numbered books (1 Samuel to 3 John) write their number the same way in \\h, \\toc1, \\toc2, \\toc3, \\mt and the book names (1 John, 1st John, I John, First John, ...), and changes the lines you tick to the usual style.";
            s.ProjectLabel = "Project: {0}";
            s.Intro = "Each field is compared on its own across the numbered books (1SA-2CH, 1CO-3JN): the way most books write the number there is the usual one, " +
                "and the books listed below write it differently. Check or type the text in \"Change to\", tick the rows to change, then click Change. " +
                "Double-click a row (or select it and click Go to in Paratext) to go to the book's header in Paratext.";
            s.Scanning = "Reading {0}...";
            s.Summary = "{0} numbered book(s), {1} inconsistency(ies) found.";
            s.NoNumberedBooks = "This project has fewer than two numbered books, so there is nothing to compare.";
            s.NoProblems = "Every field writes the number the same way in every numbered book.";
            s.BookNamesMissing = "BookNames.xml was not found, so the book names are not included.";
            s.TabIssues = "Differences";
            s.TabAll = "All numbered books";
            s.ColFix = "Change";
            s.ColBook = "Book";
            s.ColField = "Field";
            s.ColLine = "Line";
            s.ColNow = "Now";
            s.ColStyle = "How the number is written";
            s.ColUsual = "Usual in this field";
            s.ColChangeTo = "Change to";
            s.ColPermission = "Permission";
            s.CannotEdit = "cannot edit";
            s.UsualCount = "{0} ({1} of {2} books)";
            s.NoUsual = "No usual style: {0}";
            s.NoUsualShort = "(no usual style)";
            s.UsualRow = "Usual";
            s.Missing = "missing";
            s.BookNamesFix = "(change in Paratext: Scripture reference settings..., Book names tab)";
            s.NoSuggestion = "(type the new text)";
            s.AllHint = "Highlighted cells write the number differently from the usual style in their column. Double-click a book to go to its header.";
            s.GoToButton = "Go to in Paratext";
            s.SelectAll = "Tick all that can change";
            s.ClearAll = "Untick all";
            s.FixButton = "Change ticked lines ({0})";
            s.Rescan = "Scan again";
            s.Close = "Close";
            s.Counts = "{0} difference(s), {1} can be changed here, {2} ticked.";
            s.NothingSelected = "Tick at least one line to change.";
            s.ConfirmTitle = "Change numbered book titles";
            s.Confirm = "Change {0} line(s) in {1} book(s) you can edit?\n\n" +
                "A point is first added to the project history, and each changed book is saved as a .bak file in:\n{2}";
            s.HistoryMessage = "Numbered Book Titles: before changing {0} header line(s)";
            s.ResultsLabel = "Results: after you click Change, the changes made (and anything not changed) are listed here.";
            s.ResFixed = "{0}: {1} line(s) changed";
            s.ResTotal = "Changed: {0} line(s) in {1} book(s).";
            s.ResNothing = "Nothing was changed.";
            s.ResNoPermission = "Not changed, because you do not have permission to edit: {0}";
            s.ResBusy = "Not changed, because the text is in use (finish editing it, then try again): {0}";
            s.ResFailed = "Could not change: {0}";
            s.ResChanged = "Not changed, because the text changed after the scan (scan again, then try again): {0}";
            s.ResBackups = "Backups of the changed books are in: {0}";
            s.NotSaved = "Paratext did not save the change as written";
            s.SomeNotEditable = "Some books are ones you cannot edit. Their rows cannot be ticked.";
            s.OpenProjectFirst = "Open a project in a Scripture window first, then choose Numbered Book Titles...";
            s.Error = "Numbered Book Titles could not finish:\n{0}";
            s.HistoryNotRecorded = "Paratext did not record the history point (the project may be in use)";
            s.HistoryNoFolder = "the project folder or its history (.hg) was not found";
            s.HistoryStopped = "Could not add a point to the project history, so nothing was changed: {0}\n\nFinish or close any editing in this project, then try again.";
            s.FieldNames = new[]
            {
                "\\h", "\\toc1", "\\toc2", "\\toc3", "\\mt",
                "BookNames.xml (long)", "BookNames.xml (short)", "BookNames.xml (abbr)",
            };
            s.StyleNames = new[]
            {
                "Number and space (1 John)",
                "Number and non-breaking space",
                "Number with no space (1John)",
                "Number and period (1. John)",
                "Number with an ending (1st, 1.\u00BA, 1\u00AA)",
                "Roman numeral (I John)",
                "Number after the name (John 1)",
                "Roman numeral after the name (John I)",
                "Word (First)",
            };
            return s;
        }

        public static Strings Spanish()
        {
            var s = new Strings();
            s.MenuText = "T\u00EDtulos de libros numerados...";
            s.Title = "T\u00EDtulos de libros numerados";
            s.Description = "Revisa que los libros numerados (1 Samuel a 3 Juan) escriban su n\u00FAmero de la misma forma en \\h, \\toc1, \\toc2, \\toc3, \\mt y los nombres de libros (1 Juan, 1.\u00BA Juan, I Juan, Primera de Juan, ...), y cambia las l\u00EDneas que usted marque al estilo habitual.";
            s.ProjectLabel = "Proyecto: {0}";
            s.Intro = "Cada campo se compara por separado entre los libros numerados (1SA-2CH, 1CO-3JN): la forma en que la mayor\u00EDa de los libros escribe el n\u00FAmero all\u00ED es la habitual, " +
                "y los libros de la lista la escriben de otra forma. Revise o escriba el texto en \"Cambiar a\", marque las filas que desea cambiar y haga clic en Cambiar. " +
                "Haga doble clic en una fila (o selecci\u00F3nela y haga clic en Ir en Paratext) para ir al encabezado del libro en Paratext.";
            s.Scanning = "Leyendo {0}...";
            s.Summary = "{0} libro(s) numerado(s), {1} incoherencia(s) encontrada(s).";
            s.NoNumberedBooks = "Este proyecto tiene menos de dos libros numerados, as\u00ED que no hay nada que comparar.";
            s.NoProblems = "Cada campo escribe el n\u00FAmero de la misma forma en todos los libros numerados.";
            s.BookNamesMissing = "No se encontr\u00F3 BookNames.xml, as\u00ED que los nombres de libros no se incluyen.";
            s.TabIssues = "Diferencias";
            s.TabAll = "Todos los libros numerados";
            s.ColFix = "Cambiar";
            s.ColBook = "Libro";
            s.ColField = "Campo";
            s.ColLine = "L\u00EDnea";
            s.ColNow = "Ahora";
            s.ColStyle = "C\u00F3mo se escribe el n\u00FAmero";
            s.ColUsual = "Lo habitual en este campo";
            s.ColChangeTo = "Cambiar a";
            s.ColPermission = "Permiso";
            s.CannotEdit = "no puede editar";
            s.UsualCount = "{0} ({1} de {2} libros)";
            s.NoUsual = "Sin estilo habitual: {0}";
            s.NoUsualShort = "(sin estilo habitual)";
            s.UsualRow = "Habitual";
            s.Missing = "ausente";
            s.BookNamesFix = "(c\u00E1mbielo en Paratext: Configuraci\u00F3n de referencias b\u00EDblicas..., pesta\u00F1a Nombres de libros)";
            s.NoSuggestion = "(escriba el texto nuevo)";
            s.AllHint = "Las celdas resaltadas escriben el n\u00FAmero de forma distinta al estilo habitual de su columna. Haga doble clic en un libro para ir a su encabezado.";
            s.GoToButton = "Ir en Paratext";
            s.SelectAll = "Marcar todas las que se pueden cambiar";
            s.ClearAll = "Desmarcar todas";
            s.FixButton = "Cambiar las l\u00EDneas marcadas ({0})";
            s.Rescan = "Revisar de nuevo";
            s.Close = "Cerrar";
            s.Counts = "{0} diferencia(s), {1} se pueden cambiar aqu\u00ED, {2} marcada(s).";
            s.NothingSelected = "Marque al menos una l\u00EDnea para cambiar.";
            s.ConfirmTitle = "Cambiar t\u00EDtulos de libros numerados";
            s.Confirm = "\u00BFCambiar {0} l\u00EDnea(s) en {1} libro(s) que usted puede editar?\n\n" +
                "Primero se agrega un punto al historial del proyecto, y cada libro cambiado se guarda como archivo .bak en:\n{2}";
            s.HistoryMessage = "T\u00EDtulos de libros numerados: antes de cambiar {0} l\u00EDnea(s) de encabezado";
            s.ResultsLabel = "Resultados: despu\u00E9s de hacer clic en Cambiar, aqu\u00ED se muestran los cambios hechos (y lo que no se cambi\u00F3).";
            s.ResFixed = "{0}: {1} l\u00EDnea(s) cambiada(s)";
            s.ResTotal = "Cambiado: {0} l\u00EDnea(s) en {1} libro(s).";
            s.ResNothing = "No se cambi\u00F3 nada.";
            s.ResNoPermission = "No se cambiaron, porque usted no tiene permiso para editar: {0}";
            s.ResBusy = "No se cambiaron, porque el texto est\u00E1 en uso (termine de editarlo y vuelva a intentarlo): {0}";
            s.ResFailed = "No se pudo cambiar: {0}";
            s.ResChanged = "No se cambiaron, porque el texto cambi\u00F3 despu\u00E9s de la revisi\u00F3n (revise de nuevo y vuelva a intentarlo): {0}";
            s.ResBackups = "Las copias de seguridad de los libros cambiados est\u00E1n en: {0}";
            s.NotSaved = "Paratext no guard\u00F3 el cambio tal como se escribi\u00F3";
            s.SomeNotEditable = "Algunos libros son libros que usted no puede editar. Sus filas no se pueden marcar.";
            s.OpenProjectFirst = "Abra primero un proyecto en una ventana de Escritura y luego elija T\u00EDtulos de libros numerados...";
            s.Error = "T\u00EDtulos de libros numerados no pudo terminar:\n{0}";
            s.HistoryNotRecorded = "Paratext no registr\u00F3 el punto del historial (puede que el proyecto est\u00E9 en uso)";
            s.HistoryNoFolder = "no se encontr\u00F3 la carpeta del proyecto o su historial (.hg)";
            s.HistoryStopped = "No se pudo agregar un punto al historial del proyecto, as\u00ED que no se cambi\u00F3 nada: {0}\n\nTermine o cierre cualquier edici\u00F3n en este proyecto y vuelva a intentarlo.";
            s.FieldNames = new[]
            {
                "\\h", "\\toc1", "\\toc2", "\\toc3", "\\mt",
                "BookNames.xml (nombre largo)", "BookNames.xml (nombre corto)", "BookNames.xml (abreviatura)",
            };
            s.StyleNames = new[]
            {
                "N\u00FAmero y espacio (1 Juan)",
                "N\u00FAmero y espacio de no separaci\u00F3n",
                "N\u00FAmero sin espacio (1Juan)",
                "N\u00FAmero y punto (1. Juan)",
                "N\u00FAmero con terminaci\u00F3n (1.\u00BA, 1\u00AA, 1st)",
                "N\u00FAmero romano (I Juan)",
                "N\u00FAmero despu\u00E9s del nombre (Juan 1)",
                "N\u00FAmero romano despu\u00E9s del nombre (Juan I)",
                "Palabra (Primera)",
            };
            return s;
        }

        /// <summary>French. A no-break space (\u00A0) goes before : ? and inside the guillemets.</summary>
        public static Strings French()
        {
            var s = new Strings();
            s.MenuText = "Titres des livres num\u00E9rot\u00E9s...";
            s.Title = "Titres des livres num\u00E9rot\u00E9s";
            s.Description = "V\u00E9rifie que les livres num\u00E9rot\u00E9s (1 Samuel \u00E0 3 Jean) \u00E9crivent leur num\u00E9ro de la m\u00EAme fa\u00E7on dans \\h, \\toc1, \\toc2, \\toc3, \\mt et les noms des livres (1 Jean, 1re Jean, I Jean, Premi\u00E8re \u00E9p\u00EEtre de Jean, ...), et met les lignes que vous cochez dans le style habituel.";
            s.ProjectLabel = "Projet\u00A0: {0}";
            s.Intro = "Chaque champ est compar\u00E9 s\u00E9par\u00E9ment entre les livres num\u00E9rot\u00E9s (1SA-2CH, 1CO-3JN)\u00A0: la fa\u00E7on dont la plupart des livres y \u00E9crivent le num\u00E9ro est la fa\u00E7on habituelle, " +
                "et les livres de la liste l'\u00E9crivent autrement. V\u00E9rifiez ou tapez le texte dans \u00AB\u00A0Remplacer par\u00A0\u00BB, cochez les lignes \u00E0 modifier, puis cliquez sur Modifier. " +
                "Double-cliquez sur une ligne (ou s\u00E9lectionnez-la et cliquez sur Aller dans Paratext) pour aller \u00E0 l'en-t\u00EAte du livre dans Paratext.";
            s.Scanning = "Lecture de {0}...";
            s.Summary = "{0} livre(s) num\u00E9rot\u00E9(s), {1} incoh\u00E9rence(s) trouv\u00E9e(s).";
            s.NoNumberedBooks = "Ce projet a moins de deux livres num\u00E9rot\u00E9s\u00A0: il n'y a donc rien \u00E0 comparer.";
            s.NoProblems = "Chaque champ \u00E9crit le num\u00E9ro de la m\u00EAme fa\u00E7on dans tous les livres num\u00E9rot\u00E9s.";
            s.BookNamesMissing = "BookNames.xml est introuvable\u00A0: les noms des livres ne sont donc pas inclus.";
            s.TabIssues = "Diff\u00E9rences";
            s.TabAll = "Tous les livres num\u00E9rot\u00E9s";
            s.ColFix = "Modifier";
            s.ColBook = "Livre";
            s.ColField = "Champ";
            s.ColLine = "Ligne";
            s.ColNow = "Actuellement";
            s.ColStyle = "Fa\u00E7on d'\u00E9crire le num\u00E9ro";
            s.ColUsual = "Habituel dans ce champ";
            s.ColChangeTo = "Remplacer par";
            s.ColPermission = "Permission";
            s.CannotEdit = "lecture seule";
            s.UsualCount = "{0} ({1} livres sur {2})";
            s.NoUsual = "Aucun style habituel\u00A0: {0}";
            s.NoUsualShort = "(aucun style habituel)";
            s.UsualRow = "Habituel";
            s.Missing = "absent";
            s.BookNamesFix = "(\u00E0 modifier dans Paratext\u00A0: Param\u00E8tres des r\u00E9f\u00E9rences bibliques..., onglet Noms des livres)";
            s.NoSuggestion = "(tapez le nouveau texte)";
            s.AllHint = "Les cellules en surbrillance \u00E9crivent le num\u00E9ro autrement que le style habituel de leur colonne. Double-cliquez sur un livre pour aller \u00E0 son en-t\u00EAte.";
            s.GoToButton = "Aller dans Paratext";
            s.SelectAll = "Cocher toutes celles qui peuvent changer";
            s.ClearAll = "Tout d\u00E9cocher";
            s.FixButton = "Modifier les lignes coch\u00E9es ({0})";
            s.Rescan = "Analyser \u00E0 nouveau";
            s.Close = "Fermer";
            s.Counts = "{0} diff\u00E9rence(s), {1} modifiable(s) ici, {2} coch\u00E9e(s).";
            s.NothingSelected = "Cochez au moins une ligne \u00E0 modifier.";
            s.ConfirmTitle = "Modifier les titres des livres num\u00E9rot\u00E9s";
            s.Confirm = "Modifier {0} ligne(s) dans {1} livre(s) que vous pouvez modifier\u00A0?\n\n" +
                "Un point est d'abord ajout\u00E9 \u00E0 l'historique du projet, et chaque livre modifi\u00E9 est enregistr\u00E9 dans un fichier .bak dans\u00A0:\n{2}";
            s.HistoryMessage = "Titres des livres num\u00E9rot\u00E9s\u00A0: avant de modifier {0} ligne(s) d'en-t\u00EAte";
            s.ResultsLabel = "R\u00E9sultats\u00A0: apr\u00E8s un clic sur Modifier, les modifications faites (et ce qui n'a pas \u00E9t\u00E9 modifi\u00E9) s'affichent ici.";
            s.ResFixed = "{0}\u00A0: {1} ligne(s) modifi\u00E9e(s)";
            s.ResTotal = "Modifi\u00E9\u00A0: {0} ligne(s) dans {1} livre(s).";
            s.ResNothing = "Rien n'a \u00E9t\u00E9 modifi\u00E9.";
            s.ResNoPermission = "Non modifi\u00E9(s), car vous n'avez pas la permission de modifier\u00A0: {0}";
            s.ResBusy = "Non modifi\u00E9(s), car le texte est en cours d'utilisation (terminez sa modification, puis r\u00E9essayez)\u00A0: {0}";
            s.ResFailed = "Modification impossible\u00A0: {0}";
            s.ResChanged = "Non modifi\u00E9(s), car le texte a chang\u00E9 apr\u00E8s l'analyse (analysez \u00E0 nouveau, puis r\u00E9essayez)\u00A0: {0}";
            s.ResBackups = "Les copies de sauvegarde des livres modifi\u00E9s se trouvent dans\u00A0: {0}";
            s.NotSaved = "Paratext n'a pas enregistr\u00E9 la modification telle qu'elle \u00E9tait \u00E9crite";
            s.SomeNotEditable = "Vous ne pouvez pas modifier certains livres. Leurs lignes ne peuvent pas \u00EAtre coch\u00E9es.";
            s.OpenProjectFirst = "Ouvrez d'abord un projet dans une fen\u00EAtre de texte biblique, puis choisissez Titres des livres num\u00E9rot\u00E9s...";
            s.Error = "Titres des livres num\u00E9rot\u00E9s n'a pas pu terminer\u00A0:\n{0}";
            s.HistoryNotRecorded = "Paratext n'a pas enregistr\u00E9 le point d'historique (le projet est peut-\u00EAtre en cours d'utilisation)";
            s.HistoryNoFolder = "le dossier du projet ou son historique (.hg) est introuvable";
            s.HistoryStopped = "Impossible d'ajouter un point \u00E0 l'historique du projet, donc rien n'a \u00E9t\u00E9 modifi\u00E9\u00A0: {0}\n\nTerminez ou fermez toute modification en cours dans ce projet, puis r\u00E9essayez.";
            s.FieldNames = new[]
            {
                "\\h", "\\toc1", "\\toc2", "\\toc3", "\\mt",
                "BookNames.xml (nom long)", "BookNames.xml (nom court)", "BookNames.xml (abr\u00E9viation)",
            };
            s.StyleNames = new[]
            {
                "Num\u00E9ro et espace (1 Jean)",
                "Num\u00E9ro et espace ins\u00E9cable",
                "Num\u00E9ro sans espace (1Jean)",
                "Num\u00E9ro et point (1. Jean)",
                "Num\u00E9ro avec suffixe (1er, 1re, 2e)",
                "Chiffre romain (I Jean)",
                "Num\u00E9ro apr\u00E8s le nom (Jean 1)",
                "Chiffre romain apr\u00E8s le nom (Jean I)",
                "Mot (Premi\u00E8re)",
            };
            return s;
        }
    }
}
