# Pokemon Emerald on Android

Build harness for the Android target of
[gradenGnostic/pokeemerald-multiplatform](https://github.com/gradenGnostic/pokeemerald-multiplatform),
a fork of the `pret/pokeemerald` decompilation with an SDL2 port.

## Why this fork

The game's decompiled C is compiled and run **directly** on the device. There is
no bundled emulator and no commercial ROM involved, which is both faster to get
working and far cleaner legally than shipping a built `.gba` inside an APK.

The fork already provides, and this harness only drives:

- a Gradle + CMake Android project under `android/`
- an `SDLActivity` subclass with a labeled multitouch overlay (D-pad, A, B,
  Start, Select, L, R) and SDL gamepad support
- writable save storage in the app's private directory, plus lifecycle handling
- launcher icons and a display-settings page

## Constraints worth knowing before you build

- **`armeabi-v7a` only.** The game data assumes 32-bit pointers. That means
  sideloading only: Google Play has required a 64-bit ABI since 2019.
- The build is a hybrid. `data/*.s` and the song assembly are still assembled
  with `arm-none-eabi-as` and linked into `libmain.so`, so ARM binutils is a
  build dependency even though the target is Android.
- The fork marks its Android support experimental, and **its own CI builds only
  the GBA ROM** — the Android target is not verified upstream. That is what the
  workflow here exists to cover.

## Building

```sh
export JAVA_HOME=...          # JDK 17
export ANDROID_HOME=...       # with ndk;26.3.11579264 and cmake;3.22.1
sudo apt-get install -y build-essential binutils-arm-none-eabi libpng-dev

./android-build/build-android.sh
```

The APK lands at
`.pokeemerald-multiplatform/android/app/build/outputs/apk/debug/app-debug.apk`.

`.github/workflows/pokeemerald-android.yml` runs the same script on CI and
uploads the APK as a build artifact.

## Fixes this harness applies on top of the fork

- **The documented patch command does not work.** The fork's README says to run
  `git -C android/SDL2 apply ../patches/sdl2-android-lifecycle.patch`, but that
  patch is generated with zero context lines, which `git apply` rejects
  outright. `--unidiff-zero` is required. The script also reverse-checks first
  so re-running it on an existing checkout is a no-op rather than a failure.
- The script builds the decomp's host tools (`make tools`) before invoking
  Gradle. The CMake build shells out to `tools/preproc/preproc` for every
  translation unit and does not build it itself.

## Legal

Pokemon and Pokemon Emerald are trademarks of Nintendo, Creatures Inc. and
GAME FREAK inc. This is an unofficial fan project, not affiliated with or
endorsed by them. No game ROM or copyrighted asset is distributed here; the
build produces an application from decompiled source, and you are responsible
for how you use it.
