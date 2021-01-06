#nullable disable warnings
using System;
using System.IO;

namespace TSW2LM
{
    class Config
    {

        private readonly string ConfigPath = "TSW2LM.cfg";

        private bool _skipAutosave = false;
        public bool SkipAutosave
        {
            set { _skipAutosave = value; Save(); }
        }

        private void Save()
        {
            Log.AddLogMessage("Saving Config...", "Config::Save", Log.LogLevel.DEBUG);
            File.Delete(ConfigPath);

            Log.AddLogMessage($"|> Config option 'GamePath' set to '{_gamePath}'", "Config::Save", Log.LogLevel.DEBUG);
            File.AppendAllText(ConfigPath, $"GamePath={_gamePath};");

            Log.AddLogMessage($"|> Config option 'LibraryPath' set to '{_libraryPath}'", "Config::Save", Log.LogLevel.DEBUG);
            File.AppendAllText(ConfigPath, $"LibraryPath={_libraryPath};");

            Log.AddLogMessage($"|> Config option 'MaxGameLiveries' set to '{_maxGameLiveries}'", "Config::Save", Log.LogLevel.DEBUG);
            File.AppendAllText(ConfigPath, $"MaxGameLiveries={_maxGameLiveries};");

            if (_noUpdate)
            {
                Log.AddLogMessage($"|> Config option 'NoUpdate' set to 'true'", "Config::Save", Log.LogLevel.DEBUG);
                File.AppendAllText(ConfigPath, $"NoUpdate=true;");
            }

            Log.AddLogMessage("Config saved", "Config::Save", Log.LogLevel.DEBUG);
        }

        public void Load()
        {
            ApplyDefaults();
            if (File.Exists(ConfigPath))
            {
                Log.AddLogMessage("Loading Config...", "Config::Load", Log.LogLevel.DEBUG);
                string ConfigFile = File.ReadAllText(ConfigPath);
                string[] ConfigFileEntries = ConfigFile.Split(';');
                foreach (string ConfigFileEntry in ConfigFileEntries)
                {
                    if (ConfigFileEntry == "") continue;
                    string key = ConfigFileEntry.Split('=')[0];
                    string val = ConfigFileEntry.Split('=')[1];
                    Log.AddLogMessage($"|> Config option '{key}' is set to '{val}'", "Config::Load", Log.LogLevel.DEBUG);
                    try
                    {
                        switch (key)
                        {
                            case "GamePath": _gamePath = val; break;
                            case "LibraryPath": _libraryPath = val; break;
                            case "MaxGameLiveries": _maxGameLiveries = int.Parse(val); break;
                            case "NoUpdate": _noUpdate = val == "true"; break;
                        }
                    }
                    catch (Exception)
                    {
                        Log.AddLogMessage("Error loading config; applying default config...", "Config::Load", Log.LogLevel.WARNING);
                        ApplyDefaults();
                    }
                }
                Log.AddLogMessage("Config loaded", "Config::Load", Log.LogLevel.DEBUG);
            }
        }

        public void ApplyDefaults()
        {
            _gamePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Trainsimworld2\\Saved\\SaveGames\\UGCLiveries_0.sav";
            _libraryPath = "";
            _maxGameLiveries = 30;
            _noUpdate = false;
            Save();
        }

        private string _gamePath;
        public string GamePath
        {
            get { return _gamePath; }
            set { _gamePath = value; if (!_skipAutosave) Save(); }
        }

        private string _libraryPath;
        public string LibraryPath
        {
            get { return _libraryPath; }
            set { _libraryPath = value; if (!_skipAutosave) Save(); }
        }

        private bool _noUpdate;
        public bool NoUpdate
        {
            get { return _noUpdate; }
            set { _noUpdate = value; if (!_skipAutosave) Save(); }
        }

        private int _maxGameLiveries;
        public int MaxGameLiveries
        {
            get { return _maxGameLiveries; }
            set { _maxGameLiveries = value; if (!_skipAutosave) Save(); }
        }
    }

}
