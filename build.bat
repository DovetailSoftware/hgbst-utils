@echo off

echo building project...


SET MSBUILD=C:\windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe

REM ^^^ if your msbuild is in a different location edit this line ^^^^

MSBUILD 

echo copying results to bin directory

IF NOT EXIST output mkdir output

copy .\FChoice.DevTools.HgbstUtils.Text.UI\bin\Debug\*.dll .\output
copy .\FChoice.DevTools.HgbstUtils.Text.UI\bin\Debug\*.exe .\output

cd output
zip hgbst-util *.dll *.exe

REM ^^ assumes you have zip in your path.

del *.dll 
del *.exe

cd ..

echo ***************************************************************
echo All done. Look in the output directory for hgbst-util.zip 
echo ***************************************************************