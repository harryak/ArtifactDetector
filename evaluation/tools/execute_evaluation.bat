@echo off
for %%a in (\\VBOXSVR\recipes_selected\*.yml) do (
  for %%s in (\\VBOXSVR\evaluation_screenshots\*.png) do (
    for /l %%i in (1,1,1) do (
      echo %%i %%~na %%~ns
      "C:\Program Files\ITS.APE\Visual Artifact Detector\Visual-Artifact-Detector.exe" -c -s \\VBOXSVR\evaluation_screenshots\%%~ns.png -a %%~na -f C:\Users\Lab19\Desktop -e
    )
  )
)