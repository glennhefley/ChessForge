﻿using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Text;
using ChessPosition;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling Finding Identical Positions
    /// </summary>
    public class FindIdenticalPositions
    {
        /// <summary>
        /// Search mode.
        /// </summary>
        public enum Mode
        {
            FIND_AND_REPORT,
            CHECK_IF_ANY,
        }

        /// <summary>
        /// Finds positions identical to the one in the current node.
        /// Returns true if any such position found.
        /// If mode is set to CHECK_IF_ANY, this is all it does,
        /// otherwise it pops up an appropriate message or dialog for the user.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool Search(TreeNode nd, Mode mode)
        {
            AppState.MainWin.Cursor = Cursors.Wait;
            ObservableCollection<ArticleListItem> lstIdenticalPositions;
            try
            {
                lstIdenticalPositions = ArticleListBuilder.BuildIdenticalPositionsList(nd, mode == Mode.CHECK_IF_ANY, false);
            }
            finally
            {
                AppState.MainWin.Cursor = Cursors.Arrow;
            }

            bool anyFound = lstIdenticalPositions.Count > 0;

            if (mode == Mode.FIND_AND_REPORT)
            {
                if (!HasAtLeastTwoArticles(lstIdenticalPositions))
                {
                    // we only have 1 result which is the current position
                    MessageBox.Show(Properties.Resources.MsgNoIdenticalPositions, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    IdenticalPositionsExDialog dlgEx = new IdenticalPositionsExDialog(nd, ref lstIdenticalPositions)
                    {
                        Left = AppState.MainWin.ChessForgeMain.Left + 100,
                        Top = AppState.MainWin.ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = AppState.MainWin
                    };

                    if (dlgEx.ShowDialog() == true)
                    {
                        if (dlgEx.Request == IdenticalPositionsExDialog.Action.CopyArticles
                            || dlgEx.Request == IdenticalPositionsExDialog.Action.MoveArticles)
                        {
                            ProcessCopyMoveArticlesRequest(lstIdenticalPositions, dlgEx.Request);
                        }
                        else if (dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstIdenticalPositions.Count)
                        {
                            ProcessSelectedPosition(lstIdenticalPositions, dlgEx.Request, dlgEx.ArticleIndexId);
                        }
                    }
                }
            }

            return anyFound;
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested to copy/move articles
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        private static void ProcessCopyMoveArticlesRequest(ObservableCollection<ArticleListItem> lstIdenticalPositions, IdenticalPositionsExDialog.Action request)
        {
            // make a list with only games and exercises
            RemoveStudies(lstIdenticalPositions);
            string title = request == IdenticalPositionsExDialog.Action.MoveArticles ?
                           Properties.Resources.SelectItemsToMove : Properties.Resources.SelectItemsToCopy;
            SelectArticlesDialog dlg = new SelectArticlesDialog(null, title, ref lstIdenticalPositions, true)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };

            if (dlg.ShowDialog() == true)
            {
                int index = AppState.MainWin.InvokeSelectSingleChapterDialog();
                if (index >= 0)
                {
                    // perform the requested operation
                    List<ArticleListItem> articlesToInsert = new List<ArticleListItem>();
                    if (request == IdenticalPositionsExDialog.Action.CopyArticles)
                    {
                        // make copies and give them new GUIDs
                        foreach (ArticleListItem ali in lstIdenticalPositions)
                        {
                            Article newArticle = ali.Article.CloneMe();
                            newArticle.Tree.Header.SetNewTreeGuid();
                            ArticleListItem article = new ArticleListItem(ali.Chapter, ali.ChapterIndex, newArticle, ali.ArticleIndex);
                            articlesToInsert.Add(article);
                        }
                    }
                    else
                    {
                        // we are moving so store just references, not copies
                        foreach (ArticleListItem ali in lstIdenticalPositions)
                        {
                            ArticleListItem article = new ArticleListItem(ali.Chapter, ali.ChapterIndex, ali.Article, ali.ArticleIndex);
                            articlesToInsert.Add(article);
                        }
                    }

                    // TODO: move to the selected chapter
                    // make sure that if we are moving to an existing chapter, we skip the artcles already in that chapter
                }
            }
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested
        /// action on a specific item
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        /// <param name="articleIndexId"></param>
        private static void ProcessSelectedPosition(ObservableCollection<ArticleListItem> lstIdenticalPositions, IdenticalPositionsExDialog.Action request, int articleIndexId)
        {
            ArticleListItem item = lstIdenticalPositions[articleIndexId];
            uint moveNumberOffset = 0;
            if (item.Article != null && item.Article.Tree != null)
            {
                moveNumberOffset = item.Article.Tree.MoveNumberOffset;
            }
            List<TreeNode> nodelList = null;
            switch (request)
            {
                case IdenticalPositionsExDialog.Action.CopyLine:
                    nodelList = TreeUtils.CopyNodeList(item.TailLine);
                    ChfClipboard.HoldNodeList(nodelList, moveNumberOffset);
                    AppState.MainWin.PasteChfClipboard();
                    AppState.IsDirty = true;
                    break;
                case IdenticalPositionsExDialog.Action.CopyTree:
                    foreach (TreeNode node in item.TailLine[0].Parent.Children)
                    {
                        nodelList = TreeUtils.CopySubtree(node);
                        ChfClipboard.HoldNodeList(nodelList, moveNumberOffset);
                        AppState.MainWin.PasteChfClipboard();
                    }
                    AppState.IsDirty = true;
                    break;
                case IdenticalPositionsExDialog.Action.OpenView:
                    WorkbookLocationNavigator.GotoArticle(item.ChapterIndex, item.Article.Tree.ContentType, item.ArticleIndex);
                    if (item.Article.Tree.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    AppState.MainWin.SetActiveLine(item.Node.LineId, item.Node.NodeId);
                    AppState.MainWin.ActiveTreeView.SelectLineAndMove(item.Node.LineId, item.Node.NodeId);
                    break;
            }
        }

        /// <summary>
        /// Removes lines for studies and chapters that only contain studies.
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        private static void RemoveStudies(ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            List<ArticleListItem> itemsToRemove = new List<ArticleListItem>();

            ArticleListItem lastChapterItem = null;
            foreach (ArticleListItem item in lstIdenticalPositions)
            {
                if (item.Article == null)
                {
                    if (lastChapterItem != null)
                    {
                        itemsToRemove.Add(lastChapterItem);
                    }
                    lastChapterItem = item;
                }
                else if (item.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    itemsToRemove.Add(item);
                }
                else
                {
                    lastChapterItem = null;
                }
            }

            if (lastChapterItem != null)
            {
                itemsToRemove.Add(lastChapterItem);
            }

            foreach (ArticleListItem item in itemsToRemove)
            {
                lstIdenticalPositions.Remove(item);
            }
        }

        /// <summary>
        /// Checks if we have at least 2 articles i.e. 1 in addition to the currently viewed.
        /// We have to check the type of the item so that we don't count chapter items.
        /// </summary>
        /// <param name="lstPos"></param>
        /// <returns></returns>
        private static bool HasAtLeastTwoArticles(ObservableCollection<ArticleListItem> lstPos)
        {
            int count = 0;
            foreach (ArticleListItem item in lstPos)
            {
                if (item.ContentType == GameData.ContentType.STUDY_TREE
                    || item.ContentType == GameData.ContentType.MODEL_GAME
                    || item.ContentType == GameData.ContentType.EXERCISE)
                {
                    count++;
                    if (count >= 2)
                    {
                        break;
                    }
                }
            }

            return count >= 2;
        }

    }
}