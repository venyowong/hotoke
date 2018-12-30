using System;
using System.Collections.Generic;
using log4net;

namespace Patu.Config
{
    public class PatuConfig
    {
        private static ILog Logger = Utility.GetLogger(typeof(PatuConfig));

        public List<string> Seeds{get;set;}

        public string Interval{get;set;}

        private long intervalMills = 0;
        public long IntervalMills
        {
            get
            {
                if(this.intervalMills == 0)
                {
                    long radix = 0;
                    if(this.Interval != null && this.Interval.Length >= 2)
                    {
                        var unit = this.Interval[this.Interval.Length - 1];
                        if(unit == 's')
                        {
                            radix = 1000;
                        }
                        else if(unit == 'm')
                        {
                            radix = 60000;// 60 * 1000;
                        }
                        else if(unit == 'h')
                        {
                            radix = 3600000;// 60 * 60 * 1000
                        }
                        else if(unit == 'd')
                        {
                            radix = 86400000;// 24 * 60 * 60 * 1000
                        }
                        else if(unit == 'M')
                        {
                            radix = 2592000000;// 30 * 24 * 60 * 60 * 1000
                        }
                        else if(unit == 'y')
                        {
                            radix = 31536000000;// 365 * 24 * 60 * 60 * 1000
                        }
                    }
                    if(radix != 0)
                    {
                        try
                        {
                            int num = int.Parse(this.Interval.Substring(0, this.Interval.Length - 1));
                            this.intervalMills = num * radix;
                        }
                        catch(Exception e)
                        {
                            Logger.Warn("Catched an exception when getting interval", e);
                        }
                    }

                    if(intervalMills == 0)
                    {
                        Logger.Warn("Can't get interval from patu.yml, using 1h as default value.");
                        this.intervalMills = 3600000;// 60 * 60 * 1000
                    }
                }
                return this.intervalMills;
            }
        }

        public int BloomSize{get;set;} = 10000000;

        public int ExpectedPageCount{get;set;} = 500000;

        public int CrawlDeepth{get;set;}

        public List<string> TargetHosts{get;set;}

        public string Name{get;set;}

        public AutoDownConfig AutoDown{get;set;}

        public override int GetHashCode()
        {
            if(this.Seeds == null || this.Seeds.Count == 0)
            {
                return -1;
            }
            return string.Join(";", this.Seeds).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is PatuConfig config)
            {
                return this.GetHashCode() == config.GetHashCode();
            }

            return base.Equals(obj);
        }
    }
}