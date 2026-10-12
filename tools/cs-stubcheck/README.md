# cs-stubcheck

This is a compile check that runs without Unity. It compiles the repo's C# against hand-written stubs for the parts of UnityEngine, UIElements, URP and NUnit that we use, with Roslyn `csc` from the .NET 8 SDK (`~/.dotnet`), `-langversion:9` and netstandard2.1 references. Assemblies are split by the repo's `.asmdef` files. Each assembly is compiled the way the Editor sees it (`UNITY_EDITOR`, plus `UNITY_INCLUDE_TESTS` for test assemblies). Runtime assemblies are also compiled a second time as a player build, without `UNITY_EDITOR`, which catches unguarded `UnityEditor` use.

```
tools/cs-stubcheck/stubcheck.py              # asmdefs touched by changes vs origin/main, plus untracked files (run before every push)
tools/cs-stubcheck/stubcheck.py --base REV   # changes vs another base
tools/cs-stubcheck/stubcheck.py --all        # every asmdef
tools/cs-stubcheck/stubcheck.py --tree REV   # every asmdef at a commit (git archive)
```

The script exits non-zero on any `[FAIL]`. `--tree 3eb89f7` reproduces the CS0103 in PlumeDebugProbe.cs that slipped through in #130.

Limits: it checks only syntax, names, types and overloads. It does not check Unity serialization, shader or asset references, or runtime behaviour, and it does not run the tests. Missing stubs show up as CS0246, CS0117 or CS1061 on a Unity type. Add the member to `stubs/` and don't change game code. Skipped: Kestrel.Slice.GameCenter (native plugin) and Kestrel.Core.Burst.
