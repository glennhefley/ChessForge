﻿using ChessPosition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Identifies all duplicates of the passed article
    /// and allows the user to determine which ones to keep and which ones to delete.
    /// </summary>
    public partial class SelectDuplicatesDialog : Window
    {
        // Collection bound to the ListView control
        public ObservableCollection<DuplicateListItem> DuplicateList = new ObservableCollection<DuplicateListItem>();

        /// <summary>
        /// Creates the dialog, builds the list of items to display
        /// in the list view, and binds them.
        /// </summary>
        /// <param name="article"></param>
        public SelectDuplicatesDialog(ObservableCollection<DuplicateListItem> duplicateList)
        {
            InitializeComponent();

            string title = Properties.Resources.RemoveDuplicates;
            Title = TextUtils.RemoveTrailingDots(title);

            // populate _duplicateList
            InsertEmptyItems(duplicateList);
            UiLvArticles.ItemsSource = DuplicateList;
        }

        /// <summary>
        /// Inserts empty lines in between the duplicate sets.
        /// </summary>
        /// <param name="duplicateList"></param>
        private void InsertEmptyItems(ObservableCollection<DuplicateListItem> duplicateList)
        {
            DuplicateList.Clear();
            for (int i = 0; i < duplicateList.Count; i++)
            {
                DuplicateList.Add(duplicateList[i]);
                if (i < duplicateList.Count - 1 && duplicateList[i].DuplicateNo != duplicateList[i+1].DuplicateNo)
                {
                    DuplicateList.Add(new DuplicateListItem(null));
                }
            }
        }

        /// <summary>
        /// Close the dialog on OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Close the dialog on cancel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Remove-Duplicates-Dialog");
        }
    }
}
