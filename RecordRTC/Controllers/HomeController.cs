using NAudio.Wave;
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
                if (!Directory.Exists(path))
                {
                     Directory.CreateDirectory(path);
                }
                var file = Request.Files[upload];
                if (file == null) continue;
              
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
            List<FilesPath> lstmp4Files = new List<FilesPath>();
            List<FilesPath> lstwavFiles = new List<FilesPath>();

            List<string> allmp4files = Directory.GetFiles(uploadpath, "*.mp4*", SearchOption.AllDirectories).ToList();
            List<string> allwavfiles = Directory.GetFiles(uploadpath, "*.wav*", SearchOption.AllDirectories).ToList();

            foreach (var item in allmp4files)
            {
                FileInfo f = new FileInfo(item);
                FilesPath file = new FilesPath();
                file.File = f;
                string OrderBy = f.Name.Substring(0, f.Name.LastIndexOf('.'));
                string fileType = f.Name.Substring(f.Name.LastIndexOf('.') + 1, 3);
                if (fileType.ToLower().Equals("mp4"))
                {
                    file.Mp4FilePath = item;
                    file.FileType = 2;
                }
                else if (fileType.ToLower().Equals("wav"))
                {
                    file.WavFileType = item;
                    file.FileType = 1;
                }
                file.OrderBy = Convert.ToInt32(OrderBy);
                lstmp4Files.Add(file);
            }
            foreach (var item in allwavfiles)
            {
                FileInfo f = new FileInfo(item);
                FilesPath file = new FilesPath();
                file.File = f;
                string OrderBy = f.Name.Substring(0, f.Name.LastIndexOf('.'));
                string fileType = f.Name.Substring(f.Name.LastIndexOf('.') + 1, 3);
                if (fileType.ToLower().Equals("mp4"))
                {
                    file.Mp4FilePath = item;
                    file.FileType = 2;
                }
                else if (fileType.ToLower().Equals("wav"))
                {
                    file.WavFileType = item;
                    file.FileType = 1;
                }
                file.OrderBy = Convert.ToInt32(OrderBy);
                lstwavFiles.Add(file);
            }

            lstmp4Files = lstmp4Files.OrderBy(a => a.OrderBy).ToList();
            lstwavFiles = lstwavFiles.OrderBy(a => a.OrderBy).ToList();


            using (StreamWriter writer = new StreamWriter(ffmpegpath + "batchcmd.bat"))
            {
                writer.WriteLine("echo y | del merged\\*.mp4*");
                string intermediateFile = string.Empty;
                foreach (var item in lstmp4Files)
                {
                    string fileName = item.File.Name;
                    writer.WriteLine("bin\\ffmpeg -y -i \"{0}\" -qscale:v 1 {1}.mpg", "uploads\\" + fileName, fileName);
                    intermediateFile += "|" + fileName + ".mpg";
                    FilesPath wavFile = lstwavFiles.Where(a => a.OrderBy == item.OrderBy).FirstOrDefault();
                    if (wavFile != null)
                    {
                        fileName = wavFile.File.Name;
                        writer.WriteLine("bin\\ffmpeg -y -i \"{0}\" -qscale:v 1 {1}.mpg", "uploads\\" + fileName, fileName);
                        intermediateFile += "|" + fileName + ".mpg";
                    }
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
           
                proc = new Process();
                proc.StartInfo.WorkingDirectory = ffmpegpath;
                proc.StartInfo.FileName = ffmpegpath + "batchcmd.bat";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
                proc.WaitForExit();
       
            
              //  Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());


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


    }
    class FilesPath
    {
        public int OrderBy { get; set; }
        public FileInfo File { get; set; }
        public string Mp4FilePath { get; set; }
        public string WavFileType { get; set; }
        //1 audio 2 video
        public int FileType { get; set; }
    }
}