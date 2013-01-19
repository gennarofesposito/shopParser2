using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace bloodhound
{
    public class MultithreadHTMLGetter
    {
        const int HTML_RESULTS_PAGE_FEED_PAGE_SIZE = 100;
        const int MAX_RESULTS_WARN = 50;
        /// <summary>
        /// <summary>
        /// Extracts the raw data from craigslist by hitting 
        /// http://vancouver.en.craigslist.ca/search/sss?query=bike+kona+dawg&srchType=A
        /// then breaks down the individual links and hits those concurrently parsing out the relevant details
        /// We use the HTmll page not the RSS feed since the RSS feed doe not do paging correctly
        /// which makes things crap
        /// The start at parameter is &s=75
        /// 
        /// See also another piece of software that does this stuff too : 
        /// http://www.zentastic.com/blog/zencash/
        ///         /// </summary>
        /// <param name="cityName"></param>
        /// <param name="stolenDate"></param>
        /// <param name="pageNumber"></param>
        /// <param name="itemDescription"></param>
        /// <returns></returns>
        public async Task<CraigslistInfo[]> GetCraigslistInfo(string cityName, string stolenDate, int pageNumber, string itemDescription)
        {
            List<CraigslistInfo> listings = new List<CraigslistInfo>();
            //
            // The search result page is available in RSS, but that page does not have paging so try to use the HTML 
            // version instead (much harder to parse than the nice XML you get from RSS feed)
            // to make this easier use a special gizmo called the HtmlAgilityPack that parses the 
            // HTMl into a document tree (and fixes errors etc in the HTML) - this allows us to traverse the 
            // page strucvture safely
            //
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            //
            // There are various options, set as needed
            // Here I just want any errors tidied up to make a valid Document Tree so we can parse with confidence
            // Note: We get this first URl synchronously, but call up the fine detail for each item within it 
            // by calling it's details URL asynchronoursly (and concurrently) for speed purposes
            //
            htmlDoc.OptionFixNestedTags = true;
            System.Net.WebRequest webReq = System.Net.WebRequest.Create(String.Format("http://{0}.en.craigslist.ca/search/sss?query={1}&srchType=A&s={2}", cityName, itemDescription, pageNumber * HTML_RESULTS_PAGE_FEED_PAGE_SIZE));
            System.Net.WebResponse webRes = webReq.GetResponse();
            System.IO.Stream mystream = webRes.GetResponseStream();
            if (mystream != null)
            {
                htmlDoc.Load(mystream);
            }
            //
            // Use XPAth queries (see http://www.w3schools.com/xpath/xpath_syntax.asp)
            // to parse the DOm the HtmlAgilityPack has made us
            //
            //HtmlAgilityPack.HtmlNode pageCountNode = htmlDoc.DocumentNode.SelectSingleNode("//h4/b[2]");
            //string searchMatchCount = pageCountNode.InnerText.Substring(pageCountNode.InnerText.IndexOf("Found: "),pageCountNode.InnerText.IndexOf(" Displaying:") - pageCountNode.InnerText.IndexOf("Found: "));

            //
            // Get the inodvidual items for sales
            //
            HtmlAgilityPack.HtmlNodeCollection itemNodes = htmlDoc.DocumentNode.SelectNodes("//p[@class='row']");
            if (itemNodes != null)
            {
                if (itemNodes.Count < MAX_RESULTS_WARN)
                {
                    foreach (HtmlAgilityPack.HtmlNode item in itemNodes)
                    {
                        CraigslistInfo craigslistInfo = CraigslistInfo.transformSummaryHTMLNodeIntoCraigslistInfo(item);
                        listings.Add(craigslistInfo);
                    }
                }
                else
                {
                    //
                    // Break all the rest rules - which say the error message should be in the statusText header
                    // see: http://stackoverflow.com/questions/1077340/best-way-to-return-error-messages-on-rest-services
                    // instead do it wrong and return an error disguised as the object
                    //
                    CraigslistInfo cockupTooManyresults = new CraigslistInfo();
                    cockupTooManyresults.DescriptionHTML = "Error";
                    cockupTooManyresults.Title = String.Format("More than {0} results returned, please be more selective", MAX_RESULTS_WARN);
                    cockupTooManyresults.DodgyScore = 0;
                    CraigslistInfo[] results = new CraigslistInfo[1];
                    results[0] = cockupTooManyresults;
                    return results;
                }
            }
            else
            {
                //
                // Break all the rest rules - which say the error message should be in the statusText header
                // see: http://stackoverflow.com/questions/1077340/best-way-to-return-error-messages-on-rest-services
                // instead do it wrong and return an error disguised as the object
                // Oh yeah and breech the Dont reeat Yoursefl principle too!
                //
                CraigslistInfo cockupTooManyresults = new CraigslistInfo();
                cockupTooManyresults.DescriptionHTML = "";
                cockupTooManyresults.Title = "No results returned, alter your search terms";
                cockupTooManyresults.DodgyScore = 0;
                CraigslistInfo[] results = new CraigslistInfo[1];
                results[0] = cockupTooManyresults;
                return results;
            }
            return await UpdateAllListItemsWithDetailsFromTheirDetailPageAsync(listings);
        }



        /// <summary>
        /// And now, the science bit ...
        /// get clever with dot net tasks (new in dot net 4.5)
        /// to concurrently hit the web pages and extract thier data
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task<CraigslistInfo[]> UpdateAllListItemsWithDetailsFromTheirDetailPageAsync(List<CraigslistInfo> listings)
        {
            //
            // Declare an HttpClient object and increase the buffer size. The 
            // default buffer size is 65,536.
            //
            HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };

            //
            // Create a query that will process all the web gets simulatneously
            //
            IEnumerable<Task<CraigslistInfo>> downloadTasksQuery =
                from item in listings select ProcessListing(item, client);

            //
            // Use ToArray tranform out of bing a list.
            //
            Task<CraigslistInfo>[] downloadTasks = downloadTasksQuery.ToArray();

            //
            // You can do other work here before awaiting. 
            //

            //
            // Await the completion of all the running concurrent tasks. 
            //
            return await Task.WhenAll(downloadTasks);
        }



        /// <summary>
        /// The actions from the foreach loop are moved to this async method.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        async Task<CraigslistInfo> ProcessListing(CraigslistInfo currentListing, HttpClient client)
        {
            //
            // The detail page is not available in RSS, so have to parse the HTML for it (much hard than the ice XML you get frmo RSS feed)
            // to kae this easier use a special gizmo called the HTMl agility pack that parses the 
            // HTMl into a document tree (and fixes errors etc in the HTMl) - this allows us to traverse the page safely
            //
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            //
            // There are various options, set as needed
            // Here I just want any errors tidied up to make a valid Docuemtn Tree so we can parse with confidence
            //
            htmlDoc.OptionFixNestedTags = true;
            htmlDoc.Load((await (client.GetStreamAsync(currentListing.LinkURL))), true);
            //
            // Now lots of magic to parse the data from all the HTML
            //
            currentListing.updateCraigslistInfoFromFullItemDetailsPage(htmlDoc, currentListing.LinkURL);
            return currentListing;
        }
    }
}