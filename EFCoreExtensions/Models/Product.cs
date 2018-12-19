using System;
using System.Collections.Generic;

namespace EFCoreExtensions.Models
{
    public partial class Product
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ProductName { get; set; }

        public User User { get; set; }
    }
}
