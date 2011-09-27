#region Disclaimer / License
// Copyright (C) 2011, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Duplicati.Library.Interface;

namespace Duplicati.Library.Backend
{
    public class FTP : IBackend_v2, IStreamingBackend, IBackendGUI
    {
        private System.Net.NetworkCredential m_userInfo;
        private string m_url;
        Dictionary<string, string> m_options;

        private bool m_useSSL = false;
        private bool m_defaultPassive = true;
        private bool m_passive = false;

        public FTP()
        {
        }

        public FTP(string url, Dictionary<string, string> options)
        {
            //This can be made better by keeping a single ftp stream open,
            //unfortunately the .Net model does not allow this as the request is 
            //bound to a single url (path+file).
            //
            //To fix this, a thirdparty FTP library is required,
            //this would also allow a fix for the FTP servers
            //that only support SSL during authentication, not during transfers
            //
            //If you have experience with a stable open source .Net FTP library,
            //please let the Duplicati authors know

            Uri u = new Uri(url);

            if (!string.IsNullOrEmpty(u.UserInfo))
            {
                m_userInfo = new System.Net.NetworkCredential();
                if (u.UserInfo.IndexOf(":") >= 0)
                {
                    m_userInfo.UserName = u.UserInfo.Substring(0, u.UserInfo.IndexOf(":"));
                    m_userInfo.Password = u.UserInfo.Substring(u.UserInfo.IndexOf(":") + 1);
                }
                else
                {
                    m_userInfo.UserName = u.UserInfo;
                    if (options.ContainsKey("ftp-password"))
                        m_userInfo.Password = options["ftp-password"];
                }
            }
            else
            {
                if (options.ContainsKey("ftp-username"))
                {
                    m_userInfo = new System.Net.NetworkCredential();
                    m_userInfo.UserName = options["ftp-username"];
                    if (options.ContainsKey("ftp-password"))
                        m_userInfo.Password = options["ftp-password"];
                }
            }

            m_useSSL = Utility.Utility.ParseBoolOption(m_options, "use-ssl");

            m_options = options;
            m_url = url;
            if (!m_url.EndsWith("/"))
                m_url += "/";

            //HACK: We modify the commandline options to alter the setting it the ftp backend is loaded
            if (!options.ContainsKey("list-verify-uploads"))
                options.Add("list-verify-uploads", "true");

            if (Utility.Utility.ParseBoolOption(m_options, "ftp-passive"))
            {
                m_defaultPassive = false;
                m_passive = true;
            }
            if (Utility.Utility.ParseBoolOption(m_options, "ftp-regular"))
            {
                m_defaultPassive = false;
                m_passive = false;
            }
        }

        #region Regular expression to parse list lines
        //Regexps found here: http://www.dotnetfunda.com/articles/article125.aspx
        //Modified to allow hyphens in username and groupname
        internal readonly static Regex[] PARSEFORMATS =
        {
            new Regex(@"(?<dir>[\-d])(?<permission>([\-r][\-w][\-xs]){3})\s+\d+\s+(?<groupname>\S+)\s+(?<username>\S+)\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{4})\s+(?<name>.+)"),
            new Regex(@"(?<dir>[\-d])(?<permission>([\-r][\-w][\-xs]){3})\s+(?<groupname>\d+)\s+(?<username>\d+)\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{4})\s+(?<name>.+)"),
            new Regex(@"(?<dir>[\-d])(?<permission>([\-r][\-w][\-xs]){3})\s+(?<groupname>\d+)\s+(?<username>\d+)\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{1,2}:\d{2})\s+(?<name>.+)"),
            new Regex(@"(?<dir>[\-d])(?<permission>([\-r][\-w][\-xs]){3})\s+\d+\s+(?<groupname>\S+)\s+(?<username>\S+)\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{1,2}:\d{2})\s+(?<name>.+)"),
            new Regex(@"(?<dir>[\-d])(?<permission>([\-r][\-w][\-xs]){3})(\s+)(?<size>(\d+))(\s+)(?<ctbit>(\w+\s\w+))(\s+)(?<size2>(\d+))\s+(?<timestamp>\w+\s+\d+\s+\d{2}:\d{2})\s+(?<name>.+)"),
            new Regex(@"(?<timestamp>\d{2}\-\d{2}\-\d{2}\s+\d{2}:\d{2}[Aa|Pp][mM])\s+(?<dir>\<\w+\>){0,1}(?<size>\d+){0,1}\s+(?<name>.+)"),
            new Regex(@"([<timestamp>]*\d{2}\-\d{2}\-\d{2}\s+\d{2}:\d{2}[Aa|Pp][mM])\s+([<dir>]*\<\w+\>){0,1}([<size>]*\d+){0,1}\s+([<name>]*.+)")
        };
        #endregion
        
