namespace Store.Handlers.Model;

public class ItemInShoppingBag
{
  public int ItemId { get; set; }
  public int ShoppingBagId { get; set; }
  public double Quantity { get; set; }
  public double Price { get; set; }
}