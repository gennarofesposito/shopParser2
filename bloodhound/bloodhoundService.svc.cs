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
        /// this is part of the doginess check. If a stock image is used then it's dodgier
        /// </summary>
        /// <param name="craigslistitemId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetTinEyePropertiesForImagesInArticleCollection(string craigslistitemId, Stream data)
        {
            /// Geno to here
            return "not yet implemented";
        }

        

        #endregion
    }
}
