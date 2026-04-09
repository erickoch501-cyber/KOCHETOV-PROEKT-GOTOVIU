using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Kochetov.Data;
using Kochetov.Models;
using Kochetov.Models.Auth;
using Kochetov.Services;

namespace Kochetov.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        IJwtTokenService tokenService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation($"Попытка регистрации: {request.Email}");

            var existingUser = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new AppUser
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            // ДОБАВЛЯЕМ В customers
            var customer = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                Address = request.Address,
                Phone = ""
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Пользователь сохранен с ID: {user.Id}");

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}