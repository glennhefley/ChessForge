﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling operations on a list of IntroView elements.
    /// </summary>
    public class IntroViewClipboard
    {
        /// <summary>
        /// Types of elements supported by the view.
        /// </summary>
        public enum ElementType
        {
            None,
            Paragraph,
            Run,
            Move,
            Diagram
        }

        /// <summary>
        /// The list of elements operated on.
        /// </summary>
        public static List<IntroViewClipboardElement> Elements = new List<IntroViewClipboardElement>();

        /// <summary>
        /// Clears the list of elements.
        /// </summary>
        public static void Clear()
        {
            Elements.Clear();
        }

        /// <summary>
        /// Adds a Run element to the list.
        /// </summary>
        /// <param name="run"></param>
        public static void AddRun(Run run, Thickness? margins = null)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Run);
            if (margins != null)
            {
                element.SetMargins(margins.Value);
            }

            // make a copy of the run
            Run runToAdd = RichTextBoxUtilities.CopyRun(run);
            element.SetAsRun(runToAdd);

            Elements.Add(element);
        }

        /// <summary>
        /// Adds a paragraph element to the list.
        /// </summary>
        /// <param name="para"></param>
        public static void AddParagraph(Paragraph para)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Paragraph);

            element.SetAsParagraph(para);
            Elements.Add(element);
        }

        /// <summary>
        /// Adds a Move element to the list.
        /// </summary>
        /// <param name="node"></param>
        public static void AddMove(TreeNode node)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Move);
            element.SetAsMove(node);
            Elements.Add(element);
        }

        /// <summary>
        /// Adds a diagram element to the list.
        /// </summary>
        /// <param name="diagram"></param>
        public static void AddDiagram(TreeNode node, bool? flipped)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Diagram);
            element.SetAsDiagram(node);

            if (flipped != null)
            {
                element.BoolState = flipped;
            }

            Elements.Add(element);
        }
    }

}
