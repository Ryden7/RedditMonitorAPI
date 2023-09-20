# RedditMonitorAPI

Subreddit Monitor Web API created by Rizwan Mohammed to monitor any subreddit (defaulted to "funny" for boiler plate demo). The app consumes the top 25 posts from the funny subreddit in near real time using the Reddit 3rd party nuget package and filters it based off of the DateTime the application was started. It keeps track of the following statistics between the time the application starts until it ends:

Posts with most up votes

Users with most posts

# Usage

The application MUST have a valid appId and secret from your reddit account! Those credentials need to be input into the appsettings.json for both the application and tests to run as expected.

# How it works

Once a correct and valid set of appId and secret are passed to appsettings.json, the appication can be started.

Once the application starts. it will open up an elegant interactive Swagger API which will allow you to get the posts with the most upvotes and users with the posts posts since the application was started.

NOTE: The application takes about 45 seconds to collect REAL TIME data about changes in votes and displays them to you. You will be allowed to complete the API call and see the data once the data has been gathered. The API is designed to guide you through the process. 

The data is presented to you in a json Tuple Dictionary for easy consumption and human readability. The first Dictionary (Item1) representing the Posts and their titles with the most upvotes and the second Dictionary (Item2) representing the authors and their post count.

Real time data is gathered and refreshed every 45 seconds. 


# Created by: 
# Rizwan Mohammed
# 09/19/2023
