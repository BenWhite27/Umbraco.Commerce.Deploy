﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Deploy.Configuration;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Commerce.Deploy.Connectors.ValueConnectors
{
    public class UmbracoCommercePriceValueConnector : IValueConnector
    {
        private readonly IUmbracoCommerceApi _umbracoCommerceApi;
        private readonly UmbracoCommerceDeploySettingsAccessor _settingsAccessor;

        public IEnumerable<string> PropertyEditorAliases => new[] { "Umbraco.Commerce.Price" };

        public UmbracoCommercePriceValueConnector(IUmbracoCommerceApi umbracoCommerceApi, UmbracoCommerceDeploySettingsAccessor settingsAccessor)
        {
            _umbracoCommerceApi = umbracoCommerceApi;
            _settingsAccessor = settingsAccessor;
        }

        public string ToArtifact(object value, IPropertyType propertyType, ICollection<ArtifactDependency> dependencies)
        {
            var svalue = value as string;

            if (string.IsNullOrWhiteSpace(svalue))
                return null;

            var srcDict = JsonConvert.DeserializeObject<Dictionary<Guid, decimal?>>(svalue);
            var dstDict = new Dictionary<GuidUdi, decimal?>();

            foreach (var kvp in srcDict)
            {
                var udi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Currency, kvp.Key);

                // Because we store Guid IDs anyway we don't neceserily need to fetch
                // the Currency entity to look anything up, it's mostly a question
                // of whether we want to validate the Currency exists. I'm not sure
                // whether this should really be the responsibility of the property editor
                // though and we should just be able to trust the property editor value
                // is valid?

                dependencies.Add(new UmbracoCommerceArtifactDependency(udi, ArtifactDependencyMode.Exist));

                dstDict.Add(udi, kvp.Value);
            }

            return JsonConvert.SerializeObject(dstDict);
        }

        public object FromArtifact(string value, IPropertyType propertyType, object currentValue)
        {
            var svalue = value as string;

            if (string.IsNullOrWhiteSpace(svalue))
                return null;

            var srcDict = JsonConvert.DeserializeObject<Dictionary<string, decimal?>>(svalue);
            var dstDict = new Dictionary<Guid, decimal?>();

            foreach (var kvp in srcDict)
            {
                if (UdiHelper.TryParseGuidUdi(kvp.Key, out var udi) && udi.EntityType == UmbracoCommerceConstants.UdiEntityType.Currency)
                {
                    var currencyEntity = _umbracoCommerceApi.GetCurrency(udi.Guid);
                    if (currencyEntity != null)
                    {
                        dstDict.Add(currencyEntity.Id, kvp.Value);
                    }
                }
            }

            return JsonConvert.SerializeObject(dstDict);
        }
    }
}
