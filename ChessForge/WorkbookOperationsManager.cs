﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Monitors users operations for the purpose
    /// of undoing them on request.
    /// Objects of this type will be placed in:
    /// - VariationTrees: for the purpose of undoing move editing
    /// - Chapters: for undoing Game/Exercise deletions
    /// - Workbook: for undoing Chapter deletions
    /// </summary>
    public class WorkbookOperationsManager
    {
        // queue of operations
        private Queue<WorkbookOperation> _operations = new Queue<WorkbookOperation>();

        // parent tree if hosted in a VariationTree
        private VariationTree _owningTree;

        // parent chapter if hosted in a chapter
        private Chapter _owningChapter;

        // parent workbook if hosted in a workbook
        private Workbook _owningWorkbook;

        /// <summary>
        /// Contructor for OperationsManager created in a VariationTree
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(VariationTree tree)
        {
            _owningTree = tree;
        }

        /// <summary>
        /// Contructor for OperationsManager created in a Chapter
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(Chapter chapter)
        {
            _owningChapter = chapter;
        }

        /// <summary>
        /// Contructor for OperationsManager created in a Workbook
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(Workbook workbook)
        {
            _owningWorkbook = workbook;
        }

        /// <summary>
        /// Clears the Operations queue
        /// </summary>
        public void Reset()
        {
            _operations.Clear();
        }
    }
}
