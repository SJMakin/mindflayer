
#nullable enable

namespace Replicate
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class WebhooksDefaultSecretGetResponse
    {
        /// <summary>
        /// The signing secret.
        /// </summary>
        [global::System.Text.Json.Serialization.JsonPropertyName("key")]
        public string? Key { get; set; }

        /// <summary>
        /// Additional properties that are not explicitly defined in the schema
        /// </summary>
        [global::System.Text.Json.Serialization.JsonExtensionData]
        public global::System.Collections.Generic.IDictionary<string, object> AdditionalProperties { get; set; } = new global::System.Collections.Generic.Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhooksDefaultSecretGetResponse" /> class.
        /// </summary>
        /// <param name="key">
        /// The signing secret.
        /// </param>
        [global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public WebhooksDefaultSecretGetResponse(
            string? key)
        {
            this.Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhooksDefaultSecretGetResponse" /> class.
        /// </summary>
        public WebhooksDefaultSecretGetResponse()
        {
        }
    }
}