@echo off
for %%a in (\\VBOXSVR\recipes\*.yml) do (
  for %%s in (\\VBOXSVR\build_cache\*.png) do (
    for /l %%i in (1,1,1) do (
      echo %%i %%~na %%~ns
      "C:\Program Files\ITS.APE\Visual Artifact Detector\Visual-Artifact-Detector.exe" -c -s \\VBOXSVR\build_cache\%%~ns.png -a %%~na -f \\VBOXSVR\recipes -e
    )
  )
)