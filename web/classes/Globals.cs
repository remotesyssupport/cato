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
using System.ComponentModel;
using System.Data;


namespace Globals
{
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
        Ecosystem = 41,
        EcoTemplate = 42
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
    public class CloudObjectTypes
    {

        public ArrayList alCloudObjectTypes = new ArrayList();
        public CloudObjectType GetCloudObjectType(string sObjectType)
        {
            //This will find the object in this class by the ObjectType property and return it.
            CloudObjectType cob = null;
            foreach (CloudObjectType cob_loopVariable in alCloudObjectTypes)
            {
                cob = cob_loopVariable;
                if (cob.ObjectType == sObjectType)
                {
                    return cob;
                }
            }

            return null;
        }

        public void Fill()
        {
            //get the cloud objects from the db
            dataAccess dc = new dataAccess();

            string sErr = "";

            string sSQL = "select cloud_object_type, vendor, label, api_call, api_hostname, api_version," +
                " request_protocol, request_method, request_group_filter, request_record_filter," +
                " result_record_xpath" +
                " from cloud_object_type" +
                " order by vendor, cloud_object_type";

            DataTable dt = new DataTable();
            if (dc.sqlGetDataTable(ref dt, sSQL, ref sErr))
            {
                if (dt.Rows.Count > 0)
                {
                    DataRow dr = null;
                    foreach (DataRow dr_loopVariable in dt.Rows)
                    {
                        dr = dr_loopVariable;
                        CloudObjectType cot = new CloudObjectType();
                        cot.ObjectType = dr["cloud_object_type"].ToString();
                        cot.Label = dr["label"].ToString();
                        cot.API = dr["cloud_object_type"].ToString();
                        cot.APICall = dr["api_call"].ToString();
                        cot.APIHostName = dr["api_hostname"].ToString();
                        cot.APIRequestMethod = dr["request_method"].ToString();
                        cot.APIRequestProtocol = dr["request_protocol"].ToString();
                        cot.APIRequestGroupFilter = dr["request_group_filter"].ToString();
                        cot.APIRequestRecordFilter = dr["request_record_filter"].ToString();
                        cot.APIVersion = dr["api_version"].ToString();
                        cot.Vendor = dr["vendor"].ToString();
                        cot.XMLRecordXPath = dr["result_record_xpath"].ToString();

                        //OK, lets get all the properties for this type
                        sSQL = "select property_name, property_label, property_xpath, id_field, has_icon, short_list" +
                            " from cloud_object_type_props" +
                            " where cloud_object_type = '" + cot.ObjectType + "'" +
                            " order by id_field desc, sort_order";

                        DataTable dtProps = new DataTable();
                        if (dc.sqlGetDataTable(ref dtProps, sSQL, ref sErr))
                        {
                            if (dtProps.Rows.Count > 0)
                            {
                                DataRow drProps = null;
                                foreach (DataRow drProps_loopVariable in dtProps.Rows)
                                {
                                    drProps = drProps_loopVariable;
                                    CloudObjectTypeProperty cop = new CloudObjectTypeProperty();
                                    //use the label if you have one
                                    cop.PropertyName = drProps["property_name"].ToString();
                                    //IIf(String.IsNullOrEmpty(drProps("property_label").ToString()), drProps("property_name").ToString(),drProps("property_label").ToString())
                                    cop.PropertyLabel = drProps["property_label"].ToString();
                                    cop.PropertyXPath = drProps["property_xpath"].ToString();
                                    cop.PropertyHasIcon = dc.IsTrue(drProps["has_icon"].ToString());
                                    cop.IsID = dc.IsTrue(drProps["id_field"].ToString());
                                    cop.ShortList = dc.IsTrue(drProps["short_list"].ToString());

                                    cot.Properties.Add(cop);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Unable to lookup Cloud object type properties." + sErr);
                        }

                        alCloudObjectTypes.Add(cot);
                    }
                }
            }

        }
    }
    public class CloudObjectType
    {
        public string ObjectType;
        public string Vendor;
        public string API;
        public string Label;
        public string APICall;
        public string APIHostName;
        public string APIVersion;
        public string APIRequestProtocol;
        public string APIRequestMethod;
        public string APIRequestGroupFilter;
        public string APIRequestRecordFilter;
        public string XMLRecordXPath;

        public ArrayList Properties = new ArrayList();
        public bool IsValidForCalls()
        {
            if (string.IsNullOrEmpty(this.APICall) || string.IsNullOrEmpty(this.APIHostName) || string.IsNullOrEmpty(this.APIRequestMethod) || string.IsNullOrEmpty(this.APIRequestProtocol) || string.IsNullOrEmpty(this.APIVersion) || string.IsNullOrEmpty(this.ObjectType))
                return false;

            return true;
        }

        public string AsString()
        {
            //This will find the object in this class by the ObjectType property and return it as a nice string.
            string sReq = "<span class='ui-state-error'>required</span>";
            string sOut = "";
            sOut += "ObjectType:" + (string.IsNullOrEmpty(this.ObjectType) ? sReq : this.ObjectType).ToString() + "<br />";
            sOut += "Label:" + this.Label + "<br />";
            sOut += "API:" + this.API + "<br />";
            sOut += "APICall:" + (string.IsNullOrEmpty(this.APICall) ? sReq : this.APICall).ToString() + "<br />";
            sOut += "APIHostName:" + (string.IsNullOrEmpty(this.APIHostName) ? sReq : this.APIHostName).ToString() + "<br />";
            sOut += "APIRequestMethod:" + (string.IsNullOrEmpty(this.APIRequestMethod) ? sReq : this.APIRequestMethod).ToString() + "<br />";
            sOut += "APIRequestProtocol:" + (string.IsNullOrEmpty(this.APIRequestProtocol) ? sReq : this.APIRequestProtocol).ToString() + "<br />";
            sOut += "APIRequestGroupFilter:" + this.APIRequestGroupFilter + "<br />";
            sOut += "APIRequestRecordFilter:" + this.APIRequestRecordFilter + "<br />";
            sOut += "APIRequestRecordFilter:" + (string.IsNullOrEmpty(this.APIVersion) ? sReq : this.APIVersion).ToString() + "<br />";
            sOut += "Vendor:" + this.Vendor + "<br />";
            sOut += "XMLRecordXPath:" + (string.IsNullOrEmpty(this.XMLRecordXPath) ? sReq : this.XMLRecordXPath).ToString() + "<br />";

            sOut += "Properties:<br />";
            if (this.Properties.Count > 0)
            {
                CloudObjectTypeProperty cop = new CloudObjectTypeProperty();
                foreach (CloudObjectTypeProperty cop_loopVariable in this.Properties)
                {
                    cop = cop_loopVariable;
                    sOut += "<div style='margin-left:10px; padding: 2px;border: solid 1px #cccccc;'>";
                    sOut += "PropertyName:" + cop.PropertyName + "<br />";
                    sOut += "PropertyLabel:" + cop.PropertyLabel + "<br />";
                    sOut += "PropertyXPath:" + cop.PropertyXPath + "<br />";
                    sOut += "PropertyHasIcon:" + cop.PropertyHasIcon + "<br />";
                    sOut += "IsID:" + cop.IsID + "<br />";
                    sOut += "ShortList:" + cop.ShortList;
                    sOut += "</div>";
                }
            }
            else
            {
                sOut += "<span class='ui-state-error'>At least one Property is required.</span>";
            }

            return sOut;
        }

    }
    public class CloudObjectTypeProperty
    {
        public string PropertyName;
        public string PropertyLabel;
        public string PropertyXPath;
        public bool PropertyHasIcon;
        public bool IsID;
        public bool ShortList;
    }
}
