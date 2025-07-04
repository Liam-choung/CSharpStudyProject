## 类函数执行顺序
configureservice()&nbsp;&nbsp;&nbsp;&nbsp;--->&nbsp;&nbsp;&nbsp;&nbsp;configure()

**服务注册** &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;   **HPPT请求处理管道构建**

顺序不重要 &nbsp;&nbsp;&nbsp;&nbsp;     &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;      管道注册顺序至关重要        

:punch::punch::punch:

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


            services.AddMemoryCache();
            services.AddHttpContextAccessor();
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ApiAuthorizeFilter));
                options.Filters.Add(typeof(ActionExecuteFilter));
                //  options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddControllers()
              .AddNewtonsoftJson(op =>
              {
                  op.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                  op.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
              });

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
            //必须appsettings.json中配置
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

