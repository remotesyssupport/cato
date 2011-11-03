//Copyright 2011 Cloud Sidekick
// 
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;


namespace Globals
{
	#region "Enums"
    public enum acObjectTypes : int
    {
        None = 0,
        User = 1,
        Asset = 2,
        Task = 3,
        Schedule = 4,
        Procedure = 5,
        Registry = 6,
        DashLayout = 10,
        DashWidget = 11,
        FileReader = 14,
        MessageTemplate = 18,
        AssetAttribute = 22,
        Parameter = 34,
        Credential = 35,
        Domain = 36,
        CloudAccount = 40,
		Cloud = 41,
		Ecosystem = 50,
        EcoTemplate = 51
    }
    public enum FieldValidatorTypes : int
    {
        [Description("Restricts entry to any numeric value, positive or negative, with or without a decimal.")]
        Number = 0,
        [Description("Restricts entry to a positive number.")]
        PositiveNumber = 1,
        [Description("Limits entry to upper and lowercase letters, numbers, dot, underscore and @.")]
        UserName = 2,
        [Description("Restricts entry to a strict identifier style.  Uppercase letters, numbers and underscore only.")]
        TaskName = 3
    }
    public enum SecurityLogTypes
    {
        Object = 0,
        Security = 1,
        Usage = 2,
        Other = 3
    }
    public enum SecurityLogActions
    {
        UserLogin = 0,
        UserLogout = 1,
        UserLoginAttempt = 2,
        UserPasswordChange = 3,
        UserSessionDrop = 4,
        SystemLicenseException = 5,

        ObjectAdd = 100,
        ObjectModify = 101,
        ObjectDelete = 102,
        ObjectView = 103,
        ObjectCopy = 104,

        PageView = 800,
        ReportView = 801,

        APIInterface = 700,

        Other = 900,
        ConfigChange = 901
    }
#endregion    

	//this CurrentUser class will be used when we are ready to stop using Session
    public class CurrentUser
    {
        private static string sUserID = "";
        public static string UserID
        {
            get { return sUserID; }
            set { sUserID = value; }
        }
    }

	#region "Database Settings"
	public class DatabaseSettings
    {

        private static int iConnectionTimeout = 90;
        public static int ConnectionTimeout
        {
            get { return iConnectionTimeout; }
            set { iConnectionTimeout = value; }
        }

        private static string sAppInstance = "";
        public static string AppInstance
        {
            get { return sAppInstance; }
            set { sAppInstance = value; }
        }

        private static string sDatabaseName = "";
        public static string DatabaseName
        {
            get { return sDatabaseName; }
            set { sDatabaseName = value; }
        }

        private static string sServerAddress = "";
        public static string DatabaseAddress
        {
            get { return sServerAddress; }
            set { sServerAddress = value; }
        }

        private static string sServerPort = "";
        public static string DatabasePort
        {
            get { return sServerPort; }
            set { sServerPort = value; }
        }

        private static string sServerUserName = "";
        public static string DatabaseUID
        {
            get { return sServerUserName; }
            set { sServerUserName = value; }
        }

        private static string sServerUserPassword = "";
        public static string DatabaseUserPassword
        {
            get { return sServerUserPassword; }
            set { sServerUserPassword = value; }
        }

        private static string sEnvironmentKey = "";
        public static string EnvironmentKey
        {
            get { return sEnvironmentKey; }
            set { sEnvironmentKey = value; }
        }

        private static string sServerUseSSL = "";
        public static string UseSSL
        {
            get { return sServerUseSSL; }
            set { sServerUseSSL = value; }
        }

    }
	#endregion    

