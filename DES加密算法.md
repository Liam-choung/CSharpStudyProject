# 登陆密码验证流程
- 登陆的密码不能通过明文存储，通过加密后进行验证，因为数据库里面的密码都不是明文存储，如：

```
Sys_User user =await repository.FindAsIQuerable(x=>x.UserName==loginInfo.UserName).FirstOrDefaultAsync();

if(user==null||loginInfo.Password.Trim().EncryotDES(AppSetting.Secret.User)!=(User.Pwd ?? ""))

return webResponse.Erro(ResponseType.LoginError);

```

- 其中 ` loginInfo.Password.Trim().EncryotDES(AppSetting.Secret.User) `表示对用户的密码进行加密，通过加密后与数据库的密码进行比对

```
AppSetting.json:
"Serect":{
    "JWT":"dsjfigjhfi23gf4",
    "User":"h5kjsf7sf8er"
}
```

这里的User则是**加密密匙**

## DES加密算法
- 简介

DES（Data Encryption Standard）是一种对称加密算法，使用56位密匙对明文块进行加密

- 核心加密流程

    1. 初始置换（IP置换）：将64位明文重新排列，增加加密复杂性
    2. 16轮加密：每轮包括分组、拓展、异或、S盒替换和P盒置换
    3. 逆置换（FP置换）：对最终结果进行逆排序，生成密文
- 优点

  简单、容易实现、运行效率高

- 缺点

  密码生命周期短、容易被暴力破解，密钥不好管理，无签名认证密钥容易被冒充
