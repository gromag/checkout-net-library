using System;
using System.Collections.Generic;


namespace Checkout.ApiServices.Basket.ResponseModels
{
    public class Basket
    {
        public Guid Id { get; set; }
        public ICollection<Item> Items { get; set; }
    }

}
