using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Middleware;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Lumino.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // CORS (Frontend)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173",
                        "https://localhost:5173",
                        "http://localhost:5174",
                        "https://localhost:5174"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            var jwtSettings = builder.Configuration.GetSection("Jwt");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
                    )
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<LuminoDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                );
            });

            // конфіг навчання
            builder.Services.Configure<LearningSettings>(
                builder.Configuration.GetSection("Learning")
            );

            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

            builder.Services.AddScoped<IRegisterRequestValidator, RegisterRequestValidator>();
            builder.Services.AddScoped<ILoginRequestValidator, LoginRequestValidator>();
            builder.Services.AddScoped<ISubmitLessonRequestValidator, SubmitLessonRequestValidator>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<IAdminCourseService, AdminCourseService>();
            builder.Services.AddScoped<ITopicService, TopicService>();
            builder.Services.AddScoped<IAdminTopicService, AdminTopicService>();
            builder.Services.AddScoped<ILessonService, LessonService>();
            builder.Services.AddScoped<IAdminLessonService, AdminLessonService>();
            builder.Services.AddScoped<IExerciseService, ExerciseService>();
            builder.Services.AddScoped<IAdminExerciseService, AdminExerciseService>();
            builder.Services.AddScoped<ILessonResultService, LessonResultService>();
            builder.Services.AddScoped<IProgressService, ProgressService>();
            builder.Services.AddScoped<ICourseProgressService, CourseProgressService>();
            builder.Services.AddScoped<ICourseCompletionService, CourseCompletionService>();
            builder.Services.AddScoped<IAchievementService, AchievementService>();
            builder.Services.AddScoped<IAchievementQueryService, AchievementQueryService>();
            builder.Services.AddScoped<ILessonResultQueryService, LessonResultQueryService>();
            builder.Services.AddScoped<IVocabularyService, VocabularyService>();
            builder.Services.AddScoped<ISceneService, SceneService>();
            builder.Services.AddScoped<IAdminSceneService, AdminSceneService>();
            builder.Services.AddScoped<IMediaService, MediaService>();
            builder.Services.AddScoped<IRefreshTokenCleanupService, RefreshTokenCleanupService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<INextActivityService, NextActivityService>();
            builder.Services.AddScoped<ILearningPathService, LearningPathService>();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter JWT token like: Bearer {your token}"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                LuminoSeeder.Seed(app);

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCors("AllowFrontend");

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
