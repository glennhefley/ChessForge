﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// EventArgs for the Web Access events
    /// </summary>
    public class WebAccessEventArgs : EventArgs
    {
        /// <summary>
        /// Whether event's result was success
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Id of the tree to which the handled Node belongs
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Id of the Node being handled.
        /// </summary>
        public int NodeId { get; set; }
    }
}
