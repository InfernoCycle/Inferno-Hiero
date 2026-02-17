using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfernoVEditor
{
    internal class Resizer
    {
        private MainWindow window;
        double origWidth = 1300;
        double origHeight = 700;
        double sizeDiff = 0;

        double heightChange = 0;

        int timesRate = 0;

        int sizeIndex = 0;

        public Resizer(MainWindow window) { 
            this.window = window; 
        }

        //media original size is 
        public void MediaResize(object sender, EventArgs e)
        {
            MainWindow newWindow = sender as MainWindow;
            newWindow.HomeMiddleGrid.Height = newWindow.HomeFileSelectionScroll.ActualHeight; //set middle grid size to match that of the left side since it's more trustable

            //add all the first 3 rows heights then subtract by the left sides height (left sides height value was the only one that changed) to get the remaining open space that's usable
            double l = newWindow.HomeMiddleGrid.RowDefinitions[0].ActualHeight + newWindow.HomeMiddleGrid.RowDefinitions[1].ActualHeight + newWindow.HomeMiddleGrid.RowDefinitions[2].ActualHeight;
            double restOfAvailableSpace = newWindow.HomeFileSelectionScroll.ActualHeight - l-10;

            //Debug.WriteLine("l: " + l);
            //Debug.WriteLine("rest of available: " + restOfAvailableSpace);

            //set the last row's contents to take up the rest of the space calculated above
            newWindow.fileInfoScroller.Height = restOfAvailableSpace;

            //media changing area
            if (newWindow.ActualWidth >= 1800 && newWindow.ActualHeight >= 900)
            {
                newWindow.SourceMedia.Width = 600;
                newWindow.SourceMedia.Height = 450;

                newWindow.EditedMedia.Width = 600;
                newWindow.EditedMedia.Height = 450;
            }
            else
            {
                newWindow.SourceMedia.Width = 400;
                newWindow.SourceMedia.Height = 300;

                newWindow.EditedMedia.Width = 600;
                newWindow.EditedMedia.Height = 300;

                newWindow.fileInfoScroller.Height = 290;
            }

            /*newWindow.ActualTimeLineDock.Width = newWindow.ActualWidth-400-20;
            Debug.WriteLine("timeLineDock width: " + newWindow.ActualTimeLineDock.Width);
            Debug.WriteLine("timeLineDock actual width: " + newWindow.ActualTimeLineDock.ActualWidth);
            //Debug.WriteLine(newWindow.ActualWidth);
            //Debug.WriteLine(newWindow.ActualHeight);*/
        }
    }
}
