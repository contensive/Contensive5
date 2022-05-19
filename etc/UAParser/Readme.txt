


https://github.com/ua-parser/uap-csharp

instructions: 

- clone repository

git clone https://github.com/ua-parser/uap-csharp


- download references

- open in visual studio and compile

-- his instructions:

Make sure you pull down the submodules that includes the yaml files (otherwise you won't be able to compile):

git submodule update --init --recursive
You can then build and run the tests by invoking the build.bat script

.\build.bat

(the xunit tests fail, but the UAParser DLLs are there)