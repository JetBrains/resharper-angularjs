# resharper-angularjs

A plugin for ReSharper 7.1 that adds support for AngularJS. 

## What does it do? ##

This plugin provides code completion for AngularJS attributes in HTML files. It supports the standard "ng-" sytnax, as well as the "data-ng-" syntax.

The plugin also installs a set of Live Templates for use in HTML, JavaScript, and basic support for TypeScript. The templates are grouped into helpers for module, directive, scope, global, html and routing. They can be seen and edited in the Templates Explorer.

The Live Templates are based on the IntelliJ templates by [Pawel Kozlowski](https://github.com/angularjs-livetpls/angularjs-webstorm-livetpls).

## How do I get it? ##

If you wish to just install a copy of the plugins without building yourself:

- Download the latest zip file: [resharper-angularjs.1.1.0.zip](http://download.jetbrains.com/resharper/plugins/resharper-angularjs.1.1.0.zip)
- Extract everything
- Run the Install-AngularJS.7.1.bat file

## Building ##

To build the source, you need the [ReSharper 7.1 SDK](http://www.jetbrains.com/resharper/download/index.html) installed. Then just open the src\resharper-angularjs.sln file and build.

