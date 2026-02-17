using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace InfernoVEditor
{
    public class MediaExtension
    {
        Utils util = new Utils();
        MainWindow window;
        MediaElement mediaElement;

        Thread thread;
        Thread volumeThread;

        Slider mediaSlider;
        Label mediaPositionLabel;
        Label volumeLabel;

        bool isDragging = false;
        bool autoPause = false;

        float autoPauseValue = 0.0f;

        public MediaExtension(MainWindow window, MediaElement mediaElement, Slider mediaSlider, Label mediaPositionLabel, bool autoPause=false, float autoPauseValue=0.0f)
        {
            this.mediaElement = mediaElement;
            this.window = window;
            this.mediaSlider = mediaSlider;
            this.mediaPositionLabel = mediaPositionLabel;
            this.autoPause = autoPause;
            this.autoPauseValue = autoPauseValue;
        }

        private void draggingControlHandle(object sender, EventArgs e)
        {
            isDragging = true;
        }
        private void draggingControlHandleOff(object sender, EventArgs e)
        {
            Debug.WriteLine(sender);
            Slider slider = sender as Slider;
            this.mediaElement.Position =TimeSpan.FromSeconds(slider.Value);
            isDragging = false;
        }
        public void startThread()
        {
            this.window.Dispatcher.Invoke(() => {
                this.mediaSlider.AddHandler(ButtonBase.PreviewMouseDownEvent, new RoutedEventHandler(draggingControlHandle));
                this.mediaSlider.AddHandler(ButtonBase.PreviewMouseUpEvent, new RoutedEventHandler(draggingControlHandleOff));
                //this.mediaSlider.Thumb;
            });
            Regex regex = new Regex("\\d+:\\d+:\\d+");
            Match match;

            int timeUtil = 0;
            while (true)
            {
                Thread.Sleep(100);
                this.window.Dispatcher.Invoke(() =>
                {
                    match = regex.Match(this.mediaElement.Position.ToString());
                    if (match.Success)
                    {
                        timeUtil = util.TimeToSeconds(match.Groups[0].Value);
                        if (!isDragging)
                        {
                            this.mediaSlider.Value = util.TimeToSeconds(match.Groups[0].Value);

                            if(this.mediaPositionLabel != null)
                            {
                                this.mediaPositionLabel.Content = util.SecondsToTime(timeUtil);
                            }
                            if (this.autoPause)
                            {
                                if(timeUtil >= this.autoPauseValue-1 && timeUtil <= this.autoPauseValue)
                                {
                                    this.window.rclipPlay.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                    //this.mediaElement.Pause();
                                }
                            }
                            //Debug.WriteLine("Position: " + util.TimeToSeconds(match.Groups[0].Value) + ", Natural Duration: " + util.TimeToSeconds(this.mediaElement.NaturalDuration.ToString()));
                        }
                    }
                });

                if(thread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    Debug.WriteLine("Aborted Thread");
                    break;
                }
                    //Debug.WriteLine(this.mediaElement.Position);
            }
            /*this.mediaElement.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                /*while (true)
                {
                    Debug.WriteLine(this.mediaElement.Position);
                   
                    if (util.TimeToSeconds(this.mediaElement.NaturalDuration.ToString()) == util.TimeToSeconds(this.mediaElement.Position.ToString()))
                    {
                        break;
                    }
                }
            });*/
        }
        async public void PositionChanged()
        {
            this.thread = new Thread(new ThreadStart(this.startThread));
            this.thread.IsBackground = true;
            this.thread.Start();
        }

        async public void volumeChangeHandle(object sender, EventArgs e)
        {
            Slider obj = sender as Slider;
            this.mediaElement.Volume = (obj.Value);
            Debug.WriteLine(obj.Value);

            if (this.volumeLabel != null)
            {
                this.volumeLabel.Content = "Volume: " + Math.Floor(obj.Value * 100).ToString();
            }
        }

        async public void Volume(Slider volumeSlider, Label volumeLabel=null)
        {
            this.volumeLabel = volumeLabel;
            volumeSlider.ValueChanged += volumeChangeHandle;
        }

        async public void setPauseValue(float pauseValue)
        {
            this.autoPauseValue = pauseValue;
        }
        async public void setAutoPause(bool autoPause)
        {
            this.autoPause = autoPause;
        }

        async public void disposeThread()
        {
            if(thread != null)
            {
                thread.Abort();
            }
        }

        ~MediaExtension()
        {
            disposeThread();
        }
    }
}
