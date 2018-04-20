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
        /// Get's the IDs of the best stories from Hacker News API
        /// </summary>
        /// <returns></returns>
        public async Task<List<int>> getBestStoryIDs()
        {
            // Setup variables
            string respString;
            List<int> stories;

            // Setup cache expiration
            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(cacheExpirationMin));

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

                        // Cache the data
                        _cache.Set("storyIDs", stories, cacheOptions);
                    }
                }
            }
            return stories;
        }

        /// <summary>
        /// Get's the story details from the list of story IDs
        /// </summary>
        /// <param name="stories"></param>
        /// <returns></returns>
        public async Task<List<BestStoriesModel>> getBestStories(List<int> stories)
        {
            // Setup variables
            List<BestStoriesModel> bestStories;

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
            return bestStories;
        }

        /// <summary>
        /// Gets the list of story IDs from the HackerNews API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<BestStoriesModel>> Get()
        {
            // First get the story IDs
            var stories = await getBestStoryIDs();

            // Now attempt to get the best story details
            if (stories.Count > 0)
            {
                var bestStories = await getBestStories(stories);
                return bestStories;
            }
            else
            {
                return new List<BestStoriesModel>();
            }
        }
    }
}
