
using Req = Checkout.ApiServices.Basket.RequestModels;
using Resp = Checkout.ApiServices.Basket.ResponseModels;
using Checkout.ApiServices.SharedModels;
using Checkout.Utilities;
using System;

namespace Checkout.ApiServices.Basket
{
    public class BasketService 
    {
        /// <summary>
        /// Creates a new empty shopping Basket.
        /// </summary>
        public HttpResponse<Resp.Basket> CreateNewBasket()
        {
            return new ApiHttpClient().PostRequest<Resp.Basket>(ApiUrls.BasketCreateNew, AppSettings.SecretKey,null);
        }

        /// <summary>
        /// Adds an item to the shopping Basket.
        /// </summary>
        public HttpResponse<OkResponse> AddNewItem(Guid basketId, Req.Item requestModel)
        {
            return new ApiHttpClient().PostRequest<OkResponse>(string.Format(ApiUrls.BasketAddNewItem, basketId), AppSettings.SecretKey, requestModel);
        }
        /// <summary>
        /// Updates an item from the shopping basket. Note this method is idempotent.
        /// </summary>
        /// <param name="basketId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public HttpResponse<OkResponse> UpdateItem(Guid basketId, Req.Item item)
        {
            return new ApiHttpClient().PutRequest<OkResponse>(string.Format(ApiUrls.BasketUpdateItem, basketId), AppSettings.SecretKey, item);
        }
        /// <summary>
        /// Get an item from the shopping basket.
        /// </summary>
        /// <param name="basketId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public HttpResponse<Resp.Basket> GetItem(Guid basketId, string name)
        {
            return new ApiHttpClient().GetRequest<Resp.Basket>(string.Format(ApiUrls.BasketGetItem, basketId, name), AppSettings.SecretKey);
        }
        /// <summary>
        /// Get all items from the shopping basket.
        /// </summary>
        /// <param name="basketId"></param>
        /// <returns></returns>
        public HttpResponse<Resp.Basket> Get(Guid basketId)
        {
            return new ApiHttpClient().GetRequest<Resp.Basket>(string.Format(ApiUrls.BasketGet, basketId), AppSettings.SecretKey);
        }
        /// <summary>
        /// Delete an item from the shopping basket.
        /// </summary>
        /// <param name="basketId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public HttpResponse<OkResponse> DeleteItem(Guid basketId, string name)
        {
            return new ApiHttpClient().DeleteRequest<OkResponse>(string.Format(ApiUrls.BasketDeleteItem, basketId, name ), AppSettings.SecretKey);
        }
    }
}