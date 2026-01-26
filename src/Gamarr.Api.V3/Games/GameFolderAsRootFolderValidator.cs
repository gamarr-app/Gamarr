using System;
using System.IO;
using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;

namespace Gamarr.Api.V3.Games
{
    public class GameFolderAsRootFolderValidator : PropertyValidator<object, string>
    {
        private readonly IBuildFileNames _fileNameBuilder;

        public GameFolderAsRootFolderValidator(IBuildFileNames fileNameBuilder)
        {
            _fileNameBuilder = fileNameBuilder;
        }

        public override string Name => "GameFolderAsRootFolderValidator";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            if (context.InstanceToValidate is not GameResource gameResource)
            {
                return true;
            }

            var rootFolderPath = value;

            if (rootFolderPath.IsNullOrWhiteSpace())
            {
                return true;
            }

            var rootFolder = new DirectoryInfo(rootFolderPath!).Name;
            var game = gameResource.ToModel();
            var gameFolder = _fileNameBuilder.GetGameFolder(game);

            context.MessageFormatter.AppendArgument("rootFolderPath", rootFolderPath);
            context.MessageFormatter.AppendArgument("gameFolder", gameFolder);

            if (gameFolder == rootFolder)
            {
                return false;
            }

            var distance = gameFolder.LevenshteinDistance(rootFolder);

            return distance >= Math.Max(1, gameFolder.Length * 0.2);
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Root folder path '{rootFolderPath}' contains game folder '{gameFolder}'";
    }
}
