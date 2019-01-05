# hotoke
构建自己的搜索引擎

本项目暂时以百度、必应、360的搜索结果为主，也加入了 stackoverflow 的爬虫，爬虫时使用了[async-proxy-pool](https://github.com/chenjiandongx/async-proxy-pool) 代理池

## [线上 Demo](http://venyo.cn/)

## 快速启动

本项目的主体应用为 mainsite 目录下的 asp.net core 项目，启动后即可使用百度、必应、360的综合搜索。

1. 下载或克隆本项目：`git clone https://github.com/venyowong/hotoke.git` 或下载[打包好的文件](https://github.com/venyowong/hotoke/releases/download/lastest-alpha-2019.01.05/hotoke.mainsite.zip)
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

1. 按照 [async-proxy-pool 安装教程](https://github.com/chenjiandongx/async-proxy-pool#如何使用) 安装好代理池，当然此步骤也可以省去，可以直接使用我已经搭建好的环境
2. [安装、配置 Java 环境](http://venyo.cn/?keyword=java%20%E5%AE%89%E8%A3%85%E9%85%8D%E7%BD%AE)
3. 下载[已打包好的 hotoke-search jar 包](https://github.com/venyowong/hotoke/releases/download/lastest-alpha-2019.01.05/hotoke.search.jar)或自行使用 maven 打包 search 项目
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
