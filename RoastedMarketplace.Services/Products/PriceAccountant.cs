﻿using System;
using System.Collections.Generic;
using System.Linq;
using RoastedMarketplace.Core.Extensions;
using RoastedMarketplace.Core.Services;
using RoastedMarketplace.Data.Entity.Addresses;
using RoastedMarketplace.Data.Entity.Promotions;
using RoastedMarketplace.Data.Entity.Purchases;
using RoastedMarketplace.Data.Entity.Settings;
using RoastedMarketplace.Data.Entity.Shop;
using RoastedMarketplace.Data.Entity.Users;
using RoastedMarketplace.Services.Extensions;
using RoastedMarketplace.Services.Helpers;
using RoastedMarketplace.Services.Promotions;
using RoastedMarketplace.Services.Purchases;
using RoastedMarketplace.Services.Taxes;
using RoastedMarketplace.Services.Users;

namespace RoastedMarketplace.Services.Products
{
    public class PriceAccountant : IPriceAccountant
    {
        private readonly IDiscountCouponService _discountCouponService;
        private readonly IUserService _userService;
        private readonly ICartItemService _cartItemService;
        private readonly IProductService _productService;
        private readonly ITaxAccountant _taxAccountant;
        private readonly TaxSettings _taxSettings;
        private readonly ICartService _cartService;
        private readonly IProductVariantService _productVariantService;

        public PriceAccountant(IDiscountCouponService discountCouponService, IUserService userService, ICartItemService cartItemService, IProductService productService, ITaxAccountant taxAccountant, TaxSettings taxSettings, ICartService cartService, IProductVariantService productVariantService)
        {
            _discountCouponService = discountCouponService;
            _userService = userService;
            _cartItemService = cartItemService;
            _productService = productService;
            _taxAccountant = taxAccountant;
            _taxSettings = taxSettings;
            _cartService = cartService;
            _productVariantService = productVariantService;
        }

        public DiscountApplicationStatus ApplyDiscountCoupon(string couponCode, Cart cart)
        {
            if (couponCode.IsNullEmptyOrWhitespace())
            {
                return DiscountApplicationStatus.InvalidCode;
            }

            //first get the coupon
            var discountCoupon = _discountCouponService.GetByCouponCode(couponCode);
            return ApplyDiscountCoupon(discountCoupon, cart);
        }

