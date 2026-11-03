using Microsoft.AspNetCore.Mvc;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;
using RedisCachingLab.Infrastructure.Services;

namespace RedisCachingLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();

        return Ok(products);
    }

    [HttpGet("get-{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product is null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Product product)
    {
        await _productService.CreateAsync(product);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }


    [HttpPut("update-{id:int}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest("Route ID and Product ID do not match.");

        var existingProduct = await _productService.GetByIdAsync(id);

        if (existingProduct is null)
            return NotFound();

        await _productService.UpdateAsync(product);

        return NoContent();
    }

    [HttpDelete("delete-{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

}
