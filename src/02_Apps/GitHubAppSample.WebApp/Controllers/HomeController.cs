using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GitHubAppSample.WebApp.Models;
using Octokit;

namespace GitHubAppSample.WebApp.Controllers
{
    public partial class HomeController : Controller
    {
        /* ********************************************************
         * NOTE: Both CLIENT_ID and CLIENT_SECRET *MUST* be stored
         *       in a different way like Web.config.
         * ********************************************************/
        private const string CLIENT_ID = "2b09ada4c97ab1b398c4";
        private const string CLIENT_SECRET = "9e155a3038c3fba80f48e1411df555f509dd9be1";

        /* ********************************************************
         * NOTE: REDIRECT_URL *MUST* be stored
         *       in a different way like Web.config.
         * ********************************************************/
        private const string REDIRECT_URL = "http://localhost:14083/Home/Authorise";

        /* ********************************************************
         * NOTE: GitHubClient should be managed by IoC container
         *       for better maintenance.
         * ********************************************************/
        private static readonly GitHubClient github = new GitHubClient(new ProductHeaderValue("GitHubAppSample"));

        public virtual async Task<ActionResult> Index()
        {
            var gitHubCookie = Request.Cookies["gitHubOauth"];
            if (gitHubCookie == null)
            {
                var gitHubLoginUrl = this.GetGitHubLoginUrl();
                return this.Redirect(gitHubLoginUrl.ToString());
            }

            var token = gitHubCookie.Value;
            github.Credentials = new Credentials(token);

            var user = await github.User.Current();
            IList<string> organisations;
            try
            {
                var orgs = await github.Organization.GetAllForCurrent();
                organisations = orgs.Select(p => p.Login).ToList();
            }
            catch
            {
                organisations = new List<string>();
            }

            var vm = new HomeIndexViewModel() { Username = user.Login, Organisations = organisations };
            return View(vm);
        }

        public virtual async Task<ActionResult> Authorise(string code, string state)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                var gitHubLoginUrl = this.GetGitHubLoginUrl();
                return this.Redirect(gitHubLoginUrl.ToString());
            }

            var stateCookie = Request.Cookies["gitHubState"];
            if (stateCookie == null)
            {
                var gitHubLoginUrl = this.GetGitHubLoginUrl();
                return this.Redirect(gitHubLoginUrl.ToString());
            }

            var expectedState = stateCookie.Value;
            if (state != expectedState)
            {
                throw new InvalidOperationException("Validation fail!!");
            }

            stateCookie.Expires = DateTime.Now.AddSeconds(-1);
            Response.SetCookie(stateCookie);

            var request = new OauthTokenRequest(CLIENT_ID, CLIENT_SECRET, code) { RedirectUri = new Uri(REDIRECT_URL) };
            var token = await github.Oauth.CreateAccessToken(request);

            var gitHubCookie = new HttpCookie("gitHubOauth") { Value = token.AccessToken };
            Response.SetCookie(gitHubCookie);

            return RedirectToAction(MVC.Home.ActionNames.Index);
        }

        private Uri GetGitHubLoginUrl()
        {
            string state;
            using (var rng = new RNGCryptoServiceProvider())
            {
                var data = new byte[32];
                rng.GetBytes(data);
                state = Convert.ToBase64String(data);
            }

            var stateCookie = new HttpCookie("gitHubState") { Value = state };

            Response.SetCookie(stateCookie);

            /* ********************************************************
             * NOTE: Scope values should be defined in Web.config.
             *       They can be found at:
             *       https://developer.github.com/v3/oauth/#scopes
             * ********************************************************/
            var request = new OauthLoginRequest(CLIENT_ID) { State = state, Scopes = { "user", "read:org" } };
            var loginUrl = github.Oauth.GetGitHubLoginUrl(request);
            return loginUrl;
        }
    }
}