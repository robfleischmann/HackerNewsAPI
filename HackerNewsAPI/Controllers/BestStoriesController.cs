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
            List<BestStoriesModel> bestStories;

            // Setup cache expiration
            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(cacheExpirationMin));

            // Now check if we have cached best stories, else pull them from the HackerNewsAPI
            if (!_cache.TryGetValue("bestStories", out bestStories))
            {
                // First get the story IDs
                using (HttpClient httpClient = new HttpClient())
                {
                    // Await the response from the cient API
                    HttpResponseMessage response = await httpClient.GetAsync(HNBestStoriesURL);

                    if (response.IsSuccessStatusCode)
                    {
                        respString = await response.Content.ReadAsStringAsync();
                        stories = JsonConvert.DeserializeObject<List<int>>(respString);
                    }
                    else
                    {
                        // Return an empty value
                        return new List<BestStoriesModel>();
                    }
                }

                // If we successfully obtained story IDs, we get their author and title, else return empty set
                if (stories.Count > 0)
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
                            else
                            {
                                // Return an empty value
                                return new List<BestStoriesModel>();
                            }
                        }
                    }
                    // Now cache the stories
                    _cache.Set("bestStories", bestStories, cacheOptions);
                }
                else
                {
                    // Return an empty value
                    return new List<BestStoriesModel>();
                }
            }
            return bestStories;
        }
    }
}
