using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;


namespace bloodhound
{
[ServiceContract]
public interface IbloodhoundService
{
    [OperationContract]
    [WebGet(UriTemplate = "getCraigslistItems/{city}/{stolenDate}/{pageNumber}/{itemDescription}", ResponseFormat = WebMessageFormat.Json)]
    Task<CraigslistInfo[]> GetCraigslistItems(string city, string stolenDate, string pageNumber, string itemDescription);
    [WebGet(UriTemplate = "getCraigslistItemsByRSS/{city}/{stolenDate}/{pageNumber}/{itemDescription}", ResponseFormat = WebMessageFormat.Json)]
    Task<CraigslistInfo[]> GetCraigslistItemsByRSS(string city, string stolenDate, string pageNumber, string itemDescription);
    [WebGet(UriTemplate = "getTinEyePropertiesForImagesInArticleCollection/{craigsListItemId}/{imagesCollection}", ResponseFormat = WebMessageFormat.Json)]
    Task<List<TinEyeInfo>> GetTinEyePropertiesForImagesInArticleCollection(string craigsListItemId, string imagesCollection);
}



    [DataContract(Namespace = "", Name = "tinEyeInfo")]
    public class TinEyeInfo
    {
        [DataMember(Name = "craigslistInfoId", Order = 0)]
        public string CraigslistInfoId;
        [DataMember(Name = "tinEyeImageInfos", Order = 1)]
        public List<TinEyeImageInfo> TinEyeImageInfos;

        public TinEyeInfo()
        {
            this.TinEyeImageInfos = new List<TinEyeImageInfo>();
        }
    }

    [DataContract(Namespace = "", Name = "tinEyeImageInfo")]
    public class TinEyeImageInfo
    {
        [DataMember(Name = "imageURI", Order = 0)]
        public string ImageURI;
        [DataMember(Name = "tinEyeImageCount", Order = 1)]
        public int TinEyeImageCount;
    }

    [DataContract(Namespace = "", Name = "craigslistInfo")]
    public class CraigslistInfo
    {
        const int DODGY_POINTS_IMAGE_MAXIMUM = 500;
        const int DODGY_POINTS_IMAGE_ITEM_REDUCTION_VALUE = -50;
        const int DODGY_POINTS_WORD_MAXIMUM = 500;
        const int DODGY_POINTS_WORD_ITEM_REDUCTION_VALUE = -1;
        const int LENGTH_OF_TEL_NUMBER = 12;
        const string VANCOUVER_TEL_CODE1 = "604";
        const string VANCOUVER_TEL_CODE2 = "708";
        const string DEFAULT_TEL_NUMBER = "12345678";

        [DataMember(Name = "linkURL", Order = 0)]
        public string LinkURL;
        [DataMember(Name = "decriptionHTML", Order = 1)]
        public string DescriptionHTML;
        [DataMember(Name = "datePosted", Order = 2)]
        public string DatePosted;
        [DataMember(Name = "images", Order = 3)]
        public List<string> Images;
        [DataMember(Name = "title", Order = 4)]
        public string Title;
        [DataMember(Name = "dodgyScore", Order = 5)]
        public int DodgyScore;
        [DataMember(Name = "datePostedReal", Order = 6)] 
        public DateTime DatePostedReal;        
        [DataMember(Name = "phoneNumber", Order = 7)]
        public string PhoneNumber;
        [DataMember(Name = "price", Order = 8)]
        public string Price;
        [DataMember(Name = "saleArea", Order = 9)]
        public string SaleArea;
        [DataMember(Name = "shortTitle", Order = 10)]
        public string ShortTitle;
        [DataMember(Name = "category", Order = 11)]
        public string Category;
        [DataMember(Name = "datePostedParsed", Order = 12)]
        public string DatePostedParsed;
        [DataMember(Name = "dodgyDescription", Order = 13)]
        public string DodgyDesciption;

