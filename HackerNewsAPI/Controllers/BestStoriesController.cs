using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using HackerNewsAPI.Models;

namespace HackerNewsAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/BestStories")]
    public class BestStoriesController : Controller
    {
        private IMemoryCache _cache;
        private const int cacheExpirationMin = 4;
        private const string HNBestStoriesURL = "https://hacker-news.firebaseio.com/v0/beststories.json";
        private const string HNStoryDetailsURL = "https://hacker-news.firebaseio.com/v0/item/";

        public BestStoriesController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        /// <summary>
        /// Gets the list of story IDs from the HackerNews API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<BestStoriesModel>> Get()
        {
            // Setup variables
            string respString;
            List<int> stories;

            // Attempt to get the cached object for storyIDs, else get the best story IDs and store in cache
            if (!_cache.TryGetValue("storyIDs", out stories))
            {
                // Get the IDs from the Hacker News API
                // Now get the IDs from the API
                using (HttpClient httpClient = new HttpClient())
                {
                    // Await the response from the cient API
                    HttpResponseMessage response = await httpClient.GetAsync(HNBestStoriesURL);

                    if (response.IsSuccessStatusCode)
                    {
                        respString = await response.Content.ReadAsStringAsync();
                        stories = JsonConvert.DeserializeObject<List<int>>(respString);

                        // Set cache expiration and store the data
                        var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(cacheExpirationMin));
                        _cache.Set("storyIDs", stories, cacheOptions);
                    }
                }
            }

            // Now attempt to get the best story details
            if (stories.Count > 0)
            {
                // Setup variables
                List<BestStoriesModel> bestStories;

                // Attempt to get the cached best stories details, else we build it and store it in cache
                if (!_cache.TryGetValue("bestStories", out bestStories))
                {
                    // Setup our best stories list object
                    bestStories = new List<BestStoriesModel>();

                    // Loop through our story IDs and generate a list of best stories          
                    using (HttpClient httpClient = new HttpClient())
                    {
                        foreach (var storyID in stories)
                        {
                            // Await the response from the cient API
                            HttpResponseMessage response = await httpClient.GetAsync(HNStoryDetailsURL + storyID + ".json");

                            if (response.IsSuccessStatusCode)
                            {
                                var respData = await response.Content.ReadAsStringAsync();
                                var story = JsonConvert.DeserializeObject<HNStoryModel>(respData);
                                var hnstory = new BestStoriesModel();
                                hnstory.author = story.by;
                                hnstory.title = story.title;
                                bestStories.Add(hnstory);
                            }
                        }
                    }
                }
                return bestStories;
            }
            else
            {
                return new List<BestStoriesModel>();
            }
        }
    }
}
