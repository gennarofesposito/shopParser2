
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace bloodhound
{
    public class MultithreadedTinEyeGetter
    {
        const int MAX_RESULTS_WARN = 1;
        /// <summary>
        /// <summary>
        /// Extracts the raw data from tineye by hitting 
        /// URL=http://tineye.com/search
        /// with each URL of an image of a craigslist item
        /// We pass in a collection these - but we expect only one lot of images to be inspected for now - so as not to annoy TinEye by 
        /// scraping their site - they have a paid for API so will be monitoring scrapes
        /// 
        public async Task<List<TinEyeInfo>> GetTinEyeInfo(List<TinEyeInfo> tinEyeInfos)
        {
            return await lookupImagesInTinEye(tinEyeInfos);

        }


        /// <summary>
        /// And now, the science bit ...
        /// get clever with dot net tasks (new in dot net 4.5)
        /// to concurrently hit the web pages and extract thier data
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task<List<TinEyeInfo>> lookupImagesInTinEye(List<TinEyeInfo> tinEyeInfos)
        {
            //
            // Declare an HttpClient object and increase the buffer size. The 
            // default buffer size is 65,536.
            //
            HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };

            client.BaseAddress = new Uri("http://tineye.com/search");
            client.DefaultRequestHeaders.Host = "tineye.com";
            client.DefaultRequestHeaders.Referrer = new Uri("http://tineye.com/");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MOZILLA", "5.0"));
            
            //
            // I really want to set the content-type, but cannot find how to do it - am assuming it gets set autmoatically
            //
            
            //client.DefaultRequestHeaders.Add("Content-Type","application/x-www-form-urlencoded");
            
            //
            // Create a query that will process all the web gets simulatneously
            //
            foreach (TinEyeInfo tinEyeInfo in tinEyeInfos)
            {
                IEnumerable<Task<TinEyeImageInfo>> downloadTasksQuery =
                    from item in tinEyeInfo.TinEyeImageInfos select ProcessListing(item, client);

                //
                // Use ToArray tranform out of being a list.
                //
                Task<TinEyeImageInfo>[] downloadTasks = downloadTasksQuery.ToArray();

                //
                // You can do other work here before awaiting. 
                //

                //
                // Await the completion of all the running concurrent tasks. 
                //
                //return await Task.WhenAll(downloadTasks);
                await Task.WhenAll(downloadTasks);
            }
            return tinEyeInfos;
        }





        /// <summary>
        /// The actions from the foreach loop are moved to this async method.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        async Task<TinEyeImageInfo> ProcessListing(TinEyeImageInfo tinEyeImageInfo, HttpClient client)
        {

            
            StringContent postData = new StringContent(String.Format("url={0}",tinEyeImageInfo.ImageURI), Encoding.UTF8, "application/x-www-form-urlencoded");
            var result = await client.PostAsync("http://tineye.com/search", postData);
            if (result.IsSuccessStatusCode)
            {
                //
                // Parse the HTML and extract the image count and us it to update the tinEyeImageInfo.TinEyeImageCount property
                //
                // Geno to here : just started debugging this tin eye stuff to get it working boradly
                // will then need to get the regexs right to pull out the tineye count
                // something like :
                //         its in their page in the format :  <h2><span>0</span> Results</h2>
                //         HtmlAgilityPack.HtmlNodeCollection itemNodes = htmlDoc.DocumentNode.SelectNodes("/h2/span[1]']");
                // only I scrapped the HTMlAgailitypack in this calss as having trouble matching it's input types and the return tpye of the POST I am making
                //
                string x = await result.Content.ReadAsStringAsync();
            }
            
            return tinEyeImageInfo;
        }
    }
}
