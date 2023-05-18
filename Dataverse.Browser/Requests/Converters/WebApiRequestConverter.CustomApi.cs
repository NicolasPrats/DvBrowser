using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.DOM;
using Dataverse.Browser.Constants;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Dataverse.Browser.Requests.Converter
{
    internal partial class WebApiRequestConverter
    {
        private void ConvertToAction(IEdmOperation operation, InterceptedWebApiRequest webApiRequest)
        {
            OrganizationRequest request = new OrganizationRequest(operation.Name);
            using (JsonDocument json = JsonDocument.Parse(webApiRequest.SimpleHttpRequest.Body))
            {
                foreach (var node in json.RootElement.EnumerateObject())
                {
                    string key = node.Name;
                    var parameter = operation.FindParameter(key);
                    if (parameter == null)
                    {
                        throw new NotSupportedException($"parameter {key} not found!");
                    }
                    request[key] = ConvertValueToAttribute(node.Value, parameter.Type);
                }
            }
            webApiRequest.ConvertedRequest = request;
        }

        private object ConvertValueToAttribute(JsonElement value, IEdmTypeReference type)
        {
            switch (type.FullName())
            {
                case "Edm.Boolean":
                    return value.GetBoolean();
                case "Edm.Byte":
                    return value.GetByte();
                case "Edm.DateTime":
                    return value.GetDateTime();
                case "Edm.Decimal":
                    return value.GetDecimal();
                case "Edm.Double":
                    return value.GetDouble();
                case "Edm.Single":
                    return value.GetSingle();
                case "Edm.Guid":
                    return value.GetGuid();
                case "Edm.Int16":
                    return value.GetInt16();
                case "Edm.Int32":
                    return value.GetInt32();
                case "Edm.Int64":
                    return value.GetInt64();
                case "Edm.SByte":
                    return value.GetSByte();
                case "Edm.String":
                    return value.GetString();                
            }
            throw new NotSupportedException("Type is unknown:" + type.FullName());
        }

    }
}
