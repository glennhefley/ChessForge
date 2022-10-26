﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition.GameTree;
using GameTree;

namespace ChessPosition
{
    public class TextUtils
    {
        /// <summary>
        /// Builds text for variation line passed as a list
        /// of nodes. Depending on the fromIndex and toIndex arguments,
        /// an entire list or only a part of it will be included.
        /// </summary>
        /// <param name="line">The variation line to process</param>
        /// <param name="withNAG">Whether to include NAG symbols</param>
        /// <param name="fromIndex">The index to start from.</param>
        /// <param name="toIndex">The index of the last included ply.  
        /// If -1, the whole line starting as fromIndex will be included.</param>
        /// <returns></returns>
        public static string BuildTextForLine(List<TreeNode> line, bool withNAG = false, int fromIndex = 0, int toIndex = -1)
        {
            StringBuilder sb = new StringBuilder();

            if (toIndex < 0 || toIndex >= line.Count - 1)
            {
                toIndex = line.Count - 1;
            }

            bool isFirstPly = true;
            for (int i = fromIndex; i <= toIndex; i++)
            {
                TreeNode nd = line[i];

                // if NodeId is 0 this the starting position Node and we must not process it
                if (nd.NodeId != 0)
                {
                    if (nd.Position.ColorToMove == PieceColor.Black)
                    {
                        if (nd.Position.MoveNumber != 1)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(nd.Position.MoveNumber.ToString() + ".");
                    }
                    else if (isFirstPly)
                    {
                        sb.Append(nd.Position.MoveNumber.ToString() + "...");
                    }

                    isFirstPly = false;

                    if (withNAG)
                    {
                        sb.Append(" " + MoveUtils.BuildSingleMoveText(nd, true, true));
                    }
                    else
                    {
                        sb.Append(" " + MoveUtils.BuildSingleMoveText(nd, true, false));
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Given a string, checks if it is in Chess Forge / PGN
        /// format (yyyy.mm.dd) and if not gets out of it what it can.
        /// Returns the corrected string.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="hasMonth">Returns whether the string had a month included</param>
        /// <param name="hasDay">Returns whether the string had a day included</param>
        /// <returns></returns>
        public static string AdjustPgnDateString(string val, out bool hasMonth, out bool hasDay)
        {
            hasMonth = false;
            hasDay = false;

            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(val))
            {
                return Constants.EMPTY_PGN_DATE;
            }

            string[] tokens = val.Split(new char[] { '.', '-', '/' });
            if (tokens[0].Length == 4)
            {
                if (int.TryParse(tokens[0], out int year) && year > 1000)
                {
                    sb.Append(tokens[0] + '.');
                    if (tokens.Length > 1 && int.TryParse(tokens[1], out int month) && month >= 1 && month <= 12)
                    {
                        sb.Append(month.ToString("00") + '.');
                        hasMonth = true;
                        if (tokens.Length > 2 && int.TryParse(tokens[2], out int day) && day >= 1 && day <= 31)
                        {
                            sb.Append(day.ToString("00"));
                            hasDay = true;
                        }
                        else
                        {
                            sb.Append("??");
                        }
                    }
                    else
                    {
                        sb.Append("??.??");
                    }
                }
            }
            else
            {
                sb.Append(Constants.EMPTY_PGN_DATE);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a PGN formatted date from a DateTiem object
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="ignoreMonthDay">whether month and day should be replaced with "??.??"</param>
        /// <param name="ignoreDay">whether day should be replaced with "??"</param>
        /// <returns></returns>
        public static string BuildPgnDateString(DateTime? dt, bool ignoreMonthDay = false, bool ignoreDay = false)
        {
            if (dt == null)
            {
                return Constants.EMPTY_PGN_DATE;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(dt.Value.Year.ToString("0000") + '.');
                if (ignoreMonthDay)
                {
                    sb.Append("??.??");
                }
                else
                {
                    sb.Append(dt.Value.Month.ToString("00") + '.');
                    if (ignoreDay)
                    {
                        sb.Append("??");
                    }
                    else
                    {
                        sb.Append(dt.Value.Day.ToString("00"));
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts a PGN string "xxxx.xx.xx" to a date.
        /// </summary>
        /// <param name="pgn"></param>
        /// <returns></returns>
        public static DateTime? GetDateFromPgnString(string pgn)
        {
            if (string.IsNullOrEmpty(pgn))
            {
                return null;
            }

            DateTime? dt = null;
            string[] tokens = pgn.Split('.');

            int year;
            int month = 1;
            int day = 1;

            if (int.TryParse(tokens[0], out year))
            {
                if (tokens.Length > 1)
                {
                    if (int.TryParse(tokens[1], out month))
                    {
                        if (tokens.Length > 2)
                        {
                            if (!int.TryParse(tokens[2], out day))
                            {
                                day = 1;
                            }
                        }
                    }
                    else
                    {
                        month = 1;
                    }
                }
                dt = new DateTime(year, month, day);
            }

            return dt;
        }

        /// <summary>
        /// Extracts the integer value from a string that has it
        /// as a suffix following the last underscore.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="lastChar"></param>
        /// <returns></returns>
        public static int GetIdFromPrefixedString(string s, char lastChar = '_')
        {
            int nodeId = -1;

            int lastCharPos = s.LastIndexOf('_');
            if (lastCharPos >= 0 && lastCharPos < s.Length - 1)
            {
                if (!int.TryParse(s.Substring(lastCharPos + 1), out nodeId))
                {
                    nodeId = -1;
                }
            }

            return nodeId;
        }

        /// <summary>
        /// Returns the PieceColor value corresponding to the passed string.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static PieceColor ConvertStringToPieceColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return PieceColor.None;
            }

            if (color.ToLower() == "white")
            {
                return PieceColor.White;
            }
            else if (color.ToLower() == "black")
            {
                return PieceColor.Black;
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Builds a line of text for display in the processing errors list.
        /// </summary>
        /// <param name="gm"></param>
        /// <param name="gameNo"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string BuildGameProcessingErrorText(GameData gm, int gameNo, string message)
        {
            StringBuilder sbErrors = new StringBuilder();

            if (gm != null)
            {
                sbErrors.Append("Game #" + gameNo.ToString() + " : " + gm.Header.BuildGameHeaderLine());
                sbErrors.Append(Environment.NewLine);
                sbErrors.Append("     " + message);
                sbErrors.Append(Environment.NewLine);
            }

            return sbErrors.ToString();
        }
    }
}
