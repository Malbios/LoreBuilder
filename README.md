# LoreBuilder

## Setup

This repo uses a git submodule for `external/blazor-dragdrop`. After cloning:

```
git submodule update --init --recursive
```

(or clone with `git clone --recurse-submodules`)

## Build & run

```
dotnet build LoreBuilder.sln
dotnet run --project src/LoreBuilder/LoreBuilder.fsproj
```

See `CLAUDE.md` for the full command reference and an architecture overview.
