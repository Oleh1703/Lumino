using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace Lumino.Api.Middleware
{
    // У HTTP інтеграційних тестах ми авторизуємося через TestAuthHandler (тільки Claims),
    // але сам запис User у БД може бути відсутнім.
    // Частина бізнес-логіки (hearts, crystals, profile, etc.) очікує, що User існує.
    // Тому у середовищі "Testing" автододаємо користувача при першому запиті.
    public class EnsureTestUserMiddleware
    {
        private readonly RequestDelegate _next;

        public EnsureTestUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, LuminoDbContext dbContext, IHostEnvironment env)
        {
            if (env.IsEnvironment("Testing")
                && context.User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(idClaim, out var userId) && userId > 0)
                {
                    var exists = dbContext.Users.Any(x => x.Id == userId);

                    if (!exists)
                    {
                        // Мінімальний валідний User для тестів.
                        // Email/PasswordHash потрібні як required поля.
                        dbContext.Users.Add(new User
                        {
                            Id = userId,
                            Email = $"test{userId}@local",
                            PasswordHash = "test",
                            Role = Domain.Enums.Role.User,
                            CreatedAt = DateTime.UtcNow,
                            Hearts = 5,
                            Crystals = 0,
                            Theme = "light"
                        });

                        dbContext.SaveChanges();
                    }
                }
            }

            await _next(context);
        }
    }
}
