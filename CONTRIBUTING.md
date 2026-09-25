# How to Contribute

We're always looking for people to help make Gamarr better, and there are
several ways to do it.

# Documentation

Setup guides, the [FAQ](/gamarr/faq) — the more information we have on the
[wiki](https://github.com/gamarr-app/Gamarr/wiki), the better.

# Development

Gamarr's backend is C# on the .NET 10 framework; the frontend is JavaScript with
React.

## Tools required

- Visual Studio 2022 or higher is recommended (<https://www.visualstudio.com/vs/>). The community version is free and works (<https://www.visualstudio.com/downloads/>).

> VS 2022 V17.14 or higher is recommended as it includes the .NET 10 SDK

- An HTML/JavaScript editor of your choice (VS Code, Sublime Text, WebStorm, Atom, and so on)
- [Git](https://git-scm.com/downloads)
- The [Node.js](https://nodejs.org/) runtime. The supported version is:
  - **20** (any minor or patch version within this)

> The application will **NOT** run on older versions such as `18.x`, `16.x` or any version below 20.0. Because of a dependency issue, it will also not run on `21.x`, and it is untested on other versions.

- [Yarn](https://yarnpkg.com/getting-started/install) is required to build the frontend
  - Yarn ships with **Node 22**+ by default. Enable it with `corepack enable`
  - On other Node versions, install it with `npm i -g corepack`

## Getting started

1. Fork Gamarr
1. Clone your fork onto your development machine ([how forking works](https://docs.github.com/en/get-started/quickstart/fork-a-repo))

> Before committing any front-end change, run lint with `yarn lint --fix`. For CSS changes, run `yarn stylelint-windows --fix`.

### Building the frontend

1. Navigate to the cloned directory
1. Install the required Node packages

    ```bash
    yarn install
    ```

1. Start webpack, which watches your development environment for changes that need post-processing

    ```bash
    yarn start
    ```

### Building the Backend

Visual Studio or Rider is the easiest way to build and run the backend solution.
If your only priority is working on the frontend UI, the command line builds it
just as well, provided the correct SDK is installed.

#### Visual Studio

> Ensure startup project is set to `Gamarr.Console` and framework to `net10.0`

1. `Build` the solution in Visual Studio. This makes sure all projects are built and dependencies restored
1. `Debug/Run` the project to start Gamarr
1. Open <http://localhost:6767>

#### Command line

1. Clean the solution

```shell
dotnet clean src/Gamarr.sln -c Debug
```

1. Restore and build the Debug configuration for your platform (Posix or Windows)

```shell
dotnet msbuild -restore src/Gamarr.sln -p:Configuration=Debug -p:Platform=Posix -t:PublishAllRids
```

1. Run the produced executable from `/_output`

## Contributing Code

- If you're adding a feature that has already been requested, comment on [GitHub Issues](https://github.com/gamarr-app/Gamarr/issues) so the work isn't duplicated. If you want to add something that isn't on there, talk to us first
- Rebase onto `main`, do not merge — `main` rejects merge commits
- Make meaningful commits, or squash them
- Open a pull request before the work is complete if you like — it lets us see where it's at and suggest improvements
- Reach out to us on Discord with any questions
- Add tests (unit/integration)
- Commit with \*nix line endings for consistency (we check out Windows and commit \*nix)
- One feature or bug fix per pull request, to keep things easy to review
- Use 4 spaces instead of tabs — the default in VS 2022 and WebStorm

## Pull Requesting

- `main` is the only long-lived branch. Make pull requests against it
- Expect comments or questions from us. They're there to keep the codebase consistent and maintainable
- We try to respond to pull requests as soon as possible. If it's been a day or two, reach out — we may have missed it
- Each PR should come from its own [feature branch](http://martinfowler.com/bliki/FeatureBranch.html), not from `main` in your fork, and the branch name should say what is being added or fixed
  - `new-feature` (Good)
  - `fix-bug` (Good)
  - `patch` (Bad)
  - `main` (Bad)
- Write commits as `New:` or `Fixed:` for changes that would not be considered a `maintenance release`

## Unit Testing

Gamarr uses NUnit for its unit, integration, and automation test suite.

### Running Tests

Run tests from within VS using the included nunit3testadapter nuget package, or
from the command line using the included bash script `test.sh`.

In VS, go to Test Explorer and run or debug the tests you want to examine, one
at a time or all at once.

From the command line, `test.sh` accepts 3 parameters

```bash
test.sh <PLATFORM> <TYPE> <COVERAGE>
```

### Writing Tests

While not always fun, we encourage writing unit tests for any backend code
change. A test confirms the change does what you intended, and stops a future
change from breaking that behaviour.

> We currently require 80% coverage on new code when submitting a PR

If you have any questions about any of this, let us know.

# Translation

Gamarr uses a self hosted open access [Weblate](https://translate.servarr.com) instance to manage its json translation files. These files are stored in the repo at `src/NzbDrone.Core/Localization`

## Contributing to an Existing Translation

Weblate handles synchronization and translation of strings for every language other than English. Edit translated strings and translate existing strings for supported languages there, on the Gamarr project.

The English translation, `en.json`, is the source for all other translations and is managed in the GitHub repo.

## Adding a Language

Adding a translation to Gamarr takes two steps

- Add the language to Weblate
- Add the language to the Gamarr codebase

## Adding Translation Strings in Code

The English translation, `src/NzbDrone.Core/Localization/Core/en.json`, is the source for all other translations and is managed in the GitHub repo. When you add a new string to the UI or the backend, add a key to `en.json` along with its default English value. Consume the key as follows.

> PRs for translation of log messages will not be accepted

### Backend Strings

Add backend strings with the Localization Service `GetLocalizedString` method

```dotnet
private readonly ILocalizationService _localizationService;

public IndexerCheck(ILocalizationService localizationService)
{
  _localizationService = localizationService;
}
        
var translated = _localizationService.GetLocalizedString("IndexerHealthCheckNoIndexers")
```

### Frontend Strings

Add frontend strings by importing the translate function and using a key from `en.json`

```js
import translate from 'Utilities/String/translate';

<div>
  {translate('UnableToAddANewIndexerPleaseTryAgain')}
</div>
```
