using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Media.Effects;

namespace MoveOverEmpty
{
    public enum BoardStateEnum { Normal, Picked }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private Tile[,] Board;
        private BoardStateEnum BoardState;
        private int RowPicked, ColPicked;

        public Window1()
        {
            InitializeComponent();
            Reset();
        }

        public void Reset()
        {
            ConvertToColor ctc = new ConvertToColor();
            Board = new Tile[7, 7];
            BoardState = BoardStateEnum.Normal;
            RowPicked = ColPicked = -1;

            LayoutRoot.Children.Clear();

            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    Board[r, c] = new Tile(r, c);

                    Shape rect = new Ellipse();
                    rect.Margin = new Thickness(5);
                    Grid.SetRow(rect, r);
                    Grid.SetColumn(rect, c);

                    Binding bb = new Binding();
                    bb.Source = Board[r, c];
                    bb.Converter = ctc;
                    bb.Path = new PropertyPath("State");
                    rect.SetBinding(Ellipse.FillProperty, bb);
                    LayoutRoot.Children.Add(rect);

                    if (Board[r, c].State != TileStateEnum.Blocked)
                    {
                        rect.Stroke = Brushes.Black;
                        rect.StrokeThickness = 1;
                    }

                    rect.MouseDown += new MouseButtonEventHandler(rect_MouseDown);
                }
            }
        }

        void rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Shape r = sender as Shape;
            BindingExpression be = r.GetBindingExpression(Ellipse.FillProperty);
            Tile t = be.DataItem as Tile;
            BoardState = t.MakeMove(BoardState, ref RowPicked, ref ColPicked, Board);

            CheckAnyMorePossible();
        }

        private void CheckAnyMorePossible()
        {
            if (BoardState == BoardStateEnum.Picked) return;

            // Do the rows first
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    if (Board[r, c].State == TileStateEnum.Filled
                        && Board[r, c + 1].State == TileStateEnum.Filled
                        && Board[r, c + 2].State == TileStateEnum.Empty
                       )
                    {
                        return; // Move possible - so go ahead
                    }
                    if (Board[r, c].State == TileStateEnum.Empty
                        && Board[r, c + 1].State == TileStateEnum.Filled
                        && Board[r, c + 2].State == TileStateEnum.Filled
                       )
                    {
                        return; // Move possible - so go ahead
                    }
                }
            }

            // Do the cols next
            for (int c = 0; c < 7; c++)
            {
                for (int r = 0; r < 5; r++)
                {
                    if (Board[r, c].State == TileStateEnum.Filled
                        && Board[r + 1, c].State == TileStateEnum.Filled
                        && Board[r + 2, c].State == TileStateEnum.Empty
                       )
                    {
                        return; // Move possible - so go ahead
                    }
                    if (Board[r, c].State == TileStateEnum.Empty
                        && Board[r + 1, c].State == TileStateEnum.Filled
                        && Board[r + 2, c].State == TileStateEnum.Filled
                       )
                    {
                        return; // Move possible - so go ahead
                    }
                }
            }

            int count = 0;
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (Board[r, c].State == TileStateEnum.Filled)
                    {
                        count++;
                    }
                }
            }
            MessageBox.Show("Game Over. Your Score is " + count.ToString(), "Done");
            Reset();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }
    }

    public class INPC : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void FPC(string pname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(pname));
            }
        }
    }
    public enum TileStateEnum { Blocked, Empty, Filled, Picked };
    public class Tile : INPC
    {
        private int m_row, m_col;

        public Tile(int r, int c)
        {
            State = TileStateEnum.Filled;
            m_row = r;
            m_col = c;

            if (r == 3 && c == 3) State = TileStateEnum.Empty;
            else if (r == 0 && (c == 0 || c == 1 || c == 5 || c == 6)) State = TileStateEnum.Blocked;
            else if (r == 1 && (c == 0 || c == 1 || c == 5 || c == 6)) State = TileStateEnum.Blocked;
            else if (r == 5 && (c == 0 || c == 1 || c == 5 || c == 6)) State = TileStateEnum.Blocked;
            else if (r == 6 && (c == 0 || c == 1 || c == 5 || c == 6)) State = TileStateEnum.Blocked;
        }

        public TileStateEnum State
        {
            get { return m_state; }
            set { m_state = value; FPC("State");  }
        }

        TileStateEnum m_state;

        internal BoardStateEnum MakeMove(BoardStateEnum bs, ref int rowPicked, ref int colPicked, Tile[,] Board)
        {
            BoardStateEnum rbs = bs;
            if (bs == BoardStateEnum.Normal)
            {
                if (State == TileStateEnum.Filled)
                {
                    State = TileStateEnum.Picked;
                    rowPicked = m_row;
                    colPicked = m_col;
                    rbs = BoardStateEnum.Picked;
                }
            }
            else
            {
                switch (State)
                {
                    case TileStateEnum.Blocked: break; // Not allowed
                    case TileStateEnum.Filled: break; // Not allowed
                    case TileStateEnum.Picked: // Dropping it back
                        {
                            State = TileStateEnum.Filled;
                            rowPicked = -1;
                            colPicked = -1;
                            rbs = BoardStateEnum.Normal;
                            break;
                        }
                    case TileStateEnum.Empty: // We need to check if dropping here is allowed
                        {
                            if ((m_row == rowPicked)
                                && (Math.Abs(m_col - colPicked) == 2)
                                && (Board[m_row, (m_col + colPicked) / 2].State == TileStateEnum.Filled)
                               )
                            {
                                State = TileStateEnum.Filled;
                                Board[m_row, (m_col + colPicked) / 2].State = TileStateEnum.Empty;
                            }

                            if ((m_col == colPicked)
                                && (Math.Abs(m_row - rowPicked) == 2)
                                && (Board[(m_row + rowPicked) / 2, m_col].State == TileStateEnum.Filled)
                               )
                            {
                                State = TileStateEnum.Filled;
                                Board[(m_row + rowPicked) / 2, m_col].State = TileStateEnum.Empty;
                            }

                            if (State == TileStateEnum.Filled)
                            {
                                Board[rowPicked, colPicked].State = TileStateEnum.Empty;
                                rowPicked = -1;
                                colPicked = -1;
                                rbs = BoardStateEnum.Normal;
                            }

                            break;
                        }
                    default: // Do nothing
                        break;
                }
            }
            return rbs;
        }

    }
    public class ConvertToColor : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TileStateEnum tse = (TileStateEnum)value;
            Color cs, ce;
            switch (tse)
            {
                case TileStateEnum.Empty: cs = Colors.LightGoldenrodYellow; ce = Colors.SandyBrown; break;
                case TileStateEnum.Blocked: cs = Colors.White; ce = Colors.Turquoise;  break;
                case TileStateEnum.Picked: cs = Colors.Red; ce = Colors.Black; break;
                default: //case TileStateEnum.Filled:
                    cs = Colors.Blue;
                    ce = Colors.Silver;
                    break;
            }
            return new RadialGradientBrush(cs, ce);
            //return new SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
