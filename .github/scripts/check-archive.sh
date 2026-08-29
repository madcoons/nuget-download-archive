#!/usr/bin/env bash
# Fails unless every RID given after the directory has its geckodriver in that directory.
set -euo pipefail

dir=$1
shift

status=0
for rid in "$@"; do
    exe=geckodriver
    case $rid in
        win-*) exe=geckodriver.exe ;;
    esac

    if [ -f "$dir/$rid/$exe" ]; then
        echo "ok      $dir/$rid/$exe"
    else
        echo "::error::missing $dir/$rid/$exe"
        status=1
    fi
done

if [ $status -ne 0 ]; then
    echo "=== contents of $dir ==="
    ls -R "$dir" || true
fi

exit $status
