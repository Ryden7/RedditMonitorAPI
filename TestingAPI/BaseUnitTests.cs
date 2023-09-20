using JackHenryRedditMonitorAPI.ConfigureAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestingAPI
{
    [TestClass]
    public class BaseUnitTests
    {
        /// <summary>
        /// Appsettings configuration.
        /// (prefill with your reddit appId and secret).
        /// </summary>
        /// <returns></returns>
        public IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }

        /// <summary>
        /// Test for Successful AppID and Secret.
        /// If this test fails, the appsettings need to be configured. Please configure both appsettings.json files with reddit appid and secret.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task EmptyAppIdOrSecretTest()
        {
            var config = InitConfiguration();
            var AppId = config["MySettings:AppId"];
            var AppSecret = config["MySettings:AppSecret"];

            Assert.AreNotEqual("", AppId);
            Assert.AreNotEqual("", AppSecret);
        }

        /// <summary>
        /// Test for Successful 3rd party client initialization.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InitializeRedditClientSuccess()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var Logger = new Mock<ILogger>();

            var client = new HttpClient();
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
            IHttpClientFactory factory = mockFactory.Object;
            ILogger log = Logger.Object;

            var config = InitConfiguration();
            var AppId = config["MySettings:AppId"];
            var AppSecret = config["MySettings:AppSecret"];

            var str = await RedditAdapter.InitializeRedditClient(factory, AppId, AppSecret, "funny", log);
            Assert.AreEqual(str, "Initialization complete");
        }

        /// <summary>
        /// Test to check if appsettings.json error response is sent back properly.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InitializeRedditClientFailure()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var Logger = new Mock<ILogger>();
            var client = new HttpClient();
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
            IHttpClientFactory factory = mockFactory.Object;
            ILogger log = Logger.Object;

            var str = await RedditAdapter.InitializeRedditClient(factory, "", null, "funny", log) ;
            Assert.AreEqual(str, "appsettings.json configuration required");
        }
    }
}