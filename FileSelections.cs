using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Unosquare.FFME.Common;

namespace InfernoVEditor
{
    class FileSelections
    {
        MainWindow window;
        int index = 1;
        int currentSelected = -1;
        List<string> files = new List<string>(); //holds the file paths
        List<Label> collection = new List<Label>(); //holds the labels
        List<Button> btnCollection = new List<Button>(); //holds the delete buttons
        List<bool> included = new List<bool>();//gives state on which files should be included

        List<int> asciiValues = new List<int>();

        private MediaController media;
        private Utils utils = new Utils();
        private Clipping masterFiles;

        private Projects projectAccess;
        private ProjectController projectController;
        private App application;
        private FileStream prjFile;
        private int projectIndex = 0;
        private ProjectPickerView picker;
        private string currentProject;

        RadioButton r1;
        System.Windows.Controls.RadioButton r2;

        public FileSelections(MainWindow window, MediaController media, App projectAccess, ProjectPickerView picker, Clipping masterFiles)
        {
            this.window = window;
            this.media = media;
            this.media.setUtil(utils);
            this.masterFiles = masterFiles;

            this.application = projectAccess;
            this.projectController = this.application.getProjectSingleton();
            
            this.projectAccess = this.projectController.getProject();
            this.prjFile = this.application.getProjectFileStream();
            this.picker = picker;

            Debug.WriteLine("Width: " + this.window.vidLists.Width.ToString());
            Debug.WriteLine("Max Width: " + this.window.vidLists.MaxWidth.ToString());
            Debug.WriteLine("Actual Width: " + this.window.vidLists.ActualWidth.ToString());

            asciiValues.Add(45); //insert dash -
            asciiValues.Add(46); //insert period .
            asciiValues.Add(95); //insert underscore _
            for (int i = 45; i < 127; i++) { 
                if(i >= 48 && i <= 57) //insert number ascii values
                {
                    asciiValues.Add(i);
                }
                if(i >= 65 && i <= 90) //insert lowercase ascii values
                {
                    asciiValues.Add(i);
                }
                if(i>=97 && i <= 122) { asciiValues.Add(i);} //insert uppercase ascii values
                if(i == 126) { asciiValues.Add(i); } //insert tilde ~
            }

            r1 = this.window.includeRadio1;
            r2 = this.window.excludeRadio1;

            r1.Click += this.addFileToChooser;
            r2.Click += this.removeFileFromChooser;
        }

        async public Task<string> find_values(string data, string fileName, string originalPath, string hash)
        {
            //check if file is alredy in your project (no need for duplicates)
            for(int i = 0; i < this.projectAccess.UserProjects.Count; i++)
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    for(int k = 0; k < this.projectAccess.UserProjects[i].homeFiles.Count; k++)
                    {
                        if (this.projectAccess.UserProjects[i].homeFiles[k].hash == hash.Replace("-", ""))
                        {
                            Debug.WriteLine("Similar File found exiting");
                            return null;
                        }
                    }
                }
            }

            bool durationFound = false;
            bool frameRateFound = false;
            bool audioBitRateFound = false;
            bool videoBitRateFound = false;

            string codec = "";
            string codec_type = "";
            string videoCodec = "";
            string audioCodec = "";
            string sampleRate = "";
            int channels = 1;
            string audioBitRate = "";
            string videoBitRate = "";
            string width = "";
            string height = "";
            string fps = "";
            double duration = 0;

            bool reading = false;
            bool readingValue = false;

            string temp = "";
            //Debug.WriteLine(data);
            for(int i = 0; i < data.Length; i++)
            {
                if (!reading)
                {
                    temp += data[i];
                }

                if (data[i] == '\n')
                {
                    string[] keyValue = temp.Split('=');

                    if (keyValue[0] == "codec_name")
                    {
                        codec = keyValue[1];
                    }
                    if (keyValue[0] == "codec_type")
                    {
                        codec_type = keyValue[1].Trim();
                        if (keyValue[1].Replace("\n", "").Trim() == "video"){ videoCodec = codec.Trim(); }else if(keyValue[1].Replace("\n", "").Trim() == "audio") { audioCodec = codec.Trim(); }
                    }
                    if (codec_type == "video" && !videoBitRateFound) { if (keyValue[0] == "bit_rate") { videoBitRate = keyValue[1].Trim(); videoBitRateFound = true; } }
                    if (codec_type == "audio" && !audioBitRateFound) { if (keyValue[0] == "bit_rate") { audioBitRate = keyValue[1].Trim(); audioBitRateFound = true; } }

                    if (keyValue[0] == "sample_rate") { sampleRate = keyValue[1].Trim(); }
                    if (keyValue[0] == "channels") { channels = int.Parse(keyValue[1].Replace("\n", "").Trim()); }
                    if (keyValue[0] == "width") { width = keyValue[1].Trim(); }
                    if (keyValue[0] == "height") { height = keyValue[1].Trim(); }
                    if (keyValue[0] == "avg_frame_rate" && !frameRateFound && codec_type == "video") { fps = keyValue[1].Trim(); frameRateFound = true; }
                    if (keyValue[0] == "duration" && !durationFound) { duration = double.Parse(keyValue[1].Replace("\n", "").Trim()); durationFound = true; }

                    reading = false;

                    temp = "";
                    continue;
                }
            }

