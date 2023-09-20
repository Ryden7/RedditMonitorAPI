using JackHenryRedditMonitorAPI.ConfigureAPI;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

ConfigureRedditClient(builder.Configuration["MySettings:AppId"], builder.Configuration["MySettings:AppSecret"]);

app.Run();

///Configure worker Client with 3rd party on program start
async void ConfigureRedditClient(string AppId, string AppSecret)
{
    //Extensibility: prompt user for the Subreddit here, default to funny for now...
    var subreddit = "funny";
    //var subreddit = Console.ReadLine();

    //Dependency Injection
    var IHttpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Adapter logger");

    //Initialize worker client
    await RedditAdapter.InitializeRedditClient(IHttpClientFactory, AppId, AppSecret, subreddit, logger);

    //No websockets... So poll instead! using new .NET 6 PeriodicTimer.
    var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(45));
    while (await periodicTimer.WaitForNextTickAsync())
    {
        // Poll reddit every 45 (optimal time) function
        RedditAdapter.PollReddit(subreddit);
    }
}
