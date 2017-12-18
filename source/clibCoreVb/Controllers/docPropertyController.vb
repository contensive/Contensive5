﻿
Option Explicit On
Option Strict On

Imports Contensive.Core.Controllers
Imports Contensive.Core.Controllers.genericController

Namespace Contensive.Core.Controllers
    '
    '====================================================================================================
    ''' <summary>
    ''' doc properties are properties limited in scope to this single hit, or viewing
    ''' </summary>
    Public Class docPropertyController
        '
        Private cpCore As coreClass
        '
        Private docPropertiesDict As New Dictionary(Of String, docPropertiesClass)
        '
        Public Sub New(cpCore As coreClass)
            MyBase.New()
            Me.cpCore = cpCore
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As Integer)
            Call setProperty(key, CStr(value))
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As Date)
            Call setProperty(key, value.ToString)
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As Boolean)
            Call setProperty(key, value.ToString())
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As String)
            setProperty(key, value, False)
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As String, isForm As Boolean)
            Try
                Dim prop As New docPropertiesClass
                prop.NameValue = key
                prop.FileSize = 0
                prop.fileType = ""
                prop.IsFile = False
                prop.IsForm = isForm
                prop.Name = key
                prop.NameValue = key & "=" & value
                prop.Value = value
                setProperty(key, prop)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '====================================================================================================
        '
        Public Sub setProperty(key As String, value As docPropertiesClass)
            Dim propKey As String = encodeDocPropertyKey(key)
            If Not String.IsNullOrEmpty(propKey) Then
                If docPropertiesDict.ContainsKey(propKey) Then
                    docPropertiesDict.Remove(propKey)
                End If
                docPropertiesDict.Add(propKey, value)
            End If
        End Sub
        '
        '====================================================================================================
        '
        Public Function containsKey(ByVal RequestName As String) As Boolean
            Return docPropertiesDict.ContainsKey(encodeDocPropertyKey(RequestName))
        End Function
        '
        '====================================================================================================
        '
        Public Function getKeyList() As List(Of String)
            Dim keyList As New List(Of String)
            For Each kvp As KeyValuePair(Of String, docPropertiesClass) In docPropertiesDict
                keyList.Add(kvp.Key)
            Next
            Return keyList
        End Function
        '
        '=============================================================================================
        '
        Public Function getNumber(ByVal RequestName As String) As Double
            Try
                Return genericController.EncodeNumber(getProperty(RequestName).Value)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return 0
        End Function
        '
        '=============================================================================================
        '
        Public Function getInteger(ByVal RequestName As String) As Integer
            Try
                Return genericController.EncodeInteger(getProperty(RequestName).Value)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return 0
        End Function
        '
        '=============================================================================================
        '
        Public Function getText(ByVal RequestName As String) As String
            Try
                Return genericController.encodeText(getProperty(RequestName).Value)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return String.Empty
        End Function
        '
        '=============================================================================================
        '
        Public Function getRenderedActiveContent(ByVal RequestName As String) As String
            Try
                Return cpCore.html.convertEditorResponseToActiveContent(genericController.encodeText(getProperty(RequestName).Value))
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return String.Empty
        End Function
        '
        '=============================================================================================
        '
        Public Function getBoolean(ByVal RequestName As String) As Boolean
            Try
                Return genericController.EncodeBoolean(getProperty(RequestName).Value)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return False
        End Function
        '
        '=============================================================================================
        '
        Public Function getDate(ByVal RequestName As String) As Date
            Try
                Return genericController.EncodeDate(getProperty(RequestName).Value)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return Date.MinValue
        End Function
        '
        '====================================================================================================
        '
        Public Function getProperty(ByVal RequestName As String) As docPropertiesClass
            Try
                Dim Key As String
                '
                Key = encodeDocPropertyKey(RequestName)
                If Not String.IsNullOrEmpty(Key) Then
                    If docPropertiesDict.ContainsKey(Key) Then
                        Return docPropertiesDict(Key)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return New docPropertiesClass
        End Function
        '
        '====================================================================================================
        '
        Private Function encodeDocPropertyKey(sourceKey As String) As String
            Dim returnResult As String = ""
            Try
                If Not String.IsNullOrEmpty(sourceKey) Then
                    returnResult = sourceKey.ToLower()
                    If cpCore.webServer.requestSpaceAsUnderscore Then
                        returnResult = genericController.vbReplace(returnResult, " ", "_")
                    End If
                    If cpCore.webServer.requestDotAsUnderscore Then
                        returnResult = genericController.vbReplace(returnResult, ".", "_")
                    End If
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return returnResult
        End Function
        '
        '
        '
        '==========================================================================================
        ''' <summary>
        ''' add querystring to the doc properties
        ''' </summary>
        ''' <param name="QS"></param>
        Public Sub addQueryString(QS As String)
            Try
                '
                Dim ampSplit() As String
                Dim ampSplitCount As Integer
                Dim ValuePair() As String
                Dim key As String
                Dim Ptr As Integer
                '
                ampSplit = Split(QS, "&")
                ampSplitCount = UBound(ampSplit) + 1
                For Ptr = 0 To ampSplitCount - 1
                    Dim nameValuePair As String = ampSplit(Ptr)
                    Dim docProperty As New docPropertiesClass
                    With docProperty
                        If Not String.IsNullOrEmpty(nameValuePair) Then
                            If genericController.vbInstr(1, nameValuePair, "=") <> 0 Then
                                ValuePair = Split(nameValuePair, "=")
                                key = DecodeResponseVariable(CStr(ValuePair(0)))
                                If key <> "" Then
                                    .Name = key
                                    If UBound(ValuePair) > 0 Then
                                        .Value = DecodeResponseVariable(CStr(ValuePair(1)))
                                    End If
                                    .IsForm = False
                                    .IsFile = False
                                    'cpCore.webServer.readStreamJSForm = cpCore.webServer.readStreamJSForm Or (UCase(.Name) = genericController.vbUCase(RequestNameJSForm))
                                    cpCore.docProperties.setProperty(key, docProperty)
                                End If
                            End If
                        End If
                    End With
                Next
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' return the docProperties collection as the legacy optionString
        ''' </summary>
        ''' <returns></returns>
        Public Function getLegacyOptionStringFromVar() As String
            Dim returnString As String = ""
            Try
                For Each key As String In getKeyList()
                    With getProperty(key)
                        returnString &= "" & "&" & genericController.encodeLegacyOptionStringArgument(key) & "=" & encodeLegacyOptionStringArgument(.Value)
                    End With
                Next
            Catch ex As Exception
                Throw (ex)
            End Try
            Return returnString
        End Function
    End Class


End Namespace