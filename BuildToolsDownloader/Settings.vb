Imports System.IO
Imports Newtonsoft.Json

Public Enum FileOption
    XML
    JSON
End Enum

Public Class Settings
    Public Property BuildInfo As Uri
    Public Property BuildTools As Uri
    Public Property CurrentBuild As Integer
    Public Property Bash As String
    Public Property BuildLocation As String
    Public Property BuildFolderFormat As String
    Public Property RequireUniqueDir As Boolean
    Public Property SpigotLocation As String
    Public Property StartMinecraft As Boolean
    Public Property CurrentBuildFolder As String
    Public Property JavaArguments As String
    Public Property BuildToolsArguments As String
    Public Property AlwaysCheckForNewBuild As Boolean
    Public Property BuildLog As String
    Public Property TimeToReload As Integer
    Public Property BuildFileType As FileOption
    Public Property UseJSONBuildOutput As Boolean

    Public Sub Save(ByVal FilePath As String)
        Dim JsonSettings As String = JsonConvert.SerializeObject(Me, Formatting.Indented)
        Using Writer As New StreamWriter(FilePath)
            Writer.Write(JsonSettings)
            Writer.Flush()
        End Using
    End Sub

    Public Shared Function GetSettings(ByVal FilePath As String) As Settings
        Dim JsonSettings As String
        Using Reader As New StreamReader(FilePath)
            JsonSettings = Reader.ReadToEnd()
        End Using
        Dim NewSettings As Settings = JsonConvert.DeserializeObject(Of Settings)(JsonSettings)
        Return NewSettings
    End Function
End Class
