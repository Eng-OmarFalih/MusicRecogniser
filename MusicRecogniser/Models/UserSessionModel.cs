using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicRecogniser.Models
{
    public class UserSessionModel
    {
        public Guid? SessionID { get; set; }
        public DateTime? SessionDate { get; set; }
        public int Progress { get; set; }
    }
}
