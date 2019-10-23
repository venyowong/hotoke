# hotoke
Build your own search engine.

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

# hotoke
构建自己的搜索引擎

本项目暂时以百度、必应、360的搜索结果为主

## [线上 Demo](http://venyo.cn/)

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

`GET http://venyo.cn/search?keyword={keyword}&requestId=`

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
`GET http://venyo.cn/search?keyword={keyword}&requestId={首次调用接口返回的 requestId}`
继续获取搜索结果，直至 finished 为 true。
