using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Xml;
using System.Threading.Tasks;

namespace bloodhound
{
    public class MultithreadedRSSGetter
    {
        const int RSS_RESULTS_PAGE_FEED_PAGE_SIZE = 25;
        /// <summary>
        /// Extracts the raw data from craigslist by hitting 
        /// http://vancouver.en.craigslist.ca/search/sss?query=bike+kona+dawg&srchType=A&format=rss
        /// then breaks down the individual links and hits those concurrently parsing out the relevant details
        /// I cannot find a paging paramter for this RSS feed
        /// The start at parameter is &s=75
        /// You only get 25 items on a page and have no clue in the RSS feed of how many matching 
        /// items there are in total. The non rss version of the page has a page count / total 
        /// matching items count
        /// 
        /// See also another piece of software that does this stuff too : 
        /// http://www.zentastic.com/blog/zencash/
        /// 
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="stolenDate"></param>
        /// <param name="itemDescription"></param>
        /// <returns></returns>
        public async Task<CraigslistInfo[]> GetCraigslistInfoByRSS(string cityName, string stolenDate, int pageNumber, string itemDescription)
        {
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(String.Format("http://{0}.en.craigslist.ca/search/sss?query={1}&srchType=A&format=rss&s={2}", cityName, itemDescription, pageNumber * RSS_RESULTS_PAGE_FEED_PAGE_SIZE));

            //
            // Again a bit of fragile code here, but whaddaya gonna do!
            //
            if (xmlDoc.ChildNodes[1].ChildNodes[0].ChildNodes[13].ChildNodes[0].ChildNodes.Count > 0)
            {
                if (xmlDoc.ChildNodes[1].ChildNodes[0].ChildNodes[13].ChildNodes[0].ChildNodes.Count < RSS_RESULTS_PAGE_FEED_PAGE_SIZE)
                {
                    //* Get elements.
                    XmlNodeList items = xmlDoc.ChildNodes[1].ChildNodes[0].ChildNodes[13].ChildNodes[0].ChildNodes;

                    //
                    // This multitaking stuff is a bit of a basard
                    // used this link http://stackoverflow.com/questions/10806951/how-to-limit-the-amount-of-concurrent-async-i-o-operations and also
                    // See http://msdn.microsoft.com/en-gb/library/vstudio/hh300224.aspx    
                    // and especially http://msdn.microsoft.com/en-us/library/hh556530.aspx
                    return await GetAllUrlsAsync(items);
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
                    cockupTooManyresults.Title = String.Format("More than {0} results returned, please be more selective", RSS_RESULTS_PAGE_FEED_PAGE_SIZE);
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
                cockupTooManyresults.DescriptionHTML = "Error";
                cockupTooManyresults.Title = "No results returned, alter your search terms";
                cockupTooManyresults.DodgyScore = 0;
                CraigslistInfo[] results = new CraigslistInfo[1];
                results[0] = cockupTooManyresults;
                return results;
            }
        }

        /// <summary>
        /// And now, the science bit ...
        /// get clever with dot net tasks (new in dot net 4.5)
        /// to concurrently hit the web pages and extract thier data
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task<CraigslistInfo[]> GetAllUrlsAsync(XmlNodeList items)
        {
            //
            // Make a list of web addresses.
            //
            List<string> urlList = new List<string>();

            foreach (XmlNode item in items)
            {
                urlList.Add(item.Attributes[0].Value);
            }

            //
            // Declare an HttpClient object and increase the buffer size. The 
            // default buffer size is 65,536.
            //
            HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };

            //
            // Create a query that will process all the web gets simulatneously
            //
            IEnumerable<Task<CraigslistInfo>> downloadTasksQuery =
                from url in urlList select CraigslistInfo.ProcessURL(url, client);

            //
            // Use ToArray to execute the query and start the download tasks.
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

    }
}