using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Globalization;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.IO;

namespace bloodhound
{
    //
    // Added this AspNetCompatibilityRequirements stuff from http://forums.silverlight.net/t/21944.aspx
    // since the adding of 
    // <serviceHostingEnvironment multipleSiteBindingsEnabled="true" aspNetCompatibilityEnabled="true"/>
    // (as recommended in the excellent http://social.technet.microsoft.com/wiki/contents/articles/hosting-a-wcf-rest-service-on-iis.aspx ) 
    // to the web config still seemed to generate the error about compatability
    //
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class bloodhoundService : IbloodhoundService
    {


        #region IbloodhoundService Members
        const string IMAGESEPERATOR = "XKRXKRXKR";
        const int DEFAULT_IMAGE_COUNT = 0;
        const string TINEYE_IMAGE_URL_STUB = "http://images.craigslist.org/";
        /// <summary>
        /// Search craislist site for mathing items
        /// </summary>
        /// <param name="city"></param>
        /// <param name="stolenDate"></param>
        /// <param name="pageNumber"></param>
        /// <param name="itemDescription"></param>
        /// <returns></returns>
        public async Task<CraigslistInfo[]> GetCraigslistItems(string city, string stolenDate,string pageNumber, string itemDescription)
        {

          MultithreadHTMLGetter stuff = new MultithreadHTMLGetter();
          return await stuff.GetCraigslistInfo(city, stolenDate, Convert.ToInt32(pageNumber), itemDescription);
        }

        /// <summary>
        /// Search craigslist RSS feed for matching ietms
        /// </summary>
        /// <param name="city"></param>
        /// <param name="stolenDate"></param>
        /// <param name="pageNumber"></param>
        /// <param name="itemDescription"></param>
        /// <returns></returns>
        public async Task<CraigslistInfo[]> GetCraigslistItemsByRSS(string city, string stolenDate, string pageNumber, string itemDescription)
        {

            MultithreadedRSSGetter stuff = new MultithreadedRSSGetter();
            return await stuff.GetCraigslistInfoByRSS(city, stolenDate, Convert.ToInt32(pageNumber), itemDescription);
        }

        /// <summary>
        /// Check the images associated with a sraigslit article in tineye to see if they are original or are 'stock' images from elsewhere on the web
        /// this is part of the dodginess check. If a stock image is used then it's dodgier
        /// </summary>
        /// <param name="craigsListItemId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<TinEyeInfo>> GetTinEyePropertiesForImagesInArticleCollection(string craigsListItemId, string imagesCollection)
        {
            //
            // Convert the incoming text from the querystring into a collection objects and pass into the multithreaded getters
            //
            string[] images = imagesCollection.Split(new string[] { IMAGESEPERATOR }, StringSplitOptions.None);
            List<TinEyeInfo> tinEyeInfos = new List<TinEyeInfo>();
            TinEyeInfo tinEyeInfo = new TinEyeInfo();
            tinEyeInfo.CraigslistInfoId = craigsListItemId;
            foreach ( string image in images) {
                TinEyeImageInfo tinEyeImageInfo = new TinEyeImageInfo();
                tinEyeImageInfo.ImageURI = TINEYE_IMAGE_URL_STUB + image;
                tinEyeImageInfo.TinEyeImageCount = DEFAULT_IMAGE_COUNT;
                tinEyeInfo.TinEyeImageInfos.Add(tinEyeImageInfo);
            }
            tinEyeInfos.Add(tinEyeInfo);
            MultithreadedTinEyeGetter tinEye = new MultithreadedTinEyeGetter();
            return await tinEye.GetTinEyeInfo(tinEyeInfos);
            //return "not yet implemented";
        }

        

        #endregion
    }
}
