# hotoke
构建自己的搜索引擎

本项目暂时以百度、必应、360的搜索结果为主，也加入了 stackoverflow 的爬虫，爬虫时使用了[async-proxy-pool](https://github.com/chenjiandongx/async-proxy-pool) 代理池

## [线上 Demo](http://venyo.cn/)

## 快速启动

本项目的主体应用为 mainsite 目录下的 asp.net core 项目，启动后即可使用百度、必应、360的综合搜索。

1. 下载或克隆本项目：`git clone https://github.com/venyowong/hotoke.git` 或下载[打包好的文件](https://github.com/venyowong/hotoke/releases/download/alpha-2019.01.05/hotoke.mainsite.zip)
2. mainsite 项目依赖 .net core 运行环境，所以需要先[安装 .net core](https://dotnet.microsoft.com/download)
3. 编辑 mainsite/appsettings.json 配置文件中的 Engines 属性，保留自己想使用的搜索引擎。
4. 在 mainsite 目录下，启动终端，执行 `dotnet run` 命令；如果下载了已打包好的文件可以执行 `dotnet MainSite.dll`(这一步可能需要权限),你将会看到类似以下的输出：
    ```
    Hosting environment: 
    Content root path: 
    Now listening on: http://0.0.0.0:80
    Application started. Press Ctrl+C to shut down.
    ```
5. 打开浏览器，访问 http://localhost 或 http://{your_ip}

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