        private static Match MatchLine(string line)
        {
            Match m = null;
            foreach (Regex s in PARSEFORMATS)
                if ((m = s.Match(line)).Success)
                    return m;

            return null;
        }

        public static FileEntry ParseLine(string line)
        {
            Match m = MatchLine(line);
            if (m == null)
                return null;

            FileEntry f = new FileEntry(m.Groups["name"].Value);

            string time = m.Groups["timestamp"].Value;
            string dir = m.Groups["dir"].Value;

			//Unused
            //string permission = m.Groups["permission"].Value;

            if (dir != "" && dir != "-")
                f.IsFolder = true;
            else
                f.Size = long.Parse(m.Groups["size"].Value);

            DateTime t;
            if (DateTime.TryParse(time, out t))
                f.LastAccess = f.LastModification = t;

            return f;
        }

        #region IBackendInterface Members

        public string DisplayName
        {
            get { return Strings.FTPBackend.DisplayName; }
        }

        public string ProtocolKey
        {
            get { return "ftp"; }
        }

        public bool SupportsStreaming
        {
            get { return true; }
        }

        public List<IFileEntry> List()
        {
            System.Net.FtpWebRequest req = CreateRequest("");
            req.Method = System.Net.WebRequestMethods.Ftp.ListDirectoryDetails;
            req.UseBinary = false;

            try
            {
                List<IFileEntry> lst = new List<IFileEntry>();
                using (System.Net.WebResponse resp = req.GetResponse())
                using (System.IO.Stream rs = resp.GetResponseStream())
                using (System.IO.StreamReader sr = new System.IO.StreamReader(new StreamReadHelper(rs)))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        FileEntry f = ParseLine(line);
                        if (f != null)
                            lst.Add(f);
                    }
                }
                return lst;
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response as System.Net.FtpWebResponse != null && (wex.Response as System.Net.FtpWebResponse).StatusCode == System.Net.FtpStatusCode.ActionNotTakenFileUnavailable)
                    throw new Interface.FolderMissingException(string.Format(Strings.FTPBackend.MissingFolderError, req.RequestUri.PathAndQuery, wex.Message), wex);
                else
                    throw;
            }
        }

        public void Put(string remotename, System.IO.Stream input)
        {
            System.Net.FtpWebRequest req = null;
            try
            {
                req = CreateRequest(remotename);
                req.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                req.UseBinary = true;

                using (System.IO.Stream rs = req.GetRequestStream())
                    Utility.Utility.CopyStream(input, rs, true);
            }
            catch (System.Net.WebException wex)
            {
                if (req != null && wex.Response as System.Net.FtpWebResponse != null && (wex.Response as System.Net.FtpWebResponse).StatusCode == System.Net.FtpStatusCode.ActionNotTakenFileUnavailable)
                    throw new Interface.FolderMissingException(string.Format(Strings.FTPBackend.MissingFolderError, req.RequestUri.PathAndQuery, wex.Message), wex);
                else
                    throw;
            }
        }

        public void Put(string remotename, string localname)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(localname, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                Put(remotename, fs);
        }

        public void Get(string remotename, System.IO.Stream output)
        {
            System.Net.FtpWebRequest req = CreateRequest(remotename);
            req.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;
            req.UseBinary = true;

            using (System.Net.WebResponse resp = req.GetResponse())
            using (System.IO.Stream rs = resp.GetResponseStream())
                Utility.Utility.CopyStream(rs, output, false);
        }

        public void Get(string remotename, string localname)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(localname, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                Get(remotename, fs);
        }

        public void Delete(string remotename)
        {
            System.Net.FtpWebRequest req = CreateRequest(remotename);
            req.Method = System.Net.WebRequestMethods.Ftp.DeleteFile;
            using (req.GetResponse())
            { }
        }

        public IList<ICommandLineArgument> SupportedCommands
        {
            get
            {
                return new List<ICommandLineArgument>(new ICommandLineArgument[] {
                    new CommandLineArgument("ftp-passive", CommandLineArgument.ArgumentType.Boolean, Strings.FTPBackend.DescriptionFTPPassiveShort, Strings.FTPBackend.DescriptionFTPPassiveLong, "false"),
                    new CommandLineArgument("ftp-regular", CommandLineArgument.ArgumentType.Boolean, Strings.FTPBackend.DescriptionFTPActiveShort, Strings.FTPBackend.DescriptionFTPActiveLong, "true"),
                    new CommandLineArgument("ftp-password", CommandLineArgument.ArgumentType.String, Strings.FTPBackend.DescriptionFTPPasswordShort, Strings.FTPBackend.DescriptionFTPPasswordLong),
                    new CommandLineArgument("ftp-username", CommandLineArgument.ArgumentType.String, Strings.FTPBackend.DescriptionFTPUsernameShort, Strings.FTPBackend.DescriptionFTPUsernameLong),
                    new CommandLineArgument("use-ssl", CommandLineArgument.ArgumentType.Boolean, Strings.FTPBackend.DescriptionUseSSLShort, Strings.FTPBackend.DescriptionUseSSLLong),
                });
            }
        }

        public string Description
        {
            get
            {
                return Strings.FTPBackend.Description;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (m_userInfo != null)
                m_userInfo = null;
            if (m_options != null)
                m_options = null;
        }

        #endregion

        private System.Net.FtpWebRequest CreateRequest(string remotename)
        {
            System.Net.FtpWebRequest req = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(m_url + remotename);

            if (m_userInfo != null)
                req.Credentials = m_userInfo;
            req.KeepAlive = false;

            if (!m_defaultPassive)
                req.UsePassive = m_passive;

            if (m_useSSL)
                req.EnableSsl = m_useSSL;

            //Set half-hour total timeout and 5 minutes acticity timeout
            req.Timeout = (int)TimeSpan.FromMinutes(30).TotalMilliseconds;
            req.ReadWriteTimeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;

            return req;
        }

        #region IBackend_v2 Members

        public void Test()
        {
            List();
        }

        public void CreateFolder()
        {
            System.Net.FtpWebRequest req = CreateRequest("");
            req.Method = System.Net.WebRequestMethods.Ftp.MakeDirectory;
            req.KeepAlive = false;
            using (req.GetResponse())
            { }
        }

        #endregion

        #region IBackendGUI Members

        public string PageTitle
        {
            get { return FTPUI.PageTitle; }
        }

        public string PageDescription
        {
            get { return FTPUI.PageDescription; }
        }

        public System.Windows.Forms.Control GetControl(IDictionary<string, string> applicationSettings, IDictionary<string, string> options)
        {
            return new FTPUI(options);
        }

        public void Leave(System.Windows.Forms.Control control)
        {
            ((FTPUI)control).Save(false);
        }

        public bool Validate(System.Windows.Forms.Control control)
        {
            return ((FTPUI)control).Save(true);
        }

        public string GetConfiguration(IDictionary<string, string> applicationSettings, IDictionary<string, string> guiOptions, IDictionary<string, string> commandlineOptions)
        {
            return FTPUI.GetConfiguration(guiOptions, commandlineOptions);
        }

        #endregion

        /// <summary>
        /// Private helper class to fix a bug with the StreamReader
        /// </summary>
        private class StreamReadHelper : Utility.OverrideableStream
        {
            /// <summary>
            /// Once the stream has returned 0 as the read count it is disposed
            /// in the FtpRequest, and subsequent read requests will throw an ObjectDisposedException
            /// </summary>
            private bool m_empty = false;

            /// <summary>
            /// Basic initialization, just pass the stream to the super class
            /// </summary>
            /// <param name="stream"></param>
            public StreamReadHelper(System.IO.Stream stream)
                : base(stream)
            {
            }

            /// <summary>
            /// Override the read function to make sure that we only return less than the requested amount of data if the stream is exhausted
            /// </summary>
            /// <param name="buffer">The buffer to place data in</param>
            /// <param name="offset">The offset into the buffer to start at</param>
            /// <param name="count">The number of bytes to read</param>
            /// <returns>The number of bytes read</returns>
            public override int Read(byte[] buffer, int offset, int count)
            {
                int readCount = 0;
                int a;
                
                while(!m_empty && count > 0)
                {
                    a = base.Read(buffer, offset, count);
                    readCount += a;
                    count -= a;
                    offset += a;
                    m_empty = a == 0;
                }

                return readCount;
            }
        }
    }
}