﻿using System;
using System.Collections.Generic;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Deploy.Artifacts;
using Umbraco.Commerce.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.Commerce.Deploy.Connectors.ServiceConnectors
{
    [UdiDefinition(UmbracoCommerceConstants.UdiEntityType.ExportTemplate, UdiType.GuidUdi)]
    public class UmbracoCommerceExportTemplateServiceConnector : UmbracoCommerceStoreEntityServiceConnectorBase<ExportTemplateArtifact, ExportTemplateReadOnly, ExportTemplate, ExportTemplateState>
    {
        public override int[] ProcessPasses => new[]
        {
            2
        };

        public override string[] ValidOpenSelectors => new[]
        {
            "this",
            "this-and-descendants",
            "descendants"
        };

        public override string AllEntitiesRangeName => "All Umbraco Commerce Export Templates";

        public override string UdiEntityType => UmbracoCommerceConstants.UdiEntityType.ExportTemplate;

        public UmbracoCommerceExportTemplateServiceConnector(IUmbracoCommerceApi umbracoCommerceApi, UmbracoCommerceDeploySettingsAccessor settingsAccessor)
            : base(umbracoCommerceApi, settingsAccessor)
        { }

        public override string GetEntityName(ExportTemplateReadOnly entity)
            => entity.Name;

        public override ExportTemplateReadOnly GetEntity(Guid id)
            => _umbracoCommerceApi.GetExportTemplate(id);

        public override IEnumerable<ExportTemplateReadOnly> GetEntities(Guid storeId)
            => _umbracoCommerceApi.GetExportTemplates(storeId);

        public override ExportTemplateArtifact GetArtifact(GuidUdi udi, ExportTemplateReadOnly entity)
        {
            if (entity == null)
                return null;

            var storeUdi = new GuidUdi(UmbracoCommerceConstants.UdiEntityType.Store, entity.StoreId);

            var dependencies = new ArtifactDependencyCollection
            {
                new UmbracoCommerceArtifactDependency(storeUdi)
            };

            return new ExportTemplateArtifact(udi, storeUdi, dependencies)
            {
                Name = entity.Name,
                Alias = entity.Alias,
                Category = (int)entity.Category,
                FileMimeType = entity.FileMimeType,
                FileExtension = entity.FileExtension,
                ExportStrategy = (int)entity.ExportStrategy,
                TemplateView = entity.TemplateView,
                SortOrder = entity.SortOrder
            };
        }

        public override void Process(ArtifactDeployState<ExportTemplateArtifact, ExportTemplateReadOnly> state, IDeployContext context, int pass)
        {
            state.NextPass = GetNextPass(ProcessPasses, pass);

            switch (pass)
            {
                case 2:
                    Pass2(state, context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pass));
            }
        }

        private void Pass2(ArtifactDeployState<ExportTemplateArtifact, ExportTemplateReadOnly> state, IDeployContext context)
        {
            _umbracoCommerceApi.Uow.Execute(uow =>
            {
                var artifact = state.Artifact;

                artifact.Udi.EnsureType(UmbracoCommerceConstants.UdiEntityType.ExportTemplate);
                artifact.StoreUdi.EnsureType(UmbracoCommerceConstants.UdiEntityType.Store);

                var entity = state.Entity?.AsWritable(uow) ?? ExportTemplate.Create(uow, artifact.Udi.Guid, artifact.StoreUdi.Guid, artifact.Alias, artifact.Name);

                entity.SetName(artifact.Name, artifact.Alias)
                    .SetCategory((TemplateCategory)artifact.Category)
                    .SetFileMimeType(artifact.FileMimeType)
                    .SetFileExtension(artifact.FileExtension)
                    .SetExportStrategy((ExportStrategy)artifact.ExportStrategy)
                    .SetTemplateView(artifact.TemplateView)
                    .SetSortOrder(artifact.SortOrder);

                _umbracoCommerceApi.SaveExportTemplate(entity);

                uow.Complete();
            });
        }
    }
}
