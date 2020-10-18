using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicRecogniser.Models
{
    public class ResultClass
    {
        public string artist { get; set; }
        public string title { get; set; }
        public string album { get; set; }
        public string release_date { get; set; }
        public string label { get; set; }
        public string timecode { get; set; }
        public string song_link { get; set; }
    }
}
