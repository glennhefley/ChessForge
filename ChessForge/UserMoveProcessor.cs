﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Handles moves that were made on the board by the user.
    /// </summary>
    public class UserMoveProcessor
    {
        /// <summary>
        /// Invoked after the user made a move on the chessboard
        /// and released the mouse.
        /// </summary>
        /// <param name="destSquare"></param>
        public static void FinalizeUserMove(SquareCoords destSquare)
        {
            // if the move is valid swap image at destination and clear image at origin
            if (destSquare.Xcoord != DraggedPiece.OriginSquare.Xcoord || destSquare.Ycoord != DraggedPiece.OriginSquare.Ycoord)
            {
                StringBuilder moveEngCode = new StringBuilder();
                SquareCoords origSquareNorm = new SquareCoords(DraggedPiece.OriginSquare);
                SquareCoords destSquareNorm = new SquareCoords(destSquare);
                if (AppState.MainWin.MainChessBoard.IsFlipped)
                {
                    origSquareNorm.Flip();
                    destSquareNorm.Flip();
                }

                bool isPromotion = false;
                PieceType promoteTo = PieceType.None;

                if (IsMoveToPromotionSquare(origSquareNorm, destSquareNorm))
                {
                    isPromotion = true;
                    promoteTo = AppState.MainWin.GetUserPromoSelection(destSquareNorm);
                }

                // do not process if this was a canceled promotion
                if ((!isPromotion || promoteTo != PieceType.None))
                {
                    moveEngCode.Append((char)(origSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(origSquareNorm.Ycoord + (int)'1'));
                    moveEngCode.Append((char)(destSquareNorm.Xcoord + (int)'a'));
                    moveEngCode.Append((char)(destSquareNorm.Ycoord + (int)'1'));

                    // add promotion char if this is a promotion
                    if (isPromotion)
                    {
                        moveEngCode.Append(FenParser.PieceToFenChar[promoteTo]);
                    }

                    if (ProcessMoveAndReport(moveEngCode.ToString(), destSquare, promoteTo, out TreeNode nd, out bool isCastle, out bool reportDupe))
                    {
                    }
                    else
                    {
                        AppState.MainWin.ReturnDraggedPiece(false);
                    }
                }
                else
                {
                    AppState.MainWin.ReturnDraggedPiece(false);
                }
            }
            else
            {
                AppState.MainWin.ReturnDraggedPiece(false);
            }
        }

        /// <summary>
        /// Processes the move and reflects it in the GUI
        /// </summary>
        /// <param name="moveEngCode"></param>
        /// <param name="destSquare"></param>
        /// <param name="promoteTo"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <param name="reportDupe"></param>
        /// <returns></returns>
        public static bool ProcessMoveAndReport(string moveEngCode, SquareCoords destSquare, PieceType promoteTo,
                                                out TreeNode nd, out bool isCastle, out bool reportDupe)
        {
            if (ProcessMove(moveEngCode.ToString(), out nd, out isCastle, out reportDupe))
            {
                // check promotion for the side who moved i.e. opposite of what we have in the new nd Node
                ImageSource imgSrc = DraggedPiece.ImageControl.Source;
                if (promoteTo != PieceType.None)
                {
                    if (nd.ColorToMove == PieceColor.Black)
                    {
                        imgSrc = AppState.MainWin.MainChessBoard.GetWhitePieceRegImg(promoteTo);
                    }
                    else
                    {
                        imgSrc = AppState.MainWin.MainChessBoard.GetBlackPieceRegImg(promoteTo);
                    }
                }
                AppState.MainWin.MainChessBoard.GetPieceImage(destSquare.Xcoord, destSquare.Ycoord, true).Source = imgSrc;

                AppState.MainWin.ReturnDraggedPiece(true);
                if (isCastle)
                {
                    AppState.MainWin.MoveCastlingRook(moveEngCode.ToString());
                }

                PostMoveReporting(nd, reportDupe);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Post processes a move, updating the board position
        /// and the board comment box.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="reportDupe"></param>
        public static void PostMoveReporting(TreeNode nd, bool reportDupe)
        {
            SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);

            if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                AppState.ShowMoveEvaluationControls(TrainingSession.IsContinuousEvaluation, false);
            }

            if (nd.Position.IsCheckmate)
            {
                AppState.MainWin.BoardCommentBox.ReportCheckmate(true);
            }
            else if (nd.Position.IsStalemate)
            {
                AppState.MainWin.BoardCommentBox.ReportStalemate();
            }
            else
            {
                AppState.MainWin.BoardCommentBox.GameMoveMade(nd, true);
                if (reportDupe)
                {
                    AppState.MainWin.BoardCommentBox.ReportIdenticalPositionFound(nd);
                }
            }
            AppState.MainWin.ColorMoveSquares(nd.LastMoveEngineNotation);
            if (nd != null)
            {
                AppState.MainWin.MainChessBoard.DisplayPosition(nd, true);
            }

        }

        /// <summary>
        /// Processed the move's business logic.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        public static bool ProcessMove(string move, out TreeNode nd, out bool isCastle, out bool reportDupe)
        {
            reportDupe = false;

            if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                return ProcessMoveInManualReviewMode(move, out nd, out isCastle, out reportDupe);
            }
            else
            {
                return ProcessMoveInGameMode(move, out nd, out isCastle);
            }
        }

        /// <summary>
        /// Checks if the move leads to a promotion
        /// i.e. a pawn reaching the last rank.
        /// </summary>
        /// <param name="origSquareNorm"></param>
        /// <param name="destSquareNorm"></param>
        /// <returns></returns>
        private static bool IsMoveToPromotionSquare(SquareCoords origSquareNorm, SquareCoords destSquareNorm)
        {
            if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                TreeNode nd = AppState.MainWin.ActiveLine.GetSelectedTreeNode();
                PieceColor pieceColor = AppState.MainWin.MainChessBoard.GetPieceColor(origSquareNorm);
                if (AppState.MainWin.MainChessBoard.GetPieceType(origSquareNorm) == PieceType.Pawn
                 && ((nd.ColorToMove == PieceColor.White && destSquareNorm.Ycoord == 7)
                     || (nd.ColorToMove == PieceColor.Black && destSquareNorm.Ycoord == 0)))
                {
                    return true;
                }
            }
            else if (EngineGame.GetPieceType(origSquareNorm) == PieceType.Pawn
                && ((EngineGame.ColorToMove == PieceColor.White && destSquareNorm.Ycoord == 7)
                    || (EngineGame.ColorToMove == PieceColor.Black && destSquareNorm.Ycoord == 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes a user's move made in the context of a game
        /// with the engine.
        /// TODO: do we need a lock here so ProcessUserGameMoveEvent does not start before
        /// we finish this?
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool ProcessMoveInGameMode(string move, out TreeNode nd, out bool isCastle)
        {
            if (CreateNewPlyNode(move, out nd, out isCastle, out bool preExist))
            {
                bool endOfGame = false;
                if (PositionUtils.IsCheckmate(nd.Position, out _))
                {
                    endOfGame = true;
                    AppState.MainWin.BoardCommentBox.ReportCheckmate(true);
                }
                else if (PositionUtils.IsStalemate(nd.Position))
                {
                    endOfGame = true;
                    AppState.MainWin.BoardCommentBox.ReportStalemate();
                }

                EngineGame.Line.AddPlyAndMove(nd);
                EngineGame.SwitchToAwaitEngineMove(nd, endOfGame);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Processes a move made by the user while
        /// in Manual Review mode.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool ProcessMoveInManualReviewMode(string move, out TreeNode nd, out bool isCastle, out bool reportDupe)
        {
            reportDupe = false;

            if (CreateNewPlyNode(move, out nd, out isCastle, out bool preExist))
            {
                if (PositionUtils.IsCheckmate(nd.Position, out _))
                {
                    nd.Position.IsCheckmate = true;
                }
                else if (PositionUtils.IsStalemate(nd.Position))
                {
                    nd.Position.IsStalemate = true;
                }

                //TODO: update Workbook, ActiveLine and Workbook View
                // if the move already has a LineId, it means we found it to exist so just select the new line
                // and the move in the views

                // if the move is new but has no siblings, "inherit" line id from the parent 
                if (!preExist && string.IsNullOrEmpty(nd.LineId) && !AppState.MainWin.ActiveVariationTree.NodeHasSiblings(nd.NodeId))
                {
                    nd.LineId = nd.Parent.LineId;
                }

                // if we have LineId we are done
                if (!string.IsNullOrEmpty(nd.LineId))
                {
                    if (nd.IsNewUserMove && !preExist)
                    {
                        AppState.MainWin.AppendNodeToActiveLine(nd, false);

                        AppState.MainWin.RebuildActiveTreeView();
#if false
                    //TODO: optimizations below did not quite work. Need a performance refactor

                        // in exercise this can be the first move (nd.Parent.NodeId == 0) in which case we want to call a Rebuild so we get the move number
                        if (nd.Parent == null || nd.Parent.NodeId == 0 || AppState.MainWin.ActiveVariationTree.NodeHasSiblings(nd.Parent.NodeId))
                        {
                            AppState.MainWin.RebuildActiveTreeView();
                        }
                        else
                        {
                            AppState.MainWin.AddNewNodeToVariationTreeView(nd);
                        }
#endif
                        AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, AppState.MainWin.ActiveLine.GetLineId(), AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
                    }
                    else
                    {
                        AppState.MainWin.SetActiveLine(nd.LineId, nd.NodeId, false);
                        AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, AppState.MainWin.ActiveLine.GetLineId(), AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
                    }
                }
                else
                {
                    // new move for which we need a new line id
                    // if it is new and has siblings, rebuild line ids
                    // Workbook view will need a full update unless TODO this node AND its parent have no siblings
                    AppState.MainWin.ActiveVariationTree.SetLineIdForNewNode(nd);
                    AppState.MainWin.SetActiveLine(nd.LineId, nd.NodeId, false);
                    AppState.MainWin.RebuildActiveTreeView();
                    AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, AppState.MainWin.ActiveLine.GetLineId(), AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
                }

                try
                {
                    if (AppState.MainWin.ActiveArticle.Solver.GetAppSolvingMode() == VariationTree.SolvingMode.GUESS_MOVE)
                    {
                        AppState.MainWin.Timers.Start(AppTimers.TimerId.SOLVING_GUESS_MOVE_MADE);
                    }
                }
                catch
                {
                    return false;
                }

                if (!preExist)
                {
                    ChessForgeEventArgs args = new ChessForgeEventArgs();
                    reportDupe = FindIdenticalPositions.Search(nd, FindIdenticalPositions.Mode.CHECK_IF_ANY);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Process a user's move supplied in engine notation.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="nd"></param>
        /// <param name="isCastle"></param>
        /// <returns></returns>
        private static bool CreateNewPlyNode(string move, out TreeNode nd, out bool isCastle, out bool preExist)
        {
            isCastle = false;
            preExist = false;

            TreeNode curr;
            if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                curr = AppState.MainWin.ActiveLine.GetSelectedTreeNode();
            }
            else
            {
                curr = EngineGame.GetLastGameNode();
            }

            nd = AppState.MainWin.ActiveVariationTree.CreateNewChildNode(curr);
            string algMove;
            try
            {
                algMove = MoveUtils.EngineNotationToAlgebraic(move, ref nd.Position, out isCastle);
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateNewPlyNode()", ex);
                algMove = "";
            }

            // check that it starts with a letter as it may be something invalid like "???"
            if (!string.IsNullOrEmpty(algMove) && char.IsLetter(algMove[0]))
            {
                nd.Position.ColorToMove = nd.Position.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
                nd.LastMoveAlgebraicNotation = algMove;

                if (MoveUtils.IsCaptureOrPawnMove(algMove))
                {
                    nd.Position.HalfMove50Clock = 0;
                }
                else
                {
                    nd.Position.HalfMove50Clock += 1;
                }

                TreeNode sib = AppState.MainWin.ActiveVariationTree.GetIdenticalSibling(nd);
                if (sib == null)
                {
                    // if this is a new move, mark as such and add to Workbook
                    if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
                    {
                        nd.IsNewUserMove = true;
                        if (AppState.MainWin.ActiveVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.GUESS_MOVE
                            && AppState.MainWin.ActiveVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.ANALYSIS)
                        {
                            AppState.IsDirty = true;
                        }
                    }
                    else
                    {
                        nd.IsNewTrainingMove = true;
                    }
                    AppState.MainWin.ActiveVariationTree.AddNodeToParent(nd);

                    //if we are in MANUAL_REVIEW prepare UndoAddMove
                    if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
                    {
                        EditOperation op = new EditOperation(EditOperation.EditType.ADD_MOVE, nd);
                        AppState.ActiveVariationTree.OpsManager.PushOperation(op);
                    }
                }
                else
                {
                    preExist = true;
                    nd = sib;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