            string[] fpsSplit = fps.Split("/");
            float trueFPS = 0f;
            if(fpsSplit.Length == 2) { 
                trueFPS = float.Parse(fpsSplit[0])/float.Parse(fpsSplit[1]);
            }else if(fpsSplit.Length == 1)
            {
                if (fpsSplit[0] != "")
                {
                    trueFPS = float.Parse(fpsSplit[0]);
                }
            }

            /*Debug.WriteLine("Audio Codec: " + audioCodec);
            Debug.WriteLine("Video Codec: " + videoCodec);

            Debug.WriteLine("Audio BitRate: " + audioBitRate);
            Debug.WriteLine("Video BitRate: " + videoBitRate);

            Debug.WriteLine("Height: " + height);
            Debug.WriteLine("Width: " + width);

            Debug.WriteLine("avg frame rate: " + trueFPS);

            Debug.WriteLine("Channels: " + channels);

            Debug.WriteLine("duration: " + duration);*/

            string newFilePath = await createFileCopy(originalPath, fileName); //create file copy in designated directory and returns the new file copy path

            //create new HashFile object and add to the current project and project file
            HomeFiles homeFiles = new HomeFiles();
            homeFiles.fps = (int)trueFPS;
            homeFiles.vbitrate = videoBitRate;
            homeFiles.vcodec = videoCodec;
            homeFiles.abitrate = audioBitRate;
            homeFiles.resolution = width + "x" + height;
            homeFiles.acodec = audioCodec;
            homeFiles.include = false;
            homeFiles.fileName = fileName;
            homeFiles.originalFilePath = originalPath;
            homeFiles.projectFilePath = newFilePath;
            homeFiles.playbackSpeed = 1;
            homeFiles.hash = hash.Replace("-","");
            homeFiles.channels = channels;
            homeFiles.duration = duration;
            homeFiles.sample_rate = sampleRate;

            if(videoCodec != "")
            {
                homeFiles.hasVideo = true;
            }
            if(audioCodec != "")
            {
                homeFiles.hasAudio = true;
            }

            files.Add(newFilePath);
            //this.masterFiles.ChoosableFiles.Add(newFilePath);

            for (int a = 0; a < this.projectAccess.UserProjects.Count; a++)
            {
                if (this.projectAccess.UserProjects[a].ProjectName == this.currentProject)
                {
                    this.projectAccess.UserProjects[a].homeFiles.Add(homeFiles);
                    break;
                }
            }

            Debug.WriteLine("New JSON Data: " + JsonSerializer.Serialize(this.projectAccess));

