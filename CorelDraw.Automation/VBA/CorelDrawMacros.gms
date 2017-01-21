
Public Sub CloseAllDocuments()
    For Each Doc In Application.Documents
        If Doc.Name = "c#" Then
            Doc.Dirty = False
            Doc.Close
        End If
    Next Doc
    Application.VBE.MainWindow.Visible = False
End Sub

Public Sub CloseActiveDocument()
    If Not Application.ActiveDocument Is Nothing Then
        Application.ActiveDocument.Dirty = False
        Application.ActiveDocument.Close
    End If
    Application.VBE.MainWindow.Visible = False
End Sub