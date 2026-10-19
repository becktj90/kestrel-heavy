#!/usr/bin/env python3
"""Semantic C# compile check without Unity: builds stub UnityEngine/UnityEditor/NUnit assemblies from
stubs/*.cs, then compiles each Unity asmdef (and its asmdef dependencies) with `mcs -target:library`.

Catches what `mcs --parse` cannot: undeclared names (CS0103), wrong types/members on our own code, missing
using/asmdef references. Stubs model only the Unity APIs this repo uses; a CS0117/CS1061/CS0246 that names a
UnityEngine/UnityEditor/NUnit member means the STUB is missing it (add it to stubs/), not that the code is wrong.

Usage:
  tools/cs-stubcheck/stubcheck.py                 # asmdefs containing .cs files changed vs origin/main (+ deps)
  tools/cs-stubcheck/stubcheck.py --base <rev>    # changed vs <rev>
  tools/cs-stubcheck/stubcheck.py --all           # every asmdef under the default roots
  tools/cs-stubcheck/stubcheck.py --tree <rev>    # check a committed tree (git worktree export), e.g. 3eb89f7
Exit 0 = all compiled; 1 = errors (printed). Compiler: Roslyn csc from a .NET SDK (C# 9 like Unity 6, against
netstandard2.1 reference assemblies). Looks for dotnet in $DOTNET_ROOT, ~/.dotnet, then PATH; install with
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
(mono mcs is NOT used: it cannot parse C# 8/9 switch expressions, local functions etc. used in this repo).
"""
import argparse, glob, json, os, re, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOTS = ["Kestrel.Game/Pad", "Kestrel.Game/Kit", "Kestrel.Game/Environment", "Kestrel.Slice/Runtime",
         "Kestrel.Game.Tests", "Kestrel.Game/Vehicle", "Kestrel.Game/Shell", "Kestrel.Game/TextFit",
         "Kestrel.Game/Quality", "Kestrel.Game/Layout", "Kestrel.Core"]
SKIP = {"Kestrel.Slice.GameCenter", "Kestrel.Core.Burst"}   # third-party / Burst-only
STUB_ASM = {"UnityEngine.TestRunner": None, "UnityEditor.TestRunner": None,
            "Unity.RenderPipelines.Universal.Runtime": None, "Unity.RenderPipelines.Core.Runtime": None}

def toolchain():
    roots = [os.environ.get("DOTNET_ROOT", ""), os.path.expanduser("~/.dotnet")] + [os.path.dirname(os.path.realpath(p)) for p in [shutil.which("dotnet") or ""] if p]
    for r in roots:
        dn = os.path.join(r, "dotnet") if r else ""
        if not dn or not os.path.exists(dn): continue
        cscs = sorted(glob.glob(os.path.join(r, "sdk", "*", "Roslyn", "bincore", "csc.dll")))
        ns = sorted(glob.glob(os.path.join(r, "packs", "NETStandard.Library.Ref", "*", "ref", "netstandard2.1")))
        if cscs and ns:
            refs = sorted(glob.glob(os.path.join(ns[-1], "*.dll")))
            return [dn, "exec", cscs[-1]], refs
    sys.exit("no .NET SDK with Roslyn + NETStandard.Library.Ref found; see the header for the install command")

CSC, NSREFS = None, []
def csc(out, files, refs, defines=(), nowarn="0067,0105,0108,0114,0162,0168,0169,0219,0414,0618,0649,0659,0661,1998,8632"):
    cmd = CSC + ["-nologo", "-noconfig", "-nostdlib+", "-langversion:9", "-target:library", "-nullable:disable", "-nowarn:" + nowarn,
                 "-out:" + out] + ["-r:" + r for r in NSREFS + list(refs)] + (["-define:" + ";".join(defines)] if defines else []) + list(files)
    return sh(cmd)

def sh(cmd, cwd=None):
    p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True); return p.returncode, p.stdout + p.stderr

def asmdefs(assets):
    out = {}
    for f in glob.glob(os.path.join(assets, "**", "*.asmdef"), recursive=True):
        j = json.load(open(f, encoding="utf-8-sig")); d = os.path.dirname(f)
        out[j["name"]] = dict(dir=d, refs=j.get("references", []), engine=not j.get("noEngineReferences", False),
                              defines=j.get("defineConstraints", []), editor="Editor" in j.get("includePlatforms", []))
    # An .asmref folds its folder into the named asmdef (e.g. Kestrel.Game/Failure -> Kestrel.Slice).
    refdirs = {}
    for f in glob.glob(os.path.join(assets, "**", "*.asmref"), recursive=True):
        j = json.load(open(f, encoding="utf-8-sig"))
        if j.get("reference") in out: refdirs.setdefault(j["reference"], []).append(os.path.dirname(f))
    roots = [o["dir"] for o in out.values()] + [d for ds in refdirs.values() for d in ds]
    for n, a in out.items():   # files belong to the nearest asmdef or asmref
        files = []
        for root in [a["dir"]] + refdirs.get(n, []):
            nested = [x for x in roots if x != root and x.startswith(root + os.sep)]
            files += [f for f in glob.glob(os.path.join(root, "**", "*.cs"), recursive=True)
                      if not any(f.startswith(x + os.sep) for x in nested)]
        a["files"] = sorted(files)
    return out

