﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChessPosition;
using GameTree;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace WebAccess
{
    public class ChesscomUserGames
    {
        /// <summary>
        /// Handler for the UserGamesReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> UserGamesReceived;

        /// <summary>
        /// Downloads games from chess.com.
        /// This is a multistage process.
        /// First we need to get the list of archives with games of the requested. Then
        /// we have to select the right ones to match the filter criteria
        /// and issue as many requestes as required.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static async Task<string> GetChesscomUserGames(GamesFilter filter)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                // STAGE 1: get list of possible archives
                List<string> urlArchivesList = null;
                string urlArchivesCommand = GetArchiveListUrl(filter.User);
                string text = await ExecuteHttpCall(urlArchivesCommand);
                eventArgs.TextData = text.Replace(',', ' ');
                urlArchivesList = TextUtils.MatchUrls(eventArgs.TextData);

                List<uint> lstYearMonth = BuildYearMonthList(filter, urlArchivesList);

                if (lstYearMonth.Count == 0)
                {
                    eventArgs.TextData = "";
                }
                else
                {
                    // STAGE 2: start loading archive after archive and retrieving games until we get the required number
                    ReadGamesFromArchives(lstYearMonth, filter);
                }
                eventArgs.Success = true;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
            catch (Exception ex)
            {
                eventArgs.Success = true;
                eventArgs.Message = ex.Message;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
        }

        private async static void ReadGamesFromArchives(List<uint> lstYearMonths, GamesFilter filter)
        {
            int totalGames = 0;

            // if start date is not null we start from there, otherwise, we go back from the EndDate or latest.
            if (!filter.StartDate.HasValue)
            {
                lstYearMonths.Reverse();
            }

            StringBuilder allGames = new StringBuilder();

            foreach (uint yearMonth in lstYearMonths)
            {
                // get month and year
                DecodeYearMonth(yearMonth, out uint year, out uint month);
                string sYear = year.ToString("0000");
                string sMonth = month.ToString("00");
                string url = string.Format("https://api.chess.com/pub/player/{0}/games/{1}/{2}/pgn", filter.User, sYear, sMonth);
                string text = await ExecuteHttpCall(url);
                allGames.AppendLine(text);

                ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                totalGames += PgnMultiGameParser.ParsePgnMultiGameText(text, ref games);
                if (totalGames >= filter.MaxGames || totalGames == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Executes an http REST call.
        /// Returns the text received in response.
        /// </summary>
        /// <param name="rest"></param>
        /// <returns></returns>
        private static async Task<string> ExecuteHttpCall(string rest)
        {
            string text = "";

            var response = await RestApiRequest.GameImportClient.GetAsync(rest);
            using (var fs = new MemoryStream())
            {
                await response.Content.CopyToAsync(fs);
                fs.Position = 0;
                StreamReader sr = new StreamReader(fs);
                text = sr.ReadToEnd().Replace(',', ' ');
            }

            return text;
        }

        /// <summary>
        /// Returns a list of urls after urls for archives from outside
        /// the filter's range were removed.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="urlList"></param>
        /// <returns></returns>
        private static List<uint> BuildYearMonthList(GamesFilter filter, List<string> urlList)
        {
            List<uint> trimmedList = new List<uint>();

            Regex urlRegex = new Regex(@"\/\d{4}\/\d{2}$", RegexOptions.Compiled);
            foreach (string url in urlList)
            {
                Match match = urlRegex.Match(url);
                // first 4 digits is year followed by slash and 2 digits for month
                try
                {
                    uint year = uint.Parse(match.Value.Substring(1, 4));
                    uint month = uint.Parse(match.Value.Substring(6, 2));
                    if (IsDateToYearMonthGood(true, filter.StartDate, year, month) && IsDateToYearMonthGood(false, filter.EndDate, year, month))
                    {
                        trimmedList.Add(EncodeYearMonth(year, month));
                    }
                }
                catch
                {
                }
            }

            trimmedList.Sort();
            return trimmedList;
        }

        /// <summary>
        /// Checks if the passed year & month represents an earlier/later date than the passed date
        /// depending on wheter startDate is true/false.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="date"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        private static bool IsDateToYearMonthGood(bool startDate, DateTime? date, uint year, uint month)
        {
            if (!date.HasValue)
            {
                return true;
            }

            if (startDate)
            {
                if (date.Value.Year < year || date.Value.Year == year && date.Value.Month <= month)
                {
                    return true;
                }
            }
            else
            {
                if (date.Value.Year > year || date.Value.Year == year && date.Value.Month >= month)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds a REST string to query archives with player's games.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetArchiveListUrl(string user)
        {
            string url = string.Format("https://api.chess.com/pub/player/{0}/games/archives", user);
            return url;
        }

        private static uint EncodeYearMonth(uint year, uint month)
        {
            return year << 4 | month;
        }

        private static void DecodeYearMonth(uint encoded, out uint year, out uint month)
        {
            month = encoded & 0x0F;
            year = encoded >> 4;
        }

    }
}
