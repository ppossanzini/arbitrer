using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store.Handlers.Model;

[Table("ShoppingBags")]
public class ShoppingBag
{
  [Key] public int Id { get; set; }
  [StringLength(200)] public required string User { get; set; }
  public DateTime CreationDate { get; set; }
  public double Subtotal { get; set; }
  public double Discount { get; set; }
  public double Total { get; set; }
}