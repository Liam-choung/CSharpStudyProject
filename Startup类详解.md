## 目录


- [类函数执行顺序](类函数执行顺序)

- [token与JWT之间关系](token与JWT之间关系)
- [什么是跨源配置（services.AddCors）？](什么是跨源配置（services.AddCors）？)

## 类函数执行顺序
configureservice()&nbsp;&nbsp;&nbsp;&nbsp;--->&nbsp;&nbsp;&nbsp;&nbsp;configure()

**服务注册** &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;   **HPPT请求处理管道构建**

顺序不重要 &nbsp;&nbsp;&nbsp;&nbsp;     &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;      管道注册顺序至关重要        

:punch::punch::punch:
```
namespace VOL.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private IServiceCollection Services { get; set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //初始化模型验证配置，不是ASP.NET Core框架自带的标准方法。来自项目中引用的vol框架的一部分
            services.UseMethodsModelParameters().UseMethodsGeneralParameters();

            //禁用ASP.NET Core内置的模型验证功能
            -----------------------------------------------------------------------------------------------------
           ｜【1】NullObjectModelValidator是一个“空”实现，它的Validate方法什么也不做，直接返回成功。                       ｜
           ｜【2】IObjectModelValidator是ASP.NET Core用于执行模型验证（例如检查[Required], [MaxLength]等特性）的核心服务。 ｜
           ｜【3】如果启用内置的模型验证服务只需删掉这段启用代码即可。                                                      |
           ｜【4】AddSingleton：单例模式，该类只可创建一个对象，全局访问                                                  |
           ｜【5】Transient：瞬态模式，每次从DI请求该对象时，获取一个全新对象                                              |
           ｜【6】Scoped：作用域，同一个 HTTP 请求的生命周期内，DI 容器返回相同的实例。新的 HTTP 请求到来时，创建一个新的服务实例 |
            -----------------------------------------------------------------------------------------------------
            services.AddSingleton<IObjectModelValidator>(new NullObjectModelValidator());
            

            Services = services;
            // services.Replace( ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            //用于注册Session（会话）状态管理所需的服务。Session允许在用户的多次请求之间存储特定于该用户的数据。例如，可以存储用户的购物车信息、登录状态的某些临时信息等
            //对于REST API风格的架构更推荐使用 JWT、Token 等进行临时数据存储以代替session
             三种不同架构：
               【1】传统MVC：页面由后端渲染生成，浏览器请求 → 后端 Controller → 渲染 HTML → 返回完整页面
               【2】RestFuL：面向资源，用 HTTP 方法来操作资源（GET/POST/PUT/DELETE），每个请求独立，不依赖 Session
               【3】前后端分离：浏览器请求前端 → 加载 Vue/React 页面 → 页面发起 AJAX 请求 → 调用后端 API → 渲染数据
             -------------------------------------------------------------------
             | 特性      | 传统MVC      | RESTful 架构  | 前后端分离项目            |
             | -----    | --------    | ----------    | ------------------      |
             | 页面生成  | 服务端渲染    | 客户端渲染      | 客户端渲染（SPA）         |
             | 状态管理  | Session     | 无状态（如 JWT） | 通常使用 Token         |
             | 前后端关系 | 耦合        | 松耦合          | 完全分离               |
             | 请求形式  | 表单提交     | HTTP API       | AJAX 请求 API        |
             | 用户体验  | 中等        | 一般            | 极佳（无刷新、响应快）        |
             | 技术复杂度 | 低         | 中              | 高                  |
             | 适用场景  | 中小网站、CMS | 移动端/API服务  | 现代Web系统、后台系统、App配套 |

            services.AddSession();

            //注册了基于服务器内存的缓存服务（IMemoryCache）。将频繁访问但不经常变化的数据临时存储在内存中，避免每次都从数据库或其他慢速数据源中读取。
            services.AddMemoryCache();

           //【1】注册了IHttpContextAccessor接口及其默认实现HttpContextAccessor。
           //【2】在一个非Controller的类中（比如在一个单独的业务逻辑服务、工具类或单例服务里）安全地访问当前HTTP请求的上下文HttpContext。
           //【3】HttpContext包含了关于当前请求的所有信息，如用户信息(User)、请求头(Headers)、查询字符串(Query)等。
            services.AddHttpContextAccessor();

            //【1】services.AddMvc(...) 注册MVC框架所需的所有服务。
            //【2】options.Filters.Add(typeof(ApiAuthorizeFilter)) 和 options.Filters.Add(typeof(ActionExecuteFilter)) 全局注册了两个过滤器。
            //【3】这意味着所有的Controller Action在执行前后都会自动应用这两个过滤器
            ⚠️ 这里仅仅是注册，若需要使用两个过滤器需要在configure函数中启用
            services.AddMvc(options =>
            {
                ⚠️ 这里注册的过滤器是全局注册，也可以通过在controller类前以及方法前使用注解来注册过滤器
                options.Filters.Add(typeof(ApiAuthorizeFilter));
                options.Filters.Add(typeof(ActionExecuteFilter));
                //  options.SuppressAsyncSuffixInActionNames = false;
            });

            //【1】 AddControllers() 告诉 ASP.NET Core 扫描并激活所有带 [ApiController] 或继承自 ControllerBase 的类，才能通过路由处理 HTTP 请求
            //【2】如果不调用 AddControllers()（或 AddMvc()），所有 ApiController 类都不会被激活，收不到任何请求，也不会生成路由映射，返回 404
            //【3】框架默认使用的是 System.Text.Json 来做 JSON 序列化/反序列化，但它在一些高级场景下不够灵活。
            //【4】AddNewtonsoftJson() 则是切换回 Newtonsoft.Json（也叫 Json.NET），它经过多年打磨，功能更丰富、可扩展性更强
            //【5】注册并启用以后意味着 整个 MVC/Web API 管道 中所有数据传输都回使用序列化的数据，所有 Controller 的输入输出都用 Newtonsoft.Json
            services.AddControllers()
              .AddNewtonsoftJson(op =>
              {
                  op.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                  op.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
              });

            //在 RESTful 或前后端分离项目中，Session 不再适用，JWT 可将用户身份信息、安全声明（Claims）、过期时间等都封装在 Token 本身
            Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             .AddJwtBearer(options =>
             {
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     SaveSigninToken = true,//保存token,后台验证token是否生效(重要)
                     ValidateIssuer = true,//是否验证Issuer
                     ValidateAudience = true,//是否验证Audience
                     ValidateLifetime = true,//是否验证失效时间
                     ValidateIssuerSigningKey = true,//是否验证SecurityKey
                     ValidAudience = AppSetting.Secret.Audience,//Audience
                     ValidIssuer = AppSetting.Secret.Issuer,//Issuer，这两项和前面签发jwt的设置一致
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSetting.Secret.JWT))
                 };


                 options.Events = new JwtBearerEvents()
                 {
                     OnChallenge = context =>
                     {
                         context.HandleResponse();
                         context.Response.Clear();
                         context.Response.ContentType = "application/json";
                         context.Response.StatusCode = 401;
                         context.Response.WriteAsync(new { message = "授权未通过", status = false, code = 401 }.Serialize());
                         return Task.CompletedTask;
                     }
                 };
             });

            //必须appsettings.json中配置，配置跨源策略
            string corsUrls = Configuration["CorsUrls"];
            if (string.IsNullOrEmpty(corsUrls))
            {
                throw new Exception("请配置跨请求的前端Url");
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                           .SetPreflightMaxAge(TimeSpan.FromSeconds(2520))
                            .AllowAnyHeader().AllowAnyMethod();
                        });
            });
            //是 ASP.NET Core 中 依赖注入（Dependency Injection, DI） 的一个典型用法，用于注册 HttpContextAccessor 服务。
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                //分为2份接口文档
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VOL.Core后台Api", Version = "v1", Description = "这是对文档的描述。。" });
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "VOL.Core对外三方Api", Version = "v2", Description = "xxx接口文档" });  //控制器里使用[ApiExplorerSettings(GroupName = "v2")]              
                                                                                                                             //启用中文注释功能
                                                                                                                             // var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                                                                                                                             //  var xmlPath = Path.Combine(basePath, "VOL.WebApi.xml");
                                                                                                                             //   c.IncludeXmlComments(xmlPath, true);//显示控制器xml注释内容
                                                                                                                             //添加过滤器 可自定义添加对控制器的注释描述
                                                                                                                             //c.DocumentFilter<SwaggerDocTag>();

                var security = new Dictionary<string, IEnumerable<string>> { { AppSetting.Secret.Issuer, new string[] { } } };
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "JWT授权token前面需要加上字段Bearer与一个空格,如Bearer token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            })
             .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressConsumesConstraintForFormFileParameters = true;
                options.SuppressInferBindingSourcesForParameters = true;
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressMapClientErrors = true;
                options.ClientErrorMapping[404].Link =
                    "https://*/404";
            });
            services.AddSignalR();
            //services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            //services.AddTransient<IPDFService, PDFService>();

            services.AddHttpClient();
            Services.AddTransient<HttpResultfulJob>();
            Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            Services.AddSingleton<Quartz.Spi.IJobFactory, IOCJobFactory>();

            //设置文件上传大小限制
            //设置文件上传大小限制
            services.Configure<FormOptions>(x =>
            {
                x.MultipartBodyLengthLimit = 1024 * 1024 * 100;//100M
            });
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 1024 * 1024 * 100;//100M
            });
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 1024 * 1024 * 100;//100M
            });
        }
        public void ConfigureContainer(ContainerBuilder builder)
        {
            Services.AddModule(builder, Configuration);
            //初始化流程表，表里面必须有AuditStatus字段
            WorkFlowContainer.Instance
               .Use<MES_ProductionReporting>(
                 "生产报工",
                    filterFields: x => new { x.ReportingNumber, x.AcceptedQuantity, x.RejectedQuantity, x.Total, x.ReportedBy, x.ReportingTime },
                    //审批界面显示表数据字段
                    formFields: x => new { x.ReportedBy, x.ReportingNumber, x.ReportingTime, x.AcceptedQuantity, x.RejectedQuantity, x.Total }
                )
                //run方法必须写在最后位置
                .Run();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseQuartz(env);
            }
            app.UseMiddleware<ExceptionHandlerMiddleWare>();
            app.UseDefaultFiles();
            app.UseStaticFiles().UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true
            });
            app.Use(HttpRequestMiddleware.Context);

            //2021.06.27增加创建默认upload文件夹
            string _uploadPath = (env.ContentRootPath + "/Upload").ReplacePath();

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), @"Upload")),
                //配置访问虚拟目录时文件夹别名
                RequestPath = "/Upload",
                OnPrepareResponse = (Microsoft.AspNetCore.StaticFiles.StaticFileResponseContext staticFile) =>
                {
                    //可以在此处读取请求的信息进行权限认证
                    //  staticFile.File
                    //  staticFile.Context.Response.StatusCode;
                }
            });
            //配置HttpContext
            app.UseStaticHttpContext();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //2个下拉框选项  选择对应的文档
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VOL.Core后台Api");
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "测试第三方Api");
                c.RoutePrefix = "";
            });
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //配置SignalR
                if (AppSetting.UseSignalR)
                {
                    string corsUrls = Configuration["CorsUrls"];

                    endpoints.MapHub<HomePageMessageHub>("/message")
                    .RequireCors(t =>
                    t.WithOrigins(corsUrls.Split(',')).
                    AllowAnyMethod().
                    AllowAnyHeader().
                    AllowCredentials());
                }

            });
        }
    }

    /// <summary>
    /// Swagger注释帮助类
    /// </summary>
    public class SwaggerDocTag : IDocumentFilter
    {
        /// <summary>
        /// 添加附加注释
        /// </summary>
        /// <param name="swaggerDoc"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            //添加对应的控制器描述
            swaggerDoc.Tags = new List<OpenApiTag>
            {
                new OpenApiTag { Name = "Test", Description = "这是描述" },
                //new OpenApiTag { Name = "你的控制器名字，不带Controller", Description = "控制器描述" },
            };
        }
    }
}
```

