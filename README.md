<div align="center">

<img src="assets/Logo.png">

# EazyDevirt

[EazyDevirt] is an open-source toolkit that allows you to automatically restore the original IL code from an assembly virtualized with [Eazfuscator.NET].

[Installation](#installation) •
[Usage](#usage) •
[Roadmap](#roadmap)

[![forthebadge](https://forthebadge.com/images/badges/powered-by-black-magic.svg)](https://forthebadge.com)

[![GPLv3 License](https://img.shields.io/badge/License-GPL%20v3-orangered.svg)](https://opensource.org/licenses/)

</div>

## Usage
```
EazyDevirt <assembly> [<output>] [options]
```

```console
Arguments:
  <assembly>  Path to target assembly
  <output>    Path to output directory [default: ./eazydevirt-output]

Options:
  -v, --verbose <verbosity>  Level of verbosity [1: Verbose, 2: Very Verbose, 3: Very Very Verbose] [default: 0]
  --preserve-all             Preserves all metadata tokens [default: False]
  -kt, --keep-types          Keeps obfuscator types [default: False]
  --save-anyway              Saves output of devirtualizer even if it fails [default: False]
  --only-save-devirted       Only saves successfully devirtualized methods (This option only matters if you use the
                             save anyway option) [default: False]
  --version                  Show version information
  -?, -h, --help             Show help and usage information
```

#### Example:
```console
$ EazyDevirt.exe test.exe -v 3 -kt --preserve-all --save-anyway true
```

## Installation
To clone the project use:

```console
$ git clone --recurse-submodules https://github.com/puff/EazyDevirt.git
```

Then you can use your favourite IDE or build from the command line:

```console
$ dotnet restore
$ dotnet build
```

## Roadmap
See the [open issues](https://github.com/puff/EazyDevirt/issues) for a list of proposed features (and known issues).

### Credits
- [void-stack] for the many contributions.
- [saneki] for the [eazdevirt] project.
- [TobitoFatitoRE] for the [HexDevirt] project.
- [Washi1337] for the [AsmResolver] library.

And a thank you, to [all other contributors](https://github.com/puff/EazyDevirt/graphs/contributors). 

[EazyDevirt]:https://github.com/puff/EazyDevirt
[eazdevirt]:https://github.com/saneki/eazdevirt
[HexDevirt]:https://github.com/TobitoFatitoRE/HexDevirt
[TobitoFatitoRE]:https://github.com/TobitoFatitoRE
[void-stack]:https://github.com/void-stack
[saneki]:https://github.com/saneki
[Washi1337]:https://github.com/Washi1337
[AsmResolver]:https://github.com/Washi1337/AsmResolver
[Eazfuscator.NET]:https://www.gapotchenko.com/eazfuscator.net
