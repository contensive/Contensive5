
rem https://stackoverflow.com/questions/331520/how-to-fix-referenced-assembly-does-not-have-a-strong-name-error

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\ildasm.exe" /all /out=Stubble.Core.il unsigned-assembly\1.9.3\net472\Stubble.Core.dll 

pause

"c:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe" /dll /key=key\stubble.snk Stubble.Core.il

pause