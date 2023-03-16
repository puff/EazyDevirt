<div align="center">

<img src="assets/Logo.png">

# EazyDevirt - Eazfuscator.NET

[EazyDevirt] is an open-source toolkit that allows you to automatically restore the original IL code from an assembly virtualized with [Eazfuscator.NET].

[Installation](#installation) •
[Usage](#usage) •
[Roadmap](#roadmap) •
[License](#license) •
[Contributing](#contributing) •

[![forthebadge](https://forthebadge.com/images/badges/powered-by-black-magic.svg)](https://forthebadge.com)

</div>

## Usage
Running the following command:
```
EazyDevirt <assembly> [<output>] [options]
```

```
Arguments:
  <assembly>  Path to target assembly
  <output>    Path to output directory [default: ./eazydevirt-output]

Options:
  -v, --verbose <verbosity>  Level of verbosity [1: Verbose, 2: Very Verbose, 3: Very Very Verbose] [default: 0]
  --preserve-all             Preserves all metadata tokens [default: False]
  -kt, --keep-types          Keeps obfuscator types [default: False]
  --save-anyway              Saves output of devirtualizer even if it fails [default: False]
  --only-save-devirted       Only saves successfully devirtualized methods (This option only matters if you use the save anyway option) [default: False]
  --version                  Show version information
  -?, -h, --help             Show help and usage information
```

##### Example:
```
EazyDevirt.exe `test.exe -v 3 -kt --preserve-all --save-anyway true`
```

## Installation
To clone the project use:

```
$ git clone --recurse-submodules https://github.com/puff/EazyDevirt.git
```

Then you can use your favourite IDE such as Visual Studio and JetBrains Rider or build from the commandline:

```
$ dotnet restore
$ dotnet build
```

## Roadmap
See the [open issues](https://github.com/puff/EazyDevirt/issues) for a list of proposed features (and known issues).

## Contributing
First off, thanks for taking the time to contribute! Contributions are what makes the open-source community such an amazing place to learn, inspire, and create. Any contributions you make will benefit everybody else and are greatly appreciated.

See [CONTRIBUTING.md] for guidelines on general workflow and code style.

## Credits
- clifford for helping with [Eazfuscator.NET]'s VM.
- [saneki] for the well-documented [eazdevirt] project.
- [TobitoFatitoRE] for the [HexDevirt] project.
- [Washi1337] for the wonderful [AsmResolver] library.

## License
[![GPLv3 License](https://img.shields.io/badge/License-GPL%20v3-yellow.svg)](https://opensource.org/licenses/)

[CONTRIBUTING.md]:https://github.com/puff/EazyDevirt
[EazyDevirt]:https://github.com/puff/EazyDevirt
[eazdevirt]:https://github.com/saneki/eazdevirt
[HexDevirt]:https://github.com/TobitoFatitoRE/HexDevirt
[TobitoFatitoRE]:https://github.com/TobitoFatitoRE
[saneki]:https://github.com/saneki
[Washi1337]:https://github.com/Washi1337
[AsmResolver]:https://github.com/Washi1337/AsmResolver
[Eazfuscator.NET]:https://www.gapotchenko.com/eazfuscator.net
