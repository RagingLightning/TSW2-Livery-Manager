#nullable enable
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
        private const int MAX_GAME_LIVERIES = 30;
        private const string VERSION = "0.0.3";

        //COUNT OF LIVERIES
        readonly byte[] COL = new byte[] { 0x53, 0x74, 0x72, 0x75, 0x63, 0x74, 0x50, 0x72, 0x6f, 0x70, 0x65, 0x72, 0x74, 0x79, 0, 0 };
        //START OF LIVERY
        readonly byte[] SOL = new byte[] { 0, 3, 0, 0, 0, 0x49, 0x44 };
        //END OF LIVERIES
        readonly byte[] EOL = new byte[] { 0, 5, 0, 0, 0, 0x4e, 0x6f, 0x6e, 0x65, 0, 0, 0, 0, 0 };
        // START OF NAME
        readonly byte[] SON = new byte[] { 0x44, 0x69, 0x73, 0x70, 0x6c, 0x61, 0x79, 0x4e, 0x61, 0x6d, 0x65, 0, 0xd, 0, 0, 0, 0x54, 0x65, 0x78, 0x74, 0x50, 0x72, 0x6f, 0x70, 0x65, 0x72, 0x74, 0x79, 0, 0x13, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0xff, 1, 0, 0, 0, 6, 0, 0, 0 };
        //BYTE 29 AND 47 INCREMENT
        readonly int[] SONs = new int[] { 29, 47};
        //END OF NAME
        readonly byte[] EON = new byte[] { 0, 15 };
        //START OF MODEL
        readonly byte[] SOM = new byte[] { 0, 0, 0, 0x2f };
        //END OF MODEL
        readonly byte[] EOM = new byte[] { 0 };

        readonly string ConfigPath = "TSW2LM.cfg";
        static Dictionary<string, string> Cfg = new Dictionary<string, string>();

        //0 - header data
        //1-MAX_GAME_LIVERIES - livery data
        //MAX_GAME_LIVERIES+1 - footer data
        Dictionary<int, byte[]> SplitFile = new Dictionary<int, byte[]>();

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        public MainWindow()
        {

            AttachConsole(-1);

            Log.AddLogFile("TSW2LM.log", Log.LogLevel.INFO);
            if (Environment.GetCommandLineArgs().Contains("-debug"))
            {
                Log.AddLogFile("TSW2LM_debug.log", Log.LogLevel.DEBUG);
                Log.ConsoleLevel = Log.LogLevel.DEBUG;
            }

            LoadCfg();

            if (!Cfg.ContainsKey("NoUpdate") || Cfg["NoUpdate"] != "1")
            {
                try
                {
                    Log.AddLogMessage("Checking for updates...", "MW::<init>");
                    WebRequest UpdateRequest = WebRequest.Create("https://raw.githubusercontent.com/RagingLightning/TSW2-Livery-Manager/deploy/version.dat");
                    string UpdateResponse = new StreamReader(UpdateRequest.GetResponse().GetResponseStream()).ReadToEnd();
                    Log.AddLogMessage($"Got version information: {VERSION}->{UpdateResponse}", "MW::<init>");
                    string[] NewVersion = UpdateResponse.Split('.');
                    string[] CurrentVersion = VERSION.Split('.');
                    for (int i = 0; i < NewVersion.Length; i++)
                    {
                        if (int.Parse(NewVersion[i]) > int.Parse(CurrentVersion[i]))
                        {
                            new UpdateNotifier(VERSION, UpdateResponse, $"https://github.com/RagingLightning/TSW2-Livery-Manager/releases/tag/v{UpdateResponse}").ShowDialog();
                        }
                    }
                }
                catch (WebException e)
                {
                    Log.AddLogMessage($"Unable to check for updates: {e.Message}", "MW::<init>", Log.LogLevel.DEBUG);
                }
                
            }

            InitializeComponent();
            DataContext = new Data();

            if (Cfg.ContainsKey("GamePath"))
            {
                Log.AddLogMessage("Loading GamePath Data...", "MW::<init>");
                txtGameDir.Text = Cfg["GamePath"];
                string GameStatus = LoadGameLiveries();
                if (GameStatus != "OK") lblMessage.Content += $"ERROR WHILE LOADING GAME LIVERIES:\n{GameStatus}";
            }
            if (Cfg.ContainsKey("LibraryPath"))
            {
                txtLibDir.Text = Cfg["LibraryPath"];
                string LibraryStatus = UpdateLibraryLiveries();
                if (LibraryStatus != "OK") lblMessage.Content += $"ERROR WHILE LOADING LIBRARY LIVERIES:\n{LibraryStatus}";
            }

        }

        private void LoadCfg()
        {
            if (File.Exists(ConfigPath))
            {
                Log.AddLogMessage("Loading Config...", "MW::LoadCfg", Log.LogLevel.DEBUG);
                string ConfigFile = File.ReadAllText(ConfigPath);
                string[] ConfigFileEntries = ConfigFile.Split(';');
                foreach (string ConfigFileEntry in ConfigFileEntries)
                {
                    if (ConfigFileEntry == "") continue;
                    string key = ConfigFileEntry.Split('=')[0];
                    string val = ConfigFileEntry.Split('=')[1];
                    Log.AddLogMessage($"|> Config option {key} is set to {val}", "MW::LoadCfg", Log.LogLevel.DEBUG);
                    Cfg.Add(key, val);
                }
                Log.AddLogMessage("Config loaded","MW::LoadCfg",Log.LogLevel.DEBUG);
            }
            else
            {
                Log.AddLogMessage("No config file found, applying default config...", "MW::LoadConfig", Log.LogLevel.DEBUG);
                Cfg["GamePath"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Trainsimworld2\\Saved\\SaveGames\\UGCLiveries_0.sav";
                SaveCfg();
                Log.AddLogMessage("Default config applied", "MW::LoadCfg", Log.LogLevel.DEBUG);
            }
        }

        private void SaveCfg()
        {
            Log.AddLogMessage("Saving Config...", "MW::SaveCfg", Log.LogLevel.DEBUG);
            File.Delete(ConfigPath);
            foreach(string key in Cfg.Keys)
            {
                Log.AddLogMessage($"|> Config option {key} set to {Cfg[key]}", "MW::SaveCfg", Log.LogLevel.DEBUG);
                File.AppendAllText(ConfigPath, $"{key}={Cfg[key]};");
            }
            Log.AddLogMessage("Config saved", "MW::SaveCfg", Log.LogLevel.DEBUG);
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
                if(hay[i] == needle[0])
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
            for(int j = 0; j < needles.Length; j++)
            {
                int LocalFind = LocateInByteArray(hay, needles[j], start, end);
                FindStart = ((LocalFind != -1 && LocalFind < FindStart) || FindStart == -1) ? LocalFind : FindStart;
            }
            return FindStart;
        }

        private string LoadGameLiveries()
        {
            SplitFile.Clear();
            try
            {
                Log.AddLogMessage("Loading game livery file...","MW::LoadGameLiveries");
                Log.AddLogMessage($"File Path: {Cfg["GamePath"]}", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);
                byte[] LiveryFile = File.ReadAllBytes(Cfg["GamePath"]);

                int HeaderEnd = LocateInByteArray(LiveryFile, SOL);
                byte[] Header = new byte[HeaderEnd];
                Array.Copy(LiveryFile, Header, Header.Length);
                SplitFile.Add(0, Header);
                Log.AddLogMessage($"Extracted game livery header (bytes 0 - {HeaderEnd})", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);

                Log.AddLogMessage("Extracting game liveries...", "MW::LoadGameLiveries");
                int LiveryEnd = 0;

                for (int i = 1; i <= MAX_GAME_LIVERIES; i++)
                {
                    int LiveryStart = LocateInByteArray(LiveryFile, SOL, LiveryEnd);
                    if (LiveryStart == -1) break;
                    LiveryEnd = LocatesInByteArray(LiveryFile, new byte[][] { SOL, EOL }, LiveryStart + 1);
                    if (LiveryEnd == -1)
                    {
                        Log.AddLogMessage($"Non-Ending livery {i} starting at byte {LiveryStart}, aborting file parsing", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                        ((Data)DataContext).Useable = false;
                        return $"!NoEndLivery! {i}/{LiveryStart} - your game's livery file appears to be corrupted\n - restore it from a previously created backup\n - if the problem persists, or you are certain, the file is not corrupted, feel free to create an issue on github";
                    }
                    byte[] LiveryData = new byte[LiveryEnd - LiveryStart];
                    Array.Copy(LiveryFile, LiveryStart, LiveryData, 0, LiveryData.Length);
                    SplitFile.Add(i, LiveryData);
                    Log.AddLogMessage($"Extracted livery {i} (bytes {LiveryStart} - {LiveryEnd})","MW::LoadGameLiveries",Log.LogLevel.DEBUG);
                }
                Log.AddLogMessage("All game liveries extracted successfully", "MW::LoadGameLiveries");

                byte[] Footer = new byte[LiveryFile.Length - LiveryEnd];
                Array.Copy(LiveryFile, LiveryEnd, Footer, 0, Footer.Length);
                SplitFile.Add(MAX_GAME_LIVERIES+1, Footer);
                Log.AddLogMessage($"Extracted game livery footer (bytes {LiveryEnd} - {LiveryFile.Length - 1})", "MW::LoadGameLiveries", Log.LogLevel.DEBUG);

                ((Data)DataContext).Useable = true;

                return UpdateLocalGameLiveries();
            }
            catch (FileNotFoundException e)
            {
                Cfg.Remove("GamePath");
                SaveCfg();
                Log.AddLogMessage($"FileNotFoundException: {e.FileName}", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                ((Data)DataContext).Useable = false;
                return $"!FileNotFound! game livery file - Make sure, you selected the Trainsimworld2 folder";
            }
            catch (IOException e)
            {
                Cfg.Remove("GamePath");
                SaveCfg();
                Log.AddLogMessage($"IOException: {e.Message}", "MW::LoadGameLiveries", Log.LogLevel.WARNING);
                ((Data)DataContext).Useable = false;
                return $"!IOException! game livery file - Make sure, you selected the Trainsimworld2 folder";
            }
        }

        private string UpdateLocalGameLiveries()
        {
            Log.AddLogMessage("Updating local game liveries...", "MW::UpdateLocalGameLiveries");
            lstGameLiveries.Items.Clear();
            for (int i = 1; i <= MAX_GAME_LIVERIES; i++)
            {
                byte[] LiveryData;
                SplitFile.TryGetValue(i, out LiveryData);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!GameLiveryData! livery {i} - your game's livery file appears to be corrupted\n - restore the game livery file from a previous backup\n - create an issue on github";

                lstGameLiveries.Items.Add($"({i}): {Display}");
                Log.AddLogMessage($"Added game livery {i} ({Display})", "MW::UpdateLocalGameLiveries", Log.LogLevel.DEBUG);
            }
            return "OK";
        }

        private string UpdateLibraryLiveries()
        {
            Log.AddLogMessage("Updating library liveries...", "MW::UpdateLibraryLiveries");
            lstLibraryLiveries.Items.Clear();
            DirectoryInfo Info = new DirectoryInfo(Cfg["LibraryPath"]);
            foreach (FileInfo file in Info.GetFiles("*.tsw2liv"))
            {
                byte[] LiveryData = File.ReadAllBytes(file.FullName);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!LibraryLiveryData! {file.Name} - The specific library livery appears to be corrupted\n - remove the file from your library\n - export or download it again\n - if the problem persists, contact the person, who shared the livery and/or create an issue on github";
                lstLibraryLiveries.Items.Add($"{Display} <{file.Name}>");
                Log.AddLogMessage($"Added library livery {file.Name} ({Display})", "MW::UpdateLocalGameLiveries", Log.LogLevel.DEBUG);
            }
            return "OK";
        }

        private string LoadLivery(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";

            string Name = getLiveryName(liveryData);
            string Model = getLiveryModel(liveryData);

            if (Name == null || Model == null) return null;

            return $"{Name} for {Model}";
        }

        private string getLiveryName(byte[] liveryData)
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

        private string getLiveryModel(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";
            int ModelStart = LocateInByteArray(liveryData, SOM) + SOM.Length;
            if (ModelStart == -1) return null;
            int ModelEnd = LocateInByteArray(liveryData, EOM, ModelStart);
            if (ModelEnd == -1) return null;
            byte[] ModelArray = new byte[ModelEnd - ModelStart];
            Array.Copy(liveryData, ModelStart, ModelArray, 0, ModelArray.Length);
            string Model =  System.Text.Encoding.UTF8.GetString(ModelArray);
            return Model.Split('.')[Model.Split('.').Length - 1];
        }

        private byte[] GetSelectedGameLivery()
        {
            if (lstGameLiveries.SelectedItem == null || lstGameLiveries.SelectedIndex == -1) return null;
            int Id = int.Parse(((string)lstGameLiveries.SelectedItem).Split('(')[1].Split(')')[0]);
            byte[] LiveryData = null;
            SplitFile.TryGetValue(Id, out LiveryData);
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

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            byte[] LiveryData = GetSelectedGameLivery();
            if(LiveryData != null)
            {
                Log.AddLogMessage($"Exporting game livery {lstGameLiveries.SelectedItem}...", "MW::ExportClick");
                string Name = getLiveryName(LiveryData);
                string Model = getLiveryModel(LiveryData);
                string FilePreset = $"{Model};{Name}";
                string FileName = FilePreset;
                while (File.Exists($"{Cfg["LibraryPath"]}\\{FileName}.tsw2liv"))
                {
                    if (FileName.Split('#')[0] == FileName)
                    {
                        FileName += "#1";
                    } else
                    {
                        FileName = $"{FilePreset}#{int.Parse(FileName.Split('#')[1]) + 1}";
                    }
                }
                Log.AddLogMessage($"Exporting to file {FileName}.tsw2liv", "MW::ExportClick", Log.LogLevel.DEBUG);
                File.WriteAllBytes($"{Cfg["LibraryPath"]}\\{FileName}.tsw2liv", LiveryData);
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
            lblMessage.Content = "";
            byte[] OldData = GetSelectedGameLivery();
            if (OldData == null && lstGameLiveries.SelectedItem != null && lstGameLiveries.SelectedIndex != -1 && lstLibraryLiveries.SelectedItem != null && lstLibraryLiveries.SelectedIndex != -1)
            {
                Log.AddLogMessage($"Importing livery {lstLibraryLiveries.SelectedItem} into game slot {lstGameLiveries.SelectedIndex + 1}", "MW::ImportClick");
                string FileName = lstLibraryLiveries.SelectedItem.ToString().Split('<')[1].Split('>')[0];
                byte[] LiveryData = File.ReadAllBytes($"{Cfg["LibraryPath"]}\\{FileName}");
                if (!SetSelectedGameLivery(LiveryData))
                {
                    lblMessage.Content = "!ImportWriteFail! - Something went wrong\n - if the issue persists, create an issue on github";
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
                lblMessage.Content = "Before importing, please ensure you:\n - Have an empty Game Livery slot selected\n - Have a Library Livery selected";
            }
        }

        private void lstLibrary_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnLibDir_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
            Dialog.Description = "Select a folder for all your liveries to be exported to";
            if (Dialog.ShowDialog() == true)
            {
                Log.AddLogMessage("Changing library path...","MW::LibDirClick",Log.LogLevel.DEBUG);
                if (Cfg.ContainsKey("LibraryPath"))
                    Cfg["LibraryPath"] = Dialog.SelectedPath;
                else
                    Cfg.Add("LibraryPath", Dialog.SelectedPath);
                txtLibDir.Text = Dialog.SelectedPath;
                SaveCfg();
                Log.AddLogMessage($"Changed library path to {Cfg["LibraryPath"]}", "MW::LibDirClick");
            }
            string Status = UpdateLibraryLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void lstGame_Change(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnGameDir_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            VistaFolderBrowserDialog Dialog = new VistaFolderBrowserDialog();
            Dialog.Description = "Select the TSW2 game folder";
            if (Dialog.ShowDialog() == true)
            {
                Log.AddLogMessage("Changing game path...", "MW::GameDirClick", Log.LogLevel.DEBUG);
                if (Cfg.ContainsKey("GamePath"))
                    Cfg["GamePath"] = $"{Dialog.SelectedPath}\\Saved\\SaveGames\\UGCLiveries_0.sav";
                else
                    Cfg.Add("GamePath", $"{Dialog.SelectedPath}\\Saved\\SaveGames\\UGCLiveries_0.sav");
                txtGameDir.Text = Dialog.SelectedPath;
                SaveCfg();
                Log.AddLogMessage($"Changed game path to {Cfg["GamePath"]}", "MW::GameDirClick");
            }
            string Status = LoadGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            SaveFileDialog Dialog = new SaveFileDialog();
            Dialog.InitialDirectory = Cfg["LibraryPath"];
            Dialog.Filter = "TSW2 Livery Backup (*.tsw2bak)|*.tsw2bak";
            Dialog.DefaultExt = "*.tsw2bak";
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Cfg["GamePath"]);
                File.WriteAllBytes(Dialog.FileName, Contents);
                Log.AddLogMessage($"Created backup: {Dialog.FileName}", "MW::BackupClick");
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "TSW2 Livery Backup  (*.tsw2bak)|*.tsw2bak";
            Dialog.DefaultExt = "*.tsw2bak";
            Dialog.InitialDirectory = Cfg["LibraryPath"];
            if (Dialog.ShowDialog() == true)
            {
                byte[] Contents = File.ReadAllBytes(Dialog.FileName);
                File.WriteAllBytes(Cfg["GamePath"], Contents);
                Log.AddLogMessage($"Restored from backup: {Dialog.FileName}", "MW::RestoreClick");
            }
            string Status = LoadGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Log.AddLogMessage("Saving local game liveries to disk...", "MW::SaveClick");
            lblMessage.Content = "";
            byte[] AllData = SplitFile[0];
            for (int i = 1; i < MAX_GAME_LIVERIES; i++)
            {
                byte[] Data = null;
                SplitFile.TryGetValue(i, out Data);
                if (Data == null) break;
                Log.AddLogMessage($"Saving livery {LoadLivery(Data)}","MW::SaveClick",Log.LogLevel.DEBUG);
                AllData = AllData.Concat(Data).ToArray();
            }
            AllData = AllData.Concat(SplitFile[MAX_GAME_LIVERIES+1]).ToArray();

            int CountLocation = LocateInByteArray(AllData, COL) + COL.Length;
            AllData[CountLocation] = (byte)(SplitFile.Count() - 2);
            File.WriteAllBytes(Cfg["GamePath"], AllData);
            Log.AddLogMessage("Saved local game liveries to disk", "MW::SaveClick", Log.LogLevel.DEBUG);
            string Status = LoadGameLiveries();
            if (Status != "OK") lblMessage.Content = Status;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
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
            lblMessage.Content = "";
            if (lstGameLiveries.SelectedItem == null ||lstGameLiveries.SelectedIndex == -1)
            {
                Log.AddLogMessage($"Deleting game livery {lstGameLiveries.SelectedItem}...", "MW::DeleteClick");
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have a Game Livery selected";
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
