using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Configuration;
using System.IO;
using DataAccess.Globals;

using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Web.pages
{
    public partial class awsEC2Instances : System.Web.UI.Page
    {
        acUI.acUI ui = new acUI.acUI();
        acUI.AppGlobals ag = new acUI.AppGlobals();

        static AmazonEC2 ec2;

        protected void Page_Load(object sender, EventArgs e)
        {
            /*
             NOTE: this page looks cool and will be used on the ecosystem managemnet.
             * 
             * but, it was the first page I did and I do NOT want to keep using the XmlDataSource.
             * 
             * I think that's the reason it won't refresh the data when I call a location.reload in script... 
             * it's caching it somehow.
             * 
             * The correct implementation of this page will be to pull data PRIMARILY from our ecosystem table
             * then merge in informational data from an AWS API call.
             * 
             * In any case, switch this to DataTables like the discovery page and the awsMethods class.
             * 
             * .
             */
            string sAccessKeyID = ui.GetCloudLoginID();
            string sSecretAccessKeyID = ui.GetCloudLoginPassword();

            ec2 = Amazon.AWSClientFactory.CreateAmazonEC2Client(sAccessKeyID, sSecretAccessKeyID);

            DescribeInstancesRequest request = new DescribeInstancesRequest();
            DescribeInstancesResponse resp = ec2.DescribeInstances(request);

            XDocument xDoc = XDocument.Parse(resp.DescribeInstancesResult.ToXML().Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://ec2.amazonaws.com/doc/2011-02-28/\"",""));
            if (xDoc == null) ui.RaiseError(Page, "Reservations XML document is invalid.", false, "");


            XmlDataSource xmlsrc = new XmlDataSource();
            xmlsrc.ID = "xmlReservations";
            xmlsrc.Data = xDoc.ToString();
            xmlsrc.DataBind();
            xmlsrc.XPath = "DescribeInstancesResult/Reservation";

            rptReservations.DataSource = xmlsrc;
            rptReservations.DataBind();

            //Response.Write(xDoc.ToString());

        }
    }
}
