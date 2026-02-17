using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace InfernoVEditor
{
    class Utils
    {
        private string videoStreamCommand = "-show_streams -select_streams v:0 ";
        private string audioStreamCommand = "-show_streams -select_streams a:0 ";
        private string allStreams = "-show_format -show_streams ";
        public async Task<string[]> getInfoAsync(string fileName, bool isVideoInfo=true) //gets printout of ffmpeg output
        {
            string commandExt = "";
            if (isVideoInfo)
            {
                //commandExt = this.videoStreamCommand;
                commandExt = this.allStreams;
            }
            else { commandExt = this.audioStreamCommand; }

            DateTime dateTime = DateTime.Now;
            string[] sDateTime = dateTime.ToString().Split(" ");
            string time_in_seconds = TimeToSeconds(sDateTime[1]).ToString();
            string date_underscore = sDateTime[0].Replace("/", "-");
            //MessageBox.Show("infernoOutput_"+date_underscore+"_"+time_in_seconds);
            await Task.Delay(1000);
            //Thread.Sleep(1000);
            //Initialize the process (note can use Process class too to do same thing)
            Process process = new Process();
            //process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.FileName = "./lib/ffmpeg/ffprobe.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.Arguments = "/C ffprobe " + commandExt + "-i \"" + fileName + "\"";
            process.StartInfo.Arguments = commandExt + "-i \"" + fileName + "\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            Debug.WriteLine(process.StartInfo.Arguments);

            //start the process
            process.Start();

            //capture the output
            string result = process.StandardOutput.ReadToEnd();
            string result2 = process.StandardError.ReadToEnd();

            //wait for the process to exit
            process.WaitForExit();

            return [result, result2];
        }
        private string convertSingle(string value)
        {
            if (value.Length == 1)
            {
                return "0" + value;
            }
            else
            {
                return value;
            }
        }
        private string removeDouble(string value) //removes string numbers that have leading 0's and are 2 in length
        {
            char[] arr = value.ToCharArray();
            if (arr.Length == 2 && arr[0] == '0')
            {
                return arr[1].ToString();
            }
            else
            {
                return value;
            }
        }

        private string addZero(string value)
        {
            if(value.Length == 1)
            {
                return "0" + value;
            }
            return value;
        }

        public int TimeToSeconds(string value) //turns time format into readable seconds
        {
            string[] split = value.Split(":");
            int secInHr = 3600;
            int secInMin = 60;
            int secInSec = 1;
            int total = 0;

            if (split.Length > 1)
            {
                for (short i = 0; i < split.Length; i++)
                {//hr = 0; min = 1; sec = 2
                    if (i == 0) { total += (int.Parse(removeDouble(split[i])) * secInHr); }
                    if (i == 1) { total += (int.Parse(removeDouble(split[i])) * secInMin); }
                    if (i == 2) { total += (int)(float.Parse(removeDouble(split[i])) * 1); }
                }
            }

            return total;
        }

        public string SecondsToTime(int seconds)
        {
            double sec2 = seconds;
            double hours = seconds / 3600;

            string newFormat = "";

            sec2 = sec2 - (3600 * hours); //get hours left
           // Debug.WriteLine("Hours: " + hours);
            newFormat += addZero(hours.ToString()) + ":";
            double minutes = Math.Floor(sec2 / 60);
            //Debug.WriteLine("Minutes: " + minutes);
            newFormat += addZero(minutes.ToString()) + ":";
            sec2 = sec2 - (minutes * 60);
            //Debug.WriteLine("Seconds: " + sec2);
            newFormat += addZero(sec2.ToString());
            //Debug.WriteLine("Hours: " + hours.ToString() + ", Minutes: " + minutes.ToString());

            return newFormat;
        }

        public int hexToInt(string hex)
        {
            char [] hexString = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            short[] hexValue = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

            int total = 1;

            for(short i = 0; i < hex.Length; i++)
            {
                if (hexString.Contains(hex[i]))
                {
                    total *= hexValue[Array.IndexOf(hexString, hex[i])];
                }
            }

            return total;
        }

        public SolidColorBrush customHexColor(string clipHex)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                            Convert.ToByte(clipHex.Substring(0, 2), 16),
                            Convert.ToByte(clipHex.Substring(2, 2), 16),
                            Convert.ToByte(clipHex.Substring(4, 2), 16),
                            Convert.ToByte(clipHex.Substring(6, 2), 16)
                            ));
        }

        async public Task<bool> transcode_clip(MainWindow window, ProgressBar progressBar, Label progressLabel, string fileName, string outputName, float start, float end, bool hasVideo, bool hasAudio)
        {
            string commandExt = "";

            if (hasAudio) {
                commandExt += "-c:a aac";
            }if (hasVideo) {
                if (hasAudio) { commandExt += " "; }
                commandExt += "-c:v libx264";
            }

            DateTime dateTime = DateTime.Now;
            string[] sDateTime = dateTime.ToString().Split(" ");
            string time_in_seconds = TimeToSeconds(sDateTime[1]).ToString();
            string date_underscore = sDateTime[0].Replace("/", "-");
            //MessageBox.Show("infernoOutput_"+date_underscore+"_"+time_in_seconds);
            //await Task.Delay(1000);
            //Thread.Sleep(1000);
            //Initialize the process (note can use Process class too to do same thing)
            Process process = new Process();
            //process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.FileName = "./Lib/ffmpeg/ffmpeg.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.Arguments = "/C ffprobe " + commandExt + "-i \"" + fileName + "\"";
            process.StartInfo.Arguments = "-i \"" + fileName + "\" -y " + commandExt + " -ss " + start.ToString() + " -to " + end.ToString() + " " + outputName;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            Debug.WriteLine("Arguments: " + process.StartInfo.Arguments);
            //return true;
            progressBar.Value = 0;

            await Task.Run(() =>
            {
                StringBuilder error = new StringBuilder();

                //for reading the process as it's running. it's not running yet until you start
                process.OutputDataReceived += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    //Debug.WriteLine(e.Data);
                    Regex reg = new Regex("time=\\d+:\\d+:\\d+");
                    if (e.Data != null)
                    {
                        Match m1 = reg.Match(e.Data);
                        if (m1.Success)
                        {
                            //float progress = 0;
                            float progress = (float)TimeToSeconds(m1.Groups[0].ToString().Replace("time=", "").Trim()) / (end-start);
                            Debug.WriteLine((int)(progress * 100));
                            window.Dispatcher.Invoke(new Action(() =>
                            {
                                progressBar.Value = (int)(progress * 100);
                                progressLabel.Content = ((int)(progress * 100)).ToString();
                                //progressBar.Update();
                            }));
                        }
                    }
                };

                //start the process
                process.Start();

                //capture the output
                //string result = process.StandardOutput.ReadToEnd();
                //string result2 = process.StandardError.ReadToEnd();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                //wait for the process to exit
                //print("ffmpeg -y -i \"" + MainFile.Text + "\"-c:v h264_nvenc -c:a copy ./" + this.outputFile);
                process.WaitForExit();

                window.Dispatcher.Invoke(new Action(() =>
                {
                    //UNC progressBar.Value = 100;
                }));

                process.Close();

                //print("Exit Code: " + process.ExitCode.ToString());

                //MessageBox.Show("Accept: " + result + ", Deny: " + result2);
            });

            //return [result, result2];
            return true;
        }
    }
}
