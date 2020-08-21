using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace TSW2_Livery_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        //1-30 - livery data
        //31 - footer data
        Dictionary<int, byte[]> SplitFile = new Dictionary<int, byte[]>();

       
        
        public MainWindow()
        {
            InitializeComponent();

            loadCfg();

            if (Cfg.ContainsKey("GamePath"))
            {
                txtGameDir.Text = Cfg["GamePath"];
                string GameStatus = LoadGameLiveries();
                if (GameStatus != "OK") lblMessage.Content += $"ERROR WHILE LOADING GAME LIVERY {GameStatus}";
            }
            if (Cfg.ContainsKey("LibraryPath"))
            {
                txtLibDir.Text = Cfg["LibraryPath"];
                string LibraryStatus = LoadLibraryLiveries();
                if (LibraryStatus != "OK") lblMessage.Content += $"ERROR WHILE LOADING LIBRARY LIVERY {LibraryStatus}";
            }

        }

        private void loadCfg()
        {
            if (File.Exists(ConfigPath))
            {
                string ConfigFile = File.ReadAllText(ConfigPath);
                string[] ConfigFileEntries = ConfigFile.Split(';');
                foreach (string ConfigFileEntry in ConfigFileEntries)
                {
                    if (ConfigFileEntry == "") continue;
                    Cfg.Add(ConfigFileEntry.Split('=')[0], ConfigFileEntry.Split('=')[1]);
                }
            }
        }

        private void saveCfg()
        {
            File.Delete(ConfigPath);
            foreach(string key in Cfg.Keys)
            {
                File.AppendAllText(ConfigPath, $"{key}={Cfg[key]};");
            }
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
            byte[] LiveryFile = File.ReadAllBytes(Cfg["GamePath"]);

            int HeaderEnd = LocateInByteArray(LiveryFile, SOL);
            byte[] Header = new byte[HeaderEnd];
            Array.Copy(LiveryFile, Header, Header.Length);
            SplitFile.Add(0, Header);

            int LiveryEnd = 0;

            for (int i = 1; i <= 30; i++)
            {
                int LiveryStart = LocateInByteArray(LiveryFile, SOL, LiveryEnd);
                if (LiveryStart == -1) break;
                LiveryEnd = LocatesInByteArray(LiveryFile, new byte[][] { SOL, EOL }, LiveryStart + 1);
                if (LiveryEnd == -1)
                    return $"!{i}/{LiveryStart}-{LiveryEnd}!";
                byte[] LiveryData = new byte[LiveryEnd - LiveryStart];
                Array.Copy(LiveryFile, LiveryStart, LiveryData, 0, LiveryData.Length);
                SplitFile.Add(i, LiveryData);
            }

            byte[] Footer = new byte[LiveryFile.Length - LiveryEnd];
            Array.Copy(LiveryFile, LiveryEnd, Footer, 0, Footer.Length);
            SplitFile.Add(31, Footer);

            return UpdateLocalGameLiveries();
        }

        private string UpdateLocalGameLiveries()
        {
            lstGameLiveries.Items.Clear();
            for (int i = 1; i <= 30; i++)
            {
                byte[] LiveryData;
                SplitFile.TryGetValue(i, out LiveryData);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!L{i}!";

                lstGameLiveries.Items.Add($"({i}): {Display}");
            }
            return "OK";
        }

        private string LoadLibraryLiveries()
        {
            lstLibraryLiveries.Items.Clear();
            DirectoryInfo Info = new DirectoryInfo(Cfg["LibraryPath"]);
            foreach (FileInfo file in Info.GetFiles("*.tsw2liv"))
            {
                byte[] LiveryData = File.ReadAllBytes(file.FullName);
                string Display = LoadLivery(LiveryData);
                if (Display == null) return $"!{file.Name}!";
                lstLibraryLiveries.Items.Add($"[{file.Name}]: {Display}");
            }
            return "OK";
        }

        private string LoadLivery(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";

            string Name = getLiveryName(liveryData);
            string Model = getLiveryModel(liveryData);

            return $"{Name} for {Model}";
        }

        private string getLiveryName(byte[] liveryData)
        {
            if (liveryData == null) return "<empty>";
            int NameStart = LocateInByteArray(liveryData, SON, SONs) + SON.Length;
            Console.WriteLine($"--{BitConverter.ToString(liveryData,NameStart-SON.Length+29,1)};{BitConverter.ToString(liveryData,NameStart - SON.Length + 47,1)}");
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
            Console.WriteLine(Model);
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
                File.WriteAllBytes($"{Cfg["LibraryPath"]}\\{FileName}.tsw2liv", LiveryData);
                LoadLibraryLiveries();
            }
            else
            {
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have a Game Livery selected";
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            byte[] OldData = GetSelectedGameLivery();
            if (OldData == null && lstGameLiveries.SelectedItem != null && lstGameLiveries.SelectedIndex != -1 && lstLibraryLiveries.SelectedItem != null && lstLibraryLiveries.SelectedIndex != -1)
            {
                string FileName = lstLibraryLiveries.SelectedItem.ToString().Split('[')[1].Split(']')[0];
                byte[] LiveryData = File.ReadAllBytes($"{Cfg["LibraryPath"]}\\{FileName}");
                if (!SetSelectedGameLivery(LiveryData))
                    lblMessage.Content = "Something went wrong";
                UpdateLocalGameLiveries();
            }
            else
            {
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have an empty Game Livery slot selected\n - Have a Library Livery selected";
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
                if (Cfg.ContainsKey("LibraryPath"))
                    Cfg["LibraryPath"] = Dialog.SelectedPath;
                else
                    Cfg.Add("LibraryPath", Dialog.SelectedPath);
                txtLibDir.Text = Dialog.SelectedPath;
                saveCfg();
            }
            LoadLibraryLiveries();
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
                if (Cfg.ContainsKey("GamePath"))
                    Cfg["GamePath"] = $"{Dialog.SelectedPath}\\Saved\\SaveGames\\UGCLiveries_0.sav";
                else
                    Cfg.Add("GamePath", $"{Dialog.SelectedPath}\\Saved\\SaveGames\\UGCLiveries_0.sav");
                txtGameDir.Text = Dialog.SelectedPath;
                saveCfg();
            }
            LoadGameLiveries();
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
            }
            LoadGameLiveries();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            byte[] AllData = SplitFile[0];
            for (int i = 1; i < SplitFile.Count(); i++)
            {
                byte[] Data = null;
                SplitFile.TryGetValue(i, out Data);
                if (Data != null)
                    AllData = AllData.Concat(Data).ToArray();
            }
            AllData = AllData.Concat(SplitFile[31]).ToArray();

            int CountLocation = LocateInByteArray(AllData, COL) + COL.Length;
            AllData[CountLocation] = (byte)(SplitFile.Count() - 2);
            File.WriteAllBytes(Cfg["GamePath"], AllData);
            LoadGameLiveries();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            LoadGameLiveries();
            LoadLibraryLiveries();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "";
            if (lstGameLiveries.SelectedItem == null ||lstGameLiveries.SelectedIndex == -1)
            {
                lblMessage.Content = "Something went wrong, please ensure you:\n - Have a Game Livery selected";
                return;
            }
            int Id = int.Parse(lstGameLiveries.SelectedItem.ToString().Split('(')[1].Split(')')[0]);
            SplitFile.Remove(Id);
            UpdateLocalGameLiveries();
        }
    }
}
