using NAudio.Wave;
using Splicer.Renderer;
using Splicer.Timeline;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RecordRTC.Controllers
{
    public class HomeController : Controller
    {
        List<string> lstFilenames = null;

        public ActionResult Index()
        {

            // var path = AppDomain.CurrentDomain.BaseDirectory + "uploads/";
            // string[] files = Directory.GetFiles(path, "*.avi", SearchOption.AllDirectories);

            //// Concatenate(path + "/pankaj1.wav", files);
            // ConcatenateVideo(path + "/pankaj1.avi", files);

            return View();

        }
        

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult MultiStreamRecorder()
        {
            Session["filename"] = new List<string>();
            return View();
        }

        // ---/RecordRTC/PostRecordedAudioVideo
        [HttpPost]
        public ActionResult PostRecordedAudioVideo()
        {
            foreach (string upload in Request.Files)
            {
                var path = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg/uploads/";
                var file = Request.Files[upload];
                if (file == null) continue;
                if (Session["filename"] == null)
                {
                    Session["filename"] = new List<string>();
                }
                else
                {
                    lstFilenames = (Session["filename"] as List<string>);
                }

                lstFilenames.Add(Path.Combine(path, Request.Form[0]));
                Session["filename"] = lstFilenames;

                file.SaveAs(Path.Combine(path, Request.Form[0]));
            }
            return Json(Request.Form[0]);
        }

        // ---/RecordRTC/DeleteFile
        [HttpPost]
        public ActionResult DeleteFile()
        {
            var fileUrl = AppDomain.CurrentDomain.BaseDirectory + "uploads/" + Request.Form["delete-file"];
            new FileInfo(fileUrl + ".wav").Delete();
            new FileInfo(fileUrl + ".webm").Delete();
            return Json(true);
        }

        [HttpGet]
        public string MergeALL()
        {
            var ffmpegpath = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg\\";
            var uploadpath = ffmpegpath + "uploads\\";
            lstFilenames = (List<string>)(Session["filename"]);
            string[] mp4files = Directory.GetFiles(uploadpath, "*.mp4", SearchOption.AllDirectories).ToList().OrderBy(a => a).ToArray();
            string[] wavfiles = Directory.GetFiles(uploadpath, "*.wav", SearchOption.AllDirectories).ToList().OrderBy(a => a).ToArray();


            using (StreamWriter writer = new StreamWriter(ffmpegpath + "batchcmd.bat"))
            {
                writer.WriteLine("echo y | del merged\\*.mp4*");
                string intermediateFile = string.Empty;
                for (int i = 0; i < lstFilenames.Count; i++)
                {
                    FileInfo f = new FileInfo(lstFilenames[i]);

                    string fileName = f.Name;//mp4files[i].Substring(mp4files[i].LastIndexOf('\'') + 1, mp4files[i].LastIndexOf('.'));
                    writer.WriteLine("bin\\ffmpeg -y -i \"{0}\" -qscale:v 1 {1}.mpg", "uploads\\" + fileName, fileName);
                    intermediateFile += "|" + fileName + ".mpg";
                    //f = new FileInfo(wavfiles[i]);
                    //fileName = f.Name;//.Substring(0, wavfiles[i].LastIndexOf('.'));
                    //writer.WriteLine("bin\\ffmpeg -y -i \"{0}\" -qscale:v 1 {1}.mpg", "uploads\\" + fileName, fileName);
                    //intermediateFile += "|" + fileName + ".mpg";
                }
                intermediateFile = intermediateFile.Remove(0, 1);
                Session["filename"] = new List<string>();

                writer.WriteLine("bin\\ffmpeg -y -i concat:\"{0}\" -c copy intermediate_all.mpg", intermediateFile);
                writer.WriteLine("bin\\ffmpeg -y -i intermediate_all.mpg -qscale:v 2 merged\\output.mp4");
                writer.WriteLine("echo y | del *.mpg*");
                writer.WriteLine("echo y | del uploads\\*.wav*");
                writer.WriteLine("echo y | del uploads\\*.mp4*");


                writer.Close();
            }


            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo.WorkingDirectory = ffmpegpath;
                proc.StartInfo.FileName = ffmpegpath + "batchcmd.bat";
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }

            return "/ffmpeg/merged/output.mp4";

        }

        public static void Concatenate(string outputFile, IEnumerable<string> sourceFiles)
        {
            byte[] buffer = new byte[1024];
            WaveFileWriter waveFileWriter = null;

            try
            {
                foreach (string sourceFile in sourceFiles)
                {
                    using (WaveFileReader reader = new WaveFileReader(sourceFile))
                    {
                        if (waveFileWriter == null)
                        {
                            // first time in create new Writer
                            waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
                        }
                        else
                        {
                            if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                            {
                                throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                            }
                        }

                        int read;
                        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            waveFileWriter.WriteData(buffer, 0, read);
                        }
                    }
                }
            }
            finally
            {
                if (waveFileWriter != null)
                {
                    waveFileWriter.Dispose();
                }
            }

        }

        private static void ConcatenateVideo(string outputFile, IEnumerable<string> sourceFiles)
        {

            using (ITimeline timeline = new DefaultTimeline())
            {
                IGroup group = timeline.AddVideoGroup(32, 720, 576);

                IClip lastVideoClip = null;// group.AddTrack().AddVideo(firstVideoFilePath);
                // var secondVideoClip = group.AddTrack().AddVideo(secondVideoFilePath, firstVideoClip.Duration);
                foreach (var item in sourceFiles)
                {
                    if (lastVideoClip != null)
                    {
                        lastVideoClip = group.AddTrack().AddVideo(item, lastVideoClip.Duration);
                    }
                    else
                    {
                        lastVideoClip = group.AddTrack().AddVideo(item);
                    }

                }

                using (AviFileRenderer renderer = new AviFileRenderer(timeline, outputFile))
                {
                    renderer.Render();
                }
            }
        }


    }
}