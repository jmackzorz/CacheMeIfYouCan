namespace CacheMeIfYouCan.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Product? Product { get; set; }
}
