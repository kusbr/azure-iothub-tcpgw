namespace SocketIoT.IoTHubProvider.Addressing
{
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
    using Microsoft.Extensions.Configuration;
    using SocketIoT.Core.Common;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public sealed class ConfigurableMessageAddressConverter : IMessageAddressConverter
    {
        static readonly Uri BaseUri = new Uri("http://x/", UriKind.Absolute);
        IList<UriPathTemplate> topicTemplateTable;
        static readonly IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();


        UriPathTemplate outboundTemplate;

        public ConfigurableMessageAddressConverter()
            : this("mqttTopicNameConversion")
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="ConfigurableMessageAddressConverter" />.
        /// </summary>
        /// <param name="configurationSectionName">Name of configuration section that contains routing configuration.</param>
        /// <remarks>
        ///     This constructor uses a section from application configuration to generate routing configuration.
        /// </remarks>
        /// <example>
        ///     <code>
        ///     <mqttTopicNameConversion>
        ///             <inboundTemplate>{deviceId}/messages/events</inboundTemplate>
        ///             <outboundTemplate>devices/{deviceId}/messages/devicebound/{*subTopic}</outboundTemplate>
        ///         </mqttTopicNameConversion>
        /// </code>
        /// </example>
        public ConfigurableMessageAddressConverter(string configurationSectionName)
        {
            Contract.Requires(!string.IsNullOrEmpty(configurationSectionName));

#if NETSTANDARD1_3
            var configuration = new MessageAddressConversionConfiguration();
            configurationRoot.GetSection(configurationSectionName).Bind(configuration);
#else
            var configuration =
(MessageAddressConversionConfiguration)ConfigurationManager.GetSection(configurationSectionName);
#endif

            this.InitializeFromConfiguration(configuration);
        }

        public ConfigurableMessageAddressConverter(MessageAddressConversionConfiguration configuration)
        {
            this.InitializeFromConfiguration(configuration);
        }

        public ConfigurableMessageAddressConverter(List<string> inboundTemplates, List<string> outboundTemplates)
        {
            var configuration = new MessageAddressConversionConfiguration(inboundTemplates, outboundTemplates);
            this.InitializeFromConfiguration(configuration);
        }

        void InitializeFromConfiguration(MessageAddressConversionConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            this.topicTemplateTable = new List<UriPathTemplate>();
            this.topicTemplateTable.Add(new UriPathTemplate(configuration.InboundTemplates[0]));
            this.outboundTemplate = configuration.OutboundTemplates.Select(x => new UriPathTemplate(x)).Single();

        }

        public IList<UriPathTemplate> TopicTemplates { get { return topicTemplateTable; } }

        public bool TryDeriveOutboundAddress(IMessage message, out string address)
        {
            UriPathTemplate template = this.outboundTemplate;
            try
            {
                address = template.Bind(message.Properties);
            }
            catch (InvalidOperationException)
            {
                address = null;
                return false;
            }
            return true;
        }

        public bool TryParseAddressIntoMessageProperties(string address, IMessage message)
        {
#if NETSTANDARD1_3
            return TryParseAddressIntoMessagePropertiesWithRegex(address, message);
#else
            return TryParseAddressIntoMessagePropertiesDefault(address, message);
#endif
        }


#if NETSTANDARD1_3

        private bool TryParseAddressIntoMessagePropertiesWithRegex(string address, IMessage message)
        {
            bool matched = false;
            foreach (UriPathTemplate uriPathTemplate in this.topicTemplateTable)
            {
                IList<KeyValuePair<string, string>> matches = uriPathTemplate.Match(new Uri(BaseUri, address));

                if (matches.Count == 0)
                {
                    continue;
                }

                if (matched)
                {
                    if (CommonEventSource.Log.IsVerboseEnabled)
                    {
                        //CommonEventSource.Log.Verbose("Topic name matches more than one route.", address);
                    }
                    break;
                }
                matched = true;

                int variableCount = matches.Count;
                for (int i = 0; i < variableCount; i++)
                {
                    // todo: this will unconditionally set property values - is it acceptable to overwrite existing value?
                    message.Properties.Add(matches[i].Key, matches[i].Value);
                }
            }
            return matched;
        }

#else
        private bool TryParseAddressIntoMessagePropertiesDefault(string address, IMessage message)
        {
            //Collection<UriTemplateMatch> matches = this.topicTemplateTable.Match(new Uri(BaseUri, address));
            Collection<KeyValuePair<string, string>> matches = new Collection<KeyValuePair<string, string>>();

            foreach(var urlPath in this.topicTemplateTable)
            {
                var theMatch = urlPath.Match(new Uri(BaseUri, address));
                if (theMatch != null && theMatch.Count() > 0)
                {
                    matches.ToList().AddRange(theMatch);
                }
            }

            if (matches.Count == 0)
            {
                return false;
            }

            if (matches.Count > 1)
            {
                //if (CommonEventSource.Log.IsVerboseEnabled)
                //{
                //    CommonEventSource.Log.Verbose("Topic name matches more than one route.", address);
                //}
            }

            ////UriTemplateMatch match = matches[0];
            //int variableCount = match.BoundVariables.Count;
            //for (int i = 0; i < variableCount; i++)
            //{
            //    // todo: this will unconditionally set property values - is it acceptable to overwrite existing value?
            //    message.Properties.Add(match.BoundVariables.GetKey(i), match.BoundVariables.Get(i));
            //}
            return true;
        }
#endif
    }
}