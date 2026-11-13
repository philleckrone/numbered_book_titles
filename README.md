# Numbered Book Titles

*This page is the English user guide, [NumberedBookTitles_UserGuide.rtf](plugin/NumberedBookTitles_UserGuide.rtf). The guide is also available in Spanish ([NumberedBookTitles_UserGuide_sp.rtf](plugin/NumberedBookTitles_UserGuide_sp.rtf)) and French ([NumberedBookTitles_UserGuide_fr.rtf](plugin/NumberedBookTitles_UserGuide_fr.rtf)).*

## Purpose

Numbered Book Titles is a Paratext plugin. It checks that the numbered books (1 Samuel to 2 Chronicles, 1 Corinthians to 3 John) write their number the same way, and changes the lines you choose to the usual way. It finds the same differences as the "Numbered books" section of the Report of TOC custom check.

Each of these fields is compared on its own across the numbered books:

- \h, \toc1, \toc2, \toc3 and the main title (\mt, \mt1, \mt2 ... joined in the order they appear).
- The long, short and abbreviation names in BookNames.xml.

For each field the plugin works out how the number is written in each book:

- A number and a space (1 Juan), a number and a non-breaking space, a number with no space (1Juan), a number and a period (1. Juan).
- A number with an ending: 1st, 2nd, 1er, 1ro, 1.º, 1ª, 2e and similar.
- A Roman numeral (I Juan, II Juan), or a number or Roman numeral after the name (Juan 1, Juan II).
- A word (Primera, Segundo, First).

The way most books write it in that field is the usual one, and every book that writes it differently is listed. Different fields may use different ways: \toc1 can use a word ("Primera carta de Juan") while \toc2 uses a number ("1 Juan"). If no way is clearly the most common, every book in that field is listed.

Two ordinal words are not compared with each other: "First" and "Primera" count as the same way, and "Primer libro" beside "Primera carta" is correct grammar, not a difference.

## Installing

1. Download Install_Phils_tools.exe from the Paratext_Plugins folder. If your browser warns about the file, choose to keep it.

2. Close Paratext.

3. Double-click Install_Phils_tools.exe. If Windows says it protected your PC, click "More info", then "Run anyway" (the installer is not signed yet).

4. Tick Numbered Book Titles and click "Install / update ticked". It installs into every Paratext 9 on the computer. If Windows asks for permission, click Yes. The same window updates the plugin later (its status then says "Update available") or removes it ("Remove ticked"), and its "User guide" button opens this guide.

## How to use

1. Open the project in a Scripture window and choose Tools > Numbered Book Titles...

2. The Differences tab lists one row per book and field that differs: the book, the field, the marker line, the text now, how the number is written there, the usual way in that field, and the suggested new text ("Change to"). Nothing is changed yet.

3. Double-click a row, or select it and click "Go to in Paratext", to go to the book's header in Paratext. The window stays open beside Paratext.

4. Check the text in "Change to". Click it to type a different text. When no new text can be worked out safely, the cell says "(type the new text)". Typing a text ticks the row.

5. Tick the rows to change ("Tick all that can change" ticks every row that has a new text), then click "Change ticked lines" and answer Yes.

The "All numbered books" tab shows every numbered book and field in one table, with the usual way at the top and the cells that differ highlighted.

## The suggested text

- When the usual way uses a number, the number is written that way in front of (or after) the rest of the text: "1st Tesalonicenses" and "Primero Pedro" become "1 Tesalonicenses" and "1 Pedro", "II Juan" becomes "2 Juan".
- A word is replaced by a number only when the rest of the text is the book's name as another field of the same book writes it after a number. So "Primera carta a los Corintios" is not turned into "1 carta a los Corintios": type the text yourself.
- When the usual way uses a word, \toc1 takes the \mt title, and a single \mt line takes \toc1. Otherwise the word is taken from the other books with the same number, if they all use the same word.
- For \mt, only the \mt line that holds the number is changed: the first one, or the last one when the number comes after the name.

BookNames.xml names cannot be changed by a plugin. Change them in Paratext: Scripture reference settings..., Book names tab.

## What happens when you change

1. A point is added to the project history ("Numbered Book Titles: before changing ..."). If Paratext cannot record it (for example because the project is in use), nothing is changed and the window says so.

2. Each book that will change is saved whole as Backup_BOOK_date-time.bak in the project's local\NumberedBookTitles folder, which Send/Receive does not share.

3. Only the ticked lines are changed, and only the text after their marker. The header is in chapter 1, so only that chapter is written, and only in books you may edit. Rows in books you may not edit are grey and marked "cannot edit".

4. If the header of a book changed after the scan, that book is left alone and listed: click "Scan again" and try again.

5. The results box lists what was changed in each book, and anything not changed. The list is then scanned again.
