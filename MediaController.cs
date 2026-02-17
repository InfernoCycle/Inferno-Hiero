using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace InfernoVEditor
{
    internal class MediaController
    {
        MainWindow window;
        Utils utils;

        public MediaController(MainWindow window)
        {
            this.window = window;
        }

        public MediaController(MainWindow window, Utils utils)
        {
            this.window= window;
            this.utils = utils;
        }

        public void setUtil(Utils utils) { 
            this.utils = utils;
        }

        async public void playSrcMedia(object sender, EventArgs e)
        {
            if (this.window.SourceMedia.IsLoaded && this.window.SourceMedia.IsLoaded)
            {
                if (this.window.sourcePlay.Content == "Play")
                {
                    this.window.sourcePlay.Content = "Pause";
                    this.window.SourceMedia.Play();
                }
                else
                {
                    this.window.sourcePlay.Content = "Play";
                    this.window.SourceMedia.Pause();
                }
            }
            /*if (this.window.EditedMedia.IsLoaded && this.window.EditedMedia.IsLoaded)
            {
                if (this.window.clipPlay.Content == "Play")
                {
                    this.window.clipPlay.Content = "Pause";
                    this.window.EditedMedia.Play();
                }
                else
                {
                    this.window.clipPlay.Content = "Play";
                    this.window.EditedMedia.Pause();
                }
            }*/
        }

        async public void stopSrcMedia(object sender, EventArgs e)
        {
            if (this.window.SourceMedia.IsLoaded && this.window.SourceMedia.IsLoaded)
            {
                this.window.sourcePlay.Content = "Play";
                this.window.SourceMedia.Stop();
            }
            
           
            /*if (this.window.EditedMedia.IsLoaded && this.window.EditedMedia.IsLoaded)
            {
                this.window.clipPlay.Content = "Play";
                this.window.EditedMedia.Stop();
            }*/
        }
        public void getLength(object sender, EventArgs e)
        {

        }
        public void setSeek(object sender, EventArgs e)
        {
            Unosquare.FFME.MediaElement src = sender as Unosquare.FFME.MediaElement;
            Regex reg1 = new Regex("\\d+:\\d+:\\d+");
            Match match1 = reg1.Match(src.Position.ToString());
            if (match1.Success) {
                this.window.srcSeeker.Value = this.utils.TimeToSeconds(match1.Groups[0].Value);
            }
        }

        async public void playClipMedia(object sender, EventArgs e)
        {
            if (this.window.clipMedia.IsLoaded && this.window.clipMedia.IsLoaded)
            {
                if (this.window.rclipPlay.Content == "Play")
                {
                    this.window.rclipPlay.Content = "Pause";
                    this.window.clipMedia.Play();
                }
                else
                {
                    this.window.rclipPlay.Content = "Play";
                    this.window.clipMedia.Pause();
                }
            }
        }

        async public void stopClipMedia(object sender, EventArgs e)
        {
            if (this.window.clipMedia.IsLoaded && this.window.clipMedia.IsLoaded)
            {
                this.window.rclipPlay.Content = "Play";
                this.window.clipMedia.Stop();
            }


            /*if (this.window.EditedMedia.IsLoaded && this.window.EditedMedia.IsLoaded)
            {
                this.window.clipPlay.Content = "Play";
                this.window.EditedMedia.Stop();
            }*/
        }

    }
}
