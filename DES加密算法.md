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

- 