def build_stubs(work):
    srcs = sorted(glob.glob(os.path.join(HERE, "stubs", "*.cs")))
    eng = [s for s in srcs if not os.path.basename(s).startswith(("Editor", "NUnit"))]
    code, out = csc(os.path.join(work, "UnityEngine.dll"), eng, [])
    if code: print(out); sys.exit("stub UnityEngine build failed")
    for name, pre in (("UnityEditor", "Editor"), ("nunit.framework", "NUnit")):
        s = [x for x in srcs if os.path.basename(x).startswith(pre)]
        code, out = csc(os.path.join(work, name + ".dll"), s, [os.path.join(work, "UnityEngine.dll")])
        if code: print(out); sys.exit(f"stub {name} build failed")

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--base", default="origin/main"); ap.add_argument("--all", action="store_true")
    ap.add_argument("--tree"); ap.add_argument("--repo", default=os.path.abspath(os.path.join(HERE, "..", "..")))
    a = ap.parse_args()
    global CSC, NSREFS; CSC, NSREFS = toolchain()
    work = tempfile.mkdtemp(prefix="stubcheck-"); repo = a.repo
    if a.tree:   # export a committed tree
        src = os.path.join(work, "tree"); os.makedirs(src)
        sh(["bash", "-c", f"git -C '{repo}' archive {a.tree} unity/Assets | tar -x -C '{src}'"]); repo = src
    assets = os.path.join(repo, "unity", "Assets"); A = asmdefs(assets)
    in_roots = lambda n: any(A[n]["dir"].startswith(os.path.join(assets, r)) for r in ROOTS)
    if a.all or a.tree:
        targets = [n for n in A if in_roots(n) and n not in SKIP]
    else:
        code, out = sh(["git", "-C", a.repo, "diff", "--name-only", a.base, "--", "unity/Assets"])
        changed = [os.path.join(a.repo, l) for l in out.split() if l.endswith(".cs")]
        changed += [os.path.join(a.repo, l) for l in sh(["git", "-C", a.repo, "ls-files", "--others", "--exclude-standard", "unity/Assets"])[1].split() if l.endswith(".cs")]
        targets = sorted({n for n in A for f in changed if os.path.abspath(f) in map(os.path.abspath, A[n]["files"])} - SKIP)
    build_stubs(work); built = {}; failed = False
    builtp = {}
    def buildp(n):
        """Release player compile: dependencies are the PLAYER dlls too, so a type that only exists under
        UNITY_EDITOR / DEVELOPMENT_BUILD in one assembly is missing for its dependents, as on a device."""
        nonlocal failed
        if n in builtp: return builtp[n]
        if n not in A: return None
        files = A[n]["files"]
        if not files: builtp[n] = None; return None
        refs = [r for r in (buildp(x) for x in A[n]["refs"]) if r]
        base = ["UNITY_5_3_OR_NEWER", "UNITY_2021_1_OR_NEWER", "UNITY_6000_0_OR_NEWER"]
        code, msg = csc(os.path.join(work, n + ".player.dll"), files, refs + ([os.path.join(work, "UnityEngine.dll")] if A[n]["engine"] else []), base)
        if code:
            print(f"[FAIL] {n} (player build, no UNITY_EDITOR / DEVELOPMENT_BUILD)"); failed = True
            for l in [l for l in msg.splitlines() if ": error " in l]: print("   ", l.replace(assets + os.sep, ""))
            builtp[n] = None; return None
        builtp[n] = os.path.join(work, n + ".player.dll"); return builtp[n]
    def build(n):
        nonlocal failed
        if n in built: return built[n]
        if n not in A: return None   # Unity/package assembly: provided by the stubs
        refs = [r for r in (build(x) for x in A[n]["refs"]) if r]
        files = A[n]["files"]
        if not files: built[n] = None; return None
        base = ["UNITY_5_3_OR_NEWER", "UNITY_2021_1_OR_NEWER", "UNITY_6000_0_OR_NEWER"]
        is_test = any(d == "UNITY_INCLUDE_TESTS" for d in A[n]["defines"]) or n.endswith("Tests") or "Editor" in n
        # Editor compile (what the Unity Editor and EditMode tests see): every assembly gets UNITY_EDITOR.
        defs = ["UNITY_EDITOR"] + (["UNITY_INCLUDE_TESTS"] if is_test else []) + base
        if not is_test: buildp(n)   # player compile too (iOS/web release build): catches unguarded UnityEditor and dev-only (DEVELOPMENT_BUILD) use
        lib = [os.path.join(work, "UnityEngine.dll")] if A[n]["engine"] else []
        lib += [os.path.join(work, "UnityEditor.dll"), os.path.join(work, "nunit.framework.dll")]
        out = os.path.join(work, n + ".dll")
        code, msg = csc(out, files, refs + lib, defs)
        errs = [l for l in msg.splitlines() if ": error " in l or l.startswith("error ")]
        rel = lambda s: s.replace(assets + os.sep, "")
        print(f"[{'ok' if code == 0 else 'FAIL'}] {n} ({len(files)} files)")
        for l in errs: print("   ", rel(l))
        if code: failed = True; built[n] = None; return None
        built[n] = out; return out
    for t in targets: build(t)
    if not targets: print("no changed asmdefs")
    shutil.rmtree(work, ignore_errors=True)
    sys.exit(1 if failed else 0)

if __name__ == "__main__": main()
