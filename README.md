# Entity Framework 6

Entity Framework 6 (EF6) is an object-relational mapper that enables .NET developers to work with relational data using domain-specific objects. It eliminates the need for most of the data-access code that developers usually need to write.

Entity Framework 6 is developed by the Entity Framework team in collaboration with a community of open source developers.

## Status and Support

The latest version of EF6Tools is still supported by Microsoft, however the EF6Tools are no longer being actively developed. This means that:

- Security fixes will continue to be provided, consistent with Microsoft’s support lifecycle.
- High‑impact bugs: may be addressed at Microsoft’s discretion, typically if they affect a large portion of the user base.
- Other bugs will _not_ be fixed.
- New features will _not_ be implemented

 This is consistent with the [EF6 support policy](https://github.com/dotnet/ef6?tab=readme-ov-file#status-and-support). 

## EF6 here, EF Core elsewhere

This repository is for the Entity Framework 6 Visual Studio tools. Entity Framework Core is a lightweight and extensible version of Entity Framework and is maintained at https://github.com/dotnet/efcore.

## EF6 PowerTools development has moved

Further development of the EF6 PowerTools is happening in a community-driven project, the [EF6 PowerTools Community Edition](https://github.com/ErikEJ/EntityFramework6PowerTools).

## How do I use EF

If you want to use an officially supported Entity Framework release to develop your applications then head to https://docs.microsoft.com/ef/ef6/ where you can find installation information, documentation, tutorials, samples, and videos.

If you want to try out the latest changes that have not been officially released yet, you can choose to [build the code](https://github.com/aspnet/EntityFramework6/wiki/Building-the-Runtime). We regularily also make [nightly builds](https://github.com/aspnet/EntityFramework6/wiki/Nightly-Builds) of the Entity Framework codebase available.

## How do I contribute

There are lots of ways to [contribute to the Entity Framework project](https://github.com/aspnet/EntityFramework6/wiki/Contributing) including testing out nighty builds, reporting bugs, and contributing code.

All code submissions will be rigorously reviewed and tested by the Entity Framework team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.