	#region "Cloud"
	/*
	 * The Cloud Providers class contains all the definition of a Cloud Provider, including
	 * object types and their properties.
	 * 
	 * This class is instantiated at login, and the populated class is cached in the session.
	 * 
	 * The CloudProviders class is an Arraylist of Provider objects.
	 * 
	 * The Provider object has a name and other properties, including an ArrayList of CloudObjectType objects.
	 * 
	 * */
	public class CloudProviders : Dictionary<string, Provider>  //CloudProviders IS a named dictionary of Provider objects
	{
		//the constructor requires an XDocument
		public CloudProviders (XDocument xProviders)
		{

			if (xProviders == null) {
				throw new Exception ("Error: Invalid or missing Cloud Providers XML.");
			} else {
				foreach (XElement xProvider in xProviders.XPathSelectElements("//providers/provider")) {
					if (xProvider.Attribute ("name") == null) 
						throw new Exception ("Cloud Providers XML: All Providers must have the 'name' attribute.");
					
					Provider pv = new Provider();
					pv.Name = xProvider.Attribute ("name").Value;
					pv.TestProduct = (xProvider.Attribute ("test_product") == null ? "" : xProvider.Attribute ("test_product").Value);
					pv.TestObject = (xProvider.Attribute ("test_object") == null ? "" : xProvider.Attribute ("test_object").Value);
					
					//get the cloudobjecttypes for this provider.					
					foreach (XElement xProduct in xProvider.XPathSelectElements("products/product"))
	                {
						if (xProduct.Attribute("name") == null) 
							throw new Exception("Cloud Providers XML: All Products must have the 'name' attribute.");
						if (xProduct.Attribute("api_protocol") == null) 
							throw new Exception("Cloud Providers XML: All Products must have the 'api_protocol' attribute.");
						
						Product p = new Product(pv);
						p.Name = xProduct.Attribute("name").Value;
						//use the name for the label if it doesn't exist.
						p.Label = (xProduct.Attribute("label") == null ? p.Name : xProduct.Attribute("label").Value);
						p.APIProtocol = xProduct.Attribute("api_protocol").Value;
						p.APIUrlPrefix = (xProduct.Attribute("api_url_prefix") == null ? "" : xProduct.Attribute("api_url_prefix").Value);
						p.APIUri = (xProduct.Attribute("api_uri") == null ? "" : xProduct.Attribute("api_uri").Value);
						p.APIVersion = (xProduct.Attribute("api_version") == null ? "" : xProduct.Attribute("api_version").Value);
						
						//the product contains object type definitions
						foreach (XElement xType in xProduct.XPathSelectElements("object_types/type"))
		                {
							if (xType.Attribute("id") == null) 
								throw new Exception("Cloud Providers XML: All Object Types must have the 'id' attribute.");
							if (xType.Attribute("label") == null) 
								throw new Exception("Cloud Providers XML: All Object Types must have the 'label' attribute.");
							
							CloudObjectType cot = new CloudObjectType(p);
							cot.ID = xType.Attribute("id").Value;
							cot.Label = xType.Attribute("label").Value;
							
							cot.APICall = (xType.Attribute("api_call") == null ? "" : xType.Attribute("api_call").Value);
							cot.APIRequestGroupFilter = (xType.Attribute("request_group_filter") == null ? "" : xType.Attribute("request_group_filter").Value);
							cot.APIRequestRecordFilter = (xType.Attribute("request_record_filter") == null ? "" : xType.Attribute("request_record_filter").Value);
							cot.XMLRecordXPath = (xType.Attribute("xml_record_xpath") == null ? "" : xType.Attribute("xml_record_xpath").Value);
							
							//the type contains property definitions
							foreach (XElement xProperty in xType.XPathSelectElements("property"))
			                {
								// name="ImageId" label="" xpath="imageId" id_field="1" has_icon="0" short_list="1" sort_order="1"
								if (xProperty.Attribute("name") == null) 
									throw new Exception("Cloud Providers XML: All Object Type Properties must have the 'name' attribute.");
								if (xProperty.Attribute("xpath") == null) 
									throw new Exception("Cloud Providers XML: All Object Type Properties must have the 'xpath' attribute.");
								
								CloudObjectTypeProperty cotp = new CloudObjectTypeProperty(cot);
								cotp.Name = xProperty.Attribute("name").Value;
								cotp.XPath = xProperty.Attribute("xpath").Value;
								
								cotp.Label = (xProperty.Attribute("label") == null ? "" : xProperty.Attribute("label").Value);
								cotp.SortOrder = (xProperty.Attribute("sort_order") == null ? "" : xProperty.Attribute("sort_order").Value);
								cotp.IsID = (xProperty.Attribute("id_field") == null ? false : 
									(xProperty.Attribute("id_field").Value == "1" ? true : false));
								cotp.HasIcon = (xProperty.Attribute("has_icon") == null ? false : 
									(xProperty.Attribute("has_icon").Value == "1" ? true : false));
								cotp.ShortList = (xProperty.Attribute("short_list") == null ? false : 
									(xProperty.Attribute("short_list").Value == "1" ? true : false));
								
								cot.Properties.Add(cotp);
							}
							
							p.CloudObjectTypes.Add(cot.ID, cot);
						}						
							
						pv.Products.Add(p.Name, p);
					}

					this.Add(pv.Name, pv);
				}
			}
        }
	}
	public class Provider
	{
		public string Name { get; set; }
		public string TestProduct { get; set; }
		public string TestObject { get; set; }
		
		//Provider CONTAINS a named dictionary of Products;
		public Dictionary<string, Product> Products = new Dictionary<string, Product>();
		
		public CloudObjectType GetObjectTypeByName(string sObjectType)
		{
	        foreach (Product p in this.Products.Values)
	        {
				try
				{
					CloudObjectType cot = p.CloudObjectTypes[sObjectType];
					if (cot != null)
						return cot;
				}
	            catch (Exception)
	            {
					//don't crash while it's iterating, it may find it in the next object.
					//don't worry, we'll return null if it doesn't find anything.
				}
			}
			return null;
		}
		public Dictionary<string, CloudObjectType> GetAllObjectTypes()
		{
			Dictionary<string, CloudObjectType> cots = new Dictionary<string, CloudObjectType>();
			
	        foreach (Product p in this.Products.Values)
	        {
		        foreach (CloudObjectType cot in p.CloudObjectTypes.Values)
		        {
					if (cot != null)
						cots.Add(cot.ID, cot);
				}
			}
			return cots;
		}
	}

