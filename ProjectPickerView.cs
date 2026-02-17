using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InfernoVEditor
{
    internal class ProjectPickerView
    {
        MainWindow window;
        FileStream prjFile;
        private double labelSize = 18;
        Utils util;
        private string currentProject;
        int projectIndex = 0;
        App application;
        Projects prj;
        FileSelections fileSelection;
        Clipping clipping;

        public ProjectPickerView(MainWindow window, App app, Utils util)
        {
            this.application = app;
            this.window = window;
            this.prjFile = this.application.getProjectFileStream();
            this.prj = this.application.getProjectSingleton().getProject();
            this.util = util;
            this.showCreatedProjects();
        }

        void showCreatedProjects()
        {
            //get created project panel
            StackPanel projectsPanel = this.window.CreatedProjectsPanel;

            byte[] arr = new byte[prjFile.Length];
            string jsonData = "";
            //read fileStream stuff
            while(true)
            {
                if (prjFile.Read(arr, 0, (int)prjFile.Length) != 0)
                {
                    jsonData += Encoding.UTF8.GetString(arr);
                }
                else
                {
                    break;
                }
            }

            Projects prj = JsonSerializer.Deserialize<Projects>(jsonData);

            Debug.WriteLine(prj.UserProjects.Count);
            for(int i = 0; i < prj.UserProjects.Count; i++)
            {
                //create the elements in this junketh
                this.window.CreatedProjectsPanel.Children.Add(projectBorder("Project: " + prj.UserProjects[i].ProjectName));
            }
        }

        private Border projectBorder(string projectName)
        {
            //create border
            Border border = new Border();
            border.BorderBrush = new SolidColorBrush(Colors.Black);
            border.BorderThickness = new Thickness(0, 0, 0, 2);

            //create stackPanel
            StackPanel panel = new StackPanel();
            panel.Height = 80;

            //create Labels
            Label prjNameLabel = new Label();
            prjNameLabel.Content = projectName;
            prjNameLabel.FontSize = this.labelSize;
            prjNameLabel.Foreground = new SolidColorBrush(Colors.White);

            Label accessedDate = new Label();
            accessedDate.Content = "Modified: 3/15/2025";
            accessedDate.FontSize = this.labelSize;
            accessedDate.Foreground = new SolidColorBrush(Colors.White);

            //add labels to new stackPanel
            panel.Children.Add(prjNameLabel);
            panel.Children.Add(accessedDate);

            //add panel to border
            border.Child = panel;
            border.AddHandler(Border.MouseLeftButtonDownEvent, new RoutedEventHandler(ClickEvent));
            border.AddHandler(Border.MouseEnterEvent, new RoutedEventHandler(HoverEvent));
            border.AddHandler(Border.MouseLeaveEvent, new RoutedEventHandler(UnHoverEvent));

            return border;
        }

        public string getProjectName()
        {
            return this.currentProject;
        }

        public int getProjectIndex()
        {
            return this.projectIndex;
        }

        public void setFileSelection(FileSelections fileSelections)
        {
            this.fileSelection = fileSelections;
        }

        public void setClipping(Clipping clipping)
        {
            this.clipping = clipping;
        }

        private void ClickEvent(object sender, EventArgs e)
        {
            StackPanel panel = (StackPanel)((Border)sender).Child;
            Label prjNameLabel = (Label)panel.Children[0];

            this.currentProject = prjNameLabel.Content.ToString().Replace("Project: ", "").Trim();

            for (short i = 0; i < this.prj.UserProjects.Count; i++)
            {
                if (this.prj.UserProjects[i].ProjectName == currentProject)
                {
                    this.fileSelection.setGlobalValues(this.currentProject, i);
                    this.clipping.setGlobalValues(this.currentProject);
                    this.projectIndex = i;
                    break;
                }
            }

            this.window.ProjectPickerPanel.Visibility = Visibility.Hidden;
            this.window.MainEditor.Visibility = Visibility.Visible;
            this.fileSelection.loadFiles();
            this.clipping.loadClips();
        }

        private void HoverEvent(object sender, EventArgs e)
        {
            Border b = (Border)sender;
            b.Background = new SolidColorBrush(Colors.Gray);
            Mouse.OverrideCursor = Cursors.Hand;
        }
        private void UnHoverEvent(object sender, EventArgs e)
        {
            Border b = (Border)sender;
            b.Background = this.util.customHexColor("FF332E2E");
            Mouse.OverrideCursor = null;
        }
    }
}
