using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Messaging.Commands
{
    [JsonConverter(typeof(PolymorphicWriteOnlyJsonConverter<Command>))]
    public abstract class Command
    {
        private bool _sendUpdatesToClient;

        public virtual bool SendUpdatesToClient
        {
            get
            {
                return _sendUpdatesToClient;
            }

            set
            {
                _sendUpdatesToClient = value;
            }
        }

        public virtual bool UpdateScheduledTask => true;
        public virtual string CompletionMessage => null;
        public virtual bool RequiresDiskAccess => false;
        public virtual bool IsExclusive => false;
        public virtual bool IsTypeExclusive => false;
        public virtual bool IsLongRunning => false;

        public string Name { get; private set; }
        public DateTime? LastExecutionTime { get; set; }
        public DateTime? LastStartTime { get; set; }
        public CommandTrigger Trigger { get; set; }
        public bool SuppressMessages { get; set; }

        public string ClientUserAgent { get; set; }

        public Command()
        {
            Name = GetType().Name.Replace("Command", "");
        }

        /// <summary>
        /// Required-input checks for a command that arrived from an API client.
        /// Anything reported here is rejected with a 400 before the command is
        /// queued, so a missing field becomes a client error instead of an
        /// exception on the executor thread. Shape only: this runs without
        /// services, so it cannot check that an id exists.
        /// </summary>
        /// <remarks>A method, not a property, so it is never serialized into the command body.</remarks>
        public virtual IEnumerable<string> GetValidationFailures()
        {
            return Enumerable.Empty<string>();
        }
    }
}
