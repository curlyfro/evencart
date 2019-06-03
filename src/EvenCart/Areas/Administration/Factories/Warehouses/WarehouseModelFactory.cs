﻿using EvenCart.Areas.Administration.Factories.Addresses;
using EvenCart.Areas.Administration.Models.Warehouse;
using EvenCart.Data.Entity.Shop;

namespace EvenCart.Areas.Administration.Factories.Warehouses
{
    public class WarehouseModelFactory : IWarehouseModelFactory
    {
        private readonly IAddressModelFactory _addressModelFactory;
        public WarehouseModelFactory(IAddressModelFactory addressModelFactory)
        {
            _addressModelFactory = addressModelFactory;
        }

        public WarehouseModel Create(Warehouse entity)
        {
            var model = new WarehouseModel()
            {
                Id = entity.Id
            };
            if (entity.Address != null)
                model.Address = _addressModelFactory.Create(entity.Address);
            return model;
        }
    }
}