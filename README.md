# **Microsoft.AspNetCore.Authentication Extensions**

> 集成微信PC端扫码登录和QQ登录

## Get Started

\- Install nuget package

 WebChat

```c#
PM> Install-Package My.AspNetCore.Authentication.WeChat
  ```

QQ

```c#
PM> Install-Package My.AspNetCore.Authentication.QQ
```

MiniProgram

```c#
PM> Install-Package My.AspNetCore.Authentication.MiniProgram
```

## 微信登录

1、在MVC5项目中使用，支持.Net Framework

```c#
app.UseWeChatAuthentication(appId: "***",appSecret: "*****");
```

2、支持Asp Net Core3.0、3.1和5.0

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

2、支持Asp Net Core3.0、3.1和5.0

startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    // .... others code ...
    // config 
    services.AddAuthentication() 
        .AddQQConnect(o =>
        {
              o.ClientId = Configuration["QQ:AppId"];
              o.ClientSecret = Configuration["QQ:AppSecret"];
         });

    // .... others code ...
}
```

## 小程序登录

支持Asp Net Core3.0、3.1和5.0

小程序端代码

```javascript
// ...
wx.request({
        url: 'http://localhost:5000/signin-wechat-miniprogram',
        data: {
            'code': code,
            'encryptData': "需要解密的用户敏感数据",
            'iv': "解密算法初始向量"
        },
        method: 'POST',
        header: {
            "Content-Type": "application/json"
        },
        success: function (data) {
            // 响应结果处理
        }
    )
}

// ...
```

startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    // .... others code ...
    // config 
    services.AddAuthentication() 
        .AddMiniProgram<MiniProgramLoginHandler>(o => {
              o.ClientId = Configuration["MiniProgram:AppId"];
              o.ClientSecret = Configuration["MiniProgram:AppSecret"];
        });

    // .... others code ...
}
```

## 参考链接

[AspNetCore3.1_Secutiry源码解析_5_Authentication_OAuth](https://www.cnblogs.com/holdengong/p/12563558.html)
[asp.net core 外部认证多站点模式实现](https://www.cnblogs.com/passingwind/p/9511022.html)
[微信扫码网页登录，redirect_uri参数错误解决方法](https://www.cnblogs.com/zmdComeOn/p/12727308.html)
[第三方登录：微信扫码登录（OAuth2.0](https://cloud.tencent.com/developer/article/1447723)
[Authentication — (3.3) 认证处理器的实现之RemoteAuthenticationHandler](https://mp.weixin.qq.com/s/t0PsP0hZ5HSZtitzLODkQw)
[AspNetCore3.1_Secutiry源码解析_5_Authentication_OAuth](https://www.cnblogs.com/holdengong/p/12563558.html)
[AspNetCore.AuthenticationQQ-WebChat](https://github.com/jxnkwlp/AspNetCore.AuthenticationQQ-WebChat)
[微信开放平台官方教程](https://developers.weixin.qq.com/doc/oplatform/Website_App/WeChat_Login/Wechat_Login.html)
[ASP.NET Core集成微信登录](https://www.jb51.net/article/91575.htm)
[ASP.NET实现QQ、微信、新浪微博OAuth2.0授权登录 原创](https://www.jb51.net/article/81624.htm)
[微信开放平台之网站授权微信登录功能](https://www.jb51.net/article/72666.htm)
