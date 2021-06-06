Attribute VB_Name = "Modul"
'LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt,
'im Folgenden Seanox Software Solutions oder kurz Seanox genannt.
'Diese Software unterliegt der Version 2 der GNU General Public License.
'
'Startup, Background Program Launcher
'Copyright (C) 2015 Seanox Software Solutions
'
'This program is free software; you can redistribute it and/or modify it
'under the terms of version 2 of the GNU General Public License as published
'by the Free Software Foundation.
'
'This program is distributed in the hope that it will be useful, but WITHOUT
'ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
'FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
'more details.
'
'You should have received a copy of the GNU General Public License along
'with this program; if not, write to the Free Software Foundation, Inc.,
'51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
'
'Dialog stellt die grafische Nutzerschnittstelle, Ressourcen und den Einsprung
'in die Anwendung zur Verfügung.
'
'Startup 1.1.1 20151015
'Copyright (C) 2015 Seanox Software Solutions.
'Alle Rechte vorbehalten
'
'@author  Seanox Software Solutions.
'@version 1.1.1 20151015
Sub Main()

    On Error Resume Next
    
    If (Dir(App.Path + "\" + App.EXEName + ".cmd") = "") Then
        Call MsgBox(App.EXEName + ".cmd not found.", vbCritical Or vbSystemModal)
    Else
        Call Shell(App.Path + "\" + App.EXEName + ".cmd", vbMinimizedNoFocus)
    End If
End Sub
