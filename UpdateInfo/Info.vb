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
End Class
