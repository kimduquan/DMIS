using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using ContentRepository.Storage;
using ContentRepository.Schema;
using ContentRepository.i18n;
using System.Globalization;

namespace Messaging
{
    public class MessageTemplate
    {
        private CultureInfo _cultureInfo;
        public MessageTemplate(string langCode)
        {
            _cultureInfo = CultureInfo.CreateSpecificCulture(langCode);
        }

        private const string CLASSNAME = "MessageTemplate";

        public const string IMMEDIATELYSUBJECT = "ImmediatelySubject";
        public const string DAILYSUBJECT = "DailySubject";
        public const string WEEKLYSUBJECT = "WeeklySubject";
        public const string MONTHLYSUBJECT = "MonthlySubject";

        public const string IMMEDIATELYHEADER = "ImmediatelyHeader";
        public const string DAILYHEADER = "DailyHeader";
        public const string WEEKLYHEADER = "WeeklyHeader";
        public const string MONTHLYHEADER = "MonthlyHeader";

        public const string IMMEDIATELYFOOTER = "ImmediatelyFooter";
        public const string DAILYFOOTER = "DailyFooter";
        public const string WEEKLYFOOTER = "WeeklyFooter";
        public const string MONTHLYFOOTER = "MonthlyFooter";

        public const string CREATEDTEMPLATE = "DocumentCreated";
        public const string MAJORVERSIONMODIFIEDTEMPLATE = "DocumentMajorVersionModified";
        public const string MINORVERSIONMODIFIEDTEMPLATE = "DocumentMinorVersionModified";
        public const string COPIEDFROMTEMPLATE = "DocumentCopiedFrom";
        public const string MOVEDFROMTEMPLATE = "DocumentMovedFrom";
        public const string MOVEDTOTEMPLATE = "DocumentMovedTo";
        public const string RENAMEDFROMTEMPLATE = "DocumentRenamedFrom";
        public const string RENAMEDTOTEMPLATE = "DocumentRenamedTo";
        public const string DELETEDTEMPLATE = "DocumentDeleted";
        public const string RESTOREDTEMPLATE = "DocumentRestored";

        //========

        public string ImmediatelySubject { get { return ResourceManager.Current.GetString(CLASSNAME, IMMEDIATELYSUBJECT, _cultureInfo); } }
        public string DailySubject { get { return ResourceManager.Current.GetString(CLASSNAME, DAILYSUBJECT, _cultureInfo); } }
        public string WeeklySubject { get { return ResourceManager.Current.GetString(CLASSNAME, WEEKLYSUBJECT, _cultureInfo); } }
        public string MonthlySubject { get { return ResourceManager.Current.GetString(CLASSNAME, MONTHLYSUBJECT, _cultureInfo); } }

        public string ImmediatelyHeader { get { return ResourceManager.Current.GetString(CLASSNAME, IMMEDIATELYHEADER, _cultureInfo); } }
        public string DailyHeader { get { return ResourceManager.Current.GetString(CLASSNAME, DAILYHEADER, _cultureInfo); } }
        public string WeeklyHeader { get { return ResourceManager.Current.GetString(CLASSNAME, WEEKLYHEADER, _cultureInfo); } }
        public string MonthlyHeader { get { return ResourceManager.Current.GetString(CLASSNAME, MONTHLYHEADER, _cultureInfo); } }

        public string ImmediatelyFooter { get { return ResourceManager.Current.GetString(CLASSNAME, IMMEDIATELYFOOTER, _cultureInfo); } }
        public string DailyFooter { get { return ResourceManager.Current.GetString(CLASSNAME, DAILYFOOTER, _cultureInfo); } }
        public string WeeklyFooter { get { return ResourceManager.Current.GetString(CLASSNAME, WEEKLYFOOTER, _cultureInfo); } }
        public string MonthlyFooter { get { return ResourceManager.Current.GetString(CLASSNAME, MONTHLYFOOTER, _cultureInfo); } }

        public string CreatedTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, CREATEDTEMPLATE, _cultureInfo); } }
        public string MajorVersionModifiedTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, MAJORVERSIONMODIFIEDTEMPLATE, _cultureInfo); } }
        public string MinorVersionModifiedTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, MINORVERSIONMODIFIEDTEMPLATE, _cultureInfo); } }
        public string CopiedFromTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, COPIEDFROMTEMPLATE, _cultureInfo); } }
        public string MovedFromTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, MOVEDFROMTEMPLATE, _cultureInfo); } }
        public string MovedToTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, MOVEDTOTEMPLATE, _cultureInfo); } }
        public string RenamedFromTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, RENAMEDFROMTEMPLATE, _cultureInfo); } }
        public string RenamedToTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, RENAMEDTOTEMPLATE, _cultureInfo); } }
        public string DeletedTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, DELETEDTEMPLATE, _cultureInfo); } }
        public string RestoredTemplate { get { return ResourceManager.Current.GetString(CLASSNAME, RESTOREDTEMPLATE, _cultureInfo); } }
    }
}
