# hotoke
Build your own search engine.

## [中文版](README_CN.md)

## [Online Demo](http://venyo.cn/)

## Quick Start

1. Download and unzip [Release](https://github.com/venyowong/hotoke/releases)
2. Run `./Hotoke` or double click `Hotoke.exe` in the root directory, and you will see this output
    ```
    Hosting environment: 
    Content root path: 
    Now listening on: http://127.0.0.1:11565
    Application started. Press Ctrl+C to shut down.
    ```
3. Browse http://127.0.0.1:11565

## Use the existing online demo

### HTTP API

`GET http://venyo.cn/search?keyword={keyword}&requestId=`

Calling this interface for the first time will return some search results first, as well as some related parameters of the search status. The reason for this is that the response times of multiple search engines are inconsistent. In order to speed up the response of the interface, the search results of the first search engine will be returned first. The result data structure is as follows:
```
{
	"requestId": "92f8d2eb-811d-4c22-abb8-ae06476a0372",
	"searched": 4,
	"finished": true,
	"results": [{
		"title": "Mary Venyo | LinkedIn",
		"url": "http://www.baidu.com/link?url=p07qw3oxp79g9S7KYyTyjGIEDPQwLjEXGAe5nJuQbguM0sj5b-m0X6am_DXe51rKSqB98j3pfE3QzrV4bp7_PK",
		"uri": "http://www.baidu.com/link?url=p07qw3oxp79g9S7KYyTyjGIEDPQwLjEXGAe5nJuQbguM0sj5b-m0X6am_DXe51rKSqB98j3pfE3QzrV4bp7_PK",
		"desc": null,
		"score": 0.772727251,
		"base": 11.0,
		"source": "baidu",
		"sources": ["baidu"]
	}, {
		"title": "Venyo - 个人中心- 云+社区- 腾讯云",
		"url": "https://cloud.tencent.com/developer/user/1352059",
		"uri": "https://cloud.tencent.com/developer/user/1352059",
		"desc": "Venyo 暂未填写个人简介 Java|C#|流计算服务|ASP.NET|数据库 在 Venyo 的专栏发表了文章 2018-07-272018-07-27 21:36:10 无需数据迁移的水平分库方案 在 Venyo 的专栏发...",
		"score": 0.7916667,
		"base": 11.0,
		"source": "360",
		"sources": ["360"]
	}]
}
```
In the above returned results, `searched` indicates the number of engines that have completed the search; `finished` indicates whether the search task has been completed; `requestId` is the id of the search request, and this field is mainly used for subsequent requests, that is, when `finished` is false, it indicates that the search task is not completed, and there may be other search results that can be called. You can continue to call the link below until finished is true.

`GET http://venyo.cn/search?keyword=&requestId={requestId}`

## Add search engine

### GenericSearch

Genericsearch is a general search template implemented by default for the project. It can grab search results through the configured XPath.

1. Add a config node in appsettings.json, like:
```
"bing": {
    "url": "https://www2.bing.com/search?q={keyword}&ensearch={ensearch}",
    "nodes": "//ol[@id='b_results']/li[@class='b_algo']",
    "link": ".//h2/a",
    "desc": ".//div[@class='b_caption']/p"
}
```
- `bing` is engine name
- `url` is a search link, and the parameters enclosed by curly brackets in the link are parameters. `keyword` indicates the keywords of user query. When the keywords are all in English, `lang` is en and `ensearch` is 1, otherwise `lang` is cn and `ensearch` is 0
- `nodes` is the XPath to extract each search result
- `link` is the XPath of hyperlink
- `desc` is the XPath of the result description

2. Add the above engine name into the `engines` config in appsettings.json. **Note: comma separated**

### Custom search engine

Another method is user-defined search class, which needs to implement ISearchEngine interface

1. Create a custom class to implement isearchengine interface, assuming the name is SearchEngine
2. Register SearchEngine
```
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new MetaSearcherConfig()
            .MapSearchEngine("name", new SearchEngine()));
    }
}
```
3. Add the above engine name into the `engines` config in appsettings.json. **Note: comma separated**

## 元搜索引擎

This project provides three kinds of meta search engines for selection, and the main difference is how to search.

### ParallelSearcher

ParallelSearcher is the default engine, which initiates query requests to all search engines at the same time when searching and returns when the first search result is obtained. To wait for all search engines to finish before returning results, you can call this interface `GET http://venyo.cn/search/all?keyword={keyword}`

### WeightFirstSearcher

WeightFirstSearcher will select the engine with the highest weight (the lowest value) in the search engine list, make a query request for it first, then initiate other query requests, and immediately return the existing search results. This engine does not support synchronous return of all results.

### CustomSearcher

CustomSearcher is positioned as a customizable meta search engine. At present, it only provides a advanced search engine list configuration, that is, which search engines are configured to have priority in parallel search. This engine does not support synchronous return of all results. The configuration is as follows：
```
"CustomSearcher": {
    "AdvancedList": [
        "bing",
        "baidu"
    ]
}
```