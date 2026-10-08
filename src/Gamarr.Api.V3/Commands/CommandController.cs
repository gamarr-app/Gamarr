using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Composition;
using NzbDrone.Common.Serializer;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaFiles.GameImport.Manual;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;
using NzbDrone.SignalR;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;
using Gamarr.Http.Validation;

namespace Gamarr.Api.V3.Commands
{
    [V3ApiController]
    public class CommandController : RestControllerWithSignalR<CommandResource, CommandModel>, IHandle<CommandUpdatedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly KnownTypes _knownTypes;
        private readonly Debouncer _debouncer;
        private readonly Dictionary<int, CommandResource> _pendingUpdates;

        private readonly CommandPriorityComparer _commandPriorityComparer = new CommandPriorityComparer();

        public CommandController(IManageCommandQueue commandQueueManager,
                             IBroadcastSignalRMessage signalRBroadcaster,
                             KnownTypes knownTypes)
            : base(signalRBroadcaster)
        {
            _commandQueueManager = commandQueueManager;
            _knownTypes = knownTypes;

            _debouncer = new Debouncer(SendUpdates, TimeSpan.FromSeconds(0.1));
            _pendingUpdates = new Dictionary<int, CommandResource>();

            PostValidator.RuleFor(c => c.Name).NotBlank();
        }

        protected override CommandResource GetResourceById(int id)
        {
            return _commandQueueManager.Get(id).ToResource();
        }

        [RestPostById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<CommandResource> StartCommand([FromBody] CommandResource commandResource)
        {
            var commandType =
                _knownTypes.GetImplementations(typeof(Command))
                               .SingleOrDefault(c => c.Name.Replace("Command", "")
                                             .Equals(commandResource.Name, StringComparison.InvariantCultureIgnoreCase));

            if (commandType == null)
            {
                throw new BadRequestException($"Unknown command '{commandResource.Name}'");
            }

            // A command with no registered handler would be queued happily and then
            // fail to resolve on the executor thread, logging an error nobody can act on.
            if (!_knownTypes.GetImplementations(typeof(IExecute<>).MakeGenericType(commandType)).Any())
            {
                throw new BadRequestException($"Command '{commandResource.Name}' has no handler");
            }

            Request.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(Request.Body))
            {
                var body = reader.ReadToEnd();
                var priority = commandType == typeof(ManualImportCommand)
                    ? CommandPriority.High
                    : CommandPriority.Normal;

                var command = STJson.Deserialize(body, commandType) as Command;

                if (command == null)
                {
                    throw new BadRequestException($"Invalid body for command '{commandResource.Name}'");
                }

                // Required-input checks before the command is queued: without these a
                // missing field surfaces as an exception on the executor thread, which
                // the client never sees and which reaches Sentry as a crash.
                var validationFailures = command.GetValidationFailures().ToList();

                if (validationFailures.Any())
                {
                    throw new BadRequestException($"Invalid {commandResource.Name} command: {string.Join(", ", validationFailures)}");
                }

                command.SuppressMessages = !command.SendUpdatesToClient;
                command.SendUpdatesToClient = true;
                command.ClientUserAgent = Request.Headers["UserAgent"];

                var trackedCommand = _commandQueueManager.Push(command, priority, CommandTrigger.Manual);

                return Created(trackedCommand.Id);
            }
        }

        [HttpGet]
        public List<CommandResource> GetStartedCommands()
        {
            return _commandQueueManager.All()
                .OrderBy(c => c.Status, _commandPriorityComparer)
                .ThenByDescending(c => c.Priority)
                .ToResource();
        }

        [RestDeleteById]
        public void CancelCommand(int id)
        {
            _commandQueueManager.Cancel(id);
        }

        [NonAction]
        public void Handle(CommandUpdatedEvent message)
        {
            if (message.Command.Body.SendUpdatesToClient)
            {
                lock (_pendingUpdates)
                {
                    _pendingUpdates[message.Command.Id] = message.Command.ToResource();
                }

                _debouncer.Execute();
            }
        }

        private void SendUpdates()
        {
            lock (_pendingUpdates)
            {
                var pendingUpdates = _pendingUpdates.Values.ToArray();
                _pendingUpdates.Clear();

                foreach (var pendingUpdate in pendingUpdates)
                {
                    BroadcastResourceChange(ModelAction.Updated, pendingUpdate);

                    if (pendingUpdate.Name == typeof(MessagingCleanupCommand).Name.Replace("Command", "") &&
                        pendingUpdate.Status == CommandStatus.Completed)
                    {
                        BroadcastResourceChange(ModelAction.Sync);
                    }
                }
            }
        }
    }
}
