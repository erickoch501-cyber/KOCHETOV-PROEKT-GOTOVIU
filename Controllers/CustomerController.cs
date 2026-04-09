using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kochetov.Data;
using Kochetov.Models;

namespace Kochetov.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(AppDbContext context, ILogger<CustomerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        try
        {
            var customers = await _context.Customers.ToListAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка клиентов");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(long id)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new { message = "Клиент не найден" });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении клиента");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        try
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании клиента");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(long id, Customer customer)
    {
        if (id != customer.Id)
        {
            return BadRequest(new { message = "ID клиента не совпадает" });
        }

        try
        {
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(customer);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CustomerExists(id))
            {
                return NotFound(new { message = "Клиент не найден" });
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении клиента");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(long id)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = "Клиент не найден" });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Клиент удален" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении клиента");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    private async Task<bool> CustomerExists(long id)
    {
        return await _context.Customers.AnyAsync(e => e.Id == id);
    }
}