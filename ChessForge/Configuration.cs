﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public class Configuration
    {
        //*********************************
        // CONFIGURATION ITEMS
        //*********************************
        /// <summary>
        /// The time in milliseconds that it takes
        /// to animate a move on the board
        /// </summary>
        public static int MoveSpeed = 200;

        /// <summary>
        /// Last directory from which a Workbook PGN file was read.
        /// </summary>
        public static string LastOpenDirectory = "";

        /// <summary>
        /// Last directory from which a a PGN file for import was read.
        /// </summary>
        public static string LastImportDirectory = "";

        /// <summary>
        /// Last read Workbook file.
        /// </summary>
        public static string LastWorkbookFile = "";

        /// <summary>
        /// Path to the engine executable
        /// </summary>
        public static string EngineExePath = "";

        /// <summary>
        /// Version for which not show the update alert
        /// </summary>
        public static string DoNotShowVersion = "";

        /// <summary>
        /// By how many pixels to adjust font size
        /// in the views.
        /// (in milliseconds)
        /// </summary>
        public static int FontSizeDiff = 0;

        /// <summary>
        /// How often AutoSave is called
        /// (in seconds)
        /// </summary>
        public static int AutoSaveFrequency = 60;

        /// <summary>
        /// Time given to the engine to evaluate a single move
        /// (in milliseconds)
        /// </summary>
        public static int EngineEvaluationTime = 1000;

        /// <summary>
        /// Time given to the engine to respond
        /// during a training game.
        /// (in milliseconds)
        /// </summary>
        public static int EngineMoveTime = 1000;

        /// <summary>
        /// When choosing "viable" repsonsed from the engine
        /// during a game, moves under consideration must not
        /// be worse than by this centipawn value from the
        /// best move.
        /// </summary>
        public static int ViableMoveCpDiff = 100;

        /// <summary>
        /// Number of moves to return with evaluations.
        /// </summary>
        public static int EngineMpv = 5;

        /// <summary>
        /// Whether the main window was maximized
        /// when Chess Forge closed last time.
        /// </summary>
        public static bool MainWinMaximized = false;

        /// <summary>
        /// Whether to show the generic PGN file info
        /// when opening a non-Chess Forge file.
        /// </summary>
        public static bool ShowGenericPgnInfo = true;

        /// <summary>
        /// Whether to show move options at a fork.
        /// </summary>
        public static bool ShowMovesAtFork = true;

        /// <summary>
        /// Whether to show the Explorers.
        /// </summary>
        public static bool ShowExplorers = true;

        /// <summary>
        /// Whether AutoSave is On.
        /// </summary>
        public static bool AutoSave = false;

        /// <summary>
        /// Whether sound in turned on
        /// </summary>
        public static bool SoundOn = true;

        /// <summary>
        /// Whether fixed size font should be used.
        /// </summary>
        public static bool UseFixedFont = false;

        /// <summary>
        /// Whether to allow replaying moves with the mouse wheel. 
        /// </summary>
        public static bool AllowMouseWheelForMoves = false;

        /// <summary>
        /// Whether to include bookmark locations in the PGN export
        /// </summary>
        public static bool PgnExportBookmarks = true;

        /// <summary>
        /// Whether to include evaluations in the PGN export
        /// </summary>
        public static bool PgnExportEvaluations = true;

        /// <summary>
        /// Debug message level.
        /// 0 - no debug messaging of logging
        /// 1 - logging for the app and the engine is enabled.
        /// 2 - messages boxes may pop up when some errors or exceptions ar caught
        /// 3 - some heavy debug tools are enabled (e.g. an extra button in Position Setup to show the current setup in the main window)
        /// </summary>
        public static int DebugLevel = 1;


        // max value by which a font size can be increased from the standard size
        private const int MAX_UP_FONT_SIZE_DIFF = 4;

        // max value by which a font size can be decreased from the standard size
        private const int MAX_DOWN_FONT_SIZE_DIFF = -2;


        //*********************************
        // CONFIGUARTION ITEM NAMES
        //*********************************

        private const string CFG_MOVE_SPEED = "MoveSpeed";
        private const string CFG_LAST_DIRECTORY = "LastDirectory";
        private const string CFG_LAST_IMPORT_DIRECTORY = "LastImportDirectory";
        private const string CFG_LAST_FILE = "LastFile";
        private const string CFG_RECENT_FILES = "RecentFiles";
        private const string CFG_MAIN_WINDOW_POS = "MainWindowPosition";
        private const string CFG_ENGINE_EXE = "EngineExe";
        private const string CFG_DO_NOT_SHOW_VERSION = "DoNotShowVersion";

        /// <summary>
        /// Time the engine has to make a move in a training game
        /// </summary>
        private const string CFG_ENGINE_MOVE_TIME = "EngineMoveTime";

        /// <summary>
        /// Time for the engine to evaluate position in the evaluation mode.
        /// </summary>
        private const string CFG_ENGINE_EVALUATION_TIME = "EngineEvaluationTime";
        private const string CFG_ENGINE_MPV = "EngineMpv";
        private const string CFG_VIABLE_MOVE_CP_DIFF = "ViableMoveCpDiff";

        private const string CFG_FONT_SIZE_DIFF = "FontSizeDiff";
        private const string CFG_AUTO_SAVE_FREQ = "AutoSaveFrequency";

        /// <summary>
        /// PGN export configuration.
        /// What to include
        /// </summary>
        private const string CFG_PGN_EXP_BOOKMARKS = "PgnExportBookmarks";
        private const string CFG_PGN_EXP_EVALS = "PgnExportEvals";

        private const string CFG_AUTO_SAVE = "AutoSave";
        private const string CFG_SOUND_ON = "SoundOn";
        private const string CFG_USE_FIXED_FONT = "UseFixedFont";
        private const string CFG_SHOW_MOVES_AT_FORK = "ShowMovesAtFork";
        private const string CFG_SHOW_EXPLORERS = "ShowExplorers";
        private const string CFG_MAIN_WIN_MAXIMIZED = "MainWinMaximized";
        private const string CFG_SHOW_GENERIC_PGN_INFO = "ShowGenericPgnInfo";
        private const string CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES = "AllowMouseWheelForMoves";

        public static string StartDirectory = "";

        // name of the file in which this configuration is stored.
        public static string ConfigurationFile = "config.txt";

        private const string CFG_DEBUG_MODE = "DebugMode";

        // position of the main application window
        public static Thickness MainWinPos = new Thickness();

        // List of recently opened files
        public static List<string> RecentFiles = new List<string>();

        private static int MAX_RECENT_FILES = 12;

        // application's main window
        private static MainWindow MainWin;

        /// <summary>
        /// Returns true if the font size is set to its max allowed value
        /// </summary>
        public static bool IsFontSizeAtMax
        {
            get => FontSizeDiff >= MAX_UP_FONT_SIZE_DIFF;
        }

        /// <summary>
        /// Returns true if the font size is set to its min allowed value
        /// </summary>
        public static bool IsFontSizeAtMin
        {
            get => FontSizeDiff <= MAX_DOWN_FONT_SIZE_DIFF;
        }

        /// <summary>
        /// Adds a file to the list of recently opened files.
        /// Removes a previous duplicate entry if exists.
        /// Removes oldes item if size of the list exceeded.
        /// </summary>
        /// <param name="path"></param>
        public static void AddRecentFile(string path)
        {
            // remove dupe if any
            RecentFiles.Remove(path);

            // remove the latest file if the list is too long
            if (RecentFiles.Count >= MAX_RECENT_FILES)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            // insert the new file
            RecentFiles.Insert(0, path);
        }

        /// <summary>
        /// Removes an item from a list of recent files.
        /// This will be called, if the app failed go open
        /// a file on the recent files list.
        /// </summary>
        /// <param name="path"></param>
        public static void RemoveFromRecentFiles(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                RecentFiles.Remove(path);
                if (LastWorkbookFile == path)
                {
                    LastWorkbookFile = "";
                }
            }
        }

        /// <summary>
        /// Maps configurable items to strings
        /// representing them in the configuration file.
        /// </summary>
        private static Dictionary<object, string> ItemToName = new Dictionary<object, string>();

        /// <summary>
        /// Initializes variables.
        /// </summary>
        /// <param name="mainWin"></param>
        public static void Initialize(MainWindow mainWin)
        {
            MainWin = mainWin;

            MoveSpeed = 200;
            LastOpenDirectory = "";
            LastImportDirectory = "";
            LastWorkbookFile = "";
        }

        /// <summary>
        /// Reads configuration key/value pairs from the configuration file
        /// </summary>
        public static void ReadConfigurationFile()
        {
            try
            {
                string fileName = Path.Combine(StartDirectory, ConfigurationFile);
                if (File.Exists(fileName))
                {
                    string[] lines = File.ReadAllLines(fileName);
                    foreach (string line in lines)
                    {
                        ProcessConfigurationItem(line);
                    }
                }
            }
            // we don't want to derail the program beacause something went wrong here
            // we'll just use defaults
            catch { }
        }

        /// <summary>
        /// Writes configuration key/value pairs to the configuration file.
        /// </summary>
        public static void WriteOutConfiguration()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                string fileName = Path.Combine(StartDirectory, ConfigurationFile);

                sb.Append(CFG_DEBUG_MODE + "=" + DebugLevel.ToString() + Environment.NewLine);

                sb.Append(CFG_MOVE_SPEED + "=" + MoveSpeed.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_DIRECTORY + "=" + LastOpenDirectory.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_IMPORT_DIRECTORY + "=" + LastImportDirectory.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_FILE + "=" + LastWorkbookFile.ToString() + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.Append(CFG_ENGINE_EXE + "=" + EngineExePath + Environment.NewLine);
                sb.Append(CFG_DO_NOT_SHOW_VERSION + "=" + DoNotShowVersion + Environment.NewLine);

                sb.Append(CFG_ENGINE_MOVE_TIME + "=" + EngineMoveTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_EVALUATION_TIME + "=" + EngineEvaluationTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_MPV + "=" + EngineMpv.ToString() + Environment.NewLine);

                sb.Append(CFG_FONT_SIZE_DIFF + "=" + FontSizeDiff.ToString() + Environment.NewLine);
                sb.Append(CFG_AUTO_SAVE_FREQ + "=" + AutoSaveFrequency.ToString() + Environment.NewLine);

                sb.Append(CFG_VIABLE_MOVE_CP_DIFF + "=" + ViableMoveCpDiff.ToString() + Environment.NewLine);

                sb.Append(CFG_PGN_EXP_BOOKMARKS + "=" + (PgnExportBookmarks ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_PGN_EXP_EVALS + "=" + (PgnExportEvaluations ? "1" : "0") + Environment.NewLine);

                sb.Append(CFG_AUTO_SAVE + "=" + (AutoSave ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SOUND_ON + "=" + (SoundOn ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_USE_FIXED_FONT + "=" + (UseFixedFont ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_MOVES_AT_FORK + "=" + (ShowMovesAtFork ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_EXPLORERS + "=" + (ShowExplorers ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_GENERIC_PGN_INFO + "=" + (ShowGenericPgnInfo ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES + "=" + (AllowMouseWheelForMoves ? "1" : "0") + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.Append(GetRecentFiles());
                sb.Append(Environment.NewLine);

                sb.Append(GetWindowPosition(out bool isMaximized));
                sb.Append(CFG_MAIN_WIN_MAXIMIZED + "=" + (isMaximized ? "1" : "0") + Environment.NewLine);

                sb.Append(Environment.NewLine);


                File.WriteAllText(fileName, sb.ToString());
            }
            catch { }
        }

        /// <summary>
        /// Gets the position of the Main Window and encodes it.
        /// Detects maximized state.
        /// for saving.
        /// </summary>
        /// <returns></returns>
        public static string GetWindowPosition(out bool isMaximized)
        {
            isMaximized = Application.Current.MainWindow.WindowState == WindowState.Maximized;

            double left = Application.Current.MainWindow.Left;
            double top = Application.Current.MainWindow.Top;
            double right = left + Application.Current.MainWindow.Width;
            double bottom = top + Application.Current.MainWindow.Height;

            // if is maximized adjust the coordinates so they are within the virtual screen and do not cause
            // IsValidPosition to return false.
            if (isMaximized)
            {
                left = Math.Max(left, SystemParameters.VirtualScreenLeft);
                top = Math.Max(top, SystemParameters.VirtualScreenTop);
                right = Math.Min(left + Application.Current.MainWindow.Width, SystemParameters.VirtualScreenWidth);
                bottom = Math.Min(top + Application.Current.MainWindow.Height, SystemParameters.VirtualScreenHeight);
            }

            return CFG_MAIN_WINDOW_POS + " = " + left.ToString() + "," + top.ToString() + ","
                + right.ToString() + "," + bottom.ToString() + Environment.NewLine;
        }

        /// <summary>
        /// Checks if the saved Window position is valid and can be used
        /// when reopening the application.
        /// We consider the position valid if both width and length are greater than 100
        /// and it is fully within the Virtual Screen.
        /// </summary>
        /// <returns></returns>
        public static bool IsMainWinPosValid()
        {
            if (MainWinPos.Right > MainWinPos.Left + 400 && MainWinPos.Bottom > MainWinPos.Top + 200)
            {
                if (SystemParameters.VirtualScreenLeft <= MainWinPos.Left && SystemParameters.VirtualScreenWidth >= MainWinPos.Right
                    && SystemParameters.VirtualScreenTop <= MainWinPos.Top && SystemParameters.VirtualScreenHeight >= MainWinPos.Bottom)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the last Window position was recorded as maximized.
        /// In the older versions this was expressed by having all coords == 0.
        /// </summary>
        /// <returns></returns>
        public static bool IsMainWinMaximized()
        {
            return MainWinMaximized || (MainWinPos.Right == 0 && MainWinPos.Left == 0 && MainWinPos.Bottom == 0 && MainWinPos.Top == 0);
        }

        /// <summary>
        /// Gets the list of recently opend files and combines it
        /// in one string for saving.
        /// </summary>
        /// <returns></returns>
        public static string GetRecentFiles()
        {
            string itemName = CFG_RECENT_FILES;

            StringBuilder sbFiles = new StringBuilder();

            for (int i = 0; i < RecentFiles.Count; i++)
            {
                sbFiles.Append(itemName + i.ToString() + " = " + RecentFiles.ElementAt(i) + Environment.NewLine);
            }

            return sbFiles.ToString();
        }

        /// <summary>
        /// Called in response to user clicking one of the RecentFile
        /// menu items.  
        /// The index of the file in the list of recent files is encoded
        /// in the name of the manu item e.g. "RecentFiles2" hence the way
        /// the MenuItem's name is processed here.
        /// </summary>
        /// <param name="menuItemName"></param>
        /// <returns></returns>
        public static string GetRecentFile(string menuItemName)
        {
            if (menuItemName.StartsWith(MainWin.MENUITEM_RECENT_FILES_PREFIX))
            {
                try
                {
                    int index = int.Parse(menuItemName.Substring(MainWin.MENUITEM_RECENT_FILES_PREFIX.Length));
                    return RecentFiles[index];
                }
                catch
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Finds engine executable by checking if it is already set
        /// and if not, if we have it in the current directory.
        /// If all fails, asks the user to find it.
        /// </summary>
        /// <returns></returns>
        public static string EngineExecutableFilePath()
        {
            if (!File.Exists(EngineExePath))
            {
                string searchPath = "";
                try
                {
                    searchPath = Path.GetDirectoryName(EngineExePath);
                }
                catch { };
                EngineExePath = "";
                DirectoryInfo info = new DirectoryInfo(".");
                FileInfo[] files = info.GetFiles();

                int latestVer = -1;
                foreach (FileInfo file in files)
                {
                    string fileLowerCase = file.Name.ToLower();
                    if (file.Name.StartsWith("stockfish", StringComparison.OrdinalIgnoreCase) && fileLowerCase.IndexOf(".exe") > 0)
                    {
                        string[] tokens = file.Name.Split('_');
                        if (tokens.Length >= 2)
                        {
                            int ver;
                            if (int.TryParse(tokens[1], out ver))
                            {
                                if (ver > latestVer)
                                {
                                    latestVer = ver;
                                    EngineExePath = file.FullName;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(EngineExePath))
                {
                    EngineExePath = SelectEngineExecutable(searchPath);
                }
            }

            return EngineExePath;
        }

        /// <summary>
        /// Opens a dialog allowing the user to choose engine executable.
        /// If selected, the configuration is saved.
        /// </summary>
        /// <returns></returns>
        public static string SelectEngineExecutable(string searchPath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Chess engine (*.exe)|*.exe";

            if (!string.IsNullOrEmpty(searchPath))
            {
                openFileDialog.InitialDirectory = searchPath;
            }
            else
            {
                openFileDialog.InitialDirectory = "";
            }

            bool? result;
            openFileDialog.InitialDirectory = "";
            result = openFileDialog.ShowDialog();

            if (result == true)
            {
                EngineExePath = openFileDialog.FileName;
                WriteOutConfiguration();
                return EngineExePath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Processes a single line/item from the configuration file.
        /// </summary>
        /// <param name="line"></param>
        private static void ProcessConfigurationItem(string line)
        {
            string[] tokens = line.Split('=');
            if (tokens.Length >= 2)
            {
                string name = tokens[0].Trim();
                string value = tokens[1].Trim();

                if (name.StartsWith(CFG_RECENT_FILES))
                {
                    RecentFiles.Add(value.Trim());
                }
                else
                {
                    switch (name)
                    {
                        case CFG_MOVE_SPEED:
                            int.TryParse(value, out MoveSpeed);
                            break;
                        case CFG_DEBUG_MODE:
                            int.TryParse(value, out DebugLevel);
                            break;
                        case CFG_LAST_DIRECTORY:
                            LastOpenDirectory = value;
                            break;
                        case CFG_LAST_IMPORT_DIRECTORY:
                            LastImportDirectory = value;
                            break;
                        case CFG_LAST_FILE:
                            LastWorkbookFile = value;
                            break;
                        case CFG_ENGINE_EXE:
                            EngineExePath = value;
                            break;
                        case CFG_DO_NOT_SHOW_VERSION:
                            DoNotShowVersion = value;
                            break;
                        case CFG_FONT_SIZE_DIFF:
                            int.TryParse(value, out FontSizeDiff);
                            break;
                        case CFG_AUTO_SAVE_FREQ:
                            int.TryParse(value, out AutoSaveFrequency);
                            break;
                        case CFG_ENGINE_EVALUATION_TIME:
                            int.TryParse(value, out EngineEvaluationTime);
                            break;
                        case CFG_ENGINE_MOVE_TIME:
                            int.TryParse(value, out EngineMoveTime);
                            break;
                        case CFG_ENGINE_MPV:
                            int.TryParse(value, out EngineMpv);
                            break;
                        case CFG_VIABLE_MOVE_CP_DIFF:
                            int.TryParse(value, out ViableMoveCpDiff);
                            // make sure this is not negative
                            ViableMoveCpDiff = Math.Abs(ViableMoveCpDiff);
                            break;
                        case CFG_PGN_EXP_BOOKMARKS:
                            PgnExportBookmarks = value != "0" ? true : false;
                            break;
                        case CFG_PGN_EXP_EVALS:
                            PgnExportEvaluations = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_GENERIC_PGN_INFO:
                            ShowGenericPgnInfo = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_MOVES_AT_FORK:
                            ShowMovesAtFork = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_EXPLORERS:
                            ShowExplorers = value != "0" ? true : false;
                            break;
                        case CFG_MAIN_WIN_MAXIMIZED:
                            MainWinMaximized = value != "0" ? true : false;
                            break;
                        case CFG_AUTO_SAVE:
                            AutoSave = value != "0" ? true : false;
                            break;
                        case CFG_SOUND_ON:
                            SoundOn = value != "0" ? true : false;
                            break;
                        case CFG_USE_FIXED_FONT:
                            UseFixedFont = value != "0" ? true : false;
                            break;
                        case CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES:
                            AllowMouseWheelForMoves = value != "0" ? true : false;
                            break;
                        case CFG_MAIN_WINDOW_POS:
                            string[] sizes = value.Split(',');
                            if (sizes.Length == 4)
                            {
                                try
                                {
                                    MainWinPos.Left = double.Parse(sizes[0]);
                                    MainWinPos.Top = double.Parse(sizes[1]);
                                    MainWinPos.Right = double.Parse(sizes[2]);
                                    MainWinPos.Bottom = double.Parse(sizes[3]);
                                }
                                catch
                                {
                                }
                            }
                            break;
                    }
                }
            }
        }

    }

}
