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
using System.Data;
using System.Diagnostics;
using System.Web.Configuration;
using System.IO;
using Globals;
using Encryption;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

public class dataAccess
{

    public string BLOWFISH_KEY = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}" +
		"{17}{18}{19}{20}{21}{22}{23}{24}{25}{26}{27}{28}{29}{30}{31}",
        (char)83, (char)116, (char)89, (char)87, (char)76, (char)99, (char)65, (char)50, 
        (char)115, (char)87, (char)97, (char)112, (char)72, (char)76, (char)66, (char)109, 
		(char)115, (char)78, (char)101, (char)86, (char)53, (char)89, (char)88, (char)69, 
        (char)69, (char)85, (char)101, (char)90, (char)107, (char)75, (char)81, (char)77);
		
    public void New()
    {
    }

    #region "Internal Functions"
	public bool TestDBConnection(ref string sErr) {
	/*
	 * We'll be doing a simple query against the database to verify connectivity.
	 * 
	 * NOTE: on several completely random places, we've noted an odd connection issue, 
	 * that seems to only happen immediately after the box (apache/mysql/mono) starts up.
	 * 
	 * In those places, hitting this function first seems to "clear the pipes" and subsequent 
	 * queries work fine.
	 * 
	 * This is a terrible workaround hack, but we can move on and fix when the cause is finally found.
	 * 
	 * */
        string sSQL = "select 'Database Test Successful'";
		string sTestResult = "";
        if (!sqlGetSingleString(ref sTestResult, sSQL, ref sErr))
        {
			return false;
        }
		return true;
	}
    public bool InitDatabaseSettings(string sPath, ref string sErr)
    {

        string sFileContents = "";

        /*
         * We will attempt to read the Environment.txt file.
         * 
         * data is stored in the key:value; format
         * 
        */
        try
        {
            StreamReader oReader = default(StreamReader);
            oReader = new StreamReader(sPath);
            sFileContents = oReader.ReadToEnd();
            oReader.Close();
        }
        catch (System.IO.FileNotFoundException)
        {
            sErr = "cato.conf file not found.";
            return false;
        }
        catch (Exception ex)
        {
            sErr = ex.Message;
            return false;
        }

        try
        {
            //each new line in the file is potentially a value we want
            string[] aPair = sFileContents.Split(Environment.NewLine.ToCharArray());
            if (aPair.Length > 0)
            {
                int iImportant = 0;

                //this approach will ignore anything not needed
                //therefore we don't need a specific "comment" token like # or //
                foreach (string sPair in aPair)
                {
                    //split each row by spaces
                    string[] aKeyVal = sPair.Split(' ');
                    string sKey = aKeyVal[0].ToString();

                    //everything else on the line is the value, and we will trim it
                    string sVal = (aKeyVal.Length > 1 ? sPair.Substring(sKey.Length + 1).Trim() : "");

                    switch (sKey.ToLower())
                    {
                        case "database":
                            DatabaseSettings.DatabaseName = sVal;
                            iImportant++;
                            break;
                        case "server":
                            DatabaseSettings.DatabaseAddress = sVal;
                            iImportant++;
                            break;
                        case "port":
                            DatabaseSettings.DatabasePort = sVal;
                            break;
                        case "password":
                            DatabaseSettings.DatabaseUserPassword = sVal;
                            iImportant++;
                            break;
                        case "user":
                            DatabaseSettings.DatabaseUID = sVal;
                            iImportant++;
                            break;
                        case "key":
                            DatabaseSettings.EnvironmentKey = sVal;
                            iImportant++;
                            break;
                        case "databaseUseSSL":
                            DatabaseSettings.UseSSL = sVal;
                            break;
                        case "dbConnectionTimeout":
                            int result;
                            if (int.TryParse(sVal, out result))
                                DatabaseSettings.ConnectionTimeout = result;
                            break;
                    }
                }

                // the 5 settings above must exist for the app to work, so if we looped
                // and didnt find at least these 6 then stop here, no point in continuing.
                // bugzilla 1225: since we added the ssl and validate certificate settings, those are optional -stan
                if (iImportant != 5)
                {
                    sErr = "Incorrect setup information. " + iImportant + " config values found in the cato.conf file ... should be 5.";
                    return false;
                }
            }
            else
            {
                sErr = "cato.conf file doesn't have any information in the config file format.";
                return false;
            }
        }
        catch (Exception ex)
        {
            //what do we do if it does not exist?
            sErr = ex.Message;
            return false;
        }

        return true;

    }
    public string getMySQLConString()
    {

        string sDecryptedString = "";

        //Fail if all the required values aren't here.
        if (string.IsNullOrEmpty(DatabaseSettings.DatabaseAddress))
            return "";
        if (string.IsNullOrEmpty(DatabaseSettings.DatabaseName))
            return "";
        if (string.IsNullOrEmpty(DatabaseSettings.DatabaseUID))
            return "";
        if (string.IsNullOrEmpty(DatabaseSettings.DatabaseUserPassword))
            return "";

        string sPort = DatabaseSettings.DatabasePort.ToString();
        if (!string.IsNullOrEmpty(sPort))
            sPort = "," + sPort;

        sDecryptedString = "SERVER=" + DatabaseSettings.DatabaseAddress + sPort + ";" +
            "DATABASE=" + DatabaseSettings.DatabaseName + ";" +
            "UID=" + DatabaseSettings.DatabaseUID + ";" +
            "PWD=" + DeCrypt(DatabaseSettings.DatabaseUserPassword) + ";" +
            "CONNECTION TIMEOUT=" + DatabaseSettings.ConnectionTimeout + ";";
        //"pooling=false" & ";"

        // bugzilla 1225, if the ssl settings are present add them
        if (DatabaseSettings.UseSSL.ToLower() == "true" || DatabaseSettings.UseSSL.ToLower() == "false")
        {
            sDecryptedString += "Encrypt=" + DatabaseSettings.UseSSL + ";";
        }

        return sDecryptedString;

    }
    public bool IsTrue(object oObj)
    {
        if (oObj != null)
        {
            switch (oObj.GetType().Name)
            {
                case "Int32":
                    if (Convert.ToInt32(oObj) > 0)
                        return true;
                    break;
                case "Integer":
                    if (Convert.ToInt32(oObj) > 0)
                        return true;
                    break;
                case "String":
                    int result;
                    if (int.TryParse(oObj.ToString(), out result))
                    {
                        if (result > 0)
                            return true;
                    }
                    else if (string.IsNullOrEmpty(Convert.ToString(oObj)))
                    {
                        return false;
                    }
                    else
                    {
                        if ("true,yes,on,enable,enabled".IndexOf(Convert.ToString(oObj).ToLower()) > -1)
                            return true;
                    }
                    break;
                case "Boolean":
                    if (Convert.ToBoolean(oObj) == true)
                        return true;
                    break;
            }
        }

        return false;
    }
    private string FormatError(string ErrorMessage)
    {
        return ErrorMessage.Replace("'", "\\'").Replace("\\r\\n", "<br />");
    }
    private bool ValidSQL(string sSQL, ref string ErrorMessage)
    {
        if (string.IsNullOrEmpty(sSQL))
        {
            ErrorMessage = "Error: No SQL provided.";
            return false;
        }
        else
        {
            return true;
        }
    }
    #endregion

    #region "SQL Functions"
    public MySqlConnection connOpen(ref string ErrorMessage)
    {
        string sConString = getMySQLConString();

        if (!string.IsNullOrEmpty(sConString))
        {
            try
            {
                MySqlConnection oConn = new MySqlConnection(sConString);
                oConn.Open();
                return oConn;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        ErrorMessage = FormatError("MySQL: Cannot connect to server.");
                        break;
                    case 1045:
                        ErrorMessage = FormatError("MySQL: Invalid username/password.");
                        break;
                    default:
                        ErrorMessage = FormatError("MySQL: Unable to connect. " + ex.Message);
                        break;
                }
                return null;
            }
            catch (Exception ex)
            {
                ErrorMessage = FormatError("MySQL: Connection Error: " + ex.Message);
                return null;
            }
        }
        else
        {
            ErrorMessage = FormatError("MySQL: Unable to load Connection String for this Environment.");
            return null;
        }
    }
    public void connClose(MySqlConnection oConn)
    {
        if (oConn != null)
        {
            if (oConn.State != ConnectionState.Closed)
            {
                oConn.Close();
				oConn.Dispose();
            }
        }
    }

    public bool sqlGetSingleObject(ref object o, string sSQL, ref string ErrorMessage)
    {
        try
        {
            sqlExecuteScalar(ref o, sSQL, ref ErrorMessage);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
    }
    public bool sqlGetSingleDouble(ref double d, string sSQL, ref string ErrorMessage)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        try
        {
            object o = new object();
            if (!sqlExecuteScalar(ref o, sSQL, ref ErrorMessage))
                return false;
            d = Convert.ToDouble(o);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }

    }
    public bool sqlGetSingleInteger(ref int i, string sSQL, ref string ErrorMessage)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        try
        {
            object o = new object();
            if (!sqlExecuteScalar(ref o, sSQL, ref ErrorMessage))
                return false;
			
			if (object.ReferenceEquals(o, DBNull.Value))
			{
				ErrorMessage = "GetSingleInteger cannot convert a null result to an integer.";
				return false;
			}
			
			bool bSuccess = Int32.TryParse(o.ToString(), out i);

			if (!bSuccess)
			{
				ErrorMessage = "GetSingleInteger cannot convert [" + o.ToString() + "] to an integer.";
				return false;
			}
			
            return true;
            
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }

    }
    public bool sqlGetSingleString(ref string s, string sSQL, ref string ErrorMessage)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        try
        {
            object o = new object();
            if (!sqlExecuteScalar(ref o, sSQL, ref ErrorMessage))
                return false;
            s = Convert.ToString((object.ReferenceEquals(o, DBNull.Value) ? "" : o));
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }

    }
    public bool sqlGetDataSet(ref DataSet ds, string sSQL, ref string ErrorMessage, string sTableName)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        MySqlConnection oConn = connOpen(ref ErrorMessage);
        if (oConn == null)
            return false;

        try
        {
            MySqlDataAdapter oCommand = new MySqlDataAdapter(sSQL, oConn);
            oCommand.SelectCommand.CommandTimeout = DatabaseSettings.ConnectionTimeout;
            oCommand.Fill(ds, sTableName);
        }
        catch (System.IndexOutOfRangeException)
        {
            DataTable dtEmptyTable = new DataTable();
            dtEmptyTable.TableName = sTableName;
            ds.Tables.Add(dtEmptyTable);
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
        finally
        {
            connClose(oConn);
        }

        return true;

    }
    public bool sqlGetDataTable(ref DataTable dt, string sSQL, ref string ErrorMessage)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        MySqlConnection oConn = connOpen(ref ErrorMessage);
        if (oConn == null)
            return false;

        try
        {
            MySqlDataAdapter oCommand = new MySqlDataAdapter(sSQL, oConn);
            oCommand.SelectCommand.CommandTimeout = DatabaseSettings.ConnectionTimeout;
            oCommand.Fill(dt);
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
        finally
        {
            connClose(oConn);
        }

        return true;

    }
    public bool sqlGetDataRow(ref DataRow dr, string sSQL, ref string ErrorMessage)
    {

        //clear it, in case someone is reusing a DataRow
        dr = null;

        try
        {
            DataTable dt = new DataTable();
            if (!sqlGetDataTable(ref dt, sSQL, ref ErrorMessage))
                return false;

            if (dt == null)
            {
                ErrorMessage = "No data returned. " + ErrorMessage;
                return true;
            }

			if (dt.Rows.Count > 0)
            {
                dr = dt.Rows[0];
                return true;
            }
            else
            {
                ErrorMessage = "No rows returned. " + ErrorMessage;
                return true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }

    }

    public bool sqlExecuteUpdate(string sSQL, ref string ErrorMessage)
    {
        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        MySqlConnection oConn = connOpen(ref ErrorMessage);
        if (oConn == null)
            return false;

        try
        {
            MySqlCommand oUpdateCommand = new MySqlCommand(sSQL, oConn);
            oUpdateCommand.CommandTimeout = DatabaseSettings.ConnectionTimeout;

            oUpdateCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
        finally
        {
            connClose(oConn);
        }

        return true;
    }

    private bool sqlExecuteScalar(ref object o, string sSQL, ref string ErrorMessage)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        MySqlConnection oConn = connOpen(ref ErrorMessage);
        if (oConn == null)
            return false;

        try
        {
            MySqlCommand oCommand = new MySqlCommand(sSQL, oConn);
            oCommand.CommandTimeout = DatabaseSettings.ConnectionTimeout;

            o = oCommand.ExecuteScalar();
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
        finally
        {
            connClose(oConn);
        }

        return true;

    }

    /// <summary>
    /// Returns a comma delimited, one dimensional array from a sql query.
    /// Row values from the first column are added to the list.
    /// </summary>
    public bool csvGetList(ref string sResult, string sSQL, ref string ErrorMessage, bool bQuoted = false)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        try
        {
            DataTable dt = new DataTable();
            if (!sqlGetDataTable(ref dt, sSQL, ref ErrorMessage))
                return false;

            DataRow dr = null;
            foreach (DataRow dr_loopVariable in dt.Rows)
            {
                dr = dr_loopVariable;
                if (bQuoted)
                {
                    sResult = sResult + "'" + dr[0].ToString() + "',";
                }
                else
                {
                    sResult = sResult + dr[0].ToString() + ",";
                }
            }
            if (sResult.Length > 0)
                sResult = sResult.Remove(sResult.Length - 1, 1);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Returns a comma delimited, two dimensional array from a sql query.
    /// </summary>
    public bool GetDelimitedData(ref string s, string sSQL, string sDelimiter, ref string ErrorMessage, bool bHeaderRow = true, bool bQuoted = false)
    {

        if (!ValidSQL(sSQL, ref ErrorMessage))
            return false;

        try
        {
            StringBuilder sb = new StringBuilder();
            DataTable dt = new DataTable();
            if (!sqlGetDataTable(ref dt, sSQL, ref ErrorMessage))
                return false;

            // add the headers (optionally)
            if (bHeaderRow)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    if (bQuoted)
                    {
                        sb.Append("\"" + dc.ColumnName + "\"" + sDelimiter);
                    }
                    else
                    {
                        sb.Append(dc.ColumnName + sDelimiter);
                    }
                }
                if (sb.Length > 0)
                {
                    sb.Replace(sDelimiter, "", sb.Length - sDelimiter.Length, 1);
                }
            }

            // then the data
            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i <= dr.ItemArray.Length - 1; i++)
                {
                    if (bQuoted)
                    {
                        sb.Append("\"" + dr.ItemArray[i].ToString() + "\"" + sDelimiter);
                    }
                    else
                    {
                        sb.Append(dr.ItemArray[i].ToString() + sDelimiter);
                    }
                }
            }
            if (sb.Length > 0)
            {
                sb.Replace(sDelimiter, "", sb.Length - sDelimiter.Length, 1);
            }

            s = sb.ToString();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = FormatError(ex.Message);
            return false;
        }
    }

    #endregion

    #region "Dataset Functions"
    public DataTable GetPageFromTable(DataTable dtSource, int iPageSize, int iPageNum, ref string sErr)
    {

        if (dtSource.Rows.Count > 0)
        {
            int iStart = iPageSize * (iPageNum - 1);
            int iEnd = iStart + (iPageSize);

            return GetRangeFromTable(dtSource, iStart, iEnd, ref sErr);
        }
        else
        {
            sErr = "Error: Unable to get range from table.  Source Datatable has no rows.";
            return null;
        }
    }
    public DataTable GetRangeFromTable(DataTable dtSource, int iStart, int iEnd, ref string sErr)
    {

        if (dtSource.Rows.Count > 0)
        {
            DataTable dt = new DataTable();
            dt = dtSource.Clone();
            dt.Rows.Clear();

            //If iPageSize > 1 Then

            //End If

            int i = 0;
            if (iStart <= 1)
                iStart = 0;
            if (iEnd >= dtSource.Rows.Count)
                iEnd = dtSource.Rows.Count - 1;

            for (i = iStart; i <= iEnd; i++)
            {
                dt.ImportRow(dtSource.Rows[i]);
            }

            return dt;
        }
        else
        {
            sErr = "Error: Unable to get range from table.  Source Datatable has no rows.";
            return null;
        }
    }
    #endregion

    #region "Logging"
    public void addSecurityLog(string sUserID, SecurityLogTypes LogType, SecurityLogActions Action, acObjectTypes ObjectType, string ObjectID, string LogMessage, ref string sErr, ref acTransaction oTrans)
    {
        string sSQL = null;
        string sTrimmedLog = LogMessage.Replace("'", "''").Trim();
        if (!string.IsNullOrEmpty(sTrimmedLog))
            if (sTrimmedLog.Length > 7999)
                sTrimmedLog = sTrimmedLog.Substring(0, 7998);

        sSQL = "insert into user_security_log (log_type, action, user_id, log_dt, object_type, object_id, log_msg)" +
            " values (" +
            "'" + LogType.ToString() + "'," +
            "'" + Action.ToString() + "'," +
            "'" + sUserID + "'," +
            "now()," + Convert.ToString((ObjectType == acObjectTypes.None ? "NULL" : ((int)ObjectType).ToString())) + "," +
            Convert.ToString((string.IsNullOrEmpty(ObjectID) ? "NULL" : "'" + ObjectID + "'")) + "," +
            "'" + sTrimmedLog + "'" +
            ")";

        if (oTrans == null)
        {
            //not in a transaction
            sqlExecuteUpdate(sSQL, ref sErr);
        }
        else
        {
            //use the provided transaction
            oTrans.Command.CommandText = sSQL;

            //if it fails, it will set the error flag
            oTrans.ExecUpdate(ref sErr);
        }
    }
    public void addSecurityLog(string sUserID, SecurityLogTypes LogType, SecurityLogActions Action, acObjectTypes ObjectType,
        string ObjectID, string LogMessage, ref  string sErr)
    {
        string sSQL = null;
        string sTrimmedLog = LogMessage.Replace("'", "''").Trim();
        if (!string.IsNullOrEmpty(sTrimmedLog))
            if (sTrimmedLog.Length > 7999)
                sTrimmedLog = sTrimmedLog.Substring(0, 7998);

        sSQL = "insert into user_security_log (log_type, action, user_id, log_dt, object_type, object_id, log_msg)" +
            " values (" +
            "'" + LogType.ToString() + "'," +
            "'" + Action.ToString() + "'," +
            "'" + sUserID + "'," +
            "now()," + Convert.ToString((ObjectType == acObjectTypes.None ? "NULL" : ((int)ObjectType).ToString())) + "," +
            Convert.ToString((string.IsNullOrEmpty(ObjectID) ? "NULL" : "'" + ObjectID + "'")) + "," +
            "'" + sTrimmedLog + "'" +
            ")";

        sqlExecuteUpdate(sSQL, ref sErr);

    }
    #endregion

    #region "Security Functions"
    public string DeCryptUsingKey(string sKey, string sStringToDecrypt)
    {
        //this function calls the blowfish decrypt, using the given key.

        if (!string.IsNullOrEmpty(sKey) && !string.IsNullOrEmpty(sStringToDecrypt))
        {
            return Blowfish.DecryptFromBase64(sKey, sStringToDecrypt);
        }
        else
        {
            return "";
        }
    }
    public string EnCryptUsingKey(string sKey, string sStringToEncrypt)
    {
        //this function calls the blowfish encrypt, using the given key.

        if (!string.IsNullOrEmpty(sKey) && !string.IsNullOrEmpty(sStringToEncrypt))
        {
            return Blowfish.EncryptToBase64(sKey, sStringToEncrypt);
        }
        else
        {
            return "";
        }
    }

    public string DeCrypt(string sStringToDecrypt)
    {
        //the "EncryptionKey" in the web.config is an encrypted version of the actual user-defined key
        //EncryptionKey is encrypted using our hardcoded, proprietary key.

        string sKey = Blowfish.DecryptFromBase64(BLOWFISH_KEY, DatabaseSettings.EnvironmentKey);
        if (!string.IsNullOrEmpty(sKey) && !string.IsNullOrEmpty(sStringToDecrypt))
        {
            return DeCryptUsingKey(sKey, sStringToDecrypt);
        }
        else
        {
            return "";
        }
    }
    public string EnCrypt(string sStringToEncrypt)
    {
        //the "EncryptionKey" in the web.config is an encrypted version of the actual user-defined key
        //EncryptionKey is encrypted using our hardcoded, proprietary key.

        string sKey = Blowfish.DecryptFromBase64(BLOWFISH_KEY, DatabaseSettings.EnvironmentKey);
        if (!string.IsNullOrEmpty(sKey) && !string.IsNullOrEmpty(sStringToEncrypt))
        {
            return EnCryptUsingKey(sKey, sStringToEncrypt);
        }
        else
        {
            return "";
        }
    }
    public bool PasswordIsComplex(string sPassword, ref string sMsg)
    {
        //checks a string to see if it is a complex enough password accoding to our rules.

        //get all of the values from the login_security_settings table
        System.Data.DataTable dtLoginSecurity = new System.Data.DataTable();
        string sSql = "select * from login_security_settings where id = 1";
        string sErr = null;
        if (!sqlGetDataTable(ref dtLoginSecurity, sSql, ref sErr))
        {
            sMsg = sErr;
            return false;
        }
        else
        {
            // the security settings are required, if no row exists give an error
            if (dtLoginSecurity.Rows.Count == 0)
            {
                sMsg = "System setup error, no security settings. Contact adminstrator.";
                return false;
            }
        }


        //check minimum password length
        int minLength = Convert.ToInt32(dtLoginSecurity.Rows[0]["pass_min_length"].ToString());
        if (sPassword.Length < minLength)
        {
            sMsg = "Passwords must be at least " + minLength.ToString() + " characters in length.";
            return false;
        }

        // check maximum password length
        int iMaxLength = Convert.ToInt32(dtLoginSecurity.Rows[0]["pass_max_length"].ToString());
        if (sPassword.Length > iMaxLength)
        {
            sMsg = "Passwords may not be more than " + iMaxLength.ToString() + " characters in length.";
            return false;
        }

        //check complexity
        string sCR = null;
        bool bCR = false;

        sCR = dtLoginSecurity.Rows[0]["pass_complexity"].ToString();

        if (string.IsNullOrEmpty(sCR) || (sCR != "1" & sCR != "0"))
        {
            sMsg = "Error: Login Defaults setting 'Password Complexity Setting' not found.";
            return false;
        }
        else
        {
            bCR = Convert.ToBoolean((sCR == "0" ? false : true));
        }
        if (bCR == true)
        {
            string sCause = "";

            try
            {
                const string lower = "abcdefghijklmnopqrstuvwxyz";
                const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string digits = "0123456789";
                const string special = "!@#$%^&*()_+-=[]{}\\/.,<>`~\\\\?";
                int iSecure = 0;

                if (sPassword.IndexOfAny(lower.ToCharArray()) > -1)
                {
                    iSecure += 1;
                }
                if (sPassword.IndexOfAny(upper.ToCharArray()) > -1)
                {
                    iSecure += 1;
                }
                if (sPassword.IndexOfAny(digits.ToCharArray()) > -1)
                {
                    iSecure += 1;
                }
                if (sPassword.IndexOfAny(special.ToCharArray()) > -1)
                {
                    iSecure += 1;
                }
                if (iSecure < 3)
                {
                    sMsg = "Password did not pass the complexity rules: The password must contain at least 3 from the following groups. Lower Case, Upper Case,0-9, and Special characters." + sCause;
                    return false;
                }
            }
            catch (Exception ex)
            {
                sMsg = ex.Message;
                return false;
            }


        }
        return true;
    }
    public bool PasswordInHistory(string sPassword, string sUserID, ref string sMsg)
    {

        // bugzilla 1347
        // check the user password history setting, and make sure the password was not used in the past x passwords
        // returns false (not used in history, or the setting is 0 which means we dont care about reuse)
        // returns true if the passwors was used in the last x passwords

        //get all of the values from the login_security_settings table
        System.Data.DataTable dtLoginSecurity = new System.Data.DataTable();
        string sSql = "select pass_history from login_security_settings where id = 1";
        string sErr = null;
        if (!sqlGetDataTable(ref dtLoginSecurity, sSql, ref sErr))
        {
            sMsg = sErr;
            return false;
        }
        else
        {
            // the security settings are required, if no row exists give an error
            if (dtLoginSecurity.Rows.Count == 0)
            {
                sMsg = "System setup error, no security settings. Contact adminstrator.";
                return false;
            }
        }

        try
        {
            //check password history setting
            int iPassHistory = Convert.ToInt32(dtLoginSecurity.Rows[0]["pass_history"].ToString());

            if (iPassHistory == 0)
            {
                return false;
            }
            else
            {
                // we have a history number of passwords
                // look into the past x user_password_history for this password
                // if it was used return true, otherwise false

                sSql = "select uph.change_time from user_password_history uph " +
                    "where uph.user_id = '" + sUserID + "' " +
                    "and uph.password in " +
                        "(select password from user_password_history " +
                        "where user_id = '" + sUserID + "' " +
                        "order by change_time desc limit " + iPassHistory + ") " +
                    "and uph.password = '" + sPassword +
                    "' limit 1";
                string sChangeTime = "";
                if (!sqlGetSingleString(ref sChangeTime, sSql, ref sErr))
                    return false;

                // if there is not a change time in the last x passwords its ok to use
                if (string.IsNullOrEmpty(sChangeTime))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            sMsg = ex.Message;
            return false;
        }
    }
    public string GenerateNewPassword()
    {
        int DEFAULT_MIN_PASSWORD_LENGTH = 8;
        int DEFAULT_MAX_PASSWORD_LENGTH = 16;

        // Define supported password characters divided into groups.
        // You can add (or remove) characters to (from) these groups.
        string PASSWORD_CHARS_LCASE = "abcdefgijkmnopqrstwxyz";
        string PASSWORD_CHARS_UCASE = "ABCDEFGHJKLMNPQRSTWXYZ";
        string PASSWORD_CHARS_NUMERIC = "23456789";
        string PASSWORD_CHARS_SPECIAL = "$?_&=!%";


        // Create a local array containing supported password characters
        // grouped by types. You can remove character groups from this
        // array, but doing so will weaken the password strength.
        char[][] charGroups = new char[][] 
        {
            PASSWORD_CHARS_LCASE.ToCharArray(),
            PASSWORD_CHARS_UCASE.ToCharArray(),
            PASSWORD_CHARS_NUMERIC.ToCharArray(),
            PASSWORD_CHARS_SPECIAL.ToCharArray()
        };

        // Use this array to track the number of unused characters in each
        // character group.
        int[] charsLeftInGroup = new int[charGroups.Length];

        // Initially, all characters in each group are not used.
        for (int i = 0; i < charsLeftInGroup.Length; i++)
            charsLeftInGroup[i] = charGroups[i].Length;

        // Use this array to track (iterate through) unused character groups.
        int[] leftGroupsOrder = new int[charGroups.Length];

        // Initially, all character groups are not used.
        for (int i = 0; i < leftGroupsOrder.Length; i++)
            leftGroupsOrder[i] = i;

        // Because we cannot use the default randomizer, which is based on the
        // current time (it will produce the same "random" number within a
        // second), we will use a random number generator to seed the
        // randomizer.

        // Use a 4-byte array to fill it with random bytes and convert it then
        // to an integer value.
        byte[] randomBytes = new byte[4];

        // Generate 4 random bytes.
        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        rng.GetBytes(randomBytes);

        // Convert 4 bytes into a 32-bit integer value.
        int seed = (randomBytes[0] & 0x7f) << 24 |
                    randomBytes[1] << 16 |
                    randomBytes[2] << 8 |
                    randomBytes[3];

        // Now, this is real randomization.
        Random random = new Random(seed);

        // This array will hold password characters.
        char[] password = null;

        // Allocate appropriate memory for the password.
        password = new char[random.Next(DEFAULT_MIN_PASSWORD_LENGTH, DEFAULT_MAX_PASSWORD_LENGTH)];

        // Index of the next character to be added to password.
        int nextCharIdx;

        // Index of the next character group to be processed.
        int nextGroupIdx;

        // Index which will be used to track not processed character groups.
        int nextLeftGroupsOrderIdx;

        // Index of the last non-processed character in a group.
        int lastCharIdx;

        // Index of the last non-processed group.
        int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

        // Generate password characters one at a time.
        for (int i = 0; i < password.Length; i++)
        {
            // If only one character group remained unprocessed, process it;
            // otherwise, pick a random character group from the unprocessed
            // group list. To allow a special character to appear in the
            // first position, increment the second parameter of the Next
            // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
            if (lastLeftGroupsOrderIdx == 0)
                nextLeftGroupsOrderIdx = 0;
            else
                nextLeftGroupsOrderIdx = random.Next(0,
                                                     lastLeftGroupsOrderIdx);

            // Get the actual index of the character group, from which we will
            // pick the next character.
            nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

            // Get the index of the last unprocessed characters in this group.
            lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

            // If only one unprocessed character is left, pick it; otherwise,
            // get a random character from the unused character list.
            if (lastCharIdx == 0)
                nextCharIdx = 0;
            else
                nextCharIdx = random.Next(0, lastCharIdx + 1);

            // Add this character to the password.
            password[i] = charGroups[nextGroupIdx][nextCharIdx];

            // If we processed the last character in this group, start over.
            if (lastCharIdx == 0)
                charsLeftInGroup[nextGroupIdx] =
                                          charGroups[nextGroupIdx].Length;
            // There are more unprocessed characters left.
            else
            {
                // Swap processed character with the last unprocessed character
                // so that we don't pick it until we process all characters in
                // this group.
                if (lastCharIdx != nextCharIdx)
                {
                    char temp = charGroups[nextGroupIdx][lastCharIdx];
                    charGroups[nextGroupIdx][lastCharIdx] =
                                charGroups[nextGroupIdx][nextCharIdx];
                    charGroups[nextGroupIdx][nextCharIdx] = temp;
                }
                // Decrement the number of unprocessed characters in
                // this group.
                charsLeftInGroup[nextGroupIdx]--;
            }

            // If we processed the last group, start all over.
            if (lastLeftGroupsOrderIdx == 0)
                lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
            // There are more unprocessed groups left.
            else
            {
                // Swap processed group with the last unprocessed group
                // so that we don't pick it until we process all groups.
                if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                {
                    int temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                    leftGroupsOrder[lastLeftGroupsOrderIdx] =
                                leftGroupsOrder[nextLeftGroupsOrderIdx];
                    leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                }
                // Decrement the number of unprocessed groups.
                lastLeftGroupsOrderIdx--;
            }
        }

        // Convert password characters into a string and return the result.
        return new string(password);
    }
    #endregion

    #region "Transaction Class"
    public class acTransaction
    {

        dataAccess dc = new dataAccess();
        public MySqlConnection Connection;
        public MySqlTransaction Transaction;
        public MySqlCommand Command;

        public int CONNECTION_TIMEOUT;
        public acTransaction(ref string ErrorMessage)
        {
            Connection = dc.connOpen(ref ErrorMessage);
            Transaction = BeginTransaction();
            Command = new MySqlCommand("", Connection, Transaction);
            Command.CommandTimeout = CONNECTION_TIMEOUT;
        }

        public bool ExecGetSingleInteger(ref int i, ref string sErr)
        {
            try
            {
                object o = new object();
                o = Command.ExecuteScalar();
                i = Convert.ToInt32(o);
                return true;
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
                sErr = ex.Message;
                return false;
            }
        }
        public bool ExecGetSingleString(ref string s, ref string sErr)
        {
            try
            {
                object o = new object();
                o = Command.ExecuteScalar();
                s = Convert.ToString((object.ReferenceEquals(o, DBNull.Value) ? "" : o));
                return true;
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
				sErr = ex.Message;
                return false;
            }
        }

        public bool ExecGetSingle(ref object ReturnValue, ref string sErr)
        {
            try
            {
                ReturnValue = Command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
                sErr = ex.Message;
                return false;
            }

            return true;
        }

        public bool ExecGetDataTable(ref DataTable dt, ref string sErr)
        {
            MySqlDataAdapter oDA = new MySqlDataAdapter(Command);
            try
            {
                oDA.SelectCommand.CommandTimeout = CONNECTION_TIMEOUT;
                oDA.Fill(dt);
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
                sErr = ex.Message;
                return false;
            }

            return true;
        }

        public bool ExecGetDataRow(ref DataRow dr, ref string sErr)
        {
            //clear it, in case someone is reusing a DataRow
            dr = null;

            try
            {
                MySqlDataAdapter oDA = new MySqlDataAdapter(Command);
                DataTable dt = new DataTable();

                oDA.SelectCommand.CommandTimeout = CONNECTION_TIMEOUT;
                oDA.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    dr = dt.Rows[0];
                    return true;
                }
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
                sErr = ex.Message;
                return false;
            }



            return true;
        }

        public bool ExecUpdate(ref string sErr)
        {
            try
            {
                Command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                Connection.Close();
				Connection.Dispose();
                sErr = ex.Message;
                return false;
            }

            return true;
        }

        public void Commit()
        {
            Transaction.Commit();
            if (Connection.State != ConnectionState.Closed)
                Connection.Close();
				Connection.Dispose();
        }

        public void RollBack()
        {
            Transaction.Rollback();
            if (Connection.State != ConnectionState.Closed)
                Connection.Close();
				Connection.Dispose();
        }

        private MySqlTransaction BeginTransaction()
        {
            MySqlTransaction oTrans = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
            return oTrans;
        }
    }
    #endregion

}
