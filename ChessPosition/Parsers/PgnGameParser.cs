﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// Parsers a PGN parsers
    /// allowing to build a game/variation tree
    /// </summary>
    public class PgnGameParser
    {
        // Remaining text of the file, yet to be processed
        private string _remainingGameText;

        // id of the node currently being processed
        private int runningNodeId = 0;

        // the workbook for which this parser was called
        private WorkbookTree _workbook;

        /// <summary>
        /// Types of PGN/CHF token
        /// </summary>
        enum PgnTokenType
        {
            Unknown,
            Move,
            MoveNumber,
            CommentStart,
            CommentEnd,
            BranchStart,
            BranchEnd,
            NAG
        }

        /// <summary>
        /// Special characters
        /// </summary>
        private char[] SingleCharTokens = new char[] { '{', '}', '(', ')' };

        // whether debug information is to be logged 
        private bool DEBUG_MODE = false;

        /// <summary>
        /// The constructor takes the entire game notation as a string.
        /// </summary>
        /// <param name="workbook"></param>
        public PgnGameParser(string pgnGametext, WorkbookTree workbook, bool debugMode = false)
        {
            if (debugMode)
            {
                DEBUG_MODE = true;
            }

            _workbook = workbook;
            _remainingGameText = ReadHeaders(pgnGametext);

            ParseWorkbookText(_remainingGameText, workbook);
        }

        /// <summary>
        /// Parses the PGN headers
        /// and returns the text of the game without
        /// the headers.
        /// </summary>
        /// <param name="pgnGametext"></param>
        /// <returns></returns>
        public string ReadHeaders(string pgnGametext)
        {
            bool readingHeaders = true;

            string line;
            StringBuilder sb = new StringBuilder();
            using (StringReader reader = new StringReader(pgnGametext))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (readingHeaders)
                    {
                        readingHeaders = ProcessHeaderLine(line);
                    }

                    if (!readingHeaders)
                    {
                        sb.Append(line + " ");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Processes the PGN Headers and returns the game text 
        /// without them.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private bool ProcessHeaderLine(string line)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                // if empty line, retrun true
                // as there may be a header line still
                // following
                return true;
            }

            if (line[0] == '[')
            {
                ParseHeaderItem(line);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The header is in the form
        /// [Name "Value"]
        /// so we will strip the brackets and split by \"
        /// </summary>
        /// <param name="line"></param>
        private void ParseHeaderItem(string line)
        {
            // process only of the first and last char are square brackets
            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                line = line.Substring(1, line.Length - 2);
                string[] tokens = line.Split('\"');
                if (tokens.Length >= 2)
                {
                    _workbook.Headers.Add(tokens[0].Trim(), tokens[1].Trim());
                }
            }
        }

        /// <summary>
        /// The game text consists of White and Black moves with each White move
        /// preceded by the move number in the form of "N." where N is a positive integer.
        /// Each game must start with "1." or a comment.
        /// Comments strat with a '{' character and end with '}'
        /// A Black move follows the White move with a space character between them. If there is an intervening
        /// branch after the White move, the Black move after the return from the branch will be preceded by
        /// "N..." where N is the last White move number.
        /// Branches can be found after any move and are surrounded by parenthesis '(' and ')'.
        /// </summary>
        /// <param name="text"></param>
        private void ParseWorkbookText(string text, WorkbookTree workbook)
        {
            // create a root node
            TreeNode rootNode = new TreeNode(null, "", runningNodeId);
            runningNodeId++;

//            WorkbookTree.SetupStartingPosition(ref rootNode);
            rootNode.Position = PositionUtils.SetupStartingPosition();
            workbook.AddNode(rootNode);

            if (DEBUG_MODE)
            {
                DebugUtils.PrintPosition(rootNode.Position);
            }

            //TreeNode
            ParseBranch(rootNode, workbook);
        }

        /// <summary>
        /// Checks if the passed string matches any of the
        /// strings that mark the end of the game/workbook text.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool IsGameTerminationMarker(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            if (s == "*" || s == "1-0" || s == "0-1" || s == "1/2-1/2")
                return true;

            return false;
        }

        /// <summary>
        /// Parses a branch of the tree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="workbook"></param>
        private void ParseBranch(TreeNode parentNode, WorkbookTree workbook)
        {
            string token;

            TreeNode previousNode = parentNode;

            while ((token = GetNextToken()) != "")
            {
                if (IsGameTerminationMarker(token))
                    break;

                PgnTokenType gtt = GetTokenType(token);

                switch (gtt)
                {
                    // if this is a new branch then invoke this method again
                    case PgnTokenType.BranchStart:
                        ParseBranch(previousNode, workbook);
                        break;
                    case PgnTokenType.BranchEnd:
                        return;
                    case PgnTokenType.CommentStart:
                        ProcessComment(parentNode);
                        break;
                    case PgnTokenType.Move:
                        // ProcessMove() will return a new node
                        // that will then be the "parentNode" for
                        // the processing of the next move (ply)
                        TreeNode newNode = ProcessMove(token, parentNode);
                        parentNode.AddChild(newNode);
                        previousNode = parentNode;
                        parentNode = newNode;
                        workbook.AddNode(parentNode);
                        if (DEBUG_MODE)
                        {
                            DebugUtils.PrintPosition(newNode.Position);
                        }

                        break;
                    case PgnTokenType.MoveNumber:
                        ProcessMoveNumber(token, parentNode);
                        break;
                    case PgnTokenType.NAG:
                        // add to the last processed move
                        AddNAGtoLastMove(workbook, token);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds an encountered NAG character to the last processed move.
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="nag"></param>
        private void AddNAGtoLastMove(WorkbookTree workbook, string nag)
        {
            TreeNode nd = workbook.Nodes[workbook.Nodes.Count - 1];
            nd.AddNag(nag);
        }

        /// <summary>
        /// This is the core processing method.
        /// It creates a new node in the game tree, parses the
        /// move text and creates a position in the new node
        /// by making the parsed move on the board of the parent position.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="charIndex"></param>
        /// <param name="previousNode"></param>
        /// <returns></returns>
        private TreeNode ProcessMove(string algMove, TreeNode parentNode)
        {
            PieceColor parentSideToMove = parentNode.ColorToMove;

            PgnMoveParser pmp = new PgnMoveParser();
            pmp.ParseAlgebraic(algMove, parentSideToMove);
            MoveData move = pmp.Move;
            if (DEBUG_MODE)
            {
                DebugUtils.PrintMove(move, algMove);
            }

            // create a new node
            TreeNode newNode = CreateNewNode(algMove, move, parentNode, parentSideToMove);

            // Make the move on it
            MoveUtils.MakeMove(newNode.Position, move);

            // do the postprocessing
            PositionUtils.UpdateCastlingRights(ref newNode.Position, move);
            PositionUtils.SetEnpassantSquare(ref newNode.Position, move);

            return newNode;
        }

        /// <summary>
        /// Creates a new node in the game tree for the next move
        /// to be stored with the position.
        /// </summary>
        /// <param name="algMove"></param>
        /// <param name="move"></param>
        /// <param name="parentNode"></param>
        /// <param name="parentSideToMove"></param>
        /// <returns></returns>
        private TreeNode CreateNewNode(string algMove, MoveData move, TreeNode parentNode, PieceColor parentSideToMove)
        {
            TreeNode newNode = new TreeNode(parentNode, algMove, runningNodeId);
            runningNodeId++;

            // copy the board from the parent
            newNode.Position.Board = (byte[,])parentNode.Position.Board.Clone();

            if (parentSideToMove == PieceColor.White)
            {
                newNode.Position.MoveNumber = parentNode.Position.MoveNumber + 1;
                newNode.Position.ColorToMove = PieceColor.Black;
            }
            else
            {
                newNode.Position.MoveNumber = parentNode.Position.MoveNumber;
                newNode.Position.ColorToMove = PieceColor.White;
            }

            if (move.IsCaptureOrPawnMove())
            {
                newNode.Position.HalfMove50Clock = 0;
            }
            else
            {
                newNode.Position.HalfMove50Clock += 1;
            }

            return newNode;
        }

        private void ProcessMoveNumber(string token, TreeNode previousNode)
        {
            // TODO: move number should be checked that it is what is expected
            // but otherwise this is spurious
        }

        /// <summary>
        /// If the comment inludes Chess Forge commands, the commands
        /// will be stored with the node.
        /// Chess Forge commands are in the form [%chf-<cmd> <params>] and must be at the start
        /// of the comment, with any (optional) free text comment following.
        /// The comment will also be stored with the node, stripped of the Chess Forge commands.
        /// </summary>
        /// <param name="node"></param>
        private void ProcessComment(TreeNode node)
        {
            int endPos = _remainingGameText.IndexOf('}');
            // if end of comment not found, there is something wrong with the file, force end of processing.
            if (endPos < 0)
            {
                _remainingGameText = "";
                return;
            }

            // process any Chess Forge commands
            while (true)
            {
                int commandStart = _remainingGameText.IndexOf("[%chf-", 0, endPos);
                if (commandStart < 0)
                    break;

                int commandEnd = _remainingGameText.IndexOf(']', 0, endPos);
                if (commandEnd > 0)
                {
                    string command = _remainingGameText.Substring(commandStart + 1, commandEnd - (commandStart + 1));
                    _workbook.AddChfCommand(node, command);
                    _remainingGameText = _remainingGameText.Substring(commandEnd + 1);
                    endPos = endPos - (commandEnd + 1);
                }
            }

            // update endPos as it may have been changed above when removing commands
            endPos = _remainingGameText.IndexOf('}');

            string comment = _remainingGameText.Substring(0, endPos);
            // trim to check if there is any comment but do not trim the comment if it is there.
            if (comment.Trim().Length > 0)
            {
                node.Comment = comment;
            }
            _remainingGameText = _remainingGameText.Substring(endPos + 1);
        }

        /// <summary>
        /// Find the next token in the remaining text.
        /// </summary>
        /// <returns></returns>
        private string GetNextToken()
        {
            int charPos = 0;
            string token = "";

            // first skip the spaces
            while ((_remainingGameText[charPos]) == ' ')
            {
                charPos++;
            }

            int tokenStartIndex = charPos;

            char c = _remainingGameText[charPos];

            if (SingleCharTokens.Contains(c))
            {
                charPos++;
                token += c;
            }
            else
            {
                // go to the next space or closing parenthesis
                while (_remainingGameText[charPos] != ' ' && _remainingGameText[charPos] != ')' && charPos < _remainingGameText.Length)
                {
                    charPos++;
                }
                token = _remainingGameText.Substring(tokenStartIndex, charPos - tokenStartIndex);
            }


            _remainingGameText = _remainingGameText.Substring(charPos);
            return token;
        }

        /// <summary>
        /// Returns the type of the passed token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private PgnTokenType GetTokenType(string token)
        {
            if (token.Length == 0)
            {
                return PgnTokenType.Unknown;
            }

            PgnTokenType gtt = PgnTokenType.Unknown;

            char c = token[0];
            if (char.IsDigit(c))
            {
                gtt = PgnTokenType.MoveNumber;
            }
            else if (char.IsLetter(c))
            {
                gtt = PgnTokenType.Move;
            }
            else
            {
                switch (c)
                {
                    case '(':
                        gtt = PgnTokenType.BranchStart;
                        break;
                    case ')':
                        gtt = PgnTokenType.BranchEnd;
                        break;
                    case '{':
                        gtt = PgnTokenType.CommentStart;
                        break;
                    case '}':
                        gtt = PgnTokenType.CommentEnd;
                        break;
                    case '$':
                        gtt = PgnTokenType.NAG;
                        break;
                }
            }

            return gtt;
        }
    }
}
