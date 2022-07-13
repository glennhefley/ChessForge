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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WorkbookRichTextBuilder _workbookRichTextBuilder;
        WorkbookRichTextBuilder _trainingBrowseRichTextBuilder;
        TrainingProgressRichTextBuilder _trainingProgressRichTextBuilder;

        ChfrgFileBuilder _chfrgFileText;

        private const int squareSize = 80;

        DispatcherTimer dispatcherTimer;
        EvaluationState Evaluation = new EvaluationState();
        public EngineEvaluationGUI EngineLinesGUI;
        AnimationState MoveAnimation = new AnimationState();
        ScoreSheet ActiveLine = new ScoreSheet();
        ChessBoard MainChessBoard;

        List<UIEelementState> _uIEelementStates;

        CommentBoxRichTextBuilder _mainboardCommentBox;
        public GameReplay gameReplay;

        /// <summary>
        /// The complete tree of the currently
        /// loaded workbook (from the PGN or CHFRG file)
        /// </summary>
        public WorkbookTree Workbook;

        private bool _isDebugMode = false;

        internal AppTimers Timers;

        public MainWindow()
        {
            AppState.MainWin = this;

            InitializeComponent();
            SoundPlayer.Initialize();

            // initialize the UIElement states table
            InitializeUIElementStates();

            _mainboardCommentBox = new CommentBoxRichTextBuilder(_rtbBoardComment.Document);

            _menuPlayComputer.Header = Strings.MENU_ENGINE_GAME_START;

            EngineLinesGUI = new EngineEvaluationGUI(_tbEngineLines, _pbEngineThinking, Evaluation);
            Timers = new AppTimers(EngineLinesGUI);

            Configuration.Initialize(this);
            Configuration.StartDirectory = Directory.GetCurrentDirectory();

            // main chess board
            MainChessBoard = new ChessBoard(MainCanvas, imgChessBoard, null, true);

            ResetBookmarks();

            Configuration.ReadConfigurationFile();
            MoveAnimation.MoveDuration = Configuration.MoveSpeed;
            gameReplay = new GameReplay(this, MainChessBoard, _mainboardCommentBox);

            if (Configuration.MainWinPos.IsValid)
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Width;
                this.Height = Configuration.MainWinPos.Height;
            }

            sliderReplaySpeed.Value = Configuration.MoveSpeed;
            _isDebugMode = Configuration.DebugMode != 0;

            EngineMessageProcessor.CreateEngineService(this, _isDebugMode);

            // add the main context menu to the Single Variation view.
            _dgActiveLine.ContextMenu = menuContext;

            _imgStop.Visibility = Visibility.Hidden;
            sliderReplaySpeed.Visibility = Visibility.Hidden;
            _lblEvaluating.Visibility = Visibility.Hidden;
            _lblMoveUnderEval.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Resets or recreates all the bookmarks.
        /// Called on app initialization or when a Workbook was closed.
        /// </summary>
        private void ResetBookmarks()
        {
            AppState.Bookmarks.Clear();

            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_1, _imgBookmark_1, _lblBookmark_1, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_2, _imgBookmark_2, _lblBookmark_2, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_3, _imgBookmark_3, _lblBookmark_3, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_4, _imgBookmark_4, _lblBookmark_4, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_5, _imgBookmark_5, _lblBookmark_5, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_6, _imgBookmark_6, _lblBookmark_6, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_7, _imgBookmark_7, _lblBookmark_7, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_8, _imgBookmark_8, _lblBookmark_8, false)));
            AppState.Bookmarks.Add(new BookmarkView(new ChessBoard(_cnvBookmark_9, _imgBookmark_9, _lblBookmark_9, false)));

            foreach (BookmarkView bv in AppState.Bookmarks)
            {
                bv.SetOpacity(0.5);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            _rtbWorkbookView.Document.Blocks.Clear();
            _rtbWorkbookView.IsReadOnly = true;

            CreateRecentFilesMenuItems();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(20000);
            AppState.ChangeCurrentMode(AppState.Mode.IDLE);
            //            AppState.CurrentMode = AppState.Mode.IDLE;

            bool engineStarted = EngineMessageProcessor.Start();
            if (!engineStarted)
            {
                MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            string lastPgnFile = Configuration.LastPgnFile;
            if (!string.IsNullOrEmpty(lastPgnFile))
            {
                try
                {
                    ReadPgnFile(lastPgnFile);
                }
                catch
                {
                }
            }

        }

        private void CreateRecentFilesMenuItems()
        {
            List<string> recentFiles = Configuration.RecentFiles;
            for (int i = 0; i < recentFiles.Count; i++)
            {
                MenuItem mi = new MenuItem();
                mi.Name = "RecentFiles" + i.ToString();
                try
                {
                    string fileName = System.IO.Path.GetFileName(recentFiles.ElementAt(i));
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        mi.Header = fileName;
                        MenuFile.Items.Add(mi);
                        mi.Click += OpenPgnFile;
                    }
                }
                catch { };
            }
        }

        private Point GetSquareTopLeftPoint(SquareCoords sq)
        {
            double left = squareSize * sq.Xcoord + imgChessBoard.Margin.Left;
            double top = squareSize * (7 - sq.Ycoord) + imgChessBoard.Margin.Top;

            return new Point(left, top);
        }

        private Point GetSquareCenterPoint(SquareCoords sq)
        {
            Point pt = GetSquareTopLeftPoint(sq);
            return new Point(pt.X + squareSize / 2, pt.Y + squareSize / 2);
        }

        private Image GetImageFromPoint(Point p)
        {
            SquareCoords sq = ClickedSquare(p);
            if (sq == null)
            {
                return null;
            }
            else
            {
                return MainChessBoard.GetPieceImage(sq.Xcoord, sq.Ycoord, true);
            }
        }

        private SquareCoords ClickedSquare(Point p)
        {
            double squareSide = imgChessBoard.Width / 8.0;
            double xPos = p.X / squareSide;
            double yPos = p.Y / squareSide;

            if (xPos > 0 && xPos < 8 && yPos > 0 && yPos < 8)
            {
                return new SquareCoords((int)xPos, 7 - (int)yPos);
            }
            else
            {
                return null;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Point clickedPoint = e.GetPosition(imgChessBoard);
                SquareCoords sq = ClickedSquare(clickedPoint);
                if (sq != null)
                {
                    if (AppState.CurrentMode == AppState.Mode.GAME_VS_COMPUTER && EngineGame.State == EngineGame.GameState.USER_THINKING
                        || AppState.CurrentMode == AppState.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_MOVE)
                    {
                        DraggedPiece.isDragInProgress = true;
                        DraggedPiece.Square = sq;

                        DraggedPiece.ImageControl = GetImageFromPoint(clickedPoint);
                        Point ptLeftTop = GetSquareTopLeftPoint(sq);
                        DraggedPiece.ptDraggedPieceOrigin = ptLeftTop;

                        // for the remainder, we need absolute point
                        clickedPoint.X += imgChessBoard.Margin.Left;
                        clickedPoint.Y += imgChessBoard.Margin.Top;
                        DraggedPiece.ptStartDragLocation = clickedPoint;


                        Point ptCenter = GetSquareCenterPoint(sq);

                        Canvas.SetLeft(DraggedPiece.ImageControl, ptLeftTop.X + (clickedPoint.X - ptCenter.X));
                        Canvas.SetTop(DraggedPiece.ImageControl, ptLeftTop.Y + (clickedPoint.Y - ptCenter.Y));
                    }

                }
            }
        }

        /// <summary>
        /// Depending on the Application and or Training mode,
        /// this may have been the user completeing a move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            dispatcherTimer.Stop();

            if (DraggedPiece.isDragInProgress)
            {
                DraggedPiece.isDragInProgress = false;
                Point clickedPoint = e.GetPosition(imgChessBoard);
                SquareCoords sq = ClickedSquare(clickedPoint);
                if (sq == null)
                {
                    // just put the piece back
                    Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
                    Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
                }
                else
                {
                    // double check that we are legitimately making a move
                    if (AppState.CurrentMode == AppState.Mode.GAME_VS_COMPUTER && EngineGame.State == EngineGame.GameState.USER_THINKING
                        || AppState.CurrentMode == AppState.Mode.TRAINING && TrainingState.CurrentMode == TrainingState.Mode.AWAITING_USER_MOVE)
                    {
                        // if the move is valid swap image at destination 
                        // and clear image at origin
                        if (sq.Xcoord != DraggedPiece.Square.Xcoord || sq.Ycoord != DraggedPiece.Square.Ycoord)
                        {
                            //TODO: handle promotion!!!
                            StringBuilder move = new StringBuilder();
                            if (!MainChessBoard.IsFlipped)
                            {
                                move.Append((char)(DraggedPiece.Square.Xcoord + (int)'a'));
                                move.Append((char)(DraggedPiece.Square.Ycoord + (int)'1'));
                                move.Append((char)(sq.Xcoord + (int)'a'));
                                move.Append((char)(sq.Ycoord + (int)'1'));
                            }
                            else
                            {
                                move.Append((char)((7 - DraggedPiece.Square.Xcoord) + (int)'a'));
                                move.Append((char)((7 - DraggedPiece.Square.Ycoord) + (int)'1'));
                                move.Append((char)((7 - sq.Xcoord) + (int)'a'));
                                move.Append((char)((7 - sq.Ycoord) + (int)'1'));
                            }

                            bool isCastle;
                            TreeNode nd;
                            if (EngineGame.ProcessUserGameMove(move.ToString(), out nd, out isCastle))
                            {
                                MainChessBoard.GetPieceImage(sq.Xcoord, sq.Ycoord, true).Source = DraggedPiece.ImageControl.Source;
                                ReturnDraggedPiece(true);
                                if (isCastle)
                                {
                                    MoveCastlingRook(move.ToString());
                                }

                                SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
                                _mainboardCommentBox.GameMoveMade(nd, true);
                            }
                            else
                            {
                                ReturnDraggedPiece(false);
                            }
                        }
                        else
                        {
                            ReturnDraggedPiece(false);
                        }
                    }
                    else
                    {
                        ReturnDraggedPiece(false);
                    }
                }
                Canvas.SetZIndex(DraggedPiece.ImageControl, 0);
            }
        }

        /// <summary>
        /// Completes a castling move. King would have already been moved.
        /// </summary>
        /// <param name="move"></param>
        private void MoveCastlingRook(string move)
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

        private void ReturnDraggedPiece(bool clearImage)
        {
            if (clearImage)
            {
                DraggedPiece.ImageControl.Source = null;
            }
            Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
            Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point clickedPoint = e.GetPosition(imgChessBoard);

            if (DraggedPiece.isDragInProgress)
            {
                Canvas.SetZIndex(DraggedPiece.ImageControl, 10);
                clickedPoint.X += imgChessBoard.Margin.Left;
                clickedPoint.Y += imgChessBoard.Margin.Top;

                Canvas.SetLeft(DraggedPiece.ImageControl, clickedPoint.X - squareSize / 2);
                Canvas.SetTop(DraggedPiece.ImageControl, clickedPoint.Y - squareSize / 2);
            }

        }

        /// <summary>
        /// Move animation requested as part of auto-replay.
        /// As such we need to flip the coordinates if
        /// the board is flipped.
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(MoveUI move)
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

            Canvas.SetZIndex(img, 1);

            Point orig = GetSquareTopLeftPoint(origin);
            Point dest = GetSquareTopLeftPoint(destination);

            TranslateTransform trans = new TranslateTransform();
            if (img.RenderTransform != null)
                img.RenderTransform = trans;

            DoubleAnimation animX = new DoubleAnimation(0, dest.X - orig.X, TimeSpan.FromMilliseconds(MoveAnimation.MoveDuration));
            DoubleAnimation animY = new DoubleAnimation(0, dest.Y - orig.Y, TimeSpan.FromMilliseconds(MoveAnimation.MoveDuration));

            AppState.CurrentTranslateTransform = trans;
            AppState.CurrentAnimationX = animX;
            AppState.CurrentAnimationY = animY;

            animX.Completed += new EventHandler(Move_Completed);
            trans.BeginAnimation(TranslateTransform.XProperty, animX);
            trans.BeginAnimation(TranslateTransform.YProperty, animY);

        }

        private void StopAnimation()
        {
            // TODO Apparently, there are 2 methods to stop animation.
            // Method 1 below keeps the animated image at the spot it was when the stop request came.
            // Method 2 returns it to the initial position.
            // Neither works fully to our satisfaction. They seem to not be exiting immediately and are leaving some garbage
            // behind which prevents us from immediatey changing the speed of animation on user's request 
            if (AppState.CurrentAnimationX != null && AppState.CurrentAnimationY != null && AppState.CurrentTranslateTransform != null)
            {
                // *** Method 1.
                //AppState.CurrentAnimationX.BeginTime = null;
                //AppState.CurrentAnimationY.BeginTime = null;
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, AppState.CurrentAnimationX);
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, AppState.CurrentAnimationY);

                // *** Method 2.
                AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
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
        private void Move_Completed(object sender, EventArgs e)
        {
            AppState.CurrentTranslateTransform = null;
            AppState.CurrentAnimationX = null;
            AppState.CurrentAnimationY = null;

            MainChessBoard.GetPieceImage(MoveAnimation.Destination.Xcoord, MoveAnimation.Destination.Ycoord, true).Source = MoveAnimation.Piece.Source;

            Point orig = GetSquareTopLeftPoint(MoveAnimation.Origin);
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
            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * MoveAnimation.Origin.Xcoord + imgChessBoard.Margin.Left);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * (7 - MoveAnimation.Origin.Ycoord) + imgChessBoard.Margin.Top);

            gameReplay.PrepareNextMoveForAnimation(gameReplay.LastAnimatedMoveIndex, false);
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (MainBoard.Width > 100)
            {
                MainBoard.Margin = new Thickness(MainBoard.Margin.Left + 10, MainBoard.Margin.Top, MainBoard.Margin.Right, MainBoard.Margin.Bottom);
                MainBoard.Width -= 10;
                //            SideBoard.Height = 640;
            }
        }

        /// <summary>
        /// Selects a move in all relevant views.
        /// </summary>
        /// <param name="moveNo"></param>
        /// <param name="colorToMove"></param>
        public void SelectPlyInTextViews(int moveNo, PieceColor colorToMove)
        {
            ViewActiveLine_SelectPly(moveNo, colorToMove);
        }

        private void OpenPgnFile(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(AppState.Mode.MANUAL_REVIEW))
            {
                string menuItemName = ((MenuItem)e.Source).Name;
                string path = Configuration.GetRecentFile(menuItemName);
                ReadPgnFile(path);
            }
        }

        /// <summary>
        /// Loads a new PGN file.
        /// If the application is NOT in the IDLE mode, it will ask the user:
        /// - to close/cancel/save/put_aside the current tree (TODO: TO BE IMPLEMENTED)
        /// - stop a game against the engine, if in progress
        /// - stop any engine evaluations if in progress (TODO: it should be allowed to continue background analysis in a separate low-pri thread).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_LoadPgn(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(AppState.Mode.MANUAL_REVIEW))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "Game files (*.pgn)|*.pgn|All files (*.*)|*.*";

                string initDir;
                if (!string.IsNullOrEmpty(Configuration.LastPgnDirectory))
                {
                    initDir = Configuration.LastPgnDirectory;
                }
                else
                {
                    initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                openFileDialog.InitialDirectory = initDir;

                bool? result = false;

                try
                {
                    result = openFileDialog.ShowDialog();
                }
                catch
                {
                    openFileDialog.InitialDirectory = "";
                    result = openFileDialog.ShowDialog();
                };

                if (result == true)
                {
                    Configuration.LastPgnDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                    ReadPgnFile(openFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Returns true if user accept the change. of mode.
        /// </summary>
        /// <param name="newMode"></param>
        /// <returns></returns>
        private bool ChangeAppModeWarning(AppState.Mode newMode)
        {
            if (AppState.CurrentMode == AppState.Mode.IDLE)
            {
                // it is a fresh state, no need for any warnings
                return true;
            }

            bool result = false;
            // we may not be changing the mode, but changing
            // the variation tree we are working with.
            if (AppState.CurrentMode == AppState.Mode.MANUAL_REVIEW && newMode == AppState.Mode.MANUAL_REVIEW)
            {
                // TODO: ask what to do with the current tree
                // abandon, save, put aside
                result = true;
            }
            else if (AppState.CurrentMode != AppState.Mode.MANUAL_REVIEW && newMode == AppState.Mode.MANUAL_REVIEW)
            {
                switch (AppState.CurrentMode)
                {
                    case AppState.Mode.GAME_VS_COMPUTER:
                        if (MessageBox.Show("Cancel Game", "Game with the Computer is in Progress", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            result = true;
                        break;
                    //case AppState.Mode.GAME_REPLAY:
                    //    if (MessageBox.Show("Cancel Replay", "Game Replay is in Progress", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    //        result = true;
                    //    break;
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

        private void RecreateRecentFilesMenuItems()
        {
            List<object> itemsToRemove = new List<object>();

            for (int i = 0; i < MenuFile.Items.Count; i++)
            {
                if (MenuFile.Items[i] is MenuItem)
                {
                    MenuItem item = (MenuItem)MenuFile.Items[i];
                    if (item.Name.StartsWith("RecentFiles"))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (MenuItem item in itemsToRemove)
            {
                MenuFile.Items.Remove(item);
            }

            CreateRecentFilesMenuItems();
        }

        /// <summary>
        /// Reads in the file and builds internal Variation Tree
        /// structures for the entire content.
        /// </summary>
        /// <param name="fileName"></param>
        private void ReadPgnFile(string fileName)
        {
            try
            {
                string gameText = File.ReadAllText(fileName);

                Configuration.AddRecentFile(fileName);
                RecreateRecentFilesMenuItems();

                Workbook = new WorkbookTree();
                _rtbWorkbookView.Document.Blocks.Clear();
                PgnGameParser pgnGame = new PgnGameParser(gameText, Workbook, true);
                _mainboardCommentBox.ShowWorkbookTitle(Workbook.Title);

                _workbookRichTextBuilder = new WorkbookRichTextBuilder(_rtbWorkbookView.Document);
                _trainingBrowseRichTextBuilder = new WorkbookRichTextBuilder(_rtbTrainingBrowse.Document);
                _trainingProgressRichTextBuilder = new TrainingProgressRichTextBuilder(_rtbTrainingProgress.Document);

                Workbook.BuildLines();

                _chfrgFileText = new ChfrgFileBuilder();
                _chfrgFileText.BuildTreeText();

                _workbookRichTextBuilder.BuildFlowDocumentForWorkbook();
                if (Workbook.Bookmarks.Count == 0)
                {
                    Workbook.GenerateBookmarks();
                }

                int startingNode = 0;
                string startLineId = Workbook.GetDefaultLineIdForNode(startingNode);
                SetActiveLine(startLineId, startingNode);

                //_dgActiveLine.ItemsSource = ActiveLine.MoveList;

                SetupDataInTreeView();

                ShowBookmarks();

                _workbookRichTextBuilder.SelectLineAndMove(startLineId, startingNode);
                _lvWorkbookTable_SelectLineAndMove(startLineId, startingNode);

                Configuration.LastPgnFile = fileName;
                Configuration.AddRecentFile(fileName);

                AppState.ChangeCurrentMode(AppState.Mode.MANUAL_REVIEW);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error processing PGN file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetActiveLine(string lineId, int selectedNodeId)
        {
            ObservableCollection<TreeNode> line = Workbook.SelectLine(lineId);
            SetActiveLine(line, selectedNodeId);
        }

        public void DisplayPosition(BoardPosition position)
        {
            MainChessBoard.DisplayPosition(position);
        }

        public void SetActiveLine(ObservableCollection<TreeNode> line, int selectedNodeId)
        {
            ActiveLine.NodeList = line;
            ActiveLine.MoveList = PositionUtils.BuildViewListFromLine(line);
            _dgActiveLine.ItemsSource = ActiveLine.MoveList;

            if (selectedNodeId > 0)
            {
                TreeNode nd = ActiveLine.NodeList.First(x => x.NodeId == selectedNodeId);
                ViewActiveLine_SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                MainChessBoard.DisplayPosition(nd.Position);
            }
        }

        private void ChessForgeMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppLog.Dump();
            EngineLog.Dump();
            Configuration.WriteOutConfiguration();
        }

        private void sliderReplaySpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Configuration.MoveSpeed = (int)e.NewValue;
            MoveAnimation.MoveDuration = Configuration.MoveSpeed;
        }

        /// <summary>
        /// The user requested evaluation of the currently selected move.
        /// Check if there is an item currently selected. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_EvaluatePosition(object sender, RoutedEventArgs e)
        {
            if (Evaluation.Mode != EvaluationState.EvaluationMode.IDLE)
            {
                // there is an evaluation running right now so do not allow another one.
                // This menu item should be disabled if that's the case so we should never
                // end up here but just in case ...
                MessageBox.Show("Cannot start an evaluation while another one in progress.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int moveIndex = ViewSingleLine_GetSelectedPlyNodeIndex();
            if (moveIndex < 0)
            {
                MessageBox.Show("Select a move to evaluate.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // the position we want to show is the next element
                int posIndex = moveIndex + 1;
                // check that the engine is available
                if (EngineMessageProcessor.IsEngineAvailable())
                {
                    // make an extra defensive check
                    if (posIndex < ActiveLine.NodeList.Count)
                    {
                        RequestMoveEvaluation(posIndex, EvaluationState.EvaluationMode.SINGLE_MOVE, true);
                    }
                }
                else
                {
                    MessageBox.Show("Chess Engine is not available.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void MenuItem_EvaluateLine(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.NodeList == null || ActiveLine.NodeList.Count == 0)
            {
                return;
            }

            if (Evaluation.Mode != EvaluationState.EvaluationMode.IDLE)
            {
                // there is an evaluation running right now so do not allow another one.
                MessageBox.Show("Cannot start an evaluation while another one in progress.", "Move Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Evaluation.PositionIndex = 1;
            // we will start with the first move of the active line
            if (EngineMessageProcessor.IsEngineAvailable())
            {
                _dgActiveLine.SelectedCells.Clear();
                RequestMoveEvaluation(Evaluation.PositionIndex, EvaluationState.EvaluationMode.FULL_LINE, true);
            }
            else
            {
                MessageBox.Show("Chess Engine is not avalable.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Starts move evaluation on user's request or when continuing line evaluation.
        /// NOTE: does not start evaluation when making a move during a user vs engine game.
        /// </summary>
        /// <param name="posIndex"></param>
        /// <param name="mode"></param>
        /// <param name="isLineStart"></param>
        private void RequestMoveEvaluation(int posIndex, EvaluationState.EvaluationMode mode, bool isLineStart)
        {
            Evaluation.PositionIndex = posIndex;
            Evaluation.Position = ActiveLine.NodeList[posIndex].Position;
            MainChessBoard.DisplayPosition(Evaluation.Position);

            ShowMoveEvaluationControls(true, isLineStart);
            UpdateLastMoveTextBox(posIndex, isLineStart);

            Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);

            PrepareMoveEvaluation(mode, Evaluation.Position);
        }

        /// <summary>
        /// This method will be called when in Training mode to evaluate
        /// user's move or moves from the Workbook.
        /// </summary>
        /// <param name="nodeId"></param>
        public void RequestMoveEvaluationInTraining(int nodeId)
        {
            TreeNode nd = Workbook.GetNodeFromNodeId(nodeId);
            RequestMoveEvaluationInTraining(nd);
            //Evaluation.Position = nd.Position;
            //ShowMoveEvaluationControls(true, false);

            //EngineMessageProcessor.StartMessagePollTimer();
            //PrepareMoveEvaluation(EvaluationState.EvaluationMode.IN_TRAINING, Evaluation.Position);
        }

        public void RequestMoveEvaluationInTraining(TreeNode nd)
        {
            Evaluation.Position = nd.Position;
            UpdateLastMoveTextBox(nd, true);
            ShowMoveEvaluationControls(true, false);

            Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
            PrepareMoveEvaluation(EvaluationState.EvaluationMode.IN_TRAINING, Evaluation.Position);
        }

        /// <summary>
        /// Prepares controls, timers 
        /// and requests the engine to make move.
        /// </summary>
        /// <param name="position"></param>
        private void RequestEngineMove(BoardPosition position)
        {
            PrepareMoveEvaluation(EvaluationState.EvaluationMode.IN_GAME_PLAY, position);
        }

        /// <summary>
        /// Preparations for move evaluation that are common for Position/Line 
        /// evaluation as well as requesting engine move in a game.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="position"></param>
        private void PrepareMoveEvaluation(EvaluationState.EvaluationMode mode, BoardPosition position)
        {
            Evaluation.Mode = mode;

            menuEvalLine.Dispatcher.Invoke(() =>
            {
                menuEvalLine.IsEnabled = false;
            });
            menuEvalPos.Dispatcher.Invoke(() =>
            {
                menuEvalPos.IsEnabled = false;
            });

            _pbEngineThinking.Dispatcher.Invoke(() =>
            {
                _pbEngineThinking.Visibility = Visibility.Visible;
                _pbEngineThinking.Minimum = 0;
                // add 50% to compensate for any processing delays
                // we don't want to be too optimistic
                _pbEngineThinking.Maximum = (int)(Configuration.EngineEvaluationTime * 1.5);
                _pbEngineThinking.Value = 0;
            });

            Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            Timers.Start(AppTimers.StopwatchId.EVALUATION_PROGRESS);

            string fen = FenParser.GenerateFenFromPosition(position);
            EngineMessageProcessor.RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

        private void UpdateLastMoveTextBox(TreeNode nd, bool isLineStart)
        {
            string moveTxt = MoveUtils.BuildSingleMoveText(nd);

            UpdateLastMoveTextBox(moveTxt, isLineStart);
        }

        private void UpdateLastMoveTextBox(int posIndex, bool isLineStart)
        {
            string moveTxt = Evaluation.Position.MoveNumber.ToString()
                    + (Evaluation.Position.ColorToMove == PieceColor.Black ? "." : "...")
                    + ActiveLine.NodeList[posIndex].LastMoveAlgebraicNotation;

            UpdateLastMoveTextBox(moveTxt, isLineStart);
        }

        private void UpdateLastMoveTextBox(string moveTxt, bool isLineStart)
        {
            if (isLineStart)
            {
                _lblMoveUnderEval.Content = moveTxt;
            }
            else
            {
                _lblMoveUnderEval.Dispatcher.Invoke(() =>
                {
                    _lblMoveUnderEval.Content = moveTxt;
                });
            }
        }

        public static object EvalLock = new object();

        /// <summary>
        /// Evaluation has finished.
        /// Tidy up and reset to prepare for the next 
        /// evaluation request.
        /// NOTE: This is called in the evaluation mode as well as during user vs engine game. 
        /// </summary>
        public void MoveEvaluationFinished()
        {
            if (AppState.CurrentMode == AppState.Mode.GAME_VS_COMPUTER)
            {
                ProcessEngineGameMoveEvent();
                Evaluation.PrepareToContinue();

                Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
            }
            else if (AppState.CurrentMode == AppState.Mode.TRAINING)
            {
                // stop the timer, reset mode and apply training mode specific handling 
                Evaluation.Mode = EvaluationState.EvaluationMode.IDLE;
                Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
                Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

                MoveEvaluationFinishedInTraining();
            }
            else
            {
                lock (EvalLock)
                {
                    AppLog.Message("Move evaluation finished for index " + Evaluation.PositionIndex.ToString());

                    // TODO need to implement all these calculations completely differently
                    bool isWhiteEval = (Evaluation.PositionIndex - 1) % 2 == 0;
                    int evalInt = isWhiteEval ? -1 * Evaluation.PositionCpScore : Evaluation.PositionCpScore;
                    //                    string eval = "(" + (((double)evalInt) / 100.0).ToString("F2") + ")";
                    string eval = (evalInt > 0 ? "+" : "") + (((double)evalInt) / 100.0).ToString("F2");
                    int moveIndex = (Evaluation.PositionIndex - 1) / 2;
                    if (isWhiteEval)
                    {
                        ActiveLine.MoveList[moveIndex].WhiteEval = eval;
                    }
                    else
                    {
                        ActiveLine.MoveList[moveIndex].BlackEval = eval;
                    }

                    Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

                    // if the mode is not FULL_LINE or this is the last move in FULL_LINE
                    // evaluation we stop here
                    // otherwise we start the next move's evaluation
                    if (Evaluation.Mode != EvaluationState.EvaluationMode.FULL_LINE
                        || Evaluation.PositionIndex == ActiveLine.NodeList.Count - 1)
                    {
                        Evaluation.Reset();

                        menuEvalLine.Dispatcher.Invoke(() =>
                        {
                            menuEvalLine.IsEnabled = true;
                        });
                        menuEvalPos.Dispatcher.Invoke(() =>
                        {
                            menuEvalPos.IsEnabled = true;
                        });
                        _pbEngineThinking.Dispatcher.Invoke(() =>
                        {
                            _pbEngineThinking.Visibility = Visibility.Hidden;
                        });

                        ShowMoveEvaluationControls(false, false);

                    }
                    else
                    {
                        AppLog.Message("Continue eval next move after index " + Evaluation.PositionIndex.ToString());
                        Evaluation.PrepareToContinue();

                        Evaluation.PositionIndex++;
                        RequestMoveEvaluation(Evaluation.PositionIndex, Evaluation.Mode, false);

                        Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    }
                }
            }
        }

        /// <summary>
        /// If in training mode, we want to keep the evaluation lines
        /// visible in the comment box, and display the response moves
        /// with their line evaluations in the Training tab.
        /// </summary>
        public void MoveEvaluationFinishedInTraining()
        {
            ShowMoveEvaluationControls(false, false, true);
            _trainingProgressRichTextBuilder.ShowEvaluationResult();
        }

        /// <summary>
        /// Sets visibility for the controls relevant to move evaluation modes.
        /// NOTE: this is not applicable to move evaluation during a game.
        /// Engine Lines TextBox replaces the Board Comment RichTextBox if
        /// we are in the Position/Line evaluation mode.
        /// </summary>
        /// <param name="visible"></param>
        /// <param name="isLineStart"></param>
        private void ShowMoveEvaluationControls(bool visible, bool isLineStart, bool keepLinesBox = false)
        {
            if (visible)
            {
                if (isLineStart)
                {
                    _rtbBoardComment.Visibility = visible ? Visibility.Hidden : Visibility.Visible;
                    _tbEngineLines.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                    _imgStop.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                    sliderReplaySpeed.Visibility = Visibility.Hidden;

                    _lblEvaluating.Visibility = Visibility.Visible;
                    _lblMoveUnderEval.Visibility = Visibility.Visible;
                }
                else
                {
                    _rtbBoardComment.Dispatcher.Invoke(() =>
                    {
                        _rtbBoardComment.Visibility = Visibility.Hidden;
                    });

                    _tbEngineLines.Dispatcher.Invoke(() =>
                    {
                        _tbEngineLines.Visibility = Visibility.Visible;
                    });

                    _imgStop.Dispatcher.Invoke(() =>
                    {
                        _imgStop.Visibility = Visibility.Visible;
                    });

                    _lblEvaluating.Dispatcher.Invoke(() =>
                    {
                        _lblEvaluating.Visibility = Visibility.Visible;
                    });

                    _lblMoveUnderEval.Dispatcher.Invoke(() =>
                    {
                        _lblMoveUnderEval.Visibility = Visibility.Visible;
                    });
                }
            }
            else
            {
                if (!keepLinesBox)
                {
                    _rtbBoardComment.Dispatcher.Invoke(() =>
                    {
                        _rtbBoardComment.Visibility = Visibility.Visible;
                    });

                    _tbEngineLines.Dispatcher.Invoke(() =>
                    {
                        _tbEngineLines.Visibility = Visibility.Hidden;
                    });
                }

                _imgStop.Dispatcher.Invoke(() =>
                {
                    _imgStop.Visibility = Visibility.Hidden;
                });

                _lblEvaluating.Dispatcher.Invoke(() =>
                {
                    _lblEvaluating.Visibility = Visibility.Hidden;
                });

                _lblMoveUnderEval.Dispatcher.Invoke(() =>
                {
                    _lblMoveUnderEval.Visibility = Visibility.Hidden;
                });
            }
        }

        /// <summary>
        /// The user requests a game against the computer starting from the current position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _menuPlayComputer_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.CurrentMode == AppState.Mode.GAME_VS_COMPUTER)
            {
                // menu item was offering to exit the game so 
                // change the header back and cleanup
                _menuPlayComputer.Header = Strings.MENU_ENGINE_GAME_START;
                StopEngineGame();
            }
            else
            {

                // check that there is a move selected in the _dgMainLineView so
                // that we have somewhere to start

                // TODO: disable this menu if no move selected.

                TreeNode nd = ViewActiveLine_GetSelectedTreeNode();
                if (nd != null)
                {
                    PlayComputer(nd, false);
                }
                else
                {
                    MessageBox.Show("Select the move from which to start.", "Computer Game", MessageBoxButton.OK);
                }
            }
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
        public void PlayComputer(TreeNode startNode, bool IsTraining)
        {
            imgChessBoard.Source = ChessBoards.ChessBoardGreen;

            AppState.ChangeCurrentMode(AppState.Mode.GAME_VS_COMPUTER);

            EngineGame.PrepareGame(startNode, true, IsTraining);
            _dgEngineGame.ItemsSource = EngineGame.Line.MoveList;

            if (startNode.ColorToMove == PieceColor.White)
            {
                if (!MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }

            RequestEngineMove(startNode.Position);
            _menuPlayComputer.Header = Strings.MENU_ENGINE_GAME_STOP;
        }

        /// <summary>
        /// We have a response from the engine so need to choose
        /// the move from the list of candidates, show it on the board
        /// and display in the Engine Game Line view.
        /// </summary>
        private void ProcessEngineGameMoveEvent()
        {
            BoardPosition pos = null;

            // NOTE: need to invoke from the Dispatcher here or the program
            // will crash when engine makes a White move (because
            // it will attempt to add an element to the MoveList ObservableCollection
            // from the "wrong" thread)
            this.Dispatcher.Invoke(() =>
            {
                TreeNode nd;
                pos = EngineGame.ProcessEngineGameMove(out nd);
                SoundPlayer.PlayMoveSound(nd.LastMoveEngineNotation);
                _mainboardCommentBox.GameMoveMade(nd, false);
            });


            // update the GUI and finish
            // (the app will wait for the user's move)
            MainChessBoard.DisplayPosition(pos);
            EngineGame.State = EngineGame.GameState.USER_THINKING;
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// This method will be invoked periodically by the 
        /// times checking for the completion of user moves.
        /// The user can make moves in 2 contexts:
        /// 1. a game against the engine (in this case EngineGame.State 
        /// should already be set to ENGINE_THINKING)
        /// 2. a user entered the move as part of training and we will
        /// provide them a feedback based on the content of the workbook.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        internal void ProcessUserGameMoveEvent(object source, ElapsedEventArgs e)
        {
            if (TrainingState.IsTrainingInProgress)
            {
                if ((TrainingState.CurrentMode & TrainingState.Mode.USER_MOVE_COMPLETED) != 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        _trainingProgressRichTextBuilder.ReportLastMoveVsWorkbook();
                        Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                    });
                }
            }
            else // this is a game user vs engine then
            {
                // check if the user move was completed and if so request engine move
                if (EngineGame.State == EngineGame.GameState.ENGINE_THINKING)
                {

                    _dgEngineGame.Dispatcher.Invoke(() =>
                    {
                        // the ply is already stored in EngineGame and now we need to add it
                        // to the list of plies.
                        EngineGame.AddLastNodeToPlies();
                    });

                    Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                    RequestEngineMove(EngineGame.GetCurrentPosition());
                }
            }
        }


        /// <summary>
        /// Reset controls and restore selection in the ActiveLine
        /// control.
        /// We are going back to the MANUAL REVIEW or TRAINING mode
        /// so Active Line view will be shown.
        /// </summary>
        private void StopEngineGame()
        {
            Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

            menuEvalLine.Dispatcher.Invoke(() =>
            {
                menuEvalLine.IsEnabled = true;
            });

            menuEvalPos.Dispatcher.Invoke(() =>
            {
                menuEvalPos.IsEnabled = true;
            });

            _pbEngineThinking.Dispatcher.Invoke(() =>
            {
                _pbEngineThinking.Visibility = Visibility.Visible;
                _pbEngineThinking.Minimum = 0;
                _pbEngineThinking.Maximum = (int)(Configuration.EngineEvaluationTime);
                _pbEngineThinking.Value = 0;
            });

            imgChessBoard.Source = ChessBoards.ChessBoardBlue;
            Evaluation.Reset();
            Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
            AppState.CurrentMode = AppState.Mode.MANUAL_REVIEW;
            EngineGame.State = EngineGame.GameState.IDLE;
            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            AppState.ExitCurrentMode();

            int row, column;

            ViewActiveLine_GetSelectedRowColumn(out row, out column);
            SelectPlyInTextViews(row, column == 1 ? PieceColor.White : PieceColor.Black);
            int nodeIndex = ViewActiveLine_GetNodeIndexFromRowColumn(row, column);
            TreeNode nd = ActiveLine.NodeList[nodeIndex];
            MainChessBoard.DisplayPosition(nd.Position);

            _mainboardCommentBox.RestoreTitleMessage();
        }

        private void ShowBookmarks()
        {
            for (int i = 0; i < Workbook.Bookmarks.Count; i++)
            {
                AppState.Bookmarks[i].BookmarkData = Workbook.Bookmarks[i];
                AppState.Bookmarks[i].Activate();
            }
        }


        /// <summary>
        /// Ensure that Workbook Tree's ListView allows
        /// mouse wheel scrilling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _scvWorkbookTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void _rtbWorkbookFull_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Hand it off to the ActiveLine view.
            // In the future we may want to handle some key strokes here
            // but for now we will respond to whatever the ActiveLine view will request.
            ViewActiveLine_PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Starts a training session.
        /// If the workbook has results from an earlier session, we will ask the user
        /// if they want to clear those results or to resume that earlier session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_StartTraining(object sender, RoutedEventArgs e)
        {
            if (Workbook.Bookmarks.Count == 0)
            {
                MessageBox.Show("There are no training positions in this Workbook.  Do you want to generate some?",
                    "ChessForge Training", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                // TODO: implement handling of the answer.
            }

            //TODO: check if we have results from an earlier session
            SetAppInTrainingMode(0);
        }

        private void SetAppInTrainingMode(int bookmarkIndex)
        {
            // Set training mode, clearing any other states
            // that may have been set
            // TODO: need to reset any possible activities like
            // replaying a line or playing against the computer.
            AppState.CurrentMode = AppState.Mode.TRAINING;

            // Get the first bookmarked position
            AppState.ActiveBookmarkInTraining = 0;
            MainChessBoard.SetBoardSourceImage(ChessBoards.ChessBoardGreen);

            TreeNode startNode = Workbook.Bookmarks[bookmarkIndex].Node;
            MainChessBoard.DisplayPosition(startNode.Position);

            _trainingBrowseRichTextBuilder.BuildFlowDocumentForWorkbook(Workbook.Bookmarks[bookmarkIndex].Node.NodeId);
            _trainingProgressRichTextBuilder.Initialize(Workbook.Bookmarks[bookmarkIndex].Node);

            EnterGuiTrainingMode();
            TrainingState.IsTrainingInProgress = true;

            _mainboardCommentBox.TrainingSessionStart();

            //TODO check if there conditions where there is no point in user making a move.
            TrainingState.CurrentMode = TrainingState.Mode.AWAITING_USER_MOVE;

            // The Line display is the same as when playing a game against the computer 
            EngineGame.PrepareGame(startNode, false, false);
            _dgEngineGame.ItemsSource = EngineGame.Line.MoveList;
            AppState.ChangeCurrentMode(AppState.Mode.TRAINING);
            //            ShowEngineGameGuiControls();
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
        }

        private void EnterGuiTrainingMode()
        {
            AppState.ChangeCurrentMode(AppState.Mode.TRAINING);
        }

        private void ExitGuiTrainingMode()
        {
            _tabMainControl.Visibility = Visibility.Visible;
            _tabTrainingControl.Visibility = Visibility.Hidden;
        }

        private void MenuItem_StopTraining(object sender, RoutedEventArgs e)
        {
            // TODO: ask questions re saving etc.
            ExitGuiTrainingMode();
            MainChessBoard.SetBoardSourceImage(ChessBoards.ChessBoardBlue);
            TrainingState.IsTrainingInProgress = false;
        }

        /// <summary>
        /// Changes visibility of the GUI elements according to the passed mode
        /// and submode.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="subMode"></param>
        public void ConfigureUIForMode(AppState.Mode mode, AppState.SubMode subMode = 0)
        {
            foreach (UIEelementState el in _uIEelementStates)
            {
                if ((el.ModeVisibilityFlags & (uint)mode) != 0
                    && (subMode == 0 || (el.ModeVisibilityFlags & (uint)subMode) != 0))
                {
                    el.Element.Visibility = Visibility.Visible;
                }
                else
                {
                    el.Element.Visibility = Visibility.Hidden;
                }

                if (el.ModeEnabledFlags == 0 || subMode == 0)
                {
                    el.Element.IsEnabled = true;
                }
                else
                {
                    if ((el.ModeEnabledFlags & (uint)mode) != 0
                        && (subMode == 0 || (el.ModeEnabledFlags & (uint)subMode) != 0))
                    {
                        el.Element.IsEnabled = true;
                    }
                    else
                    {
                        el.Element.IsEnabled = false;
                    }
                }
            }

            SpecialUIHandling(mode, subMode);
        }

        /// <summary>
        /// For some mode/submode combinations it may not be sufficient to 
        /// configure the GUI according to the configuration table.
        /// This method will perform any additional actions required.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="subMode"></param>
        public void SpecialUIHandling(AppState.Mode mode, AppState.SubMode subMode = 0)
        {
        }

        /// <summary>
        /// Initializes table with the GUI configuration data
        /// for app's modes and submodes.
        /// </summary>
        private void InitializeUIElementStates()
        {
            // helper variable
            uint allModes = 0xFFFF;

            _uIEelementStates = new List<UIEelementState>()
            {
                 // Active Line scoresheet visible and enabled except during a game vs engine and training
                 { new UIEelementState(_dgActiveLine,
                        allModes & (uint)~(AppState.Mode.GAME_VS_COMPUTER | AppState.Mode.TRAINING),
                        allModes & (uint)~(AppState.Mode.GAME_VS_COMPUTER | AppState.Mode.TRAINING),
                        0, 0 )},

                 // elements visible in all modes except training mode
                 { new UIEelementState(
                        _tabMainControl,
                        allModes & (uint)~AppState.Mode.TRAINING, 0,
                        0, 0) },

                 // elements only visible in training
                 { new UIEelementState(_tabTrainingControl,
                        (uint)AppState.Mode.TRAINING, 0,
                        0, 0) },

                 // game scoresheet visible and enabled during a game vs engine and training
                 { new UIEelementState(_dgEngineGame,
                        (uint)AppState.Mode.GAME_VS_COMPUTER | (uint)AppState.Mode.TRAINING, 0,
                        0,0) },

                 // elements only visible and enabled during a game vs engine
                 { new UIEelementState(_lblGameInProgress,
                        (uint)AppState.Mode.GAME_VS_COMPUTER, 0,
                        0, 0) },

                 // elements only visible during engine evaluation
                 { new UIEelementState(_tbEngineLines,
                        (uint)AppState.Mode.ENGINE_EVALUATION, 0,
                        0, 0) },

                 { new UIEelementState(_pbEngineThinking,
                        (uint)AppState.Mode.ENGINE_EVALUATION, 0,
                        0, 0) },
            };
        }

        private void _imgStop_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: this only leads to stopping after the current move's evaluation finishes
            // improve so that we send a stop message to the engine and abandon immediately
            lock (EvalLock)
            {
                Evaluation.Mode = EvaluationState.EvaluationMode.SINGLE_MOVE;
            }

            e.Handled = true;
        }

        private void MenuItem_FlipBoard(object sender, RoutedEventArgs e)
        {
            MainChessBoard.FlipBoard();
        }
    }

}
