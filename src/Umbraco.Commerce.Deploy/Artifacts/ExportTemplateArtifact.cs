﻿using System.Collections.Generic;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.Commerce.Deploy.Artifacts
{
    public class ExportTemplateArtifact(
        GuidUdi? udi,
        GuidUdi storeUdi,
        IEnumerable<ArtifactDependency> dependencies = null)
        : StoreEntityArtifactBase(udi, storeUdi, dependencies)
    {
        public int Category { get; set; }

        public string FileMimeType { get; set; }

        public string FileExtension { get; set; }

        public int ExportStrategy { get; set; }

        public string TemplateView { get; set; }

        public int SortOrder { get; set; }
    }
}
