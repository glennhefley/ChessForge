﻿using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectArticlesDialog.xaml
    /// </summary>
    public partial class SelectArticlesDialog : Window
    {
        /// <summary>
        /// Set to the article to be acted upon exit.
        /// </summary>
        public Article SelectedArticle;

        // last clicked article
        private Article _lastClickedArticle;

        /// <summary>
        /// Whether to show articles fromthe current chapter only
        /// </summary>
        private bool _showActiveChapterOnly = true;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        // type of articles handled
        private GameData.ContentType _articleType;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        /// <summary>
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="articleList"></param>
        public SelectArticlesDialog(TreeNode nd, bool allChaptersCheckbox, string title, ref ObservableCollection<ArticleListItem> articleList, bool allChapters, GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            _node = nd;
            _articleList = articleList;
            _articleType = articleType;

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();
            if (title != null)
            {
                Title = title;
            }
            if (allChaptersCheckbox)
            {
                UiCbAllChapters.Visibility = Visibility.Visible;
            }
            else
            {
                UiCbAllChapters.Visibility = Visibility.Collapsed;
            }

            SelectNodeReferences();

            if (!allChapters)
            {
                allChapters = IsAnySelectionOutsideActiveChapter();
            }
            _showActiveChapterOnly = !allChapters;
            UiCbAllChapters.IsChecked = allChapters;

            SetItemVisibility();

            // if everything is selected, check the box
            bool isAllSelected = true;
            foreach (ArticleListItem item in _articleList)
            {
                if (!item.IsSelected)
                {
                    isAllSelected = false;
                    break;
                }
            }
            UiCbSelectAll.IsChecked = isAllSelected;

            UiLvGames.ItemsSource = _articleList;
        }

        /// <summary>
        /// Returns a list of selected references.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedReferenceStrings()
        {
            List<string> refs = new List<string>();

            foreach (ArticleListItem item in _articleList)
            {
                if (item.Article != null && item.IsSelected)
                {
                    GameData.ContentType ctype = item.Article.Tree.Header.GetContentType(out _);
                    if (ctype == GameData.ContentType.MODEL_GAME || ctype == GameData.ContentType.EXERCISE)
                    {
                        refs.Add(item.Article.Tree.Header.GetGuid(out _));
                    }
                }
            }

            return refs;
        }

        /// <summary>
        /// Marks as selected all references currently in the node.
        /// </summary>
        private void SelectNodeReferences()
        {
            if (_node == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(_node.ArticleRefs))
                {
                    string[] refs = _node.ArticleRefs.Split('|');
                    foreach (string guid in refs)
                    {
                        foreach (ArticleListItem item in _articleList)
                        {
                            if (item.Article != null)
                            {
                                if (item.Article.Tree.Header.GetGuid(out _) == guid)
                                {
                                    item.IsSelected = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks if any selected item is not in the active chapter. 
        /// </summary>
        /// <returns></returns>
        private bool IsAnySelectionOutsideActiveChapter()
        {
            bool res = false;

            foreach (ArticleListItem item in _articleList)
            {
                if (item.IsSelected && item.Chapter != WorkbookManager.SessionWorkbook.ActiveChapter)
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        /// <summary>
        /// Sets the IsShown property on all items.
        /// </summary>
        private void SetItemVisibility()
        {
            foreach (ArticleListItem item in _articleList)
            {
                if (!_showActiveChapterOnly)
                {
                    item.IsShown = true;
                }
                else
                {
                    item.IsShown = item.Chapter == WorkbookManager.SessionWorkbook.ActiveChapter;
                }
            }
        }

        /// <summary>
        /// SelectAll box was checked
        /// Check all currently shown items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleList)
            {
                if (item.IsShown && item.Article != null)
                {
                    item.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// SelectAll box was unchecked.
        /// Uncheck all currently shown items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleList)
            {
                if (item.IsShown)
                {
                    item.IsSelected = false;
                }
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// The user wants to show articles from all chapters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbAllChapters_Checked(object sender, RoutedEventArgs e)
        {
            _showActiveChapterOnly = false;
            SetItemVisibility();
        }

        /// <summary>
        /// The user wants to show articles from the active chapter only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbAllChapters_Unchecked(object sender, RoutedEventArgs e)
        {
            _showActiveChapterOnly = true;
            SetItemVisibility();
        }

        /// <summary>
        /// Identifies a List View item from the click coordinates. 
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private ListViewItem GetListViewItemFromPoint(ListView listView, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listView, point);
            if (result == null)
            {
                return null;
            }

            DependencyObject hitObject = result.VisualHit;
            while (hitObject != null && !(hitObject is ListViewItem))
            {
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            return hitObject as ListViewItem;
        }

        /// <summary>
        /// Handles a double-click event on an Article.
        /// Opens the Game Preview dialog for the clicked game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, e.GetPosition(UiLvGames));
            if (item != null && item.Content is ArticleListItem)
            {
                Article art = (item.Content as ArticleListItem).Article;
                _lastClickedArticle = art;
                InvokeGamePreviewDialog(art);
            }
        }

        private void InvokeGamePreviewDialog(Article art)
        {
            List<string> gameIdList = new List<string>();
            List<Article> games = new List<Article> { art };
            gameIdList.Add(art.Tree.Header.GetGuid(out _));

            SingleGamePreviewDialog dlg = new SingleGamePreviewDialog(gameIdList, games)
            {
                Left = this.Left + 20,
                Top = this.Top + 20,
                Topmost = false,
                Owner = this
            };
            dlg.ShowDialog();
        }

        /// <summary>
        /// Handles a right-click even on an Article.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, e.GetPosition(UiLvGames));
            if (item != null && item.Content is ArticleListItem)
            {
                Article art = (item.Content as ArticleListItem).Article;
                _lastClickedArticle = art;
                if (art != null)
                {
                    if (art.Tree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
                    {
                        UiMnPreviewGame.Header = Properties.Resources.PreviewExercise;
                        UiMnOpenGame.Header = Properties.Resources.GoToExercises;
                    }
                    else
                    {
                        UiMnPreviewGame.Header = Properties.Resources.PreviewGame;
                        UiMnOpenGame.Header = Properties.Resources.GoToGames;
                    }
                    UiCmGame.IsOpen = true;
                }
            }
        }

        private void UiLvGames_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void UiMnPreviewGame_Click(object sender, RoutedEventArgs e)
        {
            if (_lastClickedArticle != null)
            {
                InvokeGamePreviewDialog(_lastClickedArticle);
            }
        }

        private void UiMnOpenGame_Click(object sender, RoutedEventArgs e)
        {
            SelectedArticle = _lastClickedArticle;
            UiBtnOk_Click(null, null);
        }
    }
}