# **Microsoft.AspNetCore.Authentication Extensions**

> 集成微信PC端扫码登录和QQ登录

## Get Started

\- Install nuget package

 WebChat 

  ```

  PM> Install-Package My.AspNetCore.Authentication.WeChat

  ```



## 微信登录

1、在MVC5项目中使用，支持.Net Framework

```c#
app.UseWeChatAuthentication(appId: "***",appSecret: "*****");
```

2、在支持.NET Standard2.1框架中使用

startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    // .... others code ...
    // config 
    services.AddAuthentication() 
        .AddWeChat(o =>
        {
              o.ClientId = Configuration["WeChat:AppId"];
              o.ClientSecret = Configuration["WeChat:AppSecret"];
         });

    // .... others code ...
}
```





## QQ登录

1、在MVC5项目中使用，支持.Net Framework

```c#
app.UseQQConnectAuthentication(appId: "***",appSecret: "*****");
```



## 参考链接

[AspNetCore3.1_Secutiry源码解析_5_Authentication_OAuth - holdengong - 博客园 (cnblogs.com)](https://www.cnblogs.com/holdengong/p/12563558.html)
[asp.net core 外部认证多站点模式实现 - Passingwind - 博客园 (cnblogs.com)](https://www.cnblogs.com/passingwind/p/9511022.html)
https://mp.weixin.qq.com/s/t0PsP0hZ5HSZtitzLODkQw
https://www.cnblogs.com/holdengong/p/12563558.html
https://github.com/jxnkwlp/AspNetCore.AuthenticationQQ-WebChathttps://cloud.tencent.com/developer/article/1447723
https://developers.weixin.qq.com/doc/oplatform/Website_App/WeChat_Login/Wechat_Login.html
https://www.jb51.net/article/91575.htm
https://www.jb51.net/article/81624.htm
https://www.jb51.net/article/72666.htm