using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Patu.Processor;
using Patu.Config;
using YamlDotNet.Serialization;

namespace Patu
{
    public class PatuCrawler : IDisposable
    {
        private static ILog Logger = Utility.GetLogger(typeof(PatuCrawler));

        protected CancellationTokenSource cancellation;
        protected IProcessor processor;
        protected string id = Guid.NewGuid().ToString();
        protected PatuCrawlTask task;

        public string Id{get => id;}
        public bool Active{get;private set;} = false;
        public bool Running{get => (!this.task?.Finished) ?? false;}
        public PatuConfig Config{get;set;}

        /// <summary>
        /// If processor is null, Patu will use default processor: PatuProcessor.
        /// </summary>
        /// <param name="processor"></param>
        public PatuCrawler(IProcessor processor = null)
        {
            if(processor != null)
            {
                this.processor = processor;
            }
            else
            {
                this.processor = new PatuProcessor();
            }
            this.LoadConfig();
        }

        public PatuCrawler(PatuConfig config, IProcessor processor = null)
        {
            if(config == null)
            {
                throw new TypeInitializationException(typeof(PatuCrawler).FullName, 
                    new ArgumentNullException(nameof(config)));
            }
            this.Config = config;

            if(processor != null)
            {
                this.processor = processor;
            }
            else
            {
                this.processor = new PatuProcessor();
            }
        }

        public virtual void AddSeeds(params string[] seeds)
        {
            this.Config?.Seeds.AddRange(seeds);
        }

        public virtual PatuCrawlTask GenerateTask()
        {
            return new PatuCrawlTask(this.Config, this.Config.Seeds, 
                this.processor, this.cancellation);
        }

        public virtual Task Start()
        {
            Logger.Info($"PatuCrawler({this.Id}) is running...");
            if(this.Config.Seeds == null || this.Config.Seeds.Count == 0)
            {
                Logger.Warn("The count of seeds is 0.");
                return Task.CompletedTask;
            }

            this.Active = true;
            this.cancellation = new CancellationTokenSource();
            return Task.Run(() =>
            {
                while(true)
                {
                    this.task = this.GenerateTask();
                    this.task.Start();
                    Utility.Sleep(this.Config.IntervalMills);
                }
            }, this.cancellation.Token);
        }

        public virtual void Stop()
        {
            Logger.Info($"PatuCrawler({this.Id}) is stopping...");
            this.Active = false;
            try
            {
                this.cancellation.Cancel();
            }
            catch{}
        }

        public virtual void Restart()
        {
            this.Stop();
            this.Start();
        }

        public virtual void Dispose()
        {
            this.Active = false;
            try
            {
                this.cancellation.Cancel();
            }
            catch{}
            try
            {
                this.cancellation.Dispose();
            }
            catch{}
        }

        private void LoadConfig()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "patu.yml");
            using(var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using(var reader = new StreamReader(stream))
                {
                    this.Config = new Deserializer().Deserialize<PatuConfig>(reader.ReadToEnd());
                    if(this.Config == null)
                    {
                        throw new Exception("Can't deserialize from patu.xml to PatuConfig");
                    }
                }
            }
        }
    }
}