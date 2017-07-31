using System.Linq;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using Tests.Utils;
using Req = Checkout.ApiServices.Basket.RequestModels;
using Resp = Checkout.ApiServices.Basket.ResponseModels;

namespace Tests
{
    [TestFixture(Category = "BasketApi")]
    public class BasketServiceTests : BaseServiceTests
    {
        [Test]
        public void CreateBasket()
        {
            //When
            var response = CheckoutClient.BasketService.CreateNewBasket();
            //Then
            response.Should().NotBeNull();
            response.HttpStatusCode.Should().Be(HttpStatusCode.Created);
            response.Model.Id.Should().NotBeEmpty();
        }

        [Test]
        public void AddItemToBasket()
        {
            //Given
            var basketId = CheckoutClient.BasketService.CreateNewBasket().Model.Id;
            var item = new Req.Item { Name = "Sprite", Quantity = 10 };
            //When
            var response = CheckoutClient.BasketService.AddNewItem(basketId, item);
            //Then
            response.HttpStatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Test]
        public void UpdateItemsInBasket()
        {
            //Given
            var basketId = CheckoutClient.BasketService.CreateNewBasket().Model.Id;
            var item = new Req.Item { Name = "Sprite", Quantity = 10 };
            CheckoutClient.BasketService.AddNewItem(basketId, item);
            //When
            item.Quantity = 3;
            CheckoutClient.BasketService.UpdateItem(basketId, item);
            var response = CheckoutClient.BasketService.GetItem(basketId, item.Name);
            //Then
            response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
            response.Model.Items.Should().HaveCount(1);
            response.Model.Items.First().Quantity.Should().Be(item.Quantity);
        }

        [Test]
        public void GetItemFromBasket()
        {
            //Given
            var basketId = CheckoutClient.BasketService.CreateNewBasket().Model.Id;
            var item = new Req.Item { Name = "Sprite", Quantity = 10 };
            CheckoutClient.BasketService.AddNewItem(basketId, item);
            //When
            var response = CheckoutClient.BasketService.GetItem(basketId, item.Name);
            //Then
            response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
            response.Model.Items.First().Name.Should().Be(item.Name);
            response.Model.Items.First().Quantity.Should().Be(item.Quantity);
        }

        [Test]
        public void GetAllItemsInBasket()
        {
            //Given
            var basketId = CheckoutClient.BasketService.CreateNewBasket().Model.Id;
            var item1 = new Req.Item { Name = "Sprite", Quantity = 10 };
            var item2 = new Req.Item { Name = "Fanta", Quantity = 2 };
            CheckoutClient.BasketService.AddNewItem(basketId, item1);
            CheckoutClient.BasketService.AddNewItem(basketId, item2);
            //When
            var response = CheckoutClient.BasketService.Get(basketId);
            //Then
            response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
            response.Model.Items.Should().HaveCount(2);
            response.Model.Items.Any(i => i.Name == item1.Name && i.Quantity == item1.Quantity);
            response.Model.Items.Any(i => i.Name == item2.Name && i.Quantity == item2.Quantity);
        }

        [Test]
        public void DeleteItemFromBasket()
        {
            //Given
            var basketId = CheckoutClient.BasketService.CreateNewBasket().Model.Id;
            var item1 = new Req.Item { Name = "Sprite", Quantity = 10 };
            var item2 = new Req.Item { Name = "Fanta", Quantity = 2 };
            CheckoutClient.BasketService.AddNewItem(basketId, item1);
            CheckoutClient.BasketService.AddNewItem(basketId, item2);

            //When
            var response = CheckoutClient.BasketService.DeleteItem(basketId, item1.Name);
            
            //Then
            response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
            var getItemResponse = CheckoutClient.BasketService.GetItem(basketId, item1.Name);

            getItemResponse.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);

            var getBasketResponse = CheckoutClient.BasketService.Get(basketId);
            getBasketResponse.Model.Items.Should().HaveCount(1);
            getBasketResponse.Model.Items.First().Name.Should().Be(item2.Name);
            getBasketResponse.Model.Items.First().Quantity.Should().Be(item2.Quantity);
        }

 
    }
}