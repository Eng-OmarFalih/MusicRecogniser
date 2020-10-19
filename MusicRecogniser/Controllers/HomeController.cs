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
        public string APIToken = "437cbaac5e2cb217ece5f68293d7fff7";
        public IActionResult Index()
        {
            var User = new UserSessionModel() { SessionID = Guid.NewGuid(), SessionDate = DateTime.Now, Progress = 0 };
            Startup.Users.Add(User);
            HttpContext.Session.SetString("SessionID", User.SessionID.ToString());
            return View();
        }

        [HttpPost]
        public JsonResult Get_Videos([FromBody]ParamertersModel data)
        {
            string sid = HttpContext.Session.GetString("SessionID");
            Operations op = new Operations(sid);
            try 
            {
                if (data.URL.Trim() == "")
                {
                    return Json(new { success = false });
                }
                string LocationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string FileName = Guid.NewGuid().ToString();
                FileName = op.Execute(LocationPath + "\\Services\\youtube-dl.exe", data.URL + " -f best -o " + FileName + ".%(ext)s", true);
                string InPath = "./" + FileName;
                string OutPath = FileName.Replace(FileName.Split('.').Last(), string.Empty) + "mp3";
                string Arguments = $"-i {InPath} -t 60 {OutPath}";
                op.Execute(LocationPath + "\\Services\\ffmpeg.exe", Arguments, false);
                string AudioName = OutPath;
                string ArtistInfo = op.Get_Artist(AudioName, APIToken);
                op.DeleteFile(InPath);
                op.DeleteFile(OutPath);
                if (ArtistInfo != "")
                {
                    var ListVideos = op.YoutubeSearch(ArtistInfo).Result;
                    op.DeleteOldSessions();
                    return Json(new { success = true, response = ListVideos });
                }
                else
                {
                    op.DeleteOldSessions();
                    return Json(new { success = false });
                }
            }
            catch
            {
                op.DeleteOldSessions();
                return Json(new { success = false });
            }
        }
        public IActionResult Progress()
        {
            try
            {
                Guid? id = new Guid(HttpContext.Session.GetString("SessionID"));
                var user = Startup.Users.FirstOrDefault(V => V.SessionID == id);
                return Json(user.Progress);
            }
            catch (Exception ex)
            { }
            return Json(new { success = false });
        }
    }
}