# hotoke
构建自己的搜索引擎

## [English Version](README.md)

## [线上 Demo](http://venyo.cn/hotoke/)

## 快速启动

1. 下载并解压 [Release](https://github.com/venyowong/hotoke/releases)
2. 进入解压目录，命令行运行 `./Hotoke` 或双击 `Hotoke.exe`, 你将会看到类似以下的输出：
    ```
    Hosting environment: 
    Content root path: 
    Now listening on: http://127.0.0.1:11565
    Application started. Press Ctrl+C to shut down.
    ```
3. 打开浏览器，访问 http://127.0.0.1:11565

## 接入线上 Demo 接口

如果你不想自己搭建环境，但是又认为本项目的接口对你有用的话，可以直接接入线上 Demo 的接口，以下介绍接入方式：

### HTTP API

`GET http://venyo.cn/hotoke/search?keyword={keyword}&requestId=`

首次调用该接口，将会先返回部分搜索结果，以及一些搜索状态的相关参数。这么做的原因是，多个搜索引擎的响应时间不一致，为了加快接口的响应速度，会先返回第一个搜索引擎的搜索结果。返回结果数据结构如下：
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
在以上返回结果中，searched 表示已完成搜索的引擎数量，finished 表示是否已完成本次搜索任务，requestId 为本次搜索请求的 id，该字段主要用来进行后续请求，即当 finished 为 false 时，表示搜索任务未完成，可能还有其他搜索结果，可调用
`GET http://venyo.cn/hotoke/search?keyword={keyword}&requestId={首次调用接口返回的 requestId}`
继续获取搜索结果，直至 finished 为 true。

## 接入搜索引擎

### GenericSearch

GenericSearch 为项目默认实现的通用搜索模板，它能通过配置的 XPATH 抓取搜索结果。

1. 首先在 appsettings.json 配置文件中，加入一个配置节点，格式如下：
```
"bing": {
    "url": "https://www2.bing.com/search?q={keyword}&ensearch={ensearch}",
    "nodes": "//ol[@id='b_results']/li[@class='b_algo']",
    "link": ".//h2/a",
    "desc": ".//div[@class='b_caption']/p"
}
```
- bing 为搜索引擎名称
- url 为搜索链接，链接中由花括号包围起来的为参数，keyword 表示用户查询关键词，当关键词全为英文时，lang 为 en、ensearch 为 1，否则lang 为 cn、ensearch 为 0
- nodes 为提取出每一项搜索结果的 XPATH
- link 为超链接的 XPATH
- desc 为结果描述的 XPATH

2. 在 appsettings.json 配置文件的 engines 配置项中加入以上添加的搜索引擎名称，**注：逗号分隔**

### 自定义 ISearchEngine

另一种接入方式为自定义搜索类，该类需实现 ISearchEngine 接口。

1. 创建自定义类实现 ISearchEngine 接口，假设名为 SearchEngine
2. 在代码中注册 SearchEngine
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
3. 在 appsettings.json 配置文件的 engines 配置项中加入以上添加的搜索引擎名称，**注：逗号分隔**

## 元搜索引擎

本项目提供了三种元搜索引擎以供选择，主要的差别在于如何进行搜索。

### ParallelSearcher

ParallelSearcher 为默认引擎，该引擎在搜索时同时发起对所有搜索引擎的查询请求，并且会在获取到第一个搜索结果时返回。若要等待所有搜索引擎都结束任务才返回结果，则应调用`GET http://venyo.cn/hotoke/search/all?keyword={keyword}`

### WeightFirstSearcher

WeightFirstSearcher 会在搜索引擎列表中选取权重最高(数值最低)的引擎，并对其先进行一次查询请求，随后发起其他查询请求，并立即返回现有的搜索结果。该引擎不支持同步返回所有结果。

### CustomSearcher

CustomSearcher 定位为可定制的元搜索引擎，目前仅提供一个优先搜索的引擎列表配置，即配置哪些搜索引擎优先进行并行搜索。该引擎不支持同步返回所有结果。配置如下：
```
"CustomSearcher": {
    "AdvancedList": [
        "bing",
        "baidu"
    ]
}
```