﻿using ChessPosition;
using EngineService;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Security.Policy;
using System.Diagnostics;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // prefix use for manu items showing recent files
        public readonly string MENUITEM_RECENT_FILES_PREFIX = "RecentFiles";

        public readonly string APP_NAME = "Chess Forge";

        /// <summary>
        /// The RichTextBox based full Workbook view
        /// </summary>
        private WorkbookView _workbookView;

        /// <summary>
        /// The RichTextBox based view of the lines
        /// starting from the Bookmark position being
        /// trained from.
        /// </summary>
        private WorkbookView _trainingBrowseRichTextBuilder;

        /// <summary>
        /// The RichTextBox based training view
        /// </summary>
        public TrainingView UiTrainingView;

        // width and and height of a square in the main chessboard
        private const int squareSize = 80;

        AnimationState MoveAnimation = new AnimationState();
        public EvaluationManager EvaluationMgr;

        // The main chessboard of the application
        public ChessBoard MainChessBoard;

        /// <summary>
        /// Chessboard shown over moves in different views
        /// </summary>
        public ChessBoard FloatingChessBoard;

        /// <summary>
        /// The RichTextBox based comment box
        /// underneath the main chessbaord.
        /// </summary>
        public CommentBox BoardCommentBox;

        public GameReplay ActiveLineReplay;

        /// <summary>
        /// manages data for the ActiveLine DataGrid
        /// </summary>
        public ActiveLineManager ActiveLine;

        /// <summary>
        /// The complete tree of the currently
        /// loaded workbook (from the PGN or CHF file)
        /// </summary>
        public WorkbookTree Workbook;

        /// <summary>
        /// Determines if the program is running in Debug mode.
        /// </summary>
        private bool _isDebugMode = false;

        /// <summary>
        /// Coordinates of the last right-clicked point
        /// </summary>
        private Point? _lastRightClickedPoint;

        /// <summary>
        /// Collection of timers for this application.
        /// </summary>
        public AppTimers Timers;

        /// <summary>
        /// The main application window.
        /// Initializes the GUI controls.
        /// Note that some of the controls must be initialized
        /// in a particular order as one control may use a reference 
        /// to another one.
        /// </summary>
        public MainWindow()
        {
            AppStateManager.MainWin = this;

            EvaluationMgr = new EvaluationManager();

            InitializeComponent();
            SoundPlayer.Initialize();

            BoardCommentBox = new CommentBox(UiRtbBoardComment.Document, this);
            ActiveLine = new ActiveLineManager(UiDgActiveLine, this);

            EngineLinesBox.Initialize(this, UiTbEngineLines, UiPbEngineThinking);
            Timers = new AppTimers(this);

            Configuration.Initialize(this);
            Configuration.StartDirectory = App.AppPath;
            Configuration.ReadConfigurationFile();
            if (Configuration.IsMainWinPosValid())
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Right - Configuration.MainWinPos.Left;
                this.Height = Configuration.MainWinPos.Bottom - Configuration.MainWinPos.Top;
            }

            // main chess board
            MainChessBoard = new ChessBoard(MainCanvas, UiImgMainChessboard, null, true);
            FloatingChessBoard = new ChessBoard(_cnvFloat, _imgFloatingBoard, null, true);

            BookmarkManager.InitBookmarksGui(this);

            ActiveLineReplay = new GameReplay(this, MainChessBoard, BoardCommentBox);

            _isDebugMode = Configuration.DebugMode != 0;
        }

        /// <summary>
        /// Actions taken after the main window
        /// has been loaded.
        /// In particular, if the last used file can be identified
        /// it will be read in and the session initrialized with it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UiDgActiveLine.ContextMenu = UiMnMainBoard;
            AddDebugMenu();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE);
            AppStateManager.SetupGuiForCurrentStates();

            Timers.Start(AppTimers.TimerId.APP_START);
        }

        [Conditional("DEBUG")]
        private void AddDebugMenu()
        {
            MenuItem mnDebug = new MenuItem
            {
                Name = "DebugMenu"
            };

            mnDebug.Header = "Debug";
            UiMainMenu.Items.Add(mnDebug);

            MenuItem mnDebugDump = new MenuItem
            {
                Name = "DebugDumpMenu"
            };

            mnDebugDump.Header = "Dump All";
            mnDebug.Items.Add(mnDebugDump);
            mnDebugDump.Click += UiMnDebugDump_Click;

            MenuItem mnDebugDumpStates = new MenuItem
            {
                Name = "DebugDumpStates"
            };

            mnDebugDumpStates.Header = "Dump States and Timers";
            mnDebug.Items.Add(mnDebugDumpStates);
            mnDebugDumpStates.Click += UiMnDebugDumpStates_Click;
        }

        // tracks the application start stage
        private int _appStartStage = 0;

        // lock object to use during the startup process
        private object _appStartLock = new object();

        /// <summary>
        /// This method controls the two important stages of the startup process.
        /// When the Appstart timer invokes it for the first time, the engine
        /// will be loaded while the timer is stopped.
        /// The second time it is invoked, it will read the most recent file
        /// if such file exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void AppStartTimeUp(object source, ElapsedEventArgs e)
        {
            lock (_appStartLock)
            {

                if (_appStartStage == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        BoardCommentBox.StartingEngine();
                    });
                    _appStartStage = 1;
                    Timers.Stop(AppTimers.TimerId.APP_START);
                    EngineMessageProcessor.CreateEngineService(this, _isDebugMode);
                    Timers.Start(AppTimers.TimerId.APP_START);
                }
                else if (_appStartStage == 1)
                {
                    _appStartStage = 2;
                    this.Dispatcher.Invoke(() =>
                    {

                        CreateRecentFilesMenuItems();
                        Timers.Stop(AppTimers.TimerId.APP_START);
                        bool engineStarted = EngineMessageProcessor.StartEngineService();
                        Timers.Start(AppTimers.TimerId.APP_START);
                        if (!engineStarted)
                        {
                            MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // if we have LastWorkbookFile or a name on the commend line
                        // we will try to open
                        string cmdLineFile = App.CmdLineFileName;
                        bool success = false;
                        if (!string.IsNullOrEmpty(cmdLineFile))
                        {
                            try
                            {
                                ReadWorkbookFile(cmdLineFile, true);
                                success = true;
                            }
                            catch
                            {
                                success = false;
                            }
                        }

                        if (!success)
                        {
                            string lastWorkbookFile = Configuration.LastWorkbookFile;

                            if (!string.IsNullOrEmpty(lastWorkbookFile))
                            {
                                try
                                {
                                    ReadWorkbookFile(lastWorkbookFile, true);
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                BoardCommentBox.OpenFile();
                            }
                        }
                    });
                }
            }

            if (_appStartStage == 2)
            {
                Timers.Stop(AppTimers.TimerId.APP_START);
            }
        }

        /// <summary>
        /// Creates menu items for the Recent Files and 
        /// adds them to the File menu.
        /// </summary>
        private void CreateRecentFilesMenuItems()
        {
            List<string> recentFiles = Configuration.RecentFiles;
            for (int i = 0; i < recentFiles.Count; i++)
            {
                MenuItem mi = new MenuItem
                {
                    Name = MENUITEM_RECENT_FILES_PREFIX + i.ToString()
                };
                try
                {
                    string fileName = Path.GetFileName(recentFiles.ElementAt(i));
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        mi.Header = fileName;
                        MenuFile.Items.Add(mi);
                        mi.Click += OpenRecentWorkbookFile;
                    }
                }
                catch { };
            }
        }

        /// <summary>
        /// Returns the flipped state of the Main Chessboard
        /// </summary>
        /// <returns></returns>
        public bool IsMainChessboardFlipped()
        {
            return MainChessBoard.IsFlipped;
        }

        /// <summary>
        /// Determined the color of the arrow to be drawn based
        /// on the special key pressed and calls to BoardArrowsManager
        /// to do the drawing.
        /// </summary>
        /// <param name="sq"></param>
        private void StartShapeDraw(SquareCoords sq, bool isTentative)
        {
            string color = "yellow";

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                color = Constants.COLOR_YELLOW;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                color = Constants.COLOR_RED;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                color = Constants.COLOR_BLUE;
            }
            else
            {
                color = Constants.COLOR_GREEN;
            }

            BoardShapesManager.StartShapeDraw(sq, color, isTentative);
        }

        /// <summary>
        /// Saves the Arrow positions string to the Node currently
        /// hosted in the Main Chessboard.
        /// </summary>
        /// <param name="arrowsString"></param>
        /// <return>whether the new string is different</return>
        public bool SaveArrowsStringInCurrentNode(string arrowsString)
        {
            if (MainChessBoard != null)
            {
                TreeNode nd = MainChessBoard.DisplayedNode;
                if (nd != null && nd.Arrows != arrowsString)
                {
                    nd.Arrows = arrowsString;
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
        /// Saves the Circle positions string to the Node currently
        /// hosted in the Main Chessboard.
        /// </summary>
        /// <param name="circlesString"></param>
        public bool SaveCirclesStringInCurrentNode(string circlesString)
        {
            if (MainChessBoard != null)
            {
                TreeNode nd = MainChessBoard.DisplayedNode;
                if (nd != null && nd.Circles != circlesString)
                {
                    nd.Circles = circlesString;
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
        /// Checks if the clicked piece is eleigible for making a move.
        /// </summary>
        /// <param name="sqNorm"></param>
        /// <returns></returns>
        private bool CanMovePiece(SquareCoords sqNorm)
        {
            PieceColor pieceColor = MainChessBoard.GetPieceColor(sqNorm);

            // in the Manual Review, the color of the piece on the main board must match the side on the move in the selected position
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                if (nd == null)
                {
                    nd = Workbook.Nodes[0];
                }

                if (pieceColor != PieceColor.None && pieceColor == nd.ColorToMove)
                    return true;
                else
                    return false;
            }
            else if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingSession.CurrentState == TrainingSession.State.AWAITING_USER_TRAINING_MOVE && !TrainingSession.IsBrowseActive)
            {
                if (EngineGame.GetPieceColor(sqNorm) == EngineGame.ColorToMove)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Shows a GUI element allowing the user 
        /// to select the piece to promote to.
        /// </summary>
        /// <param name="normTarget">Normalized propmotion square coordinates
        /// i.e. 0 is for Black and 7 is for White promotion.</param>
        /// <returns></returns>
        public PieceType GetUserPromoSelection(SquareCoords normTarget)
        {
            bool whitePromotion = normTarget.Ycoord == 7;
            PromotionDialog dlg = new PromotionDialog(whitePromotion);

            Point pos = CalculatePromoDialogLocation(normTarget, whitePromotion);
            dlg.Left = pos.X;
            dlg.Top = pos.Y;
            dlg.ShowDialog();

            return dlg.SelectedPiece;
        }

        /// <summary>
        /// Given the promotion square in the normalized
        /// form (i.e. ignoring a possible chessboard flip),
        /// works out the Left and Top position of the Promotion
        /// dialog.
        /// The dialog should fit entirely within the board and its boarders and should
        /// nicely overlap with the promotion square.
        /// </summary>
        /// <param name="normTarget"></param>
        /// <returns></returns>
        private Point CalculatePromoDialogLocation(SquareCoords normTarget, bool whitePromotion)
        {
            //TODO: this is far from ideal.
            // We need to find a better way of calulating the position against
            // the chessboard
            Point leftTop = new Point();
            if (!MainChessBoard.IsFlipped)
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + normTarget.Xcoord * 80;
                if (whitePromotion)
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (7 - normTarget.Ycoord) * 80;
                }
                else
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (3 - normTarget.Ycoord) * 80;
                }
            }
            else
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + (7 - normTarget.Xcoord) * 80;
                if (whitePromotion)
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord - 4) * 80;
                }
                else
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord) * 80;
                }
            }

            return leftTop;
        }

        /// <summary>
        /// Completes a castling move. King would have already been moved.
        /// </summary>
        /// <param name="move"></param>
        public void MoveCastlingRook(string move)
        {
            SquareCoords orig = null;
            SquareCoords dest = null;
            switch (move)
            {
                case "e1g1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 0) : new SquareCoords(0, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 0) : new SquareCoords(2, 7);
                    break;
                case "e8g8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 7) : new SquareCoords(0, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 7) : new SquareCoords(2, 0);
                    break;
                case "e1c1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 0) : new SquareCoords(7, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 0) : new SquareCoords(4, 7);
                    break;
                case "e8c8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 7) : new SquareCoords(7, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 7) : new SquareCoords(4, 0);
                    break;
            }

            MovePiece(orig, dest);
        }

        /// <summary>
        /// Moving a piece from square to square.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        private void MovePiece(SquareCoords orig, SquareCoords dest)
        {
            if (orig == null || dest == null)
                return;

            MainChessBoard.GetPieceImage(dest.Xcoord, dest.Ycoord, true).Source = MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source;
            MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source = null;
        }

        /// <summary>
        /// Returns the dragged piece's Image control to
        /// the square it started from.
        /// If clearImage == true, the image in the control
        /// will be cleared (e.g. because the move was successfully
        /// executed and the image has been transferred to the control
        /// on the target square.
        /// </summary>
        /// <param name="clearImage"></param>
        public void ReturnDraggedPiece(bool clearImage)
        {
            if (clearImage)
            {
                DraggedPiece.ImageControl.Source = null;
            }
            Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
            Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
        }

        /// <summary>
        /// Move animation requested as part of auto-replay.
        /// As such we need to flip the coordinates if
        /// the board is flipped.
        /// </summary>
        /// <param name="move"></param>
        public void RequestMoveAnimation(MoveUI move)
        {
            SquareCoords origin = MainChessBoard.FlipCoords(move.Origin);
            SquareCoords destination = MainChessBoard.FlipCoords(move.Destination);
            AnimateMove(origin, destination);
        }

        /// <summary>
        /// Caller must handle a possible flipped stated of the board.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        private void AnimateMove(SquareCoords origin, SquareCoords destination)
        {
            // caller already accounted for a possible flipped board so call with ignoreFlip = true
            Image img = MainChessBoard.GetPieceImage(origin.Xcoord, origin.Ycoord, true);
            MoveAnimation.Piece = img;
            MoveAnimation.Origin = origin;
            MoveAnimation.Destination = destination;

            Canvas.SetZIndex(img, Constants.ZIndex_PieceInAnimation);

            Point orig = MainChessBoardUtils.GetSquareTopLeftPoint(origin);
            Point dest = MainChessBoardUtils.GetSquareTopLeftPoint(destination);

            TranslateTransform trans = new TranslateTransform();
            if (img.RenderTransform != null)
                img.RenderTransform = trans;

            DoubleAnimation animX = new DoubleAnimation(0, dest.X - orig.X, TimeSpan.FromMilliseconds(Configuration.MoveSpeed));
            DoubleAnimation animY = new DoubleAnimation(0, dest.Y - orig.Y, TimeSpan.FromMilliseconds(Configuration.MoveSpeed));

            LearningMode.CurrentTranslateTransform = trans;
            LearningMode.CurrentAnimationX = animX;
            LearningMode.CurrentAnimationY = animY;

            animX.Completed += new EventHandler(MoveAnimationCompleted);
            trans.BeginAnimation(TranslateTransform.XProperty, animX);
            trans.BeginAnimation(TranslateTransform.YProperty, animY);

        }

        /// <summary>
        /// Stops move animation if there is one in progress.
        /// </summary>
        public void StopMoveAnimation()
        {
            // TODO Apparently, there are 2 methods to stop animation.
            // Method 1 below keeps the animated image at the spot it was when the stop request came.
            // Method 2 returns it to the initial position.
            // Neither works fully to our satisfaction. They seem to not be exiting immediately and are leaving some garbage
            // behind which prevents us from immediatey changing the speed of animation on user's request 
            if (LearningMode.CurrentAnimationX != null && LearningMode.CurrentAnimationY != null && LearningMode.CurrentTranslateTransform != null)
            {
                // *** Method 1.
                //AppState.CurrentAnimationX.BeginTime = null;
                //AppState.CurrentAnimationY.BeginTime = null;
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, AppState.CurrentAnimationX);
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, AppState.CurrentAnimationY);

                // *** Method 2.
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
        }

        /// <summary>
        /// Called when animation completes.
        /// The coords saved in the MoveAnimation object
        /// are absolute as a possible flipped state of the board was
        /// taken into account at the start fo the animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveAnimationCompleted(object sender, EventArgs e)
        {
            LearningMode.CurrentTranslateTransform = null;
            LearningMode.CurrentAnimationX = null;
            LearningMode.CurrentAnimationY = null;

            MainChessBoard.GetPieceImage(MoveAnimation.Destination.Xcoord, MoveAnimation.Destination.Ycoord, true).Source = MoveAnimation.Piece.Source;

            Point orig = MainChessBoardUtils.GetSquareTopLeftPoint(MoveAnimation.Origin);
            //_pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].Source = AnimationPiece.Source;

            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.X);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.Y);

            //TODO: there should be a better way than having to recreate the image control.
            //   but it seems the image would no longer show (tested when not removing
            //   the image from the origin square, the image won't show seemingly due to
            // RenderTransfrom being set.)
            //
            // This seems to work but re-shows the last moved piece on its origin square???
            // _pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].RenderTransform = null;
            //

            Image old = MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Remove(old);
            MainChessBoard.SetPieceImage(new Image(), MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Add(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true));
            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * MoveAnimation.Origin.Xcoord + UiImgMainChessboard.Margin.Left);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * (7 - MoveAnimation.Origin.Ycoord) + UiImgMainChessboard.Margin.Top);

            ActiveLineReplay.PrepareNextMoveForAnimation(ActiveLineReplay.LastAnimatedMoveIndex, false);
        }

        /// <summary>
        /// Invoked from the menu item File->Close Workbook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCloseWorkbook_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.AskToSaveWorkbook();
        }

        /// <summary>
        /// Returns true if user accept the change. of mode.
        /// </summary>
        /// <param name="newMode"></param>
        /// <returns></returns>
        private bool ChangeAppModeWarning(LearningMode.Mode newMode)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.IDLE)
            {
                // it is a fresh state, no need for any warnings
                return true;
            }

            bool result = false;
            // we may not be changing the mode, but changing
            // the variation tree we are working with.
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                // TODO: ask what to do with the current tree
                // abandon, save, put aside
                result = true;
            }
            else if (LearningMode.CurrentMode != LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                switch (LearningMode.CurrentMode)
                {
                    case LearningMode.Mode.ENGINE_GAME:
                        if (MessageBox.Show("Cancel Game?", "Game with the Computer is in Progress", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            result = true;
                        break;
                    default:
                        result = true;
                        break;
                }
            }
            else
            {
                return true;
            }

            return result;
        }

        /// <summary>
        /// Recreates the "Recent Files" menu items by
        /// removing the exisiting ones and inserting
        /// ones corresponding to what's in the configuration file.
        /// </summary>
        public void RecreateRecentFilesMenuItems()
        {
            List<object> itemsToRemove = new List<object>();

            for (int i = 0; i < MenuFile.Items.Count; i++)
            {
                if (MenuFile.Items[i] is MenuItem item)
                {
                    if (item.Name.StartsWith(MENUITEM_RECENT_FILES_PREFIX))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (MenuItem item in itemsToRemove.Cast<MenuItem>())
            {
                MenuFile.Items.Remove(item);
            }

            CreateRecentFilesMenuItems();
        }

        /// <summary>
        /// Reads in the file and builds internal Variation Tree
        /// structures for the entire content.
        /// The Chess Forge file will have a .chf extension.
        /// However, we also read .pgn files (with the intent
        /// to save them as .chf files)
        /// </summary>
        /// <param name="fileName"></param>
        private async void ReadWorkbookFile(string fileName, bool isLastOpen)
        {
            try
            {
                if (!WorkbookManager.CheckFileExists(fileName, isLastOpen))
                {
                    return;
                }

                AppStateManager.RestartInIdleMode(false);

                // WorkbookFilePath is reset to "" in the above call!
                AppStateManager.WorkbookFilePath = fileName;

                bool isOrigPgn = false;
                if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
                {
                    isOrigPgn = true;
                }

                await Task.Run(() =>
                {
                    BoardCommentBox.ReadingFile();
                });

                AppStateManager.WorkbookFilePath = fileName;
                AppStateManager.UpdateAppTitleBar();

                Workbook = new WorkbookTree();
                BookmarkManager.ClearBookmarksGui();
                UiRtbWorkbookView.Document.Blocks.Clear();

                if (AppStateManager.WorkbookFileType == AppStateManager.FileType.CHF)
                {
                    string workbookText = File.ReadAllText(fileName);
                    try
                    {
                        PgnGameParser pgnGame = new PgnGameParser(workbookText, Workbook, out bool isMulti, true);
                    }
                    catch
                    {
                        MessageBox.Show("Error processing the Workbook.", "CHF File", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    int gameCount = WorkbookManager.ReadPgnFile(fileName);
                    if (gameCount == 0)
                    {
                        MessageBox.Show("No games processed.", "Input File", MessageBoxButton.OK, MessageBoxImage.Error);
                        AppStateManager.WorkbookFilePath = "";
                        AppStateManager.UpdateAppTitleBar();
                        return;
                    }
                }

                BoardCommentBox.ShowWorkbookTitle();

                if (Workbook.TrainingSide == PieceColor.None)
                {
                    ShowWorkbookOptionsDialog();
                }

                if (Workbook.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped || Workbook.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }

                // If this is not a CHF file, ask the user to save the converted file.
                bool recentFilesProcessed = false;
                if (AppStateManager.WorkbookFileType != AppStateManager.FileType.CHF)
                {
                    if (WorkbookManager.SaveWorkbookToNewFile(fileName, true))
                    {
                        recentFilesProcessed = true;
                    }
                }

                if (!recentFilesProcessed)
                {
                    WorkbookManager.UpdateRecentFilesList(fileName);
                }

                BoardCommentBox.ShowWorkbookTitle();

                _workbookView = new WorkbookView(UiRtbWorkbookView.Document, this);
                _trainingBrowseRichTextBuilder = new WorkbookView(UiRtbTrainingBrowse.Document, this);
                if (Workbook.Nodes.Count == 0)
                {
                    Workbook.CreateNew();
                }
                else
                {
                    Workbook.BuildLines();
                }
                UiTabWorkbook.Focus();

                _workbookView.BuildFlowDocumentForWorkbook();
                if (Workbook.Bookmarks.Count == 0 && isOrigPgn)
                {
                    var res = AskToGenerateBookmarks();
                    if (res == MessageBoxResult.Yes)
                    {
                        Workbook.GenerateBookmarks();
                        UiTabBookmarks.Focus();
                        AppStateManager.IsDirty = true;
                    }
                }

                //TreeNode firstNode = Workbook.GetFirstNodeInMainLine();
                //int startingNode = firstNode == null ? 0 : firstNode.NodeId;
                //string startLineId = Workbook.GetDefaultLineIdForNode(startingNode);
                string startLineId = Workbook.GetDefaultLineIdForNode(0);
                SetActiveLine(startLineId, 0);
                UiRtbWorkbookView.Focus();

                SetupDataInTreeView();

                BookmarkManager.ShowBookmarks();

                SelectLineAndMoveInWorkbookViews(startLineId, 0); // ActiveLine.GetSelectedPlyNodeIndex());

                LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error processing input file", MessageBoxButton.OK, MessageBoxImage.Error);
                AppStateManager.RestartInIdleMode();
            }
        }

        /// <summary>
        /// Rebuilds the entire Workbook View
        /// </summary>
        public void RebuildWorkbookView()
        {
            _workbookView.BuildFlowDocumentForWorkbook();
        }

        /// <summary>
        /// Obtains the current ActiveLine's LineId and move,
        /// and asks other view to select / re-select.
        /// This is needed e.g. when the WorkbookTree is rebuilt after
        /// adding nodes.
        /// </summary>
        public void RefreshSelectedActiveLineAndNode()
        {
            string lineId = ActiveLine.GetLineId();
            SelectLineAndMoveInWorkbookViews(lineId, ActiveLine.GetSelectedPlyNodeIndex(true));
        }

        /// <summary>
        /// Adds a new Node to the Workbook View,
        /// avoiding the full rebuild (performance).
        /// This can only be done "safely" if we are adding a move to a leaf.
        /// </summary>
        /// <param name="nd"></param>
        public void AddNewNodeToWorkbookView(TreeNode nd)
        {
            _workbookView.AddNewNode(nd);
        }

        public void SelectLineAndMoveInWorkbookViews(string lineId, int index)
        {
            TreeNode nd = ActiveLine.GetNodeAtIndex(index);
            _workbookView.SelectLineAndMove(lineId, nd.NodeId);
            //            _lvWorkbookTable_SelectLineAndMove(lineId, nd.NodeId);
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
            {
                EvaluateActiveLineSelectedPosition(nd);
            }
        }

        private MessageBoxResult AskToGenerateBookmarks()
        {
            return MessageBox.Show("Would you like to auto-select positions for training?",
                "No Bookmarks in this Workbook", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public void SetActiveLine(string lineId, int selectedNodeId, bool displayPosition = true)
        {
            ObservableCollection<TreeNode> line = Workbook.SelectLine(lineId);
            SetActiveLine(line, selectedNodeId, displayPosition);
        }

        /// <summary>
        /// Displays the position of the passed node
        /// and any associated arrows or circles.
        /// </summary>
        /// <param name="nd"></param>
        public void DisplayPosition(TreeNode nd)
        {
            MainChessBoard.DisplayPosition(nd);
            //            BoardArrowsManager.Reset(nd.Arrows);
        }

        /// <summary>
        /// Displays the passed position.
        /// Will not show arrows and circles if associated with this position.
        /// </summary>
        /// <param name="nd"></param>
        public void DisplayPosition(BoardPosition pos)
        {
            MainChessBoard.DisplayPosition(null, pos);
        }

        public void RemoveMoveSquareColors()
        {
            MainChessBoard.RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets data and selection for the Active Line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="selectedNodeId"></param>
        /// <param name="displayPosition"></param>
        public void SetActiveLine(ObservableCollection<TreeNode> line, int selectedNodeId, bool displayPosition = true)
        {
            ActiveLine.SetNodeList(line);

            if (selectedNodeId >= 0)
            {
                TreeNode nd = ActiveLine.GetNodeFromId(selectedNodeId);
                if (selectedNodeId > 0)
                {
                    ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                }
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd);
                }
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
                {
                    EvaluateActiveLineSelectedPosition(nd);
                }
            }
        }

        /// <summary>
        /// Appends a new node to the Active Line.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="displayPosition"></param>
        public void AppendNodeToActiveLine(TreeNode nd, bool displayPosition = true)
        {
            if (nd.NodeId > 0)
            {
                ActiveLine.Line.AddPlyAndMove(nd);
                ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd);
                }
            }
        }

        /// <summary>
        /// Writes out all logs.
        /// If userRequested == true, this was requested via the menu
        /// and we dump everything with distinct file names.
        /// Otherwise we only dump app and engine logs, ovewriting previous
        /// logs.
        /// </summary>
        /// <param name="userRequested"></param>
        public void DumpDebugLogs(bool userRequested)
        {
            string distinct = null;

            if (userRequested)
            {
                distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                AppLog.DumpWorkbookTree(DebugUtils.BuildLogFileName(App.AppPath, "wktree", distinct), Workbook);
                AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
            }

            try
            {
                AppLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "applog", distinct));
                EngineLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "engine", distinct));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dump logs exception: " + ex.Message, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }


        public void DumpDebugStates()
        {
            string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
        }

        private void EvaluateActiveLineSelectedPosition()
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd == null)
            {
                nd = Workbook.Nodes[0];
            }
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            // stop the timer to prevent showing garbage after position is set but engine has not received our commands yet
            EngineMessageProcessor.RequestPositionEvaluation(nd, Configuration.EngineMpv, 0);
        }

        private void EvaluateActiveLineSelectedPosition(TreeNode nd)
        {
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            EngineMessageProcessor.RequestPositionEvaluation(nd, Configuration.EngineMpv, 0);
        }

        public void UpdateLastMoveTextBox(TreeNode nd)
        {
            string moveTxt = MoveUtils.BuildSingleMoveText(nd, true);

            UpdateLastMoveTextBox(moveTxt);
        }

        public void UpdateLastMoveTextBox(int posIndex)
        {
            string moveTxt = EvaluationManager.GetEvaluatedNode().Position.MoveNumber.ToString()
                    + (EvaluationManager.GetEvaluatedNode().Position.ColorToMove == PieceColor.Black ? "." : "...")
                    + ActiveLine.GetNodeAtIndex(posIndex).LastMoveAlgebraicNotation;

            UpdateLastMoveTextBox(moveTxt);
        }

        /// <summary>
        /// Sets text for the label showing the last/current
        /// move (depending on the context it can be e.g. the move being evaluated).
        /// </summary>
        /// <param name="moveTxt"></param>
        public void UpdateLastMoveTextBox(string moveTxt)
        {
            UiLblMoveUnderEval.Dispatcher.Invoke(() =>
            {
                UiLblMoveUnderEval.Content = moveTxt;
            });
        }

        public void ResetEvaluationProgressBar()
        {
            EngineLinesBox.ResetEvaluationProgressBar();
        }

        /// <summary>
        /// If in training mode, we want to keep the evaluation lines
        /// visible in the comment box, and display the response moves
        /// with their line evaluations in the Training tab.
        /// </summary>
        public void MoveEvaluationFinishedInTraining(TreeNode nd)
        {
            AppStateManager.ShowMoveEvaluationControls(false, true);
            UiTrainingView.ShowEvaluationResult(nd);
        }

        /// <summary>
        /// This method will start a game vs the engine.
        /// It will be called in one of two possible contexts:
        /// either the game was requested from MANUAL_REVIEW
        /// or during TRAINING.
        /// If the latter, then the EngineGame has already been
        /// constructed and we start from the last move/ply.
        /// </summary>
        /// <param name="startNode"></param>
        public void StartEngineGame(TreeNode startNode, bool IsTraining)
        {
            UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);

            // TODO: should make a call to SetupGUI for game, instead
            AppStateManager.ShowMoveEvaluationControls(false, false);

            EngineGame.InitializeGameObject(startNode, true, IsTraining);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;

            if (startNode.ColorToMove == PieceColor.White)
            {
                if (!MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }

            EngineMessageProcessor.RequestEngineMove(startNode.Position);
        }

        /// <summary>
        /// This method will be invoked periodically by the 
        /// timer checking for the completion of user moves.
        /// The user can make moves in 2 contexts:
        /// 1. a game against the engine (in this case EngineGame.State 
        /// should already be set to ENGINE_THINKING)
        /// 2. a user entered the move as part of training and we will
        /// provide them a feedback based on the content of the workbook.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void CheckForUserMoveTimerEvent(object source, ElapsedEventArgs e)
        {
            if (TrainingSession.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME)
            {
                if ((TrainingSession.CurrentState == TrainingSession.State.USER_MOVE_COMPLETED))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                        UiTrainingView.ReportLastMoveVsWorkbook();
                    });
                }
            }
            else // this is a game user vs engine then
            {
                // check if the user move was completed and if so request engine move
                if (EngineGame.CurrentState == EngineGame.GameState.ENGINE_THINKING)
                {
                    Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                    EngineMessageProcessor.RequestEngineMove(EngineGame.GetLastPosition());
                }
            }
        }

        /// <summary>
        /// Reset controls and restore selection in the ActiveLine
        /// control.
        /// We are going back to the MANUAL REVIEW mode
        /// so Active Line view will be shown.
        /// </summary>
        public void StopEngineGame()
        {
            Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

            ResetEvaluationProgressBae();

            MainChessBoard.RemoveMoveSquareColors();

            EvaluationManager.Reset();
            EngineMessageProcessor.StopEngineEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);

            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            AppStateManager.MainWin.Workbook.BuildLines();
            RebuildWorkbookView();

            AppStateManager.SetupGuiForCurrentStates();

            ActiveLine.DisplayPositionForSelectedCell();
            AppStateManager.SwapCommentBoxForEngineLines(false);
            BoardCommentBox.RestoreTitleMessage();
        }

        /// <summary>
        /// Resets the engine evaluation progress bar.
        /// Sets its visibility to hidden.
        /// and Maximum value to the appropriate engine time: move or evaluation.
        /// </summary>
        public void ResetEvaluationProgressBae()
        {
            UiPbEngineThinking.Dispatcher.Invoke(() =>
            {
                UiPbEngineThinking.Visibility = Visibility.Hidden;
                UiPbEngineThinking.Minimum = 0;

                int moveTime = AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                    Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
                UiPbEngineThinking.Maximum = moveTime;
                UiPbEngineThinking.Value = 0;
            });

        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbWorkbookFull_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Hand it off to the ActiveLine view.
            // In the future we may want to handle some key strokes here
            // but for now we will respond to whatever the ActiveLine view will request.
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Starts a training session from the specified bookmark position.
        /// </summary>
        /// <param name="bookmarkIndex"></param>
        public void SetAppInTrainingMode(int bookmarkIndex)
        {
            if (bookmarkIndex >= Workbook.Bookmarks.Count)
            {
                return;
            }

            TreeNode startNode = Workbook.Bookmarks[bookmarkIndex].Node;
            SetAppInTrainingMode(startNode);

        }

        /// <summary>
        /// Starts a training session from the specified Node.
        /// </summary>
        /// <param name="startNode"></param>
        public void SetAppInTrainingMode(TreeNode startNode)
        {
            // Set up the training mode
            StopEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            TrainingSession.IsTrainingInProgress = true;
            TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

            LearningMode.TrainingSide = startNode.ColorToMove;
            MainChessBoard.DisplayPosition(startNode);

            _trainingBrowseRichTextBuilder.BuildFlowDocumentForWorkbook(startNode.NodeId);

            UiTrainingView = new TrainingView(UiRtbTrainingProgress.Document, this);
            UiTrainingView.Initialize(startNode);

            if (LearningMode.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped
                || LearningMode.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped)
            {
                MainChessBoard.FlipBoard();
            }

            AppStateManager.ShowMoveEvaluationControls(false, false);
            BoardCommentBox.TrainingSessionStart();

            // The Line display is the same as when playing a game against the computer 
            EngineGame.InitializeGameObject(startNode, false, false);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
        }

        public void InvokeRequestWorkbookResponse(object source, ElapsedEventArgs e)
        {
            UiTrainingView.RequestWorkbookResponse();
        }

        public void ShowTrainingProgressPopupMenu(object source, ElapsedEventArgs e)
        {
            UiTrainingView.ShowPopupMenu();
        }

        public void FlashAnnouncementTimeUp(object source, ElapsedEventArgs e)
        {
            BoardCommentBox.HideFlashAnnouncement();
        }

        public void ShowFloatingChessboard(bool visible)
        {
            this.Dispatcher.Invoke(() =>
            {
                UiVbFloatingChessboard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// The user pressed a key to be handled by Active Line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewActiveLine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Auto-replays the current Active Line on a menu request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_ReplayLine(object sender, RoutedEventArgs e)
        {
            ActiveLine.ReplayLine(0);
        }

        /// <summary>
        /// Advise the Training View that the engine made a move
        /// while playing a training game against the user.
        /// </summary>
        public void EngineTrainingGameMoveMade()
        {
            this.Dispatcher.Invoke(() =>
            {
                UiTrainingView.EngineMoveMade();
            });
        }

        /// <summary>
        /// Shade the "from" and "to" squares of the passed move.
        /// </summary>
        /// <param name="engCode"></param>
        public void ColorMoveSquares(string engCode)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainChessBoard.RemoveMoveSquareColors();

                MoveUtils.EngineNotationToCoords(engCode, out SquareCoords sqOrig, out SquareCoords sqDest);
                MainChessBoard.ColorMoveSquare(sqOrig.Xcoord, sqOrig.Ycoord, true);
                MainChessBoard.ColorMoveSquare(sqDest.Xcoord, sqDest.Ycoord, false);
            });
        }

        /// <summary>
        /// Stops and restarts the engine.
        /// </summary>
        /// <returns></returns>
        public bool ReloadEngine()
        {
            EngineMessageProcessor.StopEngineService();
            EngineMessageProcessor.CreateEngineService(this, _isDebugMode);

            bool engineStarted = EngineMessageProcessor.StartEngineService();
            if (!engineStarted)
            {
                MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Shows the Workbook options dialog.
        /// </summary>
        /// <returns></returns>
        private bool ShowWorkbookOptionsDialog()
        {
            WorkbookOptionsDialog dlg = new WorkbookOptionsDialog(Workbook)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = true
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                Workbook.TrainingSide = dlg.TrainingSide;
                Workbook.Title = dlg.WorkbookTitle;
                AppStateManager.SaveWorkbookFile();
                MainChessBoard.FlipBoard(Workbook.TrainingSide);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Shows the Application Options dialog.
        /// </summary>
        private void ShowApplicationOptionsDialog()
        {
            AppOptionsDialog dlg = new AppOptionsDialog
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = true
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                if (dlg.ChangedEnginePath)
                    Configuration.WriteOutConfiguration();
                if (dlg.ChangedEnginePath)
                {
                    ReloadEngine();
                }
            }
        }

        /// <summary>
        /// Stops any evaluation that is currently happening.
        /// Resets evaluation state and adjusts the GUI accordingly. 
        /// </summary>
        public void StopEvaluation()
        {
            EngineMessageProcessor.StopEngineEvaluation();

            EvaluationManager.Reset();
            AppStateManager.ResetEvaluationControls();
            AppStateManager.ShowMoveEvaluationControls(false, true);
            AppStateManager.SetupGuiForCurrentStates();

            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                Timers.StopAll();
            }
        }

        /// <summary>
        /// Invokes the Move Assessment dialog.
        /// </summary>
        /// <param name="nd"></param>
        public void InvokeAssessmentDialog(TreeNode nd)
        {
            if (nd != null)
            {
                AssessmentDialog dlg = new AssessmentDialog(nd)
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = true
                };
                dlg.ShowDialog();
                if (dlg.ExitOk)
                {
                    nd.Assessment = ChfCommands.GetStringForAssessment(dlg.Assessment);
                    nd.Comment = dlg.Comment;
                    AppStateManager.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Invoked before the context menu shows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _lastRightClickedPoint = null;
            if (BoardShapesManager.IsShapeBuildInProgress)
            {
                BoardShapesManager.CancelShapeDraw(true);
            }
        }

    }
}