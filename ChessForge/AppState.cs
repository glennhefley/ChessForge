﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChessForge
{
    /// <summary>
    /// Application wide settings.
    /// </summary>
    public class AppState
    {
        /// <summary>
        /// The program is always in one, and only one, of these modes.
        /// 
        /// Changing between modes requires a number of
        /// steps to be performed, in particular blocking certain 
        /// activities. E.g. going from manual analysis to game replay
        /// requires that the position is set appropriately
        /// and user inputs, other than request to stop analysis
        /// are blocked.
        /// 
        /// </summary>
        public enum Mode : uint
        {
            /// <summary>
            /// No workbook loaded, the program is waiting.
            /// </summary>
            IDLE = 0x0001,              

            /// <summary>
            /// A training session is in progress
            /// </summary>
            TRAINING = 0x0002,          

            /// <summary>
            /// The user is playing against the engine
            /// </summary>
            GAME_VS_COMPUTER = 0x0004,  

            /// <summary>
            /// The user is reviewing the workbook.
            /// Can switch between different lines.
            /// </summary>
            MANUAL_REVIEW = 0x0010,  

            /// <summary>
            /// The program is evaluating a move or a line.
            /// NOTE: this is separate from evaluation during the game
            /// or training which are submodes of the respective modes.
            /// </summary>
            ENGINE_EVALUATION = 0x0020 
        }

        /// <summary>
        /// Within a mode there can be several sub-modes. For example, if a play vs computer is in progress,
        /// a SubMode will indicate whether the user or the computer is on move. 
        /// </summary>
        public enum SubMode : uint
        {
            NONE = 0x0000,
            
            /// <summary>
            /// The program is idle while in Training
            /// or Game mode, awaiting user's move.
            /// </summary>
            USER_THINKING = 0x0001,

            /// <summary>
            /// The program is monitoring
            /// engine's messages awaiting engine's move.
            /// </summary>
            ENGINE_THINKING = 0x0002,
            
            /// <summary>
            /// A selected line from the currently loaded workbook
            /// is being replayed.
            /// </summary>
            GAME_REPLAY = 0x0008,

            /// <summary>
            /// The engine is evaluating position while
            /// in the Training mode.
            /// </summary>
            TRAINING_ENGINE_EVALUATING = 0x0010
        }

        /// <summary>
        /// Main application window.
        /// Exposing the public reference through this object
        /// for convenient access/reference.
        /// </summary>
        public static MainWindow MainWin;

        /// <summary>
        /// Switches application to another mode.
        /// </summary>
        public static void ChangeCurrentMode(AppState.Mode mode)
        {
            TidyUpOnModeExit(_previousMode);

            _previousMode = _currentMode;
            _currentMode = mode;

            MainWin.ConfigureUIForMode(mode);
        }

        /// <summary>
        /// Tidies up what ever necessary when
        /// exiting a mode.
        /// E.g. stoping appropriate timers.
        /// </summary>
        /// <param name="mode"></param>
        private static void TidyUpOnModeExit(AppState.Mode previousMode)
        {
        }

        /// <summary>
        /// Exits the mode the application is currently in
        /// and returns to the previous mode.
        /// </summary>
        public static void ExitCurrentMode()
        {
            ChangeCurrentMode(_previousMode);
        }

        /// <summary>
        /// The current mode of the application.
        /// </summary>
        public static Mode CurrentMode { get => _currentMode; set => _currentMode = value; }

        /// <summary>
        /// The previous mode of the application.
        /// This is applicable when an exit from the current mode is requested.
        /// </summary>
        public static Mode PreviousMode { get => _previousMode; set => _previousMode = value; }

        /// <summary>
        /// Horizontal animation object.
        /// </summary>
        public static DoubleAnimation CurrentAnimationX;

        /// <summary>
        /// Vertical animation object.
        /// </summary>
        public static DoubleAnimation CurrentAnimationY;

        /// <summary>
        /// Animation translation object.
        /// </summary>
        public static TranslateTransform CurrentTranslateTransform;

        /// <summary>
        /// The list of bookmarks.
        /// </summary>
        public static List<BookmarkView> Bookmarks = new List<BookmarkView>();

        /// <summary>
        /// Currently selected line.
        /// There can only be one (or none) line selected in the Workbook at any time
        /// </summary>
        public static string SelectedLine;

        /// <summary>
        /// Currently selected Tree Node (ply) in the Workbook.
        /// There can only be one (or none) node selected in the Workbook at any time
        /// </summary>
        public static int NodeId;

        /// <summary>
        /// Index in the list of bookmarks of the bookmark currently being
        /// active in a training session.
        /// Precisely one bookmark can be active during a session. 
        /// </summary>
        public static int ActiveBookmarkInTraining = -1;

        private static Mode _currentMode = Mode.IDLE;
        private static Mode _previousMode = Mode.IDLE;
    }
}
