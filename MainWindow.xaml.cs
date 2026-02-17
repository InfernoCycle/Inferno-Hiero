using InfernoVEditor.InfernoVEditor;
using Mpv.NET.API;
using Mpv.NET.Player;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace InfernoVEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        MpvPlayer Player;
        MediaExtension extension; //class to extend the original media capabilities for source
        CustomTick bottomTicks;
        TimelineManager timeline;
        Clipping clipping;
        //MasterFiles mf;
        Utils util = new Utils();

        MediaExtension clipExtension;

        private double _pixelsPerSecond = 50.0;
        private const double TRACK_HEIGHT = 81.5;

        // For drag-and-drop
        private System.Windows.Point _dragStartPoint;
        private bool _isDragging = false;

        App projectAccess;

        public MainWindow()
        {
            InitializeComponent();

            this.projectAccess = ((App)Application.Current);

            // Relative path to the DLL.
            string dllPath = @"./Lib/mpv-1.dll";

           // Player = new MpvPlayer(PlayerHost.Handle, dllPath);
            //Player.Load("./vid1.mp4");
            //Player.Resume();

            this.MinWidth = 1270;
            this.MinHeight = 810;

            this.Width = 1300;
            this.Height = 820;

            this.Title = "Inferno Hiero";

            //MediaElement m = this.mergerMedia;
            //m.Source = new Uri("./vid1.mp4", UriKind.RelativeOrAbsolute);
            extension = new MediaExtension(this,this.SourceMedia, this.srcSeeker, this.sourceMediaPos);
            extension.PositionChanged(); //Starts Position Change which runs for an unspecified time.
            extension.Volume(this.srcVolumeSlider, this.srcVolumeLevel);//Starts a thread for listening for volume changes.

            ProjectPickerView picker = new ProjectPickerView(this, this.projectAccess, util);
            //mf = new MasterFiles(this);
            MediaController media = new MediaController(this); //this is where the media controls are
            clipping = new Clipping(this, this.projectAccess, media); //used for all clipping functionality. Dependent on 
            FileSelections fs = new FileSelections(this, media, this.projectAccess, picker, clipping);
            picker.setFileSelection(fs);
            picker.setClipping(clipping);

            // After initializing clipping, bind the combo box
            if (clipping != null)
            {
                ClipSelectorCombo.ItemsSource = clipping.savedClips;
            }

            Resizer resize = new Resizer(this);
            //timeline = new VideoTimeLine(this);
            //clipping = new Clipping(this);

            addFileBtn.Click += fs.addFile;
            projectCreateBtn.Click += fs.createProject;

            sourcePlay.Click += media.playSrcMedia;
            sourceStop.Click += media.stopSrcMedia;

            this.SizeChanged += resize.MediaResize; //when window reaches a certain size. increase media display size and then return back to normal when we decrease

            //start a new thread to do the timeline Action while user waits so it doesn't hold up junk
            //timeline.showTimeLineTicks();

            //clip media actions and extended features added
            clipExtension = new MediaExtension(this, this.clipMedia, this.clipSlider, this.rClipPos, false);
            clipExtension.PositionChanged();
            clipExtension.Volume(this.clipVolumeSlider, this.clipVolumeLevel);
            this.clipMedia.Volume = 0.2;
            this.clipInBtn.Click += clipping.clipInOutClicked;
            this.clipOutBtn.Click += clipping.clipInOutClicked;
            this.createClipBtn.Click += clipping.create_clip;
            this.rclipPlay.Click += media.playClipMedia;
            this.rclipStop.Click += media.stopClipMedia;

            this.clipMedia.Source = new Uri("E:\\Documents\\Inferno_yt_to_mp3_v2\\PlusVersion\\dist\\play\\Dancing ダンシング Sakuga MAD.mp4");
            this.clipMedia.MediaOpened += isMediaOpened;
            this.clipMedia.MediaEnded += isMediaEnded;
            this.clipMedia.Pause();

            this.clipping.setExtension(clipExtension);

            this.saveClipBtn.Click += clipping.saveClip;

            //bottomTicks.getCustomTicks();

            /*addFileBtn.Click 
            Label label = new Label();
            label.Content = "A Label";

            Label label2 = new Label();
            label2.Content = "A Label";

            DockPanel dockPanel = new DockPanel();
            dockPanel.Height = 100;
            dockPanel.Children.Add(label);

            vidLists.Children.Add(dockPanel);

            testDock.Children.Add(label2);

            FileDia*/
        }

        async public void isMediaOpened(object sender, RoutedEventArgs e)
        {
            //shows length
            MediaElement el = (MediaElement)sender;

            Regex pattern = new Regex("\\d+(:\\d+)?(:\\d+)?");
            Match match = pattern.Match(this.clipMedia.NaturalDuration.ToString());
            if (match.Success)
            {
                this.rClipLength.Content = this.clipMedia.NaturalDuration.ToString().Split(".")[0];
                Debug.WriteLine(match.Groups[0]);
            }
            this.clipSlider.Maximum = util.TimeToSeconds(this.clipMedia.NaturalDuration.ToString());
            this.clipping.clipOutEnd = this.clipSlider.Maximum;
            this.curClipTo.Content = util.SecondsToTime((int)this.clipSlider.Maximum) + ".000";
        }
        async public void isMediaEnded(object sender, RoutedEventArgs e)
        {
            this.clipMedia.Stop();
            this.rclipPlay.Content = "Play";
        }

        // Helper method to find a child element by name
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // If the child is the type we're looking for and has the correct name
                T childType = child as T;
                if (childType != null)
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = childType;
                        break;
                    }
                }

                // Recursively search for the child
                foundChild = FindChild<T>(child, childName);

                if (foundChild != null) break;
            }

            return foundChild;
        }

        //method to get custom style
        public ControlTemplate getControlTemplateClone(string styleName)
        {
            ControlTemplate Template = null;
            //To access a custom style
            Style customStyle = (Style)this.Resources[styleName];
            // Look for a Setter that targets the Template property
            foreach (Setter setter in customStyle.Setters)
            {
                if (setter.Property == Control.TemplateProperty)
                {
                    Template = setter.Value as ControlTemplate;
                    break;
                }
            }

            //clone template
            ControlTemplate modifyTemplate = new ControlTemplate(Template.TargetType);

            // The tricky part is that you need to copy the template content
            // This requires using XamlWriter and XamlReader to do a deep copy
            string xaml = XamlWriter.Save(Template);
            StringReader stringReader = new StringReader(xaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            ControlTemplate clonedTemplate = (ControlTemplate)XamlReader.Load(xmlReader);

            return clonedTemplate;
        }

        // ===== DRAG AND DROP FROM CLIP LIBRARY =====

        public void ClipLibraryDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        public void ClipLibraryDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                System.Windows.Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                // Check if drag threshold exceeded
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DataGrid dataGrid = sender as DataGrid;
                    if (dataGrid?.SelectedItem is Clips clip)
                    {
                        _isDragging = true;
                        DataObject dragData = new DataObject("Clips", clip);
                        DragDrop.DoDragDrop(dataGrid, dragData, DragDropEffects.Copy);
                        _isDragging = false;
                    }
                }
            }
        }

        public void ClipLibraryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clipping != null)
            {
                clipping.OnClipSelected(sender, e);
            }
        }

        // ===== TIMELINE DRAG AND DROP HANDLERS =====

        public void TrackCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Clips"))
            {
                e.Effects = DragDropEffects.Copy;

                // Highlight the track
                Border border = sender as Border;
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 60));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        public void TrackCanvas_DragLeave(object sender, DragEventArgs e)
        {
            // Remove highlight
            Border border = sender as Border;
            border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
        }

        public void TrackCanvas_Drop(object sender, DragEventArgs e)
        {
            // Remove highlight
            Border border = sender as Border;
            border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));

            if (e.Data.GetDataPresent("Clips"))
            {
                Clips clipDef = e.Data.GetData("Clips") as Clips;
                string trackTag = border.Tag as string;

                if (clipDef == null || string.IsNullOrEmpty(trackTag))
                    return;

                // Get drop position relative to the timeline
                System.Windows.Point dropPoint = e.GetPosition(TimelineGrid);
                double timelineTime = dropPoint.X / _pixelsPerSecond;

                // Snap to nearest 0.5 second
                timelineTime = Math.Round(timelineTime * 2) / 2.0;

                // Parse track info
                int trackNumber;
                TrackType trackType;

                if (trackTag.StartsWith("V"))
                {
                    trackNumber = int.Parse(trackTag.Substring(1));
                    trackType = TrackType.Video;
                }
                else if (trackTag.StartsWith("A"))
                {
                    trackNumber = int.Parse(trackTag.Substring(1));
                    trackType = TrackType.Audio;
                }
                else
                {
                    MessageBox.Show("Invalid track!");
                    return;
                }

                // Add clip to timeline via clipping manager
                if (clipping != null)
                {
                    clipping.AddClipToTimeline(clipDef, timelineTime, trackNumber, trackType);
                }
            }
        }

        // ===== CLIP EDIT BUTTON =====

        private void EditClip_Click(object sender, RoutedEventArgs e)
        {
            if (clipping == null)
            {
                Debug.WriteLine("ClippingManager is null!");
                return;
            }

            Button btn = sender as Button;
            if (btn?.Tag == null)
            {
                Debug.WriteLine("Button tag is null!");
                return;
            }

            Guid clipId = (Guid)btn.Tag;
            Debug.WriteLine($"Edit button clicked for clip ID: {clipId}");

            // Find the clip and select it in the DataGrid
            var clip = clipping.savedClips.FirstOrDefault(c => c.Id == clipId);
            Debug.WriteLine($"Clip is: {clip}");
            if (clip != null)
            {
                ClipLibraryDataGrid.SelectedItem = clip;
                ClipLibraryDataGrid.ScrollIntoView(clip);
            }
            else
            {
                Debug.WriteLine("Clip not found in savedClips!");
            }
        }

        // ===== ZOOM CONTROLS =====

        public void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _pixelsPerSecond *= 1.5;
            if (_pixelsPerSecond > 200)
                _pixelsPerSecond = 200; // Max zoom

            if (clipping != null)
            {
                var timelineManager = clipping.GetTimelineManager();
                if (timelineManager != null)
                {
                    // Trigger re-render by firing timeline changed
                    // This will be handled by the clipping manager's event handlers
                }
            }
        }

        public void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _pixelsPerSecond /= 1.5;
            if (_pixelsPerSecond < 5)
                _pixelsPerSecond = 5; // Min zoom

            if (clipping != null)
            {
                var timelineManager = clipping.GetTimelineManager();
                if (timelineManager != null)
                {
                    // Trigger re-render
                }
            }
        }

        // ===== YOUR EXISTING EVENT HANDLERS =====
        // Keep all your existing button click handlers, media controls, etc.

        // Example: If you have these handlers, keep them
        /*
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Your existing play logic
        }
        
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Your existing stop logic
        }
        
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Your existing file open logic
        }
        */

        // ===== WINDOW CLOSING =====

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Clean up resources if needed
            base.OnClosing(e);
        }
    }
}