        public DiscountApplicationStatus ApplyDiscountCoupon(DiscountCoupon discountCoupon, Cart cart)
        {
            if (discountCoupon == null || !discountCoupon.Enabled)
                return DiscountApplicationStatus.InvalidCode;
            if (discountCoupon.Expired)
                return DiscountApplicationStatus.Expired;

            //first the dates
            if (discountCoupon.StartDate > DateTime.UtcNow)
                return DiscountApplicationStatus.InvalidCode;
            if (discountCoupon.EndDate.HasValue && discountCoupon.EndDate < DateTime.UtcNow)
                return DiscountApplicationStatus.Expired;

            var cartItemUpdated = false;
            var cartUpdated = false;
            //check for restriction type
            switch (discountCoupon.RestrictionType)
            {
                case RestrictionType.Products:
                    cartItemUpdated = ApplyProductDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.Categories:
                    cartItemUpdated = ApplyCategoryDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.Users:
                    cartItemUpdated = ApplyUserDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.UserGroups:
                    cartItemUpdated = ApplyUserGroupDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.Roles:
                    cartItemUpdated = ApplyRoleDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.Vendors:
                    cartItemUpdated = ApplyVendorDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.Manufacturers:
                    cartItemUpdated = ApplyManufacturerDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.PaymentMethods:
                    cartUpdated = ApplyPaymentMethodDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.ShippingMethods:
                    cartUpdated = ApplyShippingMethodDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.OrderTotal:
                    cartUpdated = ApplyOrderTotalDiscount(discountCoupon, cart);
                    break;
                case RestrictionType.OrderSubTotal:
                    cartUpdated = ApplyOrderSubTotalDiscount(discountCoupon, cart);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (cartUpdated)
            {
                cart.DiscountCouponId = discountCoupon.Id;
                _cartService.Update(cart);
                return DiscountApplicationStatus.Success;
            }
            if (cartItemUpdated)
            {
                foreach (var cartItem in cart.CartItems)
                    _cartItemService.Update(cartItem);

                return DiscountApplicationStatus.Success;
            }
            return DiscountApplicationStatus.NotEligibleForCart;
        }

        public void ClearCouponCode(Cart cart)
        {
            cart.DiscountCouponId = 0;
            cart.DiscountCoupon = null;
            cart.Discount = 0;
            _cartService.Update(cart);
            //refresh pricing
            RefreshCartParameters(cart);
        }

        public void RefreshCartParameters(Cart cart)
        {
            //update prices if we need to
            var cartProductIds = cart.CartItems.Select(x => x.ProductId).ToList();
            if (cartProductIds.Any())
            {
                var products = _productService.GetProducts(cartProductIds);
                var productVariants = _productVariantService.Get(x => cartProductIds.Contains(x.ProductId)).ToList();

                Transaction.Initiate(transaction =>
                {
                    //update cart items if required
                    foreach (var ci in cart.CartItems)
                    {
                        var product = products.FirstOrDefault(x => x.Id == ci.ProductId);
                        if (product == null)
                        {
                            //remove from cart because we can't find the product
                            _cartService.RemoveFromCart(ci.Id, transaction);
                        }
                        else
                        {
                            if (!product.Published || product.Deleted)
                            {
                                //remove from cart because product shouldn't be visible
                                _cartService.RemoveFromCart(ci.Id, transaction);
                            }
                            else if (product.TrackInventory)
                            {
                                if (!product.HasVariants && product.StockQuantity == 0)
                                {
                                    //remove from cart because we can't find the product
                                    _cartService.RemoveFromCart(ci.Id, transaction);
                                }
                            }
                            if (ci.Quantity < product.MinimumPurchaseQuantity)
                            {
                                //is there a difference in quantity that's required
                                ci.Quantity = product.MinimumPurchaseQuantity;
                            }
                            if (ci.Quantity > product.MaximumPurchaseQuantity)
                            {
                                //is there a difference in quantity that's required
                                ci.Quantity = product.MaximumPurchaseQuantity;
                            }

                            if (product.HasVariants && ci.ProductVariantId > 0)
                            {
                                //find the product variants
                                var variant = productVariants.FirstOrDefault(x => x.Id == ci.ProductVariantId);
                                if (variant == null || (product.TrackInventory && variant.StockQuantity == 0))
                                    //remove from cart because we can't find the variant or it's out of stock
                                    _cartService.RemoveFromCart(ci.Id, transaction);
                                else
                                {
                                    var comparisonPrice = variant.ComparePrice ?? product.ComparePrice;
                                    var price = variant.Price ?? product.Price;
                                    GetProductPriceDetails(product, cart.BillingAddress, price, out decimal priceWithoutTax, out decimal tax, out decimal taxRate);

                                    //do we need an update?
                                    if (priceWithoutTax != ci.Price || comparisonPrice != ci.ComparePrice || tax != ci.Tax || taxRate != ci.TaxPercent)
                                    {
                                        ci.Price = priceWithoutTax;
                                        ci.ComparePrice = ci.ComparePrice;
                                        ci.Tax = tax * ci.Quantity;
                                        ci.TaxPercent = taxRate;
                                        ci.Discount = 0;
                                        _cartItemService.Update(ci);
                                    }
                                }
                            }
                            else
                            {
                                GetProductPriceDetails(product, cart.BillingAddress, null, out decimal priceWithoutTax, out decimal tax, out decimal taxRate);
                                tax = tax * ci.Quantity;
                                //do we need an update?
                                if (priceWithoutTax != ci.Price || product.ComparePrice != ci.ComparePrice || tax != ci.Tax || taxRate != ci.TaxPercent)
                                {
                                    ci.Price = priceWithoutTax;
                                    ci.ComparePrice = product.ComparePrice;
                                    ci.Tax = tax;
                                    ci.TaxPercent = taxRate;
                                    ci.Discount = 0;
                                    ci.FinalPrice = ci.Price * ci.Quantity;
                                    _cartItemService.Update(ci);
                                }
                            }

                        }
                    }

                    //do we have an discount coupon
                    if (cart.DiscountCoupon != null)
                    {
                        if (ApplyDiscountCoupon(cart.DiscountCoupon, cart) == DiscountApplicationStatus.Success)
                        {
                            //cart already updated so return
                            return;
                        }
                    }

                   /* cart.FinalAmount = cart.CartItems.Sum(x => x.FinalPrice);
                    cart.CompareFinalAmount = cart.CartItems.Sum(x => x.ComparePrice ?? 0);
                    //do we need to apply discount
                    _cartService.Update(cart);*/

                });
            }
        }

        public decimal GetAutoDiscountedPriceForUser(Product product, User user, ref IList<DiscountCoupon> discountCoupons)
        {
            //get active discount coupons which don't have any code
            discountCoupons = discountCoupons ?? _discountCouponService.Get(x => x.Enabled && !x.Expired && !x.HasCouponCode).ToList();
            var discount = decimal.Zero;
            foreach (var dc in discountCoupons)
            {
                var currentDiscount = GetProductDiscountedPrice(dc, product, user);
                if (currentDiscount > discount)
                    discount = currentDiscount;
            }
            return product.Price - discount;
        }

        public void GetProductPriceDetails(Product product, Address address, decimal? basePrice, out decimal price, out decimal tax, out decimal taxRate)
        {
            taxRate = _taxAccountant.GetFinalTaxRate(product, address);
            var fromBasePrice = basePrice ?? product.Price;
            if (_taxSettings.PricesIncludeTax)
            {
                price = (fromBasePrice * 100) / (taxRate + 100);
                tax = fromBasePrice - price;
            }
            else
            {
                tax = fromBasePrice * taxRate / 100;
                price = fromBasePrice;
            }
        }

        #region Helpers

        private decimal GetProductDiscountedPrice(DiscountCoupon discountCoupon, Product product, User user)
        {
            switch (discountCoupon.RestrictionType)
            {
                case RestrictionType.Products:
                    return discountCoupon.RestrictionIds().Contains(product.Id) ? discountCoupon.GetDiscountAmount(product.Price) : 0;
                case RestrictionType.Categories:
                    var categoryIds = discountCoupon.RestrictionIds().ToArray();
                    var categoryProductIds = _productService.GetProductIdsByCategoryIds(categoryIds);
                    return categoryProductIds.Contains(product.Id) ? discountCoupon.GetDiscountAmount(product.Price) : 0;
                case RestrictionType.Users:
                    return discountCoupon.RestrictionIds().Contains(user.Id) ? discountCoupon.GetDiscountAmount(product.Price) : 0;
                case RestrictionType.UserGroups:
                    return 0;
                case RestrictionType.Roles:
                    var roleIds = discountCoupon.RestrictionIds();
                    return user.Roles.Any(x => roleIds.Contains(x.Id)) ? discountCoupon.GetDiscountAmount(product.Price) : 0;
                case RestrictionType.Vendors:
                    var vendorIds = discountCoupon.RestrictionIds().ToArray();
                    var vendorProductIds = _productService.GetProductIdsByVendorIds(vendorIds);
                    return vendorProductIds.Contains(product.Id) ? discountCoupon.GetDiscountAmount(product.Price) : 0;
                case RestrictionType.Manufacturers:
                    return product.ManufacturerId.HasValue &&
                           discountCoupon.RestrictionIds().Contains(product.ManufacturerId.Value)
                        ? discountCoupon.GetDiscountAmount(product.Price)
                        : 0;
                case RestrictionType.PaymentMethods:
                case RestrictionType.ShippingMethods:
                case RestrictionType.OrderTotal:
                case RestrictionType.OrderSubTotal:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool ApplyProductDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var productIds = discountCoupon.RestrictionIds();
            var cartItemUpdated = false;
            foreach (var cartItem in cart.CartItems)
            {
                if (!productIds.Contains(cartItem.ProductId))
                    continue;
                cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                cartItemUpdated = true;
            }
            return cartItemUpdated;
        }

        private bool ApplyCategoryDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var categoryIds = discountCoupon.RestrictionIds().ToArray();
            var categoryProductIds = _productService.GetProductIdsByCategoryIds(categoryIds);
            var cartUpdated = false;
            foreach (var cartItem in cart.CartItems)
            {
                if (categoryProductIds.Contains(cartItem.ProductId))
                {
                    cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                    cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                    cartUpdated = true;
                }
            }
            return cartUpdated;
        }

        private bool ApplyUserDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var userIds = discountCoupon.RestrictionIds();
            if (userIds.Contains(cart.UserId))
            {
                foreach (var cartItem in cart.CartItems)
                {
                    cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                    cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                }
                return true;
            }
            return false;
        }

