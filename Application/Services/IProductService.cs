using CacheMeIfYouCan.Application.DTOs;
using CacheMeIfYouCan.Domain.Entities;

namespace CacheMeIfYouCan.Application.Services;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto> AddAsync(CreateProductDto dto);
    Task<bool> UpdateAsync(int id, UpdateProductDto dto);
    Task<bool> DeleteAsync(int id);
}
