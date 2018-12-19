using System;
using System.Collections.Generic;

namespace EFCoreExtensions.Models
{
    public partial class User
    {
        public User()
        {
            Products = new HashSet<Product>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
