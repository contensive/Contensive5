
rem test deploy iis package to iis site

"C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe"  -whatif -verb:sync -source:package="C:\Users\jay\Desktop\deployments\v51\Install\190909.1\iisDefaultSite.zip" -dest:iisApp=test3/ -whatif

rem "C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe"  -whatif -verb:sync -source:package="C:\Users\jay\Desktop\deployments\v51\Install\190909.1\iisDefaultSite.zip" -dest:auto -whatif

rem "C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe"  -whatif -verb:sync -source:contentPath="C:\Users\jay\Desktop\deployments\v51\Install\190909.1\iisDefaultSite.zip" -dest:metakey=lm/w3svr/2 -whatif



pause