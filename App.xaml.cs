using System.Configuration;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace InfernoVEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string fileCopiesDir = "./Projects";
        private string projectsFile = "projects.json";
        private string fileCopies = "projectFileCopies";

        public ProjectController controller;
        private FileStream fstream;
      

        [STAThread]
        static void Main()
        {
            // TODO Whatever you want to do before starting
            // the WPF application and loading all WPF dlls
            var app = new App();
            app.DispatcherUnhandledException += App_DispatcherUnhandledException;
            app.sub();
            app.InitializeComponent();
            app.Run();
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("Error Message: " + e.Exception.Message);
            //throw new NotImplementedException();
        }

        async public void error_handle(object sender, Exception e)
        {

        }

        async public void sub()
        {
            await loadLib();
        }

        //check if fileCopies directory was created.
        //fileCopies directory basically contains a copy of the file the user inserted. those are the actual files the user is editing.
        async public Task<bool> ProjectCopyDirExist()
        {
            if (Directory.Exists(this.fileCopiesDir + "/" + this.fileCopies))
            {
                return true;
            }
            else
            {
                Directory.CreateDirectory(this.fileCopiesDir + "/" + this.fileCopies);
                return false;
            }
        }

        //check if user's created projects file is saved.
        //this file holds the files that were included in a project and the path to those files and the corresponding copy files if they were created.
        async public Task<bool> ProjectFileExist()
        {
            if (File.Exists(this.fileCopiesDir + "/" + this.projectsFile))
            {
                CreateGlobalSingleton();
                openProjectFile(this.fileCopiesDir + "/" + this.projectsFile);
                return true;
            }
            else
            {
                FileStream stream1 = File.Open(this.fileCopiesDir + "/" + this.projectsFile, FileMode.Create);
                //byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{\"UserProjects\":[{\"ProjectName\":\"project1\", \"AddedFiles\":[], \"ProjectFiles\":[]}]}"); //File Format
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{\"UserProjects\":[]}");
                stream1.Write(bytes);
                stream1.Close();
                /*File.Create(this.projectsFile);
                StreamWriter writer = new StreamWriter(this.projectsFile);
                writer.Write("{}");
                writer.Close();*/
                CreateGlobalSingleton();
                return false;
            }
        }

        public void CreateGlobalSingleton()
        {
            controller = ProjectController.getModel();
            controller.setProject(JsonSerializer.Deserialize<Projects>(File.ReadAllText(this.fileCopiesDir + "/" + this.projectsFile, System.Text.Encoding.UTF8)));
            //Debug.WriteLine("Move the junk to prj: " + controller.getProject().UserProjects[0].homeFiles[0].hash);
        }

        public ProjectController getProjectSingleton()
        {
            return this.controller;
        }

        async private void openProjectFile(string path) //opens the project file so it can be used for everything
        {
            this.fstream = new FileStream(path, FileMode.Open);
        }

        public FileStream getProjectFileStream()
        {
            return this.fstream;
        }

        public string getCopyFileDir()
        {
            return this.fileCopiesDir + "/" + this.fileCopies;
        }

        async private Task<bool> loadLib()
        {
            Unosquare.FFME.Library.FFmpegDirectory = @"E:\\Documents\\Playground\\MessengerCommandLineTool\\InfernoVEditor\\InfernoVEditor\\InfernoVEditor\\Lib\\ffmpeg\\x64";
            bool loaded = await Unosquare.FFME.Library.LoadFFmpegAsync();
            Debug.WriteLine("FFMPEG Version: " + Unosquare.FFME.Library.FFmpegVersionInfo);
            Debug.WriteLine("FFMPEG Loaded: " + loaded.ToString());
            await ProjectCopyDirExist();
            await ProjectFileExist();
            return true;
        }
    }
}
