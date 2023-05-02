﻿using Umbraco.Cms.Core;

namespace Umbraco.Commerce.Deploy
{
    public static class UdiHelper
    {
        public static bool TryParseGuidUdi(string input, out GuidUdi udi)
        {
            return UdiParser.TryParse<GuidUdi>(input, out udi);
        }
    }
}
