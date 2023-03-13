﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Processes a move of a piece but without validity check.
    /// For processing valid moves, see UserMoveProcessor class
    /// </summary>
    public class RepositionPieceProcessor
    {
        /// <summary>
        /// Similar to finalizing the move, except that we make no checks on the move. 
        /// This is called when building the Intro text.
        /// </summary>
        /// <param name="destSquare"></param>
        public static string RepositionDraggedPiece(SquareCoords destSquare, bool fullNotation, ref TreeNode nd)
        {
            string moveNotation = "";

            if (destSquare.Xcoord != DraggedPiece.OriginSquare.Xcoord || destSquare.Ycoord != DraggedPiece.OriginSquare.Ycoord)
            {
                SquareCoords origSquareNorm = new SquareCoords(DraggedPiece.OriginSquare);
                SquareCoords destSquareNorm = new SquareCoords(destSquare);
                if (AppState.MainWin.MainChessBoard.IsFlipped)
                {
                    origSquareNorm.Flip();
                    destSquareNorm.Flip();
                }

                PieceType movingPieceType = PositionUtils.GetPieceType(nd, origSquareNorm);
                PieceColor movingPieceColor = PositionUtils.GetPieceColor(nd, origSquareNorm);
                bool isPromotion = false;
                PieceType promoteTo = PieceType.None;

                if (IsPromotionSquare(destSquareNorm, movingPieceColor) && movingPieceType == PieceType.Pawn)
                {
                    isPromotion = true;
                    promoteTo = AppState.MainWin.GetUserPromoSelection(destSquareNorm);
                    // if user cancelled promotion, we will just leave the pawn on the promotion square.
                    if (promoteTo == PieceType.None)
                    {
                        isPromotion = false;
                    }
                }

                // check promotion for the side who moved i.e. opposite of what we have in the new nd Node
                ImageSource imgSrc = DraggedPiece.ImageControl.Source;
                if (isPromotion)
                {
                    if (movingPieceColor == PieceColor.White)
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
                SoundPlayer.PlayMoveSound("");

                bool isCastle = TryCompleteCastle(movingPieceType, movingPieceColor, origSquareNorm, destSquareNorm, ref nd);
                moveNotation = BuildMoveText(false, nd, origSquareNorm, destSquareNorm, isCastle, promoteTo);

                PositionUtils.RepositionPiece(origSquareNorm, destSquareNorm, promoteTo, ref nd);
                nd.Position.ColorToMove = MoveUtils.ReverseColor(movingPieceColor);

                AppState.MainWin.MainChessBoard.DisplayPosition(nd, true);
            }
            else
            {
                AppState.MainWin.ReturnDraggedPiece(false);
            }

            return moveNotation;
        }


        /// <summary>
        /// Builds an algebraic notation string for the move
        /// in short or full notation.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="promoteTo"></param>
        /// <param name="fullNotation"></param>
        /// <returns></returns>
        private static string BuildMoveText(bool fullNotation, TreeNode nd, SquareCoords orig, SquareCoords dest, bool isCastling, PieceType promoteTo)
        {
            StringBuilder sb = new StringBuilder();

            PieceType piece = PositionUtils.GetPieceType(nd, orig);
            PieceColor color = PositionUtils.GetPieceColor(nd, orig);

            if (isCastling)
            {
                // before calling this method, it was determined that the move was a king's castling move, therefore, we do not need to check again
                sb.Append(dest.Xcoord == 6 ? "O-O" : "O-O-O");
            }
            else
            {
                if (piece != PieceType.Pawn)
                {
                    sb.Append(FenParser.PieceToFenChar[piece]);
                }
                if (fullNotation)
                {
                    sb.Append((char)(orig.Xcoord + (int)'a'));
                    sb.Append((char)(orig.Ycoord + (int)'1'));
                }
                // is this a capture
                if (PositionUtils.GetPieceColor(nd, dest) == MoveUtils.ReverseColor(color))
                {
                    sb.Append("x");
                }
                else
                {
                    if (fullNotation)
                    {
                        sb.Append("-");
                    }
                }
                sb.Append((char)(dest.Xcoord + (int)'a'));
                sb.Append((char)(dest.Ycoord + (int)'1'));
                if (promoteTo != PieceType.None)
                {
                    sb.Append(FenParser.PieceToFenChar[promoteTo]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines if the move being processed is castling move by the king.
        /// If so, performs castling.
        /// </summary>
        /// <param name="movingPieceType"></param>
        /// <param name="movingPieceColor"></param>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="nd"></param>
        private static bool TryCompleteCastle(PieceType movingPieceType, PieceColor movingPieceColor, SquareCoords orig, SquareCoords dest, ref TreeNode nd)
        {
            bool isCastle = false;

            SquareCoords castlingRookPos = TryGetCastlingRookPosition(movingPieceType, movingPieceColor, orig, dest);
            if (castlingRookPos != null)
            {
                isCastle = TryMoveCastlingRook(castlingRookPos, movingPieceColor, ref nd);
            }

            return isCastle;
        }

        /// <summary>
        /// Check that there is a Rook of the right color on the origin square.
        /// If so, move to the right spot in the TreeNode.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="movingPieceColor"></param>
        private static bool TryMoveCastlingRook(SquareCoords orig, PieceColor movingPieceColor, ref TreeNode nd)
        {
            bool isCastle = false;

            if (movingPieceColor == PieceColor.White && orig.Ycoord == 0
                || movingPieceColor == PieceColor.Black && orig.Ycoord == 7)
            {
                SquareCoords dest = new SquareCoords(orig);
                if (orig.Xcoord == 0)
                {
                    dest.Xcoord = 3;
                }
                else
                {
                    dest.Xcoord = 5;
                }

                if (PositionUtils.GetPieceType(nd, orig) == PieceType.Rook && PositionUtils.GetPieceColor(nd, orig) == movingPieceColor)
                {
                    PositionUtils.RepositionPiece(orig, dest, PieceType.None, ref nd);
                    isCastle = true;
                }
            }

            return isCastle;
        }

        /// <summary>
        /// If the move is determined to be a castling move by the king,
        /// returns the square at which the rook of the sam color should be.
        /// </summary>
        /// <param name="movingPieceType"></param>
        /// <param name="movingPieceColor"></param>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        private static SquareCoords TryGetCastlingRookPosition(PieceType movingPieceType, PieceColor movingPieceColor, SquareCoords orig, SquareCoords dest)
        {
            SquareCoords sqRook = null;

            if (movingPieceType == PieceType.King)
            {
                if (movingPieceColor == PieceColor.White && orig.Xcoord == 4 && orig.Ycoord == 0 && dest.Ycoord == 0)
                {
                    if (dest.Xcoord == 2)
                    {
                        sqRook = new SquareCoords(0, 0);
                    }
                    else if (dest.Xcoord == 6)
                    {
                        sqRook = new SquareCoords(7, 0);
                    }
                }
                else if (movingPieceColor == PieceColor.Black && orig.Xcoord == 4 && orig.Ycoord == 7 && dest.Ycoord == 7)
                {
                    if (dest.Xcoord == 2)
                    {
                        sqRook = new SquareCoords(0, 7);
                    }
                    else if (dest.Xcoord == 6)
                    {
                        sqRook = new SquareCoords(7, 7);
                    }
                }
            }

            return sqRook;
        }

        /// <summary>
        /// Checks if a given square is potentially a promotion square
        /// </summary>
        /// <param name="sqNorm"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private static bool IsPromotionSquare(SquareCoords sqNorm, PieceColor color)
        {
            if (color == PieceColor.White && sqNorm.Ycoord == 7)
            {
                return true;
            }
            else if (color == PieceColor.Black && sqNorm.Ycoord == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
