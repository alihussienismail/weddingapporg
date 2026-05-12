using System.Text.Json;
using weddingapporg.Models;

namespace weddingapporg.Services
{
    public class CartService
    {
        private const string CartKey = "WeddingCart";

        public List<CartItem> Get(ISession session)
        {
            var json = session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return new();
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new();
        }

        public void Add(ISession session, CartItem item)
        {
            var cart = Get(session);
            // For venues: replace if same service already in cart
            if (item.IsVenue)
                cart.RemoveAll(x => x.ServiceId == item.ServiceId);
            cart.Add(item);
            Save(session, cart);
        }

        public void Remove(ISession session, Guid cartItemId)
        {
            var cart = Get(session);
            cart.RemoveAll(x => x.CartItemId == cartItemId);
            Save(session, cart);
        }

        public void Clear(ISession session) => session.Remove(CartKey);

        public int Count(ISession session) => Get(session).Count;

        private void Save(ISession session, List<CartItem> cart)
            => session.SetString(CartKey, JsonSerializer.Serialize(cart));
    }
}
