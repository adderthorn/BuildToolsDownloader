Option Strict On

Imports System.Net
Imports System.IO
Imports System.Threading
Imports System.Text
Imports System.ComponentModel
Imports System.Xml
Imports System.Xml.Serialization

Module Program
#Region "Constants"
    Private Const DEFAULT_TIMEOUT As Integer = 20 'In seconds
    Private Const SETTINGS_FILE = "Settings.json"
    Private Const SPIGOT_PATTERN = "spigot*.jar"
    Private Const BUILD_TOOLS_NAME = "BuildTools.jar"
#End Region

#Region "Global Variables"
    Friend Event DownloadProgressChanged As DownloadProgressChangedEventHandler
    Friend Wait As New ManualResetEvent(False)
    Friend PercentagesShown As New List(Of Double)
    Friend MCUpdateInfo As New UpdateInfo()
    Friend BuildLog As String
#End Region

    Function Main(args As String()) As Integer
        Console.WriteLine("Welcome to BuildTools Downloader!")
        Console.WriteLine("Application Version: {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
        'Pull information from the app config file
        Dim MySettings As Settings = Settings.GetSettings(SETTINGS_FILE)
        BuildLog = MySettings.BuildLog
        Dim ForceUpdate As Boolean = False
        If args.Length > 0 Then
            If args(0) = "force-update" Then
                Console.WriteLine("update will be forced.")
                ForceUpdate = True
            ElseIf args(0) = "copy-only" Then
                Console.WriteLine("Skipping download...")
                GoTo Copy
            End If
        End If
Main:
        Console.WriteLine("Checking Build tools API version...")
        Dim BuildInfoReader As XmlReader
        BuildInfoReader = XmlReader.Create(MySettings.BuildInfo.ToString())
        BuildInfoReader.ReadToFollowing("buildNumber")
        Dim CurrentBuild As String = BuildInfoReader.ReadElementContentAsString()
        BuildInfoReader.ReadToFollowing("SHA1")
        Dim CurrentSHA1 As String = BuildInfoReader.ReadElementContentAsString().ToUpper()
        BuildInfoReader.Close()
        Console.WriteLine("Build installed is: {0}, latest build is: {1}", MySettings.CurrentBuild, CurrentBuild)
        If MySettings.CurrentBuild.ToString() = CurrentBuild And Not ForceUpdate Then
            Console.WriteLine("Build is the same...")
            If MySettings.StartMinecraft Then
                GoTo Begin
            Else
                Return 0
            End If
        End If
        If File.Exists(BUILD_TOOLS_NAME) Then
            File.Delete(BUILD_TOOLS_NAME)
            Console.WriteLine("removing current build...")
        End If
        Console.WriteLine("Downloading {0}", BUILD_TOOLS_NAME)
        Dim Fetcher As New WebClient()
        AddHandler Fetcher.DownloadProgressChanged, AddressOf Fetcher_DownloadProgressChanged
        AddHandler Fetcher.DownloadFileCompleted, AddressOf Fetcher_DownloadComplete
        Fetcher.DownloadFileAsync(MySettings.BuildTools, BUILD_TOOLS_NAME)
        Wait.WaitOne()
        Dim FolderName As String = MySettings.BuildFolderFormat
        FolderName = FolderName.Replace("[build]", CurrentBuild).Replace("[date]", DateTime.Now.ToString("yyyyMMdd"))
        FolderName = Path.Combine(MySettings.BuildLocation, FolderName)
        Console.WriteLine("Checking to build in {0}", FolderName)
        If MySettings.RequireUniqueDir Then
            If Directory.Exists(FolderName) Then
                Dim i As Integer = 1
                Dim NewFolderName As String = FolderName + "-" + i.ToString()
                Do Until Not Directory.Exists(NewFolderName)
                    i = i + 1
                    NewFolderName = FolderName + "-" + i.ToString()
                Loop
                Console.WriteLine("Using {0} as new folder path...", NewFolderName)
                FolderName = NewFolderName
            End If
        End If
        Console.WriteLine("Building Folder in: {0}", FolderName)
        If Not Directory.Exists(FolderName) Then Directory.CreateDirectory(FolderName)
        If Not MySettings.CurrentBuildFolder = FolderName Then
            MySettings.CurrentBuildFolder = FolderName
            MySettings.Save(SETTINGS_FILE)
        End If
        Dim NewBuildToolsPath As String = Path.Combine(FolderName, BUILD_TOOLS_NAME) 'String.Format("{0}\{1}", FolderName, BUILD_TOOLS_NAME)
        If File.Exists(NewBuildToolsPath) Then
            File.Delete(NewBuildToolsPath)
        End If
        File.Move(BUILD_TOOLS_NAME, NewBuildToolsPath)
        Console.WriteLine("Executing Build Tools from Bash...")
        Dim BashStartInfo As New ProcessStartInfo() With {
            .CreateNoWindow = False,
            .UseShellExecute = False,
            .FileName = MySettings.Bash,
            .WorkingDirectory = FolderName,
            .WindowStyle = ProcessWindowStyle.Normal
        }
        Dim ExtraArgs As String = MySettings.BuildToolsArguments
        If Not ExtraArgs.StartsWith(CChar(" ")) Then
            ExtraArgs = " " & ExtraArgs
        End If
        BashStartInfo.Arguments = String.Format("--login -i -c ""java -jar """"{0}""""{1}""", NewBuildToolsPath, ExtraArgs)
        Using BashProcess As Process = Process.Start(BashStartInfo)
            BashProcess.WaitForExit()
            Console.WriteLine("Build exited with code: {0}", BashProcess.ExitCode.ToString())
        End Using
        Console.WriteLine("Complete! Setting new build version")
        MySettings.CurrentBuild = CInt(CurrentBuild)
        MySettings.Save(SETTINGS_FILE)
Copy:
        Dim FinalFolderName As String = MySettings.CurrentBuildFolder
        Console.WriteLine("Attemping to copy file to Minecraft directory...")
        Dim MCDirInfo As New DirectoryInfo(FinalFolderName)
        If MCDirInfo.EnumerateFiles(SPIGOT_PATTERN, SearchOption.TopDirectoryOnly).Count < 1 Then
            Console.WriteLine("No Spigot file found in the directory: ""{0}""", MySettings.BuildLocation)
            Console.WriteLine("Now exiting...")
            Return -1
        End If
        Dim SpigotFile As String = MCDirInfo.GetFiles(SPIGOT_PATTERN).OrderByDescending(Function(F) F).FirstOrDefault().Name
        Try
            File.Copy(Path.Combine(FinalFolderName, SpigotFile), MySettings.SpigotLocation, True)
        Catch Ex As Exception
            Console.WriteLine("Attempted to copy file: {0} to {1}", FinalFolderName & SpigotFile.FirstOrDefault(), MySettings.SpigotLocation)
            Console.WriteLine("Cannot copy file: {0}", Ex.Message)
            Console.ReadLine()
            Return Ex.HResult
        End Try

        Console.WriteLine("Outputing XML Information...")
        Dim ThisCurrentBuild = MySettings.CurrentBuild
        MCUpdateInfo.BuildToolsVersion = ThisCurrentBuild.ToString()
        MCUpdateInfo.MinecraftVersion = SpigotFile.Replace("spigot-", "").Replace(".jar", "")
        MCUpdateInfo.StartMinecraft = DateTime.Now
        MCUpdateInfo.Heartbeat = DateTime.Now
        MCUpdateInfo.IsOnline = False
        WriteUpdateInfo()
        Console.WriteLine("Done!")

        If Not MySettings.StartMinecraft Then
            Console.WriteLine("No more work to do, exiting...")
            Return 0
        End If
Begin:
        If MCUpdateInfo.MinecraftVersion Is Nothing Then
            Console.WriteLine("Getting Update Information for heartbeats.")
            MCUpdateInfo = GetUpdateInfo()
        End If
        Dim Beater As New Timers.Timer(60000)
        AddHandler Beater.Elapsed, AddressOf Beater_Tick
        Console.WriteLine("Starting Minecraft...")
        MCUpdateInfo.StartMinecraft = DateTime.Now
        Beater.Start()
        Dim ServerInfo As New ProcessStartInfo()
        Dim SpigotFileInfo As New FileInfo(MySettings.SpigotLocation)
        Dim JavaArgs As String = MySettings.JavaArguments
        ServerInfo.CreateNoWindow = False
        ServerInfo.UseShellExecute = False
        ServerInfo.FileName = "java"
        ServerInfo.WorkingDirectory = SpigotFileInfo.DirectoryName
        ServerInfo.WindowStyle = ProcessWindowStyle.Normal
        ServerInfo.Arguments = String.Format("{0} ""{1}""", JavaArgs, SpigotFileInfo.Name)
        Using SpigotProcess As Process = Process.Start(ServerInfo)
            MCUpdateInfo.IsOnline = True
            WriteUpdateInfo()
            SpigotProcess.WaitForExit()
            Console.WriteLine("Server exited with code: {0}", SpigotProcess.ExitCode.ToString())
        End Using
        Beater.Stop()
        Dim SleepTime As Integer = MySettings.TimeToReload
        If SleepTime <= 0 Then
            SleepTime = DEFAULT_TIMEOUT
        End If
        Console.WriteLine("Server Stopped. Server will restart in {0} second(s), press any key to exit...", SleepTime)
        MCUpdateInfo.IsOnline = False
        WriteUpdateInfo()
        Dim ExitApplication As Boolean = False
        Dim Slept As Integer = 1
        Do While True
            If Console.KeyAvailable Then
                Dim Key As ConsoleKeyInfo = Console.ReadKey(True)
                If Not Key.Key = Nothing Then
                    ExitApplication = True
                    Exit Do
                End If
            End If
            Thread.Sleep(1000) 'One second
            Slept += 1
            If Slept >= SleepTime Then
                Exit Do
            End If
        Loop
        If ExitApplication Then
            Return 0
        End If
        If MySettings.AlwaysCheckForNewBuild Then GoTo Main Else GoTo Begin
    End Function

    Sub Fetcher_DownloadProgressChanged(ByVal sender As Object, ByVal e As DownloadProgressChangedEventArgs)
        Dim Percent As Double = Math.Round((e.BytesReceived / e.TotalBytesToReceive), 2)
        If Percent Mod 0.1 = 0 And Not PercentagesShown.Contains(Percent) Then
            Console.WriteLine("Downloading File... {0}% complete", e.ProgressPercentage.ToString())
            PercentagesShown.Add(Percent)
        End If
    End Sub

    Sub Fetcher_DownloadComplete(ByVal sender As Object, ByVal e As AsyncCompletedEventArgs)
        Console.WriteLine("Done!")
        Wait.Set()
    End Sub

    Private Async Sub Beater_Tick(sender As Object, e As EventArgs)
        Await Task.Run(Sub()
                           WriteUpdateInfo(DateTime.Now)
                       End Sub
                       )
    End Sub

    Sub WriteUpdateInfo(Optional ByVal Heartbeat As DateTime = Nothing)
        If BuildLog Is Nothing Then Return
        If Not Heartbeat = DateTime.MinValue Then
            MCUpdateInfo.Heartbeat = Heartbeat
        End If
        Dim Serial As New XmlSerializer(GetType([UpdateInfo]))
        Dim InfoFile As String = BuildLog
        Dim WriterSettings As New XmlWriterSettings() With {
            .NewLineChars = vbCrLf,
            .NewLineHandling = NewLineHandling.Replace,
            .NewLineOnAttributes = True,
            .WriteEndDocumentOnClose = True,
            .IndentChars = "    ",
            .Indent = True,
            .Encoding = Encoding.UTF8
        }
        Using Writer As New StringWriter()
            Using XMLWriter As XmlWriter = XmlWriter.Create(Writer, WriterSettings)
                Serial.Serialize(XMLWriter, MCUpdateInfo)
                XMLWriter.Flush()
                XMLWriter.Close()
                Using FileWriter As New StreamWriter(InfoFile, False)
                    FileWriter.Write(Writer.ToString())
                    FileWriter.Flush()
                    FileWriter.Close()
                End Using
            End Using
        End Using
    End Sub

    Function GetUpdateInfo() As UpdateInfo
        If BuildLog Is Nothing Then Return New UpdateInfo()
        Dim ThisUpdateInfo As New UpdateInfo()
        Dim InfoFile As String = BuildLog
        If Not File.Exists(InfoFile) Then
            Main(New String() {"force-update"})
            Return New UpdateInfo()
        End If
        Using InfoReader As New StreamReader(InfoFile)
            Dim Serial As New XmlSerializer(GetType([UpdateInfo]))
            ThisUpdateInfo = CType(Serial.Deserialize(InfoReader), UpdateInfo)
            InfoReader.Close()
        End Using
        Return ThisUpdateInfo
    End Function
End Module