        public void updateCraigslistInfoFromFullItemDetailsPage(HtmlAgilityPack.HtmlDocument htmlDoc, string url)
        {
            HtmlAgilityPack.HtmlNode time = htmlDoc.DocumentNode.SelectSingleNode("//time");
            HtmlAgilityPack.HtmlNode title = htmlDoc.DocumentNode.SelectSingleNode("//h2");
            //
            // Userbdy section has javascript with all the images on it and the descripion etc - the main chunk of data for the listing in other words
            // The userbody section used to be delineated by a <section id="userbody"> ... </section>
            //
            //HtmlAgilityPack.HtmlNode bodyElement = htmlDoc.GetElementbyId("userbody");
            //
            // but it changes Jan 2013 to be <sction class="userbody"> ... </section>
            //
            HtmlAgilityPack.HtmlNode bodyElement = htmlDoc.DocumentNode.SelectSingleNode("//section[@class='userbody']");

            
            //
            // magic here to trim down the HTML, break it up, analyze it etc
            // you can see example HTMl for the pages at URLs like this
            // view-source:http://vancouver.en.craigslist.ca/van/bik/3436265260.html  - no images
            // view-source:http://vancouver.en.craigslist.ca/rds/bik/3451242524.html  - got images
            //

            //
            // really pikey bit of brittle code here - but had some real difficulty parsing the image 
            // URL out of the img tags using the agility pack. Kept getting null pointers and gave up after a couple of hours.
            // TODO: ervert back ToString parsing the <img tags as iterator's a better approach
            //
            if (bodyElement.InnerText.Contains("imgList ="))
            {
                string scriptText = bodyElement.InnerText.Substring(bodyElement.InnerText.IndexOf("imgList =")).Replace("\"", "").Replace("\n", String.Empty).Replace("\r", String.Empty);
                string[] images = scriptText.Substring(scriptText.IndexOf('[') + 1, (scriptText.IndexOf(']') - scriptText.IndexOf('[')) - 1).Split(',');
                this.Images = images.ToList();
            }
            //
            // title is on the format
            //    <h2 class="postingtitle">2008 Kona Dawg - $1200 (Delta, BC)</h2>
            //
            this.Title = title.InnerText;
            this.LinkURL = url;

            if (time != null)
            {
                this.DatePosted = time.InnerText;
            }
            this.DescriptionHTML = bodyElement.InnerText.Replace("\t", String.Empty).Replace("\n", String.Empty); ;
            this.PhoneNumber = this.extractPhoneNumber(bodyElement);
            this.calculateDodginessScore(bodyElement);
        }

        /// <summary>
        /// Parses data from an HTML node for a listing items to populate most of the data into a craigslistInfo object
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static CraigslistInfo transformSummaryHTMLNodeIntoCraigslistInfo(HtmlAgilityPack.HtmlNode item)
        {
            CraigslistInfo craigslistInfo = new CraigslistInfo();            
 
            //
            // the node is a block of HTml that looks like this
            //<p class="row" data-latitude="" data-longitude="">
            //            <span class="ih" id="images:3Ee3K43m25Ne5K95Hecbmc8b000ac1a0e1376.jpg">&nbsp;</span>
            //            <span class="itemdate"> Dec 11</span>
            //            <span class="itemsep"> - </span>
            //            <a href="http://vancouver.en.craigslist.ca/pml/ctd/3427888027.html">2003 HYUNDAI TIBURON GT</a>
            //            <span class="itemsep"> - </span>
            //            <span class="itemph"></span>
            //            <span class="itempp"> $6595</span>
            //            <span class="itempn"><font size="-1"> (MISSION)</font></span>
            //            <span class="itemcg" title="ctd"> <small class="gc"><a href="/ctd/">cars & trucks - by dealer</a></small></span>
            //            <span class="itempx"> <span class="p"> pic</span></span>
            //            <br class="c">
            //        </p>
            //
            // First of all parse the date : Dec 11
            //
            // .:TODO:. this is not working and throwing the exception every time - and using the output in jscript breaks the page!
            //
            string[] formats = { " MMM dd" };
            try
            {
                craigslistInfo.DatePostedReal = DateTime.ParseExact(item.SelectSingleNode("//span[@class='itemdate']").InnerText.Substring(0, formats[0].Length), formats, null, System.Globalization.DateTimeStyles.AssumeUniversal);
            }
            catch (FormatException)
            {
                craigslistInfo.DatePostedReal = DateTime.MinValue;
                Console.WriteLine("Unable to convert '{0}' to a date.", item.SelectSingleNode("//span[@class='itemdate']").InnerText);
            }

            //
            // Next the Price
            //
            craigslistInfo.Price = item.SelectSingleNode("//span[@class='itempp']").InnerText.Trim();
            craigslistInfo.SaleArea = item.SelectSingleNode("//font").InnerText;
            craigslistInfo.LinkURL = item.ChildNodes[7].Attributes["href"].Value;
            craigslistInfo.ShortTitle = item.ChildNodes[7].InnerText;
            craigslistInfo.Category = item.ChildNodes[17].SelectSingleNode("//a").InnerText;
            craigslistInfo.DatePostedParsed = craigslistInfo.DatePostedReal.ToString("dd MMM yyyy HH:mm:ss");
            return craigslistInfo;
        }
 

