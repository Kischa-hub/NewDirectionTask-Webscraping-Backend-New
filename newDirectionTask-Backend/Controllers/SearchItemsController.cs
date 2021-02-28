using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using newDirectionTask_Backend.Models;

namespace newDirectionTask_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchItemsController : ControllerBase
    {
        private readonly SearchContext _context;

        public SearchItemsController(SearchContext context)
        {
            _context = context;
        }

        // GET: api/SearchItems
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<SearchItem>>> GetSearchItems()
        //{
        //    return await _context.SearchItems.ToListAsync();
        //}

        // GET: api/SearchItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SearchItem>> GetSearchItem(Guid id)
        {
            var searchItem = await _context.SearchItems.FindAsync(id);

            if (searchItem == null)
            {
                return NotFound();
            }

            return searchItem;
        }

        // PUT: api/SearchItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSearchItem(Guid id, SearchItem searchItem)
        {
            if (id != searchItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(searchItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SearchItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SearchItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SearchItem>> PostSearchItem(SearchItem searchItem)
        {
            _context.SearchItems.Add(searchItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSearchItem", new { id = searchItem.Id }, searchItem);
        }

        // DELETE: api/SearchItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSearchItem(Guid id)
        {
            var searchItem = await _context.SearchItems.FindAsync(id);
            if (searchItem == null)
            {
                return NotFound();
            }

            _context.SearchItems.Remove(searchItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SearchItemExists(Guid id)
        {
            return _context.SearchItems.Any(e => e.Id == id);
        }

        //*********************************************************************************
        //*********************************************************************************
        //**********************************TASK WEBSCRAPING****************************
        //*********************************************************************************
        //*********************************************************************************

        //test webscraping 
        [HttpPost("/api/pageprocessor/")]
        public async Task<ActionResult> GetLinksdoc(string url, string keyword)
        {
            //check if the Url is null or empty
            if (string.IsNullOrWhiteSpace(url))
            {

                return BadRequest();
            }

            var doc = await DownloadWebData(url);
            HashSet<string> linkList = GetLinkList(doc, url);
            int count = await CountWordsinAll(linkList, keyword);
            //Create Model
            SearchItem searchItem = new SearchItem
            {
                Id = Guid.NewGuid(),
                Count = count,
                Date = DateTime.Now.Date,
                Keyword = keyword,
                Url = url,
            };

            _context.SearchItems.Add(searchItem);
            await _context.SaveChangesAsync();

            return Ok(searchItem); ;
        }

        [HttpGet("/api/pageprocessor/items")]
        public async Task<ActionResult> GetSearchItems()
        {
            var seachItems = await _context.SearchItems.OrderByDescending(c => c.Date).ToListAsync();
            return Ok(seachItems);
        }

        //take Uri and load all the data in this uri then return htmldocument
        private Task<HtmlDocument> DownloadWebData(string url)
        {

            var web = new HtmlWeb();
            return web.LoadFromWebAsync(url);
        }

        //method take the htmldocument and the base Url and return a list with unique uri 
        private HashSet<string> GetLinkList(HtmlDocument doc, string url)
        {

            Uri baseUri = new Uri(url);
            HashSet<string> list = new HashSet<string>();

            //select all the href in the htmldocument and but them in htmlnodecollection
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");

            //loop through all nodes 
            foreach (var node in nodes)
            {
                string href = GetUrl(baseUri, node);
                if (!string.IsNullOrWhiteSpace(href))
                    list.Add(href);
            }

            return list;
        }

        //making Absolute Uri
        private static string GetUrl(Uri baseUri, HtmlNode node)
        {
            try
            {
                string href = node.Attributes["href"].Value;
                if (!href.StartsWith("http"))
                {
                    Uri childuri = new Uri(baseUri, href);
                    href = childuri.AbsoluteUri;
                }

                return href;
            }
            catch
            {
                return string.Empty;
            }
        }

        //this function count the spacific word in the Webpage 
        public async Task<int> CountWordInUri(string uri, string keyword)
        {
            HtmlDocument doc = await new HtmlWeb().LoadFromWebAsync(uri);
            var nodes = doc.DocumentNode.SelectNodes($"//text()[contains(., '{keyword}')]/..");
            return nodes == null ? 0 : nodes.Count;
        }

        //this function count the word in the website and all the subpages
        public async Task<int> CountWordsinAll(HashSet<string> alllinks, string keyword)
        {
            var stopwatch  = System.Diagnostics.Stopwatch.StartNew();
                       var count = 0;
            var links = alllinks
                .Where(c => c.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                   c.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase));

            List<string> urls = links.ToList();
            int nextIndex = 0;

            var getWordCountTasks = new List<Task<int>>();
            foreach (var item in urls)
            {
                getWordCountTasks.Add(CountWordInUri(item, keyword));
            }
            var counts = await Task.WhenAll(getWordCountTasks);
            count = counts.Sum();


            //Fill First n

            //const int CONCURRENCY_LEVEL = 10;
            //
            //while (nextIndex < CONCURRENCY_LEVEL && nextIndex < urls.Count)
            //{
            //    getWordCountTasks.Add(CountWordInUri(urls[nextIndex], keyword));
            //    nextIndex++;
            //}

            //while (getWordCountTasks.Count > 0)
            //{
            //    try
            //    {
            //        Task<int> getWordCountTask = await Task.WhenAny(getWordCountTasks);
            //        getWordCountTasks.Remove(getWordCountTask);

            //        int wordCount = await getWordCountTask;
            //        count += wordCount;
            //    }
            //    catch (Exception exc) { }

            //    if (nextIndex < urls.Count)
            //    {
            //        getWordCountTasks.Add(CountWordInUri(urls[nextIndex], keyword));
            //        nextIndex++;
            //    }
            //}
            stopwatch.Stop();
            var ministop = stopwatch.ElapsedMilliseconds;
            return count;
        }



    }
}
