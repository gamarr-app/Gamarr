using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download.Clients.RTorrent;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.rTorrent
{
    public interface IRTorrentDirectoryValidator
    {
        ValidationResult Validate(RTorrentSettings instance);
    }

    public class RTorrentDirectoryValidator : AbstractValidator<RTorrentSettings>, IRTorrentDirectoryValidator
    {
        public RTorrentDirectoryValidator(RootFolderValidator rootFolderValidator,
                                          PathExistsValidator pathExistsValidator,
                                          MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            RuleFor(c => c.GameDirectory).Cascade(CascadeMode.Stop)
                                       .IsValidPath()
                                       .Must(value => rootFolderValidator.Validate(value))
                                       .WithMessage("Path is already configured as a root folder")
                                       .Must(value => mappedNetworkDriveValidator.Validate(value))
                                       .WithMessage("Mapped Network Drive and Windows Service")
                                       .Must(value => pathExistsValidator.Validate(value))
                                       .WithMessage("Path does not exist")
                                       .When(c => c.GameDirectory.IsNotNullOrWhiteSpace())
                                       .When(c => c.Host == "localhost" || c.Host == "127.0.0.1");
        }
    }
}
