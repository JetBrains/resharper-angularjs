@echo off
setlocal enableextensions

mkdir AngularJS.7.1 2> NUL
copy /y ..\src\resharper-angularjs\bin\Release\*.* AngularJS.7.1\