## token与JWT之间关系

**JWT是一种token的具体实现方式**

| 比较项      | Token（通用）        | JWT（JSON Web Token）              |
| -------- | ---------------- | -------------------------------- |
| 是否特定格式   | ❌ 无固定格式          | ✅ 标准结构（Header.Payload.Signature） |
| 是否包含用户信息 | ❌ 不一定            | ✅ 是，Payload 中可包含 userId、role 等   |
| 是否自包含    | ❌ 通常不是，需要服务端存储验证 | ✅ 自包含，可直接验证，无需查询数据库              |
| 安全性      | 取决于实现            | 高，通过签名校验是否被篡改                    |
| 易于扩展     | ❌                | ✅ 支持自定义字段（如设备、权限等）               |
| 适用场景     | 各种认证（简单场景）       | 前后端分离、微服务、OAuth、OpenID等复杂认证      |

## 什么是跨源配置（services.AddCors）？
- 前端应用（例如运行在 http://localhost:3000）想要请求后端 API（例如运行在 http://localhost:5000），那么这两个是不同的源，属于跨源
- 跨源配置：不同源的前端应用与后端 API 进行通信，需要在服务器端明确告诉浏览器哪些外部源被允许访问
- CORS (跨域资源共享) 就是在后端配置，明确告诉浏览器，哪些前端（或者说，哪些“源”）可以访问后端提供的接口

  
【1】 配置特定前端访问
```
1️⃣ 配置特定前端访问 --仅允许 https://weather-app.com 访问
// 在 ConfigureServices 方法中
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("https://weather-app.com") // 只允许这个来源
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
2️⃣ 在configure方法中启用
app.UseCors("AllowSpecificOrigin"); // 应用这个策略
```

【2】 配置所有前端都可 访问
```
1️⃣ 在 ConfigureServices 方法中
services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.AllowAnyOrigin() // 允许所有来源
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

2️⃣ 在 Configure 方法中
app.UseCors(); // 应用默认策略
```

## 依赖注入
- AddSingleton<TService, TImplementation>()是 ASP.NET Core 中 依赖注入（Dependency Injection, DI） 的一个典型用法，用于注册 HttpContextAccessor 服务。

TService : 这是希望通过依赖注入来获取的服务接口

TImplementation ： 这是实现 TService 接口的具体类。当容器被要求提供TService 的实例时，它会创建一个TImplementation的实例并返回。


