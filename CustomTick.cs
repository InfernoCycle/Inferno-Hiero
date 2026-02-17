using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfernoVEditor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;

    namespace InfernoVEditor
    {
        class CustomTick : TickBar
        {
            StackPanel panel;
            StackPanel HorizPanel;

            Utils util = new Utils();

            public int numOfTicks { get; set; }
            public double subWidth { get; set; }

            private int TickSpacing = 100;

            public CustomTick(int numTicks, double subWidth)
            {

                Debug.WriteLine("element width 1: " + subWidth);
                //draw tick puting everything in a panel
                this.HorizPanel = new StackPanel();
                this.HorizPanel.Orientation = Orientation.Horizontal;
                this.HorizPanel.HorizontalAlignment = HorizontalAlignment.Left;

                this.numOfTicks = numTicks;
                this.subWidth = subWidth;
            }

            private Border DrawLine()
            {
                Border border = new Border()
                {
                    BorderThickness = new Thickness()
                    {
                        Bottom = 0,
                        Left = 5,
                        Right = 0,
                        Top = 0
                    },
                    BorderBrush = new SolidColorBrush(Colors.White)
                };
                border.Margin = new Thickness(5,0,0,0);
                border.Height = 7;
                border.Width = 1;
                border.HorizontalAlignment = HorizontalAlignment.Left; 
                border.VerticalAlignment = VerticalAlignment.Center;

                return border;
            }

            private Label DrawNumber(string num)
            {
                Label label = new Label();
                label.Foreground = new SolidColorBrush(Colors.White);
                label.Content = num;
                //label.Width = 100;
                label.Margin = new Thickness(2,-10,0,0);
                label.VerticalAlignment = VerticalAlignment.Top;
                label.HorizontalAlignment = HorizontalAlignment.Left;

                return label;
            }

            private StackPanel createTicks()
            {
                //double tickSpacing = Math.Floor(this.subWidth/this.numOfTicks); //gets the spacing between ticks
                //Debug.WriteLine("Tick spacing per tick is: " + tickSpacing);
                //Debug.WriteLine("Tick num of ticks: " + numOfTicks);
                Debug.WriteLine("element width: " + subWidth);
                double numTicks = subWidth/100;
                for (int i = 0; i < numTicks; i++)
                {
                    if(i % 1000 == 0)
                    {
                        Thread.Sleep(100);
                    }
                    if (i != 0)
                    {
                        StackPanel panel = new StackPanel();
                        panel.Width = TickSpacing;
                        panel.Orientation = Orientation.Vertical;

                        panel.Margin = new Thickness(0, 0, 0, 0);
                        
                        panel.Children.Add(DrawLine());
                        panel.Children.Add(DrawNumber(util.SecondsToTime(i)));
                        
                        if(i % 2 == 0)
                        {
                            panel.Background = new SolidColorBrush(Colors.Purple);
                        }

                        HorizPanel.Children.Add(panel);
                    }
                    else
                    {
                        StackPanel panel = new StackPanel();
                        panel.Width = TickSpacing;
                        panel.HorizontalAlignment = HorizontalAlignment.Left;
                        panel.Orientation = Orientation.Vertical;

                        panel.Background = new SolidColorBrush(Colors.Purple);

                        panel.Children.Add(DrawLine());
                        panel.Children.Add(DrawNumber(util.SecondsToTime(i)));

                        HorizPanel.Children.Add(panel);
                    }
                }
                return HorizPanel;
            }

            public StackPanel getCustomTicks() {
                return this.createTicks();
            }
        }
    }

}
