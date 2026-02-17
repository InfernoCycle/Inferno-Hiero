using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace InfernoVEditor
{
    public class HomeFiles
    {
        public string fileName { get; set; }
        public bool include { get; set; }
        public bool hasAudio { get; set; } = false;
        public bool hasVideo { get; set; } = false;
        public string originalFilePath { get; set; }
        public string projectFilePath { get; set; }
        public string hash { get; set; }
        public string resolution { get; set; }
        public int fps { get; set; }
        public string vcodec { get; set; }
        public string vbitrate { get; set; }
        public string sample_rate { get; set; }
        public string acodec { get; set; }
        public string abitrate { get; set; }
        public int channels { get; set; }
        public double duration { get; set; }
        public int playbackSpeed { get; set; } = 1;
    }

    public class Clips
    {
        public Guid Id { get; set; }
        public string clipName { get; set; }
        public float start { get; set; }
        public float end { get; set; }
        public string sourcePath {get; set;}
        public bool hasAudio { get; set; } = false;
        public bool hasVideo { get; set; } = false;
        public double duration { get; set; }
        public string hash { get; set;}

        public double Duration => end - start;

        public Clips()
        {
            Id = Guid.NewGuid(); // Ensure every clip has an ID
        }

        public string ClipType
        {
            get
            {
                if (hasVideo && hasAudio) return "Audio/Video";
                if (hasVideo) return "Video Only";
                if (hasAudio) return "Audio Only";
                return "Unknown";
            }
        }
    }

    public class Projects
    {
        public List<Projects> UserProjects { get; set; }

        public string ProjectName { get; set; }

        //public List<string> AddedFiles { get; set; } //the original files the user used.

        //public List<string> ProjectFiles { get; set; } //the edited files the user is editing.

        public List<Clips> clips { get; set; }

        public List<HomeFiles> homeFiles { get; set; }
    }

    public class ProjectController
    {
        private static Projects prj;
        private static ProjectController model;

        private MainWindow window;

        private ProjectController() {
            
        }

        public static ProjectController getModel()
        {
            if (model == null)
            {
                prj = new Projects();
                model = new ProjectController();
            }
            return model;
        }

        public Projects getProject()
        {
            return prj;
        }

        public void setProject(Projects prj1)
        {
            prj = prj1;
        }

        public void addProjectFile(string path)
        {

        }
    }
}
