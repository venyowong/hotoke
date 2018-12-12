using CommandLine;
using Octokit;
using System;

namespace GithubCrawler
{
    class Program
    {
        class Options
        {
            [Option('o', Required = true, HelpText = "Owner, user or organization")]
            public string Owner{get;set;}

            [Option('r', Required = true, HelpText = "repository name")]
            public string Repo{get;set;}

            [Option('t', Required = false, Default = "issue", HelpText = "Target, issue or other")]
            public string Target{get;set;}
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    var client = new GitHubClient(new ProductHeaderValue("Github-Crawler"));
                    var basicAuth = new Credentials("venyowang@163.com", "venyo283052");
                    client.Credentials = basicAuth;

                    switch(options.Target.ToLower())
                    {
                        case "issue":
                        CrawleIssues(client, options);
                        break;
                    }
                });
        }

        static void CrawleIssues(GitHubClient client, Options options)
        {
            new IssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Closed,
                Since = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14))
            };
            var issues = client.Issue.GetAllForRepository(options.Owner, options.Repo).Result;
        }
    }
}
