using CacheMeIfYouCan.Application.DTOs;
using CacheMeIfYouCan.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CacheMeIfYouCan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService service, ILogger<ProductsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <returns>List of all products</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        _logger.LogInformation("Getting all products");
        var products = await _service.GetAllAsync();
        return Ok(products);
    }

    /// <summary>
    /// Get a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        _logger.LogInformation("Getting product with ID {ProductId}", id);
        
        if (id <= 0)
            return BadRequest("Product ID must be greater than 0");

        var product = await _service.GetByIdAsync(id);
        if (product is null)
            return NotFound($"Product with ID {id} not found");

        return Ok(product);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="createDto">Product creation data</param>
    /// <returns>Created product with ID</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Creating new product: {ProductName}", createDto.Name);

        try
        {
            var product = await _service.AddAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest("Error creating product");
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateDto">Updated product data</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != updateDto.Id)
            return BadRequest("ID in URL does not match ID in body");

        _logger.LogInformation("Updating product with ID {ProductId}", id);

        var result = await _service.UpdateAsync(id, updateDto);
        if (!result)
            return NotFound($"Product with ID {id} not found");

        return NoContent();
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return BadRequest("Product ID must be greater than 0");

        _logger.LogInformation("Deleting product with ID {ProductId}", id);

        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound($"Product with ID {id} not found");

        return NoContent();
    }
}
