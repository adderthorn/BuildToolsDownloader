Imports System.IO
Imports Newtonsoft.Json

''' <summary>
''' Serializable class to hold information in the saved XML file
''' </summary>
Public Class Info
    Public Sub New()
    End Sub

    Public Property BuildToolsVersion As String
    Public Property MinecraftVersion As String
    Public Property StartMinecraft As DateTime
    Public Property Heartbeat As DateTime
    Public Property IsOnline As Boolean

    Public Sub Save(ByVal FilePath As String)
        Dim JsonBuildInfo As String = JsonConvert.SerializeObject(Me, Formatting.Indented)
        Using Writer As New StreamWriter(FilePath)
            Writer.Write(JsonBuildInfo)
            Writer.Flush()
        End Using
    End Sub

    Public Shared Function Load(ByVal FilePath As String) As Info
        Dim JsonBuildInfo As String
        If Not File.Exists(FilePath) Then Return New info()
        Using Reader As New StreamReader(FilePath)
            JsonBuildInfo = Reader.ReadToEnd()
        End Using
        Dim NewInfo As Info = JsonConvert.DeserializeObject(Of Info)(JsonBuildInfo)
        Return NewInfo
    End Function
End Class
