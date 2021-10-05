Attribute VB_Name = "Modul"
' LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
' Folgenden Seanox Software Solutions oder kurz Seanox genannt.
' Diese Software unterliegt der Version 2 der Apache License.
'
' Startup, Background Program Launcher
' Copyright (C) 2015 Seanox Software Solutions
'
' Licensed under the Apache License, Version 2.0 (the "License"); you may not
' use this file except in compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
' WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
' License for the specific language governing permissions and limitations under
' the License.
'
' Startup 1.1.1 20151015
' Copyright (C) 2015 Seanox Software Solutions.
' Alle Rechte vorbehalten
'
' @author  Seanox Software Solutions.
' @version 1.1.1 20151015
Sub Main()

    On Error Resume Next
    
    If (Dir(App.Path + "\" + App.EXEName + ".cmd") = "") Then
        Call MsgBox(App.EXEName + ".cmd not found.", vbCritical Or vbSystemModal)
    Else
        Call Shell(App.Path + "\" + App.EXEName + ".cmd", vbMinimizedNoFocus)
    End If
End Sub
