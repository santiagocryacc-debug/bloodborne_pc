# Cinematic launcher artwork

The WPF launcher loads these optional local JPEG images at runtime:

- `Assets/Yharnam.jpg` — central hunter and Yharnam artwork
- `Assets/Welcome.jpg` — upper news card
- `Assets/Nightmares.jpg` — lower news card

The launcher and GitHub Actions compile even when these files are absent. When present beside `Launcher.exe`, the images display automatically. Extract the companion **Bloodborne_Cinematic_Assets.zip** into the folder containing `Launcher.exe`, preserving the `Assets` subdirectory.

Images are cropped from the original concept generated for this launcher. The controls, page tabs, game directory and saves directory remain live WPF components. Do not use the concept image as a flat click-through interface.

To bundle artwork automatically with GitHub Actions, commit these JPG files to this directory, then add a publish step to copy `Assets` to the output directory. No copyrighted fonts are included.