        private bool ApplyUserGroupDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            //todo: implement this
            return true;
        }

        private bool ApplyRoleDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var roleIds = discountCoupon.RestrictionIds();
            var user = _userService.Get(cart.UserId);
            if (user.Roles.Any(x => roleIds.Contains(x.Id)))
            {
                foreach (var cartItem in cart.CartItems)
                {
                    cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                    cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                }
                return true;
            }
            return false;
        }

        private bool ApplyVendorDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var vendorIds = discountCoupon.RestrictionIds().ToArray();
            var vendorProductIds = _productService.GetProductIdsByVendorIds(vendorIds);
            var cartUpdated = false;
            foreach (var cartItem in cart.CartItems)
            {
                if (vendorProductIds.Contains(cartItem.ProductId))
                {
                    cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                    cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                    cartUpdated = true;
                }
            }
            return cartUpdated;
        }

        private bool ApplyManufacturerDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var manufacturerIds = discountCoupon.RestrictionIds();
            var cartItemUpdated = false;
            foreach (var cartItem in cart.CartItems)
            {
                if (cartItem.Product.ManufacturerId.HasValue &&
                    manufacturerIds.Contains(cartItem.Product.ManufacturerId.Value))
                {
                    cartItem.Discount = discountCoupon.GetDiscountAmount(cartItem.Price);
                    cartItem.FinalPrice = cartItem.Price - cartItem.Discount;
                    cartItemUpdated = true;
                }
            }
            return cartItemUpdated;
        }

        private bool ApplyPaymentMethodDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var paymentMethodNames = discountCoupon.RestrictionValues();
            if (paymentMethodNames.Contains(cart.PaymentMethodName))
            {
                var paymentHandler = PluginHelper.GetPaymentHandler(cart.PaymentMethodName);
                var discount = discountCoupon.GetDiscountAmount(paymentHandler.GetPaymentHandlerFee(cart));
                cart.PaymentMethodFee = cart.PaymentMethodFee - discount;
                return true;
            }
            return false;
        }

        private bool ApplyShippingMethodDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            var shippingMethodNames = discountCoupon.RestrictionValues();
            if (shippingMethodNames.Contains(cart.ShippingMethodName))
            {
                var shippingHandler = PluginHelper.GetShipmentHandler(cart.ShippingMethodName);
                var discount = discountCoupon.GetDiscountAmount(shippingHandler.GetShippingHandlerFee(cart));
                cart.ShippingFee = cart.ShippingFee - discount;
                return true;
            }
            return false;
        }

        private bool ApplyOrderTotalDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            IList<DiscountCoupon> discountCoupons = null;
            var orderTotalForDiscount = decimal.Zero;
            var otherOrderTotal = decimal.Zero;
            foreach (var cartItem in cart.CartItems)
            {
                var discountedPrice = GetAutoDiscountedPriceForUser(cartItem.Product, cart.User, ref discountCoupons);
                if (discountedPrice < cartItem.Price)
                {
                    if (discountCoupon.ExcludeAlreadyDiscountedProducts)
                    {
                        otherOrderTotal += discountedPrice * cartItem.Quantity;
                        continue; //exclude this product
                    }
                    orderTotalForDiscount += discountedPrice * cartItem.Quantity;
                }
                else
                {
                    orderTotalForDiscount += cartItem.Tax + cartItem.Price * cartItem.Quantity;
                }
                cartItem.Discount = 0;
            }
            cart.Discount = discountCoupon.GetDiscountAmount(orderTotalForDiscount + otherOrderTotal);
            return true;
        }

        private bool ApplyOrderSubTotalDiscount(DiscountCoupon discountCoupon, Cart cart)
        {
            //todo: implement this
            return true;
        }
        #endregion


    }
}