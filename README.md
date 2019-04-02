### 微信第三方平台网关组件

基于GRPC实现的网关组件,详细接口说明,请参照 Wechat.proto , 需要相关客户端代码,请自行学习GRPC.

#### DOCKER部署说明: REPO: https://hub.docker.com/r/simcu/wechat-gateway

将配置文件准备好,详细配置文件参照下方说明.

将配置文件挂到 /home/appsettings.json 启动即可

> docker run -d --name simcu-wechat-gateway -p 80:80 -p 50051:50051 -v /path/to/your/config.json:/home/appsettings.json simcu/wechat-gateway

#### 微信平台配置说明:
本组件可以完全独立于主业务系统和微信接口之间,使业务不需要关系微信侧相关的内容,网关启动后,会在80端口开启HTTP服务器监听微信的相关推送,并在50051(默认)端口开启客户端GRPC接口,需要在微信第三方平台中配置的内容如下:

##### 授权登录相关
1. 授权事件接收URL: http(s)://yourdomain.com/platform


##### 授权后实现业务
1. 消息校验Token: 配置后需要在网关配置文件中配置
2. 消息加解密Key: 配置后需要在网关配置文件中配置
3. 消息与事件接收URL: http(s)://yourdomain.com/user/$APPID$


#### 配置说明:

````JSON
appsettings.json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "System": "Error",
            "Microsoft": "Error"
        }
    },
    "AllowedHosts": "*",
    "Redis": {
        "Host": "127.0.0.1:6379,password=love1314",   //redis链接地址
        "CredentialDb": 5,                            // access_token 存放数据库
        "HangfireDb": 5,                              // 后台任务 存放数据库
        "QueueDb": 5                                  // 消息队列 存放数据库
    },
    "QueueWorker": {
        "Platform": 10,    //核心组件后台任务进程数量
        "Schedule": 10,    //定时任务进程数量
        "Message": 10,     //消息处理进程数量
        "HighPriorityMessage": 10  //高优先级消息进程数量
    },
    "Hangfire": {
        "Path": "/",    // 任务控制台访问地址
        "User": "123",  // 任务控制台用户名
        "Pass": "123"   // 任务控制台密码
    },
    "Grpc": {
        "Host": "0.0.0.0",  // GRPC 接口监听地址
        "Port": 50051       // GRPC 监听端口
    },
    //下面为第三方平台的相关配置
    "Wechat": {
        "AppID": "",           
        "AppSecret": "",       
        "Token": "",           
        "EncodingAESKey": ""
    }
}

````

