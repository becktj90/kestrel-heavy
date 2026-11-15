#!/bin/sh
# Xcode Cloud: make sure Git LFS objects (Libraries/*.a) are present after the clone.
set -e
if ! command -v git-lfs >/dev/null 2>&1; then brew install git-lfs; fi
git lfs install
git lfs pull
