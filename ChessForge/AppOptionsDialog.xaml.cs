﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AppOptions.xaml
    /// </summary>
    public partial class AppOptionsDialog : Window
    {

        // the code of the language currently configured
        private string _currentConfiguredLanguage = "";

        // UseFigurines value on entry
        private bool _currentUseFigurines;

        // number of threads on entry
        private int _currentEngineThreads;

        // hash table size on entry
        private long _currentEngineHashSize;

        /// <summary>
        /// The language selected upon exit
        /// </summary>
        public string ExitLanguage = "";

        /// <summary>
        /// Set on Exit to indicate whether the selected language
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool LanguageChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether the UseFigurines value
        /// differs from the one configured before the dialog was opened
        /// </summary>
        public bool UseFigurinesChanged = false;

        /// <summary>
        /// Set on Exit to indicate whether engine parameters changed
        /// that require updating engine options.
        /// </summary>
        public bool EngineParamsChanged = false;

        /// <summary>
        /// Configured path to the engine's executable.
        /// </summary>
        public string EnginePath;

        /// <summary>
        /// Configured Replay Speed
        /// </summary>
        public double ReplaySpeed;

        /// <summary>
        /// Configured time for engine to think on move
        /// while in a game.
        /// </summary>
        public double EngineTimePerMoveInGame;

        /// <summary>
        /// Configured time for engine to think on move
        /// while evaluating.
        /// </summary>
        public double EngineTimePerMoveInEvaluation;

        /// <summary>
        /// Number of lines returned by the engine
        /// </summary>
        public double EngineMpv;

        /// <summary>
        /// Tolerance of engine move selection in centipawns
        /// while evaluating.
        /// </summary>
        public int EngineMoveAccuracy;

        /// <summary>
        /// Number of threads for the engine to use.
        /// </summary>
        public int EngineThreads;

        /// <summary>
        /// Hash table size.
        /// </summary>
        public long EngineHashSize;

        /// <summary>
        /// Whether mouse wheel can be used to scroll through the notation..
        /// </summary>
        public bool AllowMouseWheel;

        /// <summary>
        /// Whether "fork tables" are shown.
        /// </summary>
        public bool ShowMovesAtFork;

        /// <summary>
        /// Whether the sound is turned on.
        /// </summary>
        public bool SoundOn;

        /// <summary>
        // Whether the engine path was changed by the user in this dialog.
        /// </summary>
        public bool ChangedEnginePath = false;

        // path to the engine as this dialog is invoked,
        private string _originalEnginePath;

        /// <summary>
        /// Creates the dialog and initializes the controls with
        /// formatted configuration values.
        /// </summary>
        public AppOptionsDialog()
        {
            InitializeComponent();
            EnginePath = Configuration.EngineExePath;
            _originalEnginePath = EnginePath;

            ReplaySpeed = (double)Configuration.MoveSpeed / 1000.0;
            EngineTimePerMoveInGame = (double)Configuration.EngineMoveTime / 1000.0;
            EngineTimePerMoveInEvaluation = (double)Configuration.EngineEvaluationTime / 1000.0;
            EngineMpv = Configuration.EngineMpv;
            EngineMoveAccuracy = (int)Configuration.ViableMoveCpDiff;
            EngineThreads = (int)Configuration.EngineThreads;
            EngineHashSize = (long)Configuration.EngineHashSize;
            AllowMouseWheel = Configuration.AllowMouseWheelForMoves;
            ShowMovesAtFork = Configuration.ShowMovesAtFork;
            SoundOn = Configuration.SoundOn;

            _currentUseFigurines = Configuration.UseFigurines;
            _currentEngineThreads = Configuration.EngineThreads;
            _currentEngineHashSize = Configuration.EngineHashSize;

            UiTbEngineExe.Text = EnginePath;
            UiTbReplaySpeed.Text = ReplaySpeed.ToString("F1");
            UiTbEngTimeInGame.Text = EngineTimePerMoveInGame.ToString("F1");
            UiTbEngEvalTime.Text = EngineTimePerMoveInEvaluation.ToString("F1");
            UiTbMultiPv.Text = EngineMpv.ToString();
            UiTbMoveAcc.Text = EngineMoveAccuracy.ToString();
            UiTbThreads.Text = EngineThreads.ToString();
            UiTbHashSize.Text = EngineHashSize.ToString();
            UiCbAllowWheel.IsChecked = (AllowMouseWheel == true);
            UiCbShowForkMoves.IsChecked = (ShowMovesAtFork == true);
            UiCbSoundOn.IsChecked = (SoundOn == true);
            UiCbFigurines.IsChecked = (Configuration.UseFigurines == true);

            Languages.AvailableLanguages.Sort();
            foreach (Language lang in Languages.AvailableLanguages)
            {
                UiLbLanguages.Items.Add(lang);
                if (lang.IsSelected)
                {
                    _currentConfiguredLanguage = lang.Code;
                    UiLbLanguages.SelectedItem = lang;
                }
            }
        }

        /// <summary>
        /// Invokes the Configuration object's Select Engine dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnLocateEngine_Click(object sender, RoutedEventArgs e)
        {
            string searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            string res = Configuration.SelectEngineExecutable(searchPath);
            if (!string.IsNullOrEmpty(res))
            {
                EnginePath = res;
                UiTbEngineExe.Text = EnginePath;
            }
        }

        /// <summary>
        /// Saves the values in the Configuration object and exits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_originalEnginePath == EnginePath)
            {
                ChangedEnginePath = false;
            }
            else
            {
                ChangedEnginePath = true;
                Configuration.EngineExePath = EnginePath;
            }

            double dval;
            int iVal;
            long lVal;

            if (double.TryParse(UiTbReplaySpeed.Text, out dval))
            {
                Configuration.MoveSpeed = (int)(dval * 1000);
            }

            if (double.TryParse(UiTbEngTimeInGame.Text, out dval))
            {
                Configuration.EngineMoveTime = (int)(dval * 1000);
            }

            if (double.TryParse(UiTbEngEvalTime.Text, out dval))
            {
                Configuration.EngineEvaluationTime = (int)(dval * 1000);
            }

            if (int.TryParse(UiTbMultiPv.Text, out iVal))
            {
                Configuration.EngineMpv = iVal;
            }

            if (int.TryParse(UiTbMoveAcc.Text, out iVal))
            {
                Configuration.ViableMoveCpDiff = iVal;
            }

            if (int.TryParse(UiTbThreads.Text, out iVal))
            {
                Configuration.EngineThreads = iVal;
            }

            if (long.TryParse(UiTbHashSize.Text, out lVal))
            {
                Configuration.EngineHashSize = lVal;
            }

            Configuration.AllowMouseWheelForMoves = (UiCbAllowWheel.IsChecked == true);
            Configuration.ShowMovesAtFork = (UiCbShowForkMoves.IsChecked == true);
            Configuration.SoundOn = (UiCbSoundOn.IsChecked == true);
            Configuration.UseFigurines = (UiCbFigurines.IsChecked == true);

            if (UiLbLanguages.SelectedItem != null)
            {
                ExitLanguage = (UiLbLanguages.SelectedItem as Language).Code;
            }

            if (ExitLanguage != _currentConfiguredLanguage)
            {
                LanguageChanged = true;
            }

            if (_currentUseFigurines != Configuration.UseFigurines)
            {
                UseFigurinesChanged = true;
            }

            if (_currentEngineThreads != Configuration.EngineThreads
                || _currentEngineHashSize != Configuration.EngineHashSize)
            {
                EngineParamsChanged = true;
            }

            DialogResult = true;
        }

        /// <summary>
        /// Exits without saving the values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Application-Options-Dialog");
        }
    }
}
