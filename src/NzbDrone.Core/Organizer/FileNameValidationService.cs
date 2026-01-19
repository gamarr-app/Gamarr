using FluentValidation.Results;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameValidationService
    {
        ValidationFailure ValidateGameFilename(SampleResult sampleResult);
    }

    public class FileNameValidationService : IFilenameValidationService
    {
        private const string ERROR_MESSAGE = "Produces invalid file names";

        public ValidationFailure ValidateGameFilename(SampleResult sampleResult)
        {
            var validationFailure = new ValidationFailure("StandardGameFormat", ERROR_MESSAGE);
            var parsedGameInfo = Parser.Parser.ParseGameTitle(sampleResult.FileName);

            if (parsedGameInfo == null)
            {
                return validationFailure;
            }

            return null;
        }
    }
}
