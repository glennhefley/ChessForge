﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessForge
{
    /// <summary>
    /// INDEX LEVELS: 
    /// Index levels begin at First Index Branch Level and include the "stem line" plus the number of 
    /// SECTION_TITLE_LEVELS Branch Levels.
    /// 
    /// The root level LineSector only contains the root node of the view and is never displayed.
    /// If it only has one child, then the First Index Branch Level is set to 1, otherwise it is set to 2.
    /// 
    /// In the case of one child we will have a stem line e.g. "1.e4 e5 2.Nf3" before the first fork.
    /// The Branch Level of the stem's Line Sector equals 1.
    /// 
    /// In the case of multiple children we have no stem line and start with index entries like "A) 1.e4", "B) 1.d4" etc
    /// The Branch Level of these entries' Line Sectors equals 2. There is no Branch Level 1.
    /// 
    /// Therefore, the last Branch Level that is still an Index Level is always SECTION_TITLE_LEVELS while
    /// the first one can be 1 or 2.
    /// 
    /// Note that the Branch Level can be determined from the LineId of the first Node 
    /// by counting the number of dots and adding 1.
    /// For example: 1.1 is branch level 2 while 1.2.3.1 is level 4 
    /// 
    /// The Display Level represents the format of the paragraph in which a given Line Sector
    /// is displayed. We start with 0 for the stem line for the child/children of the root node
    /// and then increment at each fork for all children while in the index section.
    /// 
    /// Display Level is reset back to 0, when leaving the index section and then incremented
    /// at each fork other than for the first child.  First children remain at their parent's 
    /// display level which defines the "game" layout.
    /// </summary>
    public class LineSectorManager
    {
        // sector id being progressively assigned
        private int _runningSectorId = 0;

        // helper list for deletions in processing
        private List<LineSector> _lineSectorsToDelete;

        // highest branch level in the view
        private int _maxBranchLevel = -1;

        /// <summary>
        // Accessor tp the highest branch level value in the view
        /// </summary>
        public int MaxBranchLevel
        {
            get { return _maxBranchLevel; }
        }

        /// <summary>
        /// The list of LineSectors
        /// </summary>
        public List<LineSector> LineSectors;

        /// <summary>
        /// Returns true of the passed branch level is within index section levels.
        /// Since the first "true" (i.e. not the stem line) index level is 2, 
        /// the last one is 1 + VariationIndexDepth
        /// e.g if VariationIndexDepth = 3 then the first "true" index is 2 and the last one is 4.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        public bool IsIndexLevel(int branchLevel)
        {
            return branchLevel <= (_studyView.VariationIndexDepth + 1);
        }

        // hosting Study View
        private StudyTreeView _studyView;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tree"></param>
        public LineSectorManager(StudyTreeView tree)
        {
            _studyView = tree;
        }

        /// <summary>
        /// Checks if the tree has a non-empty index at level 0.
        /// If the first child of node 0 has branch level 2, then we 
        /// do not have index level 0
        /// </summary>
        /// <returns></returns>
        public bool HasIndexLevelZero()
        {
            TreeNode root = LineSectors[0].Nodes[0];
            if (root.Children.Count > 0)
            {
                int startLevel = TreeUtils.GetBranchLevel(root.Children[0].LineId);
                return startLevel == 1;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the passed branch level corresponds
        /// to the last index section level.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        public bool IsLastIndexLine(int branchLevel)
        {
            return branchLevel == (_studyView.VariationIndexDepth + 1);
        }

        /// <summary>
        /// The first branch level for index line.
        /// Normally it will be 1 unless the node 0 is fork (e.g. the tree contains
        /// multiple first moves like 1.e4 and 1.d4) 
        /// in which case it is 2.
        /// </summary>
        private int _firstIndexBranchLevel = -1;

        /// <summary>
        /// Separates lines out of the tree, sets line Ids on the nodes
        /// and places the lines in the list.
        /// </summary>
        public void BuildLineSectors(TreeNode root)
        {
            _lineSectorsToDelete = new List<LineSector>();
            _maxBranchLevel = -1;

            LineSectors = new List<LineSector>();
            LineSector rootSector = CreateRootLineSector(root);
            LineSectors.Add(rootSector);

            if (rootSector.Children.Count > 1)
            {
                _firstIndexBranchLevel = 2;
            }
            else
            {
                _firstIndexBranchLevel = 1;
            }

            ProcessChildSectors(rootSector, root);

            CombineSiblingLineSectors();
            CombineTopLineSectors();
        }

        /// <summary>
        /// Each invocation of this method builds a line sector for the flattened view of the Workbook.
        /// The method calls itself recursively to build the complete set of clean lines.
        /// </summary>
        /// <param name="nd"></param>
        private LineSector BuildLineSector(LineSector parent, TreeNode nd, int displayLevel)
        {
            _runningSectorId++;

            LineSector sector = new LineSector();
            sector.LineSectorId = _runningSectorId;
            sector.DisplayLevel = displayLevel;
            sector.BranchLevel = parent.BranchLevel + 1;
            LineSectors.Add(sector);

            sector.BranchLevel = TreeUtils.GetBranchLevel(nd.LineId);
            if (sector.BranchLevel > _maxBranchLevel)
            {
                _maxBranchLevel = sector.BranchLevel;
            }
            sector.Nodes.Add(nd);

            // add all leaf nodes that follow
            while (nd.Children.Count == 1)
            {
                nd.Children[0].LineId = nd.LineId;
                nd = nd.Children[0];
                sector.Nodes.Add(nd);
            }

            // now the nd node has either 0 children or more than 1
            if (nd.Children.Count > 1)
            {
                // mark the sector as FORKING and build a subtree from here
                sector.SectorType = LineSectorType.FORKING;
                parent.AddChild(sector);
                ProcessChildSectors(sector, nd);
            }

            if (nd.Children.Count == 0)
            {
                // we reached the end of the branch so return
                sector.SectorType = LineSectorType.LEAF;
                parent.AddChild(sector);
            }

            return sector;
        }

        /// <summary>
        /// Kicks off recursive build of LineSectors 
        /// for child modes.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="nd"></param>
        private void ProcessChildSectors(LineSector parent, TreeNode nd)
        {
            // if child level is within index levels, proceeed in normal order, otherwise apply section layout
            if (IsIndexLevel(parent.BranchLevel + 1) || parent.Parent == null)
            {
                foreach (TreeNode child in nd.Children)
                {
                    BuildLineSector(parent, child, parent.DisplayLevel + 1);
                }
            }
            else
            {
                int displayLevel = parent.DisplayLevel;

                if (nd.Children.Count > 0)
                {
                    // this is not a root's child so we will have at least 2 children
                    // process the first one last as we are in "Game" layout now rather than "Sections" one.
                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        BuildLineSector(parent, nd.Children[i], displayLevel + 1);
                    }
                    LineSector built = BuildLineSector(parent, nd.Children[0], parent.DisplayLevel);
                    if (nd.Children.Count > 0)
                    {
                        parent.Nodes.Add(nd.Children[0]);
                        built.Nodes.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Combines siblings sectors when one is a top line sector and the other
        /// is a leaf sector.
        /// Note that the target top line sector is at position 1 in the parent as we have swapped the positions
        /// when generating the list of Sectors.
        /// </summary>
        private void CombineSiblingLineSectors()
        {
            foreach (LineSector lineSector in LineSectors)
            {
                if (!IsIndexLevel(lineSector.BranchLevel - 1) && lineSector.Parent != null)
                {
                    // do not proceed if Parent.Parent == null 'coz we then get a parenthesis first (after the 0 move!)
                    if (lineSector.Parent.Children.Count == 2 && lineSector.Parent.Parent != null && lineSector.Parent.Children[1] == lineSector && lineSector.Parent.Children[0].SectorType == LineSectorType.LEAF)
                    {
                        int index = 0;
                        lineSector.Parent.Children[1].InsertOpenBracketNode(index);
                        index++;
                        foreach (TreeNode nd in lineSector.Parent.Children[0].Nodes)
                        {
                            lineSector.Parent.Children[1].Nodes.Insert(index, nd);
                            index++;
                        }
                        lineSector.Parent.Children[1].InsertCloseBracketNode(index);
                        _lineSectorsToDelete.Add(lineSector.Parent.Children[0]);
                        lineSector.Parent.Children.RemoveAt(0);
                    }
                }
            }
            foreach (LineSector s in _lineSectorsToDelete)
            {
                LineSectors.Remove(s);
            }
        }

        /// <summary>
        /// Merges top level LineSectors that may have appeared
        /// after combining leaf sectors with siblings.
        /// </summary>
        private void CombineTopLineSectors()
        {
            _lineSectorsToDelete.Clear();
            foreach (LineSector lineSector in LineSectors)
            {
                while (true)
                {
                    if (!MergeTopLines(lineSector))
                    {
                        break;
                    }
                }
            }
            foreach (LineSector s in _lineSectorsToDelete)
            {
                LineSectors.Remove(s);
            }
        }

        /// <summary>
        /// Merges 2 top line sectors.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool MergeTopLines(LineSector target)
        {
            bool merged = false;

            if (target.Children.Count == 1)
            {
                // do not process sectors already marked for deletion
                if (_lineSectorsToDelete.Find(x => x == target.Children[0]) == null && _lineSectorsToDelete.Find(x => x == target) == null)
                {
                    foreach (TreeNode nd in target.Children[0].Nodes)
                    {
                        target.Nodes.Add(nd);
                    }
                    foreach (LineSector child in target.Children[0].Children)
                    {
                        target.Children.Add(child);
                        child.Parent = target;
                    }
                    _lineSectorsToDelete.Add(target.Children[0]);
                    target.Children.Remove(target.Children[0]);

                    merged = true;
                }
            }

            return merged;
        }

        /// <summary>
        /// Creates the root LineSector
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private LineSector CreateRootLineSector(TreeNode root)
        {
            _runningSectorId = 0;

            LineSector rootSector = new LineSector();
            rootSector.Nodes.Add(root);
            rootSector.LineSectorId = _runningSectorId;
            rootSector.BranchLevel = 0;
            rootSector.DisplayLevel = -1;

            return rootSector;
        }

    }
}
