using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MusicRecogniser.Models;
using RestSharp;
using Newtonsoft.Json;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

namespace MusicRecogniser.Controllers
{
    public class HomeController : Controller
    {
        Regex regex = new Regex("(\\d{0,2}.\\d{0,2})%");
        public string APIToken = "437cbaac5e2cb217ece5f68293d7fff7";
        public IActionResult Index()
        {
            var User = new UserSessionModel() { SessionID = Guid.NewGuid(), SessionDate = DateTime.Now, Progress = 0 };
            Startup.Users.Add(User);
            HttpContext.Session.SetString("SessionID", User.SessionID.ToString());
            return View();
        }
        void setSession(int value)
        {
            Guid? id = new Guid(HttpContext.Session.GetString("SessionID"));
            var user = Startup.Users.FirstOrDefault(V => V.SessionID == id);
            user.Progress = value;
        }
        int getSession()
        {
            Guid? id = new Guid(HttpContext.Session.GetString("SessionID"));
            var user = Startup.Users.FirstOrDefault(V => V.SessionID == id);
            return user.Progress;
        }
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            var count = HttpContext.Session.GetInt32("Progress");

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public string Execute(string exePath, string parameters, bool isDownload)
        {
            try
            {
                setSession(0);

                List<string> list = new List<string>();
                Process p = new Process();

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = parameters;
                p.Start();
                string st = "";
                while ((st = p.StandardOutput.ReadLine()) != null)
                {
                    try
                    {
                        list.Add(st);
                        var match = regex.Match(st);
                        if (match.Success && match.Groups.Count != 0)
                        {
                            setSession((int)Convert.ToDouble(match.Groups[1].Value));
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                if (isDownload)
                    return list.FirstOrDefault(v => v.Contains("Destination:")).Replace("[download] Destination:", string.Empty).Replace("\"", string.Empty).Trim();
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        string Get_Artist(string FilePath, string API_Token)
        {
            setSession(0);
            var client = new RestClient("https://api.audd.io/");
            var request = new RestRequest();
            request.AddParameter("api_token", API_Token);
            request.AddFile("file", FilePath);
            var response = client.Post(request);
            var result = JsonConvert.DeserializeObject<MainClass>(response.Content);
            if (result.status == "success")
            {
                setSession(100);
                return result.result.artist;
            }
            else
            {
                return "";
            }
        }

        private async Task<List<VideoModel>> YoutubeSearch(string ArtistName)
        {
            setSession(0);
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAt4iuuOV_HFVenpsPtT9J8-sNVWDbOCco",
                ApplicationName = this.GetType().ToString()
            });
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = ArtistName;
            searchListRequest.MaxResults = 20;
            var searchListResponse = await searchListRequest.ExecuteAsync();
            List<VideoModel> videos = new List<VideoModel>();
            int count = 0;
            foreach (var searchResult in searchListResponse.Items)
            {
                count++;
                if (searchResult.Id.Kind == "youtube#video")
                {
                    videos.Add(new VideoModel() { Title = searchResult.Snippet.Title, VideoId = searchResult.Id.VideoId });
                    setSession((int)(count / (searchListResponse.Items.Count * 100.0)));
                }
            }
            return videos;
        }

        [HttpPost]
        public JsonResult Get_Videos([FromBody]ParamertersModel data)
        {
            try
            {
                if (data.URL.Trim() == "")
                {
                    return Json(new { success = false });
                }
                string LocationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string FileName = Guid.NewGuid().ToString();
                FileName = Execute(LocationPath + "\\Services\\youtube-dl.exe", data.URL + " -f best -o " + FileName + ".%(ext)s", true);
                string InPath = "./" + FileName;
                string OutPath = FileName.Replace(FileName.Split('.').Last(), string.Empty) + "mp3";
                string Arguments = $"-i {InPath} -t 60 {OutPath}";
                Execute(LocationPath + "\\Services\\ffmpeg.exe", Arguments, false);
                string AudioName = OutPath;
                string ArtistInfo = Get_Artist(AudioName, APIToken);
                DeleteFile(InPath);
                DeleteFile(OutPath);
                if (ArtistInfo != "")
                {
                    var ListVideos = YoutubeSearch(ArtistInfo).Result;
                    DeleteOldSessions();
                    return Json(new { success = true, response = ListVideos });
                }
                else
                {
                    DeleteOldSessions();
                    return Json(new { success = false });
                }
            }
            catch
            {
                DeleteOldSessions();
                return Json(new { success = false });
            }
        }
        void DeleteOldSessions()
        {
            var Items = Startup.Users.Where(v => v.SessionDate < DateTime.Now.AddDays(-5)).ToList();
            foreach (var item in Items)
            {
                Startup.Users.Remove(item);
            }
        }

        void DeleteFile(string path)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            catch (Exception)
            {

            }

        }
        public IActionResult Progress()
        {
            try
            {
                return Json(getSession());
            }
            catch (Exception ex)
            {

            }

            return Json(new { success = false });
        }
    }
}
