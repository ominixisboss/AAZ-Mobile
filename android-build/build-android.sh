#!/usr/bin/env bash
# Builds the Pokemon Emerald Android APK from the pokeemerald-multiplatform fork.
#
# This is the native SDL2 port -- the decompiled game code is compiled directly
# for Android. No emulator is bundled and no commercial ROM is used or produced.
#
# Requires: JAVA_HOME, ANDROID_HOME (with NDK + cmake installed, see REQUIRED_*
# below), and an arm-none-eabi binutils on PATH for the game's data/song
# assembly, which is still assembled as ARM and linked into libmain.so.
set -euo pipefail

REPO_URL="${REPO_URL:-https://github.com/gradenGnostic/pokeemerald-multiplatform.git}"
REPO_REF="${REPO_REF:-master}"
WORK_DIR="${WORK_DIR:-$PWD/.pokeemerald-multiplatform}"

REQUIRED_NDK="26.3.11579264"
REQUIRED_CMAKE="3.22.1"

log() { printf '\n==> %s\n' "$*"; }

: "${ANDROID_HOME:?ANDROID_HOME must point at an Android SDK}"
command -v arm-none-eabi-as >/dev/null || {
    echo "arm-none-eabi-as not found; install binutils-arm-none-eabi" >&2
    exit 1
}

if [ ! -d "$WORK_DIR/.git" ]; then
    log "Cloning $REPO_URL @ $REPO_REF"
    git clone --branch "$REPO_REF" "$REPO_URL" "$WORK_DIR"
fi
cd "$WORK_DIR"

log "Fetching the SDL2 submodule"
git submodule update --init --recursive android/SDL2

# The fork ships a lifecycle patch that strips SDL's Android activity-mutex
# locking out of SDL_CreateRenderer and Android_CreateWindow. It is generated
# with zero context lines, so plain `git apply` -- which is what the fork's
# README tells you to run -- always rejects it. --unidiff-zero is required.
# Applying it twice would also fail, hence the reverse check.
log "Applying the SDL2 Android lifecycle patch"
if git -C android/SDL2 apply -R --check ../patches/sdl2-android-lifecycle.patch 2>/dev/null; then
    echo "already applied, skipping"
else
    git -C android/SDL2 apply --unidiff-zero ../patches/sdl2-android-lifecycle.patch
fi

# The CMake build shells out to tools/preproc/preproc for every .c and .s file.
log "Building the decomp's host tools"
make -j"$(nproc)" tools

log "Assembling the APK"
./android/SDL2/android-project/gradlew -p android :app:assembleDebug

APK="android/app/build/outputs/apk/debug/app-debug.apk"
log "Done: $WORK_DIR/$APK"
ls -la "$APK"
