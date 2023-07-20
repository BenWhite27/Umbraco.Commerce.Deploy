using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Deploy.Artifacts;
using Umbraco.Commerce.Deploy.Configuration;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.Commerce.Deploy.Connectors.ServiceConnectors
{
    [UdiDefinition(UmbracoCommerceConstants.UdiEntityType.ShippingMethod, UdiType.GuidUdi)]
    public class UmbracoCommerceShippingMethodServiceConnector : UmbracoCommerceStoreEntityServiceConnectorBase<ShippingMethodArtifact, ShippingMethodReadOnly, ShippingMethod, ShippingMethodState>
    {
        public override int[] ProcessPasses => new[]
        {
            2,4
        };

        public override string[] ValidOpenSelectors => new[]
        {
            "this",
            "this-and-descendants",
            "descendants"
        };

        public override string AllEntitiesRangeName => "All Umbraco Commerce Shipping Methods";

        public override string UdiEntityType => UmbracoCommerceConstants.UdiEntityType.ShippingMethod;

        public UmbracoCommerceShippingMethodServiceConnector(IUmbracoCommerceApi umbracoCommerceApi, UmbracoCommerceDeploySettingsAccessor settingsAccessor)
            : base(umbracoCommerceApi, settingsAccessor)
        { }

        public override string GetEntityName(ShippingMethodReadOnly entity)
            => entity.Name;

        public override ShippingMethodReadOnly GetEntity(Guid id)
            => _umbracoCommerceApi.GetShippingMethod(id);

        public override IEnumerable<ShippingMethodReadOnly> GetEntities(Guid storeId)
            => _umbracoCommerceApi.GetShippingMethods(storeId);

        public override ShippingMethodArtifact GetArtifact(GuidUdi udi, ShippingMethodReadOnly entity)
        {
            if (entity == null)
                return null;

            var storeUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Store, entity.StoreId);

            var dependencies = new ArtifactDependencyCollection
            {
                new UmbracoCommerceArtifactDependency(storeUdi)
            };

            var artifact = new ShippingMethodArtifact(udi, storeUdi, dependencies)
            {
                Name = entity.Name,
                Alias = entity.Alias,
                Sku = entity.Sku,
                ImageId = entity.ImageId, // Could be a UDI?
                SortOrder = entity.SortOrder
            };

            // Tax class
            if (entity.TaxClassId != null)
            {
                var taxClassDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.TaxClass, entity.TaxClassId.Value);
                var taxClassDep = new UmbracoCommerceArtifactDependency(taxClassDepUdi);

                dependencies.Add(taxClassDep);

                artifact.TaxClassUdi = taxClassDepUdi;
            }

            // Service prices
            if (entity.Prices.Count > 0)
            {
                var servicesPrices = new List<ServicePriceArtifact>();

                foreach (var price in entity.Prices.OrderBy(x => x.CountryId).ThenBy(x => x.RegionId).ThenBy(x => x.CurrencyId))
                {
                    var spArtifact = new ServicePriceArtifact { Value = price.Value };

                    // Currency
                    var currencyDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Currency, price.CurrencyId);
                    var currencyDep = new UmbracoCommerceArtifactDependency(currencyDepUdi);

                    dependencies.Add(currencyDep);

                    spArtifact.CurrencyUdi = currencyDepUdi;

                    // Country
                    if (price.CountryId.HasValue)
                    {
                        var countryDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Country, price.CountryId.Value);
                        var countryDep = new UmbracoCommerceArtifactDependency(countryDepUdi);

                        dependencies.Add(countryDep);

                        spArtifact.CountryUdi = countryDepUdi;
                    }

                    // Region
                    if (price.RegionId.HasValue)
                    {
                        var regionDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Region, price.RegionId.Value);
                        var regionDep = new UmbracoCommerceArtifactDependency(regionDepUdi);

                        dependencies.Add(regionDep);

                        spArtifact.RegionUdi = regionDepUdi;
                    }

                    servicesPrices.Add(spArtifact);
                }

                artifact.Prices = servicesPrices;
            }

            // Allowed country regions
            if (entity.AllowedCountryRegions.Count > 0)
            {
                var allowedCountryRegions = new List<AllowedCountryRegionArtifact>();

                foreach (var acr in entity.AllowedCountryRegions.OrderBy(x => x.CountryId).ThenBy(x => x.RegionId))
                {
                    var acrArtifact = new AllowedCountryRegionArtifact();

                    // Country
                    var countryDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Country, acr.CountryId);
                    var countryDep = new UmbracoCommerceArtifactDependency(countryDepUdi);

                    dependencies.Add(countryDep);

                    acrArtifact.CountryUdi = countryDepUdi;

                    // Region
                    if (acr.RegionId.HasValue)
                    {
                        var regionDepUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Region, acr.RegionId.Value);
                        var regionDep = new UmbracoCommerceArtifactDependency(regionDepUdi);

                        dependencies.Add(regionDep);

                        acrArtifact.RegionUdi = regionDepUdi;
                    }

                    allowedCountryRegions.Add(acrArtifact);
                }

                artifact.AllowedCountryRegions = allowedCountryRegions;
            }

            return artifact;
        }

        public override void Process(ArtifactDeployState<ShippingMethodArtifact, ShippingMethodReadOnly> state, IDeployContext context, int pass)
        {
            state.NextPass = GetNextPass(ProcessPasses, pass);

            switch (pass)
            {
                case 2:
                    Pass2(state, context);
                    break;
                case 4:
                    Pass4(state, context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pass));
            }
        }

        private void Pass2(ArtifactDeployState<ShippingMethodArtifact, ShippingMethodReadOnly> state, IDeployContext context)
        {
            _umbracoCommerceApi.Uow.Execute(uow =>
            {
                var artifact = state.Artifact;

                artifact.Udi.EnsureType(UmbracoCommerceConstants.UdiEntityType.ShippingMethod);
                artifact.StoreUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Store);

                var entity = state.Entity?.AsWritable(uow) ?? ShippingMethod.Create(uow, artifact.Udi.Guid, artifact.StoreUdi.Guid, artifact.Alias, artifact.Name);

                entity.SetName(artifact.Name, artifact.Alias)
                    .SetSku(artifact.Sku)
                    .SetImage(artifact.ImageId)
                    .SetSortOrder(artifact.SortOrder);

                _umbracoCommerceApi.SaveShippingMethod(entity);

                state.Entity = entity;

                uow.Complete();
            });
        }

        private void Pass4(ArtifactDeployState<ShippingMethodArtifact, ShippingMethodReadOnly> state, IDeployContext context)
        {
            _umbracoCommerceApi.Uow.Execute(uow =>
            {
                var artifact = state.Artifact;
                var entity = _umbracoCommerceApi.GetShippingMethod(state.Entity.Id).AsWritable(uow);

                // TaxClass
                if (artifact.TaxClassUdi != null)
                {
                    artifact.TaxClassUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.TaxClass);
                    // TODO: Check the payment method exists?
                    entity.SetTaxClass(artifact.TaxClassUdi.Guid);
                }
                else
                {
                    entity.ClearTaxClass();
                }

                // Prices
                var pricesToRemove = entity.Prices
                    .Where(x => artifact.Prices == null || !artifact.Prices.Any(y => y.CountryUdi?.Guid == x.CountryId
                        && y.RegionUdi?.Guid == x.RegionId
                        && y.CurrencyUdi.Guid == x.CurrencyId))
                    .ToList();

                if (artifact.Prices != null)
                {
                    foreach (var price in artifact.Prices)
                    {
                        price.CurrencyUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Currency);

                        if (price.CountryUdi == null && price.RegionUdi == null)
                        {
                            entity.SetDefaultPriceForCurrency(price.CurrencyUdi.Guid, price.Value);
                        }
                        else
                        {
                            price.CountryUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Country);

                            if (price.RegionUdi != null)
                            {
                                price.RegionUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Region);

                                entity.SetRegionPriceForCurrency(price.CountryUdi.Guid, price.RegionUdi.Guid, price.CurrencyUdi.Guid, price.Value);
                            }
                            else
                            {
                                entity.SetCountryPriceForCurrency(price.CountryUdi.Guid, price.CurrencyUdi.Guid, price.Value);
                            }
                        }
                    }
                }

                foreach (var price in pricesToRemove)
                {
                    if (price.CountryId == null && price.RegionId == null)
                    {
                        entity.ClearDefaultPriceForCurrency(price.CurrencyId);
                    }
                    else if (price.CountryId != null && price.RegionId == null)
                    {
                        entity.ClearCountryPriceForCurrency(price.CountryId.Value, price.CurrencyId);
                    }
                    else
                    {
                        entity.ClearRegionPriceForCurrency(price.CountryId.Value, price.RegionId.Value, price.CurrencyId);
                    }
                }

                // AllowedCountryRegions
                var allowedCountryRegionsToRemove = entity.AllowedCountryRegions
                    .Where(x => artifact.AllowedCountryRegions == null || !artifact.AllowedCountryRegions.Any(y => y.CountryUdi.Guid == x.CountryId
                        && y.RegionUdi?.Guid == x.RegionId))
                    .ToList();

                if (artifact.AllowedCountryRegions != null)
                {
                    foreach (var acr in artifact.AllowedCountryRegions)
                    {
                        acr.CountryUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Country);

                        if (acr.RegionUdi != null)
                        {
                            acr.RegionUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Region);

                            entity.AllowInRegion(acr.CountryUdi.Guid, acr.RegionUdi.Guid);
                        }
                        else
                        {
                            entity.AllowInCountry(acr.CountryUdi.Guid);
                        }
                    }
                }

                foreach (var acr in allowedCountryRegionsToRemove)
                {
                    if (acr.RegionId != null)
                    {
                        entity.DisallowInRegion(acr.CountryId, acr.RegionId.Value);
                    }
                    else
                    {
                        entity.DisallowInCountry(acr.CountryId);
                    }
                }

                _umbracoCommerceApi.SaveShippingMethod(entity);

                uow.Complete();
            });
        }
    }
}
