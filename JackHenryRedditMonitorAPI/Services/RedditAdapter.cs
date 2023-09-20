using JackHenryRedditMonitorAPI.Model;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using Reddit.Inputs;
using System.Diagnostics;

namespace JackHenryRedditMonitorAPI.ConfigureAPI
{
    /// <summary>
    /// Adapter which uses a 3rd party API to connect to Reddit and connects a REST API to a live worker.
    /// </summary>
    public static class RedditAdapter
    {
        private static HttpClient _httpClient;
        private static string _appId;
        private static string _appIdSecret;
        private static RedditClient _redditClient;
        private static Dictionary<string, int> Initial = new Dictionary<string, int>();
        private static Dictionary<string, int> UsersToPosts = new Dictionary<string, int>();
        private static Dictionary<string, int> PostToUpvotes = new Dictionary<string, int>();
        private static Dictionary<string, string> NewComments = new Dictionary<string, string>();
        private static ILogger _logger;
        private static Stopwatch sw = new Stopwatch();

        /// <summary>
        /// Generates the authorization token from the app id and app secret from your reddit profile.
        /// (replace in appsettings.json).
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetAuthorizationToken()
        {
            var authenticationString = $"{_appId}:{_appIdSecret}";
            var base64String = Convert.ToBase64String(
               System.Text.Encoding.ASCII.GetBytes(authenticationString));
            HttpRequestMessage message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.Headers.Add("Authorization", "Basic " + base64String);
            message.Headers.Add("User-Agent", "Web.APIPollExample v2.0 (By Rizwan M)");
            message.Headers.Add("Connection", "keep-alive");
            message.Headers.Add("Accept", "*/*");
            sw.Start();

            message.RequestUri = new Uri("https://www.reddit.com/api/v1/access_token?grant_type=client_credentials");
            var res = await _httpClient.SendAsync(message);
            var token = await res.Content.ReadFromJsonAsync<AccessToken>();

            return token.Access_token;
        }