	public class Product
	{
		public Provider ParentProvider { get; set; }
		public string Name { get; set; }
		public string Label { get; set; }
		public string APIProtocol { get; set; }
        public string APIUrlPrefix { get; set; }
        public string APIUri { get; set; }
		public string APIVersion { get; set; }
 		
		//constructor
		public Product(Provider parent)
		{
			this.ParentProvider = parent;
		}
		
		//Product CONTAINS a named dictionary of CloudObjectTypes;
		public Dictionary<string, CloudObjectType> CloudObjectTypes = new Dictionary<string, CloudObjectType>();
        
		public bool IsValidForCalls()
        {
            if (string.IsNullOrEmpty(this.Name) || string.IsNullOrEmpty(this.APIProtocol))
                return false;

            return true;
        }
	}

    public class CloudObjectType
    {
		public Product ParentProduct { get; set; }
        public string ID { get; set; }
        public string Label { get; set; }
        public string APICall { get; set; }
        public string APIRequestGroupFilter { get; set; }
        public string APIRequestRecordFilter { get; set; }
        public string XMLRecordXPath { get; set; }
		
		//constructor
		public CloudObjectType(Product parent)
		{
			this.ParentProduct = parent;
		}
		
		//CloudObjectType CONTAINS an array or properties
        public ArrayList Properties = new ArrayList();

		public bool IsValidForCalls()
        {
            if (string.IsNullOrEmpty(this.APICall) || string.IsNullOrEmpty(this.XMLRecordXPath) || string.IsNullOrEmpty(this.ID))
                return false;

            return true;
        }
    }
    public class CloudObjectTypeProperty
    {
 		public CloudObjectType ParentObjectType { get; set; }
		public string Name { get; set; }
        public string Label { get; set; }
        public string XPath { get; set; }
		public string SortOrder { get; set; }
        public bool HasIcon { get; set; }
        public bool IsID { get; set; }
        public bool ShortList { get; set; }

		//constructor
		public CloudObjectTypeProperty(CloudObjectType parent)
		{
			this.ParentObjectType = parent;
		}
		
    }
	
	public class Cloud
    {
        public string ID;
		public string Name;
		public Provider Provider;
        public string APIUrl;
		
		//the default constructor
		public Cloud(string sCloudID)
        {
            //get the cloud from the db
            dataAccess dc = new dataAccess();
			acUI.acUI ui = new acUI.acUI();
			
            string sErr = "";

            string sSQL = "select cloud_name, provider, api_url" +
                " from clouds" +
                " where cloud_id = '" + sCloudID + "'";

            DataRow dr = null;
            if (dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                if (dr != null)
                {
					ID = sCloudID;
					Name = dr["cloud_name"].ToString();
					APIUrl = dr["api_url"].ToString();
					
					CloudProviders cp = ui.GetCloudProviders();
					if (cp != null) {
						Provider p = cp[dr["provider"].ToString()];
						Provider = p;
					}
				}
				else 
				{
					throw new Exception("Unable to build Cloud object. Either no Clouds are defined, or no Cloud with ID [" + sCloudID + "] could be found.");	
				}
			}
			else 
			{
				throw new Exception("Error building Cloud object: " + sErr);	
			}
        }
		
        public bool IsValidForCalls()
        {
            if (string.IsNullOrEmpty(this.APIUrl))
                return false;

            return true;
        }

   }

	public class CloudAccount
    {
        public string ID;
		public string Name;
		public string Provider;
		public string AccountNumber;
        public string LoginID;
        public string LoginPassword;
		
		//the default constructor
		public CloudAccount(string sAccountID)
        {
            //get the cloud from the db
            dataAccess dc = new dataAccess();

            string sErr = "";

            string sSQL = "select account_name, account_number, provider, login_id, login_password" +
                " from cloud_account" +
                " where account_id = '" + sAccountID + "'";

            DataRow dr = null;
            if (dc.sqlGetDataRow(ref dr, sSQL, ref sErr))
            {
                if (dr != null)
                {
					ID = sAccountID;
					Name = dr["account_name"].ToString();
					Provider = dr["provider"].ToString();
					AccountNumber = (string.IsNullOrEmpty(dr["account_number"].ToString()) ? "" : dr["account_number"].ToString());
					LoginID = (string.IsNullOrEmpty (dr["login_id"].ToString()) ? "" : dr["login_id"].ToString());
					LoginPassword = (string.IsNullOrEmpty (dr["login_password"].ToString()) ? "" : dc.DeCrypt(dr["login_password"].ToString()));
				}
				else 
				{
					throw new Exception("Unable to build Cloud Account object. Either no Cloud Accounts are defined, or no Account with ID [" + sAccountID + "] could be found.");	
				}
			}
			else 
			{
				throw new Exception("Error building Cloud Account object: " + sErr);	
			}
        }
		
        public bool IsValidForConnections()
        {
			if (string.IsNullOrEmpty (this.LoginID))
				return false;

			if (string.IsNullOrEmpty (this.LoginPassword))
				return false;

            return true;
        }

   }

#endregion
}
