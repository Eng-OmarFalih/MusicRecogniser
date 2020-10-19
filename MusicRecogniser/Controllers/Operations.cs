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
using System.Web;

namespace MusicRecogniser.Controllers
{
    public class Operations
    {
        Regex regex = new Regex("(\\d{0,2}.\\d{0,2})%");
        public string SessionID;

        public Operations(string sid) {
            SessionID = sid;
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
        public string Get_Artist(string FilePath, string API_Token)
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
        public async Task<List<VideoModel>> YoutubeSearch(string ArtistName)
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
        public void DeleteOldSessions()
        {
            var Items = Startup.Users.Where(v => v.SessionDate < DateTime.Now.AddDays(-5)).ToList();
            foreach (var item in Items)
            {
                Startup.Users.Remove(item);
            }
        }
        public void DeleteFile(string path)
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
        void setSession(int value)
        {
            Guid? id = new Guid(SessionID);
            var user = Startup.Users.FirstOrDefault(V => V.SessionID == id);
            user.Progress = value;
        } 
    }
}
