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

## 加入自己感兴趣的内容

本项目除了提供整合多个搜索引擎结果的功能，还支持自定义搜索，即使用本项目实现的简易搜索引擎 hotoke-search，并使用爬虫，往 hotoke-search 里索引数据，最后在 mainsite 项目中加入 hotoke 搜索引擎。

hotoke-search 是基于 Lucene 编写的 Java 应用，因此运行之前需要安装配置 Java 环境。

本项目的 crawlers 目录下，提供了一个使用 .net core 实现的针对 Stackoverflow 的爬虫程序，程序使用到了 http ip 代理池，线上 Demo 部署时，是使用了 [async-proxy-pool](https://github.com/chenjiandongx/async-proxy-pool) 这个开源项目。

以下介绍加入 Stackoverflow 到 hotoke-search 的步骤：

1. 按照 [async-proxy-pool 安装教程](https://github.com/chenjiandongx/async-proxy-pool#如何使用) 安装好代理池，当然此步骤也可以省去，可以直接使用我已经搭建好的环境，此步骤只会影响到下面的第7步，若不想自己搭建代理池，保留 ProxyPoolUrl 配置即可
2. [安装、配置 Java 环境](http://venyo.cn/?keyword=java%20%E5%AE%89%E8%A3%85%E9%85%8D%E7%BD%AE)
3. 下载[已打包好的 hotoke-search jar 包](https://github.com/venyowong/hotoke/releases/download/alpha-2019.01.05/hotoke.search.jar)或自行使用 maven 打包 search 项目
4. 打开命令行，执行 `java -jar hotoke.search.jar`，你将会看到类似以下的输出：
    ```
    2019-01-05 10:46:46.732 [Thread-1] INFO  org.eclipse.jetty.util.log - Logging initialized @1255ms to org.eclipse.jetty.util.log.Slf4jLog
    2019-01-05 10:46:46.770 [Thread-1] INFO  spark.embeddedserver.jetty.EmbeddedJettyServer - == Spark has ignited ...
    2019-01-05 10:46:46.770 [Thread-1] INFO  spark.embeddedserver.jetty.EmbeddedJettyServer - >> Listening on 0.0.0.0:4685
    2019-01-05 10:46:46.773 [Thread-1] INFO  org.eclipse.jetty.server.Server - jetty-9.4.8.v20171121, build timestamp: 2017-11-22T05:27:37+08:00, git hash: 82b8fb23f757335bb3329d540ce37a2a2615f0a8
    2019-01-05 10:46:46.790 [Thread-1] INFO  org.eclipse.jetty.server.session - DefaultSessionIdManager workerName=node0
    2019-01-05 10:46:46.790 [Thread-1] INFO  org.eclipse.jetty.server.session - No SessionScavenger set, using defaults
    2019-01-05 10:46:46.792 [Thread-1] INFO  org.eclipse.jetty.server.session - Scavenging every 600000ms
    2019-01-05 10:46:46.812 [Thread-1] INFO  org.eclipse.jetty.server.AbstractConnector - Started ServerConnector@7bd447d3{HTTP/1.1,[http/1.1]}{0.0.0.0:4685}
    2019-01-05 10:46:46.813 [Thread-1] INFO  org.eclipse.jetty.server.Server - Started @1339ms
    ```
5. 打开浏览器，访问 http://localhost:4685/health 或 http://{your_ip}:4685/health，你会看到"ok"，则表示程序启动成功
6. 添加一个 patu.yml 文件到 crawlers/patucrawler 目录下，内容类似以下示例：
    ```
    Interval: 1y # 表示爬虫程序周期为一年
    BloomSize: 30000000 # 布隆过滤器大小
    ExpectedPageCount: 1500000 # 期望爬取到的页面数量
    CrawlDeepth: 1 # 爬虫深度
    Name: stackoverflow # 爬虫程序的名称
    AutoDown: # 爬虫程序异常时自动终止的模块
        EnableAutoDown: true # 启用
        MaxTolerableRate: 30 # 每分钟最多能忍受程序报错次数
        SmtpHost: # 爬虫程序终止后发送邮件的服务器，如果发送邮件的邮箱为 163，则是 smtp.163.com
        SendMail: # 发送邮件的邮箱
        SendPassword: # 发送邮件的邮箱的密码
        ReceiveMail: # 接收邮件的邮箱
    ```
7. 修改 crawlers/patucrawler/App.config 配置文件中的 IndexHost 属性，地址为 http://localhost:4685/index 或 http://{your_ip}:4685/index
8. 修改 crawlers/patucrawler/log4net.config 配置文件中的日志文件存放路径
9. 打开命令行，进入 crawlers/patucrawler 目录，执行 `dotnet run` 命令
10. 通过 http://localhost:4685/count 或 http://{your_ip}:4685/count 可看到写入的文档数量
11. 若发生异常或文档数量未增长，可查看爬虫程序日志或 hotoke-search 项目的日志，hotoke-search 项目日志在 hotoke.search.jar 文件所在目录的 log 子目录中
12. 当爬虫程序和 hotoke-search 都运行正常，则可按照快速启动的步骤，在第3步加入 hotoke 即可在后续搜索中看到 hotoke 的搜索结果了

## 接入线上 Demo 接口

如果你不想自己搭建环境，但是又认为本项目的接口对你有用的话，可以直接接入线上 Demo 的接口，以下介绍两种接入方式：

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

这种请求方式也是线上 Demo 所使用的方式。

### Web Socket

使用 Web Socket 的方式请求时，只需要使用客户端连接`ws://venyo.cn/ws/search`即可。发送字符串即为发送搜索请求，接收的数据即为搜索结果，数据结构如下：
```
[{
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
```

### 注：若希望有其他请求方式，可以建 issue。若有朋友写了页面接入 venyo.cn，可以的话，希望分享一下，我自己写的页面太丑了，23333333333