﻿using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using WebAccess;
using System.Text;
using ChessPosition;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for GamesPreviewDialog.xaml
    /// </summary>
    public partial class GamePreviewDialog : Window
    {
        // chessboard object for game replay
        protected ChessBoardSmall _chessBoard;

        // VariationTree into which the current game is loaded
        protected VariationTree _tree;

        // the list of Nodes to replay
        protected ObservableCollection<TreeNode> _mainLine;

        // true if we are exiting the dialog
        protected bool _isExiting = false;

        // index in the list of Nodes of the position with the move being animated
        protected int _currentNodeMoveIndex = 0;

        // move animation speed in millieconds
        protected int _animationSpeed = 200;

        // the MoveAnimator object running the animation
        protected MoveAnimator _animator;

        // whether the Animation was started and there was no completion event
        protected bool _isAnimating = false;

        // whether pause was requested by the user
        protected bool _pauseRequested = false;

        // animation speeds
        protected const int _fastAnimation = 200;
        protected const int _mediumAnimation = 400;
        protected const int _slowAnimation = 800;

        /// <summary>
        /// List of operations that can be put on hold
        /// if requested while animation is running.
        /// </summary>
        protected enum CachedOperation
        {
            NONE,
            FIRST_MOVE,
            LAST_MOVE,
            NEXT_MOVE,
            PREV_MOVE,
            PAUSE_AUTO,
            PLAY_AUTO,
            NEXT_GAME,
            PREV_GAME,
            SELECT_GAME
        }

        // currently cached operation
        protected CachedOperation _cachedOperation;

        // list of game Ids to show
        protected List<string> _gameIdList = new List<string>();

        // id of the currently shown game
        protected string _currentGameId;

        /// <summary>
        /// Creates the dialog for previewing games from a list.
        /// </summary>
        /// <param name="gameId"></param>
        public GamePreviewDialog(string gameId, List<string> gameIdList)
        {
            InitializeComponent();
            _gameIdList = gameIdList;
            _currentGameId = gameId;

            ShowControls(false, false);
            _chessBoard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, false, false);
            _animator = new MoveAnimator(_chessBoard);

            _animationSpeed = _fastAnimation;
            UiRbFastReplay.IsChecked = true;
            _animator.SetAnimationSpeed(_animationSpeed);

            _animator.AnimationCompleted += AnimationFinished;
            UiCnvPlayers.Background = ChessForgeColors.TABLE_ROW_LIGHT_GRAY;
        }

        /// <summary>
        /// Sets up the labels with players' names.
        /// </summary>
        protected void PopulateHeaderLine()
        {
            UiLblWhiteSquare.Content = Constants.CharWhiteSquare;

            string white = _tree.Header.GetWhitePlayer(out _) ?? "";
            UiLblWhite.Content = white;
            UiLblWhite.FontWeight = FontWeights.Bold;
            UiLblWhite.ToolTip = white;

            UiLblBlackSquare.Content = Constants.CharBlackSquare;

            string black = _tree.Header.GetBlackPlayer(out _) ?? "";
            UiLblBlack.Content = black;
            UiLblBlack.FontWeight = FontWeights.Bold;
            UiLblBlack.ToolTip = black;

            string result = (_tree.Header.GetResult(out _) ?? "");
            UiLblResult.Content = result;
            UiLblResult.FontWeight = FontWeights.Bold;
        }

        /// <summary>
        /// Prepares replay of the selected game.
        /// </summary>
        protected void PlaySelectGame()
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.SELECT_GAME;
            }
            else
            {
                _isAnimating = false;
                DownloadGame(_currentGameId);
            }
        }

        //********************************************************
        //
        // MOVE ANIMATION
        //
        //********************************************************

        /// <summary>
        /// Request animation of the move at a given index in the
        /// Node list.
        /// </summary>
        /// <param name="moveIndex"></param>
        protected void RequestMoveAnimation(int moveIndex)
        {
            if (moveIndex > 0 && moveIndex < _mainLine.Count)
            {
                _animator.AnimateMove(_mainLine[moveIndex]);
                _isAnimating = true;
            }
            else
            {
                _isAnimating = false;
            }
            ShowControls(true, false);
        }

        /// <summary>
        /// Invoke when move animation finishes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void AnimationFinished(object sender, EventArgs e)
        {
            _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex], false);

            if (_pauseRequested)
            {
                _isAnimating = false;
                _pauseRequested = false;

                switch (_cachedOperation)
                {
                    case CachedOperation.FIRST_MOVE:
                        UiImgFirstMove_MouseDown(null, null);
                        break;
                    case CachedOperation.LAST_MOVE:
                        UiImgLastMove_MouseDown(null, null);
                        break;
                    case CachedOperation.PREV_MOVE:
                        UiImgPreviousMove_MouseDown(null, null);
                        break;
                    case CachedOperation.NEXT_MOVE:
                        UiImgNextMove_MouseDown(null, null);
                        break;
                    case CachedOperation.PAUSE_AUTO:
                        UiImgPause_MouseDown(null, null);
                        break;
                    case CachedOperation.PLAY_AUTO:
                        UiImgPlay_MouseDown(null, null);
                        break;
                    case CachedOperation.NEXT_GAME:
                        UiNextGame_Click(null, null);
                        break;
                    case CachedOperation.PREV_GAME:
                        UiPreviousGame_Click(null, null);
                        break;
                    case CachedOperation.SELECT_GAME:
                        PlaySelectGame();
                        break;
                }
                _cachedOperation = CachedOperation.NONE;
            }
            else if (_currentNodeMoveIndex < _mainLine.Count - 1)
            {
                _currentNodeMoveIndex++;
                RequestMoveAnimation(_currentNodeMoveIndex);
            }
            else
            {
                _isAnimating = false;
                ShowControls(true, false);
            }
        }


        //********************************************************
        //
        // HELPERS
        //
        //********************************************************

        /// <summary>
        /// Returns true if the currently viewed game is last on the list.
        /// </summary>
        /// <returns></returns>
        protected bool IsCurrentGameLast()
        {
            return _gameIdList.Count == 0 || _gameIdList[_gameIdList.Count - 1] == _currentGameId;
        }

        /// <summary>
        /// Returns true if the currently viewed game is first on the list.
        /// </summary>
        /// <returns></returns>
        protected bool IsCurrentGameFirst()
        {
            return _gameIdList.Count == 0 || _gameIdList[0] == _currentGameId;
        }

        /// <summary>
        /// Finds the requested game in the list.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        protected int FindGameIndex(string gameId)
        {
            int index = -1;

            for (int i = 0; i < _gameIdList.Count; i++)
            {
                if (_gameIdList[i] == gameId)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }


        //********************************************************
        //
        // USER MOVE CONTROLS
        //
        //********************************************************

        /// <summary>
        /// Shows/Hides controls.
        /// </summary>
        /// <param name="hasGame"></param>
        protected void ShowControls(bool hasGame, bool isError)
        {
            ShowGameControls(hasGame, isError);
            ShowMoveControls(hasGame);
        }

        /// <summary>
        /// Shows/Hides game operations related controls. 
        /// </summary>
        /// <param name="hasGame"></param>
        /// <param name="isError"></param>
        virtual protected void ShowGameControls(bool hasGame, bool isError)
        {
        }

        /// <summary>
        /// Shows/Hides move related controls.
        /// </summary>
        /// <param name="hasGame"></param>
        private void ShowMoveControls(bool hasGame)
        {
            UiImgFirstMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPreviousMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPlay.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPause.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgNextMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgLastMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;

            UiImgFirstMove.IsEnabled = _currentNodeMoveIndex > 1;
            UiImgPreviousMove.IsEnabled = _currentNodeMoveIndex > 1;
            UiImgNextMove.IsEnabled = _mainLine != null && (_currentNodeMoveIndex < _mainLine.Count - 1);
            UiImgLastMove.IsEnabled = _mainLine != null && (_currentNodeMoveIndex < _mainLine.Count - 1);

            UiLblNextGame.IsEnabled = !IsCurrentGameLast();
            UiLblPrevGame.IsEnabled = !IsCurrentGameFirst();

            if (hasGame)
            {
                ShowPlayPauseButtons();
            }
        }

        /// <summary>
        /// Shows/hides Play/Pause buttons depending on the state
        /// of animation.
        /// </summary>
        protected void ShowPlayPauseButtons()
        {
            UiImgPause.Visibility = _isAnimating ? Visibility.Visible : Visibility.Collapsed;
            UiImgPlay.Visibility = _isAnimating ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Pause button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PAUSE_AUTO;
            }
            else
            {
                ShowPlayPauseButtons();
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex], false);
            }
        }

        /// <summary>
        /// Play button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PLAY_AUTO;
            }
            else
            {
                UiImgPause.Visibility = Visibility.Visible;
                UiImgPlay.Visibility = Visibility.Collapsed;

                if (_currentNodeMoveIndex < _mainLine.Count - 1)
                {
                    _currentNodeMoveIndex++;
                    RequestMoveAnimation(_currentNodeMoveIndex);
                }
            }
        }

        /// <summary>
        /// Got to first move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgFirstMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.FIRST_MOVE;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                _currentNodeMoveIndex = 1;
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1], false);
            }
        }

        /// <summary>
        /// Go to one move back button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPreviousMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PREV_MOVE;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;

                if (_currentNodeMoveIndex > 1)
                {
                    _currentNodeMoveIndex--;
                    _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1], false);
                }
            }
        }

        /// <summary>
        /// Make the next move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgNextMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.NEXT_MOVE;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                if (_currentNodeMoveIndex < _mainLine.Count - 1)
                {
                    _currentNodeMoveIndex++;
                    _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1], false);
                }
            }
        }

        /// <summary>
        /// Got to last move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgLastMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.NEXT_MOVE;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                _currentNodeMoveIndex = _mainLine.Count - 1;
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex], false);
            }
        }

        //********************************************************
        //
        // REPLAY SPEED RADIO BUTTONS
        //
        //********************************************************

        /// <summary>
        /// Request fast speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbFastReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _fastAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        /// <summary>
        /// Request medium speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbMediumReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _mediumAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        /// <summary>
        /// Request slow speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbSlowReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _slowAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        //*************************************************************
        //
        // DIALOG CONTROL EVENTS
        //
        //*************************************************************


        /// <summary>
        /// User clicked Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExit_Click(object sender, RoutedEventArgs e)
        {
            _isExiting = true;
            Close();
        }


        /// <summary>
        /// The dialog is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isExiting = true;
        }


        //*************************************************************
        //
        // METHODS THAT DO NOTHING IN THE BASE CLASS
        //
        //*************************************************************

        /// <summary>
        /// Load a game.
        /// </summary>
        /// <param name="gameId"></param>
        virtual protected void DownloadGame(string gameId)
        {
        }

        /// <summary>
        /// The Import button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void UiBtnImport_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// The View on Lichess button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void UiLblViewOnLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        /// <summary>
        /// The Lichess logo was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void UiImgLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }
        /// <summary>
        /// Downloads and displayes the next game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void UiNextGame_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Downloads and displayes the previous game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void UiPreviousGame_Click(object sender, RoutedEventArgs e)
        {
        }

    }
}
