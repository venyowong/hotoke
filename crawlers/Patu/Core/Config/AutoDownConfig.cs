using System.Collections.Generic;

namespace Patu.Config
{
    public class AutoDownConfig
    {
        public bool EnableAutoDown{get;set;}

        public int MaxTolerableRate{get;set;}

        public string SmtpHost{get;set;}

        public int SmtpPort{get;set;}

        public string SendMail{get;set;}

        public string SendPassword{get;set;}

        public string ReceiveMail{get;set;}

        public List<string> CopyMails{get;set;}
    }
}