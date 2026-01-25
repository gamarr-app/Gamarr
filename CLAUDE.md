# Claude Code Notes

## .NET SDK

The .NET 8 SDK is installed at `~/.dotnet`. Add to PATH if needed:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

## Build Commands

```bash
# Backend
dotnet build src/Gamarr.sln

# Frontend
yarn install
yarn build

# Run tests
dotnet test src/Gamarr.sln --filter "Category!=AutomationTest"
```

## Git Workflow

Push directly to main:

```bash
git push origin gamarr3-work:main
```

## Project Structure

- `src/` - .NET backend
- `frontend/` - React frontend
- `_output/` - Build output
- `_tests/` - Test output
