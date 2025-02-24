using System.ComponentModel.DataAnnotations;

namespace Business.Models
{
    public class Customer
    {
        [Key]
        public int Cus_Id { get; set; }
        public string? Cus_EmailId { get; set; }
        public string? Cus_Password { get; set; }
        public string? Cus_Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