        /// <summary>
        /// Initializes the 3rd party Reddit library and polls the subreddit to populate the initial list.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> InitializeRedditClient(IHttpClientFactory factory, string appId, string secret, string subredditString, ILogger logger)
        {
            _httpClient = factory.CreateClient();
            _appId = appId;
            _appIdSecret = secret;
            _logger = logger;
            var accessToken = await GetAuthorizationToken();

            if (appId == "" || appId == null || _appIdSecret == null || _appIdSecret == "" || secret == "" || accessToken == null)
            {
                Console.WriteLine("appsettings.json configuration required");
                Console.WriteLine("Application stopped...");
                return "";
            }

            try
            {
                _redditClient = new RedditClient(_appId, _appIdSecret, accessToken: accessToken);
                var subreddit = _redditClient.Subreddit(subredditString);
                var today = DateTime.Today;
                Console.WriteLine("## Top Posts for " + today.ToString("D") + Environment.NewLine);

                //real-time Comment monitoring for the subreddit: extra feature
                Console.WriteLine("New comments the and posts they're related to: ");
                subreddit.Comments.GetNew();
                subreddit.Comments.MonitorNew();
                subreddit.Comments.NewUpdated += C_NewCommentsUpdated;

                // Get the top 25 posts from the last 24 hours.
                var posts = subreddit.Posts.GetTop(new TimedCatSrListingInput(t: "day", limit: 25));

                if (posts.Count > 0)
                {
                    foreach (Post post in posts)
                    {
                        if (post.Created >= today && post.Created < today.AddDays(1))
                        {
                            BuildDictionaryFromTopPosts(post, Initial);
                            IncrementOrAddAuthor(post);
                            post.MonitorPostScore();
                            post.PostScoreUpdated += C_PostScoreUpdated;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("There were no new top posts today.");
                }

                subreddit.Posts.GetNew();
                subreddit.Posts.NewUpdated += C_NewPostsUpdated;
                subreddit.Posts.MonitorNew();

                return "Initialization complete";
            }
            catch(Exception e)
            {
                _logger.LogError("Error in Adapter: " + e.Message);
                return "Initialization Failed";
            }
        }

        /// <summary>
        /// Poll Reddit every 30 seconds and calculate upvote and posts since time started.
        /// </summary>
        /// <param name="subredditstring"></param>
        /// <returns></returns>
        public static (Dictionary<string, int>, Dictionary<string, int>) PollReddit(string subredditString)
        {
            if (_redditClient != null)
            {
                //add check here
                try
                {
                    var subreddit = _redditClient.Subreddit(subredditString);
                    var today = DateTime.Today;
                    PostToUpvotes = new Dictionary<string, int>();

                    // Get the top 25 posts from the last 24 hours.
                    var posts = subreddit.Posts.GetTop(new TimedCatSrListingInput(t: "day", limit: 25));

                    if (posts.Count > 0)
                    {
                        foreach (Post post in posts)
                        {
                            if (Initial.ContainsKey(post.Title))
                            {
                                var startingUpVotes = Initial[post.Title];

                                if (post.UpVotes > startingUpVotes)
                                {
                                    PostToUpvotes.Add(post.Title, post.UpVotes - startingUpVotes);
                                }
                            }
                        }
                    }

                    var PostToUpvotesOrdered = PostToUpvotes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    var UsersToPostsOrdered = UsersToPosts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    return (PostToUpvotesOrdered, UsersToPostsOrdered);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error in Reddit polling " + e.Message);
                    return (new Dictionary<string, int>(), new Dictionary<string, int>());
                }
            }
            else
            {
                Console.WriteLine("AppId and Secret have not been configured!");
                Console.WriteLine("Please configure the AppId and secret in the appsettings.json file!");
                Console.WriteLine("App stopping...");
                return (new Dictionary<string, int>(), new Dictionary<string, int>());

            }
        }

        /// <summary>
        /// Function which helps a user grab the current real time information that has been collected since the app started.
        /// </summary>
        /// <returns></returns>
        public static string GetPollInformation()
        {
            if (_redditClient != null)
            {
                var total = 50000 - sw.ElapsedMilliseconds;
                if (sw.ElapsedMilliseconds > 50000)
                {
                    var PostToUpvotesOrdered = PostToUpvotes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    var UsersToPostsOrdered = UsersToPosts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    var json = JsonConvert.SerializeObject((PostsToUpvotes: PostToUpvotesOrdered, UsersToPosts: UsersToPostsOrdered));
                    return json;
                }
                else
                    return "Gathering data... Please try again in " + total / 1000 + " seconds.";
            }
            else
                return "Please configure appsettings.json with valid reddit appId and secret";
        }

        /// <summary>
        /// Event handler for new added posts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void C_NewPostsUpdated(object? sender, PostsUpdateEventArgs e)
        {
            try
            {
                foreach (var post in e.Added)
                {
                    Console.WriteLine("New Post by " + post.Author + ": " + post.Title);

                    if (!Initial.ContainsKey(post.Author))
                        Initial.Add(post.Author, 0);
                    else
                        Initial[post.Author]++;
                }
            }
            catch(Exception err)
            {
                _logger.LogError("Exception in new posts Updated " + err.Message);
            }
        }

        /// <summary>
        /// Event Handler for Post Score updates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void C_PostScoreUpdated(object sender, PostUpdateEventArgs e)
        {
            try
            {
                Initial.TryGetValue(e.NewPost.Title, out int val);
                Initial[e.NewPost.Title] = val + e.NewPost.Score;
            }
            catch(Exception err)
            {
                _logger.LogError("Error in Post Score Updated: " + err.Message);
            }
        }

        /// <summary>
        /// Build dictionary helper function.
        /// </summary>
        /// <param name="post"></param>
        /// <param name="dict"></param>
        private static void BuildDictionaryFromTopPosts(Post post, Dictionary<string, int> dict)
        {
            dict.Add(post.Title, post.UpVotes);
        }

        /// <summary>
        /// Top posts authors and their posts count helper function.
        /// </summary>
        /// <param name="post"></param>
        private static void IncrementOrAddAuthor(Post post)
        {
            if (UsersToPosts.ContainsKey(post.Author))
            {
                UsersToPosts[post.Author]++;
            }
            else
            {
                UsersToPosts.Add(post.Author, 1);
            }
        }

        /// <summary>
        /// Event handler to collect any new comments that get added to any post.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void C_NewCommentsUpdated(object sender, CommentsUpdateEventArgs e)
        {
            foreach (Comment comment in e.Added)
            {
                var post = comment.GetRoot();

                if (!NewComments.ContainsKey(post.Title))
                {
                    Console.WriteLine("Post: " + post.Title + " : " + "Comment: " + comment.Body);
                    NewComments.Add(post.Title, comment.Body);
                }
            }
        }
    }
}
