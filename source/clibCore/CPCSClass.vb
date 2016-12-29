Imports Contensive.BaseClasses
Imports System.Runtime.InteropServices

Namespace Contensive.Core
    '
    ' comVisible to be activeScript compatible
    '
    <ComVisible(True)> _
    <ComClass(CPCSClass.ClassId, CPCSClass.InterfaceId, CPCSClass.EventsId)> _
    Public Class CPCSClass
        Inherits CPCSBaseClass
        Implements IDisposable
        '
#Region "COM GUIDs"
        Public Const ClassId As String = "63745D9C-795E-4C01-BD6D-4BA35FC4A843"
        Public Const InterfaceId As String = "3F1E7D2E-D697-47A8-A0D3-B625A906BF6A"
        Public Const EventsId As String = "04B8E338-ABB7-44FE-A8DF-2681A36DCA46"
#End Region
        '
        Private cpCore As Contensive.Core.cpCoreClass
        Private CSPointer As Integer
        Private OpeningMemberID As Integer
        Private cp As CPClass
        Protected disposed As Boolean = False
        '
        ' Constructor - Initialize the Main and Csv objects
        '
        Friend Sub New(ByRef cpParent As CPClass)
            MyBase.New()
            cp = cpParent
            cpCore = cp.core
            CSPointer = -1
            OpeningMemberID = cpCore.userId
        End Sub
        '
        ' dispose
        '
        Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposed Then
                Call appendDebugLog(".dispose, dereference cp, main, csv")
                If disposing Then
                    '
                    ' call .dispose for managed objects
                    '
                    Try
                        If CSPointer <> -1 And True Then
                            Call cpCore.db.db_csClose(CSPointer)
                        End If
                        'If Not (False) Then
                        '    Call cmc.asv.csv_CloseCS(CSPointer)
                        'End If
                    Catch ex As Exception
                    End Try
                    cpCore = Nothing
                    cp = Nothing
                End If
                '
                ' Add code here to release the unmanaged resource.
                '
                'csv = Nothing
            End If
            Me.disposed = True
        End Sub
        '
        ' Insert, called only from Processor41.CSInsert after initializing 
        '
        Public Overrides Function Insert(ByVal ContentName As String) As Boolean
            Dim success As Boolean = False
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_csInsertRecord(ContentName, OpeningMemberID)
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.insert")
            End Try
            Return success
        End Function
        '
        '
        '
        Public Overrides Function OpenRecord(ByVal ContentName As String, ByVal recordId As Integer, Optional ByVal SelectFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True) As Boolean
            Dim success As Boolean = False
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_csOpen(ContentName, "id=" & recordId, , ActiveOnly, , , , SelectFieldList, 1, 1)
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenRecord")
            End Try
            Return success
        End Function
        '
        '
        '
        Public Overrides Function Open(ByVal ContentName As String, Optional ByVal SQLCriteria As String = "", Optional ByVal SortFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True, Optional ByVal SelectFieldList As String = "", Optional ByVal pageSize As Integer = 0, Optional ByVal PageNumber As Integer = 1) As Boolean
            'Public Overrides Function Open(ByVal ContentName As String, Optional ByVal SQLCriteria As String = "", Optional ByVal SortFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True, Optional ByVal SelectFieldList As String = "", Optional ByVal ignore As Integer = 10, Optional ByVal PageNumber As Integer = 1, Optional pageSize As Integer = 0) As Boolean
            Dim success As Boolean = False
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_csOpen(ContentName, SQLCriteria, SortFieldList, ActiveOnly, , , , SelectFieldList, pageSize, PageNumber)
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.Open")
            End Try
            Return success
        End Function
        '
        '====================================================================================================
        '
        Public Overrides Function OpenGroupUsers(ByVal GroupList As List(Of String), Optional ByVal SQLCriteria As String = "", Optional ByVal SortFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True, Optional ByVal PageSize As Integer = 10, Optional ByVal PageNumber As Integer = 1) As Boolean
            Dim success As Boolean = False
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_OpenCSGroupUsers(GroupList, SQLCriteria, SortFieldList, ActiveOnly, PageSize, PageNumber)
                success = OK()
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenGroupUsers")
            End Try
            Return success
        End Function
        '
        '====================================================================================================
        '
        Public Overrides Function OpenGroupUsers(ByVal GroupName As String, Optional ByVal SQLCriteria As String = "", Optional ByVal SortFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True, Optional ByVal PageSize As Integer = 10, Optional ByVal PageNumber As Integer = 1) As Boolean
            Dim success As Boolean = False
            '
            Try
                Dim groupList As New List(Of String)
                groupList.Add(GroupName)
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_OpenCSGroupUsers(groupList, SQLCriteria, SortFieldList, ActiveOnly, PageSize, PageNumber)
                success = OK()
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenGroupUsers")
            End Try
            Return success
        End Function
        '
        '====================================================================================================
        '
        Public Overrides Function OpenGroupListUsers(ByVal GroupCommaList As String, Optional ByVal SQLCriteria As String = "", Optional ByVal SortFieldList As String = "", Optional ByVal ActiveOnly As Boolean = True, Optional ByVal PageSize As Integer = 10, Optional ByVal PageNumber As Integer = 1) As Boolean
            Dim groupList As New List(Of String)
            '
            groupList.AddRange(GroupCommaList.Split(","))
            OpenGroupListUsers = OpenGroupUsers(groupList, SQLCriteria, SortFieldList, ActiveOnly, PageSize, PageNumber)
        End Function
        '
        '====================================================================================================
        '
        Public Overrides Function OpenSQL(ByVal sql As String) As Boolean
            Dim success As Boolean = False
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                CSPointer = cpCore.db.db_openCsSql_rev("default", sql)
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenSQL")
            End Try
            Return success
        End Function
        '
        '
        '
        Public Overrides Function OpenSQL(ByVal sql As String, ByVal DataSourcename As String, Optional ByVal PageSize As Integer = 10, Optional ByVal PageNumber As Integer = 1) As Boolean
            Dim success As Boolean = False
            'Dim swap As String
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                If ((sql = "") Or (sql.ToLower = "default")) And (DataSourcename <> "") And (DataSourcename.ToLower <> "default") Then
                    '
                    ' support legacy calls were the arguments were was backwards (datasourcename is sql and vise-versa)
                    '
                    CSPointer = cpCore.db.db_csOpenSql(sql, DataSourcename, PageSize, PageNumber)
                Else
                    CSPointer = cpCore.db.db_csOpenSql(sql, DataSourcename, PageSize, PageNumber)
                End If
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenSQL")
            End Try
            Return success
        End Function
        '
        '
        '
        Public Overrides Function OpenSQL2(ByVal sql As String, Optional ByVal DataSourcename As String = "default", Optional ByVal PageSize As Integer = 10, Optional ByVal PageNumber As Integer = 1) As Boolean
            Dim success As Boolean = False
            'Dim swap As String
            '
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                End If
                If ((sql = "") Or (sql.ToLower = "default")) And (DataSourcename <> "") And (DataSourcename.ToLower <> "default") Then
                    '
                    ' support legacy calls were the arguments were was backwards (datasourcename is sql and vise-versa)
                    '
                    CSPointer = cpCore.db.db_csOpenSql(sql, DataSourcename, PageSize, PageNumber)
                Else
                    CSPointer = cpCore.db.db_csOpenSql(sql, DataSourcename, PageSize, PageNumber)
                End If
                success = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OpenSQL")
            End Try
            Return success
        End Function

        '
        Public Overrides Sub Close()
            Try
                If CSPointer <> -1 Then
                    Call cpCore.db.db_csClose(CSPointer)
                    CSPointer = -1
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.Close")
            End Try
        End Sub

        Public Overrides Function GetFormInput(ByVal ContentName As String, ByVal FieldName As String, Optional ByVal Height As String = "", Optional ByVal Width As String = "", Optional ByVal HtmlId As String = "") As Object
            If True Then
                Return cpCore.html_GetFormInputCS(CSPointer, ContentName, FieldName, Height, Width, HtmlId)
            Else
                Return ""
            End If
        End Function

        Public Overrides Sub Delete()
            Try
                Call cpCore.db.db_DeleteCSRecord(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.Delete")
            End Try
        End Sub

        Public Overrides Function FieldOK(ByVal FieldName As String) As Boolean
            Dim result As Boolean = False
            '
            Try
                result = cpCore.db.db_IsCSFieldSupported(CSPointer, FieldName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.FieldOK")
            End Try
            Return result
        End Function

        Public Overrides Sub GoFirst()
            Try
                Call cpCore.db.db_firstCSRecord(CSPointer, False)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.Delete")
            End Try
        End Sub
        '
        '
        '
        Public Overrides Function GetAddLink(Optional ByVal PresetNameValueList As String = "", Optional ByVal AllowPaste As Boolean = False) As String
            Dim result As Object
            '
            Try
                result = cpCore.main_GetCSRecordAddLink(CSPointer, PresetNameValueList, AllowPaste)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetAddLink")
                result = New String("")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetBoolean(ByVal FieldName As String) As Boolean
            Dim result As Boolean = False
            '
            Try
                result = cpCore.db.db_GetCSBoolean(CSPointer, FieldName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetBoolean")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetDate(ByVal FieldName As String) As Date
            Dim result As Date = #12:00:00 AM#
            '
            Try
                result = cpCore.db.db_GetCSDate(CSPointer, FieldName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetDate")
            End Try
            Return result
        End Function

        Public Overrides Function GetEditLink(Optional ByVal AllowCut As Boolean = False) As String
            Dim result As Object
            Try
                result = cpCore.main_GetCSRecordEditLink(CSPointer, AllowCut)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetEditLink")
                result = New String("")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetFilename(ByVal FieldName As String, Optional ByVal OriginalFilename As String = "", Optional ByVal ContentName As String = "") As String
            Dim result As Object
            '
            result = New String("")
            Try
                result = cpCore.db.db_GetCSFilename(CSPointer, FieldName, OriginalFilename, ContentName)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetFilename")
                result = New String("")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetInteger(ByVal FieldName As String) As Integer
            Dim result As Integer = 0
            '
            Try
                result = cpCore.db.db_GetCSInteger(CSPointer, FieldName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetInteger")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetNumber(ByVal FieldName As String) As Double
            Dim result As Double = 0
            '
            Try
                result = cpCore.db.db_GetCSNumber(CSPointer, FieldName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetNumber")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetRowCount() As Integer
            Dim result As Integer = 0
            '
            Try
                result = cpCore.db.db_GetCSRowCount(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetRowCount")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetSQL() As String
            Dim result As Object
            '
            result = New String("")
            Try
                result = cpCore.db.db_GetCSSource(CSPointer)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetSQL")
                result = New String("")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetText(ByVal FieldName As String) As String
            Dim result As Object
            '
            result = New String("")
            Try
                result = cpCore.db.db_GetCS(CSPointer, FieldName)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetText")
                result = New String("")
            End Try
            Return result
        End Function
        '
        ' Needs to be implemented (refactor to check the field type. if not fieldTypeHtml, encodeHtml)
        '
        Public Overrides Function GetHtml(ByVal FieldName As String) As String
            Dim result As Object
            result = GetText(FieldName)
            Return result
        End Function
        '
        '
        '
        Public Overrides Function GetTextFile(ByVal FieldName As String) As String
            Dim result As Object
            '
            result = New String("")
            Try
                result = cpCore.db.db_csGetTextFile(CSPointer, FieldName)
                If result Is Nothing Then
                    result = New String("")
                End If
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GetTextFile")
                result = New String("")
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        '
        Public Overrides Sub GoNext()
            Try
                Call cpCore.db.db_csGoNext(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.GoNext")
            End Try
        End Sub
        '
        '====================================================================================================
        '
        Public Overrides Function NextOK() As Boolean
            Try
                Call cpCore.db.db_csGoNext(CSPointer)
                Return cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.NextOK")
            End Try
        End Function
        '
        '
        '
        Public Overrides Function OK() As Boolean
            Dim result As Boolean = False
            '
            Try
                result = cpCore.db.db_csOk(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.OK")
            End Try
            Return result
        End Function
        '
        '
        '
        Public Overrides Sub Save()
            Try
                Call cpCore.db.db_SaveCS(CSPointer)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.Save")
            End Try
        End Sub
        '
        '====================================================================================================
        '
        Public Overrides Sub SetField(ByVal FieldName As String, ByVal FieldValue As Object)
            Try
                Call cpCore.db.db_setCS(CSPointer, FieldName, FieldValue)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.SetField")
            End Try
        End Sub
        '
        '====================================================================================================
        '
        Public Overrides Sub SetField(ByVal FieldName As String, ByVal FieldValue As String)
            Try
                Call cpCore.db.db_setCS(CSPointer, FieldName, FieldValue)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.SetField")
            End Try
        End Sub

        Public Overrides Sub SetFile(ByVal FieldName As String, ByVal Copy As String, ByVal ContentName As String)
            Try
                Call cpCore.db.db_SetCSTextFile(CSPointer, FieldName, Copy, ContentName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.SetFile")
            End Try
        End Sub

        Public Overrides Sub SetFormInput(ByVal FieldName As String, Optional ByVal RequestName As String = "")
            Dim success As Boolean = False
            Try
                Call cpCore.main_SetCSFormInput(CSPointer, FieldName, RequestName)
            Catch ex As Exception
                Call cp.core.handleExceptionAndRethrow(ex, "Unexpected error in cs.SetFormInput")
            End Try
        End Sub
        Private Sub appendDebugLog(ByVal copy As String)
            ''My.Computer.FileSystem.WriteAllText("c:\clibCpDebug.log", Now & " - cp.cs, " & copy & vbCrLf, True)
            ' 'My.Computer.FileSystem.WriteAllText(System.AppDocmc.main_CurrentDocmc.main_BaseDirectory() & "cpLog.txt", Now & " - " & copy & vbCrLf, True)
        End Sub
        '
        ' testpoint
        '
        Private Sub tp(ByVal msg As String)
            My.Computer.FileSystem.WriteAllText("c:\clibCpTestPoint.log", Now & " - cp.block, " & msg & vbCrLf, True)
        End Sub
#Region " IDisposable Support "
        ' Do not change or add Overridable to these methods.
        ' Put cleanup code in Dispose(ByVal disposing As Boolean).
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub
#End Region
    End Class

End Namespace

