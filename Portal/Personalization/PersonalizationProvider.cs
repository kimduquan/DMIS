using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using System.Configuration.Provider;
using System.Security.Permissions;
using System.Web;
using ContentRepository;
using ContentRepository.Storage;
using System.IO;
using ContentRepository.Storage.Security;
using SNP = Portal;
using System.Security.Cryptography;
using System.Web.Hosting;
using System.Data;
using System.Diagnostics;
using System.Collections;
using System.Web.UI;
using System.Collections.Specialized;
using Services.Instrumentation;
using Diagnostics;

namespace Portal.Personalization
{
    public class PersonalizationProvider : System.Web.UI.WebControls.WebParts.PersonalizationProvider
    {
        private string _applicationName;
        public override string ApplicationName
        {
            get
            {
                if (string.IsNullOrEmpty(_applicationName))
                {
                    _applicationName = HostingEnvironment.ApplicationVirtualPath;
                }
                return _applicationName;
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            
            if (config == null)
				throw new ArgumentNullException("config");

            if (String.IsNullOrEmpty(name))
				name = "PersonalizationProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
				config.Add("description", "Personalization Provider");
            }

            base.Initialize(name, config);

            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }
        }

        public override void SavePersonalizationState(PersonalizationState state)
        {
            //  Take care of the unexpected personalization behavior, when User scope is not used, we need to ensure that scope is able to be changed to shared scope.
            //  1. check web.config authorization section that enterSharedScope is allowed to * users. 
            //  2. misconfigured authorization section leads to data loss due to the personalization data becomes user scope before saving the state

			if (state.WebPartManager.Personalization.Scope == PersonalizationScope.User)
            {
                try
                {
                    Portal.Personalization.PersonalizationProvider.WriteLog(string.Format("SavePersonalizationState --> Personalization.Scope = PersonalizationScope.User: trying to ToggleScope();"));
                    state.WebPartManager.Personalization.ToggleScope();
                }
                catch (InvalidOperationException exc) //logged
                {
                    Logger.WriteException(exc);
                }
                catch (ArgumentOutOfRangeException exc) //logged
                {
                    Logger.WriteException(exc);
                }
            }


            try
            {
                base.SavePersonalizationState(state);
            }
            catch (InvalidContentActionException ex)
            {
                //not enough permissions to save page state, never mind
                Logger.WriteException(ex);
            }
            
        }

        protected override void LoadPersonalizationBlobs(WebPartManager webPartManager, string path, string userName, ref byte[] sharedDataBlob, ref byte[] userDataBlob)
        {
            sharedDataBlob = Portal.Personalization.PersonalizationProvider.LoadBlob(path);
        }
        protected override void SavePersonalizationBlob(WebPartManager webPartManager, string path, string userName, byte[] dataBlob)
        {
            Portal.Personalization.PersonalizationProvider.SaveBlob(path, dataBlob);
        }
        public override PersonalizationStateInfoCollection FindState(PersonalizationScope scope, PersonalizationStateQuery query, int pageIndex, int pageSize, out int totalRecords)
        {
			throw new NotSupportedException("PersonalizationProvider.FindState");
        }

        ////////////////////////////////////////////// NOT SUPPORTED //////////////////////////////////////////////

        public override int GetCountOfState(PersonalizationScope scope, PersonalizationStateQuery query)
        {
            return 0;
        }
        protected override void ResetPersonalizationBlob(WebPartManager webPartManager, string path, string userName)
        {
            var p = SNP.Page.Current;
            p.PersonalizationSettings.SetStream(null);
            p.Save();
        }
        public override int ResetState(PersonalizationScope scope, string[] paths, string[] usernames)
        {
			Portal.Personalization.PersonalizationProvider.WriteLog(String.Concat("PersonalizationProvider.ResetState called."));
            return 0;
        }
        public override int ResetUserState(string path, DateTime userInactiveSinceDate)
        {
			Portal.Personalization.PersonalizationProvider.WriteLog(String.Concat("PersonalizationProvider.ResetUserState called."));
            return 0;
        }

        ////////////////////////////////////////////// STATIC METHODS /////////////////////////////////////////////

        internal static void WriteLog(string message)
        {
            using (EventLog eventLog = new EventLog("DMIS"))
            {
                eventLog.Source = "Portal";
                eventLog.WriteEntry(message, EventLogEntryType.Information);
                eventLog.Close();
            }
        }

        public static void SaveBlob(string path, byte[] sharedDataBlob)
        {
			if (SNP.Page.Current == null || String.IsNullOrEmpty(SNP.Page.Current.Path))
            {
                throw new PersonalizationException("No page loaded.");
            }

			var p = SNP.Page.Current;
            if (p.PersonalizationSettings != null)
            {
                if (sharedDataBlob.Length == 0)
					Portal.Personalization.PersonalizationProvider.WriteLog(String.Format("SaveBlob --> missing personalization settings at {0} path.", SNP.Page.Current.Path));

                p.PersonalizationSettings.SetStream(new MemoryStream(sharedDataBlob));
            }
            else
            {
                BinaryData binaryPers = new BinaryData();
                binaryPers.SetStream(new MemoryStream(sharedDataBlob));
                p.PersonalizationSettings = binaryPers;
            }
            p.Save();
        }
        public static byte[] LoadBlob(string path)
        {
            // Elevation: Personalization settings is a technical binary that 
            // should be loaded, regardless of the current users permissions.
            // Permission check should happen a lot earlier.
            using (new SystemAccount())
            {
                var p = Page.Current;
                if (p == null || String.IsNullOrEmpty(p.Path))
                    throw new PersonalizationException("No page loaded.");

                Stream personalizationSettingsStream = null;
                if (p.PersonalizationSettings != null)
                    personalizationSettingsStream = p.PersonalizationSettings.GetStream();

                if (personalizationSettingsStream == null)
                    return null;

                var buffer = new byte[personalizationSettingsStream.Length];
                personalizationSettingsStream.Seek(0, SeekOrigin.Begin);
                personalizationSettingsStream.Read(buffer, 0, (int)personalizationSettingsStream.Length);
                return buffer; 
            }
        }

    }
}