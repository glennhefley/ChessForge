﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages the RichTextBox showing the progress of the training session.
    /// The document has the following structure:
    /// - The "stem" paragraph is displayed at the top of the box.
    /// - The "instructions" paragraph follows
    /// - The initial prompt paragraph is shown before user's first move and is then deleted.
    /// - The paragraphs that follow show user moves and "coach's" responses with comment
    ///   paragraphs, optionally, in between.
    ///   The comment paragraph will in particular include wrokbook moves other than the move
    ///   made by the user.
    /// - If the user starts a game with the engine, an engine game paragraph will be created.
    ///   It will be removed when the game ends or is abandoned.
    /// - The history paragraph will record the list of session branches (when the user goes back and restarts training 
    ///   at a different move, the new session branch is created.)
    ///      
    /// Paragraphs and Runs are assigned names that are used to upadate / delete or use them 
    /// as position references.
    /// Runs with moves are named using a prefix specific to the type of paragraph they're in
    /// followed by a NodeId for easy identification. 
    /// </summary>
    public class TrainingView : RichTextBuilder
    {
        /// <summary>
        /// Types of paragraphs used in this document.
        /// There can only be one, or none, paragraph of a given type in the document
        /// </summary>
        private enum ParaType
        {
            INTRO,
            STEM,
            CONTINUATION,
            SESSION_HEADER,
            INSTRUCTIONS,
            PLAY_ENGINE_NOTE,
            PROMPT_TO_MOVE,
            USER_MOVE,
            WORKBOOK_MOVES
        }

        /// <summary>
        /// Maps paragraph types to Paragraph objects
        /// </summary>
        private Dictionary<ParaType, Paragraph> _dictParas = new Dictionary<ParaType, Paragraph>()
        {
            [ParaType.INTRO] = null,
            [ParaType.STEM] = null,
            [ParaType.CONTINUATION] = null,
            [ParaType.SESSION_HEADER] = null,
            [ParaType.INSTRUCTIONS] = null,
            [ParaType.PLAY_ENGINE_NOTE] = null,
            [ParaType.PROMPT_TO_MOVE] = null,
            [ParaType.USER_MOVE] = null,
            [ParaType.WORKBOOK_MOVES] = null
        };

        /// <summary>
        /// Keeps the order of paragraphs that is needed 
        /// e.g. when we search for the first non-null paragraph
        /// before a given one.
        /// </summary>
        private List<ParaType> _lstParaOrder = new List<ParaType>()
        {
            ParaType.INTRO,
            ParaType.STEM,
            ParaType.CONTINUATION,
            ParaType.SESSION_HEADER,
            ParaType.INSTRUCTIONS,
            ParaType.PLAY_ENGINE_NOTE,
            ParaType.PROMPT_TO_MOVE,
            ParaType.USER_MOVE,
            ParaType.WORKBOOK_MOVES
        };

        /// <summary>
        /// IDs of button styles (here, buttons are highlighted clickable Runs
        /// </summary>
        private enum ButtonStyle
        {
            BLACK,
            BLUE,
            GREEN,
            RED
        }

        /// <summary>
        /// The paragraph holding the text of the current game
        /// against the engine if one is in progress.
        /// </summary>
        private Paragraph _paraCurrentEngineGame;

        /// <summary>
        /// Number of moves made in the game agains the engine.
        /// This is used to decide when to start a new line in the
        /// Engine Game paragraph.
        /// </summary>
        private int _currentEngineGameMoveCount;

        private TreeNode _engineGameRootNode;

        /// <summary>
        /// The Run with the move for which engine evaluation
        /// is running.
        /// </summary>
        private Run _underEvaluationRun;

        /// <summary>
        /// The list of moves found in the Workbook in the current position,
        /// except the move made by the user (if it was a Workbook move).
        /// </summary>
        private List<TreeNode> _otherMovesInWorkbook = new List<TreeNode>();

        /// <summary>
        /// The user's move being currently examined.
        /// </summary>
        private TreeNode _userMove;

        private TreeNode _lastClickedNode;
        /// <summary>
        /// The move with which the user wishes to proceed after selecting
        /// from the Workbook options.
        /// </summary>
        private int _userChoiceNodeId;

        /// <summary>
        /// The node from which we start the training session.
        /// </summary>
        private TreeNode _sessionStartNode;

        private int _nodeIdUnderEvaluation = -1;

        /// <summary>
        /// Names and prefixes for Runs.
        /// NOTE: prefixes that are to be followed by NodeId 
        /// must end with the undesrscore character.
        /// This faciliates easy parsing of the NodeId when the run's 
        /// prefix is not that important e.g. when showing the floating
        /// chessboard.
        /// </summary>
        private readonly string _run_eval_user_move = "user_eval";
        private readonly string _run_play_engine = "play_engine";
        private readonly string _run_eval_wb_move_ = "wb_eval_";
        private readonly string _run_eval_results_ = "eval_results_";
        private readonly string _run_user_eval_results = "user_eval_results";
        private readonly string _run_play_wb_move_ = "wb_play_";

        private readonly string _run_engine_game_move_ = "eng_game_";
        private readonly string _run_wb_move_ = "wb_move_";

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="doc"></param>
        public TrainingView(FlowDocument doc) : base(doc)
        {
        }

        /// <summary>
        /// Property referencing definitions of Paragraphs 
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_MOVES_MAIN = "moves_main";
        private static readonly string STYLE_COACH_NOTES = "coach_notes";
        private static readonly string STYLE_ENGINE_EVAL = "engine_eval";
        private static readonly string STYLE_ENGINE_GAME = "engine_game";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["intro"] = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            ["first_prompt"] = new RichTextPara(10, 20, 16, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            ["second_prompt"] = new RichTextPara(10, 0, 14, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            ["play_engine_note"] = new RichTextPara(10, 10, 16, FontWeights.Bold, Brushes.Black, TextAlignment.Left, Brushes.Black),
            ["stem_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            ["continuation"] = new RichTextPara(0, 20, 12, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left, Brushes.Gray),
            ["session_header"] = new RichTextPara(10, 10, 16, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left, Brushes.Black),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Left),
            ["normal"] = new RichTextPara(10, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),

            [STYLE_MOVES_MAIN] = new RichTextPara(10, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_COACH_NOTES] = new RichTextPara(50, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_ENGINE_EVAL] = new RichTextPara(80, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_ENGINE_GAME] = new RichTextPara(50, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
        };

        private void InitParaDictionary()
        {
            _dictParas[ParaType.INTRO] = null;
            _dictParas[ParaType.STEM] = null;
            _dictParas[ParaType.CONTINUATION] = null;
            _dictParas[ParaType.PLAY_ENGINE_NOTE] = null;
            _dictParas[ParaType.INSTRUCTIONS] = null;
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
            _dictParas[ParaType.USER_MOVE] = null;
            _dictParas[ParaType.WORKBOOK_MOVES] = null;
        }

        /// <summary>
        /// Initializes the state of the view.
        /// Sets the starting node of the training session
        /// and shows the intro text.
        /// </summary>
        /// <param name="node"></param>
        public void Initialize(TreeNode node)
        {
            _sessionStartNode = node;
            //_sessionStartNodeIndex = (int)(node.MoveNumber * 2 - (node.ColorToMove == PieceColor.Black ? -1 : 0));
            Document.Blocks.Clear();

            InitParaDictionary();

            BuildIntroText(node);
        }

        /// <summary>
        /// This method is invoked when user makes their move.
        /// 
        /// It dets the last play from the EngineGame.Line (which is the
        /// move made by the user) and finds its parent in the Workbook.
        /// 
        /// NOTE: If the parent is not in the Workbook, this method should
        /// not have been invoked as the TrainingMode should know that
        /// we are "out of the book".
        /// 
        /// Having found the parent, checks if the user's move corresponds to any move
        /// in the Workbook (i.e. children of that parent) and report accordingly)
        /// </summary>
        public void ReportLastMoveVsWorkbook()
        {
            RemoveIntroParas();

            _otherMovesInWorkbook.Clear();

            _userMove = EngineGame.GetCurrentNode();
            TreeNode parent = _userMove.Parent;

            // double check that we have the parent in our Workbook
            if (AppState.MainWin.Workbook.GetNodeFromNodeId(parent.NodeId) == null)
            {
                // we are "out of the book" in our training so there is nothing to report
                return;
            }

            int childCount = parent.Children.Count;
            StringBuilder wbMoves = new StringBuilder();
            TreeNode foundMove = null;
            foreach (TreeNode child in parent.Children)
            {
                if (child.LastMoveEngineNotation == _userMove.LastMoveEngineNotation && !_userMove.IsNewTrainingMove)
                {
                    // replace the TreeNode with the one from the Workbook so that
                    // we stay with the workbook as long as the user does.
                    EngineGame.ReplaceCurrentWithWorkbookMove(child);
                    foundMove = child;
                    _userMove = child;
                }
                else
                {
                    if (!child.IsNewTrainingMove)
                    {
                        wbMoves.Append(child.GetPlyText(true));
                        wbMoves.Append("; ");
                        _otherMovesInWorkbook.Add(child);
                    }
                }
            }

            if (_userMove.NodeId < 0)
            {
                _userMove.NodeId = AppState.MainWin.Workbook.GetNewNodeId();
                _userMove.IsNewTrainingMove = true;
                AppState.MainWin.Workbook.AddNode(_userMove);
            }
            BuildMoveParagraph(_userMove, true);
            BuildCommentParagraph(foundMove != null);

            if (foundMove != null)
            {
                // start the timer that will trigger a workbook response by RequestWorkbookResponse()
                AppState.MainWin.Timers.Start(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);
            }
            else
            {
                _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
                _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine has started. Wait for the engine\'s move..."));
                _engineGameRootNode = _userMove;
                // call RequestEngineResponse() directly so it invokes PlayEngine
                RequestEngineResponse();
            }
        }

        /// <summary>
        /// When the user made their move, and the training is in
        /// manual mode (as opposed to a game vs engine)
        /// a timer was started to invoke
        /// this method (via InvokeRequestWorkbookResponse).
        /// This method performs the move and starts the timer
        /// so that is gets picked up by EngineGame.CheckForTrainingWorkbookMoveMade.
        /// </summary>
        public void RequestWorkbookResponse()
        {
            int nodeId = _userMove.NodeId;
            AppState.MainWin.Timers.Stop(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);

            // user may have chosen a different move to what we originally had
            // TODO: after the re-think of the GUI that probably cannot happen (?)
            EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);

            TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
            SoundPlayer.PlayMoveSound(userChoiceNode.LastMoveAlgebraicNotation);
            _userChoiceNodeId = nodeId;

            AppState.MainWin.DisplayPosition(userChoiceNode.Position);

            TreeNode nd = AppState.MainWin.Workbook.SelectRandomChild(nodeId);

            // Selecting a random response to the user's choice from the Workbook
            EngineGame.AddPlyToGame(nd);

            // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
            EngineGame.TrainingWorkbookMoveMade = true;
            AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
            AppState.MainWin.SwapCommentBoxForEngineLines(false);
        }

        /// <summary>
        /// This method is called directly when the user
        /// made their move and the training is in engine game mode
        /// (as opposed to the manual mode).
        /// This method requests the engine to make a move.
        /// </summary>
        public void RequestEngineResponse()
        {
            int nodeId = _userMove.NodeId;
            AppState.MainWin.PlayComputer(_userMove, true);
        }

        /// <summary>
        /// This is called from MainWindow.MoveEvaluationFinished()
        /// when engine produced a move while playing with the user.
        /// </summary>
        public void EngineMoveMade()
        {
            AddMoveToEngineGamePara(EngineGame.GetCurrentNode());
        }

        /// <summary>
        /// This is called from EngineGame.ProcessUserGameMove()
        /// when engine produced a move while playing with the user.
        /// </summary>
        public void UserMoveMade()
        {
            AddMoveToEngineGamePara(EngineGame.GetCurrentNode());
        }

        /// <summary>
        /// Builds text for intorductory paragraphs.
        /// </summary>
        /// <param name="node"></param>
        private void BuildIntroText(TreeNode node)
        {
            BuildStemText(node);
            BuildInstructionsText();
        }

        private void AddMoveToEngineGamePara(TreeNode nd)
        {
            if (_paraCurrentEngineGame == null)
            {
                // should never be null here so this is just defensive
                _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
            }

            string text = "";
            if (_currentEngineGameMoveCount == 0)
            {
                _paraCurrentEngineGame.Inlines.Clear();
                _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine is in progress...\n"));
                text = "          " + MoveUtils.BuildSingleMoveText(nd, true) + " ";
            }
            else
            {
                text = MoveUtils.BuildSingleMoveText(nd, false) + " ";
                if (_currentEngineGameMoveCount % 8 == 0)
                {
                    text = "\n          " + text;
                }
            }

            Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
            _paraCurrentEngineGame.Inlines.Add(gm);

            _currentEngineGameMoveCount++;

        }

        private void RebuildEngineGamePara(TreeNode toNode)
        {
            if (_paraCurrentEngineGame == null)
            {
                _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
            }
            TreeNode nd = _engineGameRootNode.Children[0];
            _currentEngineGameMoveCount = 0;

            string text = "";
            _paraCurrentEngineGame.Inlines.Clear();
            _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine is in progress...\n"));
            text = "          " + MoveUtils.BuildSingleMoveText(nd, true) + " ";
            Run r_root = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
            _paraCurrentEngineGame.Inlines.Add(r_root);

            _currentEngineGameMoveCount++;

            while (nd.Children.Count > 0)
            {
                nd = nd.Children[0];
                _currentEngineGameMoveCount++;
                text = MoveUtils.BuildSingleMoveText(nd, false) + " ";
                if (_currentEngineGameMoveCount % 8 == 0)
                {
                    text = "\n          " + text;
                }
                Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
                _paraCurrentEngineGame.Inlines.Add(gm);
            };
        }

        /// <summary>
        /// This method will be invoked when we requested evaluation and got the results back.
        /// The EngineMessageProcessor has the results.
        /// We will create a new Run and insert it after the Run that reported
        /// the move with a "play" option. Reference to that run was preserved
        /// when calling the engine evaluation method.
        /// </summary>
        public void ShowEvaluationResult()
        {
            // restore the position we are stopped at
            AppState.MainWin.DisplayPosition(_userMove.Position);

            if (_underEvaluationRun == null)
                return;

            List<MoveEvaluation> moveCandidates = AppState.MainWin.EngineLinesGUI.Lines;
            int maxCandidates = Math.Min(5, moveCandidates.Count);

            StringBuilder sb = new StringBuilder();
            sb.Append("\n          Evaluation summary:\n");
            for (int i = 0; i < maxCandidates; i++)
            {
                TreeNode nd;

                if (_nodeIdUnderEvaluation >= 0)
                {
                    nd = AppState.MainWin.Workbook.GetNodeFromNodeId(_nodeIdUnderEvaluation);
                }
                else
                {
                    nd = _userMove;
                }

                bool dummy;
                if (i > 0)
                {
                    sb.Append("\n");
                }

                int intEval = nd.Position.ColorToMove == PieceColor.White ? moveCandidates[i].ScoreCp : -1 * moveCandidates[i].ScoreCp;
                sb.Append("               " + (i + 1).ToString() + ". (" + (((double)intEval) / 100.0).ToString("F2") + ") ");
                BoardPosition workingPosition = new BoardPosition(nd.Position);
                if (workingPosition.ColorToMove == PieceColor.White)
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + ".");
                }
                else
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + "...");
                }
                string move = MoveUtils.EngineNotationToAlgebraic(moveCandidates[i].GetCandidateMove(), ref workingPosition, out dummy);
                sb.Append(move);
            }

            AppState.MainWin._rtbTrainingBrowse.Dispatcher.Invoke(() =>
            {
                InsertEvaluationResultsRun(sb.ToString());
            });
        }

        /// <summary>
        /// Builds a paragraph containing just one move with NAGs and Comments/Commands if any. 
        /// </summary>
        private void BuildMoveParagraph(TreeNode nd, bool userMove)
        {
            string paraName = "p_moves_" + nd.NodeId.ToString();
            string runName = "r_moves_" + nd.NodeId.ToString();

            Paragraph para = AddNewParagraphToDoc(STYLE_MOVES_MAIN, "");
            para.Name = paraName;

            Run r_prefix = new Run();
            if (userMove)
            {
                r_prefix.Text = "\nYou played:   ";
            }
            else
            {
                r_prefix.Text = "\nCoach's response:   ";
            }
            r_prefix.FontWeight = FontWeights.Normal;

            para.Inlines.Add(r_prefix);

            Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true), runName, Brushes.Black);
            para.Inlines.Add(r);
        }


        /// <summary>
        /// Inserts new evaluation results for the requested move.
        /// Clears previous results if exist.
        /// </summary>
        /// <param name="text"></param>
        private void InsertEvaluationResultsRun(string text)
        {
            string runName;
            Run runToDelete = null;

            bool runInUserPara = false;

            if (_nodeIdUnderEvaluation >= 0)
            {
                runName = _run_eval_results_ + _nodeIdUnderEvaluation.ToString();

                if (_dictParas[ParaType.WORKBOOK_MOVES] != null)
                {
                    runToDelete = _dictParas[ParaType.WORKBOOK_MOVES].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                    if (runToDelete != null)
                    {
                        _dictParas[ParaType.WORKBOOK_MOVES].Inlines.Remove(runToDelete);
                    }
                }

                if (runToDelete == null)
                {
                    runToDelete = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                    _dictParas[ParaType.USER_MOVE].Inlines.Remove(runToDelete);
                    runInUserPara = true;
                }

            }
            else
            {
                runName = _run_user_eval_results;
                runToDelete = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                _dictParas[ParaType.USER_MOVE].Inlines.Remove(runToDelete);
            }

            Run evalRun = new Run(text);
            evalRun.Name = runName;
            if (_nodeIdUnderEvaluation >= 0 && !runInUserPara)
            {
                _dictParas[ParaType.WORKBOOK_MOVES].Inlines.InsertAfter(_underEvaluationRun, evalRun);
            }
            else
            {
                _dictParas[ParaType.USER_MOVE].Inlines.InsertAfter(_underEvaluationRun, evalRun);
            }
        }


        /// <summary>
        /// Builds "intro" and "stem line" paragraphs that are always visible at the top
        /// of the view.
        /// </summary>
        /// <param name="node"></param>
        private void BuildStemText(TreeNode node)
        {
            _dictParas[ParaType.STEM] = AddNewParagraphToDoc("stem_line", null);

            Run r_prefix = new Run("\nThis training session with your virtual Coach starts from the position arising after: ");
            r_prefix.FontWeight = FontWeights.Normal;
            _dictParas[ParaType.STEM].Inlines.Add(r_prefix);

            Run r_stem = new Run(GetStemLineText(node));
            _dictParas[ParaType.STEM].Inlines.Add(r_stem);
        }

        /// <summary>
        /// Initial prompt to advise the user make their move.
        /// This paragraph is removed later on to reduce clutter.
        /// </summary>
        private void BuildInstructionsText()
        {
            StringBuilder sbInstruction = new StringBuilder();
            sbInstruction.Append("You will be making moves for");
            sbInstruction.Append(_sessionStartNode.ColorToMove == PieceColor.White ? " White " : " Black ");
            sbInstruction.Append("and the program (a.k.a. the \"Coach\") will respond for");
            sbInstruction.Append(_sessionStartNode.ColorToMove == PieceColor.White ? " Black." : " White.");
            sbInstruction.AppendLine();
            sbInstruction.Append("The Coach will comment on your every move based on the content of the Workbook.\n");
            sbInstruction.Append("\nRemember that you can:\n");
            sbInstruction.Append("- click on an alternative move in the Coach's comment to play it instead of your original choice,\n");
            sbInstruction.Append("- right click on any move to invoke a context menu where, among other options, you can request engine evaluation of the move.\n");

            _dictParas[ParaType.INSTRUCTIONS] = AddNewParagraphToDoc("intro", sbInstruction.ToString());

            _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc("first_prompt", "To begin, make your first move on the chessboard.");
        }

        private void BuildSecondPromptParagraph()
        {
            TreeNode nd = EngineGame.GetCurrentNode();
            BuildMoveParagraph(nd, false);

            Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc("second_prompt", "\n   Your turn...");
        }

        private void BuildCommentParagraph(bool isWorkbookMove)
        {
            string paraName = "p_coach_" + _userMove.NodeId.ToString();

            Paragraph para = AddNewParagraphToDoc(STYLE_COACH_NOTES, "");
            para.Name = paraName;

            Run coach = new Run("Coach says:");
            coach.TextDecorations = TextDecorations.Underline;
            para.Inlines.Add(coach);

            string txt = "  ";
            if (_otherMovesInWorkbook.Count == 0)
            {
                txt += "The move you made is the only move in the Workbook.";
                Run r_only = new Run(txt);
                para.Inlines.Add(r_only);
            }
            else
            {

                if (!isWorkbookMove)
                {
                    txt += "This is not in the Workbook. ";
                    Run r_notWb = new Run(txt);
                    para.Inlines.Add(r_notWb);

                    txt = "";
                }

                if (!isWorkbookMove)
                {
                    if (_otherMovesInWorkbook.Count == 1)
                    {
                        txt += "The only Workbook move is ";
                    }
                    else
                    {
                        txt += "The Workbook moves are ";
                    }
                    Run r_wb = new Run(txt);
                    para.Inlines.Add(r_wb);
                }
                else
                {
                    if (_otherMovesInWorkbook.Count == 1)
                    {
                        txt += "The only other Workbook move is ";
                    }
                    else
                    {
                        txt += "Other Workbook moves are ";
                    }
                    Run r_wb = new Run(txt);
                    para.Inlines.Add(r_wb);
                }

                BuildOtherWorkbookMovesRun(para);
            }
        }

        private void BuildOtherWorkbookMovesRun(Paragraph para)
        {
            foreach (TreeNode nd in _otherMovesInWorkbook)
            {
                para.Inlines.Add(CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true) + "; ", _run_wb_move_ + nd.NodeId.ToString(), Brushes.Green));
            }
        }



        /// <summary>
        /// Creates a highlighted, clickable text that here we refer to
        /// as a "button".
        /// </summary>
        /// <param name="text"></param>
        /// <param name="runName"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        private Run CreateButtonRun(string text, string runName, Brush color)
        {
            Run r = new Run(text);
            r.Name = runName;

            r.Foreground = color;

            r.FontWeight = FontWeights.Bold;
            r.MouseDown += EventRunClicked;
            r.MouseMove += EventRunMoveOver;
            r.Cursor = Cursors.Hand;

            return r;
        }

        public void ShowPopupMenu()
        {
            AppState.MainWin.ShowFloatingChessboard(false);

            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                string midTxt;
                if (_lastClickedNode.ColorToMove != AppState.TrainingSide)
                {
                    midTxt = " your ";
                }
                else
                {
                    midTxt = " engine\'s ";
                }
                // this is user move - adjust menu text accordingly

                ContextMenu cm = AppState.MainWin.FindResource("_cmTrainingEngineGame") as ContextMenu;
                foreach (object o in cm.Items)
                {
                    if (o is MenuItem)
                    {
                        MenuItem mi = o as MenuItem;
                        switch (mi.Name)
                        {
                            case "_mnEvalMove":
                                mi.Header = "Evaluate" + midTxt + "move " + MoveUtils.BuildSingleMoveText(_lastClickedNode, true);
                                break;
                            case "_mnRestartGame":
                                mi.Header = "Restart game after" + midTxt + "move " + MoveUtils.BuildSingleMoveText(_lastClickedNode, true);
                                mi.Click += RestartGameAfter;
                                break;
                        }
                    }
                }
                cm.PlacementTarget = AppState.MainWin._rtbTrainingProgress;
                cm.IsOpen = true;
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUMENU);
            });
        }

        public void RestartGameAfter(object sender, RoutedEventArgs e)
        {
            TreeNode nd = _lastClickedNode;
            if (nd != null)
            {
                if (_lastClickedNode.ColorToMove != AppState.TrainingSide)
                {
                    EngineGame.RestartAtUserMove(nd);
                }
                else
                {
                    EngineGame.RestartAtEngineMove(nd);
                }
                AppState.MainWin.DisplayPosition(nd.Position);
                RebuildEngineGamePara(nd);
            }
        }

        /// <summary>
        /// Based on the name of the clicked run, performs an action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            if (r.Name.StartsWith(_run_eval_wb_move_))
            {
                // get id of the node
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);

                _nodeIdUnderEvaluation = nodeId;
                // find Run for the same NodeId with "play" option,
                // we want to remember the reference so that we can append
                // the evaluation results run after it
                _underEvaluationRun = GetPlayRunForNodeId(nodeId);
                AppState.MainWin.DisplayPosition(userChoiceNode.Position);

                AppState.MainWin.RequestMoveEvaluationInTraining(nodeId);
            }
            else if (r.Name.StartsWith(_run_engine_game_move_))
            {
                // a move from a game against the engine was clicked,
                // we take the game back to that move.
                // If it is an engine move, the user will be required to respond,
                // otherwise it will be engine's turn.
                int nodeId = GetNodeIdFromRunName(r.Name, _run_engine_game_move_);
                TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
                _lastClickedNode = nd;

                string moveTxt = MoveUtils.BuildSingleMoveText(nd, true);

                AppState.MainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUMENU);
                //if (nd.ColorToMove != AppState.TrainingSide)
                //{
                //    if (MessageBox.Show("Restart the game after your " + moveTxt + " move?", "Training Game with Engine", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                //    {
                //        // a user move clicked so keep it and get a response  from the engine
                //        EngineGame.RestartAtUserMove(nd);
                //        AppState.MainWin.DisplayPosition(nd.Position);
                //        RebuildEngineGamePara(nd);
                //    }
                //}
                //else
                //{
                //    if (MessageBox.Show("Restart the game after engine\'s " + moveTxt + " move?", "Training Game with Engine", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                //    {
                //        // an engine move clicked so keep it and ask user for their move
                //        EngineGame.RestartAtEngineMove(nd);
                //        AppState.MainWin.DisplayPosition(nd.Position);
                //        RebuildEngineGamePara(nd);
                //    }
                //}
            }
            else if (r.Name == _run_eval_user_move)
            {
                _underEvaluationRun = GetPlayRunForNodeId(-1);
                _nodeIdUnderEvaluation = -1;

                AppState.MainWin.RequestMoveEvaluationInTraining(_userMove);
            }
            else if (r.Name.StartsWith(_run_play_wb_move_))
            {
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);
                TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
                SoundPlayer.PlayMoveSound(userChoiceNode.LastMoveAlgebraicNotation);
                _userChoiceNodeId = nodeId;

                ClearDecisionParas();

                AppState.MainWin.DisplayPosition(userChoiceNode.Position);

                TreeNode nd = AppState.MainWin.Workbook.SelectRandomChild(nodeId);

                // Selecting a random response to the user's choice from the Workbook
                EngineGame.AddPlyToGame(nd);

                // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
                EngineGame.TrainingWorkbookMoveMade = true;
                AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
                AppState.MainWin.SwapCommentBoxForEngineLines(false);
            }
            else if (r.Name == _run_play_engine)
            {
                AppState.MainWin.SwapCommentBoxForEngineLines(false);
                Document.Blocks.Remove(_dictParas[ParaType.USER_MOVE]);
                Document.Blocks.Remove(_dictParas[ParaType.WORKBOOK_MOVES]);
                _dictParas[ParaType.PLAY_ENGINE_NOTE] = AddNewParagraphToDoc("play_engine_note", "\nYou are now playing against the engine.", NonNullParaAtOrBefore(ParaType.INSTRUCTIONS));
                AppState.MainWin.PlayComputer(_userMove, true);
            }
        }

        private void EventRunMoveOver(object sender, MouseEventArgs e)
        {
            // check if a move run was clicked
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            int underscore = r.Name.LastIndexOf('_');
            if (underscore < 0 || underscore == r.Name.Length - 1)
            {
                return;
            }

            int nodeId;
            if (int.TryParse(r.Name.Substring(underscore + 1), out nodeId))
            {
                Point pt = e.GetPosition(AppState.MainWin._rtbTrainingProgress);
                AppState.MainWin.TrainingViewChessBoard.DisplayPosition(AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId).Position);
                AppState.MainWin._vbFloatingChessboard.Margin = new Thickness(pt.X, pt.Y - 165, 0, 0);
                AppState.MainWin._vbFloatingChessboard.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// This method will be called when the program made a move 
        /// and we want to update the Training view.
        /// </summary>
        public void WorkbookMoveMade()
        {
            AppState.MainWin._rtbTrainingProgress.Dispatcher.Invoke(() =>
            {
                BuildSecondPromptParagraph();
            });
        }

        /// <summary>
        /// Given a run name and a prefix, returns the NodeId
        /// associated with the run.
        /// The run name is expected to be in the form prefix + NodeId.
        /// </summary>
        /// <param name="runName"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        int GetNodeIdFromRunName(string runName, string prefix)
        {
            string sId = runName.Substring(prefix.Length);
            int iId = int.Parse(sId);
            return iId;
        }

        /// <summary>
        /// Returns the Run with the "button" to play the move
        /// from a given NodeId.
        /// We will use it as refrence after which 
        /// to insert the evaluation results.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private Run GetPlayRunForNodeId(int nodeId)
        {
            if (nodeId >= 0)
            {
                string runName = _run_play_wb_move_ + nodeId.ToString();
                Run r = null;
                if (_dictParas[ParaType.WORKBOOK_MOVES] != null)
                {
                    r = _dictParas[ParaType.WORKBOOK_MOVES].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                }

                if (r == null)
                {
                    r = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                }
                return r;

            }
            else
            {
                return _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == _run_play_engine) as Run;
            }
        }

        /// <summary>
        /// Removes all paragraphs related to the move decisions.
        /// This method is called once the user has chosen the move.
        /// </summary>
        private void ClearDecisionParas()
        {
            Document.Blocks.Remove(_dictParas[ParaType.WORKBOOK_MOVES]);
            Document.Blocks.Remove(_dictParas[ParaType.USER_MOVE]);
        }

        /// <summary>
        /// It the Paragraph of the passed type is not null,
        /// the reference to it is returned back.
        /// If it is null, then the first non-null paragraph
        /// above it in the document's hierarchy is returned.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Paragraph NonNullParaAtOrBefore(ParaType pt)
        {
            bool ptFound = false;
            Paragraph para = null;
            for (int i = _lstParaOrder.Count - 1; i >= 0; i--)
            {
                if (_lstParaOrder[i] == pt)
                {
                    ptFound = true;
                }

                if (ptFound)
                {
                    para = _dictParas[_lstParaOrder[i]];
                    if (para != null)
                        break;
                }
            }

            return para;
        }

        /// <summary>
        /// Removes the introductory paragraphs
        /// that are shown only at the start of the session.
        /// </summary>
        private void RemoveIntroParas()
        {
            Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
        }
    }
}