            byte [] json_data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this.projectAccess));

            this.prjFile.Position = 0;
            this.prjFile.SetLength(0);
            this.prjFile.Write(json_data);
            this.prjFile.Flush();

            return "";
        }

        async public void addFile(object sender, EventArgs e)
        {
            Debug.WriteLine("Project: " + JsonSerializer.Serialize(this.projectController.getProject().UserProjects[this.projectIndex]));
            Border border = new Border()
            {
                BorderThickness = new Thickness()
                {
                    Bottom = 2,
                    Left = 0,
                    Right = 0,
                    Top = 0
                },
                BorderBrush = new SolidColorBrush(Colors.Black)
            };

            OpenFileDialog ofd = new OpenFileDialog();

            //Set File Dialog Options
            ofd.Filter = "Media Files (*.mp4;*.mp3;*.wav;*.m4a;*.ogg;*.flv;*.flv;*.mov;*.mkv;*.avi)|*.mp4;*.mp3;*.wav;*.m4a;*.ogg;*.flv;*.flv;*.mov;*.mkv;*.avi|All files (*.*)|*.*";
            ofd.FilterIndex = 1;
            if (ofd.ShowDialog() == true)
            {
                //createFileCopy(ofd.FileName);

                ProjectController controller = ProjectController.getModel();
                Debug.WriteLine("Move the junk to prj: " + controller.getProject().UserProjects.Count);

                //Get the path of specified file
                string filePath = ofd.SafeFileName;

                //Read the contents of the file into a stream
                var fileStream = ofd.OpenFile();

                //get file Extension (will be used everywhere)
                Regex regex = new Regex("(\\.mp4|\\.mp3|\\.wav|\\.m4a|\\.ogg|\\.flv|\\.flv|\\.mov|\\.mkv|\\.avi)");
                Match match1 = regex.Match(filePath);

                Debug.WriteLine(filePath);

                //create file stuff
                Grid grid = new Grid();
                grid.ShowGridLines = false;

                // Define the Columns
                ColumnDefinition colDef1 = new ColumnDefinition();
                ColumnDefinition colDef2 = new ColumnDefinition();
                ColumnDefinition colDef3 = new ColumnDefinition();
                colDef3.Width = new GridLength(20);
                grid.ColumnDefinitions.Add(colDef1);
                grid.ColumnDefinitions.Add(colDef2);
                grid.ColumnDefinitions.Add(colDef3);

                // Define the Rows
                RowDefinition rowDef1 = new RowDefinition();
                RowDefinition rowDef2 = new RowDefinition();
                RowDefinition rowDef3 = new RowDefinition();
                RowDefinition rowDef4 = new RowDefinition();
                RowDefinition rowDef5 = new RowDefinition();
                grid.RowDefinitions.Add(rowDef1);
                grid.RowDefinitions.Add(rowDef2);
                grid.RowDefinitions.Add(rowDef3);
                grid.RowDefinitions.Add(rowDef4);

                //shows index number
                Label label = new Label();
                TextBlock textBlock1 = new TextBlock();
                textBlock1.Text = "Index# " + this.index;
                textBlock1.TextWrapping = System.Windows.TextWrapping.Wrap;
                label.Content = textBlock1;
                label.Margin = new Thickness(1, 0, 0, 0);
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, 0);
                Grid.SetColumnSpan(label, 2);

                //add index label to lists
                collection.Add(label);

                //Remove Button
                Button button1 = new Button();
                button1.Content = "X";
                button1.Name = "B"+(this.index-1).ToString();
                button1.Click += removeFile;
                Grid.SetRow(button1 , 0);
                Grid.SetColumn(button1 , 2);

                //add remove buttons to collection
                btnCollection.Add(button1);

                async Task<string> func()
                {
                    string[] res = await utils.getInfoAsync(ofd.FileName);
                    string values = await find_values(res[0], ofd.SafeFileName, ofd.FileName, generate_hash(ofd.FileName));
                    Debug.WriteLine("Values: " + values);
                    if (values == null)
                    {
                        collection.RemoveAt(collection.Count - 1);
                        btnCollection.RemoveAt(btnCollection.Count - 1);
                        files.RemoveAt(files.Count - 1);
                        return null;
                    }

                    //string[] audioRes = await utils.getInfoAsync(ofd.FileName, false);
                    //Debug.WriteLine("Res 1: " + res[1]);
                    //Debug.WriteLine("Res 2: " + res[0]);
                    //find_values(res[1]);
                    if (res[1] != "")
                    {
                        Regex pattern = new Regex("Duration: \\d+(:\\d+)?(:\\d+)?");
                        Regex fps = new Regex("\\d+(\\.\\d+)? fps");
                        Regex resolution = new Regex(", \\d+x\\d+");
                        Match match = pattern.Match(res[1]);
                        Match match2 = fps.Match(res[1]);
                        Match match3 = resolution.Match(res[1]);

                        if (match.Success)
                        {
                            string[] split = match.Groups[0].Value.Split(" ");
                            /*length1.Text = split[1];
                            secondFind.Enabled = true;
                            this.outputFileResolution = match3.Value.Replace(", ", "");

                            Debug.WriteLine("Output FPS: " + this.outputFileFps + ", Output Res: " + this.outputFileResolution);*/

                            //fps
                            /*Label FPS = new Label();
                            TextBlock textBlockFPS = new TextBlock();
                            textBlockFPS.Text = "FPS: " + match2.Value.Replace(" fps", "");
                            textBlockFPS.TextWrapping = System.Windows.TextWrapping.Wrap;
                            FPS.Content = textBlockFPS;
                            FPS.Margin = new Thickness(1, 0, 0, 0);
                            Grid.SetRow(FPS, 1);
                            Grid.SetColumn(FPS, 0);
                            Grid.SetColumnSpan(FPS, 1);*/

                            //length
                            grid.Dispatcher.Invoke(() =>
                            {
                                Label length = new Label();
                                TextBlock textBlockLength = new TextBlock();
                                textBlockLength.Text = "Length: " + split[1];
                                textBlockLength.TextWrapping = System.Windows.TextWrapping.Wrap;
                                length.Content = textBlockLength;
                                length.Margin = new Thickness(1, 0, 0, 0);
                                Grid.SetRow(length, 1);
                                Grid.SetColumn(length, 0);
                                Grid.SetColumnSpan(length, 3);


                                //grid.Children.Add(FPS);
                                grid.Children.Add(length);
                            });
                        }
                    }
                    else
                    {
                        //length1.Text = "None";
                    }
                    return "";
                }

                var task = await Task.Run(async() => await func());
                Debug.WriteLine("Task: " + task);
                if (task == null) {
                    return;
                }

                //shows filename
                Label label2 = new Label();
                TextBlock textBlock2 = new TextBlock();
                textBlock2.Text = "File Name: " + filePath;
                textBlock2.TextWrapping = System.Windows.TextWrapping.WrapWithOverflow;
                label2.Content = textBlock2;
                label2.Margin = new Thickness(1, 0, 0, 0);
                Grid.SetRow(label2, 2);
                Grid.SetColumn(label2, 0);
                Grid.SetColumnSpan(label2, 3);
                Grid.SetRowSpan(label2, 2);

                DockPanel dockPanel = new DockPanel();
                dockPanel.Height = 100;

                // Add the TextBlock elements to the Grid Children collection
                grid.Children.Add(label);
                grid.Children.Add(label2);
                grid.Children.Add(button1);

                dockPanel.Children.Add(grid);
                dockPanel.Background = new SolidColorBrush(Colors.DarkGray);

                dockPanel.MouseDown += clickEvent;

                border.Child = dockPanel;

                this.window.vidLists.Children.Add(border);
                this.index += 1;

                included.Add(false);

                /*MainFile.Enabled = true;
                MainFile.Text = ofd.FileName;
                MainFile.Enabled = false;

                length1.Text = "Loading..."; //setting loading

                //Note: have to wait for Task.Run to finish before we can continue on here but it allows us to run other functions other than runBtn_Click and not freeze the program
                /*await Task.Run(async () =>
                { //waiting for this task to run its course. meanwhile everything else can be ran in different functions
                    string[] res = await getInfoAsync();
                    Invoke(new Action(() =>
                    {//this is needed when inside Task.Run as that counts as a seperate thread
                        //length.Text = res;
                        if (res[1] != "")
                        {
                            Regex pattern = new Regex("Duration: \\d+(:\\d+)?(:\\d+)?");
                            Match match = pattern.Match(res[1]);
                            if (match.Success)
                            {
                                string[] split = match.Groups[0].Value.Split(" ");
                                length.Text = split[1];
                            }
                        }
                        else
                        {
                            length.Text = "None";
                        }
                    }));
                });*/
                /*string[] res = await getInfoAsync(MainFile);
                if (res[1] != "")
                {
                    Regex pattern = new Regex("Duration: \\d+(:\\d+)?(:\\d+)?");
                    Regex fps = new Regex("\\d+(\\.\\d+)? fps");
                    Regex resolution = new Regex(", \\d+x\\d+");
                    Match match = pattern.Match(res[1]);
                    Match match2 = fps.Match(res[1]);
                    Match match3 = resolution.Match(res[1]);

                    if (match.Success)
                    {
                        string[] split = match.Groups[0].Value.Split(" ");
                        length1.Text = split[1];
                        secondFind.Enabled = true;
                        this.outputFileFps = match2.Value.Replace(" fps", "");
                        this.outputFileResolution = match3.Value.Replace(", ", "");

                        Debug.WriteLine("Output FPS: " + this.outputFileFps + ", Output Res: " + this.outputFileResolution);
                    }
                }
                else
                {
                    length1.Text = "None";
                }*/
            }
        }

        private void removeFile(object sender, RoutedEventArgs e)
        {
            this.index -= 1;
            Button b = (Button)sender;

            int remove_idx = int.Parse(b.Name.Replace("B", ""));

            string fileRemovePath = files[remove_idx];

            /*this.masterFiles.ChoosableFiles.Remove(files[remove_idx]);
            masterFiles.buildMasterFileStackElement();

            files.RemoveAt(remove_idx);
            collection.RemoveAt(remove_idx);
            btnCollection.RemoveAt(remove_idx);
            included.RemoveAt(remove_idx);*/

            bool breakLoop = false;
            for (int i = 0; i < this.projectAccess.UserProjects.Count; i++) //loop for removing the file entry from the project json file
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    if (this.projectAccess.UserProjects[i].clips.Count > 0)
                    {
                        for(int j = 0; j < this.projectAccess.UserProjects[i].clips.Count; j++)
                        {
                            if (this.projectAccess.UserProjects[i].clips[j].sourcePath == fileRemovePath)
                            {
                                MessageBox.Show("Can't remove file as it is currently being used in another area.");
                                breakLoop = true;
                                this.index += 1;
                                return;
                            }
                        }
                    }
                    if (breakLoop)
                    {
                        break;
                    }
                    //remove file from project directory
                    File.Delete(this.projectAccess.UserProjects[i].homeFiles[remove_idx].projectFilePath);

                    this.projectAccess.UserProjects[i].homeFiles.RemoveAt(remove_idx);

                    //this.projectAccess.UserProjects[i].homeFiles.RemoveAt(remove_idx);
                    break;
                }
            }

            this.masterFiles.ChoosableFiles.Remove(files[remove_idx]);
            masterFiles.buildMasterFileStackElement();

            files.RemoveAt(remove_idx);
            collection.RemoveAt(remove_idx);
            btnCollection.RemoveAt(remove_idx);
            included.RemoveAt(remove_idx);

            //Debug.WriteLine("Json Data: " + JsonSerializer.Serialize(this.projectAccess));

            //rewrite opened file
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this.projectAccess));
            this.prjFile.Position = 0;
            this.prjFile.SetLength(0);
            
            this.prjFile.Write(bytes);
            this.prjFile.Flush();

            //remove boxes
            this.window.vidLists.Children.Remove(this.window.vidLists.Children[remove_idx+1]);

            for (int i = 1; i < this.window.vidLists.Children.Count; i++) {
                btnCollection[i-1].Name = "B" + (i-1).ToString(); //rename the button so the remove indexing stays correct above
                TextBlock textBlock = new TextBlock();
                textBlock.Text = "Index# " + i;
                collection[i-1].Content = textBlock; //re-word the indexes so they all match again
                //this.window.vidLists.Children.Remove(this.window.vidLists.Children)
            }
        }

        async private void clickEvent(object sender, RoutedEventArgs e) //event for when you select one of the files you added
        {
            DockPanel dockPanel = sender as DockPanel;
            //list of all vidlists
            for(int i = 1; i < this.window.vidLists.Children.Count; i++)
            {
                ((DockPanel)((Border)this.window.vidLists.Children[i]).Child).Background = new SolidColorBrush(Colors.DarkGray);
            }
            dockPanel.Background = new SolidColorBrush(Colors.Orange);
            try
            {
                Grid grid = dockPanel.Children[0] as Grid;
                Label fileNaame = grid.Children[2] as Label; // gets fileName Label
                Label label = grid.Children[1] as Label; //get label that's either index
                Label length = grid.Children[0] as Label; //gets length label

                TextBlock block = label.Content as TextBlock;
                TextBlock lengthBlock = length.Content as TextBlock;
                TextBlock fileBlock = fileNaame.Content as TextBlock;

                //return;
                int clicked_vid = int.Parse(block.Text.Split(" ")[1])-1; //index 1 is used
                string lengthFormat = lengthBlock.Text.Split("Length: ")[1];
                string fileName = fileBlock.Text.Split("File Name: ")[1];

                Debug.WriteLine("Clicked Index: " + clicked_vid);
                Debug.WriteLine("Clicked FileName: " + fileName);
                currentSelected = clicked_vid;

                this.addValuesToFields(fileName);
                this.getCurrentCheckState(this.r1, this.r2); // set check status for merge trim include
                await AddToSource(clicked_vid, lengthFormat);
            }
            catch(NullReferenceException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private void addValuesToFields(string fileName)
        {
            this.window.fpsField.Text = "";
            this.window.vcodecField.Text = "";
            this.window.videobitrateField.Text = "";
            this.window.resolutionField.Text = "";

            this.window.audiocodecField.Text ="";
            this.window.audiobitrateField.Text = "";
            this.window.samplerateField.Text = "";
            this.window.playbackspeedField.Text = "";
            this.window.channelField.Text = "";

            for (int i = 0; i < this.projectAccess.UserProjects.Count; i++) //loop for removing the file entry from the project json file
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    for (int j = 0; j < this.projectAccess.UserProjects[i].homeFiles.Count; j++) {
                        if (this.projectAccess.UserProjects[i].homeFiles[j].fileName == fileName)
                        {
                            HomeFiles files = this.projectAccess.UserProjects[i].homeFiles[j];
                            this.window.fpsField.Text = files.fps.ToString();
                            this.window.vcodecField.Text = files.vcodec.ToString();
                            this.window.videobitrateField.Text = files.vbitrate.ToString();
                            this.window.resolutionField.Text = files.resolution.ToString();

                            this.window.audiocodecField.Text = files.acodec.ToString();
                            this.window.audiobitrateField.Text = files.abitrate.ToString();
                            this.window.samplerateField.Text = files.sample_rate.ToString();
                            this.window.playbackspeedField.Text = files.playbackSpeed.ToString();
                            this.window.channelField.Text = files.channels.ToString();
                            return;
                        }
                    }
                }
            }
        }
        private string getValidEncoding(char value)
        {
            var utf8 = Encoding.UTF8;

            byte[] bytes = utf8.GetBytes(value.ToString());
            //Debug.WriteLine("Invalid: " + value.ToString());

            string percentEncodes = "";

            for(int i = 0; i < bytes.Length; i++)
            {
                int intValue = bytes[i];

                percentEncodes += "%" + intValue.ToString("X");
            }

            return percentEncodes;
        }
        private string fixURI(string uri, bool isDir=true)
        {
            string newUri = "";//"file:///";
            uri = uri.Replace("\\", "/");
            for (int i = 0; i < uri.Length; i++)
            {
                if (asciiValues.Contains((int)uri[i]))
                {
                    newUri += uri[i];
                }
                else
                {
                    if (isDir)
                    {
                        if (uri[i] == '/')
                        {
                            newUri += "/";
                            continue;
                        }
                        if (uri[i] == ':')
                        {
                            newUri += ":";
                            continue;
                        }
                    }
                    newUri += getValidEncoding(uri[i]);
                    //Debug.WriteLine("Percent Encode is: " + getValidEncoding(uri[i]) + ", For Value: " + uri[i]);
                }
                /*Debug.WriteLine((int)uri[i]);
                if (uri[i] == ' ')
                {
                    newUri += "%20";
                }
                else
                {
                    if(i == 0)
                    {
                        newUri += newUri[i].ToString().ToLower();
                    }
                    else
                    {
                        newUri += uri[i];
                    }
                }*/
            }
            return newUri;
        }
        private static string CreateAbsolutePathTo(string mediaFile)
        {
            return Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, mediaFile);
        }

        async private Task <bool> AddToSource(int index, string length)
        {
            Debug.WriteLine("Running the URI for: " + $"{@files[index]}");

            MediaElement src = this.window.SourceMedia;
            
           await Task.Run(() =>
            {

                this.window.Dispatcher.BeginInvoke(async() =>
                {
                    if (src.IsLoaded)
                    {
                        Debug.WriteLine("Closing and Stopping Media");
                        //await src.Stop();
                        //src.Close();
                    }

                    src.Source = new Uri(files[index], UriKind.RelativeOrAbsolute);
                    src.Pause();

                    Regex reg1 = new Regex("\\d+:\\d+:\\d+");
                    Match match1 = reg1.Match(src.NaturalDuration.ToString());
                    //Debug.WriteLine("Duration: " + src.Video);
                    Debug.WriteLine("Is Loaded: " + src.IsLoaded);
                    this.window.sourceMediaLength.Content = length;
                    this.window.srcSeeker.Maximum = utils.TimeToSeconds(length);
                    this.window.SourceMedia.Volume = this.window.srcVolumeSlider.Value;
                });

                //await this.window.SourceMedia.Play();
                /*Uri uri = new Uri(files[index]);
                Debug.WriteLine("FFM Dir: " + Unosquare.FFME.Library.FFmpegDirectory);
                Debug.WriteLine("Worked: " + cleared);
                Debug.WriteLine("Is Open: " + src.IsOpen);
                Debug.WriteLine("Source: " + src.Source);
                Debug.WriteLine("Opening: " + src.IsOpening);
                Debug.WriteLine("Natural Duration: " + src.NaturalDuration);
                Regex reg1 = new Regex("\\d+:\\d+:\\d+");
                Match match1 = reg1.Match(src.NaturalDuration.ToString());
                this.window.sourceMediaLength.Content = match1.Groups[0].Value;
                Debug.WriteLine("Length in Seconds: " + utils.TimeToSeconds(match1.Groups[0].Value));
                this.window.srcSeeker.Maximum = utils.TimeToSeconds(match1.Groups[0].Value);

                src.PositionChanged += this.media.setSeek;

                Debug.WriteLine("Absolute Path: " + CreateAbsolutePathTo(files[index]));
                //Debug.WriteLine(Uri.IsWellFormedUriString(@"file:///c:/Users/Carl%20R/Documents/infernoOutput_2-16-2025_42742_temp.mp4", UriKind.Absolute));

                Debug.WriteLine("Current Uri: " + fixURI(files[index]));
                Uri validatedUri;
                Debug.WriteLine("Valid Uri: " + Uri.TryCreate(fixURI(files[index]), UriKind.Absolute, out validatedUri));*/
            });

            try
            {

                await this.window.sourcePlay.Dispatcher.BeginInvoke(() =>
                {
                    this.window.sourcePlay.Content = "Play";
                });
                if (!src.IsLoaded)
                {
                    await this.window.sourcePlay.Dispatcher.BeginInvoke(() =>
                    {
                        this.window.sourcePlay.Content = "Fail";
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Debug.WriteLine(ex.ToString());
            }

            return true;
        }

        async void getCurrentCheckState(RadioButton a, RadioButton b)
        {
            for (int i = 0; i < included.Count; i++) {
                if (currentSelected == i) {
                    if (included[i]) {
                        a.IsChecked = true;
                        b.IsChecked = false;
                    }
                    else
                    {
                        b.IsChecked = true;
                        a.IsChecked = false;
                    }
                }
            }
        }

        async Task<HomeFiles> getHomeFileList()
        {
            for (int i = 0; i < this.projectAccess.UserProjects.Count; i++)
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    for (int k = 0; k < this.projectAccess.UserProjects[i].homeFiles.Count; k++)
                    {
                        if (this.projectAccess.UserProjects[i].homeFiles[k].projectFilePath == this.files[currentSelected])
                        {
                            return this.projectAccess.UserProjects[i].homeFiles[k];
                        }
                    }
                }
            }
            return null;
        }

        async Task<Projects> getCurrentProjectList()
        {
            for (int i = 0; i < this.projectAccess.UserProjects.Count; i++)
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    return this.projectAccess.UserProjects[i];
                }
            }
            return null;
        }

        async void WriteToProject()
        {
            this.prjFile.Position = 0;
            this.prjFile.SetLength(0);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this.projectAccess));
            this.prjFile.Write(bytes);
            this.prjFile.Flush();
        }

        async void addFileToChooser(object sender, EventArgs e)
        {
            if(currentSelected != -1)
            {
                RadioButton rbtn = (RadioButton)sender;

                if (included.Count > 0)
                {
                    included[currentSelected] = true;

                    for (short i = 0; i < included.Count; i++)
                    {
                        Debug.Write(included[i].ToString() + ", ");
                    }
                    Debug.WriteLine("");

                    HomeFiles temp = await getHomeFileList();
                    temp.include = true;


                    if (!this.masterFiles.ChoosableFiles.Contains(temp.projectFilePath))
                    {
                        this.masterFiles.ChoosableFiles.Add(temp.projectFilePath);
                        masterFiles.buildMasterFileStackElement();
                    }

                    WriteToProject();
                }
            }
        }

        async void removeFileFromChooser(object sender, EventArgs e)
        {
            if (currentSelected != -1)
            {
                RadioButton rbtn = (RadioButton)sender;

                if (included.Count > 0)
                {
                    included[currentSelected] = false;

                    for (short i = 0; i < included.Count; i++)
                    {
                        Debug.Write(included[i].ToString() + ", ");
                    }
                    Debug.WriteLine("");
                }

                HomeFiles temp = await getHomeFileList();
                temp.include = false;

                this.masterFiles.ChoosableFiles.RemoveAt(currentSelected);
                masterFiles.buildMasterFileStackElement();

                WriteToProject();
            }
        }

        string generate_hash(string FilePath)
        {
            byte [] byteStuff = SHA256.HashData(File.ReadAllBytes(FilePath));
            return BitConverter.ToString(byteStuff, 0);
        }

        public void setGlobalValues(string prjName, int prjIndex)
        {
            this.projectIndex = prjIndex;
            this.currentProject = prjName;
        }

        public void loadFiles()
        {
            //sage is about to witness MASTERY IN THE CODETH
            for (int i = 0; i < this.projectAccess.UserProjects.Count; i++)
            {
                if (this.projectAccess.UserProjects[i].ProjectName == this.currentProject)
                {
                    for (int k = 0; k < this.projectAccess.UserProjects[i].homeFiles.Count; k++)
                    {
                        Border border = new Border()
                        {
                            BorderThickness = new Thickness()
                            {
                                Bottom = 2,
                                Left = 0,
                                Right = 0,
                                Top = 0
                            },
                            BorderBrush = new SolidColorBrush(Colors.Black)
                        };

                        files.Add(this.projectAccess.UserProjects[i].homeFiles[k].projectFilePath);

                        //create the columns and what not
                        //create file stuff
                        Grid grid = new Grid();
                        grid.ShowGridLines = false;

                        // Define the Columns
                        ColumnDefinition colDef1 = new ColumnDefinition();
                        ColumnDefinition colDef2 = new ColumnDefinition();
                        ColumnDefinition colDef3 = new ColumnDefinition();
                        colDef3.Width = new GridLength(20);
                        grid.ColumnDefinitions.Add(colDef1);
                        grid.ColumnDefinitions.Add(colDef2);
                        grid.ColumnDefinitions.Add(colDef3);

                        // Define the Rows
                        RowDefinition rowDef1 = new RowDefinition();
                        RowDefinition rowDef2 = new RowDefinition();
                        RowDefinition rowDef3 = new RowDefinition();
                        RowDefinition rowDef4 = new RowDefinition();
                        RowDefinition rowDef5 = new RowDefinition();
                        grid.RowDefinitions.Add(rowDef1);
                        grid.RowDefinitions.Add(rowDef2);
                        grid.RowDefinitions.Add(rowDef3);
                        grid.RowDefinitions.Add(rowDef4);

                        //shows index number
                        Label label = new Label();
                        TextBlock textBlock1 = new TextBlock();
                        textBlock1.Text = "Index# " + this.index;
                        textBlock1.TextWrapping = System.Windows.TextWrapping.Wrap;
                        label.Content = textBlock1;
                        label.Margin = new Thickness(1, 0, 0, 0);
                        Grid.SetRow(label, 0);
                        Grid.SetColumn(label, 0);
                        Grid.SetColumnSpan(label, 2);

                        //add index label to lists
                        collection.Add(label);

                        //Remove Button
                        Button button1 = new Button();
                        button1.Content = "X";
                        button1.Name = "B" + (this.index - 1).ToString();
                        button1.Click += removeFile;
                        Grid.SetRow(button1, 0);
                        Grid.SetColumn(button1, 2);

                        //add remove buttons to collection
                        btnCollection.Add(button1);

                        //create length label and text
                        Label length = new Label();
                        TextBlock textBlockLength = new TextBlock();
                        textBlockLength.Text = "Length: " + this.utils.SecondsToTime(int.Parse(this.projectAccess.UserProjects[i].homeFiles[k].duration.ToString().Split(".")[0]));
                        textBlockLength.TextWrapping = System.Windows.TextWrapping.Wrap;
                        length.Content = textBlockLength;
                        length.Margin = new Thickness(1, 0, 0, 0);
                        Grid.SetRow(length, 1);
                        Grid.SetColumn(length, 0);
                        Grid.SetColumnSpan(length, 3);


                        //grid.Children.Add(FPS);
                        grid.Children.Add(length);

                        //shows filename
                        Label label2 = new Label();
                        TextBlock textBlock2 = new TextBlock();
                        textBlock2.Text = "File Name: " + this.projectAccess.UserProjects[i].homeFiles[k].fileName;
                        textBlock2.TextWrapping = System.Windows.TextWrapping.WrapWithOverflow;
                        label2.Content = textBlock2;
                        label2.Margin = new Thickness(1, 0, 0, 0);
                        Grid.SetRow(label2, 2);
                        Grid.SetColumn(label2, 0);
                        Grid.SetColumnSpan(label2, 3);
                        Grid.SetRowSpan(label2, 2);

                        DockPanel dockPanel = new DockPanel();
                        dockPanel.Height = 100;

                        // Add the TextBlock elements to the Grid Children collection
                        grid.Children.Add(label);
                        grid.Children.Add(label2);
                        grid.Children.Add(button1);

                        dockPanel.Children.Add(grid);
                        dockPanel.Background = new SolidColorBrush(Colors.DarkGray);

                        dockPanel.MouseDown += clickEvent;

                        border.Child = dockPanel;

                        this.window.vidLists.Children.Add(border);
                        this.index += 1;

                        included.Add(this.projectAccess.UserProjects[i].homeFiles[k].include);

                        if (this.projectAccess.UserProjects[i].homeFiles[k].include)
                        {
                            this.masterFiles.ChoosableFiles.Add(this.projectAccess.UserProjects[i].homeFiles[k].projectFilePath);
                            this.masterFiles.index.Add(k);
                        }
                    }
                }
            }


            this.masterFiles.buildMasterFileStackElement(); //build the list in the other page
        }

        async Task<string> createFileCopy(string filePath, string fileName)
        {
            //Debug.WriteLine("Hash Value: " + generate_hash(chosenFile));
            
            //Debug.WriteLine("Move the junk to prj: " + projectAccess.UserProjects[0].homeFiles[0].hash);

            //generate randomChars
            Random random = new Random();
            string endSpecifier = "";
            long total = 0;
            for(int i = 0; i < 10; i++)
            {
                endSpecifier += random.NextInt64(1, 9).ToString();
            }

            string[] fileSpec = fileName.Split('.');

            if (Directory.Exists(this.application.getCopyFileDir + "/" + this.currentProject))
            {
                File.Copy(filePath, this.application.getCopyFileDir() + "/" + fileSpec[0] + "_" + endSpecifier + "." + fileSpec[1]);
            }
            else
            {
                Directory.CreateDirectory(this.application.getCopyFileDir() + "/" + this.currentProject);
                File.Copy(filePath, this.application.getCopyFileDir() + "/" + this.currentProject + "/" + fileSpec[0] + "_" + endSpecifier + "." + fileSpec[1]);
            }
            

            return this.application.getCopyFileDir() + "/" + this.currentProject + "/" + fileSpec[0] + "_" + endSpecifier + "." + fileSpec[1];
        }

        public void createProject(object sender, EventArgs e)
        {
            TextBox tbox = this.window.projectNameInput;

            //create new HashFile object
            HomeFiles homeFiles = new HomeFiles();
            homeFiles.fps = 0;
            homeFiles.vbitrate = "0";
            homeFiles.vcodec = "h254";
            homeFiles.abitrate = "0";
            homeFiles.resolution = "1920x1080";
            homeFiles.acodec = "aac";
            homeFiles.include = true;
            homeFiles.fileName = "stuff.mp3";
            homeFiles.originalFilePath = "c://somefile.mp3";
            homeFiles.projectFilePath = "./somefile123.mp3";
            homeFiles.playbackSpeed = 1;
            homeFiles.hash = "3SW7AB19KMP";

            Projects newProject = new Projects();
            newProject.homeFiles = new List<HomeFiles> { homeFiles };
            newProject.clips = new List<Clips>();
            newProject.ProjectName = tbox.Text;

            projectAccess.UserProjects.Add(newProject);


            Debug.WriteLine("Project Name: " + picker.getProjectName());
            Debug.WriteLine("Project Index: " + picker.getProjectIndex());

            //JsonSerializer.Deserialize<Projects>(File.ReadAllText(this.fileCopiesDir + "/" + this.projectsFile, System.Text.Encoding.UTF8));
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;
            string jsonData = JsonSerializer.Serialize(projectAccess);

            Debug.WriteLine("Create Project: " + jsonData);

            //byte[] updatedJsonData = Encoding.UTF8.GetBytes(jsonData);
            //this.prjFile.Write(updatedJsonData);
            //this.prjFile.Flush();

            Debug.WriteLine("Created Project: " + tbox.Text);

            /*Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].ProjectName);
            Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].homeFiles[0].fileName);
            Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].homeFiles[0].originalFilePath);
            Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].homeFiles[0].projectFilePath);
            Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].homeFiles[0].abitrate);
            Debug.WriteLine("Create Project: " + projectAccess.UserProjects[2].homeFiles[0].acodec);

            Projects remade = JsonSerializer.Deserialize<Projects>(jsonData);

            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].ProjectName);
            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].homeFiles[0].fileName);
            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].homeFiles[0].originalFilePath);
            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].homeFiles[0].projectFilePath);
            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].homeFiles[0].abitrate);
            Debug.WriteLine("Create Project 2: " + remade.UserProjects[2].homeFiles[0].acodec);*/

            //this.window.ProjectPickerPanel.Visibility = Visibility.Hidden;
            //this.window.MainEditor.Visibility = Visibility.Visible;

            for (int i = 0; i < projectAccess.UserProjects.Count; i++)
            {
                //Debug.WriteLine("Create Project: " +jsonData);
            }
        }
    }
}
