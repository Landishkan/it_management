using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderManager.App.Data;
using OrderManager.App.Models;

using System.Windows;

namespace OrderManager.App
{
    public partial class App : Application
    {

        private IHost _apiHost;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Запускаем API в фоне
            _apiHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseStartup<ApiStartup>();
                    web.UseUrls("http://localhost:5012");
                })
                .Build();

            _apiHost.Start();
            // Открываем браузер с сайтом
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "http://localhost:5012",
                UseShellExecute = true
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _apiHost?.Dispose();
            base.OnExit(e);
        }
    }

    public class ApiStartup
    {
        private readonly IConfiguration _configuration;

        // Конструктор автоматически внедрит конфигурацию из appsettings.json
        public ApiStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Настройка контроллеров (один раз, без дублирования)
            services.AddControllers()
     .AddApplicationPart(typeof(ApiStartup).Assembly)
     .AddControllersAsServices()
     // Добавляем вот эту настройку для JSON:
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
     });

            // Определяем тип БД из файла настроек
            var dbType = _configuration["DatabaseSettings:TargetBot"] ?? "PostgreSql";

            string connectionString = dbType == "PostgreSql"
                ? _configuration.GetConnectionString("PostgreSqlConnection")
                : _configuration.GetConnectionString("SqliteConnection");

            services.AddDbContext<AppDbContext>(options =>
            {
                if (dbType == "PostgreSql")
                    options.UseNpgsql(connectionString);
                else
                    options.UseSqlite(connectionString);
            }, contextLifetime: ServiceLifetime.Scoped);

            // JWT настройки из appsettings.json
            var jwtKey = _configuration["Jwt:Key"];
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidAudience = _configuration["Jwt:Audience"],
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            services.AddAuthorization();

            // Добавляем поддержку CORS, чтобы JS на сайте не блокировал запросы к API
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Сид данных (оставляем твой рабочий код инициализации)
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                if (!db.Users.Any())
                {
                    db.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), // Явно создаем уникальный Guid
                        Email = "admin@mail.ru",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        FullName = "Администратор Системы",
                        Role = "Admin"
                    });
                    db.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), // Явно создаем уникальный Guid
                        Email = "manager@mail.ru",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                        FullName = "Марина Менеджер",
                        Role = "Manager"
                    });
                    db.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), // Явно создаем уникальный Guid
                        Email = "dev@mail.ru",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                        FullName = "Иван Разработчик",
                        Role = "Developer"
                    });
                    db.Users.Add(new User
                    {
                        Id = Guid.NewGuid(), // Явно создаем уникальный Guid
                        Email = "client@mail.ru",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                        FullName = "Алексей Заказчиков",
                        Role = "Client"
                    });
                  
                    db.SaveChanges();
                }
            }

            app.UseDeveloperExceptionPage();
            app.UseRouting();

            // Включаем CORS
            app.UseCors("AllowAll");

            // Раздача фронтенда из wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html"); // Если роут не найден в API, отдаем index.html
            });
        }
    }
}