        /// <summary>
        /// Do some clever stuff to work out how dodgy it is
        /// 
        /// TODO: make this better
        /// Ideas might be 
        ///     1)not many images                   (naive implementation DONE)
        ///     2)interesting idea of David Kitchens - test if the image is a stock one or a unique one taken by the owner
        ///         Theives will tend to use stock images they find elsewhere on the web rather than photograph the real 
        ///         bike - photographing the real bike makes it more likely the owner may recognise it's ideosyncracies
        ///         and cause trouble, so they use stock images from manufacturer sites, google images etc.
        ///         Amazingly there is a site called TinEye.com than hold hashes of most images on the web and can 
        ///         tell if an image is original or stock! Their Api is £300 but lets hit their site for the moment 
        ///         as our small volumes may go unnoticed / unpunished. 
        ///         For the time make it event driven - so we do tineye checks by clicking on the FE to initiatite 
        ///         tineye checks - this will keep volumes small
        ///     2a) When we get a tineye hit we need to store the URL of the original miage tineye founf in our DB, 
        ///         subsequent tineye hits that use the same stock image then become more dodgy since this fits the 
        ///         profile of one guy selling lots of ripped off gear with the same craigslist modus opperandi
        ///         Dermot says this might be especially true of e.g. iPhone thefts
        ///         [easy - but need a postgres DB]
        ///     3)brevity of the descriptive text   (naive implementation DONE)
        ///     4) Bayesian filtering to 'learn' dodgy words or phrases (same algorithmic tech as in spam filters)
        ///     5) Timing - is there an average time after somthing is stolen that it goes on craigslist? 
        ///        If so then the closer this listing is to that average the dodgier
        ///     6) Price: is the price suspiciously low when compared to either
        ///         i) other listings in this collection (we'd need the craigslist search to be 
        ///            very tighly defined to be sure we were matching apples with apples)
        ///         ii) A known DB of average prices for items 
        ///     7) Lack of some types of data e.g. phonenumber
        ///     8) presence of some types of data e.g. phonenumber
        ///     9) How dodgy the area the item is in (e.g. downtown > dodginess than uptown)
        ///     10) A weighting algorithm (Learning algorithm) for balancing each of those factors listed above 
        ///     11)lots of other adverts by the same guy
        ///         ii) and if those adverts are dodgy too 
        ///         [hard / impossible as cannot identify seller in most cases no way to identify seller from the ad's data]
        ///     12) looking up the phone number in a Database of known offenders phone numbers 
        ///        [hard / impossible as cannot identify seller in almost all cases there are no phone numbers]
        ///     13) looking up the phone number in a Database all other phone numbers we've found before 
        ///         when the same guy is posting lots of stuff - it is suspicous
        ///         [easy - but need a postgres DB]
        ///     14) Are some phone numbers 'dodgy' by definition - like the 'burner phones' in The Wire - do 
        ///        those have specific number ranges?
        ///        [hard / impossible as cannot identify seller in almost all cases there are no phone numbers]
        ///     
        /// For exmaple this one looks dodgy
        /// http://vancouver.en.craigslist.ca/bnc/bik/3418309806.html
        /// one image, naff all description, looks cheap
        /// </summary>
        /// <param name="bodyElement"></param>
        /// <returns></returns>
        public void calculateDodginessScore(HtmlAgilityPack.HtmlNode bodyElement)
        {
            //
            // Count the images
            // The more images you have the less dodgy you are
            //
            if (null != this.Images)
            {
                this.DodgyScore = DODGY_POINTS_IMAGE_MAXIMUM + (this.Images.Count * DODGY_POINTS_IMAGE_ITEM_REDUCTION_VALUE);
                this.DodgyDesciption = String.Format("{0} images mean a score of {1}", this.Images.Count, this.DodgyScore);
            }
            else
            {
                this.DodgyScore = DODGY_POINTS_IMAGE_MAXIMUM;
                this.DodgyDesciption = String.Format("No images at all mean a score of {0}", DODGY_POINTS_IMAGE_MAXIMUM);
            }
            //
            // Again count the words in the description
            // More words is better 
            //
            int count = 0;
            foreach (char a in bodyElement.InnerText)
            {
                if (char.IsWhiteSpace(a))
                {
                    count++;
                }
            }
            this.DodgyScore += (DODGY_POINTS_WORD_MAXIMUM + (count * DODGY_POINTS_WORD_ITEM_REDUCTION_VALUE));
            this.DodgyDesciption += String.Format("\nDescription is {0} words long so score of {1}" , count, (DODGY_POINTS_WORD_MAXIMUM + (count * DODGY_POINTS_WORD_ITEM_REDUCTION_VALUE)));
            
        }

