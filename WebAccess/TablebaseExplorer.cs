﻿using GameTree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Manages querying lichess Tablebases.
    /// </summary>
    public class TablebaseExplorer
    {
        /// <summary>
        /// Handler for the DataReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> DataReceived;

        /// <summary>
        /// Statistics and data received from Lichess
        /// </summary>
        public static LichessTablebaseResponse Response;

        /// <summary>
        /// Requests Opening Stats from lichess
        /// </summary>
        /// <returns></returns>
        public static async void TablebaseRequest(int treeId, TreeNode nd)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.TreeId = treeId;
            eventArgs.NodeId = nd.NodeId;
            try
            {
                var json = await RestApiRequest.Client.GetStringAsync("http://tablebase.lichess.ovh/standard?" + "fen=" + fen);
                Response = JsonConvert.DeserializeObject<LichessTablebaseResponse>(json);
                eventArgs.Success = true;
                DataReceived?.Invoke(null, eventArgs);
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                DataReceived?.Invoke(null, eventArgs);
            }
        }
    }

    /// <summary>
    /// The lichess json response structure
    /// </summary>
    public class LichessTablebaseResponse
    {
        public bool Checkmate;
        public bool Stalemate;
        public bool Variant_win;
        public bool Variant_loss;
        public bool Insufficient_material;
        public int dtz;
        public int precise_dtz;
        public int dtm;
        public string category;
        public LichessTablebaseMove[] Moves;
    }

    /// <summary>
    /// A move structure for the lichess response.
    /// </summary>
    public class LichessTablebaseMove
    {
        public string Uci;
        public string San;
        public bool Zeroing;
        public bool Checkmate;
        public bool Stalemate;
        public bool Variant_win;
        public bool Variant_loss;
        public bool Insufficient_material;
        public int dtz;
        public int precise_dtz;
        public int dtm;
        public string category;
    }

}
