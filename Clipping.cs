using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace InfernoVEditor
{
    class Clipping
    {
        MainWindow window;
        ControlTemplate clonedTemplate;

        Border border = new Border();
        Slider slider = new Slider();
        Canvas canvas;
        Thumb clipThumb;
        Utils util = new Utils();
        public ObservableCollection<Clips> savedClips = new ObservableCollection<Clips>();

        public List<string> ChoosableFiles { get; set; } = new List<string>();
        public List<int> index { get; set; } = new List<int>();

        App application;
        Projects prj;

        private double clipSliderWidth = 381.96;

        string currentProject = "";
        List<string> clipNames = new List<string>();

        bool editMode = false;

        private double clipInPosition = 0;
        public double clipOutPosition = 0;

        private double clipInStart = 0;
        public double clipOutEnd = 0;

        private bool hasVideo = false;
        private bool hasAudio = false;

        private int selectedClipIndex = -1;

        private MediaController media;

        private MediaExtension extension;

        // NEW: Timeline integration
        private TimelineManager _timelineManager;

        public Clipping() { }

        public Clipping(MainWindow window)
        {
            this.window = window;

            this.clonedTemplate = this.window.getControlTemplateClone("clipSliderStyle");

            if (this.clonedTemplate != null)
            {
                this.window.clipSlider.Template = this.clonedTemplate;
                this.window.clipSlider.ApplyTemplate();

                //find the border
                if (this.window.clipSlider.Template.FindName("TrackBackground", this.window.clipSlider) != null)
                {
                    this.border = (Border)this.window.clipSlider.Template.FindName("TrackBackground", this.window.clipSlider);
                }

                //find thumb
                if (this.window.clipSlider.Template.FindName("Thumb", this.window.clipSlider) != null)
                {
                    this.clipThumb = (Thumb)this.window.clipSlider.Template.FindName("Thumb", this.window.clipSlider);
                    this.clipThumb.DragDelta += this.thumbChangePosition;
                }

                //find canvas
                if (this.window.clipSlider.Template.FindName("clipSliderCanvas", this.window.clipSlider) != null)
                {
                    this.canvas = (Canvas)this.window.clipSlider.Template.FindName("clipSliderCanvas", this.window.clipSlider);
                }
            }
        }

        public Clipping(MainWindow window, App application, MediaController mediaController)
        {
            this.window = window;
            this.application = application;
            this.prj = this.application.getProjectSingleton().getProject();
            this.media = mediaController;

            InitializeSlider();
            InitializeTimeline();
        }

        // NEW: Initialize timeline
        private void InitializeTimeline()
        {
            _timelineManager = new TimelineManager();

            // Subscribe to events
            _timelineManager.TimelineChanged += OnTimelineChanged;
            _timelineManager.ClipAdded += OnClipAddedToTimeline;

            // Add tracks
            _timelineManager.AddTrack(TrackType.Video); // V1
            _timelineManager.AddTrack(TrackType.Video); // V2
            _timelineManager.AddTrack(TrackType.Audio); // A1
            _timelineManager.AddTrack(TrackType.Audio); // A2

            // Initial timeline setup
            UpdateTimelineWidth();
            RenderTimeRuler();
        }

        private void InitializeSlider()
        {
            this.clonedTemplate = this.window.getControlTemplateClone("clipSliderStyle");

            if (this.clonedTemplate != null)
            {
                this.window.clipSlider.Template = this.clonedTemplate;
                this.window.clipSlider.ApplyTemplate();

                if (this.window.clipSlider.Template.FindName("TrackBackground", this.window.clipSlider) != null)
                {
                    this.border = (Border)this.window.clipSlider.Template.FindName("TrackBackground", this.window.clipSlider);
                }

                if (this.window.clipSlider.Template.FindName("Thumb", this.window.clipSlider) != null)
                {
                    this.clipThumb = (Thumb)this.window.clipSlider.Template.FindName("Thumb", this.window.clipSlider);
                    this.clipThumb.DragDelta += this.thumbChangePosition;
                }

                if (this.window.clipSlider.Template.FindName("clipSliderCanvas", this.window.clipSlider) != null)
                {
                    this.canvas = (Canvas)this.window.clipSlider.Template.FindName("clipSliderCanvas", this.window.clipSlider);
                }
            }
        }

        public void setExtension(MediaExtension extension)
        {
            this.extension = extension;
        }

        async Task<HomeFiles> getHomeFile(string filePath)
        {
            for (int i = 0; i < this.prj.UserProjects.Count; i++)
            {
                if (this.prj.UserProjects[i].ProjectName == this.currentProject)
                {
                    for (int k = 0; k < this.prj.UserProjects[i].homeFiles.Count; k++)
                    {
                        if (this.prj.UserProjects[i].homeFiles[k].projectFilePath == filePath)
                        {
                            return this.prj.UserProjects[i].homeFiles[k];
                        }
                    }
                }
            }
            return null;
        }

        public void loadClips() //load the clips already saved on the page upon starting application
        {
            for (int i = 0; i < this.application.getProjectSingleton().getProject().UserProjects.Count; i++)
            {
                if (this.prj.UserProjects[i].ProjectName == this.currentProject)
                {
                    for (int k = 0; k < this.prj.UserProjects[i].clips.Count; k++)
                    {
                        Clips theClip = this.prj.UserProjects[i].clips[k];

                        this.savedClips.Add(theClip);
                        clipNames.Add(theClip.clipName);
                    }
                    break;
                }
            }
            // Update DataGrid binding if not already set
            if (this.window.ClipLibraryDataGrid.ItemsSource == null)
            {
                this.window.ClipLibraryDataGrid.ItemsSource = savedClips;
            }
        }

        public void setGlobalValues(string currentProject)
        {
            this.currentProject = currentProject;
        }

        //function to show clip in and out spots (clip in is red and clip out is purple)
        //clip in color is: #FFF95000
        //clip out color is: FF9C00F9
        public void clipInOutClicked(object sender, EventArgs e)
        {
            //Formula to use is: W = (Video Position/Duration) * 391.96 
            double w = 0;
            Button btn = (Button)sender;

            //was original 391.96
            w = (this.window.clipSlider.Value / this.window.clipSlider.Maximum) * this.clipSliderWidth;

            if (btn.Content.ToString() == "Clip In")
            {

                this.clipInStart = this.window.clipSlider.Value;

                this.window.curClipFrom.Content = util.SecondsToTime(int.Parse(this.clipInStart.ToString())) + ".000";

                if (this.clipInStart < this.clipOutEnd)
                {
                    btn.Content = "No Clip In";
                    //Create clip in Mark
                    string clipHex = "FFF95000";
                    System.Windows.Shapes.Rectangle rec = new();
                    rec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                            Convert.ToByte(clipHex.Substring(0, 2), 16),
                            Convert.ToByte(clipHex.Substring(2, 2), 16),
                            Convert.ToByte(clipHex.Substring(4, 2), 16),
                            Convert.ToByte(clipHex.Substring(6, 2), 16)
                            ));
                    rec.Height = 20;
                    rec.Width = 3;
                    rec.StrokeThickness = 2;

                    rec.Name = "clipIn";
                    rec.Margin = new Thickness(w, -7, 0, 0);

                    this.canvas.Children.Add(rec);
                    this.clipInPosition = w;
                }
                else
                {
                    this.window.curClipFrom.Content = "00:00:00.000";
                    this.clipInStart = 0;
                }
                if (this.editMode)
                {
                    Debug.WriteLine("Add the clipIn for: " + this.savedClips[selectedClipIndex].clipName);
                }
            }
            else if (btn.Content.ToString() == "No Clip In")
            {
                btn.Content = "Clip In";

                for (int i = 0; i < this.canvas.Children.Count; i++)
                {
                    System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                    if (temp.Name == "clipIn")
                    {
                        this.canvas.Children.Remove(temp);
                    }
                }

                this.window.curClipFrom.Content = "00:00:00.000";
                this.clipInStart = 0;
                this.clipInPosition = 0;

                if (this.editMode)
                {
                    Debug.WriteLine("Remove the clipIn for: " + this.savedClips[selectedClipIndex].clipName);
                }
            }

            //Clip Out
            if (btn.Content.ToString() == "Clip Out")
            {
                this.clipOutEnd = this.window.clipSlider.Value;
                this.window.curClipTo.Content = util.SecondsToTime(int.Parse(this.clipOutEnd.ToString())) + ".000";

                if (this.clipOutEnd > this.clipInStart)
                {

                    btn.Content = "No Clip Out";

                    string clipHex = "FF9C00F9";
                    System.Windows.Shapes.Rectangle rec = new();
                    rec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                            Convert.ToByte(clipHex.Substring(0, 2), 16),
                            Convert.ToByte(clipHex.Substring(2, 2), 16),
                            Convert.ToByte(clipHex.Substring(4, 2), 16),
                            Convert.ToByte(clipHex.Substring(6, 2), 16)
                            ));
                    rec.Height = 20;
                    rec.Width = 3;
                    rec.StrokeThickness = 2;

                    rec.Name = "clipOut";
                    rec.Margin = new Thickness(w, -7, 0, 0);

                    this.canvas.Children.Add(rec);

                    this.clipOutPosition = w;
                }
                else
                {
                    this.clipOutEnd = this.window.clipSlider.Maximum;
                    this.window.curClipTo.Content = util.SecondsToTime(int.Parse(this.clipOutEnd.ToString())) + ".000";
                }

                if (this.editMode)
                {
                    Debug.WriteLine("Add the clipOut for: " + this.savedClips[selectedClipIndex].clipName);
                }
            }
            else if (btn.Content.ToString() == "No Clip Out")
            {
                btn.Content = "Clip Out";

                for (int i = 0; i < this.canvas.Children.Count; i++)
                {
                    System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                    if (temp.Name == "clipOut")
                    {
                        this.canvas.Children.Remove(temp);
                    }
                }

                this.clipOutEnd = this.window.clipSlider.Maximum;
                this.window.curClipTo.Content = util.SecondsToTime(int.Parse(this.clipOutEnd.ToString())) + ".000";
                this.clipOutPosition = 0;

                if (this.editMode)
                {
                    Debug.WriteLine("Remove the clipOut for: " + this.savedClips[selectedClipIndex].clipName);
                }
            }
        }

        private void thumbChangePosition(object sender, DragDeltaEventArgs e)
        {
            Debug.WriteLine("Position: " + this.clipThumb.RenderTransform.Value.OffsetX);
        }

        private async void WriteToProject()
        {
            FileStream fstream = this.application.getProjectFileStream();
            fstream.Position = 0;
            fstream.SetLength(0);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this.application.getProjectSingleton().getProject()));
            fstream.Write(bytes);
            fstream.Flush();
        }

        public void create_clip(object sender, EventArgs e)
        {
            if (!clipNames.Contains(this.window.clipNameInput.Text))
            {
                if (this.window.clipMedia.Source != null && this.window.clipNameInput.Text != "")
                {

                    Clips newClip = new Clips();
                    newClip.clipName = this.window.clipNameInput.Text;
                    newClip.start = (float)clipInStart;
                    newClip.end = (float)clipOutEnd;
                    newClip.sourcePath = this.window.clipMedia.Source.ToString();
                    newClip.hasVideo = this.hasVideo;
                    newClip.hasAudio = this.hasAudio;
                    //newClip.duration = util.TimeToSeconds(this.window.clipMedia.NaturalDuration.ToString());

                    //insert Valid name
                    //clipNames.Append(this.window.clipNameInput.Text);
                    Debug.WriteLine("Current File Selected: " + this.window.clipMedia.Source);
                    Debug.WriteLine("Clip Name: " + this.window.clipNameInput.Text + ", Start: " + this.clipInStart + ", End: " + this.clipOutEnd);
                    Debug.WriteLine("Clip Name: " + this.window.clipNameInput.Text + ", Clip In Pos: " + clipInPosition + ", Clip Out Pos: " + clipOutPosition);
                    Debug.WriteLine("");

                    // Add to ObservableCollection (DataGrid will auto-update)
                    this.savedClips.Add(newClip);
                    this.clipNames.Add(newClip.clipName);

                    Debug.WriteLine("New Stuff: " + JsonSerializer.Serialize(this.application.getProjectSingleton().getProject().UserProjects));

                    //write to project file for update
                    this.WriteToProject();

                    MessageBox.Show($"Clip '{newClip.clipName}' added! Now drag it to the timeline below.", "Clip Created");
                }
            }
            else
            {
                MessageBox.Show("Name Already Exist, Please Choose a New One.");
            }
        }

        async public void addNewSource(object sender, EventArgs e)
        {
            this.editMode = false;
            Label l = (Label)sender;
            ToolTip tt = l.ToolTip as ToolTip;
            this.window.clipMedia.Source = new Uri(tt.Content.ToString(), UriKind.Relative);
            this.window.clipMedia.Pause();
            this.window.rclipPlay.Content = "Play";
            this.window.curClipFrom.Content = "00:00:00.000";
            this.clipInStart = 0;
            this.clipOutEnd = this.window.clipSlider.Maximum;
            this.clipInPosition = 0;
            this.clipOutPosition = this.clipSliderWidth;

            this.window.clipNameInput.IsEnabled = true;
            this.window.createClipBtn.IsEnabled = true;

            HomeFiles hf = await getHomeFile(tt.Content.ToString());
            if (hf != null)
            {
                this.hasAudio = hf.hasAudio;
                this.hasVideo = hf.hasVideo;
            }
            Debug.WriteLine("Canvas Children: " + this.canvas.Children.Count);
            this.extension.setAutoPause(false);

            for (int i = 0; i < this.canvas.Children.Count; i++)
            {
                Debug.WriteLine("Canvas Children: " + this.canvas.Children[i]);
                System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                if (temp.Name == "clipOut")
                {
                    this.window.clipOutBtn.Content = "Clip Out";
                    this.canvas.Children.Remove(temp);
                }
            }

            for (int i = 0; i < this.canvas.Children.Count; i++)
            {
                System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                if (temp.Name == "clipIn")
                {
                    this.window.clipInBtn.Content = "Clip In";
                    this.canvas.Children.Remove(temp);
                }
            }
        }

        // NEW: Handle DataGrid row selection for editing
        public void OnClipSelected(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is Clips selectedClip)
            {
                this.selectedClipIndex = savedClips.IndexOf(selectedClip);
                Debug.WriteLine($"Selected clip: {selectedClip.clipName} at index {selectedClipIndex}");
                LoadClipForEditing(selectedClip);
            }
        }

        private void LoadClipForEditing(Clips clip)
        {
            // Clear existing markers
            for (int i = this.canvas.Children.Count - 1; i >= 0; i--)
            {
                if (this.canvas.Children[i] is System.Windows.Shapes.Rectangle temp)
                {
                    if (temp.Name == "clipOut" || temp.Name == "clipIn")
                    {
                        this.canvas.Children.Remove(temp);
                    }
                }
            }

            this.editMode = true;
            this.clipOutEnd = clip.end;
            this.clipInStart = clip.start;

            this.window.clipSlider.Value = clip.start;
            this.window.clipNameInput.IsEnabled = false;
            this.window.createClipBtn.IsEnabled = false;

            // Load source media
            this.window.clipMedia.Source = new Uri(clip.sourcePath, UriKind.Relative);
            this.window.clipMedia.Play();
            this.window.rclipPlay.Content = "Pause";
            this.window.clipMedia.Position = TimeSpan.FromSeconds(clip.start);

            this.extension.setPauseValue(clip.start);

            // Calculate positions
            double w = (clip.start / clip.duration) * this.clipSliderWidth;
            this.clipInPosition = w;

            w = (clip.end / clip.duration) * this.clipSliderWidth;
            this.clipOutPosition = w;

            // Add clip out marker
            this.window.clipOutBtn.Content = "No Clip Out";
            AddMarker("clipOut", this.clipOutPosition, "FF9C00F9");

            // Add clip in marker
            this.window.clipInBtn.Content = "No Clip In";
            AddMarker("clipIn", this.clipInPosition, "FFF95000");
        }

        private void AddMarker(string name, double position, string colorHex)
        {
            System.Windows.Shapes.Rectangle rec = new();
            rec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    Convert.ToByte(colorHex.Substring(0, 2), 16),
                    Convert.ToByte(colorHex.Substring(2, 2), 16),
                    Convert.ToByte(colorHex.Substring(4, 2), 16),
                    Convert.ToByte(colorHex.Substring(6, 2), 16)
                    ));
            rec.Height = 20;
            rec.Width = 3;
            rec.StrokeThickness = 2;
            rec.Name = name;
            rec.Margin = new Thickness(position, -7, 0, 0);
            this.canvas.Children.Add(rec);
        }

        private void ClickEvent(object sender, EventArgs e) /* THE CLICK EVENT FOR WHEN SELECTING A CLIP*/
        {
            this.clipInPosition = 0;
            StackPanel p = (StackPanel)sender;

            string[] flat = p.Name.Split("_");
            if (flat.Length > 1)
            {
                //this.extension.setAutoPause(true);

                for (int i = 0; i < this.canvas.Children.Count; i++)
                {
                    System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                    if (temp.Name == "clipOut")
                    {
                        this.canvas.Children.Remove(temp);
                    }
                }

                for (int i = 0; i < this.canvas.Children.Count; i++)
                {
                    System.Windows.Shapes.Rectangle temp = (System.Windows.Shapes.Rectangle)this.canvas.Children[i];
                    if (temp.Name == "clipIn")
                    {
                        this.canvas.Children.Remove(temp);
                    }
                }

                this.editMode = true;
                int index = int.Parse(flat[1]);
                string selectedClip = this.clipNames[index];
                this.selectedClipIndex = index;
                Debug.WriteLine(this.clipNames[index]);
                Debug.WriteLine(this.savedClips[index].sourcePath);

                this.clipOutEnd = this.savedClips[index].end;
                this.clipInStart = this.savedClips[index].start;

                double maxSliderValue = 0;
                for (int i = 0; i < this.savedClips.Count; i++)
                {
                    if (this.savedClips[i].clipName == selectedClip)
                    {
                        Debug.WriteLine("Saved Clip Name: " + this.savedClips[i].duration);
                        maxSliderValue = this.savedClips[i].duration;
                    }
                }

                Label startPart = (Label)p.Children[2]; //get start clip values
                Debug.WriteLine(startPart.Content);

                Label endPart = (Label)p.Children[3]; //get end clip value
                Debug.WriteLine(endPart.Content);

                //this.window.clipSlider.Value = double.Parse(startPart.Content.ToString());
                this.window.clipSlider.Value = this.savedClips[index].start;

                this.window.clipNameInput.IsEnabled = false;
                this.window.createClipBtn.IsEnabled = false;

                //add source clip timestamp AND START IT IMMEDIATELY
                this.window.clipMedia.Source = new Uri(this.savedClips[index].sourcePath, UriKind.Relative);
                this.window.clipMedia.Play();
                this.window.rclipPlay.Content = "Pause";
                this.window.clipMedia.Position = TimeSpan.FromSeconds(double.Parse(startPart.Content.ToString()));

                this.extension.setPauseValue(float.Parse(startPart.Content.ToString()));

                //Formula to use for already determined value is: W = (Video Position/Duration) * 391.96 
                double w = 0;
                w = (int.Parse(startPart.Content.ToString()) / maxSliderValue) * this.clipSliderWidth;
                Debug.WriteLine("Position: " + w);

                this.clipInPosition = w;

                w = (int.Parse(endPart.Content.ToString()) / maxSliderValue) * this.clipSliderWidth;
                Debug.WriteLine("Position: " + w);

                this.clipOutPosition = w;

                //clip out insert----------------------------------------------------------------------------------------------------------
                this.window.clipOutBtn.Content = "No Clip Out";

                string clipHex = "FF9C00F9";
                System.Windows.Shapes.Rectangle rec = new();
                rec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                        Convert.ToByte(clipHex.Substring(0, 2), 16),
                        Convert.ToByte(clipHex.Substring(2, 2), 16),
                        Convert.ToByte(clipHex.Substring(4, 2), 16),
                        Convert.ToByte(clipHex.Substring(6, 2), 16)
                        ));
                rec.Height = 20;
                rec.Width = 3;
                rec.StrokeThickness = 2;

                rec.Name = "clipOut";
                rec.Margin = new Thickness(this.clipOutPosition, -7, 0, 0);

                this.canvas.Children.Add(rec);

                //clip in insert----------------------------------------------------------------------------------------------------------
                this.window.clipInBtn.Content = "No Clip In";
                //Create clip in Mark
                clipHex = "FFF95000";
                System.Windows.Shapes.Rectangle rec2 = new();
                rec2.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                        Convert.ToByte(clipHex.Substring(0, 2), 16),
                        Convert.ToByte(clipHex.Substring(2, 2), 16),
                        Convert.ToByte(clipHex.Substring(4, 2), 16),
                        Convert.ToByte(clipHex.Substring(6, 2), 16)
                        ));
                rec2.Height = 20;
                rec2.Width = 3;
                rec2.StrokeThickness = 2;

                rec2.Name = "clipIn";
                rec2.Margin = new Thickness(this.clipInPosition, -7, 0, 0);

                this.canvas.Children.Add(rec2);
            }
        }

        private void insertMarkers()
        {

        }

        public void buildMasterFileStackElement()
        {
            //main element for the stacks
            StackPanel stackPanel = this.window.FileChooserStack;
            stackPanel.Children.Clear();
            Regex regex = new Regex("[\\/\\\\]");


            Debug.WriteLine("Choosable count: " + this.ChoosableFiles.Count);
            for (int i = 0; i < this.ChoosableFiles.Count; i++)
            {
                Border border = new Border()
                {
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = new SolidColorBrush(Colors.Black)
                };
                Label fileNameLabel = new Label();
                fileNameLabel.Background = new SolidColorBrush(Colors.Gray);
                TextBlock textBlock = new TextBlock();
                textBlock.TextWrapping = TextWrapping.NoWrap;
                textBlock.Text = this.ChoosableFiles[i];
                string[] temp = regex.Split(this.ChoosableFiles[i]);
                fileNameLabel.Content = temp[temp.Length - 1];
                ToolTip toolTip = new ToolTip();
                toolTip.Content = this.ChoosableFiles[i];
                fileNameLabel.ToolTip = toolTip;
                fileNameLabel.MouseEnter += HoverEvent;
                fileNameLabel.MouseLeave += UnHoverEvent;
                fileNameLabel.AddHandler(Label.MouseDownEvent, new RoutedEventHandler(this.addNewSource));

                border.Child = fileNameLabel;

                stackPanel.Children.Add(border);
            }
        }

        public void saveClip(object sender, EventArgs e) //saves clip in a designated area of your choice
        {
            if (selectedClipIndex == -1)
            {
                MessageBox.Show("Select a Clip first.");
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog()
            {
                AddExtension = true,
                CreatePrompt = true,
                DefaultExt = "Media Files|*.mp4;*.mp3;*.mov;*.vlc;*.flv;*.ogg",
            };
            dialog.ShowDialog();

            Regex r = new Regex("\\/|\\\\");
            string[] splitDirectoryName = r.Split(dialog.FileName);
            string saveDirectory = "";
            for (int i = 0; i < splitDirectoryName.Length; i++)
            {
                if (i == splitDirectoryName.Length - 1) { break; }
                saveDirectory += splitDirectoryName[i] + "/";
            }
            Debug.WriteLine("Save Directory: " + saveDirectory);

            Popup clipDownload = new Popup();
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            ProgressBar progressBar = new ProgressBar();
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;
            progressBar.Height = 30;
            progressBar.Width = 250;

            Label progressLabel = new Label();
            progressLabel.Content = "0%";
            progressLabel.VerticalAlignment = VerticalAlignment.Center;

            Button btn = new Button();
            btn.Content = "OK";
            btn.Visibility = Visibility.Visible;

            stackPanel.Children.Add(progressBar);
            stackPanel.Children.Add(progressLabel);
            stackPanel.Children.Add(btn);

            Window newWindow = new Window();
            newWindow.Content = stackPanel;
            newWindow.Show();

            newWindow.Width = 300;
            newWindow.Height = 150;

            Debug.WriteLine("Clip Name: " + this.savedClips[selectedClipIndex].clipName);
            Debug.WriteLine("End: " + ((float)this.clipOutEnd).ToString());
            Debug.WriteLine("Start: " + ((float)this.clipInStart).ToString());

            util.transcode_clip(this.window, progressBar, progressLabel, this.savedClips[selectedClipIndex].sourcePath, dialog.FileName, (float)this.clipInStart, (float)this.clipOutEnd, this.savedClips[selectedClipIndex].hasVideo, this.savedClips[selectedClipIndex].hasAudio);
        }

        private void HoverEvent(object sender, EventArgs e)
        {
            Label label = (Label)sender;
            label.Background = new SolidColorBrush(Colors.Orange);
            Mouse.OverrideCursor = Cursors.Hand;
        }
        private void UnHoverEvent(object sender, EventArgs e)
        {
            Label label = (Label)sender;
            label.Background = new SolidColorBrush(Colors.Gray);
            Mouse.OverrideCursor = null;
        }

        // ===== TIMELINE INTEGRATION METHODS =====

        private double _pixelsPerSecond = 50.0;
        private const double TRACK_HEIGHT = 81.5;

        private void UpdateTimelineWidth()
        {
            double duration = Math.Max(_timelineManager.TotalDuration, 60);
            this.window.TimelineGrid.Width = duration * _pixelsPerSecond + 200;
        }

        private void RenderTimeRuler()
        {
            this.window.TimeRulerCanvas.Children.Clear();

            double totalWidth = this.window.TimelineGrid.Width;
            double secondInterval = 1.0;

            if (_pixelsPerSecond < 20) secondInterval = 5.0;
            else if (_pixelsPerSecond < 10) secondInterval = 10.0;

            for (double time = 0; time < totalWidth / _pixelsPerSecond; time += secondInterval)
            {
                double xPos = time * _pixelsPerSecond;

                Line tick = new Line
                {
                    X1 = xPos,
                    X2 = xPos,
                    Y1 = 15,
                    Y2 = 35,
                    Stroke = System.Windows.Media.Brushes.Gray,
                    StrokeThickness = 1
                };
                this.window.TimeRulerCanvas.Children.Add(tick);

                TextBlock label = new TextBlock
                {
                    Text = FormatTime(time),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 10
                };
                Canvas.SetLeft(label, xPos + 2);
                Canvas.SetTop(label, 0);
                this.window.TimeRulerCanvas.Children.Add(label);
            }
        }

        private string FormatTime(double seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }

        private void OnTimelineChanged(object sender, TimelineChangedEventArgs e)
        {
            UpdateTimelineWidth();
            RenderTimeRuler();
            RenderAllClips();
        }

        private void OnClipAddedToTimeline(object sender, ClipEventArgs e)
        {
            UpdateTimelineWidth();
            RenderClip(e.Clip);
        }

        private void RenderClip(TimelineClip clip)
        {
            Canvas targetCanvas = GetCanvasForTrack(clip.TrackNumber, clip.TrackType);
            if (targetCanvas == null) return;

            double left = clip.TimelineStart * _pixelsPerSecond;
            double width = clip.TimelineDuration * _pixelsPerSecond;

            Border clipBorder = new Border
            {
                Width = width,
                Height = TRACK_HEIGHT - 10,
                Background = GetClipColor(clip),
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Tag = clip.Id,
                Cursor = Cursors.Hand
            };

            Grid clipContent = new Grid();

            TextBlock clipLabel = new TextBlock
            {
                Text = clip.ClipName,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(8, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            clipContent.Children.Add(clipLabel);

            TextBlock durationLabel = new TextBlock
            {
                Text = $"{clip.TimelineDuration:F1}s",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 5, 5),
                Opacity = 0.7
            };
            clipContent.Children.Add(durationLabel);

            clipBorder.Child = clipContent;

            Canvas.SetLeft(clipBorder, left);
            Canvas.SetTop(clipBorder, 5);

            clipBorder.MouseRightButtonDown += TimelineClip_RightClick;
            clipBorder.MouseEnter += TimelineClip_MouseEnter;
            clipBorder.MouseLeave += TimelineClip_MouseLeave;

            targetCanvas.Children.Add(clipBorder);
        }

        private void RenderAllClips()
        {
            this.window.V1Canvas.Children.Clear();
            this.window.V2Canvas.Children.Clear();
            this.window.A1Canvas.Children.Clear();
            this.window.A2Canvas.Children.Clear();

            foreach (var track in _timelineManager.Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    RenderClip(clip);
                }
            }
        }

        private Canvas GetCanvasForTrack(int trackNumber, TrackType trackType)
        {
            if (trackType == TrackType.Video || trackType == TrackType.Both)
            {
                return trackNumber == 1 ? this.window.V1Canvas : this.window.V2Canvas;
            }
            else if (trackType == TrackType.Audio)
            {
                return trackNumber == 1 ? this.window.A1Canvas : this.window.A2Canvas;
            }
            return null;
        }

        private System.Windows.Media.Brush GetClipColor(TimelineClip clip)
        {
            if (clip.TrackType == TrackType.Video || clip.TrackType == TrackType.Both)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 139, 50));
            else
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 50, 212));
        }

        private void TimelineClip_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.BorderBrush = System.Windows.Media.Brushes.Yellow;
            border.BorderThickness = new Thickness(3);
        }

        private void TimelineClip_MouseLeave(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.BorderBrush = System.Windows.Media.Brushes.Black;
            border.BorderThickness = new Thickness(2);
        }
        private void TimelineClip_RightClick(object sender, MouseButtonEventArgs e)
        {
            Border clipBorder = sender as Border;
            Guid clipId = (Guid)clipBorder.Tag;

            ContextMenu contextMenu = new ContextMenu();

            MenuItem deleteItem = new MenuItem { Header = "Delete Clip" };
            deleteItem.Click += (s, args) =>
            {
                _timelineManager.RemoveClip(clipId);
            };

            MenuItem propertiesItem = new MenuItem { Header = "Properties" };
            propertiesItem.Click += (s, args) => {
                var clip = _timelineManager.GetClip(clipId);
                MessageBox.Show($"Clip: {clip.ClipName}\n" +
                              $"Timeline: {clip.TimelineStart:F2}s - {clip.TimelineEnd:F2}s\n" +
                              $"Duration: {clip.TimelineDuration:F2}s\n" +
                              $"Source: {clip.SourceStartTime:F2}s - {clip.SourceEndTime:F2}s",
                              "Clip Properties");
            };

            contextMenu.Items.Add(deleteItem);
            contextMenu.Items.Add(propertiesItem);

            clipBorder.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        // NEW: Public method to add clip to timeline
        public void AddClipToTimeline(Clips clipDef, double timelinePosition, int trackNumber, TrackType trackType)
        {
            try
            {
                var timelineClip = _timelineManager.AddClip(
                    clipDef.sourcePath,
                    clipDef.clipName,
                    clipDef.start,
                    clipDef.end,
                    timelinePosition,
                    trackNumber,
                    trackType
                );

                if (_timelineManager.HasOverlap(timelineClip))
                {
                    MessageBox.Show("Warning: This clip overlaps with another clip on the same track!",
                        "Overlap Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding clip: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public TimelineManager GetTimelineManager()
        {
            return _timelineManager;
        }
    }
}
