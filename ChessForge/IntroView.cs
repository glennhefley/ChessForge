﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using ChessPosition;
using System.Windows.Controls;
using GameTree;
using System.Windows.Media;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Intro Tab view with RichTextBox  
    /// </summary>
    public class IntroView : RichTextBuilder
    {
        /// <summary>
        /// Not needed in this class
        /// but required for the calss dervied from RichTextBuilder.
        /// </summary>
        internal override Dictionary<string, RichTextPara> RichTextParas => throw new NotImplementedException();

        /// <summary>
        /// The selected node.
        /// If no previous selection, returns the root node.
        /// </summary>
        public TreeNode SelectedNode
        {
            get => _selectedNode ?? Nodes[0];
        }

        /// <summary>
        /// The list of diagrams in this view.
        /// </summary>
        private List<IntroViewDiagram> DiagramList = new List<IntroViewDiagram>();

        /// <summary>
        /// Names and prefixes for xaml elements.
        /// </summary>
        private readonly string _run_move_ = "run_move_";
        private readonly string _tb_move_ = "tb_move_";
        private readonly string _uic_move_ = "uic_move_";
        private readonly string _para_diagram_ = "para_diag_";

        // current highest run id (it is 0 initially, because we have the root node)
        private int _maxRunId = 0;

        /// <summary>
        /// List of nodes currently represented in the view.
        /// </summary>
        private List<TreeNode> Nodes
        {
            get => Intro.Tree.Nodes;
        }

        // currently selected node
        private TreeNode _selectedNode;

        // refrence to the RichTextBox of this view.
        private RichTextBox _rtb = AppState.MainWin.UiRtbIntroView;

        // flag to use to prevent unnecessary saving after the load.
        private bool _ignoreTextChange = false;

        /// <summary>
        /// Constructor. Builds the content if not empty.
        /// Initializes data structures.
        /// </summary>
        public IntroView(Chapter parentChapter) : base(AppState.MainWin.UiRtbIntroView.Document)
        {
            _rtb.Document.Blocks.Clear();
            _rtb.IsDocumentEnabled = true;
            _rtb.AllowDrop = false;

            ParentChapter = parentChapter;

            // set the event handler for text changes.
            _rtb.TextChanged += UiRtbIntroView_TextChanged;
            if (!string.IsNullOrEmpty(Intro.Tree.RootNode.Data))
            {
                _ignoreTextChange = true;
                LoadXAMLContent();
            }
            Nodes[0].Position = PositionUtils.SetupStartingPosition();
            foreach (var node in Nodes)
            {
                if (node.NodeId > _maxRunId)
                {
                    _maxRunId = node.NodeId;
                }
            }
        }

        /// <summary>
        /// Clear the content of the RTB
        /// </summary>
        public void Clear()
        {
            _ignoreTextChange = true;
            _rtb.Document.Blocks.Clear();
        }

        /// <summary>
        /// Chapter for which this view was created.
        /// </summary>
        public Chapter ParentChapter { get; set; }

        /// <summary>
        /// The Intro article shown in the view.
        /// </summary>
        public Article Intro
        {
            get => ParentChapter.Intro;
        }

        /// <summary>
        /// Loads content of the view
        /// </summary>
        public void LoadXAMLContent()
        {
            if (!string.IsNullOrEmpty(Intro.CodedContent))
            {
                string xaml = EncodingUtils.Base64Decode(Intro.CodedContent);
                _rtb.Document = StringToFlowDocument(xaml);
            }
        }

        /// <summary>
        /// Saves the content of the view into the root node of the view.
        /// </summary>
        /// <returns></returns>
        public void SaveXAMLContent()
        {
            string xamlText = XamlWriter.Save(_rtb.Document);
            Nodes[0].Data = EncodingUtils.Base64Encode(xamlText);
        }

        /// <summary>
        /// Inserts new move at the caret.
        /// This function is invoked when the user made a move on the main chessboard.
        /// TODO: try guessing the move number based on what number we see in the preceding paragraphs.
        /// </summary>
        /// <param name="node"></param>
        public void InsertMove(TreeNode node)
        {
            if (string.IsNullOrEmpty(node.LastMoveAlgebraicNotation))
            {
                return;
            }

            _selectedNode = node;
            int nodeId = AddNode(node);
            
            Run rMove = new Run();
            rMove.Name = _run_move_ + nodeId.ToString(); 
            rMove.Text = node.LastMoveAlgebraicNotation;
            rMove.Text = Languages.MapPieceSymbols(node.LastMoveAlgebraicNotation);
            // TODO: TRANSLATE back when saving ?!
            rMove.Foreground = Brushes.Blue;

            rMove.MouseDown += EventMoveClicked;
            InsertMoveTextBlock(rMove, nodeId);
        }

        /// <summary>
        /// Returns the Node with the passed id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private TreeNode GetNodeById(int nodeId)
        {
            return Nodes.FirstOrDefault(x => x.NodeId == nodeId);
        }

        /// <summary>
        /// Handles the click move event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventMoveClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Run)
            {
                try
                {
                    Run r = e.Source as Run;
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                    TreeNode nd = GetNodeById(nodeId);
                    if (nd != null)
                    {
                        Inline runClicked = FindInlineByName(r.Name);
                        _rtb.CaretPosition = runClicked.ElementEnd;
                        AppState.MainWin.DisplayPosition(nd);

                        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                        {
                            // TODO: allow text edit and the option to edit position
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Invokes the Diagram Setup dialog,
        /// creates GUI objects for the position
        /// and inserts in the document.
        /// </summary>
        public void CreateDiagram()
        {
            try
            {
                TextPointer tp = _rtb.CaretPosition.InsertParagraphBreak();
                Paragraph nextPara = tp.Paragraph;

                _selectedNode = AppState.MainWin.MainChessBoard.DisplayedNode;
                DiagramSetupDialog dlg = new DiagramSetupDialog(SelectedNode)
                {
                    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                    Top = AppState.MainWin.Top + 100,
                    Topmost = false,
                    Owner = AppState.MainWin
                };

                if (dlg.ShowDialog() == true)
                {
                    BoardPosition pos = dlg.PositionSetup;
                    TreeNode node = new TreeNode(null, "", 0);
                    node.Position = new BoardPosition(pos);
                    
                    int node_id = AddNode(node);
                    _selectedNode = node;

                    IntroViewDiagram diag = new IntroViewDiagram();
                    Paragraph para = BuildDiagramParagraph(diag, node);
                    diag.Chessboard.DisplayPosition(node, false);
                    diag.Node = node;

                    DiagramList.Add(diag);

                    AppState.MainWin.DisplayPosition(node);

                    AppState.IsDirty = true;

                    _rtb.Document.Blocks.InsertBefore(nextPara, para);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateDiagram()", ex);
            }
        }

        /// <summary>
        /// Inserts a Run into a TextBlock that is then inserted into the Document.
        /// </summary>
        /// <param name="run"></param>
        private void InsertMoveTextBlock(Run run, int nodeId)
        {
            try
            {
                TextPointer tp = _rtb.CaretPosition;
                TextBlock tbMove = new TextBlock();
                tbMove.Name = _tb_move_ + nodeId.ToString();

                run.Text = " " + run.Text + " ";
                tbMove.Inlines.Add(run);
                tbMove.Background = ChessForgeColors.INTRO_MOVE_BACKGROUND;

                InlineUIContainer uic = new InlineUIContainer();
                uic.Name = _uic_move_ + nodeId.ToString();
                uic.Child = tbMove;

                // Insert the new Run after the original Run
                Run newRun = SplitRun(_rtb);
                if (newRun != null)
                {
                    Paragraph para = newRun.Parent as Paragraph;
                    para.Inlines.InsertBefore(newRun, uic as Inline);
                }
                else
                {
                    Run adjRun = GetRunUnderCaret(_rtb);
                    if (adjRun == null)
                    {
                        if (tp.Paragraph != null)
                        {
                            tp.Paragraph.Inlines.Add(uic);
                        }
                        else
                        {
                            tp = _rtb.CaretPosition.InsertParagraphBreak();
                            tp.Paragraph.Inlines.Add(uic);
                        }
                    }
                    else
                    {
                        Paragraph para = newRun.Parent as Paragraph;
                        para.Inlines.InsertBefore(newRun, uic as Inline);
                    }
                }

                _rtb.CaretPosition = uic.ElementEnd;
                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertMoveTextBlock()", ex);
            }
        }

        /// <summary>
        /// Builds a paragraph with the diagram.
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Paragraph BuildDiagramParagraph(IntroViewDiagram diag, TreeNode nd)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(20, 20, 0, 20);
            para.Name = _para_diagram_ + nd.NodeId.ToString();

            Canvas canvas = SetupDiagramCanvas();
            Image imgChessBoard = CreateChessBoard(canvas, diag);
            canvas.Children.Add(imgChessBoard);
            Viewbox viewBox = SetupDiagramViewbox(canvas);

            InlineUIContainer uIContainer = new InlineUIContainer();
            uIContainer.Child = viewBox;
            para.Inlines.Add(uIContainer);

            return para;
        }

        /// <summary>
        /// Creates the chessboard control.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Image CreateChessBoard(Canvas canvas, IntroViewDiagram diag)
        {
            Image imgChessBoard = new Image();
            imgChessBoard.Margin = new Thickness(5, 5, 5, 5);
            imgChessBoard.Source = ChessBoards.ChessBoardGreySmall;

            diag.Chessboard = new ChessBoardSmall(canvas, imgChessBoard, null, null, false, false);
            AlignExerciseAndMainBoards();

            return imgChessBoard;
        }

        /// <summary>
        /// Sets the "passive" exercise board to the same
        /// orientation as the main board.
        /// </summary>
        public void AlignExerciseAndMainBoards()
        {
        }

        /// <summary>
        /// Adds a new Node to the list and increments max run id.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private int AddNode(TreeNode nd)
        {
            _maxRunId++;
            nd.NodeId = _maxRunId;
            Nodes.Add(nd);
            return nd.NodeId;
        }


        /// <summary>
        /// Creates a Canvas for the chessboard. 
        /// </summary>
        /// <returns></returns>
        private Canvas SetupDiagramCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Background = Brushes.Black;
            canvas.Width = 250;
            canvas.Height = 250;

            return canvas;
        }

        /// <summary>
        /// Creates a Viewbox for the chessboard
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Viewbox SetupDiagramViewbox(Canvas canvas)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Child = canvas;
            viewBox.Width = 250;
            viewBox.Height = 250;
            viewBox.Visibility = Visibility.Visible;

            return viewBox;
        }

        /// <summary>
        /// Creates a FlowDocument from XAML string.
        /// </summary>
        /// <param name="xamlString"></param>
        /// <returns></returns>
        private FlowDocument StringToFlowDocument(string xamlString)
        {
            FlowDocument flowDocument;

            try
            {
                flowDocument = XamlReader.Parse(xamlString) as FlowDocument;
            }
            catch(Exception ex) 
            {
                flowDocument = new FlowDocument();
                AppLog.Message("StringToFlowDocument()", ex);
            }

            return flowDocument;
        }

        /// <summary>
        /// Handles the Text Change event. Sets the Workbook's dirty flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TODO: get Paragraph from CaretPosition to see if we are deleting a diagram
            if (_ignoreTextChange)
            {
                _ignoreTextChange = false;
            }
            else
            {
                AppState.IsDirty = true;
            }
        }
    }
}
