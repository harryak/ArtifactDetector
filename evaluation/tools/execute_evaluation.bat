@echo off
  for %%a in (C:\Users\Felix\source\repos\recipes\*.yml) do (
    for %%s in (C:\Users\Felix\source\repos\Artifact-Detector\evaluation\evaluation_screenshots\*.png) do (
      for /l %%i in (1,1,1) do (
        echo %%i %%~na %%~ns
        "C:\Users\Felix\source\repos\Artifact-Detector\Visual-Artifact-Detector\bin\x86\Release\Visual-Artifact-Detector.exe" -s C:\Users\Felix\source\repos\Artifact-Detector\evaluation\evaluation_screenshots\%%~ns.png -a %%~na -f C:\Users\Felix\source\repos\recipes -e
      )
    )
  )
)