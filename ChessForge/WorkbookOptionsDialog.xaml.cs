﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for WorkbookOptionsDialog.xaml
    /// </summary>
    public partial class WorkbookOptionsDialog : Window
    {
        private readonly string _strWhite = "WHITE";
        private readonly string _strBlack = "BLACK";

        /// <summary>
        /// Creates the dialog, initializes controls
        /// </summary>
        public WorkbookOptionsDialog(WorkbookTree _workbook)
        {
            InitializeComponent();
            WorkbookTitle = _workbook.Title;
            TrainingSide = _workbook.TrainingSide;

            _tbTitle.Text = _workbook.Title;
            _tbSideOnMove.Text = _workbook.TrainingSide == PieceColor.Black ? _strBlack : _strWhite;
        }

        /// <summary>
        /// The Training Side selected in the dialog.
        /// </summary>
        public PieceColor TrainingSide;

        /// <summary>
        /// The Workbook Title entered by the user
        /// </summary>
        public string WorkbookTitle;

        /// <summary>
        /// True if dialog exited by clicking Save
        /// </summary>
        public bool ExitOK = false;

        /// <summary>
        /// The user pressed the OK button.
        /// Saves the workbook's title and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnOK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookTitle = _tbTitle.Text;

            if (_tbSideOnMove.Text == _strBlack)
            {
                TrainingSide = PieceColor.Black;
            }
            else
            {
                TrainingSide = PieceColor.White;
            }

            ExitOK = true;
            this.Close();
        }

        /// <summary>
        /// In response to the user clicking on the Swap icon,
        /// swaps the training side.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _imgSwapColor_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapTrainingSide();
        }

        private void SwapTrainingSide()
        {
            if (_tbSideOnMove.Text == _strWhite)
            {
                _tbSideOnMove.Text = _strBlack;
                TrainingSide = PieceColor.Black;
            }
            else
            {
                _tbSideOnMove.Text = _strWhite;
                TrainingSide = PieceColor.White;
            }
        }

        /// <summary>
        /// Exists the dialog without setting ExitOK to true.
        /// The caller should consider such exit as cancellation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnCancel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
