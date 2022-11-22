﻿#nullable disable warnings
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using TSW2LM;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;

namespace TSW2_Livery_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string VERSION = "1.0.0";

        //COUNT OF LIVERIES
        readonly byte[] COL = new byte[] { 0x53, 0x74, 0x72, 0x75, 0x63, 0x74, 0x50, 0x72, 0x6f, 0x70, 0x65, 0x72, 0x74, 0x79, 0, 0 };
        //START OF LIVERY
        readonly byte[] SOL = new byte[] { 0, 3, 0, 0, 0, 0x49, 0x44 };
        //END OF LIVERIES
        readonly byte[] EOL = new byte[] { 0, 5, 0, 0, 0, 0x4e, 0x6f, 0x6e, 0x65, 0, 0, 0, 0, 0 };
        // START OF NAME
        readonly byte[] SON = new byte[] { 0x44, 0x69, 0x73, 0x70, 0x6c, 0x61, 0x79, 0x4e, 0x61, 0x6d, 0x65, 0, 0xd, 0, 0, 0, 0x54, 0x65, 0x78, 0x74, 0x50, 0x72, 0x6f, 0x70, 0x65, 0x72, 0x74, 0x79, 0, 0x13, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0xff, 1, 0, 0, 0, 6, 0, 0, 0 };
        //BYTE 29 AND 47 INCREMENT
        readonly int[] SONs = new int[] { 29, 47 };
        //END OF NAME
        readonly byte[] EON = new byte[] { 0, 15 };
        //START OF MODEL
        readonly byte[] SOM = new byte[] { 0, 0, 0, 0x2f };
        //END OF MODEL
        readonly byte[] EOM = new byte[] { 0 };

        private readonly Config Cfg = new Config();

        bool jImportWarning = false;
        bool jExportWarning = false;

        char[] illegalFileNameCharacters = { '/', '\\', ':', '*', '?', '"', '<', '>', '|'};

        //0 - header data
        //1-MAX_GAME_LIVERIES - livery data
        //MAX_GAME_LIVERIES+1 - footer data
        private readonly Dictionary<int, byte[]> SplitFile = new Dictionary<int, byte[]>();

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        [DllImport("Kernel32.dll")]
        public static extern bool FreeConsole();

        public MainWindow()
        {

            AttachConsole(-1);

            Cfg.Load();

            Log.AddLogFile("TSW2LM.log", Log.LogLevel.INFO);
            if (Environment.GetCommandLineArgs().Contains("-debug"))
            {
                Log.AddLogFile("TSW2LM_debug.log", Log.LogLevel.DEBUG);
                Log.ConsoleLevel = Log.LogLevel.DEBUG;
            }
            Log.AddLogMessage($"Command line: {Environment.GetCommandLineArgs()}", "MW::<init>", Log.LogLevel.DEBUG);

            string[] args = Environment.GetCommandLineArgs();
            Cfg.SkipAutosave = true;
            for (int i = 1; i < args.Length; i++)
            {
                try
                {
                    switch (args[i])
                    {
                        case "-maxGameLiveries":
                            if (!int.TryParse(args[i + 1], out int count)) PrintHelp();
                            Cfg.MaxGameLiveries = (count > 30 && count < 256) ? count : 30;
                            break;
                        case "-noUpdate":
                            if (!(args[i + 1] == "true" || args[i + 1] == "false")) PrintHelp();
                            Cfg.NoUpdate = args[i + 1] == "true";
                            break;
                        case "-devUpdates":
                            if (!(args[i + 1] == "true" || args[i + 1] == "false")) PrintHelp();
                            Cfg.DevUpdates = args[i + 1] == "true";
                            break;
                        case "-reset":
                            Cfg.ApplyDefaults();
                            break;
                        case "-help":
                        case "-?":
                            PrintHelp();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.AddLogMessage($"Failed to parse command line argument '{args[i]}': {e.Message}", "MW::<init>", Log.LogLevel.WARNING);
                }
            }
            Cfg.SkipAutosave = false;

            new UpdateNotifier().ShowDialog();

            /*if (!Cfg.NoUpdate)
            {
                try
                {
                    Log.AddLogMessage("Checking for updates...", "MW::<init>");
                    WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW2-Livery-Manager/deploy/version.dat");
                    string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
                    Log.AddLogMessage($"Got version information: {VERSION}->{UpdateResponse}", "MW::<init>");
                    string[] NewVersion = UpdateResponse.Split('.');
                    string[] CurrentVersion = VERSION.Split('.');
                    char CurrentSuffix = ' ';
                    if (!int.TryParse(CurrentVersion[^1], out int _))
                    {
                        CurrentSuffix = VERSION.Last();
                        CurrentVersion[^1] = CurrentVersion[^1].Split(CurrentSuffix)[0];
                    }
                    int update = 0;
                    bool fullVersionUpdate = true;
                    for (int i = 0; i < NewVersion.Length; i++)
                    {
                        if (int.Parse(NewVersion[i]) < int.Parse(CurrentVersion[i]))
                        {
                            update -= (int)Math.Pow(10, 2 - i);
                        }
                        if (int.Parse(NewVersion[i]) > int.Parse(CurrentVersion[i]))
                        {
                            update += (int)Math.Pow(10, 2 - i);
                        }
                        if (int.Parse(NewVersion[i]) != int.Parse(CurrentVersion[i])) fullVersionUpdate = false;
                    }
                    if (update > 0 || (fullVersionUpdate && CurrentSuffix != ' ')) new UpdateNotifier(VERSION, UpdateResponse, $"https://github.com/RagingLightning/TSW2-Livery-Manager/releases/tag/v{UpdateResponse}").ShowDialog();
                }
                catch (WebException e)
                {
                    Log.AddLogMessage($"Unable to check for updates: {e.Message}", "MW::<init>", Log.LogLevel.DEBUG);
                }

            }

            if (Cfg.DevUpdates)
            {
                Log.AddLogMessage("Checking for dev updates...", "MW::<init>");
                WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW2-Livery-Manager/deploy/devversion.dat");
                string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
                Log.AddLogMessage($"Got version information: {VERSION}->{UpdateResponse}", "MW::<init>");
                string[] NewVersion = UpdateResponse.Split('.');
                string[] CurrentVersion = VERSION.Split('.');
                char NewSuffix = ' ';
                char CurrentSuffix = ' ';
                if (!int.TryParse(NewVersion[^1], out int _))
                {
                    NewSuffix = UpdateResponse.Last();
                    NewVersion[^1] = NewVersion[^1].Split(NewSuffix)[0];
                }
                if (!int.TryParse(CurrentVersion[^1], out int _))
                {
                    CurrentSuffix = VERSION.Last();
                    CurrentVersion[^1] = CurrentVersion[^1].Split(CurrentSuffix)[0];
                }
                bool update = false;
                bool devUpdate = NewSuffix != ' ' && (NewSuffix > CurrentSuffix || CurrentSuffix == ' ');
                for (int i = 0; i < NewVersion.Length; i++)
                {
                    if (NewSuffix == ' ' && int.Parse(NewVersion[i]) > int.Parse(CurrentVersion[i]))
                    {
                        update = true;
                    }
                    if (int.Parse(NewVersion[i]) < int.Parse(CurrentVersion[i])) devUpdate = false;
                }
                if (update) new UpdateNotifier(VERSION, UpdateResponse, $"https://github.com/RagingLightning/TSW2-Livery-Manager/releases/tag/v{UpdateResponse}").ShowDialog();
                else if (devUpdate) new UpdateNotifier(VERSION, UpdateResponse, $"https://github.com/RagingLightning/TSW2-Livery-Manager/releases/tag/dev-v{UpdateResponse}").ShowDialog();
            }*/

            InitializeComponent();
            DataContext = new Data();

            if (Cfg.GamePath != "")
            {
                Log.AddLogMessage("Loading GamePath Data...", "MW::<init>");
                if (File.Exists(Cfg.GamePath))
                {
                    txtGameDir.Text = Cfg.GamePath;
                    string GameStatus = LoadGameLiveries();
                    if (GameStatus != "OK") lblMessage.Content = $"ERROR WHILE LOADING GAME LIVERIES:\n{GameStatus}";
                }
                else
                {
                    lblMessage.Content = $"ERROR WHILE LOADING GAME LIVERIES, please ensure you:\n - have created at least one livery in the game\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                }
            }
            if (Cfg.LibraryPath != "")
            {
                txtLibDir.Text = Cfg.LibraryPath;
                string LibraryStatus = UpdateLibraryLiveries();
                if (LibraryStatus != "OK") lblMessage.Content = $"ERROR WHILE LOADING LIBRARY LIVERIES:\n{LibraryStatus}";
            }

        }

        private void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  Train Sim World 2 Livery Manager                  ║");
            Console.WriteLine("╟──────────────────────── by RagingLightning ────────────────────────╢");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Command Line Arguments:");
            Console.WriteLine(" -help / -? :");
            Console.WriteLine("    Show this help page");
            Console.WriteLine();
            Console.WriteLine(" -reset :");
            Console.WriteLine("    Resets all config options back to default");
            Console.WriteLine();
            Console.WriteLine(" -noUpdate <true|false> :");
            Console.WriteLine("    Toggle automatic update check at startup");
            Console.WriteLine();
            Console.WriteLine(" -devUpdate <true|false> :");
            Console.WriteLine("    Toggle automatic dev-update check on startup");
            Console.WriteLine();
            Console.WriteLine(" -maxGameLiveries <count> :");
            Console.WriteLine("    Change the number of in-game liveries !!EXPERIMENTAL!!");
            Console.WriteLine("    any number less than 30 reverts back to the default setting of 30");
            Console.WriteLine();
            FreeConsole();
            Application.Current.Shutdown();
        }

        private int LocateInByteArray(byte[] hay, byte[] needle)
        {
            return LocateInByteArray(hay, needle, new int[] { }, 0, hay.Length);
        }

        private int LocateInByteArray(byte[] hay, byte[] needle, int[] skip)
        {
            return LocateInByteArray(hay, needle, skip, 0, hay.Length);
        }

        private int LocateInByteArray(byte[] hay, byte[] needle, int start)
        {
            return LocateInByteArray(hay, needle, new int[] { }, start, hay.Length);
        }

        private int LocateInByteArray(byte[] hay, byte[] needle, int start, int end)
        {
            return LocateInByteArray(hay, needle, new int[] { }, start, end);
        }

        /// <summary>searches a byte array for a given sequence of bytes</summary>
        /// <param name="hay">The hay stack to be searched</param>
        /// <param name="needle">The needle</param>
        /// <param name="skip">The indices of bytes that should be ignored</param>
        /// <param name="start">The index in the hay stack where search begins</param>
        /// <param name="end">The index in the hay stack where searching ends</param>
        /// <returns>the starting index of the first occurrence found</returns>
        private int LocateInByteArray(byte[] hay, byte[] needle, int[] skip, int start, int end)
        {
            if (hay == null || needle == null || hay.Length == 0 || needle.Length == 0 || needle.Length > hay.Length) return -1;

            for (int i = start; i < end; i++)
            {
                if (hay[i] == needle[0])
                {
                    if (needle.Length == 1) return i;
                    if (i + needle.Length > end) return -1;
                    for (int j = 1; j < needle.Length; j++)
                    {
                        if (hay[i + j] != needle[j] && !skip.Contains(j)) break;
                        if (j == needle.Length - 1) return i;
                    }
                }
            }
            return -1;
        }

        private int LocatesInByteArray(byte[] hay, byte[][] needles)
        {
            return LocatesInByteArray(hay, needles, 0, hay.Length);
        }

        private int LocatesInByteArray(byte[] hay, byte[][] needles, int start)
        {
            return LocatesInByteArray(hay, needles, start, hay.Length);
        }

        private int LocatesInByteArray(byte[] hay, byte[][] needles, int start, int end)
        {
            int FindStart = -1;
            for (int j = 0; j < needles.Length; j++)
            {
                int LocalFind = LocateInByteArray(hay, needles[j], start, end);
                FindStart = ((LocalFind != -1 && LocalFind < FindStart) || FindStart == -1) ? LocalFind : FindStart;
            }
            return FindStart;
        }

        private string LoadGameLiveries()
        {
            if (Cfg.GamePath == string.Empty)
            {
                return "Configuration error - Please make sure, you selected a valid game data folder.";
            }
            SplitFile.Clear();
            try
            {
                Log.AddLogMessage("Loading game livery file...", "MW::LoadGameLiveries");
                Log.AddLogMessage($"File Path: {Cfg.GamePath}", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);
                byte[] LiveryFile = File.ReadAllBytes(Cfg.GamePath);

                int HeaderEnd = LocateInByteArray(LiveryFile, SOL);
                if (HeaderEnd < 0)

                {
                    Log.AddLogMessage("No livery found - at least one livery needs to already exist.", "MW::LoadGameLiveries", Log.LogLevel.ERROR);
                    ((Data)DataContext).Useable = false;
                    return "Empty livery file - please ensure you've created at least one livery.";
                }

                byte[] Header = new byte[HeaderEnd];
                Array.Copy(LiveryFile, Header, Header.Length);
                SplitFile.Add(0, Header);
                Log.AddLogMessage($"Extracted game livery header (bytes 0 - {HeaderEnd})", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);

                Log.AddLogMessage("Extracting game liveries...", "MW::LoadGameLiveries");
                int LiveryEnd = 0;

                for (int i = 1; i <= Cfg.MaxGameLiveries; i++)
                {
                    int LiveryStart = LocateInByteArray(LiveryFile, SOL, LiveryEnd);
                    if (LiveryStart == -1) break;
                    LiveryEnd = LocatesInByteArray(LiveryFile, new byte[][] { SOL, EOL }, LiveryStart + 1);
                    if (LiveryEnd == -1)
                    {
                        Log.AddLogMessage($"Non-Ending livery {i} starting at byte {LiveryStart}, aborting file parsing", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                        ((Data)DataContext).Useable = false;
                        return $"!NoEndLivery! {i}/{LiveryStart} - your game's livery file appears to be corrupted\n - restore it from a previously created backup\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                    }
                    byte[] LiveryData = new byte[LiveryEnd - LiveryStart];
                    Array.Copy(LiveryFile, LiveryStart, LiveryData, 0, LiveryData.Length);
                    SplitFile.Add(i, LiveryData);
                    Log.AddLogMessage($"Extracted livery {i} (bytes {LiveryStart} - {LiveryEnd})", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);
                }
                Log.AddLogMessage("All game liveries extracted successfully", "MW::LoadGameLiveries");

                byte[] Footer = new byte[LiveryFile.Length - LiveryEnd];
                Array.Copy(LiveryFile, LiveryEnd, Footer, 0, Footer.Length);
                SplitFile.Add(Cfg.MaxGameLiveries + 1, Footer);
                Log.AddLogMessage($"Extracted game livery footer (bytes {LiveryEnd} - {LiveryFile.Length - 1})", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);

                ((Data)DataContext).Useable = true;

                return UpdateLocalGameLiveries();
            }
            catch (FileNotFoundException e)
            {
                Cfg.GamePath = "";
                Log.AddLogMessage($"FileNotFoundException: {e.FileName}", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                ((Data)DataContext).Useable = false;
                return $"!FileNotFound! game livery file - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }
            catch (IOException e)
            {
                Cfg.GamePath = "";
                Log.AddLogMessage($"IOException: {e.Message}", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                ((Data)DataContext).Useable = false;
                return $"!IOException! game livery file - Make sure:\n -  you selected the appropriate folder\n - have created at least one livery in the game\n\nif you need help, consult the wiki at https://github.com/RagingLightning/TSW2-Livery-Manager/wiki/(1)-Getting-Started \n or @RagingLightning on discord or creare an issue on github";
            }
        }

        private string UpdateLocalGameLiveries()
        {
            Log.AddLogMessage("Updating local game liveries...", "MW::UpdateLocalGameLiveries");
            lstGameLiveries.Items.Clear();
            for (int i = 1; i <= Cfg.MaxGameLiveries; i++)
            {
                SplitFile.TryGetValue(i, out byte[] LiveryData);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!GameLiveryData! livery {i} - your game's livery file appears to be corrupted\n - restore the game livery file from a previous backup\n - create an issue on github and/or @RagingLightning on discord";

                lstGameLiveries.Items.Add($"({i}): {Display}");
                Log.AddLogMessage($"Added game livery {i} ({Display})", "MW::UpdateLocalGameLiveries", Log.LogLevel.DEBUG);
            }
            return "OK";
        }

        private string UpdateLibraryLiveries()
        {
            Log.AddLogMessage("Updating library liveries...", "MW::UpdateLibraryLiveries");
            lstLibraryLiveries.Items.Clear();
            DirectoryInfo Info = new DirectoryInfo(Cfg.LibraryPath);
            foreach (FileInfo file in Info.GetFiles("*.tsw2liv"))
            {
                byte[] LiveryData = File.ReadAllBytes(file.FullName);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!LibraryLiveryData! {file.Name} - The specific library livery appears to be corrupted\n - remove the file from your library\n - export or download it again\n - if the problem persists, contact the person, who shared the livery and/or create an issue on github\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                lstLibraryLiveries.Items.Add($"{Display} <{file.Name}>");
                Log.AddLogMessage($"Added library livery {file.Name} ({Display})", "MW::UpdateLocalGameLiveries", Log.LogLevel.DEBUG);
            }
            if (lstLibraryLiveries.Items.CanSort)
            {
                lstLibraryLiveries.Items.SortDescriptions.Add(
                    new SortDescription("", ListSortDirection.Ascending));
            }
            return "OK";
        }

        private string LoadLivery(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";

            string Name = GetLiveryName(liveryData);
            string Model = GetLiveryModel(liveryData);

            if (Name == null || Model == null) return null;

            if (Model.StartsWith("RF_"))
            {
                Model = Model.Remove(0, 3);
            }

            return $"{Model} | {Name}";
        }

        private string GetLiveryName(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";
            int NameStart = LocateInByteArray(liveryData, SON, SONs) + SON.Length;
            if (NameStart == -1) return null;
            int NameEnd = LocateInByteArray(liveryData, EON, NameStart);
            if (NameEnd == -1) return null;
            byte[] NameArray = new byte[NameEnd - NameStart];
            Array.Copy(liveryData, NameStart, NameArray, 0, NameArray.Length);
            return System.Text.Encoding.UTF8.GetString(NameArray);
        }

        private string GetLiveryModel(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";
            int ModelStart = LocateInByteArray(liveryData, SOM) + SOM.Length;
            if (ModelStart == -1) return null;
            int ModelEnd = LocateInByteArray(liveryData, EOM, ModelStart);
            if (ModelEnd == -1) return null;
            byte[] ModelArray = new byte[ModelEnd - ModelStart];
            Array.Copy(liveryData, ModelStart, ModelArray, 0, ModelArray.Length);
            string Model = System.Text.Encoding.UTF8.GetString(ModelArray);
            return Model.Split('.')[^1];
        }

        private byte[] GetSelectedGameLivery()
        {
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1) return null;
            int Id = int.Parse(((string)lstGameLiveries.SelectedItem).Split('(')[1].Split(')')[0]);
            SplitFile.TryGetValue(Id, out byte[] LiveryData);
            return LiveryData;
        }
        private bool SetSelectedGameLivery(byte[] liveryData)
        {
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1) return false;
            int Id = int.Parse(((string)lstGameLiveries.SelectedItem).Split('(')[1].Split(')')[0]);

            int PatchStart = LocateInByteArray(liveryData, SON, SONs);
            //TODO: PATCH BYTE SON+29 AND SON+47

            SplitFile.Add(Id, liveryData);
            return true;
        }

        private string DetermineWindowsStoreSaveFile()
        {
            string saveFilePath = Environment.GetEnvironmentVariable("LocalAppData") + "\\Packages";
            string containerFilePath;
            string pattern = "<n/a>";
            try
            {
                pattern = "DovetailGames.TrainSimWorld2021_*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                Log.AddLogMessage($"Found TrainSimWorld2021 package at '{saveFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.DEBUG);
                saveFilePath += "\\SystemAppData\\wgs";
                pattern = "*_*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                pattern = "*";
                saveFilePath = Directory.EnumerateDirectories(saveFilePath, pattern).First();
                pattern = "container.*";
                containerFilePath = Directory.EnumerateFiles(saveFilePath, pattern).First();
            }
            catch (Exception e)
            {
                Log.AddLogMessage($"couldn't find pattern '{pattern}' in '{saveFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.WARNING);
                return "";
            }
            Log.AddLogMessage($"container idx file is at '{containerFilePath}'", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.DEBUG);
            try
            {
                byte[] containerFile = File.ReadAllBytes(containerFilePath);

                byte[] key = new byte[] { 0x55, 0, 0x47, 00, 0x43, 00, 0x4c, 00, 0x69, 00, 0x76, 00, 0x65, 00, 0x72, 00, 0x69, 00, 0x65, 00, 0x73, 00, 0x5f, 00, 0x30, 00 };
                int start = LocateInByteArray(containerFile, key);
                int idx = start + key.Length;

                while (containerFile[idx] == 0) idx++;

                string fileBuilder = BitConverter.ToString(new byte[] { containerFile[idx + 3], containerFile[idx + 2], containerFile[idx + 1], containerFile[idx] });
                idx += 4;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx + 1], containerFile[idx] });
                idx += 2;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx + 1], containerFile[idx] });
                idx += 2;
                fileBuilder += BitConverter.ToString(new byte[] { containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++], containerFile[idx++] });
                fileBuilder = fileBuilder.ToUpper().Replace("-", "");
                saveFilePath += "\\" + fileBuilder;
            }
            catch (Exception e)
            {
                Log.AddLogMessage($"Unable to parse container idx file", "MW::DetermineWindowsStoreSaveFile", Log.LogLevel.WARNING);
                return "";
            }
            Log.AddLogMessage($"Liveries file is at '{saveFilePath}'");
            return saveFilePath;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            byte[] LiveryData = GetSelectedGameLivery();
            if (LiveryData != null)
            {
                Log.AddLogMessage($"Exporting game livery {lstGameLiveries.SelectedItem}...", "MW::ExportClick");
                string Name = GetLiveryName(LiveryData);
                string Model = GetLiveryModel(LiveryData);
                string FilePreset = $"{Model};{Name}";
                string FileName = FilePreset;
                foreach (char c in illegalFileNameCharacters)
                    FileName = FileName.Replace(c, '-');
                while (File.Exists($"{Cfg.LibraryPath}\\{FileName}.tsw2liv"))
                {
                    if (FileName.Split('#')[0] == FileName)
                    {
                        FileName += "#1";
                    }
                    else
                    {
                        FileName = $"{FilePreset}#{int.Parse(FileName.Split('#')[1]) + 1}";
                    }
                }
                Log.AddLogMessage($"Exporting to file {FileName}.tsw2liv", "MW::ExportClick", Log.LogLevel.DEBUG);
                File.WriteAllBytes($"{Cfg.LibraryPath}\\{FileName}.tsw2liv", LiveryData);
                string Status = UpdateLibraryLiveries();
                if (Status != "OK") lblMessage.Content = Status;
            }
            else
            {
                Log.AddLogMessage("Livery data for export empty, aborting...", "MW::ExportClick", Log.LogLevel.WARNING);
                lblMessage.Content = "Before exporting, please ensure you:\n - Have a Game Livery selected";
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            byte[] OldData = GetSelectedGameLivery();
            if (OldData == null && lstGameLiveries.SelectedItem != null && lstGameLiveries.SelectedIndex != -1 && lstLibraryLiveries.SelectedItem != null && lstLibraryLiveries.SelectedIndex != -1)
            {
                Log.AddLogMessage($"Importing livery {lstLibraryLiveries.SelectedItem} into game slot {lstGameLiveries.SelectedIndex + 1}", "MW::ImportClick");
                string FileName = lstLibraryLiveries.SelectedItem.ToString().Split('<')[1].Split('>')[0];
                byte[] LiveryData = File.ReadAllBytes($"{Cfg.LibraryPath}\\{FileName}");
                if (!SetSelectedGameLivery(LiveryData))
                {
                    lblMessage.Content = "!ImportWriteFail! - Something went wrong\n - if the issue persists, create an issue on github\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                }
                else
                {
                    string Status = UpdateLocalGameLiveries();
                    if (Status != "OK") lblMessage.Content = Status;
                }
            }
            else
            {
                Log.AddLogMessage("Import conditions not met, aborting...", "MW::ImportClick", Log.LogLevel.WARNING);
                lblMessage.Content = "Before importing, please ensure you:\n - Have an empty Game Livery slot selected\n - Have a Library Livery selected\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
            }
        }

        private void btnJExport_Click(object sender, RoutedEventArgs e)
        {
            if (!jExportWarning)
            {
                lblMessage.Content = "IMPORTANT!! - This feature is very experimental, make sure you have a backup of your liveries!!";
                Log.AddLogMessage("First Click on JSON Export, warning and ignoring...", "MW::JExportClick", Log.LogLevel.DEBUG);
                jExportWarning = true;
            }
            else if (File.Exists("GvasConverter\\GvasConverter.exe"))
            {
                Log.AddLogMessage("Second Click on JSON Export, exporting game livery file to json...", "MW::JExportClick", Log.LogLevel.DEBUG);
                SaveFileDialog Dialog = new SaveFileDialog();
                Dialog.InitialDirectory = Cfg.LibraryPath;
                Dialog.Filter = "JSON-File (*.json)|*.json";
                Dialog.DefaultExt = "*.json";
                if (Dialog.ShowDialog() == true)
                {
                    Process.Start("GvasConverter\\GvasConverter.exe", $"\"{Cfg.GamePath}\" \"{Dialog.FileName}\"");
                }
                jExportWarning = false;
            }
            else
            {
                lblMessage.Content = "ERROR: 'GvasConverter.exe' could not be found, please make sure it is in a folder called GVASConverter.";
                Log.AddLogMessage("Second Click on JSON Export, unable to find gvas coverter, aborting...", "MW::JExportClick", Log.LogLevel.WARNING);
                jExportWarning = false;
            }
        }

        private void btnJImport_Click(object sender, RoutedEventArgs e)
        {
            if (!jImportWarning)
            {
                lblMessage.Content = "IMPORTANT!! - This feature is very experimental, make sure you have a backup of your liveries!!";
                Log.AddLogMessage("First Click on JSON Import, warning and ignoring...", "MW::JImportClick", Log.LogLevel.DEBUG);
                jImportWarning = true;
            }
            else if (File.Exists("GvasConverter\\GvasConverter.exe"))
            {
                Log.AddLogMessage("Second Click on JSON Import, exporting game livery file to json...", "MW::JImportClick", Log.LogLevel.DEBUG);
                OpenFileDialog Dialog = new OpenFileDialog();
                Dialog.InitialDirectory = Cfg.LibraryPath;
                Dialog.Filter = "JSON-File (*.json)|*.json";
                Dialog.DefaultExt = "*.json";
                if (Dialog.ShowDialog() == true)
                {
                    Process.Start("GvasConverter\\GvasConverter.exe", $"{Dialog.FileName} {Cfg.GamePath}");
                }
                jImportWarning = false;
            }
            else
            {
                lblMessage.Content = "ERROR: 'GvasConverter.exe' could not be found, please make sure it is in a folder called GVASConverter.";
                Log.AddLogMessage("Second Click on JSON Import, unable to find gvas coverter, aborting...", "MW::JImportClick", Log.LogLevel.WARNING);
                jImportWarning = false;
            }
        }

        private void lstLibrary_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnLibDir_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
            Dialog.Description = "Select a folder for all your liveries to be exported to";
            if (Dialog.ShowDialog() == true)
            {
                Log.AddLogMessage("Changing library path...", "MW::LibDirClick", Log.LogLevel.DEBUG);
                Cfg.LibraryPath = Dialog.SelectedPath;
                txtLibDir.Text = Dialog.SelectedPath;
                Log.AddLogMessage($"Changed library path to {Cfg.LibraryPath}", "MW::LibDirClick");
            }
            string Status = UpdateLibraryLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void lstGame_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnGameDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                jImportWarning = false;
                jExportWarning = false;
                lblMessage.Content = "";
                VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
                Dialog.Description = "Select the TSW2 game folder";
                if (Dialog.ShowDialog() == true)
                {
                    Log.AddLogMessage("Changing game path...", "MW::GameDirClick", Log.LogLevel.DEBUG);
                    if (Dialog.SelectedPath.Contains("TrainSimWorld2WGDK"))
                    {
                        Log.AddLogMessage("Detected Windows store version", "MW::GameDirClick", Log.LogLevel.DEBUG);
                        Cfg.GamePath = DetermineWindowsStoreSaveFile();
                    }
                    else
                    {
                        Log.AddLogMessage("Detected Steam or epic store version", "MW::GameDirClick", Log.LogLevel.DEBUG);
                        Cfg.GamePath = $@"{Dialog.SelectedPath}\Saved\SaveGames\UGCLiveries_0.sav";
                    }
                    txtGameDir.Text = Dialog.SelectedPath;
                    Log.AddLogMessage($"Changed game path to {Cfg.GamePath}", "MW::GameDirClick");
                }
                string Status = LoadGameLiveries();
                if (Status != "OK") lblMessage.Content = Status;
            }catch (Exception ex)
            {
                Log.AddLogMessage(ex.ToString(), "MW::", Log.LogLevel.ERROR);
            }
        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            SaveFileDialog Dialog = new SaveFileDialog();
            Dialog.InitialDirectory = Cfg.LibraryPath;
            Dialog.Filter = "TSW2 Livery Backup (*.tsw2bak)|*.tsw2bak";
            Dialog.DefaultExt = "*.tsw2bak";
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Cfg.GamePath);
                File.WriteAllBytes(Dialog.FileName, Contents);
                Log.AddLogMessage($"Created backup: {Dialog.FileName}", "MW::BackupClick");
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "TSW2 Livery Backup (*.tsw2bak)|*.tsw2bak";
            Dialog.DefaultExt = "*.tsw2bak";
            Dialog.InitialDirectory = Cfg.LibraryPath;
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Dialog.FileName);
                File.WriteAllBytes(Cfg.GamePath, Contents);
                Log.AddLogMessage($"Restored from backup: {Dialog.FileName}", "MW::RestoreClick");
            }
            string Status = LoadGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            Log.AddLogMessage("Saving local game liveries to disk...", "MW::SaveClick");
            lblMessage.Content = "";
            byte[] AllData = SplitFile[0];
            for (int i = 1; i <= Cfg.MaxGameLiveries; i++)
            {
                byte[] Data = null;
                SplitFile.TryGetValue(i, out Data);
                if (Data == null) break;
                Log.AddLogMessage($"Saving livery {LoadLivery(Data)}", "MW::SaveClick", Log.LogLevel.DEBUG);
                AllData = AllData.Concat(Data).ToArray();
            }
            AllData = AllData.Concat(SplitFile[Cfg.MaxGameLiveries + 1]).ToArray();

            int CountLocation = LocateInByteArray(AllData, COL) + COL.Length;
            AllData[CountLocation] = (byte)(SplitFile.Count() - 2);
            string dt = DateTime.Now.ToString("yMMdd-HHmmss");
            File.WriteAllBytes($"{Cfg.GamePath}_{dt}.bak", File.ReadAllBytes(Cfg.GamePath));
            File.WriteAllBytes(Cfg.GamePath, AllData);
            Log.AddLogMessage("Saved local game liveries to disk", "MW::SaveClick", Log.LogLevel.DEBUG);
            string Status = LoadGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            string Status = LoadGameLiveries();
            if (Status != "OK") { lblMessage.Content = Status; }
            else
            {
                Status = UpdateLibraryLiveries();
                if (Status != "OK") lblMessage.Content = Status;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            jImportWarning = false;
            jExportWarning = false;
            lblMessage.Content = "";
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1)
            {
                Log.AddLogMessage($"Deleting game livery {lstGameLiveries.SelectedItem}...", "MW::DeleteClick");
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have a Game Livery selected\n\nif you need help, please @RagingLightning on discord or creare an issue on github";
                return;
            }
            int Id = int.Parse(lstGameLiveries.SelectedItem.ToString().Split('(')[1].Split(')')[0]);
            SplitFile.Remove(Id);
            string Status = UpdateLocalGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }
    }

    public class Data : INotifyPropertyChanged
    {
        private bool _useable = false;
        public bool Useable
        {
            get { return _useable; }
            set { _useable = value; OnPropertyChanged("Useable"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
