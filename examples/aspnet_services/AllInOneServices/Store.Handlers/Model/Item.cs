using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store.Handlers.Model;

[Table("items")]
public class Item
{
  [Key] public int Id { get; set; }
  [StringLength(200)] public required string Code { get; set; }
  [StringLength(2000)] public string? Description { get; set; }
  public double Price { get; set; }
}