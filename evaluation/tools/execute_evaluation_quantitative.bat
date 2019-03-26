@echo off
for %%a in (C:\Users\Lab19\Desktop\artifacts_quantitative\*.yml) do (
  for %%s in (C:\Users\Lab19\Desktop\evaluation_quantitative\*.png) do (
    for /l %%i in (1,1,1000) do (
      echo %%i %%~na %%~ns
      "C:\Program Files\ITS.APE\Visual Artifact Detector\Visual-Artifact-Detector.exe" -s C:\Users\Lab19\Desktop\evaluation_quantitative\%%~ns.png -a %%~na -f C:\Users\Lab19\Desktop\artifacts_quantitative -e
    )
  )
)