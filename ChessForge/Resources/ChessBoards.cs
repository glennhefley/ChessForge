﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using static ChessForge.ChessBoards;

namespace ChessForge
{
    /// <summary>
    /// Images for the main chess boards
    /// </summary>
    public class ChessBoards
    {
        /// <summary>
        /// Identifiers of board sets
        /// </summary>
        public enum ColorSet
        {
            BLUE,
            LIGHT_BLUE,
            LIGH_GREEN,
        }

        public static BitmapImage ChessBoardBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlueSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardLightBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightBlueSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardLightGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardLightGreenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardLightGreenSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardGrey = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGrey.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreySmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreySmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreenSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardPaleBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardPaleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardPaleBlueSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardPaleBlueSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardBrown = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrown.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBrownSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrownSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChessBoardOrangeShades = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardOrangeShades.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardOrangeShadesSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardOrangeShadesSmall.png", UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Preconfigured board color sets.
        /// </summary>
        public static Dictionary<ColorSet, BoardSet> BoardSets = new Dictionary<ColorSet, BoardSet>()
        {
            {ColorSet.BLUE,       new BoardSet(ChessBoardBlue, ChessBoardBlueSmall) },
            {ColorSet.LIGHT_BLUE, new BoardSet(ChessBoardLightBlue, ChessBoardLightBlueSmall) },
            {ColorSet.LIGH_GREEN, new BoardSet(ChessBoardLightGreen, ChessBoardLightGreenSmall) },
        };

    }

    /// <summary>
    /// Board set configuration.
    /// </summary>
    public class BoardSet
    {
        public BitmapImage MainBoard;
        public BitmapImage SmallBoard;

        public BoardSet(BitmapImage main, BitmapImage small)
        {
            MainBoard = main;
            SmallBoard = small;
        }
    }

    /// <summary>
    /// Images for the arrows to be drawn on the board.
    /// </summary>
    public class ChessBoardArrows
    {
        public static BitmapImage YellowTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage YellowStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage YellowHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleYellow.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage GreenTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleGreen.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlueTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleBlue.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage RedTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleRed.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemRed.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleRed.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage OrangeTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleOrange.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage PurpleTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTrianglePurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemPurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCirclePurple.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage DarkredTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleDarkred.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemDarkred.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleDarkred.png", UriKind.RelativeOrAbsolute));

    }

    /// <summary>
    /// Images for the square selection circles on the board.
    /// </summary>
    public class ChessBoardCircles
    {
        public static BitmapImage YellowCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage GreenCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleGreen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlueCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage RedCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleRed.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage OrangeCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleOrange.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage PurpleCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CirclePurple.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage DarkredCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/CircleDarkred.png", UriKind.RelativeOrAbsolute));
    }
}
