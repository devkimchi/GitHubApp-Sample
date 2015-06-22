using System.Collections.Generic;

namespace GitHubAppSample.WebApp.Models
{
    public class HomeIndexViewModel
    {
        public string Username { get; set; }

        public IList<string> Organisations { get; set; }
    }
}