        /// <summary>
        /// Figure out the phone number using regex's
        /// see http://code.google.com/p/libphonenumber/
        /// in practice I've only found 1 or 2 craigslist adverts that have phione numbers so lets not bother with this currently
        /// unfortunately the google stuff is not c# so do just some simple stuff:
        /// Look for 604***** and 708******
        /// In practice we should make a list of the phone numbers we find and - then if they start to repeat - it is dodgy
        /// </summary>
        /// <param name="bodyElement"></param>
        /// <returns></returns>
        public string extractPhoneNumber(HtmlAgilityPack.HtmlNode bodyElement)
        {
            string result = DEFAULT_TEL_NUMBER;
            int potentialPhoneNumber = bodyElement.InnerText.IndexOf(VANCOUVER_TEL_CODE1, 0);
            if (potentialPhoneNumber > 0)
            {
                result = bodyElement.InnerText.Substring(potentialPhoneNumber, LENGTH_OF_TEL_NUMBER);
            }
            else
            {
                potentialPhoneNumber = bodyElement.InnerText.IndexOf(VANCOUVER_TEL_CODE2, 0);
                if (potentialPhoneNumber > 0)
                {
                    result = bodyElement.InnerText.Substring(potentialPhoneNumber, LENGTH_OF_TEL_NUMBER);
                }
            }
            //
            // strip out anything that's not a number (dashes, whitespace, letters)
            //
            char[] arr = result.ToCharArray();
            //arr = Array.FindAll<char>(arr, (c => (char.IsLetter(c) || char.IsWhiteSpace(c) || c == '-')));
            arr = Array.FindAll<char>(arr, (c => (char.IsDigit(c))));
            return new string(arr);
        }


        /// <summary>
        /// The actions from the foreach loop are moved to this async method.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task<CraigslistInfo> ProcessURL(string url, HttpClient client)
        {
            //
            // The detail page is not available in RSS, so have to parse the HTML for it (much harder than the nice XML you get frmo RSS feed)
            // to kae this easier use a special gizmo called the HTMl agility pack that parses the 
            // HTMl into a document tree (and fixes errors etc in the HTMl) - this allows us to traverse the page safely
            //
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            //
            // There are various options, set as needed
            // Here I just want any errors tidied up to make a valid Docuemtn Tree so we can parse with confidence
            //
            htmlDoc.OptionFixNestedTags = true;
            htmlDoc.Load((await (client.GetStreamAsync(url))), true);
            //
            // Now ots of magic to parse the data from all the HTML
            //
            CraigslistInfo currentListing = new CraigslistInfo();
            currentListing.updateCraigslistInfoFromFullItemDetailsPage(htmlDoc, url);
            return currentListing;
        }
    }

}
