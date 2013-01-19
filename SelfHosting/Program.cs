using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using bloodhound;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace SelfHosting
{
    class Program
    {
        static void Main(string[] args)
        {
//ServiceHost sh = new ServiceHost(typeof(mClubDWServiceType));
//string baseUri = "http://localhost/bloodhoundService";
//ServiceEndpoint se = sh.AddServiceEndpoint(typeof(ImClubDWService),
//                                            new WebHttpBinding(),
//                                            baseUri);
//se.Behaviors.Add(new WebHttpBehavior());
// when this doesn't work and throws a
// "Your process does not have access rights to this namespace"
// then issue this command in admin DOS 
// netsh http add urlacl url=http://+:80/MyUri user=HAL\Geno
// netsh http add urlacl url=http://+:80/mClubDWService user=HAL\Geno
//
string baseUri = "http://localhost/bloodhoundService";
WebServiceHost sh = new WebServiceHost(typeof(bloodhoundService),
                                    new Uri(baseUri));
sh.Open();
            Console.ReadLine();
        }
    }
}
