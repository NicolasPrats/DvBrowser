using System;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Plugin.Emulator
{
    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/understand-the-data-context#outputparameters
    internal sealed class ResponsePropertyMapping
    {
        internal static readonly Dictionary<Type, (string, string)> Mapping = new Dictionary<Type, (string, string)>()
        {
            { typeof(BackgroundSendEmailResponse), ("EntityCollection", "BusinessEntityCollection") },
            { typeof(CloneContractResponse), ("Entity", "BusinessEntity") },
            { typeof(CloneMobileOfflineProfileResponse), ("CloneMobileOfflineProfile", "EntityReference") },
            { typeof(CloneProductResponse), ("ClonedProduct", "EntityReference") },
            { typeof(ConvertSalesOrderToInvoiceResponse), ("Entity", "BusinessEntity") },
            { typeof(CreateKnowledgeArticleTranslationResponse), ("CreateKnowledgeArticleTranslation", "EntityReference") },
            { typeof(CreateKnowledgeArticleVersionResponse), ("CreateKnowledgeArticleVersion", "EntityReference") },
            { typeof(GenerateQuoteFromOpportunityResponse), ("Entity", "BusinessEntity") },
            { typeof(GetDefaultPriceLevelResponse), ("PriceLevels", "BusinessEntityCollection") },
            { typeof(RetrieveResponse), ("Entity", "BusinessEntity") },
            { typeof(RetrieveMultipleResponse), ("EntityCollection", "BusinessEntityCollection") },
            { typeof(RetrievePersonalWallResponse), ("EntityCollection", "BusinessEntityCollection") },
            { typeof(RetrieveRecordWallResponse), ("EntityCollection", "BusinessEntityCollection") },
            { typeof(RetrieveUnpublishedResponse), ("Entity", "BusinessEntity") },
            { typeof(RetrieveUnpublishedMultipleResponse), ("EntityCollection", "BusinessEntityCollection") },
            {
                typeof(RetrieveUserQueuesResponse), ("EntityCollection", "BusinessEntityCollection") }
        };
    }
}
